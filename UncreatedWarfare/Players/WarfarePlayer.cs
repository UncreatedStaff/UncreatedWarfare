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
using Uncreated.Warfare.Players.PendingTasks;
using Uncreated.Warfare.Players.Permissions;
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
    ISpotter
{
    private int _modalHandles;
    private readonly CancellationTokenSource _disconnectTokenSource;
    private readonly ILogger _logger;
    private PlayerNames _playerNameHelper;
    private PlayerService.PlayerTaskData _playerTaskData;
    private PlayerPoints _cachedPoints;
    private readonly uint _acctId;
    private readonly SingleUseTypeDictionary<IPlayerComponent> _components;
    private readonly CSteamID _steam64;

    public ModalHandle GetModalHandle()
    {
        GameThread.AssertCurrent();
        ++_modalHandles;
        if ((UnturnedPlayer.pluginWidgetFlags & (EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur)) == 0)
        {
            UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
        }

        return new ModalHandle(this);
    }

    internal void DisposeModalHandle()
    {
        if (GameThread.IsCurrent)
        {
            --_modalHandles;
            if (_modalHandles == 0 && (UnturnedPlayer.pluginWidgetFlags & (EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur)) == (EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur))
                UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                if (!IsOnline)
                    return;

                DisposeModalHandle();
            });
        }
    }

    public ref readonly CSteamID Steam64 => ref _steam64;

    CSteamID IPlayer.Steam64 => _steam64;
    CSteamID ICommandUser.Steam64 => _steam64;

    /// <summary>
    /// Generic data persisting over the player's lifetime.
    /// </summary>
    public ConcurrentDictionary<string, object?> Data { get; } = new ConcurrentDictionary<string, object?>();

    /// <summary>
    /// If this player was created for unit tests.
    /// </summary>
    public bool IsTesting { get; }
    public Player UnturnedPlayer { get; }
    public SteamPlayer SteamPlayer { get; }
    public Transform Transform { get; }
    public Team Team { get; private set; }
    public BinaryPlayerSave Save { get; }
    public WarfarePlayerLocale Locale { get; }
    public PlayerSummary SteamSummary { get; }
    public SessionRecord CurrentSession { get; internal set; }
    public ref PlayerPoints CachedPoints => ref _cachedPoints;
    public double CachedReputation { get; internal set; } = double.NaN;
    public bool IsFirstTimePlaying { get; }
    public DateTime JoinTime { get; }

    /// <summary>
    /// List of steam IDs of this player's friends, if theyre public.
    /// </summary>
    /// <remarks>Private profiles will just have an empty array here.</remarks>
    public ulong[] SteamFriends { get; internal set; }

    /// <inheritdoc />
    public Vector3 Position
    {
        get => IsOnline && !IsTesting ? Transform.position : Vector3.zero;
        set
        {
            GameThread.AssertCurrent();
            UnturnedPlayer.teleportToLocationUnsafe(value, Transform.eulerAngles.y);
        }
    }

    /// <summary>
    /// If the player is currently on duty.
    /// </summary>
    /// <remarks>This should not be used for permission checks, instead proper permissions should be created for instances like that.</remarks>
    public bool IsOnDuty { get; private set; }

    /// <summary>
    /// The player's current staff level. This will be correct even when off duty.
    /// </summary>
    /// <remarks>This should not be used for permission checks, instead proper permissions should be created for instances like that.</remarks>
    public DutyLevel DutyLevel { get; private set; }

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

    public bool CanInfluenceObjective =>
#if DEBUG
        true;
#else
        !IsOnDuty;
#endif

    internal WarfarePlayer(PlayerService playerService, Player player, in PlayerService.PlayerTaskData taskData, PlayerPending pendingEvent, ILogger logger, IPlayerComponent[] components, IServiceProvider serviceProvider)
    {
        /*
         *  Real constructor used for live build
         */
        SteamSummary = pendingEvent.Summary;
        _disconnectTokenSource = taskData.TokenSource;
        _logger = logger;
        _playerTaskData = taskData;
        _playerNameHelper = new PlayerNames(player);
        UnturnedPlayer = player;
        SteamPlayer = player.channel.owner;
        _steam64 = player.channel.owner.playerID.steamID;
        _acctId = _steam64.GetAccountID().m_AccountID;
        Transform = player.transform;
        Save = new BinaryPlayerSave(Steam64, _logger);
        Save.Load();

        JoinTime = DateTime.UtcNow;
        
        pendingEvent.LanguagePreferences.Steam64 = _steam64.m_SteamID;
        Locale = new WarfarePlayerLocale(this, pendingEvent.LanguagePreferences, serviceProvider);

        _components = new SingleUseTypeDictionary<IPlayerComponent>(playerService.PlayerComponents, components);
        Components = new ReadOnlyCollection<IPlayerComponent>(_components.Values);

        Team = Team.NoTeam;

        for (int i = 0; i < components.Length; ++i)
        {
            components[i].Player = this;
        }

        IsFirstTimePlaying = !Save.WasReadFromFile;

        if (IsFirstTimePlaying)
            _logger.LogInformation($"Player {this} joined the server for the first time.");
        else
            _logger.LogInformation($"Player {this} joined the server.");
    }

    internal WarfarePlayer(uint id, ILogger logger, IServiceProvider serviceProvider, Action<WarfarePlayer>? modification = null)
    {
        /*
         * Creates a test WarfarePlayer for unit testing and sets everything up properly.
         */
        UnturnedPlayer = null!;
        SteamPlayer = null!;
        Transform = null!;
        CurrentSession = new SessionRecord
        {
            StartedTimestamp = DateTimeOffset.UtcNow,
            StartedGame = true
        };

        _steam64 = new CSteamID(new AccountID_t(id), EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeIndividual);
        _acctId = id;
        _logger = logger;
        Locale = new WarfarePlayerLocale(this, new LanguagePreferences { Steam64 = _steam64.m_SteamID, Language = new LanguageInfo { Code = "en-US" }, LanguageId = 1 }, serviceProvider);
        JoinTime = DateTime.UtcNow;
        Team = Team.NoTeam;
        Save = new BinaryPlayerSave(Steam64, _logger);
        _disconnectTokenSource = new CancellationTokenSource();
        _components = new SingleUseTypeDictionary<IPlayerComponent>();
        Components = new ReadOnlyCollection<IPlayerComponent>(_components.Values);
        string name = "t_" + id;
        SteamSummary = new PlayerSummary
        {
            Steam64 = _steam64.m_SteamID,
            AvatarUrlSmall = "https://avatars.fastly.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb.jpg",
            AvatarUrlFull = "https://avatars.fastly.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg",
            AvatarUrlMedium = "https://avatars.fastly.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_medium.jpg",
            CountryCode = "US",
            PlayerName = name,
            AvatarHash = "fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb",
            ProfileUrl = $"https://steamcommunity.com/profiles/{_steam64.m_SteamID:D17}/",
            Visibility = 3,
            ProfileState = 1,
            RealName = $"Test User {id}",
            RegionCode = "NC",
            TimeCreated = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600,
            LastLogOff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1800,
            PlayerState = 0,
            PlayerStateFlags = 0,
            PrimaryGroupId = 103582791457436331,
            CommentPermissionLevel = 1
        };
        SteamFriends = Array.Empty<ulong>();
        IsTesting = true;
        IsOnline = true;
        IsDisconnecting = false;
        IsConnecting = false;

        _playerNameHelper = new PlayerNames
        {
            WasFound = true,
            Steam64 = _steam64,
            CharacterName = name,
            PlayerName = name,
            NickName = name
        };

        IsFirstTimePlaying = true;
        modification?.Invoke(this);
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

    internal void UpdateDutyState(bool isOnDuty, DutyLevel level)
    {
        IsOnDuty = isOnDuty && level != DutyLevel.Member;
        DutyLevel = level;
    }

    internal void UpdateTeam(Team team)
    {
        Team = team;
        Save.TeamId = team.GroupId.m_SteamID;
    }

    internal void ApplyOfflineState()
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

    internal void StartDisconnecting()
    {
        IsDisconnecting = true;
        IsConnecting = false;
    }

    internal void EndConnecting()
    {
        IPlayerPendingTask[] tasks = _playerTaskData.PendingTasks;
        for (int i = 0; i < tasks.Length; ++i)
        {
            tasks[i].Apply(this);
        }

        IsConnecting = false;
    }

    public override string ToString()
    {
        return _playerNameHelper.ToString();
    }

    
    public static readonly SpecialFormat FormatCharacterName = new SpecialFormat("Character Name", "cn");
    
    public static readonly SpecialFormat FormatNickName = new SpecialFormat("Nick Name", "nn");
    
    public static readonly SpecialFormat FormatPlayerName = new SpecialFormat("Player Name", "pn");

    public static readonly SpecialFormat FormatDisplayOrCharacterName = new SpecialFormat("Display or Character Name", "dcn");

    public static readonly SpecialFormat FormatDisplayOrNickName = new SpecialFormat("Display or Nick Name", "dnn");

    public static readonly SpecialFormat FormatDisplayOrPlayerName = new SpecialFormat("Display or Player Name", "dpn");
    
    public static readonly SpecialFormat FormatSteam64 = new SpecialFormat("Steam64 ID", "64");
    
    public static readonly SpecialFormat FormatColoredCharacterName = new SpecialFormat("Colored Character Name", "ccn");
    
    public static readonly SpecialFormat FormatColoredNickName = new SpecialFormat("Colored Nick Name", "cnn");
    
    public static readonly SpecialFormat FormatColoredPlayerName = new SpecialFormat("Colored Player Name", "cpn");
    
    public static readonly SpecialFormat FormatColoredSteam64 = new SpecialFormat("Colored Steam64 ID", "c64");

    public static readonly SpecialFormat FormatColoredDisplayOrCharacterName = new SpecialFormat("ColoredDisplay or Character Name", "ccn");

    public static readonly SpecialFormat FormatColoredDisplayOrNickName = new SpecialFormat("ColoredDisplay or Nick Name", "cnn");

    public static readonly SpecialFormat FormatColoredDisplayOrPlayerName = new SpecialFormat("ColoredDisplay or Player Name", "cpn");

    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return new OfflinePlayer(in _playerNameHelper, Team).Translate(formatter, in parameters);
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

    bool ITransformObject.Alive => IsOnline && UnturnedPlayer.life.IsAlive;

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
        return new ValueTask<string>(SteamSummary.PlayerName);
    }

    bool ISpotter.IsTrackable => true;
    private event Action<ISpotter>? OnDestroyed;

    event Action<ISpotter>? ISpotter.OnDestroyed
    {
        add => OnDestroyed += value;
        remove => OnDestroyed -= value;
    }
}

/// <summary>
/// Allows for multiple UI's to be open at once that require the modal active without clearing each other.
/// </summary>
public struct ModalHandle(WarfarePlayer player) : IDisposable
{
    private readonly WarfarePlayer? _player = player;

    private int _disposed;

    public void Dispose()
    {
        if (_player == null)
            return;

        if (Interlocked.Exchange(ref _disposed, 1) != 0 || !_player.IsOnline)
            return;

        _player.DisposeModalHandle();
    }

    public static void TryGetModalHandle(WarfarePlayer player, ref ModalHandle modal)
    {
        if (modal is { _player: not null, _disposed: 0 })
            return;

        modal = player.GetModalHandle();
    }
}