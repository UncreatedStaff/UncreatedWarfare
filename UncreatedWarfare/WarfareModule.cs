using DanielWillett.ModularRpcs;
using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using HarmonyLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MySqlConnector;
using SDG.Framework.Modules;
using StackCleaner;
using Stripe;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Discord;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.ListenerProviders;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.UI;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction.Tweaks;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.Deployment.Tweaks;
using Uncreated.Warfare.FOBs.StateStorage;
using Uncreated.Warfare.FOBs.StateStorage.Tweaks;
using Uncreated.Warfare.FOBs.SupplyCrates.AutoResupply;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Kits.Tweaks;
using Uncreated.Warfare.Kits.UI;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Seeding;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Discord;
using Uncreated.Warfare.Moderation.GlobalBans;
using Uncreated.Warfare.Moderation.Reports;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Networking.Purchasing;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Players.Tweaks;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Projectiles;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Quests.Daily;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Sessions;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Squads.Signs;
using Uncreated.Warfare.Squads.Spotted;
using Uncreated.Warfare.Squads.UI;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Stats.EventHandlers;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Storage;
using Uncreated.Warfare.Tweaks;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Events;
using Uncreated.Warfare.Vehicles.Events.Tweaks;
using Uncreated.Warfare.Vehicles.Events.Tweaks.AdvancedDamage;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.UI;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;
using Module = SDG.Framework.Modules.Module;
#if TELEMETRY
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using System.Diagnostics;
using DanielWillett.ReflectionTools.Formatting;
#endif

namespace Uncreated.Warfare;
public sealed class WarfareModule
{
    /// <summary>
    /// The current season.
    /// </summary>
    public static readonly int Season = typeof(WarfareModule).Assembly.GetName().Version.Major;

    private static EventDispatcher? _dispatcher;

#nullable disable

    /// <summary>
    /// Static instance of the event dispatcher singleton for harmony patches to access it.
    /// </summary>
    /// <remarks>Do not use unless in a patch.</remarks>
    public static EventDispatcher EventDispatcher => _dispatcher ??= Singleton?.ServiceProvider.Resolve<EventDispatcher>();

    /// <summary>
    /// Static instance of this module singleton for harmony patches to access it.
    /// </summary>
    /// <remarks>Do not use unless in a patch.</remarks>
    public static WarfareModule Singleton { get; private set; }

    /// <summary>
    /// If Uncreated.Warfare is loaded as a module instead of as a library.
    /// </summary>
    public static bool IsActive { get; private set; }

    private CancellationTokenSource _cancellationTokenSource;
    private GameObject _gameObjectHost;
    private ILogger<WarfareModule> _logger;
    private WarfarePluginLoader _pluginLoader;


#nullable restore

    private IDisposable? _systemConfigChangeToken;

    public event Action? LayoutStarted;
    private bool _unloadedHostedServices = true;
    private ILifetimeScope? _activeScope;
    private Layout? _activeLayout;

#nullable disable
    /// <summary>
    /// A global logger that can be used from patches mainly.
    /// </summary>
    public ILogger GlobalLogger { get; private set; }

    /// <summary>
    /// A path to the top-level 'Warfare' folder.
    /// </summary>
    public string HomeDirectory { get; private set; }

    /// <summary>
    /// System Config.yml. Stores information not directly related to gameplay.
    /// </summary>
    public IConfiguration Configuration { get; private set; }

    /// <summary>
    /// Handles tracking file changing and configuration for any files in <c>Servers/[Server ID]/Warfare/</c>.
    /// </summary>
    public PhysicalFileProvider FileProvider { get; private set; }

    /// <summary>
    /// Global service provider. Gamemodes have their own scoped service providers and should be used instead.
    /// </summary>
    public IContainer ServiceProvider { get; private set; }

    /// <summary>
    /// Game-specific service provider. If a game is not active, this will throw an error.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public ILifetimeScope ScopedProvider => _activeScope ?? throw new InvalidOperationException("A scope hasn't been created yet.");

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
#nullable restore
    internal void Initialize()
    {
        IsActive = true;

        // will setup the main thread in GameThread before asserting
        GameThread.Setup();
        GameThread.AssertCurrent();

        // setup UniTask
        InitializeUniTask();

        // this needs to be separated to give the above events time to be subscribed before loading types
        Init();
    }

    // for unit testing
    internal void InitForTests(ContainerBuilder bldr)
    {
        HomeDirectory = Path.Combine(Environment.CurrentDirectory, "Warfare");

        Directory.CreateDirectory(HomeDirectory);

        // Add system configuration provider.
        IConfigurationBuilder configBuilder = new ConfigurationBuilder();

        string systemConfigLocation = Path.Join(HomeDirectory, "System Config.yml");

        FileProvider = new PhysicalFileProvider(HomeDirectory, ExclusionFilters.Sensitive);

        ConfigurationHelper.AddJsonOrYamlFile(configBuilder, FileProvider, systemConfigLocation, optional: true, reloadOnChange: true);
        Configuration = configBuilder.Build();

        bldr.RegisterInstance(Configuration);

        bldr.RegisterBuildCallback(s => ServiceProvider = (IContainer)s);
    }

#if TELEMETRY
    private static readonly DefaultOpCodeFormatter TelemetrySourceFormatter = new DefaultOpCodeFormatter
    {
        UseFullTypeNames = true
    };

#nullable disable
    private ActivitySource _activitySource;
#nullable restore

    private static readonly string Version = typeof(WarfareModule).Assembly.GetName().Version.ToString();

    internal static void RecordActivityException(Activity? activity, Exception ex, bool stop = true)
    {
        if (activity == null)
            return;

        activity.AddEvent(new ActivityEvent("Exception thrown"));

        if (stop && !activity.IsStopped)
            activity.Stop();
        
        activity.AddTag("exception", Accessor.Formatter.Format(ex.GetType()));
        activity.AddTag("exception-info", ex.ToString());
        activity.SetStatus(ActivityStatusCode.Error, "Error thrown.");
    }

    internal static void RecordActivityCancelled(Activity? activity, bool stop = true)
    {
        if (activity == null)
            return;

        activity.AddEvent(new ActivityEvent("Cancelled."));

        if (stop && !activity.IsStopped)
            activity.Stop();
        
        activity.SetStatus(ActivityStatusCode.Error);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActivitySource CreateActivitySource()
    {
        string source = "Uncreated.Warfare";
        try
        {
            StackTrace st = new StackTrace(1, false);
            MethodBase? method = st.GetFrame(0)?.GetMethod();
            if (method?.DeclaringType != null)
                source = TelemetrySourceFormatter.Format(method.DeclaringType);
        }
        catch
        {
            // ignored
        }

        //Singleton.GlobalLogger.LogInformation($"Created activity source: {source}."); ( throws error on init )
        return new ActivitySource(source, Version);
    }
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Init()
    {
        Singleton = this;

        _gameObjectHost = new GameObject("Uncreated.Warfare");
        Object.DontDestroyOnLoad(_gameObjectHost);

        _cancellationTokenSource = new CancellationTokenSource();

        ConfigurationSettings.SetupTypeConverters();

        // cant create the real logger factory until the service provider is built, but we need a logger to load plugins
        using ILoggerFactory tempLoggerFactory =
            new LoggerFactory(
            [
                new WarfareLoggerProvider(null)
            ],
            new LoggerFilterOptions { MinLevel = LogLevel.Trace }
        );

        // adds the plugin to the server lobby screen and sets the plugin framework type to 'Unknown'.
        IPluginAdvertising pluginAdvService = PluginAdvertising.Get();
        pluginAdvService.AddPlugin("Uncreated Warfare");
        pluginAdvService
            .GetType()
            .GetProperty("PluginFrameworkTag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            ?.GetSetMethod(true)
            ?.Invoke(pluginAdvService, [ "uw" ]);

        // Disables the socket that hosts the Connection Code functionality. It's nice to keep it in DEBUG for testing purposes without port forwarding.
#if RELEASE
        if (Type.GetType("SDG.NetTransport.SteamNetworkingSockets.ServerTransport_SteamNetworkingSockets, Assembly-CSharp")
            ?.GetField("clUseP2pSocket", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) is CommandLineFlag clUseP2PSocket)
        {
            clUseP2PSocket.value = false;
        }
#endif

        Provider.modeConfigData.Players.Lose_Items_PvP = 0;
        Provider.modeConfigData.Players.Lose_Items_PvE = 0;
        Provider.modeConfigData.Players.Lose_Clothes_PvP = false;
        Provider.modeConfigData.Players.Lose_Clothes_PvE = false;
        Provider.modeConfigData.Barricades.Decay_Time = 0;
        Provider.modeConfigData.Structures.Decay_Time = 0;

        CommandLogMemoryUsage.OnExecuted += LogMemoryUsage;

        HomeDirectory = Path.Combine(UnturnedPaths.RootDirectory.FullName, "Servers", Provider.serverID, "Warfare");
        Directory.CreateDirectory(HomeDirectory);

        // Add system configuration provider.
        IConfigurationBuilder configBuilder = new ConfigurationBuilder();

        string systemConfigLocation = Path.Join(HomeDirectory, "System Config.yml");

        FileProvider = new PhysicalFileProvider(HomeDirectory, ExclusionFilters.Sensitive);

        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, FileProvider, systemConfigLocation);
        Configuration = configBuilder.Build();

        _systemConfigChangeToken = ChangeToken.OnChange(
            () => Configuration.GetReloadToken(),
            HandleSystemConfigUpdated
        );

        ContainerBuilder bldr = new ContainerBuilder();

        _pluginLoader = new WarfarePluginLoader(this, tempLoggerFactory);
        _pluginLoader.LoadPlugins();

        bldr.RegisterType<WarfareLoggerProvider>()
            .As<ILoggerProvider>().AsSelf()
            .OwnedByLifetimeScope();

        bldr.Register(x => x.Resolve<WarfareLoggerProvider>().StackCleaner)
            .SingleInstance();

        bldr.RegisterFromCollection(collection =>
        {
            collection.AddTransient<IOptionsMonitor<LoggerFilterOptions>, LoggerOptionsMonitor>();
            collection.Configure<LoggerFactoryOptions>(_ => { });
            collection.AddLogging(l =>
            {
#if TELEMETRY
                //l.AddOpenTelemetry(ot =>
                //{
                //    ot.AddConsoleExporter();
                //});
#endif
            });
        });

        // service configurers from config
        foreach (IConfigurationSection typeName in Configuration.GetSection("services").GetChildren())
        {
            Type? type = ContextualTypeResolver.ResolveType(typeName.Value, typeof(IServiceConfigurer));
            if (type == null)
            {
                CommandWindow.LogError($"Service configurer not found in system config: {typeName.Value}.");
                UnloadModule();
                Provider.shutdown();
                return;
            }

            try
            {
                IServiceConfigurer c = (IServiceConfigurer)Activator.CreateInstance(type);
                c.ConfigureServices(bldr);
                if (c is IDisposable disp)
                    disp.Dispose();
                CommandWindow.Log($"Loaded service configurer {Accessor.Formatter.Format(type)} from system config.");
            }
            catch (Exception ex)
            {
                CommandWindow.LogError($"Service configurer {Accessor.Formatter.Format(type)} from system config threw an exception:");
                CommandWindow.LogError(ex);
                UnloadModule();
                Provider.shutdown();
                return;
            }
        }

#if TELEMETRY
        _activitySource = CreateActivitySource();
#endif

        ConfigureServices(bldr);

        _pluginLoader.ConfigureServices(bldr);

        ServiceProvider = bldr.Build();

        Accessor.Logger = new ReflectionToolsLoggerProxy(ServiceProvider.Resolve<ILoggerFactory>());
#if DEBUG
        Accessor.LogDebugMessages = true;
#endif
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        _logger = ServiceProvider.Resolve<ILogger<WarfareModule>>();

        GlobalLogger = ServiceProvider.Resolve<ILoggerFactory>().CreateLogger("Global");

        _logger.LogInformation($"Using {ServiceProvider.ComponentRegistry.Registrations.Count()} services from core and {_pluginLoader.Plugins.Count} plugin(s).");

        UniTask.Create(async () =>
        {
            try
            {
                UniTask t = HostAsync();
                await t;
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                CommandWindow.LogError(ExceptionFormatter.FormatException(ex, ServiceProvider.Resolve<StackTraceCleaner>()));
                UnloadModule();
                Provider.shutdown();
            }
        });
    }

    private static void LogMemoryUsage(List<string> obj)
    {
        long mem = GC.GetTotalMemory(true);

        obj.Add($"Heap memory size: {mem / 1_073_741_824m:F4} GiB");
    }

    private void HandleSystemConfigUpdated()
    {
        ServiceProvider.Resolve<ILoggerFactory>();
    }

    internal void Shutdown()
    {
        if (Singleton == this)
            Singleton = null;

        _dispatcher = null;

        _systemConfigChangeToken?.Dispose();
        _systemConfigChangeToken = null;

        CommandLogMemoryUsage.OnExecuted -= LogMemoryUsage;

        if (Configuration is IDisposable disposableConfig)
            disposableConfig.Dispose();

        if (_gameObjectHost != null)
        {
            Object.Destroy(_gameObjectHost);
            _gameObjectHost = null!;
        }

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (AggregateException ex)
        {
            _logger.LogError(ex, "Error(s) while canceling module cancellation token source.");
        }

        _cancellationTokenSource.Dispose();

        if (FileProvider is IDisposable disp)
            disp.Dispose();

        if (!_unloadedHostedServices)
        {
            UnhostServices();
        }

        try
        {
            ServiceProvider.ResolveOptional<HarmonyPatchService>()?.RemoveAllPatches();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing patches.");
        }

#if TELEMETRY
        try
        {
            _logger.LogTrace("Flushing telemetry...");
            ServiceProvider.Resolve<TracerProvider>().ForceFlush(2000);
            ServiceProvider.Resolve<TracerProvider>().Shutdown(2000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down tracer provider.");
        }
#endif

        _logger.LogInformation("Cleaning up container...");
        ServiceProvider.Dispose();
        CommandWindow.Log("Done - Shutting down");
    }

    private void ConfigureServices(ContainerBuilder bldr)
    {
        Assembly thisAsm = Assembly.GetExecutingAssembly();
        Module thisModule = ModuleHook.modules.First(x => x.config.Name.Equals("Uncreated.Warfare", StringComparison.Ordinal) && x.assemblies.Contains(thisAsm));

        // all module assemblies and plugins
        Assembly[] relevantAssemblies = [ thisAsm ];

        bldr.RegisterType<MapScheduler>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<ServerHeartbeatTimer>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<ActionLoggerService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterRpcType<ReportService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterRpcType<WarfareGameStateService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterRpcType<RemotePlayerListService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // global zones (not used for layouts)
        bldr.RegisterType<MapZoneProvider>()
            .As<IZoneProvider>();

        bldr.RegisterType<ZoneStore>()
            .WithParameter("isGlobal", true)
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<ElectricalGridService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // external tools
        bldr.RegisterFromCollection(collection =>
        {
            collection.AddReflectionTools();
            collection.AddModularRpcs(
                isServer: false,
                (_, configuration, parsers, _) =>
                {
                    parsers.RegisterParserAttributes(configuration, relevantAssemblies);
                }
            );
        });

        bldr.RegisterType<NullRpcConnectionService>()
            .As<IRpcConnectionService>();

        bldr.RegisterInstance(this)
            .As<WarfareModule>()
            .ExternallyOwned();

        bldr.RegisterInstance(thisModule)
            .As<Module>()
            .ExternallyOwned();

        bldr.RegisterType<HarmonyPatchService>()
            .SingleInstance();

        bldr.Register<HarmonyPatchService, Harmony>((_, p) => p.Patcher)
            .SingleInstance();

        bldr.Register(sp => (WarfareLifetimeComponent)ProxyGenerator.Instance.CreateProxyComponent(_gameObjectHost, typeof(WarfareLifetimeComponent), sp.Resolve<IRpcRouter>()))
            .SingleInstance();

        bldr.RegisterInstance(FileProvider)
            .As<IFileProvider>().As<PhysicalFileProvider>()
            .SingleInstance()
            .ExternallyOwned();

        bldr.RegisterType<AssetConfiguration>().SingleInstance();
        bldr.RegisterInstance(Configuration).ExternallyOwned();
        
        bldr.RegisterType<UnityLoopTickerFactory>() 
            .As<ILoopTickerFactory>();

        // homebase
        bldr.RegisterRpcType<HomebaseConnector>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // UI
        bldr.RegisterType<HudManager>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<ModerationUI>().SingleInstance();
        bldr.RegisterType<DutyUI>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<SquadMenuUI>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<PlayerSquadHUD>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        
        bldr.RegisterType<FobHUD>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        
        bldr.RegisterType<KitSelectionUI>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<PopupUI>().SingleInstance();
        bldr.RegisterType<StagingUI>().SingleInstance();
        bldr.RegisterType<WinToastUI>().SingleInstance();

        bldr.RegisterType<PointsUI>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<VehicleHUD>().SingleInstance();
        bldr.RegisterType<FlagListUI>().SingleInstance();
        bldr.RegisterType<CaptureUI>().AsSelf().AsImplementedInterfaces().SingleInstance();
        bldr.RegisterType<OptionsUI>().SingleInstance();
        bldr.RegisterType<SeedingPlayHud>().SingleInstance();

        bldr.RegisterType<TipService>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        
        bldr.RegisterType<SendChatMutedEventHandler>()
            .AsImplementedInterfaces();

        // for DebugEventTestCommand
        // bldr.RegisterType<TestEventService1>().AsSelf().AsImplementedInterfaces().InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);
        // bldr.RegisterType<TestEventService2>().AsSelf().AsImplementedInterfaces().SingleInstance();
        // bldr.RegisterType<TestEventService3>().AsSelf().AsImplementedInterfaces().SingleInstance();
        // bldr.RegisterType<TestEventService4>().AsSelf().AsImplementedInterfaces().SingleInstance();
        // bldr.RegisterType<TestEventService5>().AsSelf().AsImplementedInterfaces().SingleInstance();
        // bldr.RegisterType<TestEventService6>().AsSelf().AsImplementedInterfaces().SingleInstance();
        // bldr.RegisterType<TestEventService7>().AsSelf().AsImplementedInterfaces().SingleInstance();


        bldr.RegisterType<WarfareSteamApiService>()
            .As<ISteamApiService>()
            .SingleInstance();

        bldr.RegisterType<AudioRecordManager>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

#if REMOTE_WORKSHOP_UPLOAD
        bldr.RegisterRpcType<RemoteWorkshopUploader>()
            .AsImplementedInterfaces()
            .SingleInstance();
#else
        bldr.RegisterRpcType<LocalWorkshopUploader>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();
#endif

        bldr.RegisterType<TimeZoneRegionalDatabase>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<QuestService>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<DailyQuestService>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<LayoutFactory>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<SeedingPlayerCountMonitor>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<EventSynchronizer>();
        bldr.RegisterType<EventDispatcher>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<CooldownManager>()
            .AsSelf().As<ILayoutHostedService>()
            .SingleInstance();

        bldr.RegisterType<CommandDispatcher>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterRpcType<UserPermissionStore>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();
        bldr.RegisterRpcType<DutyService>()
            .AsImplementedInterfaces().AsSelf();
        
        bldr.RegisterInstance(_gameObjectHost).ExternallyOwned();

        bldr.RegisterType<BuildableAttributesDataStore>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<MainBaseBuildables>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<VehicleInfoStore>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<AbandonService>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<VehicleService>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<MapVehicleSpawnerDataStore>()
            .As<IVehicleSpawnerDataStore>()
            .ExternallyOwned();

        bldr.RegisterType<VehicleRequestService>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<VehicleSpawnerService>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<VehicleSeatRestrictionService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<VehicleDamageTrackerItemTweaks>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();
        bldr.RegisterType<AdvancedVehicleDamageTweaks>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();
        
        bldr.RegisterType<AutoResupplyLoop>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        // Players
        bldr.RegisterType<PlayerService>()
            .As<IPlayerService>()
            .SingleInstance();

        bldr.RegisterType<PlayerComponentListenerProvider>()
            .As<IEventListenerProvider>()
            .SingleInstance();

        bldr.RegisterType<DeathTracker>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<DeathMessageResolver>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<FactionDataStore>()
            .AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<DefaultTeamSelectorBehavior>()
            .As<ITeamSelectorBehavior>();

        bldr.RegisterType<LobbyZoneManager>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<LobbyConfiguration>()
            .SingleInstance();

        bldr.RegisterType<LobbyHudUI>()
            .SingleInstance();

        // Stats
        bldr.RegisterType<PointsRewardsEvents>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<DatabaseStatsBuffer>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<PlayerDatabaseStatsEventHandlers>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<PlayerGameStatsEventHandlers>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<MySqlPointsStore>()
            .As<IPointsStore>()
            .SingleInstance();

        bldr.RegisterType<PointsConfiguration>()
            .SingleInstance();

        bldr.RegisterType<PointsService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<SessionManager>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        // Kits
        bldr.RegisterType<DefaultLoadoutItemsConfiguration>()
            .SingleInstance();

        bldr.RegisterType<KitCreateMissingDefaultKitsTweak>().As<ILayoutHostedService>();
        bldr.RegisterType<KitNoSwapStorageClothingTweak>().AsSelf().AsImplementedInterfaces();
        bldr.RegisterType<KitGiveDefaultOnLeaveSquadKit>().AsSelf().AsImplementedInterfaces();

        bldr.RegisterType<MySqlKitsDataStore>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<KitSettableRegistration>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterRpcType<MySqlKitFavoriteService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterRpcType<MySqlKitAccessService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<KitStatisticService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<KitLayoutService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<KitCommandLookResolver>()
            .SingleInstance();

        bldr.RegisterRpcType<PlayerNitroBoostService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterRpcType<LoadoutService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<KitSignService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<KitBestowService>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<KitWeaponTextService>()
            .AsSelf().SingleInstance();

        bldr.RegisterType<KitRequestService>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<KitRequirementManager>()
            .AsSelf().InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        
        bldr.RegisterType<PunchToRequestTweaks>()
            .AsSelf().AsImplementedInterfaces();

        if (false && ItemUtility.SupportsFastKits)
        {
            // todo: this needs fixed
            bldr.RegisterType<FastItemDistributionService>()
                .As<IItemDistributionService>();
        }
        else
        {
            bldr.RegisterType<FallbackItemDistributionService>()
                .As<IItemDistributionService>();
        }

        bldr.Register<IServiceProvider, IKitItemResolver>((_, serviceProvider) =>
            HolidayUtil.isHolidayActive(ENPCHoliday.APRIL_FOOLS)
                ? ActivatorUtilities.CreateInstance<DootpressorKitItemResolver>(serviceProvider)
                : ActivatorUtilities.CreateInstance<BaseKitItemResolver>(serviceProvider)
        ).InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<AssetRedirectService>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<DroppedItemTracker>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<WhitelistService>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<SignInstancer>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterInstance(new TextMeasurementService())
            .OwnedByLifetimeScope()
            .SingleInstance();

        // Stripe
        bldr.RegisterType<UnityWebRequestsHttpClient>()
            .As<IHttpClient>();

        bldr.Register(serviceProvider =>
        {
            IConfiguration systemConfig = serviceProvider.Resolve<IConfiguration>();
            string? apiKey = systemConfig["stripe:api_key"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("Stripe API key missing at stripe:api_key.");

            string clientId = $"Uncreated Warfare/{Assembly.GetExecutingAssembly().GetName().Version}";
            return new StripeClient(apiKey, clientId, httpClient: serviceProvider.Resolve<IHttpClient>());
        }).As<IStripeClient>();

        bldr.Register(serviceProvider => new ProductService(serviceProvider.Resolve<IStripeClient>()))
            .ExternallyOwned();

        bldr.RegisterType<StripeService>()
            .As<IStripeService>();

        bldr.RegisterRpcType<DiscordUserService>()
            .SingleInstance();

        bldr.RegisterType<LayoutPhaseEventListenerProvider>()
            .As<IEventListenerProvider>()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        // FOBs
        bldr.RegisterType<DeploymentService>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<FobManager>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        
        bldr.RegisterType<FobStrategyMapHandler>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        
        bldr.RegisterType<ThrowableSupplyCrateTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<WorldIconManager>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<FobConfiguration>()
            .SingleInstance();

        bldr.RegisterType<BarricadeStateStore>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<BarricadeApplySavedStateTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // Strategy Tables
        bldr.RegisterType<StrategyMapManager>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<StrategyMapsConfiguration>()
            .SingleInstance();

        // Squads
        bldr.RegisterType<SquadManager>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<SquadConfiguration>()
            .SingleInstance();
        
        bldr.RegisterType<SquadSignEvents>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        // Spotting
        bldr.RegisterType<SpottedService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // Projectiles
        bldr.RegisterType<ProjectileSolver>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // Tweaks
        bldr.RegisterType<FobPlacementTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<ShovelableTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<VehicleLockRequestedHandler>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<SendChatFilterEventHandler>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<ClaimToRearmTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<GuidedMissileLaunchTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<FlareTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<QueueShutdownOnUnturnedUpdate>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<VoiceChatRestrictionsTweak>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<RemoveKitOnGameEnd>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<VehicleRestrictions>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<BattlEyeBanEventHandler>()
            .AsSelf().AsImplementedInterfaces();
        bldr.RegisterType<VehicleTrunkTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<PlayerChooseSpawnPointTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<KeepPlayerStateOnRejoinTweak>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<KeepItemsAndStatsOnDeathTweak>()
            .AsImplementedInterfaces();
        bldr.RegisterType<DisallowGroupsTweak>()
            .AsImplementedInterfaces();
        bldr.RegisterType<ShovelableWarningTweak>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<DisallowPickUpSupplyCrate>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<WarTableDoorTweak>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);

        bldr.RegisterType<NoCraftingTweak>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<InvinciblePassengersTweak>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<CombatCooldownTweak>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();
        bldr.RegisterType<LandmineExplosionRestrictions>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<PreventLeaveGroupTweak>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
        bldr.RegisterType<SafezoneTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);        
        bldr.RegisterType<MapMarkerTweaks>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // Localization
        bldr.RegisterType<LanguageService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<MySqlLanguageDataStore>()
            .As<ICachableLanguageDataStore>()
            .As<ILanguageDataStore>()
            .SingleInstance();

        bldr.RegisterType<ChatService>()
            .AsSelf()
            .SingleInstance();

        bldr.RegisterType<NerdService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // Translations
        bldr.RegisterType<PropertiesTranslationStorageFactory>()
            .As<ITranslationStorageFactory>()
            .SingleInstance();

        bldr.RegisterType<TranslationValueFormatter>()
            .As<ITranslationValueFormatter>()
            .SingleInstance();

        bldr.RegisterGeneric(typeof(TranslationInjection<>))
            .SingleInstance();

        bldr.RegisterType<TranslationService>()
            .AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<ItemIconProvider>()
            .SingleInstance();

        bldr.RegisterType<AnnouncementService>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        // Database

        SshTunnelHelper.AddSshTunnelService(bldr);

        bldr.RegisterRpcType<DatabaseInterface>()
            .AsSelf()
            .SingleInstance();

        bldr.RegisterType<ModerationEventHandlers>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<GlobalBanWhitelistService>()
            .As<IGlobalBanWhitelistService>();

        bldr.RegisterType<UserDataService>()
            .As<IUserDataService>()
            .SingleInstance();

        bldr.RegisterRpcType<AccountLinkingService>()
            .SingleInstance();

        bldr.Register(sp => WarfareDbContext.GetOptions(sp.Resolve<IServiceProvider>()))
            .SingleInstance();

        bldr.RegisterType<WarfareDbContext>()
            .AsSelf().As(typeof(WarfareDbContext).GetInterfaces().Where(x => typeof(IDbContext).IsAssignableFrom(x)).ToArray())
            .InstancePerDependency();

        bldr.Register<IManualMySqlProvider>(serviceProvider =>
        {
            IConfiguration sysConfig = serviceProvider.Resolve<IConfiguration>();
            IConfiguration databaseSection = sysConfig.GetSection("database");

            string? connectionStringType = databaseSection["connection_string_name"];

            if (string.IsNullOrWhiteSpace(connectionStringType))
                connectionStringType = "warfare-db";

            string? connectionString = sysConfig.GetConnectionString(connectionStringType);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Missing connection string: \"{connectionStringType}\".");

            return new ManualMySqlProvider(connectionString, serviceProvider.Resolve<ILogger<ManualMySqlProvider>>());
        });

#if TELEMETRY
        // Telemetry

        TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(
                "Uncreated.Warfare.Events.EventDispatcher",
                "Uncreated.Warfare.Interaction.Commands.CommandDispatcher",
                "Uncreated.Warfare.WarfareModule",
                "Uncreated.Warfare.Layouts.LayoutFactory"
            )
            .ConfigureResource(resource =>
                resource.AddService(
                    serviceName: "Uncreated.Warfare",
                    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version.ToString()
                )
            )
            // run Zipkin with:
            // docker run -p 9411:9411 --name zipkin-uncreated ghcr.io/openzipkin-contrib/zipkin-otel
            .AddOtlpExporter(zipkin =>
            {
                zipkin.Endpoint = new Uri(
                    new Uri(Configuration.GetValue("zipkin_uri", "http://localhost:9411/")),
                    new Uri("v1/traces", UriKind.Relative)
                );
                zipkin.Protocol = OtlpExportProtocol.HttpProtobuf;
                zipkin.ExportProcessorType = ExportProcessorType.Batch;
                zipkin.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                {
                    ExporterTimeoutMilliseconds = 10000,
                    ScheduledDelayMilliseconds = 1000
                };
                zipkin.UserAgentProductIdentifier = "Uncreated.Warfare";
            })
            .Build();

        bldr.RegisterInstance(tracerProvider)
            .OwnedByLifetimeScope();

        //MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
        //    .AddMeter(serviceName)
        //    .Build();

        //bldr.RegisterInstance(meterProvider)
        //    .OwnedByLifetimeScope();
#endif
    }

    public async UniTask ShutdownAsync(string reason, CancellationToken token = default)
    {
#if TELEMETRY
        Activity.Current = null;
        using (Activity? activity = _activitySource.StartActivity("Shutdown"))
#endif
        {
            await ServiceProvider.Resolve<WarfareLifetimeComponent>().NotifyShutdownNow(reason);

            await UniTask.SwitchToMainThread(token);

            // prevent players from joining after shutdown start
            IPlayerService? playerService = ServiceProvider.ResolveOptional<IPlayerService>();
            playerService?.TakePlayerConnectionLock(token);

            RemotePlayerListService? remoteStateManager = ServiceProvider.ResolveOptional<RemotePlayerListService>();
            if (remoteStateManager != null)
            {
                await remoteStateManager.UpdateReplicatedServerState(ServerStateType.Shutdown, reason);
            }

            // kick all players
            await UniTask.SwitchToMainThread(token);
            for (int i = Provider.clients.Count - 1; i >= 0; --i)
            {
                Provider.kick(Provider.clients[i].playerID.steamID, !string.IsNullOrWhiteSpace(reason) ? "Shutting down: \"" + reason + "\"." : "Shutting down.");
            }

            await EventDispatcher.WaitForEvents();

            Object.Destroy(_gameObjectHost);
            _gameObjectHost = null!;

            if (!_unloadedHostedServices)
            {
                await UnhostAsync(token
#if TELEMETRY
                    , activity
#endif
                );
            }
        }


        await UniTask.SwitchToMainThread(CancellationToken.None);
        UnloadModule();
        Provider.shutdown();

        while (true)
        {
            // wait for shutdown
            await UniTask.NextFrame();
        }

        // ReSharper disable once FunctionNeverReturns
    }

    /// <summary>
    /// Start all hosted services.
    /// </summary>
    private async UniTask HostAsync()
    {
        GameThread.AssertCurrent();

#if TELEMETRY
        Activity.Current = null;
        using Activity? activity = _activitySource.StartActivity("Host Module");
#endif

        // set up level measurements as early as possible
        Level.onPrePreLevelLoaded += CartographyUtility.Init;

        CancellationToken token = UnloadToken;

        // this needs to happen almost instantly, can't wait for migration
        ServiceProvider.Resolve<MapScheduler>().ApplyMapSetting();

        // this too
        HarmonyPatchService patchService = ServiceProvider.Resolve<HarmonyPatchService>();

        {
#if TELEMETRY
            using Activity? childActivity = _activitySource.StartActivity(
                "Harmony patches",
                ActivityKind.Internal,
                parentContext: activity?.Context ?? default
            );
#endif
            patchService.ApplyAllPatches();
        }

        bool connected = false;

        try
        {
#if TELEMETRY
            using Activity? sshActivity = _activitySource.StartActivity(
                "SSH Authentication",
                ActivityKind.Internal,
                parentContext: activity?.Context ?? default
            );
#endif
            await SshTunnelHelper.OpenIfAvailableAsync(ServiceProvider, token);
        }
        catch (Exception ex)
        {
            await UniTask.SwitchToMainThread(token);
            _logger.LogError(ex, "Unable to connect to SSH tunnel for MySQL database. Please reconfigure and restart.");
            UnloadModule();
            Provider.shutdown();
#if TELEMETRY
            activity?.SetStatus(ActivityStatusCode.Error, "SSH connection failed.");
#endif
            return;
        }

#if TELEMETRY
        Activity.Current = null;
#endif

        // migrate database before loading services
        _logger.LogDebug("Migrating database...");
        try
        {
#if TELEMETRY
            using Activity? dbActivity = _activitySource.StartActivity(
                "Connect/Migrate database",
                ActivityKind.Internal,
                parentContext: activity?.Context ?? default
            );
#endif
            await using ILifetimeScope scope = ServiceProvider.BeginLifetimeScope();
            await using IDbContext dbContext = scope.Resolve<IDbContext>();
            const double timeoutSec = 10;

            // check connection before migrating with a 10 second timeout
            await Task.WhenAny(Task.Run(async () =>
            {
                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    connected = await ((IRelationalDatabaseCreator)((IDatabaseFacadeDependenciesAccessor)dbContext.Database).Dependencies.DatabaseCreator).ExistsAsync(token);
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                        Console.WriteLine(ex);
                    else
                        _logger.LogError(ex, "Error connecting to MySQL.");
                }

            }, token), Task.Delay(TimeSpan.FromSeconds(timeoutSec), token));

            if (!connected)
            {
                _logger.LogWarning($"Connection for migration timed out after {timeoutSec} second(s).");
            }
            else
            {
                _logger.LogDebug($"Migrating database process: {dbContext.GetType()}");
                await dbContext.Database.MigrateAsync(token).ConfigureAwait(false);
                _logger.LogInformation("Migration completed.");
            }
        }
        catch (Exception ex) when (ex.GetBaseException() is MySqlException mySqlException && ex.Message.Contains("timeout", StringComparison.InvariantCultureIgnoreCase))
        {
            // todo: use error code instead of Contains as soon as i figure out what it is
            await UniTask.SwitchToMainThread();
            _logger.LogError("TODO: Error code: " + mySqlException.ErrorCode);
            _logger.LogError("Timed out connecting to SQL database.");
            UnloadModule();
            Provider.shutdown();
#if TELEMETRY
            activity?.SetStatus(ActivityStatusCode.Error, "Database connection timed out.");
#endif
            return;
        }

        if (!connected)
        {
            await UniTask.SwitchToMainThread(token);
            _logger.LogError("Unable to connect to MySQL database. Please reconfigure and restart.");
            UnloadModule();
            Provider.shutdown();
#if TELEMETRY
            activity?.SetStatus(ActivityStatusCode.Error, "Database failed to connect.");
#endif
            return;
        }

        // this needs to run before hosted services start requesting translations
        {
#if TELEMETRY
            using Activity? langActivity = _activitySource.StartActivity(
                "Cache languages",
                ActivityKind.Internal,
                parentContext: activity?.Context ?? default
            );
#endif
            ICachableLanguageDataStore? dataStore = ServiceProvider.ResolveOptional<ICachableLanguageDataStore>();
            if (dataStore != null)
                await dataStore.ReloadCache(token);

            await UniTask.SwitchToMainThread(token);
        }

        List<IHostedService> hostedServices = ServiceProvider
            .Resolve<IEnumerable<IHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        _logger.LogDebug("Hosting {0} services.", hostedServices.Count);
        _unloadedHostedServices = false;
        int errIndex = -1;
        for (int i = 0; i < hostedServices.Count; i++)
        {
            IHostedService hostedService = hostedServices[i];
#if TELEMETRY
            using Activity? hostActivity = _activitySource.CreateActivity(
                $"Host {Accessor.ExceptionFormatter.Format(hostedService.GetType())}",
                ActivityKind.Internal,
                parentContext: activity?.Context ?? default
            );
#endif
            try
            {
                if (!GameThread.IsCurrent)
                {
#if TELEMETRY
                    hostActivity?.AddEvent(new ActivityEvent("Switch to main thread."));
#endif
                    await UniTask.SwitchToMainThread(token);
                }

#if TELEMETRY
                hostActivity?.Start();
                Activity.Current = hostActivity;
#endif
                _logger.LogDebug("Hosting {0}.", hostedService.GetType());
                await hostedService.StartAsync(token);
#if TELEMETRY
                hostActivity?.Stop();
                hostActivity?.SetStatus(ActivityStatusCode.Ok);
#endif
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
#if TELEMETRY
                RecordActivityCancelled(hostActivity);
#endif
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hosting service {hostedService.GetType()}.");
                errIndex = i;
#if TELEMETRY
                RecordActivityException(hostActivity, ex);
#endif
                break;
            }
        }

#if TELEMETRY
        activity?.Stop();
#endif

        // one of the hosted services errored, unhost all that were hosted and shut down.
        if (errIndex == -1)
        {
#if TELEMETRY
            activity?.SetStatus(ActivityStatusCode.Ok);
#endif
            return;
        }

#if TELEMETRY
        activity?.SetStatus(ActivityStatusCode.Error, "A service failed to host.");
#endif
        await UniTask.SwitchToMainThread(token);

        if (_unloadedHostedServices)
            return;

        await UnhostServices(hostedServices, errIndex - 1, token
#if TELEMETRY
            , activity
#endif
        );

        await UniTask.SwitchToMainThread(CancellationToken.None);
        UnloadModule();
        Provider.shutdown();
    }

    internal async UniTask InvokeLevelLoaded(CancellationToken token)
    {
#if TELEMETRY
        using Activity? activity = _activitySource.StartActivity("InvokeLevelLoaded");
#endif
        List<ILevelHostedService> hostedServices = GetActiveLayout().ServiceProvider
            .Resolve<IEnumerable<ILevelHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        await UniTask.SwitchToMainThread(token);

        _logger.LogDebug("Hosting {0} services on level load.", hostedServices.Count);
        for (int i = 0; i < hostedServices.Count; i++)
        {
            ILevelHostedService hostedService = hostedServices[i];
#if TELEMETRY
            using Activity? hostActivity = _activitySource.CreateActivity(
                $"Host {Accessor.ExceptionFormatter.Format(hostedService.GetType())}",
                ActivityKind.Internal,
                parentContext: activity?.Context ?? default
            );
#endif
            try
            {
                if (!GameThread.IsCurrent)
                {
#if TELEMETRY
                    hostActivity?.AddEvent(new ActivityEvent("Switch to main thread."));
#endif
                    await UniTask.SwitchToMainThread(token);
                }

#if TELEMETRY
                hostActivity?.Start();
                Activity.Current = hostActivity;
#endif
                _logger.LogDebug("Hosting {0} on level load.", hostedService.GetType());
                await hostedService.LoadLevelAsync(token);
#if TELEMETRY
                hostActivity?.Stop();
                hostActivity?.SetStatus(ActivityStatusCode.Ok);
#endif
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
#if TELEMETRY
                RecordActivityCancelled(hostActivity);
#endif
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hosting service {hostedService.GetType()} on level load.");
#if TELEMETRY
                RecordActivityException(hostActivity, ex);
#endif
                break;
            }
        }
    }

    internal async UniTask InvokeEarlyLevelLoaded(CancellationToken token)
    {
#if TELEMETRY
        using Activity? activity = _activitySource.StartActivity("InvokeEarlyLevelLoaded");
#endif
        List<IEarlyLevelHostedService> hostedServices = ServiceProvider
            .Resolve<IEnumerable<IEarlyLevelHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        await UniTask.SwitchToMainThread(token);

        _logger.LogDebug("Hosting {0} services on early level load.", hostedServices.Count);
        for (int i = 0; i < hostedServices.Count; i++)
        {
            IEarlyLevelHostedService hostedService = hostedServices[i];
#if TELEMETRY
            using Activity? hostActivity = _activitySource.CreateActivity(
                $"Host {Accessor.ExceptionFormatter.Format(hostedService.GetType())}",
                ActivityKind.Internal,
                parentContext: activity?.Context ?? default
            );
#endif
            try
            {
                if (!GameThread.IsCurrent)
                {
#if TELEMETRY
                    hostActivity?.AddEvent(new ActivityEvent("Switch to main thread."));
#endif
                    await UniTask.SwitchToMainThread(token);
                }

#if TELEMETRY
                hostActivity?.Start();
                Activity.Current = hostActivity;
#endif
                _logger.LogDebug("Hosting {0} on early level load.", hostedService.GetType());
                await hostedService.EarlyLoadLevelAsync(token);
#if TELEMETRY
                hostActivity?.Stop();
                hostActivity?.SetStatus(ActivityStatusCode.Ok);
#endif
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
#if TELEMETRY
                RecordActivityCancelled(hostActivity);
#endif
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hosting service {hostedService.GetType()} on early level load.");
#if TELEMETRY
                RecordActivityException(hostActivity, ex);
#endif
                break;
            }
        }
    }

    private async UniTask UnhostAsync(CancellationToken token
#if TELEMETRY
        , Activity? parentActivity
#endif
    )
    {
        await UniTask.SwitchToMainThread(token);

        if (_unloadedHostedServices)
            return;
#if TELEMETRY
        using Activity? activity = _activitySource.StartActivity(
            "Unhost",
            ActivityKind.Internal,
            parentActivity?.Context ?? default
        );
#endif

        List<IHostedService> hostedServices = ServiceProvider
            .Resolve<IEnumerable<IHostedService>>()
            .OrderBy(x => x.GetType().GetPriority())
            .ToList();

        _logger.LogDebug("Unhosting {0} services.", hostedServices.Count);

        await UnhostServices(hostedServices, hostedServices.Count - 1, token
#if TELEMETRY
            , activity
#endif
        );
    }

    private UnhostingTask[] StartUnhostingServices(List<IHostedService> hostedServices, int startIndex, CancellationToken token
#if TELEMETRY
        , Activity? activity = null
#endif
    )
    {
        _unloadedHostedServices = true;

        UnhostingTask[] tasks = new UnhostingTask[startIndex + 1];
        for (int i = startIndex; i >= 0; --i)
        {
            IHostedService hostedService = hostedServices[i];
#if TELEMETRY
            Activity? tempActivity = _activitySource.StartActivity(
                $"Unhost {Accessor.ExceptionFormatter.Format(hostedService.GetType())}",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            try
            {
                _logger.LogTrace($"Unhosting {hostedService.GetType()}.");
                tasks[i] = hostedService.StopAsync(CancellationToken.None).Preserve()
#if TELEMETRY
                        .ContinueWith(() =>
                        {
                            tempActivity?.Stop();
                        })
#endif
                    ;
            }
            catch (Exception ex)
            {
                tasks[i] = UniTask.FromException(ex);
            }
#if TELEMETRY
            tasks[i].Activity = tempActivity;
#endif
        }

        return tasks;
    }

    private async UniTask UnhostServices(List<IHostedService> hostedServices, int startIndex, CancellationToken token
#if TELEMETRY
        , Activity? activity
#endif
    )
    {
        UnhostingTask[] tasks = StartUnhostingServices(hostedServices, startIndex, token
#if TELEMETRY
            , activity
#endif
        );
        try
        {
            await UniTask.WhenAll(tasks.Select(x => x.Task));
        }
        catch
        {
            _logger.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
        }
        finally
        {
            for (int i = 0; i < tasks.Length; ++i)
            {
                tasks[i].Dispose();
            }
        }
    }

    /// <summary>
    /// Synchronously unhost so <see cref="IModuleNexus.shutdown"/> can wait on unhost.
    /// </summary>
    private void UnhostServices()
    {
        _unloadedHostedServices = true;
        using CancellationTokenSource timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3d));

        List<IHostedService> hostedServices = ServiceProvider
            .Resolve<IEnumerable<IHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        _logger.LogDebug("Unhosting {0} services synchronously.", hostedServices.Count);
        UnhostingTask[] tasks = StartUnhostingServices(hostedServices, hostedServices.Count - 1, CancellationToken.None);

        bool canceled = false;
        try
        {
            UniTask.WhenAll(tasks.Select(x => x.Task)).AsTask().Wait(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            canceled = true;
        }
        catch
        {
            _logger.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
            Thread.Sleep(500);
            return;
        }

        if (!canceled)
            return;

        _logger.LogError("Unloading timed out:");
        for (int i = 0; i < tasks.Length; ++i)
        {
            if (tasks[i].Task.Status is not UniTaskStatus.Canceled and not UniTaskStatus.Pending)
                continue;

            _logger.LogError(hostedServices[i].GetType() + $" - {tasks[i].Task.Status}.");
        }
        Thread.Sleep(500);
    }

    /// <summary>
    /// Start a new scope, used for each game.
    /// </summary>
    /// <returns>The newly created scope.</returns>
    internal async UniTask<ILifetimeScope> CreateScopeAsync(Action<ContainerBuilder>? builder, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _activeLayout = null;

        ILifetimeScope newScope = builder == null
            ? ServiceProvider.BeginLifetimeScope(LifetimeScopeTags.Layout)
            : ServiceProvider.BeginLifetimeScope(LifetimeScopeTags.Layout, builder);


        ILifetimeScope? oldScope = Interlocked.Exchange(ref _activeScope, newScope);
        _logger.LogInformation("Created new layout scope.");

        if (oldScope != null)
        {
            await oldScope.DisposeAsync().ConfigureAwait(false);
            await UniTask.SwitchToMainThread();
        }

        return newScope;
    }

    /// <summary>
    /// Set the new game after calling <see cref="CreateScopeAsync"/>.
    /// </summary>
    internal void SetActiveLayout(Layout? layout)
    {
        Layout? oldLayout = Interlocked.Exchange(ref _activeLayout, layout);
        try
        {
            LayoutStarted?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LayoutStarted listener threw an exception in SetActiveLayout.");
        }

        if (oldLayout == null)
            return;

        if (layout != null)
            _logger.LogError("A layout was started while one was already active.");

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
        // calls IModuleNexus.shutdown()
        ServiceProvider.Resolve<Module>().isEnabled = false;

#if TELEMETRY
        Interlocked.Exchange(ref _activitySource, null)?.Dispose();
#endif
    }

    private void InitializeUniTask()
    {
        if (PlayerLoopHelper.IsInjectedUniTaskPlayerLoop())
        {
            CommandWindow.Log("UniTask already initialized.");
            return;
        }

        MethodInfo? initMethod = typeof(PlayerLoopHelper).GetMethod("Init",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, Type.EmptyTypes, null
        );

        if (initMethod != null)
        {
            initMethod.Invoke(null, Array.Empty<object>());
            CommandWindow.Log("Initialized UniTask.");
        }
        else
            CommandWindow.LogError("Failed to initialize UniTask.");

        UniTaskScheduler.UnobservedTaskException += UnitaskExceptionUnobserved;
        UniTaskScheduler.DispatchUnityMainThread = false;
        UniTaskScheduler.PropagateOperationCanceledException = true;
    }

    private void UnitaskExceptionUnobserved(Exception ex)
    {
        if (ServiceProvider == null)
        {
            if (GameThread.IsCurrent)
            {
                CommandWindow.LogError("UniTask caught an unobserved exception.");
                CommandWindow.LogError(ex);
            }
            else
            {
                Exception ex2 = ex;
                UniTask.Create(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    CommandWindow.LogError("UniTask caught an unobserved exception.");
                    CommandWindow.LogError(ex2);
                });
            }
        }
        else
        {
            ServiceProvider.Resolve<ILogger<UniTask>>().LogError(ex, "UniTask caught an unobserved exception.");
        }
    }
}

public class WarfareModuleNexus : IModuleNexus
{
    private object? _module;
    void IModuleNexus.initialize()
    {
        try
        {
            Setup();
        }
        catch (Exception ex)
        {
            CommandWindow.LogError(ex);
        }
    }

    void IModuleNexus.shutdown()
    {
        ((WarfareModule?)_module)?.Shutdown();
    }

    private void Setup()
    {
        WarfareModule mod = new WarfareModule();
        _module = mod;
        mod.Initialize();
    }
}

internal struct UnhostingTask : IDisposable
{
    public UniTask Task;
#if TELEMETRY
    public Activity? Activity;
#endif

    public void Dispose()
    {
#if TELEMETRY
        Activity?.Dispose();
#endif
    }

    public static implicit operator UnhostingTask(UniTask task)
    {
        return new UnhostingTask { Task = task };
    }
}