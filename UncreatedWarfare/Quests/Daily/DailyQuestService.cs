using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Quests.Daily.Workshop;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Quests.Daily;

[Priority(-1 /* after QuestService */)]
public class DailyQuestService : ILayoutHostedService, IEventListener<PlayerJoined>, IEventListener<PlayerLeft>
{

    private readonly WarfareModule _module;
    private readonly ChatService _chatService;
    private readonly ILogger<DailyQuestService> _logger;
    private readonly QuestService _questService;
    private readonly WorkshopUploader _uploader;
    private readonly QuestTranslations _translations;
    private readonly IPlayerService _playerService;
    private readonly EventDispatcher _eventDispatcher;

    // basic JSON storage for the generated daily quest file which stored the randomly generated quests for the next 2 weeks
    private readonly DailyQuestConfiguration _config;

    // handles waiting for warnings before regeneration then finally the regeneration itself.
    // didnt want to use a coroutine here because i think system timers are more effecient for longer running tasks (maybe idk)
    private Timer? _tickTimer;

    // list of warnings before the actual regeneration, where each element is a fraction of the total length of a day - NextDailyQuestDay.Subtract( DaySimulatedLength * WarningTimes[n] )
    // 0 - next tick will go to next day
    // 1 - next tick is 1/144 th of a day from next day
    // 2 - next tick is 1/24 th of a day from next day
    private static readonly float[] WarningTimes = [ 0, 1f/144f, 1f/24f ]; //[ 0, 1f/4f, 1f/2f ];

    // the next warning to be sent in WarningTimes, where 0 will generate new quests
    private int _warningStep;

    // if a mod is uploading
    private bool _isAwaitingDailyQuestRenewal;

    private ulong _workshopId;
    private bool _enabled;
    private readonly bool _modEnabled;
    private readonly string? _steamcmdPath;
    private readonly string? _modName;
    private readonly string? _modIconPath;
    private readonly string? _steamcmdLoginUsername;
    private readonly string? _steamcmdLoginPassword;

    private ILoopTicker? _nextDailyQuestUploadTicker;
    private bool _isClosing;

    // if -1, no data available. regenerate asap
    private int _index = -1;

    private CancellationTokenSource? _regenerationTokenSource;
    private Task? _regenerationTask;

    /// <summary>
    /// Number of quests generated at a time.
    /// </summary>
    /// <remarks>Twice this number will be in the mod at once.</remarks>
    public const int DaySectionLength = 7;

    /// <summary>
    /// Number of quests per day.
    /// </summary>
    public const int PresetLength = 3;

    // This is double the amount of days per mod generation to allow time for people to download the new version of the mod.
    // Basically the last half of last weeks become the first half of next weeks that way players already have them in their mod without needing to update for a few days.
    // 7 days should be plenty of time.

    /// <summary>
    /// Total number of days that will exist at once.
    /// </summary>
    internal const int DayLength = DaySectionLength * 2;

    internal const ushort StartQuestAssetId = 62000;
    internal const ushort StartFlagId = 62000;

    /// <summary>
    /// Amount of time in a 'day'. This can be shortened significantly for testing purposes (30 seconds is usually good but WarningTimes also needs changed).
    /// </summary>
    private static readonly TimeSpan DaySimulatedLength = TimeSpan.FromDays(1d);

    /// <summary>
    /// The time at which new quests will be generated.
    /// </summary>
    public DateTime NextDailyQuestRegenerate { get; set; }

    /// <summary>
    /// The time at which the next day will start.
    /// </summary>
    public DateTime NextDailyQuestDay { get; set; }

    public DailyQuestDay[]? Days => _config is { HasData: true, Days.Length: DayLength } && _config.Days.All(x => x?.Presets is { Length: PresetLength })
                                         ? (DailyQuestDay[])_config.Days!
                                         : null;

    public DailyQuestService(
        IConfiguration systemConfig,
        ChatService chatService,
        WarfareModule module,
        ILogger<DailyQuestService> logger,
        ILogger<DailyQuestConfiguration> configLogger,
        QuestService questService,
        WorkshopUploader uploader,
        TranslationInjection<QuestTranslations> translations,
        IPlayerService playerService,
        EventDispatcher eventDispatcher)
    {
        _chatService = chatService;
        _logger = logger;
        _questService = questService;
        _uploader = uploader;
        _playerService = playerService;
        _eventDispatcher = eventDispatcher;
        _translations = translations.Value;
        _module = module;
        _config = new DailyQuestConfiguration(Path.Combine(module.HomeDirectory, "Quests", "Daily Quests.json"), configLogger, questService);


        IConfigurationSection section = systemConfig.GetSection("daily_quests");

        _workshopId = section.GetValue<ulong>("workshop_id");

        _enabled = section.GetValue<bool>("enabled");
        _steamcmdPath = section["steamcmd"];
        _modIconPath = section["mod_icon"];
        _modName = section["mod_name"];
        _steamcmdLoginUsername = section["steam_username"];

        try
        {
            string? passwordBase64 = section["steam_password"];
            if (!string.IsNullOrWhiteSpace(passwordBase64))
                _steamcmdLoginPassword = Encoding.UTF8.GetString(Convert.FromBase64String(passwordBase64));
        }
        catch (FormatException)
        {
            _logger.LogError("Invalid Base64 password.");
        }

        _modEnabled = section.GetValue<bool>("mod_enabled")
                      && !string.IsNullOrWhiteSpace(_steamcmdPath)
                      && !string.IsNullOrWhiteSpace(_steamcmdLoginPassword)
                      && !string.IsNullOrWhiteSpace(_steamcmdLoginUsername);

        if (string.IsNullOrWhiteSpace(_steamcmdPath) || !File.Exists(_steamcmdPath))
        {
            _logger.LogError("SteamCMD executable not found.");
            _modEnabled = false;
        }
        else
        {
            _logger.LogDebug("SteamCMD executable located at {0}.", _steamcmdPath);
        }

        if (!_enabled)
        {
            _logger.LogInformation("Daily Quests disabled.");
        }
        else if (!_modEnabled)
        {
            _logger.LogInformation("Daily Quests mod upload disabled.");
        }
        else
        {
            _logger.LogInformation("Daily Quests enabled: {0}.", _workshopId);
        }
    }

    private void AddModIdToServerMenu(ulong workshopId)
    {
        GameThread.AssertCurrent();

        if (Provider.getServerWorkshopFileIDs().Contains(workshopId))
            return;

        Provider.registerServerUsingWorkshopFileId(workshopId);
        List<ulong> ids = Provider.getServerWorkshopFileIDs();

        if (ids.Count <= 0)
            return;

        StringBuilder modList = new StringBuilder(ids.Count * 17 + (ids.Count - 1));
        for (int index = 0; index < ids.Count; ++index)
        {
            if (index != 0)
                modList.Append(',');

            modList.Append(ids[index]);
        }

        int ttlLen = modList.Length;

        // split the mod list into 127 character segments of the whole string.
        // See Provder.onDedicatedUGCInstalled and MenuPlayServerInfoUI.onRulesQueryRefreshed
        int segmentCount = (ttlLen - 1) / 127 + 1;
        int segmentIndex = 0;
        SteamGameServer.SetKeyValue("Mod_Count", segmentCount.ToString());
        for (int segmentStartIndex = 0; segmentStartIndex < ttlLen; segmentStartIndex += 127)
        {
            int length = Math.Min(ttlLen - segmentStartIndex, 127);
            string segmentContents = modList.ToString(segmentStartIndex, length);
            SteamGameServer.SetKeyValue("Mod_" + segmentIndex, segmentContents);
            ++segmentIndex;
        }
    }

    async UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        if (!_enabled)
        {
            return;
        }

        if (_questService.Templates.Count(x => x.CanBeDailyQuest) < PresetLength)
        {
            _enabled = false;
            _logger.LogWarning("Daily quests disabled - not enough registered quest templates.");
            return;
        }

        if (_modEnabled && _workshopId != 0)
        {
            AddModIdToServerMenu(_workshopId);
        }

        await _config.Read(token);
        await UniTask.SwitchToMainThread(token);

        if (TickRegenerate())
        {
            DailyQuestRegenerateResult result = await RegenerateDailyQuests();
            if (result.Days != null)
                StartDailyQuestUpload(result);
        }
        else
        {
            if (UpdateTimers())
                SetupQuestDayForAllPlayers();
            LoadAssets(GetModFolder());
        }
    }

    async UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        RemoveTrackers(true);

        Interlocked.Exchange(ref _nextDailyQuestUploadTicker, null)?.Dispose();

        _isClosing = true;

        _regenerationTokenSource?.Cancel();
        if (_regenerationTask is { IsCompleted: false } regenTask)
        {
            try
            {
                await regenTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error continuing regen task.");
            }
            _regenerationTask = null;
        }

        _regenerationTokenSource?.Dispose();

        await UniTask.SwitchToMainThread(token);

        UpdateTimers();
    }

    /// <summary>
    /// Actually generates either 7 or 14 quests. This is done by picking random quest templates and evaluating random values for the parameters.
    /// </summary>
    private async UniTask<DailyQuestRegenerateResult> RegenerateDailyQuests()
    {
        _isAwaitingDailyQuestRenewal = true;
        List<QuestTemplate> availableTemplates = _questService.Templates.Where(x => x.CanBeDailyQuest).ToList();
        int presetLength = PresetLength;
        if (availableTemplates.Count < PresetLength)
        {
            _logger.LogWarning("Not enough quests to generate a full day.");
            presetLength = availableTemplates.Count;
            if (presetLength == 0)
                return default;
        }

        bool shift = _index >= DaySectionLength && _config.Days != null && _config.Days.All(x => x?.Presets != null && x.Presets.All(y => y != null));

        DailyQuestDay[] days = new DailyQuestDay[DayLength];
        int index = 0;
        if (shift)
        {
            // copy last half of days to the first half and only generate a week instead of two, this helps with missing asset errors from clients
            index = DaySectionLength;
            Array.Copy(_config.Days!, DaySectionLength, days, 0, DayLength - DaySectionLength);
        }
                                                     // only round to day if not in testing mode
        DateTime time = shift ? days[0].StartTime : (DaySimulatedLength == TimeSpan.FromDays(1d) ? DateTime.UtcNow.Date : DateTime.UtcNow);
        ushort startId = (ushort)(shift ? days[DaySectionLength - 1].Id + 1 : StartQuestAssetId);
        ushort flagStartId = (ushort)(shift ? days[DaySectionLength - 1].Presets![^1]!.Flag + 1 : StartFlagId);

        if (startId - StartQuestAssetId >= DaySectionLength && flagStartId - StartFlagId >= DaySectionLength * presetLength)
        {
            startId = StartQuestAssetId;
            flagStartId = StartFlagId;
        }

        for (int day = index; day < DayLength; ++day)
        {
            DailyQuestDay d = new DailyQuestDay
            {
                StartTime = time,
                Asset = Guid.NewGuid(),
                Id = startId++,
                Presets = new DailyQuestPreset?[presetLength]
            };

            time = time.Add(DaySimulatedLength);
            for (int preset = 0; preset < presetLength; ++preset)
            {
                QuestTemplate template;
                do
                {
                    int templateIndex = RandomUtility.GetIndex(availableTemplates);
                    template = availableTemplates[templateIndex];
                }
                while (Array.Exists(d.Presets, x => x != null && string.Equals(x.TemplateName, template.Name)));

                DailyQuestPreset p = new DailyQuestPreset
                {
                    Flag = flagStartId++,
                    Key = Guid.NewGuid(),
                    TemplateName = template.Name,
                    Day = d
                };

                d.Presets[preset] = p;

                IQuestState state = await template.CreateState();

                IQuestReward[] rewards = new IQuestReward[template.Rewards.Count];

                int rewardIndex = 0;
                for (int i = 0; i < rewards.Length; ++i)
                {
                    IQuestReward? reward = template.Rewards[i].GetReward(state);
                    if (reward == null)
                        continue;

                    rewards[rewardIndex] = reward;
                    ++rewardIndex;
                }

                if (rewardIndex != rewards.Length)
                    Array.Resize(ref rewards, rewardIndex);

                p.UpdateState(state);
                p.RewardOverrides = rewards;
            }

            days[day] = d;
        }

        DailyQuestRegenerateResult result = default;
        result.Days = days;
        return result;
    }

    private string GetModFolder()
    {
        return Path.Combine(_module.HomeDirectory, "Quests", "UncreatedDailyQuests");
    }

    public async Task<bool> ReuploadMod(bool uploadMod, DailyQuestDay[] days, CancellationToken token)
    {
        string outDir = GetModFolder();
        DailyQuestAssetWriter.WriteDailyQuestFiles(days, outDir);

        if (!uploadMod)
            return false;

        string iconPath = string.IsNullOrWhiteSpace(_modIconPath)
            ? Path.GetFullPath(_modIconPath, _module.HomeDirectory)
            : string.Empty;

        Console.WriteLine("1");
        ulong? modId = await _uploader.UploadMod(new WorkshopUploadParameters
        {
            Title = _modName ?? "Uncreated Daily Quests",
            ChangeNote = "Added this week's quests.",
            ContentFolder = GetModFolder(),
            Description = "Automatically generated workshop item that is filled with automatically generated daily quests for the next week.",
            SteamCmdPath = _steamcmdPath!,
            Username = _steamcmdLoginUsername!,
            Password = _steamcmdLoginPassword!,
            ModId = _workshopId,
            ImageFile = iconPath
        }, _logger, token);

        if (modId.HasValue)
        {
            if (modId.Value != 0)
            {
                await UniTask.SwitchToMainThread(CancellationToken.None);
                AddModIdToServerMenu(modId.Value);
            }
            _logger.LogInformation("Mod upload complete. ID: {0}.", modId.Value);
            _workshopId = modId.Value;
            return true;
        }

        _logger.LogWarning("Mod upload failed.");
        return false;
    }

    /// <summary>
    /// Writes the quest files as <see cref="QuestAsset"/> .dat files and uploads them to the workshop. Fire-and-forget.
    /// </summary>
    private void StartDailyQuestUpload(DailyQuestRegenerateResult result)
    {
        if (_isClosing)
            return;

        CancellationTokenSource newSrc = new CancellationTokenSource();
        if (Interlocked.Exchange(ref _regenerationTokenSource, newSrc) is { } tokenSrc)
        {
            tokenSrc.Cancel();
            tokenSrc.Dispose();
        }

        Task regenTask = Task.Factory.StartNew(async () =>
        {
            // wait for _regenerationTask to be set before continuing and testing _isClosing
            await Task.Yield();

            if (_isClosing)
                return;

            try
            {
                // load the quest assets into the game's asset directory

                CancellationToken token = newSrc.Token;
                token.ThrowIfCancellationRequested();

                if (_modEnabled)
                {
                    await ReuploadMod(true, result.Days, token);
                }

                _config.Days = result.Days;
                _config.HasData = true;
                _config.Write();

                _isAwaitingDailyQuestRenewal = false;

                await UniTask.SwitchToMainThread(token);

                LoadAssets(GetModFolder());

                int oldIndex = _index;
                if (TickRegenerate())
                {
                    DailyQuestRegenerateResult result = await RegenerateDailyQuests();
                    if (result.Days != null)
                        StartDailyQuestUpload(result);
                }
                else
                {
                    if (oldIndex != _index && oldIndex is >= 0 and < DayLength && _index is >= 0 and < DayLength)
                    {
                        _logger.LogInformation("Daily quest day changed from {0} to {1} after quest upload.", oldIndex, _index);
                        RemoveTrackers(save: true);
                        SetupQuestDayForAllPlayers();
                    }

                    UpdateTimers();
                }
            }
            catch (OperationCanceledException) when (newSrc.IsCancellationRequested)
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily quest files.");
            }
        }, TaskCreationOptions.LongRunning);

        _regenerationTask = regenTask.ContinueWith(t =>
        {
            Interlocked.CompareExchange(ref _regenerationTask, null, t);
        }, CancellationToken.None);
    }

    private AssetOrigin CreateAssetOrigin()
    {
        AssetOrigin dailyQuestsModOrigin = new AssetOrigin { name = "Daily Quests", workshopFileId = _workshopId };

        // sets 'AssetOrigin.shouldAssetsOverrideExistingIds' to 'true' which will make assets override each other instead of logging overlapping ID errors
        FieldInfo? shouldAssetsOverrideExistingIdsField = typeof(AssetOrigin).GetField("shouldAssetsOverrideExistingIds", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (shouldAssetsOverrideExistingIdsField == null)
        {
            _logger.LogError("Unable to locate field {0}.", new FieldDefinition("shouldAssetsOverrideExistingIds").DeclaredIn<AssetOrigin>(isStatic: false).WithFieldType<bool>());
        }
        else
        {
            shouldAssetsOverrideExistingIdsField.SetValue(dailyQuestsModOrigin, true);
        }

        return dailyQuestsModOrigin;
    }

    /// <summary>
    /// Loads the <see cref="QuestAsset"/>s into the game's asset directory
    /// </summary>
    private void LoadAssets(string folder)
    {
        _logger.LogInformation("Loading assets from \"{0}\"...", folder);
        if (!Directory.Exists(folder))
        {
            _logger.LogWarning("Directory doesn't exist!");
        }
        else
        {
            AssetOrigin origin = CreateAssetOrigin();

            string parent = Path.Combine(folder, "NPCs", "Quests");
            for (int i = 0; i < DayLength; ++i)
            {
                string name = "DailyQuest" + i.ToString("D2", CultureInfo.InvariantCulture);
                string path = Path.Combine(parent, name, name + ".dat");
                List<string>? errors = AssetUtility.LoadAsset(path, origin);
                if (errors == null)
                    continue;

                foreach (string error in errors)
                {
                    _logger.LogError($"Error loading asset {name}: \"{error}\".");
                }
            }

            AssetUtility.SyncAssetsFromOrigin(origin);
            _logger.LogInformation("Daily Quest assets loaded.");

            #if DEBUG
            using IDisposable? disp = _logger.BeginScope("Assets");
            foreach (Asset asset in origin.GetAssets())
            {
                _logger.LogDebug("Asset: {0}.", AssetLink.Create(asset));
            }
            #endif
        }
    }

    /// <summary>
    /// Determines what day should be the active one and returns <see langword="true"/> if quests need to be generated.
    /// </summary>
    /// <returns></returns>
    private bool TickRegenerate()
    {
        int oldIndex = _index;
        _index = -1;

        // already generating
        if (_isAwaitingDailyQuestRenewal)
            return false;

        DateTime now = DateTime.UtcNow;

        // incorrect data, regenerate now
        if (!_config.HasData || _config.Days == null || _config.Days.Any(x => x?.Presets == null || x.Presets.Any(y => y == null)))
        {
            NextDailyQuestRegenerate = now;
            NextDailyQuestDay = DateTime.MaxValue;
            return true;
        }

        // determine when the next day is and pick the index before that
        for (int i = 0; i < _config.Days!.Length; ++i)
        {
            if (_config.Days[i]!.StartTime > now.AddSeconds(3d))
                break;

            _index = i;
        }

        _warningStep = WarningTimes.Length;

        // server has been down for so long the file got out of date, regenerate now
        if (_index is < 0 or >= DayLength - 1)
        {
            NextDailyQuestRegenerate = now;
            NextDailyQuestDay = DateTime.MaxValue;
            _logger.LogInformation("Daily quests all expired. More quests will be generated.");
            _index = -1;
            return true;
        }

        // setup next day time, 1 day after current day's start time
        NextDailyQuestDay = _index + 1 >= _config.Days.Length ? _config.Days[_index]!.StartTime.Add(DaySimulatedLength) : _config.Days[_index + 1]!.StartTime;
        if (_index >= DaySectionLength)
        {
            // regenerate now if index is past the 7th day (but before the end of the two week period)
            NextDailyQuestRegenerate = now;
        }
        else
        {
            // regenerate at the end of the 7th day
            NextDailyQuestRegenerate = _config.Days[DaySectionLength]!.StartTime;
        }

        if (_index != oldIndex && oldIndex >= 0)
        {
            // chat broadcast for new quests
            _chatService.Broadcast(_translations.DailyQuestNextDay);
        }

        _logger.LogInformation("Daily quests advanced. Next regeneration at {0} UTC. Next day starts at {1} UTC. Current day is index {2}.", NextDailyQuestRegenerate, NextDailyQuestDay, _index);
        return _index >= DaySectionLength;
    }

    private bool UpdateTimers()
    {
        if (_isClosing)
        {
            if (_tickTimer != null)
            {
                _tickTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _tickTimer.Dispose();
                _tickTimer = null;
            }

            return false;
        }

        DateTime now = DateTime.UtcNow;
        DateTime nowBuffer = now.AddSeconds(3d);

        DateTime nextWarning;

        // determine what happens next; show a warning that quests will regenerate soon, or regenerate the quests
        int warningStep = _warningStep;
        if (warningStep > 0)
        {
            // move to next warning. keep going if the next warning already happened
            do
            {
                --warningStep;
                nextWarning = NextDailyQuestDay.Subtract(DaySimulatedLength * WarningTimes[warningStep]);
            }
            while (warningStep > 0 && nextWarning < nowBuffer);
        }
        else
        {
            nextWarning = NextDailyQuestDay;
        }

        if (warningStep <= 0)
        {
            if (now >= NextDailyQuestDay)
            {
                return MoveToNextDay();
            }

            nextWarning = nextWarning.AddSeconds(1d);
            _logger.LogDebug("Daily quests regenerate in {0}. Warning step: {1}.", FormattingUtility.ToTimeString(nextWarning - now), warningStep);
        }
        else
        {
            _logger.LogDebug("Daily quests warning in {0}. Warning step: {1}.", FormattingUtility.ToTimeString(nextWarning - now), warningStep);
        }

        _warningStep = warningStep;

        // start a timer that will invoke after the next warning (or when its time for the next day if the last warning has already passed)
        if (_tickTimer == null)
        {
            _tickTimer = new Timer(TimerTicked, null, nextWarning - now, Timeout.InfiniteTimeSpan);
        }
        else
        {
            _tickTimer.Change(nextWarning - now, Timeout.InfiniteTimeSpan);
        }

        return true;
    }

    private void TimerTicked(object state)
    {
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            if (_warningStep > 0)
            {                                        // round time left to the nearest 5 seconds
                TimeSpan regenIn = TimeSpan.FromSeconds((int)Math.Round((NextDailyQuestDay - DateTime.UtcNow).TotalSeconds * 5) / 5);
                _chatService.Broadcast(_translations.DailyQuestNextDayWarning, regenIn);
                _logger.LogDebug("Daily quests regenerating in {0}. Warning step: {1}.", FormattingUtility.ToTimeString(regenIn), _warningStep);
            }
            else
            {
                _logger.LogDebug("Daily quests should be regenerating, warning step is {0}.", _warningStep);
            }

            UpdateTimers();
        });
    }

    /// <summary>
    /// Remove active trackers from the current daily quests. Optionally save them (this would be used on layout end).
    /// </summary>
    private void RemoveTrackers(bool save)
    {
        List<QuestTracker> trackersToRemove = _questService.ActiveTrackers.Where(x => x.IsDailyQuestTracker).ToList();
        if (save && _config.Days != null && _index >= 0 && _index < _config.Days.Length && _config.Days[_index] is { } today)
        {
            // save trackers for all players
            HashSet<ulong> players = new HashSet<ulong>();
            foreach (QuestTracker tracker in trackersToRemove)
            {
                if (tracker.Preset is not DailyQuestPreset p || p.Day != today || !players.Add(tracker.Player.Steam64.m_SteamID))
                    continue;

                SaveTrackers(tracker.Player, p.Day);
            }
        }

        foreach (QuestTracker dailyQuestTracker in trackersToRemove)
        {
            _questService.RemoveTracker(dailyQuestTracker);
        }
    }

    private string GetDailyQuestSavePath(CSteamID steam64)
    {
        return ConfigurationHelper.GetPlayerFilePath(steam64, "Daily Quests.json");
    }

    private bool MoveToNextDay()
    {
        _warningStep = WarningTimes.Length;
        _logger.LogInformation("Moving daily quests to next day ({0}).", _index + 1);
        RemoveTrackers(save: false);
        if (TickRegenerate())
        {
            UniTask.Create(async () =>
            {
                try
                {
                    DailyQuestRegenerateResult result = await RegenerateDailyQuests();
                    if (result.Days != null)
                        StartDailyQuestUpload(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Regeneration error.");
                }
            });

            if (_index is < 0 or >= DayLength)
                return false;
        }

        SetupQuestDayForAllPlayers();
        UpdateTimers();
        _logger.LogInformation("Moved daily quests to next day ({0}).", _index);
        if (_index is >= 0 and < DayLength && _config.Days != null && _config.Days.Length > _index && _config.Days[_index] is { Presets: not null } day && day.Presets.All(x => x != null))
        {
            DailyQuestsUpdated questUpdated = new DailyQuestsUpdated
            {
                Day = day,
                Days = _config.Days,
                Service = this
            };

            _ = _eventDispatcher.DispatchEventAsync(questUpdated, CancellationToken.None);
        }
        return false;
    }

    private void SetupQuestDayForAllPlayers()
    {
        DailyQuestDay day = _config.Days![_index]!;

        bool log = true;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            SetupQuestDayForPlayer(day, player, log);
            log = false;
        }
    }

    private void SetupQuestDayForPlayer(DailyQuestDay day, WarfarePlayer player, bool log)
    {
        lock (_config)
        {
            bool hasSave = false;
            string saveFile = GetDailyQuestSavePath(player.Steam64);
            Utf8JsonReader reader = default;
            try
            {
                if (File.Exists(saveFile))
                {
                    // read player save file up to the 'Presets' array if 'Asset' matches today's day
                    hasSave = true;
                    JsonUtility.ReadFileSkipBOM(saveFile, out reader, ConfigurationSettings.JsonReaderOptions);
                    Guid asset = default;
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject
                                       || !reader.Read() || reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not "Asset"
                                       || !reader.Read() || reader.TokenType != JsonTokenType.String || !Guid.TryParse(reader.GetString(), out asset) || asset != day.Asset
                                       || !reader.Read() || reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not "Presets"
                                       || !reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                    {
                        if (asset != Guid.Empty)
                        {
                            _logger.LogConditional("Expired save file {0} for day {1} for player {2}.", saveFile, day.Asset, player);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to parse save file {0} for day {1} for player {2}.", saveFile, day.Asset, player);
                        }
                        hasSave = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading trackers for day {0} for player {1}.", day.Asset, player);
            }

            for (int i = 0; i < day.Presets!.Length; i++)
            {
                DailyQuestPreset preset = day.Presets![i]!;

                /*
                 * Player save will look like one of the following:
                 *
                 * {
                 *  // always says true, tells us not to create a QuestTracker since its already done
                 *  "IsCompleted": true
                 * }
                 *  OR
                 * {
                 *  // Gets loaded into a QuestTracker
                 *  "Kills": 5
                 * }
                 */

                if (hasSave)
                {
                    // check for IsCompleted
                    Utf8JsonReader reader2 = reader;
                    if (JsonUtility.SkipToProperty(ref reader2, "IsCompleted") && reader2.TokenType == JsonTokenType.True)
                    {
                        ushort flagId = preset.Flag;
                        short flagValue = (short)preset.State.FlagValue.GetSingleValue();
                        if (flagId != 0 && (!player.UnturnedPlayer.quests.getFlag(flagId, out short currentValue) || currentValue != flagValue))
                        {
                            _logger.LogConditional("Flag updated because complete: {0} {1} -> {2}.", flagId, currentValue, flagValue);
                            player.UnturnedPlayer.quests.setFlag(flagId, flagValue);
                        }

                        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                        {
                            _logger.LogWarning("Failed to parse save file {0} for day {1} for player {2} on preset {3}.", saveFile, day.Asset, player, i);
                            hasSave = false;
                        }
                        else
                        {
                            JsonUtility.ReadTopLevelProperties(ref reader, null);
                        }

                        _logger.LogConditional("Daily quest completed: day {0} preset {1} for player {2}.", day.Asset, i, player);
                        continue;
                    }
                }

                QuestTemplate? template = _questService.Templates.FirstOrDefault(x => x.Name.Equals(preset.TemplateName, StringComparison.Ordinal));
                if (template == null)
                {
                    if (log)
                        _logger.LogWarning("Template not found: {0}.", preset.TemplateName);
                    continue;
                }

                QuestTracker tracker = template.CreateTracker(preset, player);

                if (hasSave)
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                    {
                        _logger.LogWarning("Failed to parse save file {0} for day {1} for player {2} on preset {3}.", saveFile, day.Asset, player, i);
                        hasSave = false;
                    }
                    else
                    {
                        // read tracker progress properties. Ex. { "Kills": 4 }.
                        tracker.ReadProgress(ref reader);
                    }

                    tracker.InvokeUpdate();
                }

                tracker.Updated += TrackerOnUpdated;
            }
            
            // re-save the file
            SaveTrackers(player, day);
        }
    }

    private void TrackerOnUpdated(QuestTracker tracker)
    {
        if (tracker.Preset is DailyQuestPreset { Day: { } day })
            SaveTrackers(tracker.Player, day);
    }

    private void SaveTrackers(WarfarePlayer player, DailyQuestDay day)
    {
        try
        {
            lock (_config)
            {
                SaveTrackersIntl(player, day);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving trackers for day {0} for player {1}.", day.Asset, player);
        }
    }

    private void SaveTrackersIntl(WarfarePlayer player, DailyQuestDay day)
    {
        string path = GetDailyQuestSavePath(player.Steam64);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 1024, FileOptions.SequentialScan);
#if DEBUG
        using Utf8JsonWriter writer = new Utf8JsonWriter(fs, ConfigurationSettings.JsonWriterOptions);
#else
        using Utf8JsonWriter writer = new Utf8JsonWriter(fs, ConfigurationSettings.JsonCondensedWriterOptions);
#endif

        writer.WriteStartObject();

        writer.WriteString("Asset", day.Asset);
        writer.WritePropertyName("Presets");
        writer.WriteStartArray();

        if (day.Presets != null && day.Presets.All(x => x != null))
        {
            foreach (DailyQuestPreset? preset in day.Presets!)
            {
                if (preset == null)
                    continue;

                QuestTracker? tracker = _questService.ActiveTrackers.FirstOrDefault(x =>
                    x.Player.Equals(player)
                    && x.Preset is DailyQuestPreset p
                    && p.Key == preset.Key);

                writer.WriteStartObject();
                if (tracker == null || tracker.IsComplete)
                {
                    // quest is completed
                    writer.WriteBoolean("IsCompleted", true);
                }
                else
                {
#if DEBUG
                    writer.WriteCommentValue($"Quest : {tracker.Quest.Name}");
                    writer.WriteCommentValue($"Type  : {tracker.Quest.Type.Name}");
                    writer.WriteCommentValue($"Value : {preset.State.CreateQuestDescriptiveString()}");
#endif
                    tracker.WriteProgress(writer);
                }
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();

        writer.WriteEndObject();

        writer.Flush();
    }

    [EventListener(MustRunInstantly = true, Priority = -1 /* after QuestService removes pre-existing quests that were present on disconnect */)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (_index is >= 0 and < DayLength && _config.Days != null && _config.Days.Length > _index && _config.Days[_index] is { Presets: not null } day && day.Presets.All(x => x != null))
        {
            SetupQuestDayForPlayer(day, e.Player, false);
        }
    }

    [EventListener(MustRunInstantly = true, Priority = 1 /* before QuestService removes trackers */)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (_index is >= 0 and < DayLength && _config.Days != null && _config.Days.Length > _index && _config.Days[_index] is { Presets: not null } day && day.Presets.All(x => x != null))
        {
            SaveTrackers(e.Player, day);
        }
    }
}

public struct DailyQuestRegenerateResult
{
    public DailyQuestDay[] Days;
}

[EventModel(EventSynchronizationContext.Pure)]
public sealed class DailyQuestsUpdated
{
    public required DailyQuestDay Day { get; init; }
    public required DailyQuestDay?[] Days { get; init; }
    public required DailyQuestService Service { get; init; }
}