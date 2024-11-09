using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Plugins;
public class WarfarePluginLoader
{
    private readonly WarfareModule _warfare;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WarfarePluginLoader> _logger;
    private Assembly[]? _allAssemblies;
    public IReadOnlyList<WarfarePlugin> Plugins { get; private set; }
    public WarfarePluginLoader(WarfareModule warfare, ILoggerFactory loggerFactory)
    {
        _warfare = warfare;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<WarfarePluginLoader>();
    }

    /// <summary>
    /// List of all assemblies including this assembly and all plugin assemblies.
    /// </summary>
    internal Assembly[] AllAssemblies
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            if (_allAssemblies != null)
                return _allAssemblies;

            Assembly[] arr = new Assembly[Plugins.Count + 1];
            arr[0] = Assembly.GetExecutingAssembly();
            for (int i = 0; i < Plugins.Count; ++i)
            {
                arr[i + 1] = Plugins[i].LoadedAssembly;
            }

            _allAssemblies = arr;
            return arr;
        }
    }

    internal void LoadPlugins()
    {
        string pluginDir = Path.Combine(_warfare.HomeDirectory, "Plugins");
        
        Directory.CreateDirectory(pluginDir);

        List<WarfarePlugin> plugins = new List<WarfarePlugin>();
        foreach (string file in Directory.EnumerateFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly))
        {
            AssemblyName name;
            try
            {
                name = AssemblyName.GetAssemblyName(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to read file \"{0}\" as a plugin.", Path.GetRelativePath(pluginDir, file));
                continue;
            }

            _logger.LogInformation("Loading plugin: {0} v{1}.", name.Name, name.Version);
            
            Assembly assembly = Assembly.LoadFrom(file);

            WarfarePlugin plugin = new WarfarePlugin(name, file, assembly);
            plugins.Add(plugin);
        }

        Plugins = new ReadOnlyCollection<WarfarePlugin>(plugins.ToArray());
        _logger.LogInformation("Loaded {0} plugin(s).", plugins.Count);
    }

    internal void ConfigureServices(ContainerBuilder bldr)
    {
        bldr.RegisterInstance(this)
            .As<WarfarePluginLoader>()
            .OwnedByLifetimeScope();

        foreach (WarfarePlugin plugin in Plugins)
        {
            List<Type> allTypes = Accessor.GetTypesSafe(plugin.LoadedAssembly);

            bool configuredConfig = false;
            foreach (Type type in allTypes)
            {
                if (!type.IsPublic || type.IsAbstract || !typeof(IServiceConfigurer).IsAssignableFrom(type) || type.IsIgnored())
                    continue;

                ConstructorInfo? ctor = GetValidServiceConfigurerConstructor(type, out ParameterInfo[]? parameters);
                if (ctor == null)
                {
                    _logger.LogWarning(
                        "Service configurer type {0} in plugin {1} does not have a valid public constructor.",
                        type,
                        plugin.AssemblyName.Name
                    );
                    continue;
                }
                
                // manually inject services for IServiceConfigurer
                object[] args = InjectParameters(parameters!, type, bldr, plugin, ref configuredConfig);

                IServiceConfigurer configurer = (IServiceConfigurer)ctor.Invoke(args);
                
                try
                {
                    configurer.ConfigureServices(bldr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Service configurer type {0} in plugin {1} threw an error.",
                        type,
                        plugin.AssemblyName.Name
                    );
                }

                if (configurer is not IDisposable disposable)
                    continue;

                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Service configurer type {0} in plugin {1} threw an error when disposing.",
                        type,
                        plugin.AssemblyName.Name
                    );
                }
            }

            if (!configuredConfig)
            {
                bldr.Register(_ => new WarfarePluginConfiguration(plugin, CreateConfigurationForPlugin(plugin)))
                    .As<WarfarePluginConfiguration>()
                    .Named<WarfarePluginConfiguration>(plugin.AssemblyName.Name)
                    .OwnedByLifetimeScope();
            }

            bldr.RegisterInstance(plugin)
                .As<WarfarePlugin>()
                .Named<WarfarePlugin>(plugin.AssemblyName.Name)
                .OwnedByLifetimeScope();
        }
    }

    private object[] InjectParameters(ParameterInfo[] parameters, Type type, ContainerBuilder bldr, WarfarePlugin plugin, ref bool configuredConfig)
    {
        object[] args = parameters.Length == 0 ? Array.Empty<object>() : new object[parameters.Length];
        for (int i = 0; i < args.Length; ++i)
        {
            Type parameterType = parameters[i].ParameterType;
            WarfarePluginConfiguration? configWrapper = null;

            if (parameterType == typeof(object))
            {
                throw new InvalidOperationException(
                    $"Unable to inject service {Accessor.ExceptionFormatter.Format(parameterType)} for IServiceConfigurer type {Accessor.ExceptionFormatter.Format(type)}."
                );
            }

            // IConfiguration
            if (parameterType.IsAssignableFrom(typeof(IConfigurationRoot)))
            {
                if (configWrapper == null)
                    args[i] = GetOrCreateConfigurationForPlugin(bldr, plugin, out configWrapper);
                else
                    args[i] = configWrapper.Configuration;
                configuredConfig = true;
            }
            // ILogger[<>]
            else if (parameterType.IsAssignableFrom(typeof(ILogger))
                     || parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(ILogger<>))
            {
                args[i] = Activator.CreateInstance(typeof(Logger<>).MakeGenericType(parameterType.GetGenericArguments()[0]), _loggerFactory);
            }
            // WarfarePlugin
            else if (parameterType.IsAssignableFrom(typeof(WarfarePlugin)))
            {
                args[i] = plugin;
            }
            // ContainerBuilder
            else if (parameterType.IsAssignableFrom(typeof(ContainerBuilder)))
            {
                args[i] = bldr;
            }
            // WarfarePluginLoader
            else if (parameterType.IsAssignableFrom(typeof(WarfarePluginLoader)))
            {
                args[i] = this;
            }
            // WarfareModule
            else if (parameterType.IsAssignableFrom(typeof(WarfareModule)))
            {
                args[i] = _warfare;
            }
            // PluginConfiguration
            else if (parameterType.IsAssignableFrom(typeof(WarfarePluginConfiguration)))
            {
                if (configWrapper == null)
                    GetOrCreateConfigurationForPlugin(bldr, plugin, out configWrapper);

                args[i] = configWrapper;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unable to inject service {Accessor.ExceptionFormatter.Format(parameterType)} for IServiceConfigurer type {Accessor.ExceptionFormatter.Format(type)}."
                );
            }
        }

        return args;
    }

    private ConstructorInfo? GetValidServiceConfigurerConstructor(Type type, out ParameterInfo[]? expectedParameters)
    {
        ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        int index = Array.FindIndex(ctors, x => x.IsDefinedSafe<ActivatorUtilitiesConstructorAttribute>());

        if (index >= 0)
        {
            ConstructorInfo expectedCtor = ctors[index];
            expectedParameters = expectedCtor.GetParameters();
            return expectedCtor;
        }

        foreach (ConstructorInfo ctor in ctors.OrderByDescending(x => x.GetParameters().Length))
        {
            expectedParameters = ctor.GetParameters();

            if (expectedParameters.Any(x =>
                {
                    Type parameterType = x.ParameterType;
                    if (parameterType == typeof(object) ||
                        (!parameterType.IsAssignableFrom(typeof(IConfigurationRoot))
                         && !(parameterType.IsAssignableFrom(typeof(ILogger)) || parameterType.IsGenericType &&
                             parameterType.GetGenericTypeDefinition() == typeof(ILogger<>))
                         && !parameterType.IsAssignableFrom(typeof(WarfarePlugin))
                         && !parameterType.IsAssignableFrom(typeof(ContainerBuilder))
                         && !parameterType.IsAssignableFrom(typeof(WarfarePluginLoader))
                         && !parameterType.IsAssignableFrom(typeof(WarfareModule))
                         && !parameterType.IsAssignableFrom(typeof(WarfarePluginConfiguration))
                        ))
                        return true;

                    return false;
                }))
            {
                continue;
            }

            return ctor;
        }

        expectedParameters = null;
        return null;
    }

    // create optional plugin configuration if required
    private IConfigurationRoot GetOrCreateConfigurationForPlugin(ContainerBuilder bldr, WarfarePlugin plugin, out WarfarePluginConfiguration configWrapper)
    {
        IConfigurationRoot config = CreateConfigurationForPlugin(plugin);

        plugin.Configuration = config;

        bldr.RegisterInstance(configWrapper = new WarfarePluginConfiguration(plugin, config))
            .As<WarfarePluginConfiguration>()
            .Named<WarfarePluginConfiguration>(plugin.AssemblyName.Name)
            .OwnedByLifetimeScope();

        return config;
    }

    private IConfigurationRoot CreateConfigurationForPlugin(WarfarePlugin plugin)
    {
        if (plugin.Configuration != null)
            return plugin.Configuration;

        ReadOnlySpan<char> asmLocation = plugin.AssemblyLocation.AsSpan();

        string configLocation = Path.Join(Path.GetDirectoryName(asmLocation), Path.GetFileNameWithoutExtension(asmLocation), "Config.yml");

        IConfigurationBuilder configBuilder = new ConfigurationBuilder();

        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, _warfare.FileProvider, configLocation, optional: true);

        return configBuilder.Build();
    }
}

public class WarfarePluginConfiguration : IDisposable
{
    public WarfarePlugin Plugin { get; }
    public IConfigurationRoot Configuration { get; }
    public WarfarePluginConfiguration(WarfarePlugin plugin, IConfigurationRoot configuration)
    {
        Plugin = plugin;
        Configuration = configuration;
    }

    public void Dispose()
    {
        if (Configuration is IDisposable disposable)
            disposable.Dispose();
    }
}