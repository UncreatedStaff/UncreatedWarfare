using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Saves;
using Uncreated.Warfare.Steam.Models;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Abstraction for <see cref="WarfarePlayer"/> and offline players.
/// </summary>
public interface IPlayer : ITranslationArgument
{
    public CSteamID Steam64 { get; }
}

[CannotApplyEqualityOperator]
public class WarfarePlayer : IPlayer, ICommandUser, IEquatable<IPlayer>, IEquatable<WarfarePlayer>, ITransformObject
{
    private readonly CancellationTokenSource _disconnectTokenSource;
    private readonly ILogger _logger;
    private readonly PlayerNames _playerNameHelper;
    private readonly uint _acctId;
    private readonly IPlayerComponent[] _componentsArray;

    public CSteamID Steam64 { get; }
    public Player UnturnedPlayer { get; }
    public SteamPlayer SteamPlayer { get; }
    public Transform Transform { get; }
    public Team Team { get; private set; }
    public BinaryPlayerSave Save { get; }
    public WarfarePlayerLocale Locale { get; }
    public SemaphoreSlim PurchaseSync { get; }
    public PlayerSummary SteamSummary { get; internal set; }

    /// <summary>
    /// List of steam IDs of this player's friends, if theyre public.
    /// </summary>
    /// <remarks>Private profiles will just have an empty array here.</remarks>
    public ulong[] SteamFriends { get; internal set; }

    /// <inheritdoc />
    public Vector3 Position
    {
        get => Transform.position;
        set
        {
            GameThread.AssertCurrent();
            UnturnedPlayer.teleportToLocationUnsafe(value, Transform.eulerAngles.y);
        }
    }

    /// <summary>
    /// If the player this object represents is currently online. Set to <see langword="false"/> *after* the leave event is fired.
    /// </summary>
    public bool IsOnline { get; private set; } = true;

    /// <summary>
    /// If the player this object represents is currently offline. Set to <see langword="true"/> *after* the leave event is fired.
    /// </summary>
    public bool IsDisconnected => !IsOnline;

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

    public float Yaw => Transform.eulerAngles.y;

    /// <summary>
    /// A <see cref="CancellationToken"/> that cancels after the player leaves.
    /// </summary>
    public CancellationToken DisconnectToken => _disconnectTokenSource.Token;
    internal WarfarePlayer(Player player, in PlayerService.PlayerTaskData taskData, ILogger logger, IPlayerComponent[] components, IServiceProvider serviceProvider)
    {
        _disconnectTokenSource = taskData.TokenSource;
        _logger = logger;
        _playerNameHelper = new PlayerNames(player);
        UnturnedPlayer = player;
        SteamPlayer = player.channel.owner;
        Steam64 = player.channel.owner.playerID.steamID;
        _acctId = Steam64.GetAccountID().m_AccountID;
        Transform = player.transform;
        Save = new BinaryPlayerSave(Steam64, _logger);
        Save.Load();

        Locale = new WarfarePlayerLocale(this, new LanguagePreferences { Steam64 = Steam64.m_SteamID }, serviceProvider);

        _componentsArray = components;
        Components = new ReadOnlyCollection<IPlayerComponent>(components);

        Team = Team.NoTeam;
        _logger.LogInformation("Player {0} joined the server", this);

        PurchaseSync = new SemaphoreSlim(1, 1);

        for (int i = 0; i < taskData.PendingTasks.Length; ++i)
        {
            taskData.PendingTasks[i].Apply(this);
        }
    }

    /// <summary>
    /// Get the given component type from <see cref="Components"/>.
    /// </summary>
    /// <remarks>Always returns a value or throws.</remarks>
    /// <exception cref="PlayerComponentNotFoundException">Component not found.</exception>
    [Pure]
    public TComponentType Component<TComponentType>() where TComponentType : IPlayerComponent
    {
        for (int i = 0; i < _componentsArray.Length; i++)
        {
            IPlayerComponent component = _componentsArray[i];
            if (component is TComponentType comp)
                return comp;
        }

        throw new PlayerComponentNotFoundException(typeof(TComponentType), this);
    }

    /// <summary>
    /// Get the given component type from <see cref="Components"/>.
    /// </summary>
    [Pure]
    public TComponentType? ComponentOrNull<TComponentType>() where TComponentType : IPlayerComponent
    {
        for (int i = 0; i < _componentsArray.Length; i++)
        {
            IPlayerComponent component = _componentsArray[i];
            if (component is TComponentType comp)
                return comp;
        }

        return default;
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

    
    public static readonly SpecialFormat FormatCharacterName = new SpecialFormat("Character Name", "cn");
    
    public static readonly SpecialFormat FormatNickName = new SpecialFormat("Nick Name", "nn");
    
    public static readonly SpecialFormat FormatPlayerName = new SpecialFormat("Player Name", "pn");
    
    public static readonly SpecialFormat FormatSteam64 = new SpecialFormat("Steam64 ID", "64");
    
    public static readonly SpecialFormat FormatColoredCharacterName = new SpecialFormat("Colored Character Name", "ccn");
    
    public static readonly SpecialFormat FormatColoredNickName = new SpecialFormat("Colored Nick Name", "cnn");
    
    public static readonly SpecialFormat FormatColoredPlayerName = new SpecialFormat("Colored Player Name", "cpn");
    
    public static readonly SpecialFormat FormatColoredSteam64 = new SpecialFormat("Colored Steam64 ID", "c64");
    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        // todo make this a proper implementation later.
        return new OfflinePlayer(in _playerNameHelper).Translate(formatter, in parameters);
    }

    public bool Equals(IPlayer other)
    {
        return Steam64.m_SteamID == other.Steam64.m_SteamID;
    }

    public bool Equals(WarfarePlayer other)
    {
        return Steam64.m_SteamID == other.Steam64.m_SteamID;
    }

    public bool Equals(Player other)
    {
        return Steam64.m_SteamID == other.channel.owner.playerID.steamID.m_SteamID;
    }
    
    public bool Equals(SteamPlayer other)
    {
        return Steam64.m_SteamID == other.playerID.steamID.m_SteamID;
    }
    
    public bool Equals(SteamPlayerID other)
    {
        return Steam64.m_SteamID == other.steamID.m_SteamID;
    }

    public override bool Equals(object? obj)
    {
        return obj is IPlayer player && Steam64.m_SteamID == player.Steam64.m_SteamID;
    }

    public override int GetHashCode()
    {
        return unchecked ( (int)_acctId );
    }

    bool ICommandUser.IsSuperUser => false;

    bool ICommandUser.IsTerminal => false;

    bool ICommandUser.IMGUI => Save.IMGUI;

    void ICommandUser.SendMessage(string message)
    {
        GameThread.AssertCurrent();
        ChatManager.serverSendMessage(message, Palette.AMBIENT, null, SteamPlayer, EChatMode.SAY, useRichTextFormatting: true);
    }

    Quaternion ITransformObject.Rotation
    {
        get => Transform.rotation;
        set
        {
            GameThread.AssertCurrent();
            UnturnedPlayer.teleportToLocationUnsafe(Transform.position, value.eulerAngles.y);
        }
    }
    
    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    void ITransformObject.SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        GameThread.AssertCurrent();
        UnturnedPlayer.teleportToLocationUnsafe(position, rotation.eulerAngles.y);
    }
}