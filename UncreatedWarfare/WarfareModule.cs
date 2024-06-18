using Cysharp.Threading.Tasks;
using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Modules;
using SDG.Unturned;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;

namespace Uncreated.Warfare;
public sealed class WarfareModule : IModuleNexus
{
    private IServiceScope? _activeScope;

    /// <summary>
    /// System Config.yml. Stores information not directly related to gameplay.
    /// </summary>
    public IConfiguration Configuration { get; private set; }

    /// <summary>
    /// Global service provider. Gamemodes have their own scoped service providers and should be used instead.
    /// </summary>
    public ServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// Game-specific service provider. If a game is not active, this will return <see cref="ServiceProvider"/>.
    /// </summary>
    public IServiceProvider ScopedProvider => _activeScope?.ServiceProvider ?? ServiceProvider;
    void IModuleNexus.initialize()
    {
        // Set the environment directory to the folder now at U3DS/Servers/ServerId/Warfare/
        string homeFolder = Path.Combine(UnturnedPaths.RootDirectory.Name, "Servers", Provider.serverID, "Warfare");
        Directory.CreateDirectory(homeFolder);
        Environment.CurrentDirectory = homeFolder;

        // Add system configuration provider.
        IConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, Path.Join(".", "System Config.yml"));
        Configuration = configBuilder.Build();

        IServiceCollection serviceCollection = new ServiceCollection();

        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
    void IModuleNexus.shutdown()
    {
        if (Configuration is IDisposable disposableConfig)
            disposableConfig.Dispose();
    }

    /// <summary>
    /// Start a new scope, used for each game.
    /// </summary>
    /// <returns>The newly created scope.</returns>
    internal async UniTask<IServiceProvider> CreateScope()
    {
        await UniTask.SwitchToMainThread();

        if (_activeScope is IAsyncDisposable asyncDisposableScope)
        {
            ValueTask vt = asyncDisposableScope.DisposeAsync();
            _activeScope = null;
            await vt.ConfigureAwait(false);
            await UniTask.SwitchToMainThread();
        }
        else if (_activeScope is IDisposable disposableScope)
        {
            disposableScope.Dispose();
            _activeScope = null;
        }

        IServiceScope scope = ServiceProvider.CreateScope();
        _activeScope = scope;
        return scope.ServiceProvider;
    }

    private void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContext<WarfareDbContext>(contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Singleton);

        serviceCollection.AddReflectionTools();
        serviceCollection.AddModularRpcs(isServer: false, searchedAssemblies: [ Assembly.GetExecutingAssembly() ]);

        serviceCollection.AddSingleton<ActionManager>();
        serviceCollection.AddSingleton<CommandDispatcher>();
        serviceCollection.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<CommandDispatcher>().Parser);
        serviceCollection.AddRpcSingleton<UserPermissionStore>();
    }
}
