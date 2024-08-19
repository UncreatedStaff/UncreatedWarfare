using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.ListenerProviders;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Squads.UI;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Events;
using Uncreated.Warfare.Zones;
using Module = SDG.Framework.Modules.Module;

namespace Uncreated.Warfare;
public sealed class WarfareModule : IModuleNexus
{
    private static EventDispatcher2? _dispatcher;

#nullable disable

    /// <summary>
    /// Static instance of the event dispatcher singleton for harmony patches to access it.
    /// </summary>
    /// <remarks>Do not use unless in a patch.</remarks>
    public static EventDispatcher2 EventDispatcher => _dispatcher ??= Singleton?.ScopedProvider.GetRequiredService<EventDispatcher2>();

    /// <summary>
    /// Static instance of this module singleton for harmony patches to access it.
    /// </summary>
    /// <remarks>Do not use unless in a patch.</remarks>
    public static WarfareModule Singleton { get; private set; }

#nullable restore

    private bool _unloadedHostedServices;
    private IServiceScope? _activeScope;
    private CancellationTokenSource _cancellationTokenSource;
    private Layout? _activeLayout;
    private GameObject _gameObjectHost;

    /// <summary>
    /// A path to the top-level 'Warfare' folder.
    /// </summary>
    public string HomeDirectory { get; private set; }

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

    /// <summary>
    /// Token that will cancel when the module shuts down.
    /// </summary>
    public CancellationToken UnloadToken
    {
        get
        {
            try
            {
                return _cancellationTokenSource.Token;
            }
            catch (ObjectDisposedException)
            {
                return new CancellationToken(true);
            }
        }
    }

    void IModuleNexus.initialize()
    {
        // will setup the main thread in GameThread before asserting
        GameThread.AssertCurrent();

        Singleton = this;
        _gameObjectHost = new GameObject("Uncreated.Warfare");
        _cancellationTokenSource = new CancellationTokenSource();

        ConfigurationSettings.SetupTypeConverters();

        // Set the environment directory to the folder now at U3DS/Servers/ServerId/Warfare/
        HomeDirectory = Path.Combine(UnturnedPaths.RootDirectory.Name, "Servers", Provider.serverID, "Warfare");
        Directory.CreateDirectory(HomeDirectory);

        // Add system configuration provider.
        IConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, Path.Join(HomeDirectory, "System Config.yml"));
        Configuration = configBuilder.Build();

        IServiceCollection serviceCollection = new ServiceCollection();

        // todo rewrite logging
        serviceCollection.AddSingleton<ILoggerFactory, L.UCLoggerFactory>();

        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        UniTask.Create(HostAsync);
    }

    void IModuleNexus.shutdown()
    {
        if (Singleton == this)
            Singleton = null;

        _dispatcher = null;

        if (Configuration is IDisposable disposableConfig)
            disposableConfig.Dispose();

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (AggregateException ex)
        {
            L.LogError("Error(s) while canceling module cancellation token source.");
            L.LogError(ex);
        }

        _cancellationTokenSource.Dispose();

        if (!_unloadedHostedServices)
        {
            Unhost();
        }
        
        ServiceProvider.Dispose();
    }

    private void ConfigureServices(IServiceCollection serviceCollection)
    {
        Assembly thisAsm = Assembly.GetExecutingAssembly();

        serviceCollection.AddTransient<IManualMySqlProvider, ManualMySqlProvider>(_ =>
        {
            string connectionString = UCWarfare.Config.SqlConnectionString ??
                                      (UCWarfare.Config.RemoteSQL ?? UCWarfare.Config.SQL).GetConnectionString("Uncreated.Warfare", true, true);

            return new ManualMySqlProvider(connectionString);
        });

        // global zones (not used for layouts)
        serviceCollection.AddTransient<IZoneProvider, MapZoneProvider>();
        serviceCollection.AddSingleton(serviceProvider => new ZoneStore(serviceProvider.GetServices<IZoneProvider>(), serviceProvider.GetRequiredService<ILogger<ZoneStore>>(), true));

        serviceCollection.AddDbContext<WarfareDbContext>(contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Singleton);

        serviceCollection.AddReflectionTools();
        serviceCollection.AddModularRpcs(isServer: false, searchedAssemblies: [Assembly.GetExecutingAssembly()]);

        serviceCollection.AddSingleton(this);
        serviceCollection.AddSingleton(ModuleHook.modules.First(x => x.config.Name.Equals("Uncreated.Warfare", StringComparison.Ordinal) && x.assemblies.Contains(thisAsm)));

        // UI
        serviceCollection.AddSingleton<ModerationUI>();

        serviceCollection.AddSingleton<KitMenuUI>();

        serviceCollection.AddSingleton<ActionMenuUI>();

        serviceCollection.AddSingleton<SquadMenuUI>();
        serviceCollection.AddSingleton<SquadListUI>();

        // event handlers
        serviceCollection.AddTransient<VehicleSpawnedHandler>();

        serviceCollection.AddTransient<SteamAPIService>();

        serviceCollection.AddSingleton<AudioRecordManager>();

        serviceCollection.AddSingleton<LayoutFactory>();
        serviceCollection.AddSingleton<ActionManager>();
        serviceCollection.AddSingleton<EventDispatcher2>();
        serviceCollection.AddSingleton<CommandDispatcher>();
        serviceCollection.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<CommandDispatcher>().Parser);
        serviceCollection.AddRpcSingleton<UserPermissionStore>();
        serviceCollection.AddSingleton(_gameObjectHost);

        serviceCollection.AddScoped<BuildableSaver>();
        serviceCollection.AddSingleton<VehicleInfoStore>();
        serviceCollection.AddSingleton<AbandonService>();
        serviceCollection.AddSingleton<VehicleService>();

        serviceCollection.AddTransient<ILoopTickerFactory, UnityLoopTickerFactory>();

        // Players
        serviceCollection.AddSingleton<PlayerService>();
        serviceCollection.AddSingleton<IEventListenerProvider, PlayerComponentListenerProvider>();

        // Kits
        KitManager.ConfigureServices(serviceCollection);
        serviceCollection.AddSingleton<WhitelistService>();

        // Layouts
        serviceCollection.AddTransient(serviceProvider => serviceProvider.GetRequiredService<WarfareModule>().GetActiveLayout());
        serviceCollection.AddTransient<IEventListenerProvider>(serviceProvider => serviceProvider.GetRequiredService<Layout>());

        // Active ILayoutPhase
        serviceCollection.AddTransient(serviceProvider => serviceProvider.GetRequiredService<WarfareModule>().GetActiveLayout().ActivePhase
                                                          ?? throw new InvalidOperationException("There is not a phase currently loaded."));

        // All layout types so they can be individually requested (i'm not too sure about adding this)
        foreach (Type type in Accessor.GetTypesSafe(thisAsm).Where(x => x.IsSubclassOf(typeof(Layout))))
        {
            serviceCollection.Add(new ServiceDescriptor(type, _ =>
            {
                Layout session = GetActiveLayout();
                if (!type.IsInstanceOfType(session))
                {
                    throw new InvalidOperationException($"The current layout type is not {Accessor.ExceptionFormatter.Format(type)}.");
                }

                return session;
            }, ServiceLifetime.Transient));
        }

        // FOBs
        serviceCollection.AddScoped<DeploymentService>();

        // Active ITeamManager
        serviceCollection.AddTransient(serviceProvider => serviceProvider.GetRequiredService<WarfareModule>().GetActiveLayout().TeamManager);

        // Localization
        serviceCollection.AddSingleton<LanguageService>();
        serviceCollection.AddSingleton<ILanguageDataStore, MySqlLanguageDataStore<WarfareDbContext>>();

        // Translations
        serviceCollection.AddSingleton<ITranslationValueFormatter, TranslationValueFormatter>();
        serviceCollection.AddSingleton<ITranslationService, TranslationService>();
        serviceCollection.AddTransient(typeof(TranslationInjection<>));
    }

    public async UniTask ShutdownAsync(CancellationToken token = default)
    {
        if (!_unloadedHostedServices)
        {
            await UnhostAsync(token);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);
        UnloadModule();
        Provider.shutdown();
    }

    /// <summary>
    /// Start all hosted services.
    /// </summary>
    private async UniTask HostAsync()
    {
        CancellationToken token = UnloadToken;

        List<IHostedService> hostedServices = ServiceProvider
            .GetServices<IHostedService>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        int errIndex = -1;
        for (int i = 0; i < hostedServices.Count; i++)
        {
            IHostedService hostedService = hostedServices[i];
            try
            {
                await UniTask.SwitchToMainThread(token);
                await hostedService.StartAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                L.LogError($"Error hosting service {Accessor.Formatter.Format(hostedService.GetType())}.");
                L.LogError(ex);
                errIndex = i;
                break;
            }
        }

        // one of the hosted services errored, unhost all that were hosted and shut down.
        if (errIndex == -1)
            return;

        await UniTask.SwitchToMainThread(token);

        if (_unloadedHostedServices)
            return;

        _unloadedHostedServices = true;
        UniTask[] tasks = new UniTask[errIndex];
        for (int i = errIndex - 1; i >= 0; --i)
        {
            IHostedService hostedService = hostedServices[i];
            try
            {
                tasks[i] = hostedService.StopAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                L.LogError($"Error stopping service {Accessor.Formatter.Format(hostedService.GetType())}.");
                L.LogError(ex);
            }
        }

        _unloadedHostedServices = true;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch
        {
            await UniTask.SwitchToMainThread();
            L.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);
        UnloadModule();
        Provider.shutdown();
    }

    private async UniTask UnhostAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (_unloadedHostedServices)
            return;

        List<IHostedService> hostedServices = ServiceProvider
            .GetServices<IHostedService>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < hostedServices.Count; ++i)
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

        _unloadedHostedServices = true;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch
        {
            await UniTask.SwitchToMainThread();
            L.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
        }
    }

    /// <summary>
    /// Synchronously unhost so <see cref="IModuleNexus.shutdown"/> can wait on unhost.
    /// </summary>
    private void Unhost()
    {
        _unloadedHostedServices = true;
        using CancellationTokenSource timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3d));

        List<IHostedService> hostedServices = ServiceProvider
            .GetServices<IHostedService>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < hostedServices.Count; ++i)
        {
            try
            {
                tasks[i] = hostedServices[i].StopAsync(timeoutSource.Token);
            }
            catch (Exception ex)
            {
                tasks[i] = UniTask.FromException(ex);
            }
        }

        bool canceled = false;
        try
        {
            UniTask.WhenAll(tasks).AsTask().Wait(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            canceled = true;
        }
        catch
        {
            L.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(tasks, hostedServices);
            Thread.Sleep(500);
            return;
        }

        if (!canceled)
            return;

        L.LogError("Unloading timed out:");
        for (int i = 0; i < tasks.Length; ++i)
        {
            if (tasks[i].Status is not UniTaskStatus.Canceled and not UniTaskStatus.Pending)
                continue;

            L.LogError(Accessor.Formatter.Format(hostedServices[i].GetType()) + $" - {tasks[i].Status}.");
        }
        Thread.Sleep(500);
    }

    /// <summary>
    /// Start a new scope, used for each game.
    /// </summary>
    /// <returns>The newly created scope.</returns>
    internal async UniTask<IServiceProvider> CreateScopeAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _activeLayout = null;
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

    /// <summary>
    /// Set the new game after calling <see cref="CreateScopeAsync"/>.
    /// </summary>
    internal void SetActiveLayout(Layout? layout)
    {
        Layout? oldLayout = Interlocked.Exchange(ref _activeLayout, layout);
        if (oldLayout == null || layout == null)
            return;

        L.LogError("A layout was started while one was already active.");
        oldLayout.Dispose();
    }

    /// <summary>
    /// Check if there is an active layout.
    /// </summary>
    public bool IsLayoutActive() => _activeLayout != null;

    /// <summary>
    /// Get the active layout.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is not an active layout.</exception>
    public Layout GetActiveLayout()
    {
        return _activeLayout ?? throw new InvalidOperationException("There is not an active layout.");
    }

    private void UnloadModule()
    {
        ServiceProvider.GetRequiredService<Module>().isEnabled = false;
    }
}
