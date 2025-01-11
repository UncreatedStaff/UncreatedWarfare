using SDG.NetTransport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Saves;
using Uncreated.Warfare.Squads.Spotted;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Steam.Models;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Abstraction for <see cref="WarfarePlayer"/> and offline players.
/// </summary>
public interface IPlayer : ITranslationArgument
{
    public CSteamID Steam64 { get; }
}

[CannotApplyEqualityOperator]
public class WarfarePlayer :
    IPlayer,
    ICommandUser,
    IModerationActor,
    IComponentContainer<IPlayerComponent>,
    IEquatable<IPlayer>,
    IEquatable<WarfarePlayer>,
    ITransformObject,
    ISpotter
{
    private readonly CancellationTokenSource _disconnectTokenSource;
    private readonly ILogger _logger;
    private PlayerNames _playerNameHelper;
    private PlayerPoints _cachedPoints;
    private readonly uint _acctId;
    private readonly SingleUseTypeDictionary<IPlayerComponent> _components;
    private readonly CSteamID _steam64;

    public ref readonly CSteamID Steam64 => ref _steam64;

    CSteamID IPlayer.Steam64 => _steam64;
    CSteamID ICommandUser.Steam64 => _steam64;

    /// <summary>
    /// Generic data persisting over the player's lifetime.
    /// </summary>
    public ConcurrentDictionary<string, object?> Data { get; } = new ConcurrentDictionary<string, object?>();

    public Player UnturnedPlayer { get; }
    public SteamPlayer SteamPlayer { get; }
    public Transform Transform { get; }
    public Team Team { get; private set; }
    public BinaryPlayerSave Save { get; }
    public WarfarePlayerLocale Locale { get; }

    [Obsolete]
    public SemaphoreSlim PurchaseSync { get; }
    public PlayerSummary SteamSummary { get; internal set; } = null!;
    public SessionRecord CurrentSession { get; internal set; }
    public ref PlayerPoints CachedPoints => ref _cachedPoints;

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
    /// If the player's <see cref="PlayerJoined"/> event is still invoking.
    /// </summary>
    public bool IsConnecting { get; private set; } = true;

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
    public ref PlayerNames Names => ref _playerNameHelper;

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
    internal WarfarePlayer(PlayerService playerService, Player player, in PlayerService.PlayerTaskData taskData, ILogger logger, IPlayerComponent[] components, IServiceProvider serviceProvider)
    {
        _disconnectTokenSource = taskData.TokenSource;
        _logger = logger;
        _playerNameHelper = new PlayerNames(player);
        UnturnedPlayer = player;
        SteamPlayer = player.channel.owner;
        _steam64 = player.channel.owner.playerID.steamID;
        _acctId = _steam64.GetAccountID().m_AccountID;
        Transform = player.transform;
        Save = new BinaryPlayerSave(Steam64, _logger);
        Save.Load();

        Locale = new WarfarePlayerLocale(this, new LanguagePreferences { Steam64 = Steam64.m_SteamID }, serviceProvider);

        _components = new SingleUseTypeDictionary<IPlayerComponent>(playerService.PlayerComponents, components);
        Components = new ReadOnlyCollection<IPlayerComponent>(_components.Values);

        Team = Team.NoTeam;
        _logger.LogInformation("Player {0} joined the server", this);

        PurchaseSync = new SemaphoreSlim(1, 1);

        for (int i = 0; i < taskData.PendingTasks.Length; ++i)
        {
            taskData.PendingTasks[i].Apply(this);
        }
    }

    /// <inheritdoc />
    [Pure]
    public TComponentType Component<TComponentType>() where TComponentType : class, IPlayerComponent
    {
        return _components.Get<TComponentType, WarfarePlayer>(this);
    }

    /// <inheritdoc />
    [Pure]
    public TComponentType? ComponentOrNull<TComponentType>() where TComponentType : class, IPlayerComponent
    {
        return _components.TryGet(out TComponentType? comp) ? comp : null;
    }

    /// <inheritdoc />
    [Pure]
    public object Component(Type t)
    {
        return _components.Get(t, this);
    }

    /// <inheritdoc />
    [Pure]
    public object? ComponentOrNull(Type t)
    {
        return _components.TryGet(t, out object? comp) ? comp : null;
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
            IsConnecting = false;
            IsOnline = false;
            OnDestroyed?.Invoke(this);
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
        IsConnecting = false;
    }
    public void EndConnecting()
    {
        IsConnecting = false;
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

    public bool Equals([NotNullWhen(true)] IPlayer? other)
    {
        return other != null && Steam64.m_SteamID == other.Steam64.m_SteamID;
    }

    public bool Equals([NotNullWhen(true)] WarfarePlayer? other)
    {
        return other != null && Steam64.m_SteamID == other.Steam64.m_SteamID;
    }

    public bool Equals([NotNullWhen(true)] Player? other)
    {
        return other != null && Steam64.m_SteamID == other.channel.owner.playerID.steamID.m_SteamID;
    }
    
    public bool Equals([NotNullWhen(true)] SteamPlayer? other)
    {
        return other != null && Steam64.m_SteamID == other.playerID.steamID.m_SteamID;
    }
    
    public bool Equals([NotNullWhen(true)] SteamPlayerID? other)
    {
        return other != null && Steam64.m_SteamID == other.steamID.m_SteamID;
    }
    
    public bool Equals(CSteamID steam64)
    {
        return Steam64.m_SteamID == steam64.m_SteamID;
    }
    
    public bool Equals(ulong steam64)
    {
        return Steam64.m_SteamID == steam64;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj switch
        {
            IPlayer player => Steam64.m_SteamID == player.Steam64.m_SteamID,
            Player uPlayer => Steam64.m_SteamID == uPlayer.channel.owner.playerID.steamID.m_SteamID,
            SteamPlayer stPlayer => Steam64.m_SteamID == stPlayer.playerID.steamID.m_SteamID,
            SteamPlayerID stPlayerID => Steam64.m_SteamID == stPlayerID.steamID.m_SteamID,
            ulong id => Steam64.m_SteamID == id,
            CSteamID cid => Steam64.m_SteamID == cid.m_SteamID,
            _ => false
        };
    }

    public IModerationActor GetModerationActor()
    {
        return this;
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

    bool ITransformObject.Alive => IsOnline;

    bool IModerationActor.Async => false;

    ulong IModerationActor.Id => Steam64.m_SteamID;

    ValueTask<string?> IModerationActor.GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token)
    {
        return new ValueTask<string?>(size switch
        {
            AvatarSize.Medium => SteamSummary.AvatarUrlMedium,
            AvatarSize.Small => SteamSummary.AvatarUrlSmall,
            _ => SteamSummary.AvatarUrlFull
        });
    }

    ValueTask<string> IModerationActor.GetDisplayName(DatabaseInterface database, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    bool ISpotter.IsTrackable => true;
    private event Action<ISpotter>? OnDestroyed;

    event Action<ISpotter>? ISpotter.OnDestroyed
    {
        add => OnDestroyed += value;
        remove => OnDestroyed -= value;
    }
}