using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts;
public class LayoutFactory : IHostedService
{
    private readonly WarfareModule _warfare;
    private readonly ILogger<LayoutFactory> _logger;

    public LayoutFactory(WarfareModule warfare, ILogger<LayoutFactory> logger)
    {
        _warfare = warfare;
        _logger = logger;
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        Level.onPostLevelLoaded += OnLevelLoaded;
        return default;
    }

    /// <inheritdoc />
    public async UniTask StopAsync(CancellationToken token)
    {
        Level.onPostLevelLoaded -= OnLevelLoaded;

        if (!_warfare.IsLayoutActive())
            return;
        
        Layout layout = _warfare.GetActiveLayout();
        if (layout.IsActive)
        {
            await layout.EndLayoutAsync(CancellationToken.None);
        }

        layout.Dispose();
        _warfare.SetActiveLayout(null);
    }

    private void OnLevelLoaded(int level)
    {
        if (level != Level.BUILD_INDEX_GAME)
            return;

        UniTask.Create(async () =>
        {
            try
            {
                await StartNextLayout(_warfare.UnloadToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting layout.");
                _ = _warfare.ShutdownAsync($"Error starting layout - {Accessor.Formatter.Format(ex.GetType())}");
            }
        });
    }

    /// <summary>
    /// Forcibly end the current layout and create a new random layout from the config files.
    /// </summary>
    public async UniTask StartNextLayout(CancellationToken token = default)
    {
        await UniTask.SwitchToThreadPool();
        
        LayoutInfo newLayout = SelectRandomLayouts();

        IPlayerService? playerServiceImpl = _warfare.ServiceProvider.ResolveOptional<IPlayerService>();

        await UniTask.SwitchToMainThread(token);

        // stops players from joining both before the first layout starts and between layouts.
        bool playerJoinLockTaken = false;

        try
        {
            if (_warfare.IsLayoutActive())
            {
                if (playerServiceImpl != null)
                {
                    await playerServiceImpl.TakePlayerConnectionLock(token);
                    playerJoinLockTaken = true;
                }

                Layout oldLayout = _warfare.GetActiveLayout();
                if (oldLayout.IsActive)
                {
                    await oldLayout.EndLayoutAsync(CancellationToken.None);
                }

                oldLayout.Dispose();
                _warfare.SetActiveLayout(null);
            }
            else
            {
                // lock is on by default on startup.
                playerJoinLockTaken = true;

                // invoke ILevelHostedService
                await _warfare.InvokeLevelLoaded(CancellationToken.None);
            }

            await CreateLayoutAsync(newLayout, CancellationToken.None);
        }
        finally
        {
            if (playerJoinLockTaken && playerServiceImpl != null)
            {
                playerServiceImpl.ReleasePlayerConnectionLock();
            }
        }
    }

    /// <summary>
    /// Actually creates a new layout with <paramref name="layoutInfo"/> as it's startup args.
    /// </summary>
    public async UniTask CreateLayoutAsync(LayoutInfo layoutInfo, CancellationToken token = default)
    {
        if (!typeof(Layout).IsAssignableFrom(layoutInfo.LayoutType))
        {
            throw new ArgumentException($"Type {Accessor.ExceptionFormatter.Format(layoutInfo.LayoutType)} is not assignable to Layout.", nameof(layoutInfo));
        }

        Action<ContainerBuilder>? lifetimeAction = null;

        // load plugin services
        IConfigurationSection serviceInfo = layoutInfo.Layout.GetSection("Services");
        if (serviceInfo.GetChildren().Any())
        {
            lifetimeAction = TryLoadServicesFromServiceList(layoutInfo, serviceInfo);
        }

        ILifetimeScope scopedProvider = await _warfare.CreateScopeAsync(lifetimeAction, token);
        await UniTask.SwitchToMainThread(token);

        // active layout is set in Layout default constructor
        Layout layout = (Layout)Activator.CreateInstance(layoutInfo.LayoutType, [ scopedProvider, layoutInfo ]);
        if (_warfare.GetActiveLayout() != layout)
        {
            _warfare.SetActiveLayout(layout);
        }

        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(layout.UnloadToken);
        await layout.InitializeLayoutAsync(CancellationToken.None);

        IEnumerable<ILayoutStartingListener> listeners = scopedProvider.Resolve<IEnumerable<ILayoutStartingListener>>()
            .OrderByDescending(x => x.GetType().GetPriority());

        foreach (ILayoutStartingListener listener in listeners)
        {
            try
            {
                if (!GameThread.IsCurrent)
                    await UniTask.SwitchToMainThread(CancellationToken.None);

                await listener.HandleLayoutStartingAsync(layout, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hosting ILayoutStartingListener {0} for layout {1}.", Accessor.Formatter.Format(listener.GetType()), layout);
            }
        }

        await layout.BeginLayoutAsync(CancellationToken.None);
    }

    /// <summary>
    /// Loads all instances of <see cref="ILayoutServiceConfigurer"/> from the 'Services' list in the config and creates a lifetime scope builder using them.
    /// </summary>
    private Action<ContainerBuilder> TryLoadServicesFromServiceList(LayoutInfo layoutInfo, IConfiguration serviceInfo)
    {
        List<(string, Type?)> types = serviceInfo.GetChildren()
            .Select(x => (x.Value, ContextualTypeResolver.ResolveType(x.Value, typeof(ILayoutServiceConfigurer))))
            .ToList();

        List<Exception>? missingTypeExceptions = null;
        foreach ((string typeName, Type? configurerType) in types)
        {
            if (configurerType == null)
            {
                (missingTypeExceptions ??= new List<Exception>(1)).Add(new TypeLoadException($"Failed to find a type by the name \"{typeName}\" in \"{layoutInfo.FilePath}\"."));
            }
        }

        if (missingTypeExceptions is { Count: > 0 })
        {
            throw new AggregateException(missingTypeExceptions);
        }

        object[] parameters = [ layoutInfo, serviceInfo ];
        return bldr =>
        {
            IServiceProvider serviceProvider = _warfare.ServiceProvider.Resolve<IServiceProvider>();
            foreach ((_, Type? configurerType) in types)
            {
                ILayoutServiceConfigurer configurer =
                    (ILayoutServiceConfigurer)ReflectionUtility.CreateInstanceFixed(serviceProvider,
                        configurerType!, parameters);

                try
                {
                    configurer.ConfigureServices(bldr);

                    _logger.LogInformation(
                        "Registered services using configurer {0} from assembly {1}.",
                        Accessor.Formatter.Format(configurerType!),
                        configurerType!.Assembly.GetName().Name
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to configure services using type {0} from assembly {1}.",
                        Accessor.Formatter.Format(configurerType!),
                        configurerType!.Assembly.GetName().Name
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
                    _logger.LogError(ex,
                        "Failed to dispose configurer {0} from assembly {1}.",
                        Accessor.Formatter.Format(configurerType!),
                        configurerType!.Assembly.GetName().Name
                    );
                }
            }
        };
    }

    /// <summary>
    /// Read all layouts and select a random one.
    /// </summary>
    /// <remarks>Reading them each time keeps us from having to reload config.</remarks>
    /// <exception cref="InvalidOperationException">No layouts are configured.</exception>
    public LayoutInfo SelectRandomLayouts()
    {
        List<LayoutInfo> layouts = GetBaseLayoutFiles()
            .Select(x => ReadLayoutInfo(x.FullName))
            .Where(x => x != null)
            .ToList()!;

        if (layouts.Count == 0)
        {
            throw new InvalidOperationException("There are no layouts configured.");
        }

        int index = RandomUtility.GetIndex(layouts, x => x.Weight);
        return layouts[index];
    }

    /// <summary>
    /// Read a <see cref="LayoutInfo"/> from the given file and open a configuration root for the file.
    /// </summary>
    public LayoutInfo? ReadLayoutInfo(string file)
    {
        if (!File.Exists(file))
            return null;

        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, _warfare.FileProvider, file);
        IConfigurationRoot root = configBuilder.Build();

        // read the full type name from the config file
        string? layoutTypeName = root["Type"];
        if (layoutTypeName == null)
        {
            _logger.LogDebug("Layout config file missing \"Type\" config value in \"{0}\".", file);
            return null;
        }

        Type? layoutType = ContextualTypeResolver.ResolveType(layoutTypeName, typeof(Layout));
        if (layoutType == null)
        {
            _logger.LogError("Unknown layout type \"{0}\" in layout config \"{1}\".", layoutTypeName, file);
            return null;
        }

        // read the selection weight from the config file
        if (!double.TryParse(root["Weight"], NumberStyles.Number, CultureInfo.InvariantCulture, out double weight))
        {
            weight = 1;
        }

        // read display name
        if (root["Name"] is not { Length: > 0 } displayName)
        {
            displayName = Path.GetFileNameWithoutExtension(file);
        }

        return new LayoutInfo
        {
            LayoutType = layoutType,
            Layout = root,
            Weight = weight,
            DisplayName = displayName,
            FilePath = file
        };
    }

    /// <summary>
    /// Get all base config files in the layout folder.
    /// </summary>
    public List<FileInfo> GetBaseLayoutFiles()
    {
        DirectoryInfo layoutDirectory = new DirectoryInfo(Path.Join(_warfare.HomeDirectory, "Layouts"));

        // get all folders or yaml files.
        List<FileSystemInfo> layouts = layoutDirectory
            .GetFileSystemInfos("*", SearchOption.TopDirectoryOnly)
            .Where(x => x is DirectoryInfo || Path.GetExtension(x.FullName).Equals(".yml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<FileInfo> baseLayoutConfigs = new List<FileInfo>(layouts.Count);
        foreach (FileSystemInfo layout in layouts)
        {
            switch (layout)
            {
                case FileInfo { Length: > 0 } yamlFile:
                    baseLayoutConfigs.Add(yamlFile);
                    break;

                case DirectoryInfo dir:
                    FileInfo[] files = dir.GetFiles("*.yml", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0)
                        break;

                    // find file with least periods in it's name to get which one doesn't have a map-specific config.
                    baseLayoutConfigs.Add(files.Aggregate((least, next) => next.Name.Count(x => x == '.') < least.Name.Count(x => x == '.') ? next : least));
                    break;
            }
        }

        return baseLayoutConfigs;
    }

    /// <summary>
    /// Host a new layout, starting all <see cref="ILayoutHostedService"/> services. Should only be called from <see cref="Layout.BeginLayoutAsync"/>.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="Exception"/>
    internal async UniTask HostLayoutAsync(Layout layout, CancellationToken token)
    {
        // start any services implementing ILayoutHostedService
        List<ILayoutHostedService> hostedServices = layout.ServiceProvider
            .Resolve<IEnumerable<ILayoutHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        Exception? thrownException = null;
        int errIndex = -1;
        for (int i = 0; i < hostedServices.Count; ++i)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await UniTask.SwitchToMainThread(token);
                await hostedServices[i].StartAsync(token);
            }
            catch (OperationCanceledException ex) when (token.IsCancellationRequested)
            {
                _logger.LogWarning("Layout {0} canceled.", layout);
                errIndex = i;
                thrownException = ex;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hosting service {0} in layout {1}.", Accessor.Formatter.Format(hostedServices[i].GetType()), layout);
                errIndex = i;
                thrownException = ex;
            }
        }

        // handles if a service failed to start up, unloads the services that did start up and ends the layout.
        if (errIndex == -1)
            return;
        
        await UniTask.SwitchToMainThread();

        if (!layout.UnloadedHostedServices)
        {
            UniTask[] tasks = new UniTask[errIndex];
            for (int i = errIndex - 1; i >= 0; --i)
            {
                ILayoutHostedService hostedService = hostedServices[i];
                try
                {
                    tasks[i] = hostedService.StopAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping service {0} in layout {1}.", Accessor.Formatter.Format(hostedServices[i].GetType()), layout);
                }
            }

            layout.UnloadedHostedServices = true;

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                _logger.LogError("Errors encountered while ending layout {0}:", layout);
                FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
            }
        }

        await layout.EndLayoutAsync(CancellationToken.None);

        if (thrownException is OperationCanceledException && token.IsCancellationRequested)
            ExceptionDispatchInfo.Capture(thrownException).Throw();

        throw new Exception($"Failed to load layout {layout}.", thrownException);
    }

    /// <summary>
    /// Stop hosting a layout, stopping all <see cref="ILayoutHostedService"/> services. Should only be called from <see cref="Layout.EndLayoutAsync"/>.
    /// </summary>
    internal async UniTask UnhostLayoutAsync(Layout layout, CancellationToken token)
    {
        if (layout.UnloadedHostedServices)
            return;

        // stop any services implementing ILayoutHostedService
        List<ILayoutHostedService> hostedServices = layout.ServiceProvider
            .Resolve<IEnumerable<ILayoutHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < tasks.Length; ++i)
        {
            try
            {
                tasks[i] = hostedServices[i].StopAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                tasks[i] = UniTask.FromException(ex);
            }
        }

        layout.UnloadedHostedServices = true;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch
        {
            _logger.LogError("Errors encountered while ending layout {0}:", layout);
            FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
        }
    }
}
