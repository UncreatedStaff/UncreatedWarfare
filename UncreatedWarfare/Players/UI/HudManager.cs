using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.UI;

/// <summary>
/// Allows hiding and re-showing the entire HUD when needed, tracking overlapping requests if needed.
/// </summary>
/// <remarks>Also contains the logic for tracking plugin voting.</remarks>
public sealed class HudManager : IEventListener<PlayerLeft>, IDisposable
{
    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;
    private readonly ILogger<HudManager> _logger;
    private readonly ChatService _chatService;
    private readonly PlayerReplicatedConfigManager _replicatedConfigManager;

    private readonly PlayerFeatureCounter<HudPlayerComponent> _hideHudCounter;
    private readonly PlayerFeatureCounter<HudPlayerComponent> _blockChatCounter;
    private readonly PlayerFeatureCounter<HudPlayerComponent> _hideCompassCounter;

    /// <summary>
    /// Invoked when a player's plugin voting status changes.
    /// </summary>
    /// <remarks>Used by the Duty UI to lower it when a vote is happening.</remarks>
    public event Action<WarfarePlayer, bool>? OnPluginVotingUpdated;

    public HudManager(
        WarfareModule module,
        IPlayerService playerService,
        ILogger<HudManager> logger,
        ChatService chatService,
        PlayerReplicatedConfigManager replicatedConfigManager)
    {
        _module = module;
        _playerService = playerService;
        _logger = logger;
        _chatService = chatService;
        _replicatedConfigManager = replicatedConfigManager;
        _chatService.OnSendingChatMessage += SendingChatMessage;

        _hideHudCounter = new PlayerFeatureCounter<HudPlayerComponent>(
            _playerService,
            (c, i) => c.HandleCount += i,
            ClearHud,
            ShowHud
        );

        _blockChatCounter = new PlayerFeatureCounter<HudPlayerComponent>(
            _playerService,
            (c, i) => c.ChatBlockHandleCount += i,
            DoBlockChat,
            DoUnblockChat
        );

        _hideCompassCounter = new PlayerFeatureCounter<HudPlayerComponent>(
            _playerService,
            (c, i) => c.DisableCompassHandleCount += i,
            DoDisableCompass,
            DoRestoreCompass
        );
    }

    /// <summary>
    /// Whether or not HUD elements are hidden for all players. Note that some may be hidden for only specific players.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsHiddenForAllPlayers => _hideHudCounter.AppliedGlobally;

    /// <summary>
    /// Whether or not HUD elements are hidden for at least one online player (or all players).
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsHiddenForAnyPlayers => _hideHudCounter.AppliedToAnyPlayers;
    
    /// <summary>
    /// Whether or not HUD elements are hidden for the given player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsHidden(WarfarePlayer player)
    {
        return !player.IsOnline || _hideHudCounter.HasFeature(player);
    }

    /// <summary>
    /// Sets whether or not a player is currently voting with a plugin-controlled vote HUD.
    /// </summary>
    public void SetIsPluginVoting(WarfarePlayer player, bool value)
    {
        HudPlayerComponent comp = player.Component<HudPlayerComponent>();
        if (comp.IsPluginVoting == value)
            return;

        comp.IsPluginVoting = value;
        try
        {
            OnPluginVotingUpdated?.Invoke(player, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPluginVotingUpdated.");
        }
    }

    /// <summary>
    /// Sets whether or not all players are currently voting with a plugin-controlled vote HUD.
    /// </summary>
    public void SetAllIsPluginVoting(bool value)
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            SetIsPluginVoting(player, value);
        }
    }

    /// <summary>
    /// Gets whether or not a player is currently voting with a plugin-controlled vote HUD.
    /// </summary>
    public bool GetIsPluginVoting(WarfarePlayer player)
    {
        return player.Component<HudPlayerComponent>().IsPluginVoting;
    }

    /// <summary>
    /// Hides the HUD for all players until the returned <see cref="IDisposable"/> is disposed. This also hides the compass.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public IDisposable HideHud()
    {
        return new HudHandle(this, null);
    }

    /// <summary>
    /// Hides the HUD for <paramref name="player"/> until the returned <see cref="IDisposable"/> is disposed. This also hides the compass.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public IDisposable HideHud(WarfarePlayer player)
    {
        return new HudHandle(this, player);
    }

    /// <summary>
    /// Prevents new messages from appearing in chat until the returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public IDisposable BlockChat(WarfarePlayer player)
    {
        return new BlockChatHandle(this, player);
    }

    /// <summary>
    /// Hides the compass for all players until the returned <see cref="IDisposable"/> is disposed.
    /// <see cref="HideHud()"/> also hides the compass, so no need to call both.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public IDisposable HideCompass()
    {
        return new DisableCompassHandle(this, null);
    }

    /// <summary>
    /// Hides the compass for <paramref name="player"/> until the returned <see cref="IDisposable"/> is disposed.
    /// <see cref="HideHud(WarfarePlayer)"/> also hides the compass, so no need to call both.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public IDisposable HideCompass(WarfarePlayer player)
    {
        return new DisableCompassHandle(this, player);
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        _hideHudCounter.NotifyPlayerLeft(e.Player);
        _blockChatCounter.NotifyPlayerLeft(e.Player);
        _hideCompassCounter.NotifyPlayerLeft(e.Player);
    }

    private void ClearHud(HudPlayerComponent? comp)
    {
        _logger.LogConditional($"Clearing HUD for {comp?.Player}.");
        try
        {
            ILifetimeScope scope = _module.ScopedProvider;

            foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
            {
                try
                {
                    listener.Hide(comp?.Player);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error hiding HUD: {listener.GetType()}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving HUD listeners.");
        }
        finally
        {
            if (comp != null)
            {
                TrySetVanillaHud(comp.Player, false);
            }
            else foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                TrySetVanillaHud(player, false);
            }
        }
    }

    private void ShowHud(HudPlayerComponent? comp)
    {
        _logger.LogConditional($"Restoring HUD for {comp?.Player}.");
        try
        {
            ILifetimeScope scope = _module.ScopedProvider;

            foreach (IHudUIListener listener in scope.Resolve<IEnumerable<IHudUIListener>>())
            {
                if (comp is { HandleCount: > 0 })
                    continue;

                try
                {
                    listener.Restore(comp?.Player);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error showing HUD: {listener.GetType()}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving HUD listeners.");
        }
        finally
        {
            if (comp != null)
            {
                TrySetVanillaHud(comp.Player, true);
            }
            else foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (player.Component<HudPlayerComponent>() is { HandleCount: > 0 })
                    continue;

                TrySetVanillaHud(player, true);
            }
        }
    }

    private void TrySetVanillaHud(WarfarePlayer player, bool isVisible)
    {
        try
        {
            if (isVisible)
                player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
            else
                player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error {(isVisible ? "showing" : "hiding")} PlayerLife HUD for player: {player}.");
        }
    }

    private bool _isSendingChats;

    private void DoBlockChat(HudPlayerComponent? player)
    {
        if (player == null)
        {
            _logger.LogConditional("Blocking chat for all players:");
            foreach (WarfarePlayer p in _playerService.OnlinePlayers)
            {
                DoBlockChat(p.Component<HudPlayerComponent>());
            }

            return;
        }

        _logger.LogConditional($"Blocking chat for {player.Player}.");
        player.BlockedChatMessages = [];
    }

    private void DoUnblockChat(HudPlayerComponent? player)
    {
        if (player == null)
        {
            _logger.LogConditional("Restoring chat for all players:");
            foreach (WarfarePlayer p in _playerService.OnlinePlayers)
            {
                DoUnblockChat(p.Component<HudPlayerComponent>());
            }

            return;
        }

        List<BlockedChatMessage>? chatMessages = player.BlockedChatMessages;
        if (chatMessages == null)
            return;

        _logger.LogConditional($"Restoring chat for {player.Player}.");
        player.BlockedChatMessages = null;
        _isSendingChats = true;
        try
        {
            foreach (BlockedChatMessage msg in chatMessages)
            {
                _chatService.Send(
                    player.Player,
                    msg.Text,
                    msg.Color,
                    msg.Mode,
                    msg.Icon,
                    msg.AllowRichText,
                    msg.FromPlayer
                );
            }
        }
        finally
        {
            _isSendingChats = false;
        }
    }

    private void SendingChatMessage(WarfarePlayer recipient, string text, Color color, EChatMode mode, string? iconURL, bool richText, WarfarePlayer? fromPlayer, ref bool shouldReplicate)
    {
        if (_isSendingChats)
            return;

        HudPlayerComponent component = recipient.Component<HudPlayerComponent>();
        if (component.ChatBlockHandleCount <= 0 || component.BlockedChatMessages == null)
            return;

        component.BlockedChatMessages.Add(new BlockedChatMessage
        {
            Text = text,
            Color = color,
            Mode = mode,
            Icon = iconURL,
            AllowRichText = richText,
            FromPlayer = fromPlayer
        });

        shouldReplicate = false;
    }

    private IDisposable? _globalCompassHandle;

    private void DoDisableCompass(HudPlayerComponent? player)
    {
        if (!Provider.modeConfigData.Gameplay.Compass)
        {
            _logger.LogConditional($"Skipping compass disable for {player?.Player}.");
            return;
        }

        _logger.LogConditional($"Disabling compass for {player?.Player}.");
        ModifyModeConfig apply = static config => config.Gameplay.Compass = false;
        ModifyModeConfig undo = static config => config.Gameplay.Compass = true;

        if (player == null)
        {
            _globalCompassHandle = _replicatedConfigManager.ReplicateGlobalConfigChange(apply, undo);
        }
        else
        {
            player.PlayerDisableCompassHandle = player.Player.ReplicateConfigChange(apply, undo);
        }
    }

    private void DoRestoreCompass(HudPlayerComponent? player)
    {
        if (!Provider.modeConfigData.Gameplay.Compass)
        {
            _logger.LogConditional($"Skipping compass restore for {player?.Player}.");
            return;
        }

        _logger.LogConditional($"Restoring compass for {player?.Player}.");
        if (player == null)
        {
            Interlocked.Exchange(ref _globalCompassHandle, null)?.Dispose();
        }
        else
        {
            Interlocked.Exchange(ref player.PlayerDisableCompassHandle, null)?.Dispose();
        }
    }

    public void Dispose()
    {
        _chatService.OnSendingChatMessage -= SendingChatMessage;
        Interlocked.Exchange(ref _globalCompassHandle, null)?.Dispose();
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    [PlayerComponent]
    private sealed class HudPlayerComponent : IPlayerComponent
    {
        // note: may look unused but used through OnPluginVotingUpdated event
        public bool IsPluginVoting;

        /// <summary>
        /// Incremented when the HUD needs to be hidden for this player, then decremented when that request gets disposed.
        /// </summary>
        public int HandleCount;

        public int ChatBlockHandleCount;
        public int DisableCompassHandleCount;
        public IDisposable? PlayerDisableCompassHandle;

        public List<BlockedChatMessage>? BlockedChatMessages;

        /// <inheritdoc />
        public required WarfarePlayer Player { get; init; }

        /// <inheritdoc />
        public void Init(IServiceProvider serviceProvider, bool isOnJoin)
        {
            if (!isOnJoin)
                return;

            IsPluginVoting = false;

            HudManager hudManager = serviceProvider.GetRequiredService<HudManager>();
            if (hudManager._blockChatCounter.AppliedGlobally)
            {
                hudManager.DoBlockChat(this);
            }
        }
    }

    private class HudHandle : IDisposable
    {
        private HudManager? _manager;
        private readonly HudPlayerComponent? _playerComponent;

        public HudHandle(HudManager manager, WarfarePlayer? player)
        {
            _manager = manager;
            _playerComponent = player?.Component<HudPlayerComponent>();
            _manager._hideHudCounter.Increment(_playerComponent);
            _manager._hideCompassCounter.Increment(_playerComponent);
        }

        public void Dispose()
        {
            HudManager? manager = Interlocked.Exchange(ref _manager, null);
            if (manager == null)
                return;

            manager._hideHudCounter.Decrement(_playerComponent);
            manager._hideCompassCounter.Decrement(_playerComponent);
        }
    }

    private class BlockChatHandle : IDisposable
    {
        private HudManager? _manager;
        private readonly HudPlayerComponent? _playerComponent;

        public BlockChatHandle(HudManager manager, WarfarePlayer? player)
        {
            _manager = manager;
            _playerComponent = player?.Component<HudPlayerComponent>();
            _manager._blockChatCounter.Increment(_playerComponent);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _manager, null)?._blockChatCounter.Decrement(_playerComponent);
        }
    }

    private sealed class BlockedChatMessage
    {
        public required string Text;
        public required Color Color;
        public required EChatMode Mode;
        public required string? Icon;
        public required bool AllowRichText;
        public required WarfarePlayer? FromPlayer;
    }

    private class DisableCompassHandle : IDisposable
    {
        private HudManager? _manager;
        private readonly HudPlayerComponent? _playerComponent;

        public DisableCompassHandle(HudManager manager, WarfarePlayer? player)
        {
            _manager = manager;
            _playerComponent = player?.Component<HudPlayerComponent>();
            _manager._hideCompassCounter.Increment(_playerComponent);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _manager, null)?._hideCompassCounter.Decrement(_playerComponent);
        }
    }
}