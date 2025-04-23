using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests;

public class QuestService : ILayoutHostedService, IEventListenerProvider, IDisposable, IEventListener<PlayerLeft>, IEventListener<PlayerJoined>
{
    private readonly List<QuestTracker> _activeTrackers = new List<QuestTracker>(256);
    private IConfigurationRoot? _questConfiguration;
    private IDisposable? _reloadListener;
    private List<QuestTemplate>? _templates;

    private readonly WarfareModule _module;
    private readonly ILogger<QuestService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ChatService _chatService;
    private readonly QuestTranslations _translations;
    private readonly SemaphoreSlim _rewardsSemaphore = new SemaphoreSlim(1, 1);

    public IReadOnlyList<QuestTemplate> Templates { get; private set; }

    public IReadOnlyList<QuestTracker> ActiveTrackers { get; }

    public QuestService(IServiceProvider serviceProvider, ILogger<QuestService> logger)
    {
        _logger = logger;
        _translations = serviceProvider.GetRequiredService<TranslationInjection<QuestTranslations>>().Value;
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _serviceProvider = serviceProvider;
        Templates = Array.Empty<QuestTemplate>();

        ActiveTrackers = new ReadOnlyCollection<QuestTracker>(_activeTrackers);
    }

    async UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        string dir = Path.Combine(_module.HomeDirectory, "Quests");
        Directory.CreateDirectory(dir);

        if (_questConfiguration is IDisposable disp)
            disp.Dispose();

        _questConfiguration = new ConfigurationBuilder()
            .AddYamlFile(Path.Combine(dir, "Quest Types.yml"), optional: true, reloadOnChange: true)
            .Build();

        await ReloadQuestConfiguration(_questConfiguration, true, token);

        _reloadListener?.Dispose();
        _reloadListener = ChangeToken.OnChange(
            _questConfiguration.GetReloadToken,
            OnConfigurationUpdated,
            _questConfiguration
        );
    }

    private void OnConfigurationUpdated(object? state)
    {
        if (state is not IConfiguration conf)
            return;

        _ = ReloadQuestConfiguration(conf, false, CancellationToken.None);
    }

    private async UniTask ReloadQuestConfiguration(IConfiguration config, bool isStartup, CancellationToken token)
    {
        await UniTask.SwitchToMainThread();

        List<string> found = new List<string>();

        List<QuestTemplate> newTemplates = _templates?.ToList() ?? new List<QuestTemplate>(32);
        foreach (IConfigurationSection questInfo in config.GetSection("Quests").GetChildren())
        {
            string? typeName = questInfo["Type"];
            if (typeName == null)
            {
                _logger.LogWarning("Configuration error: 'Type' invalid at index {0}.", found.Count);
                continue;
            }

            string? name = questInfo["Name"];
            if (name == null)
            {
                _logger.LogWarning("Configuration error: 'Name' invalid at index {0}.", found.Count);
                continue;
            }

            Type? type = ContextualTypeResolver.ResolveType(typeName, typeof(QuestTemplate));
            if (type == null || type.IsAbstract)
            {
                _logger.LogWarning("Configuration error: 'Type' not found: {0} at index {1}.", typeName, found.Count);
                continue;
            }

            if (found.Contains(name))
            {
                _logger.LogWarning("Configuration error: duplicate 'Name' at index {0}.", found.Count);
                continue;
            }

            found.Add(name);

            int existingIndex = newTemplates.FindIndex(x => x.Name.Equals(name, StringComparison.Ordinal));
            QuestTemplate? existing = existingIndex >= 0 ? newTemplates[existingIndex] : null;
            if (existing != null && !type.IsInstanceOfType(existing))
            {
                existing = null;
                newTemplates.RemoveAt(existingIndex);
                _logger.LogDebug("Type updated at runtime for template {0}, may cause issues.", name);
            }

            if (existing == null)
            {
                if (!isStartup)
                    _logger.LogDebug("Quest template added: {0}.", name);

                QuestTemplate template = (QuestTemplate)Activator.CreateInstance(type, questInfo, _serviceProvider);
                await template.InitializeAsync(token);

                newTemplates.Add(template);
            }
            else
            {
                await existing.UpdateAsync(questInfo, token);
            }
        }

        if (!isStartup)
        {
            // process removed templates
            for (int i = newTemplates.Count - 1; i >= 0; i--)
            {
                QuestTemplate template = newTemplates[i];
                if (found.Contains(template.Name, StringComparer.Ordinal))
                    continue;

                _logger.LogDebug("Quest template removed: {0}.", template.Name);
                newTemplates.RemoveAt(i);
            }

            _logger.LogDebug("Updated quest templates: {0} ct.", newTemplates.Count);
        }
        else
        {
            _logger.LogDebug("Loaded quest templates: {0} ct.", newTemplates.Count);
        }


        _templates = newTemplates;
        Templates = new ReadOnlyCollection<QuestTemplate>(newTemplates);
    }

    async UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        await _rewardsSemaphore.WaitAsync(20000, token);

        for (int i = _activeTrackers.Count - 1; i >= 0; --i)
        {
            RemoveTracker(_activeTrackers[i]);
        }

        _reloadListener?.Dispose();
        _reloadListener = null;

        if (_questConfiguration is IDisposable disp)
            disp.Dispose();

        _questConfiguration = null;
        _rewardsSemaphore.Dispose();
    }

    void IDisposable.Dispose()
    {
        _reloadListener?.Dispose();
        _reloadListener = null;
        if (_questConfiguration is IDisposable disp)
            disp.Dispose();

        _questConfiguration = null;
    }

    public void HandleTrackerUpdated(QuestTracker tracker)
    {
        UpdateQuestFlag(tracker, false);

        if (!tracker.Player.IsConnecting)
        {
            string desc = tracker.CreateDescriptiveStringForPlayer();
            
            // remove period from end of message (it looks weird being a different color)
            if (desc.EndsWith('.'))
                desc = desc[..^1];

            _chatService.Send(tracker.Player, tracker.IsComplete ? _translations.QuestCompleted : _translations.QuestUpdated, desc);
        }
    }

    public void HandleTrackerCompleted(QuestTracker tracker)
    {
        UpdateQuestFlag(tracker, false);

        GiveRewards(tracker);

        RemoveTracker(tracker);
    }

    private void GiveRewards(QuestTracker tracker, CancellationToken token = default)
    {
        WarfarePlayer player = tracker.Player;
        IReadOnlyList<IQuestReward> rewards = tracker.Rewards;

        UniTask.Create(async () =>
        {
            // apply all rewards at once. the semaphore is so the round doesn't end mid-way through rewards
            await _rewardsSemaphore.WaitAsync(20000, token);
            try
            {
                await UniTask.SwitchToMainThread(token);
                UniTask[] tasks = new UniTask[rewards.Count];
                int index = 0;
                foreach (IQuestReward reward in rewards)
                {
                    tasks[index] = reward.GrantRewardAsync(player, tracker, _serviceProvider, token);
                    ++index;
                }

                try
                {
                    await UniTask.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying reward(s) for quest {0} for player {1}.", tracker.Quest.Name, player);
                    for (int i = 0; i < tasks.Length; ++i)
                    {
                        if (tasks[i].AsTask().IsCompleted)
                            continue;

                        _logger.LogError(" - {0}: {1}.", i, tracker.Rewards[i]);
                    }
                }
            }
            finally
            {
                _rewardsSemaphore.Release();
            }
        });
    }

    public bool RemoveTracker(QuestTracker tracker)
    {
        GameThread.AssertCurrent();

        if (!_activeTrackers.RemoveFast(tracker))
            return false;

        if (tracker is IDisposable disp)
            disp.Dispose();

        WarfarePlayer player = tracker.Player;

        bool remove = false;
        if (player is { IsOnline: true, IsDisconnecting: false }
            && tracker.Preset is IAssetQuestPreset preset
            && Assets.find<QuestAsset>(preset.Asset) is { } questAsset
            && player.UnturnedPlayer.quests.questsList.Any(x => x.asset != null && x.asset.GUID == questAsset.GUID)
            && !_activeTrackers.Exists(x => x.Player.Equals(player) && x.Preset is IAssetQuestPreset preset && preset.Asset == questAsset.GUID))
        {
            remove = true;
            player.UnturnedPlayer.quests.ServerRemoveQuest(questAsset, wasCompleted: tracker.IsComplete);
            if (!player.Save.TrackQuests && player.UnturnedPlayer.quests.GetTrackedQuest() is { } trackedQuest)
                ServerUntrackQuest(player, trackedQuest);
        }

        UpdateQuestFlag(tracker, remove);

        _logger.LogDebug("Tracker removed: {0} for player {1}.", tracker.Quest.Name, tracker.Player);
        return true;
    }

    public void AddTracker(QuestTracker tracker)
    {
        GameThread.AssertCurrent();

        if (_activeTrackers.Contains(tracker))
            return;

        _activeTrackers.Add(tracker);

        UpdateQuestFlag(tracker, false);

        WarfarePlayer player = tracker.Player;

        if (tracker.Preset is IAssetQuestPreset preset
            && Assets.find<QuestAsset>(preset.Asset) is { } questAsset
            && !player.UnturnedPlayer.quests.questsList.Any(x => x.asset != null && x.asset.GUID == questAsset.GUID))
        {
            player.UnturnedPlayer.quests.ServerAddQuest(questAsset);
            _logger.LogConditional("Added quest: {0} for player {1}.", questAsset, tracker.Player);

            if (player.Save.TrackQuests)
                ServerTrackQuest(player, questAsset);
            else
                ServerUntrackQuest(player, questAsset);
        }

        _logger.LogDebug("Tracker added: {0} for player {1}.", tracker.Quest.Name, tracker.Player);
    }

    private void UpdateQuestFlag(QuestTracker tracker, bool isRemoving)
    {
        WarfarePlayer player = tracker.Player;

        PlayerQuests quests = player.UnturnedPlayer.quests;

        if (player is { IsOnline: false, IsConnecting: false } || tracker.Preset is not { Flag: not 0 })
        {
            _logger.LogConditional("Flag not updated: {0} {1} -> {2}. IsRemoving: {3}", tracker.Preset?.Flag, tracker.Preset != null && quests.getFlag(tracker.Preset.Flag, out short v2) ? v2 : 0, tracker.FlagValue, isRemoving);
            return;
        }

        ushort flagId = tracker.Preset.Flag;
        _logger.LogConditional("Flag updated: {0} {1} -> {2}. IsRemoving: {3}", flagId, quests.getFlag(tracker.Preset.Flag, out short v) ? v : 0, tracker.FlagValue, isRemoving);

        if (isRemoving)
        {
            if (quests.getFlag(flagId, out short oldValue))
            {
                _logger.LogConditional("Flag removed: {0} {1}. IsRemoving: {2}", flagId, oldValue, isRemoving);
                quests.sendRemoveFlag(flagId);
            }
        }
        else
        {
            short flagValue = tracker.FlagValue;
            if (!quests.getFlag(flagId, out short oldValue) || oldValue != flagValue)
            {
                _logger.LogConditional("Flag sent: {0} {1} -> {2}. IsRemoving: {3}", flagId, oldValue, tracker.FlagValue, isRemoving);
                quests.sendSetFlag(flagId, flagValue);
            }
        }
    }

    public static void ServerTrackQuest(WarfarePlayer player, QuestAsset quest)
    {
        if (quest == null)
            throw new ArgumentNullException(nameof(quest));

        GameThread.AssertCurrent();
        
        if (player is not { IsOnline: true })
            return;

        PlayerLobbyComponent? lobbyComp = player.ComponentOrNull<PlayerLobbyComponent>();
        if (lobbyComp != null && !lobbyComp.TryTrackQuest(quest))
        {
            ServerUntrackQuest(player, quest);
        }

        QuestAsset? current = player.UnturnedPlayer.quests.GetTrackedQuest();
        if (current != null)
        {
            if (current.GUID != quest.GUID && player.Save.TrackQuests)
            {
                player.UnturnedPlayer.quests.ServerAddQuest(quest);
            }
        }
        else if (player.Save.TrackQuests)
            player.UnturnedPlayer.quests.ServerAddQuest(quest);
    }

    public static void ServerUntrackQuest(WarfarePlayer player, QuestAsset quest)
    {
        if (quest == null)
            throw new ArgumentNullException(nameof(quest));

        GameThread.AssertCurrent();

        if (player is not { IsOnline: true })
            return;

        QuestAsset? current = player.UnturnedPlayer.quests.GetTrackedQuest();
        if (current == null || current.GUID != quest.GUID)
            return;

        player.UnturnedPlayer.quests.ServerAddQuest(quest);
    }

    void IEventListenerProvider.AppendListeners<TEventArgs>(TEventArgs args, List<object> listeners)
    {
        foreach (QuestTracker tracker in _activeTrackers)
        {
            if (tracker is IEventListener<TEventArgs> el)
                listeners.Add(el);
            if (tracker is IAsyncEventListener<TEventArgs> ael)
                listeners.Add(ael);
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        for (int i = _activeTrackers.Count - 1; i >= 0; --i)
        {
            QuestTracker tracker = _activeTrackers[i];
            if (tracker.Player.Equals(e.Player))
            {
                RemoveTracker(tracker);
            }
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        PlayerQuests quests = e.Player.UnturnedPlayer.quests;

        for (int i = quests.questsList.Count - 1; i >= 0; --i)
        {
            QuestAsset? q = quests.questsList[i].asset;
            if (q == null)
                continue;

            quests.ServerRemoveQuest(q, wasCompleted: false);
        }
    }
}