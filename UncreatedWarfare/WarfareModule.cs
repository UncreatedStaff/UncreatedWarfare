﻿using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using SDG.Framework.Modules;
using StackCleaner;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.ListenerProviders;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.UI;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Networking.Purchasing;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Squads.UI;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Teams;
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
    /// <summary>
    /// The current season.
    /// </summary>
    public static readonly int Season = typeof(WarfareModule).Assembly.GetName().Version.Major;

    private static EventDispatcher2? _dispatcher;

#nullable disable

    /// <summary>
    /// Static instance of the event dispatcher singleton for harmony patches to access it.
    /// </summary>
    /// <remarks>Do not use unless in a patch.</remarks>
    public static EventDispatcher2 EventDispatcher => _dispatcher ??= Singleton?.ServiceProvider.Resolve<EventDispatcher2>();

    /// <summary>
    /// Static instance of this module singleton for harmony patches to access it.
    /// </summary>
    /// <remarks>Do not use unless in a patch.</remarks>
    public static WarfareModule Singleton { get; private set; }

#nullable restore

    private bool _unloadedHostedServices = true;
    private ILifetimeScope? _activeScope;
    private CancellationTokenSource _cancellationTokenSource;
    private Layout? _activeLayout;
    private GameObject _gameObjectHost;
    private ILogger<WarfareModule> _logger;
    private WarfarePluginLoader _pluginLoader;

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

    void IModuleNexus.initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        
        // will setup the main thread in GameThread before asserting
        GameThread.Setup();
        GameThread.AssertCurrent();

        // setup UniTask
        if (!PlayerLoopHelper.HasBeenInitialized)
            PlayerLoopHelper.Init();

        // this needs to be separated to give the above events time to be subscribed before loading types
        Init();
    }

    private void Init()
    {
        Singleton = this;

        _gameObjectHost = new GameObject("Uncreated.Warfare");
        Object.DontDestroyOnLoad(_gameObjectHost);

        _cancellationTokenSource = new CancellationTokenSource();

        ConfigurationSettings.SetupTypeConverters();

        // todo rewrite action log
        _gameObjectHost.AddComponent<ActionLog>();

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

        Provider.modeConfigData.Players.Lose_Items_PvP = 0;
        Provider.modeConfigData.Players.Lose_Items_PvE = 0;
        Provider.modeConfigData.Players.Lose_Clothes_PvP = false;
        Provider.modeConfigData.Players.Lose_Clothes_PvE = false;
        Provider.modeConfigData.Barricades.Decay_Time = 0;
        Provider.modeConfigData.Structures.Decay_Time = 0;

        HomeDirectory = Path.Combine(UnturnedPaths.RootDirectory.FullName, "Servers", Provider.serverID, "Warfare");
        Directory.CreateDirectory(HomeDirectory);

        // Add system configuration provider.
        IConfigurationBuilder configBuilder = new ConfigurationBuilder();

        string systemConfigLocation = Path.Join(HomeDirectory, "System Config.yml");

        FileProvider = new PhysicalFileProvider(HomeDirectory, ExclusionFilters.Sensitive);

        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, FileProvider, systemConfigLocation);
        Configuration = configBuilder.Build();

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
            collection.AddLogging(bldr =>
            {
                bldr.SetMinimumLevel(LogLevel.Trace)
                    .AddFilter("Microsoft", LogLevel.Information)
                    .AddFilter("Uncreated.Warfare.Database", LogLevel.Information)
                    .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning)
                    .AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
            });
        });

        ConfigureServices(bldr);

        _pluginLoader.ConfigureServices(bldr);

        ServiceProvider = bldr.Build();

        _logger = ServiceProvider.Resolve<ILogger<WarfareModule>>();

        GlobalLogger = ServiceProvider.Resolve<ILoggerFactory>().CreateLogger("Global");

        _logger.LogInformation("Using {0} services from core and {1} plugin(s).", ServiceProvider.ComponentRegistry.Registrations.Count(), _pluginLoader.Plugins.Count);

        UniTask.Create(async () =>
        {
            try
            {
                UniTask t = HostAsync();
                await t;
            }
            catch (Exception ex)
            {
                CommandWindow.LogError(ExceptionFormatter.FormatException(ex, ServiceProvider.Resolve<StackTraceCleaner>()));
                UnloadModule();
                Provider.shutdown();
            }
        });
    }

    void IModuleNexus.shutdown()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= HandleAssemblyResolve;

        if (Singleton == this)
            Singleton = null;

        _dispatcher = null;

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
            Unhost();
        }

        try
        {
            ServiceProvider.ResolveOptional<HarmonyPatchService>()?.RemoveAllPatches();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing patches.");
        }

        _logger.LogInformation("Cleaning up container...");
        ServiceProvider.Dispose();
        CommandWindow.Log("Done - Shutting down");
    }

    private void ConfigureServices(ContainerBuilder bldr)
    {
        Assembly thisAsm = Assembly.GetExecutingAssembly();

        bldr.RegisterType<MapScheduler>()
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
            collection.AddModularRpcs(isServer: false, searchedAssemblies: [Assembly.GetExecutingAssembly()]);
        });

        bldr.RegisterInstance(this)
            .As<WarfareModule>()
            .ExternallyOwned();

        Module thisModule = ModuleHook.modules.First(x => x.config.Name.Equals("Uncreated.Warfare", StringComparison.Ordinal) && x.assemblies.Contains(thisAsm));
        bldr.RegisterInstance(thisModule)
            .As<Module>()
            .ExternallyOwned();

        bldr.RegisterType<HarmonyPatchService>()
            .SingleInstance();

        bldr.Register<HarmonyPatchService, HarmonyLib.Harmony>((_, p) => p.Patcher)
            .SingleInstance();

        bldr.RegisterInstance(_gameObjectHost.GetOrAddComponent<WarfareTimeComponent>())
            .SingleInstance();
        bldr.RegisterInstance(_gameObjectHost.GetOrAddComponent<WarfareLifetimeComponent>())
            .SingleInstance();

        bldr.RegisterInstance(FileProvider)
            .As<IFileProvider>().As<PhysicalFileProvider>()
            .SingleInstance()
            .ExternallyOwned();

        bldr.RegisterType<AssetConfiguration>().SingleInstance();
        bldr.RegisterInstance(Configuration).ExternallyOwned();

        // homebase
        bldr.RegisterType<HomebaseConnector>().SingleInstance();

        // UI
        bldr.RegisterType<ModerationUI>().SingleInstance();
        bldr.RegisterType<KitMenuUI>().SingleInstance();
        bldr.RegisterType<ActionMenuUI>().SingleInstance();
        bldr.RegisterType<SquadMenuUI>().SingleInstance();
        bldr.RegisterType<SquadListUI>().SingleInstance();
        bldr.RegisterType<FobListUI>().SingleInstance();
        bldr.RegisterType<PopupUI>().SingleInstance();
        bldr.RegisterType<CaptureUI>().SingleInstance();
        bldr.RegisterType<ConventionalLeaderboardUI>().SingleInstance();
        bldr.RegisterType<FlagListUI>().SingleInstance();
        bldr.RegisterType<StagingUI>().SingleInstance();
        bldr.RegisterType<WinToastUI>().SingleInstance();
        bldr.RegisterType<XPUI>().SingleInstance();
        bldr.RegisterType<CreditsUI>().SingleInstance();
        bldr.RegisterType<TeamSelectorUI>().SingleInstance();
        bldr.RegisterType<VehicleHUD>().SingleInstance();

        bldr.RegisterType<TipService>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

        // event handlers
        bldr.RegisterType<VehicleSpawnedHandler>()
            .AsImplementedInterfaces().AsSelf();

        bldr.RegisterType<SteamApiService>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<AudioRecordManager>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<LayoutFactory>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<ActionManager>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

        bldr.RegisterType<EventDispatcher2>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<CooldownManager>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<CommandDispatcher>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterRpcType<UserPermissionStore>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();
        
        bldr.RegisterInstance(_gameObjectHost).ExternallyOwned();

        bldr.RegisterType<BuildableSaver>()
            .AsImplementedInterfaces().AsSelf()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

        bldr.RegisterType<VehicleInfoStore>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<AbandonService>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<VehicleService>()
            .AsImplementedInterfaces().AsSelf()
            .SingleInstance();

        bldr.RegisterType<UnityLoopTickerFactory>()
            .As<ILoopTickerFactory>();

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

        // Kits
        KitManager.ConfigureServices(bldr);

        bldr.RegisterType<DroppedItemTracker>()
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance();

        bldr.RegisterType<WhitelistService>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

        bldr.RegisterType<SignInstancer>()
            .AsSelf().AsImplementedInterfaces()
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

        bldr.RegisterType<PurchaseRecordsInterface>()
            .As<IPurchaseRecordsInterface>();

        // Layouts
        bldr.Register(_ => GetActiveLayout())
            .AsSelf()
            .ExternallyOwned();

        bldr.RegisterType<LayoutPhaseEventListenerProvider>()
            .As<IEventListenerProvider>()
            .SingleInstance();

        // Active ILayoutPhase
        bldr.Register(_ => GetActiveLayout().ActivePhase ?? throw new InvalidOperationException("There is not a phase currently loaded."));

        // FOBs
        bldr.RegisterType<DeploymentService>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

        bldr.RegisterType<FobManager>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

        bldr.RegisterType<FobConfiguration>()
            .SingleInstance();

        bldr.RegisterType<SquadManager>()
            .AsSelf().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

        // Active ITeamManager
        bldr.Register(_ => GetActiveLayout().TeamManager)
            .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Session);

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

        // Translations
        bldr.RegisterType<TranslationValueFormatter>()
            .As<ITranslationValueFormatter>()
            .SingleInstance();

        bldr.RegisterGeneric(typeof(TranslationInjection<>))
            .SingleInstance();

        bldr.RegisterType<TranslationService>()
            .As<ITranslationService>()
            .SingleInstance();

        bldr.RegisterType<ItemIconProvider>()
            .SingleInstance();

        bldr.RegisterType<AnnouncementService>()
            .AsSelf().AsImplementedInterfaces();

        // Database

        bldr.RegisterType<DatabaseInterface>()
            .AsSelf()
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

            return new ManualMySqlProvider(connectionString);
        });
    }

    public async UniTask ShutdownAsync(string reason, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);


        // prevent players from joining after shutdown start
        IPlayerService? playerService = ServiceProvider.ResolveOptional<IPlayerService>();
        playerService?.TakePlayerConnectionLock(token);

        // kick all players
        for (int i = Provider.clients.Count - 1; i >= 0; --i)
        {
            Provider.kick(Provider.clients[i].playerID.steamID, !string.IsNullOrWhiteSpace(reason) ? "Shutting down: \"" + reason + "\"." : "Shutting down.");
        }

        Object.Destroy(_gameObjectHost);
        _gameObjectHost = null!;

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
        GameThread.AssertCurrent();

        // set up level measurements as early as possible
        Level.onPrePreLevelLoaded += CartographyUtility.Init;

        CancellationToken token = UnloadToken;

        // this needs to happen almost instantly, can't wait for migration
        ServiceProvider.Resolve<MapScheduler>().ApplyMapSetting();

        // this too
        HarmonyPatchService patchService = ServiceProvider.Resolve<HarmonyPatchService>();

        patchService.ApplyAllPatches();

        bool connected = false;

        // migrate database before loading services
        _logger.LogDebug("Migrating database...");
        await using (ILifetimeScope scope = ServiceProvider.BeginLifetimeScope())
        await using (IDbContext dbContext = scope.Resolve<IDbContext>())
        {
            const double timeoutSec = 2.5;

            // check connection before migrating with a 2.5 second timeout
            await Task.WhenAny(Task.Run(async () =>
            {
                // ReSharper disable once AccessToDisposedClosure
                connected = await dbContext.Database.CanConnectAsync(token);

            }, token), Task.Delay(TimeSpan.FromSeconds(timeoutSec), token));
            
            if (!connected)
            {
                _logger.LogWarning($"Connection for migration timed out after {timeoutSec} second(s).");
            }
            else
            {
                await dbContext.Database.MigrateAsync(token).ConfigureAwait(false);
                _logger.LogInformation("Migration completed.");
            }
        }

        if (!connected)
        {
            _logger.LogError("Unable to connect to MySQL database. Please reconfigure and restart.");
            UnloadModule();
            Provider.shutdown();
            return;
        }

        await UniTask.SwitchToMainThread(token);

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
            try
            {
                if (!GameThread.IsCurrent)
                    await UniTask.SwitchToMainThread(token);
                _logger.LogDebug("Hosting {0}.", hostedService.GetType());
                await hostedService.StartAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hosting service {hostedService.GetType()}.");
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
        _logger.LogDebug("Unhosting {0} services and shutting down.", tasks.Length);
        for (int i = errIndex - 1; i >= 0; --i)
        {
            IHostedService hostedService = hostedServices[i];
            try
            {
                _logger.LogDebug("Unhosting {0}.", hostedService.GetType());
                tasks[i] = hostedService.StopAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping service {hostedService.GetType()}.");
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
            _logger.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);
        UnloadModule();
        Provider.shutdown();
    }

    public async UniTask InvokeLevelLoaded(CancellationToken token)
    {
        List<ILevelHostedService> hostedServices = ServiceProvider
            .Resolve<IEnumerable<ILevelHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        await UniTask.SwitchToMainThread(token);

        _logger.LogDebug("Hosting {0} services on level load.", hostedServices.Count);
        for (int i = 0; i < hostedServices.Count; i++)
        {
            ILevelHostedService hostedService = hostedServices[i];
            try
            {
                if (!GameThread.IsCurrent)
                    await UniTask.SwitchToMainThread(token);
                _logger.LogDebug("Hosting {0} on level load.", hostedService.GetType());
                await hostedService.LoadLevelAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hosting service {hostedService.GetType()} on level load.");
                break;
            }
        }
    }

    private async UniTask UnhostAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (_unloadedHostedServices)
            return;

        List<IHostedService> hostedServices = ServiceProvider
            .Resolve<IEnumerable<IHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        _logger.LogDebug("Unhosting {0} services.", hostedServices.Count);
        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < hostedServices.Count; ++i)
        {
            try
            {
                IHostedService hostedService = hostedServices[i];
                _logger.LogDebug("Unhosting {0}.", hostedService.GetType());
                tasks[i] = hostedService.StopAsync(CancellationToken.None);
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
            _logger.LogError("Errors encountered while unhosting:");
            FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
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
            .Resolve<IEnumerable<IHostedService>>()
            .OrderByDescending(x => x.GetType().GetPriority())
            .ToList();

        _logger.LogDebug("Unhosting {0} services synchronously.", hostedServices.Count);
        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < hostedServices.Count; ++i)
        {
            try
            {
                IHostedService hostedService = hostedServices[i];
                _logger.LogDebug("Unhosting {0}.", hostedService.GetType());
                tasks[i] = hostedService.StopAsync(timeoutSource.Token);
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
            if (tasks[i].Status is not UniTaskStatus.Canceled and not UniTaskStatus.Pending)
                continue;

            _logger.LogError(hostedServices[i].GetType() + $" - {tasks[i].Status}.");
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
            ? ServiceProvider.BeginLifetimeScope(LifetimeScopeTags.Session)
            : ServiceProvider.BeginLifetimeScope(LifetimeScopeTags.Session, builder);


        ILifetimeScope? oldScope = Interlocked.Exchange(ref _activeScope, newScope);

        if (ServiceProvider.Resolve<IPlayerService>() is PlayerService playerServiceImpl)
        {
            playerServiceImpl.ReinitializeScopedPlayerComponentServices();
        }

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
        ServiceProvider.Resolve<Module>().isEnabled = false;
    }

    private static Assembly? HandleAssemblyResolve(object sender, ResolveEventArgs args)
    {
        // UnityEngine.CoreModule includes JetBrains annotations for some reason, may as well use them.
        const string jetbrains = "JetBrains.Annotations, ";
        if (args.Name.StartsWith(jetbrains, StringComparison.Ordinal))
        {
            return typeof(Vector3).Assembly;
        }

        return null;
    }
}
