using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.Saves;
using Uncreated.Warfare.Players.UI;

namespace Uncreated.Warfare.Players;

[CannotApplyEqualityOperator]
public class WarfarePlayer : IPlayer, ICommandUser, IEquatable<IPlayer>, IEquatable<WarfarePlayer>
{
    private readonly CancellationTokenSource _disconnectTokenSource;
    private readonly ILogger _logger;
    private readonly PlayerNames _playerNameHelper;
    public CSteamID Steam64 { get; }
    public Player UnturnedPlayer { get; }
    public SteamPlayer SteamPlayer { get; }
    public Transform Transform { get; }
    public Team Team { get; private set; }
    public BinaryPlayerSave Save { get; }
    public WarfarePlayerLocale Locale { get; }

    /// <summary>
    /// If the player this object represents is currently online. Set to false *after* the leave event is fired.
    /// </summary>
    public bool IsOnline { get; private set; } = true;
    
    /// <summary>
    /// If the player this object represents is currently in the process of disconnecting.
    /// </summary>
    public bool IsDisconnecting { get; private set; } = true;

    /// <summary>
    /// List of auto-added components.
    /// </summary>
    public IReadOnlyList<IPlayerComponent> Components { get; }

    /// <summary>
    /// Structure including all variations of the player's names.
    /// </summary>
    public ref readonly PlayerNames Names => ref _playerNameHelper;

    /// <summary>
    /// The Steam64 ID of the group the player's in.
    /// </summary>
    public CSteamID GroupId => UnturnedPlayer.quests.groupID;

    public ITransportConnection Connection => SteamPlayer.transportConnection;
    public Vector3 Position => Transform.position;
    public float Yaw => Transform.eulerAngles.y;

    /// <summary>
    /// A <see cref="CancellationToken"/> that cancels after the player leaves.
    /// </summary>
    public CancellationToken DisconnectToken => _disconnectTokenSource.Token;


    public ToastManager Toasts { get; }

    internal WarfarePlayer(Player player, ILogger logger, IReadOnlyList<IPlayerComponent> components)
    {
        _disconnectTokenSource = new CancellationTokenSource();
        _logger = logger;
        _playerNameHelper = new PlayerNames(player);
        UnturnedPlayer = player;
        SteamPlayer = player.channel.owner;
        Steam64 = player.channel.owner.playerID.steamID;
        Transform = player.transform;
        Save = new BinaryPlayerSave(Steam64, _logger);
        Save.Load();

        Locale = new WarfarePlayerLocale(this, /* todo data.LanguagePreferences */ null);

        Components = components;
        Toasts = new ToastManager(this);

        _logger.LogInformation("Player {0} joined the server", this);
    }

    public void UpdateTeam(Team team)
    {
        Team = team;
    }

    public void ApplyOfflineState()
    {
        try
        {
            _disconnectTokenSource.Cancel();
            IsDisconnecting = false;
            IsOnline = false;
        }
        finally
        {
            _disconnectTokenSource.Dispose();
            _logger.LogInformation("Player {0} left the server.", this);
        }
    }
    public void StartDisconnecting()
    {
        IsDisconnecting = true;
    }

    public override string ToString()
    {
        return _playerNameHelper.ToString();
    }

    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags)
    {
        // todo make this a proper implementation later.
        return new OfflinePlayer(in _playerNameHelper).Translate(language, format, target, culture, ref flags);
    }

    public bool Equals(IPlayer other)
    {
        return Steam64.m_SteamID == other.Steam64.m_SteamID;
    }

    public bool Equals(WarfarePlayer other)
    {
        return Steam64.m_SteamID == other.Steam64.m_SteamID;
    }

    public override bool Equals(object? obj)
    {
        return obj is IPlayer player && Steam64.m_SteamID == player.Steam64.m_SteamID;
    }

    public override int GetHashCode()
    {
        return Steam64.m_SteamID.GetHashCode();
    }

    bool ICommandUser.IsSuperUser => false;
    void ICommandUser.SendMessage(string message)
    {
        ChatManager.serverSendMessage(message, Palette.AMBIENT, null, SteamPlayer, EChatMode.SAY, useRichTextFormatting: true);
    }
}