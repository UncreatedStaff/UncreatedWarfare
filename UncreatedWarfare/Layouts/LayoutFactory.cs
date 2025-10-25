using Autofac.Builder;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Sessions;
using Uncreated.Warfare.Util;
using UnityEngine.SceneManagement;

namespace Uncreated.Warfare.Layouts;

[Priority(int.MinValue)] // run last
public class LayoutFactory : IHostedService, IEventListener<PlayerJoined>
{
    private readonly WarfareModule _warfare;
    private readonly ILogger<LayoutFactory> _logger;
    private readonly IGameDataDbContext _dbContext;
    private readonly MapScheduler _mapScheduler;
    private readonly IConfiguration _systemConfig;
    private readonly IPlayerService _playerService;
    private readonly SessionManager _sessionService;
    private readonly byte _region;
    private UniTask _setupTask;

    private readonly string _layoutDir;

    private bool _hasPlayerLock = true;
    private bool _hasSessionLock;

    public bool IsLoading
    {
        get;
        private set
        {
            if (field == value)
                return;

            field = value;
            try
            {
                LoadingStateUpdated?.Invoke(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error thrown from LoadingStateUpdated listener.");
            }
        }
    }

    public event Action<bool>? LoadingStateUpdated;

    public FileInfo? NextLayout { get; set; }

    public LayoutFactory(
        WarfareModule warfare,
        ILogger<LayoutFactory> logger,
        IGameDataDbContext dbContext,
        MapScheduler mapScheduler,
        IConfiguration systemConfig,
        IPlayerService playerService,
        SessionManager sessionService)
    {
        _warfare = warfare;
        _logger = logger;
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _mapScheduler = mapScheduler;
        _systemConfig = systemConfig;
        _playerService = playerService;
        _sessionService = sessionService;
        _region = systemConfig.GetValue<byte>("region");
        IsLoading = true;

        _layoutDir = Path.Combine(warfare.HomeDirectory, "Layouts");
        Directory.CreateDirectory(_layoutDir);

        if (systemConfig["tests:startup_layout"] is not { Length: > 0 } startupLayout)
            return;

        string path = Path.Combine(_layoutDir, startupLayout);

        if (File.Exists(path))
            NextLayout = new FileInfo(path);
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        if (Level.isLoaded)
        {
            UniTask.Create(async () =>
            {
                await UniTask.NextFrame();
                OnSceneLoded(SceneManager.GetSceneByBuildIndex(Level.BUILD_INDEX_GAME), LoadSceneMode.Single);
                OnLevelLoaded(Level.BUILD_INDEX_GAME);
            });
        }
        else
        {
            SceneManager.sceneLoaded += OnSceneLoded;

            Level.onPostLevelLoaded += OnLevelLoaded;
        }

        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public async UniTask StopAsync(CancellationToken token)
    {
        SceneManager.sceneLoaded -= OnSceneLoded;
        Level.onPostLevelLoaded -= OnLevelLoaded;

        if (!_warfare.IsLayoutActive())
            return;
        
        Layout layout = _warfare.GetActiveLayout();
        IsLoading = true;
        try
        {
            if (layout.IsActive)
            {
                await layout.EndLayoutAsync(CancellationToken.None);
            }

            layout.Dispose();

            await UniTask.SwitchToMainThread(CancellationToken.None);
            _warfare.SetActiveLayout(null);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnSceneLoded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != Level.BUILD_INDEX_GAME)
            return;

        _setupTask = UniTask.Create(async () =>
        {
            try
            {
                // invoke IEarlyLevelHostedService
                await _warfare.InvokeEarlyLevelLoaded(_warfare.UnloadToken);

                await StartNextLayout(_warfare.UnloadToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting layout.");
                _ = _warfare.ShutdownAsync($"Error starting layout - {Accessor.Formatter.Format(ex.GetType())}");
            }
        }).Preserve();
    }

    private void OnLevelLoaded(int level)
    {
        if (level != Level.BUILD_INDEX_GAME)
            return;

        UniTask.Create(async () =>
        {
            bool hasPlayerConnectionLock = _hasPlayerLock;
            try
            {
                if (_setupTask.Status == UniTaskStatus.Pending)
                {
                    await _setupTask;
                }

                // invoke ILevelHostedService
                await _warfare.InvokeLevelLoaded(CancellationToken.None);

                await StartPendingLayoutAsync(hasPlayerConnectionLock);
                hasPlayerConnectionLock = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing loading layout.");
                _ = _warfare.ShutdownAsync($"Error finishing loading layout - {Accessor.Formatter.Format(ex.GetType())}");
            }
            finally
            {
                if (hasPlayerConnectionLock)
                {
                    _playerService?.ReleasePlayerConnectionLock();
                    _hasPlayerLock = false;
                }
            }
        });
    }

    /// <summary>
    /// Forcibly end the current layout and create a new random layout from the config files.
    /// </summary>
    public async UniTask StartNextLayout(CancellationToken token = default)
    {
        await UniTask.SwitchToThreadPool();
        

        LayoutInfo? newLayout = null;
        if (NextLayout != null)
        {
            newLayout = ReadLayoutInfo(NextLayout.FullName, true);
            if (newLayout == null)
                _logger.LogWarning($"Failed to find preferred next layout {NextLayout.Name}.");
            else
                _logger.LogInformation($"Loading preferred next layout {NextLayout.Name} ({newLayout.DisplayName}).");
            NextLayout = null;
        }

        newLayout ??= SelectRandomLayout();

        await UniTask.SwitchToMainThread(token);

        // stops players from joining both before the first layout starts and between layouts.
        bool playerJoinLockTaken = _hasPlayerLock;
        IsLoading = true;
        try
        {
            if (_warfare.IsLayoutActive())
            {
                if (!playerJoinLockTaken)
                {
                    await _playerService.TakePlayerConnectionLock(token);
                    playerJoinLockTaken = true;
                    _hasPlayerLock = true;
                }

                Layout oldLayout = _warfare.GetActiveLayout();
                if (oldLayout.IsActive)
                {
                    await oldLayout.EndLayoutAsync(CancellationToken.None);
                }

                if (!_hasSessionLock)
                {
                    await _sessionService.WaitAsync();
                    await UniTask.SwitchToMainThread(token);
                    _hasSessionLock = true;
                }

                oldLayout.Dispose();
                await UniTask.SwitchToMainThread(CancellationToken.None);
                _warfare.SetActiveLayout(null);
                _logger.LogDebug($"Unloaded previous layout {oldLayout.LayoutInfo.DisplayName}.");
            }
            else
            {
                // lock is on by default on startup.
                playerJoinLockTaken = true;
            }

            await CreateLayoutAsync(newLayout, playerJoinLockTaken, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (_hasSessionLock)
            {
                _sessionService.Release();
                _hasSessionLock = false;
            }
            IsLoading = false;
            if (playerJoinLockTaken)
            {
                _playerService.ReleasePlayerConnectionLock();
                _hasPlayerLock = false; 
            }

            _logger.LogError(ex, "Error creating layout {0}.", newLayout.FilePath);
            _ = UniTask.Create(async () =>
            {
                await UniTask.NextFrame();
                await StartNextLayout(token);
            });
        }
    }

    /// <summary>
    /// Actually creates a new layout with <paramref name="layoutInfo"/> as it's startup args.
    /// </summary>
    private async UniTask CreateLayoutAsync(LayoutInfo layoutInfo, bool playerJoinLockTaken, CancellationToken token = default)
    {
        if (!typeof(Layout).IsAssignableFrom(layoutInfo.LayoutType))
        {
            throw new ArgumentException($"Type {Accessor.ExceptionFormatter.Format(layoutInfo.LayoutType)} is not assignable to Layout.", nameof(layoutInfo));
        }

        List<IDisposable> stuffThatNeedsDisposedLater = new List<IDisposable>();
        // load plugin services
        Action<ContainerBuilder> lifetimeAction = GetServiceChildLifetimeFactory(
            layoutInfo,
            layoutInfo.Layout.GetSection("Services"),
            layoutInfo.Layout.GetSection("Components"),
            stuffThatNeedsDisposedLater
            );

        ILifetimeScope scopedProvider = await _warfare.CreateScopeAsync(lifetimeAction, token);
        await UniTask.SwitchToMainThread(token);

        // active layout is set in Layout default constructor
        Layout layout = (Layout)Activator.CreateInstance(layoutInfo.LayoutType, [ scopedProvider, layoutInfo, stuffThatNeedsDisposedLater ])!;
        if (_warfare.GetActiveLayout() != layout)
        {
            _warfare.SetActiveLayout(layout);
        }

        GameRecord record = new GameRecord
        {
            Season = WarfareModule.Season,
            StartTimestamp = DateTimeOffset.UtcNow,
            Gamemode = layoutInfo.DisplayName,
            Map = _mapScheduler.Current,
            Region = _region
        };

        try
        {
            _dbContext.Games.Add(record);

            await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating layout record.");
        }

        if (_hasSessionLock)
        {
            _sessionService.Release();
            _hasSessionLock = false;
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);
        await layout.InitializeLayoutAsync(record, CancellationToken.None);

        if (_playerService is PlayerService playerServiceImpl)
        {
            playerServiceImpl.ReinitializeScopedPlayerComponentServices();
        }

        if (Level.isLoaded)
        {
            await StartPendingLayoutAsync(playerJoinLockTaken);
        }
    }

    private async Task StartPendingLayoutAsync(bool playerJoinLockTaken)
    {
        Layout layout = _warfare.GetActiveLayout();
        try
        {
            IEnumerable<ILayoutStartingListener> listeners = _warfare.ScopedProvider.Resolve<IEnumerable<ILayoutStartingListener>>()
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
                    _logger.LogError(ex, "Error hosting ILayoutStartingListener {0} for layout {1}.", listener.GetType(), layout);
                }
            }

            await layout.BeginLayoutAsync(CancellationToken.None);

            if (playerJoinLockTaken && _hasPlayerLock)
            {
                _playerService?.ReleasePlayerConnectionLock();
                _hasPlayerLock = false;
            }
        }
        finally
        {
            if (_warfare.IsLayoutActive() && _warfare.GetActiveLayout() == layout)
                IsLoading = false;
        }
    }

    private void RegisterDefaultServices(ContainerBuilder bldr, LayoutInfo layoutInfo)
    {
        // Layout
        IRegistrationBuilder<Layout, SimpleActivatorData, SingleRegistrationStyle> layoutRegistration
            = bldr.Register<WarfareModule, Layout>(wf => wf.GetActiveLayout());

        if (layoutInfo.LayoutType != typeof(Layout))
        {
            // child layout types
            layoutInfo.LayoutType.ForEachBaseType((t, _) =>
            {
                if (t.IsAssignableFrom(typeof(Layout)))
                    return false;

                layoutRegistration.As(t);
                return true;
            });
        }

        layoutRegistration
            .AsSelf().AsImplementedInterfaces()
            .SingleInstance()
            .ExternallyOwned();

        // ITeamManager
        bldr.Register<WarfareModule, ITeamManager<Team>>(wf => wf.GetActiveLayout().TeamManager)
            .SingleInstance();

        Type[]? argTypes = null;
        object[]? args = null;

        // invoke LayoutConfigureServicesCallbackAttribute methods
        layoutInfo.LayoutType.ForEachBaseType((t, _) =>
        {
            Array attributes = t.GetCustomAttributes(typeof(LayoutConfigureServicesCallbackAttribute), inherit: false);
            for (int i = 0; i < attributes.Length; i++)
            {
                LayoutConfigureServicesCallbackAttribute attr = (LayoutConfigureServicesCallbackAttribute)attributes.GetValue(i);
                if (string.IsNullOrEmpty(attr.MethodName))
                    continue;

                argTypes ??= [ typeof(ContainerBuilder), typeof(LayoutInfo) ];
                MethodInfo? method = t.GetMethod(
                    attr.MethodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null,
                    CallingConventions.Any,
                    argTypes,
                    null
                );
                if (method == null)
                {
                    _logger.LogWarning($"Unknown method \"{attr.MethodName}\" from LayoutConfigureServicesCallback attribute on layout type {t.FullName}.");
                    continue;
                }

                args ??= [ bldr, layoutInfo ];

                try
                {
                    method.Invoke(null, args);
                }
                catch (TargetInvocationException invEx) when (invEx.InnerException != null)
                {
                    _logger.LogError(invEx.InnerException, $"Error thrown by {attr.MethodName} callback on layout type {t.FullName}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error invoking {attr.MethodName} callback on layout type {t.FullName}.");
                }
            }
        });
    }

    /// <summary>
    /// Loads all instances of <see cref="ILayoutServiceConfigurer"/> from the 'Services' list in the config and creates a lifetime scope builder using them.
    /// </summary>
    private Action<ContainerBuilder> GetServiceChildLifetimeFactory(LayoutInfo layoutInfo, IConfiguration serviceInfo, IConfiguration componentInfo, IList<IDisposable> disposableConfiguration)
    {
        List<(string?, Type?)> types = serviceInfo.GetChildren()
            .Select(x => (x.Value, ContextualTypeResolver.ResolveType(x.Value, typeof(ILayoutServiceConfigurer))))
            .ToList();

        List<Exception>? missingTypeExceptions = null;
        foreach ((string? typeName, Type? configurerType) in types)
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

        List<(IConfiguration, Type)> componentTypes = new List<(IConfiguration, Type)>();

        bool anyComponentFailed = false;
        foreach (IConfigurationSection section in componentInfo.GetChildren())
        {
            string? componentTypeStr = section["Type"];
            if (!ContextualTypeResolver.TryResolveType(componentTypeStr, out Type? type))
            {
                _logger.LogError("Unknown component type: {0}.", componentTypeStr);
                anyComponentFailed = true;
                continue;
            }

            IConfiguration componentConfig = section;
            ApplyVariation(ref componentConfig, Accessor.Formatter.Format(type), layoutInfo.FilePath);
            if (componentConfig is IDisposable disp)
                disposableConfiguration.Add(disp);
            componentTypes.Add((componentConfig, type));
        }

        if (anyComponentFailed)
            throw new LayoutConfigurationException("At least one component type couldn't be found.");

        object[] parameters = [ layoutInfo, serviceInfo ];
        return bldr =>
        {
            IServiceProvider serviceProvider = _warfare.ServiceProvider.Resolve<IServiceProvider>();

            RegisterDefaultServices(bldr, layoutInfo);

            // register components
            for (int i = 0; i < componentTypes.Count; i++)
            {
                (IConfiguration section, Type type) = componentTypes[i];
                IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> reg = bldr.RegisterType(type);

                type.ForEachBaseType((x, _) => reg.As(x));
                reg.AsImplementedInterfaces()
                   .SingleInstance()
                   .WithParameter(TypedParameter.From(section));
            }

            // run service configurers
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
                        configurerType,
                        configurerType!.Assembly.GetName().Name
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to configure services using type {0} from assembly {1}.",
                        configurerType,
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
                        configurerType,
                        configurerType.Assembly.GetName().Name
                    );
                }
            }
        };
    }

    public LayoutInfo? SelectLayoutByName(string layoutName)
    {
        LayoutInfo? layout = ReadLayoutInfo(Path.Combine(
            _layoutDir,
            !layoutName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ? layoutName + ".yml" : layoutName),
            false
        );

        if (layout != null)
            return layout;

        List<FileInfo> all = GetBaseLayoutFiles();
        FileInfo? single = null;
        foreach (FileInfo info in all)
        {
            if (!info.Name.Equals(layoutName, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            if (single == null)
                single = info;
            else
                return null;
        }

        if (single == null)
            return null;

        return ReadLayoutInfo(single.FullName, false);
    }

    /// <summary>
    /// Read all layouts and select a random one.
    /// </summary>
    /// <remarks>Reading them each time keeps us from having to reload config.</remarks>
    /// <exception cref="InvalidOperationException">No layouts are configured.</exception>
    public LayoutInfo SelectRandomLayout(bool seeding = false)
    {
        List<LayoutInfo> layouts = GetBaseLayoutFiles()
            .Select(x => ReadLayoutInfo(x.FullName, false, expectedSeedingState: seeding))
            .Where(x => x != null)
            .ToList()!;

        if (layouts.Count == 0)
        {
            throw new InvalidOperationException("There are no layouts configured.");
        }

        int index = RandomUtility.GetIndex(layouts, x => x.Weight);

        // dispose other config providers
        for (int i = 0; i < layouts.Count; ++i)
        {
            layouts[i].Dispose();
        }

        return ReadLayoutInfo(layouts[index].FilePath, true) ?? throw new InvalidOperationException("Failed to load a layout somehow.");
    }

    /// <summary>
    /// Read a <see cref="LayoutInfo"/> from the given file and open a configuration root for the file.
    /// </summary>
    public LayoutInfo? ReadLayoutInfo(string file, bool variations, bool? expectedSeedingState = null, bool reloadOnChange = false)
    {
        if (!File.Exists(file))
            return null;

        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, _warfare.FileProvider, file, reloadOnChange: reloadOnChange);
        IConfigurationRoot root = configBuilder.Build();

        try
        {
            // if this layout is a seeding layout, by default all layouts in the ~/Seeding/** folder are seeding layouts
            bool seeding = Path.GetRelativePath(_layoutDir, file)
                .StartsWith("Seeding", StringComparison.OrdinalIgnoreCase);

            seeding = root.GetValue("IsSeeding", seeding);
            if (expectedSeedingState.HasValue && seeding != expectedSeedingState.Value)
            {
                if (root is IDisposable disp)
                    disp.Dispose();
                return null;
            }

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

            IConfiguration configuration = root;
            if (variations)
                ApplyVariation(ref configuration, displayName, file);

            LayoutInfo layoutInfo = new LayoutInfo
            {
                LayoutType = layoutType,
                Layout = (IConfigurationRoot)configuration,
                Weight = weight,
                DisplayName = displayName,
                FilePath = file,
                IsSeeding = seeding
            };

            configuration.Bind(layoutInfo.Configuration);

            return layoutInfo;
        }
        catch
        {
            if (root is IDisposable disp)
                disp.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Apply variations read from a specific configuration section.
    /// </summary>
    public void ApplyVariation(ref IConfiguration configuration, string context, string baseFilePath)
    {
        List<LayoutVariationInfo>? variations = ReadVariations(context, baseFilePath, configuration);
        if (variations is not { Count: > 0 })
            return;

        LayoutVariationInfo variation = variations[RandomUtility.GetIndex(variations, v => v.Weight)];
        configuration = new ConfigurationBuilder()
            .Add(new ChainedConfigurationSource { Configuration = configuration, ShouldDisposeConfiguration = true })
            .AddYamlFile(variation.FileName, false, true)
            .Build();

        _logger.LogInformation($"Variation chosen for {context}: {variation.FileName}.");
    }

    /// <summary>
    /// Read a list of variations from a configuration section.
    /// </summary>
    public List<LayoutVariationInfo>? ReadVariations(string context, string baseFilePath, IConfiguration configSection)
    {
        IConfigurationSection variationInclude = configSection.GetSection("IncludedVariations");
        IConfigurationSection variationExclude = configSection.GetSection("ExcludedVariations");

        bool anyMatches = false;
        Matcher matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        if (variationInclude.Get<string[]>() is { Length: > 0 } includes)
        {
            foreach (string str in includes)
            {
                matcher.AddInclude(str);
                anyMatches = true;
            }
        }
        else if (variationInclude.Get<string>() is { } include && !string.IsNullOrWhiteSpace(include))
        {
            matcher.AddInclude(include);
            anyMatches = true;
        }

        if (variationExclude.Get<string[]>() is { Length: > 0 } excludes)
        {
            foreach (string str in excludes)
                matcher.AddExclude(str);
        }
        else if (variationExclude.Get<string>() is { } exclude && !string.IsNullOrWhiteSpace(exclude))
            matcher.AddExclude(exclude);

        if (!anyMatches)
            return null;


        string? baseDir = Path.GetDirectoryName(baseFilePath);
        if (baseDir == null)
            return null;


        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDir)));

        // find variation files
        List<LayoutVariationInfo> variationFiles = [];
        if (result.HasMatches)
        {
            foreach (FilePatternMatch variationFile in result.Files)
            {
                string path = Path.GetFullPath(variationFile.Path, baseDir);
                if (!YamlUtility.CheckMatchesMapFilterAndReadWeight(path, out double weight))
                {
                    _logger.LogDebug($"Variations - {context} skipped variation {variationFile.Stem ?? path}.");
                    continue;
                }

                LayoutVariationInfo variation = default;
                variation.Weight = weight;
                variation.FileName = path;

                variationFiles.Add(variation);
                _logger.LogDebug($"Variations - {context} matched variation {variationFile.Stem ?? path}.");
            }
        }

        if (variationFiles.Count != 0)
            return variationFiles;

        _logger.LogWarning($"There are no matching variation files for {context}.");
        return null;
    }

    /// <summary>
    /// Get all base config files in the layout folder.
    /// </summary>
    public List<FileInfo> GetBaseLayoutFiles()
    {
        DirectoryInfo layoutDirectory = new DirectoryInfo(_layoutDir);

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
                    if (YamlUtility.CheckMatchesMapFilter(yamlFile.FullName))
                        baseLayoutConfigs.Add(yamlFile);

                    break;

                case DirectoryInfo dir:
                    FileInfo[] files = dir.GetFiles("*.yml", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0)
                        break;

                    Array.Sort(files, FileNameLengthComparer.Instance);

                    // include base files like: 'AAS Mechanized Infantry.yml' but don't include files like: 'AAS Mechanized Infantry.Yellowknife.yml'
                    string? baseFile = null;
                    for (int i = 0; i < files.Length; ++i)
                    {
                        FileInfo file = files[i];
                        if (baseFile != null && file.FullName.StartsWith(baseFile, StringComparison.Ordinal) && file.FullName.Length > baseFile.Length + 5 && file.FullName[baseFile.Length] == '.')
                            continue;

                        baseFile = file.FullName[..^4];
                        baseLayoutConfigs.Add(file);
                    }

                    break;
            }
        }

        return baseLayoutConfigs;
    }

    private sealed class FileNameLengthComparer : IComparer<FileInfo>
    {
        public static readonly FileNameLengthComparer Instance = new FileNameLengthComparer();
        static FileNameLengthComparer() { }
        private FileNameLengthComparer() { }

        /// <inheritdoc />
        public int Compare(FileInfo x, FileInfo y)
        {
            if (x == y)
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            return x.FullName.Length.CompareTo(y.FullName.Length);
        }
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
                _logger.LogTrace($"Hosting {hostedServices[i].GetType()}.");
                await hostedServices[i].StartAsync(token);
                continue;
            }
            catch (OperationCanceledException ex) when (token.IsCancellationRequested)
            {
                _logger.LogWarning("Layout {0} canceled.", layout);
                errIndex = i;
                thrownException = ex;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hosting service {0} in layout {1}.", hostedServices[i].GetType(), layout);
                errIndex = i;
                thrownException = ex;
            }

            break;
        }

        // handles if a service failed to start up, unloads the services that did start up and ends the layout.
        if (errIndex == -1)
        {
            layout.LayoutStats.StartTimestamp = DateTimeOffset.UtcNow;
            _dbContext.Update(layout.LayoutStats);
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            await UniTask.SwitchToMainThread(token);

            // set layout ID of all players
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                player.Save.LastGameId = layout.LayoutId;
                player.Save.ResetOnGameStart();
                player.Save.Save();
            }

            _logger.LogDebug("Layout {0} hosted.", layout);
            return;
        }
        
        await UniTask.SwitchToMainThread();

        if (!layout.UnloadedHostedServices)
        {
            UniTask[] tasks = new UniTask[errIndex];
            for (int i = errIndex - 1; i >= 0; --i)
            {
                ILayoutHostedService hostedService = hostedServices[i];
                try
                {
                    _logger.LogTrace($"Unhosting {hostedServices[i].GetType()}.");
                    tasks[i] = hostedService.StopAsync(CancellationToken.None).Preserve();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping service {0} in layout {1}.", hostedServices[i].GetType(), layout);
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
        if (layout.WasStarted)
        {
            layout.LayoutStats.EndTimestamp ??= DateTimeOffset.UtcNow;
            if (layout.Data.TryGetValue(KnownLayoutDataKeys.WinnerTeam, out object winner) && winner is Team team)
            {
                layout.LayoutStats.WinnerFactionId = team.Faction.PrimaryKey;
            }
            _dbContext.Update(layout.LayoutStats);

            await _dbContext.SaveChangesAsync(token);
        }

        _dbContext.ChangeTracker.Clear();

        if (layout.UnloadedHostedServices)
            return;

        // stop any services implementing ILayoutHostedService
        List<ILayoutHostedService> hostedServices = layout.ServiceProvider
            .Resolve<IEnumerable<ILayoutHostedService>>()
            .OrderBy(x => x.GetType().GetPriority())
            .ToList();

        UniTask[] tasks = new UniTask[hostedServices.Count];
        for (int i = 0; i < tasks.Length; ++i)
        {
            try
            {
                _logger.LogTrace($"Unhosting {hostedServices[i].GetType()}.");
                tasks[i] = hostedServices[i].StopAsync(CancellationToken.None).Preserve();
            }
            catch (Exception ex)
            {
                tasks[i] = UniTask.FromException(ex);
            }
        }

        layout.UnloadedHostedServices = true;

        try
        {
            Task waitTask = UniTask.WhenAll(tasks).AsTask();
            await Task.WhenAny(waitTask, Task.Delay(15000, token));
            if (!waitTask.IsCompleted)
            {
                _logger.LogError("15 second timeout reached while ending layout {0}:", layout);
                FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
            }
        }
        catch
        {
            _logger.LogError("Errors encountered while ending layout {0}:", layout);
            FormattingUtility.PrintTaskErrors(_logger, tasks, hostedServices);
        }

        _logger.LogDebug("Unhosted layout.");
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        ulong loadoutId = _warfare.IsLayoutActive() ? _warfare.GetActiveLayout().LayoutId : 0;
        if (e.Player.Save.LastGameId == loadoutId)
            return;

        e.Player.Save.LastGameId = loadoutId;
        e.Player.Save.ResetOnGameStart();
        e.Player.Save.Save();
    }
}