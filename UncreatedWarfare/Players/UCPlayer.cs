using Cysharp.Threading.Tasks;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Json;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Ranks;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using UnityEngine;
using SteamAPI = Uncreated.Warfare.Networking.SteamAPI;

namespace Uncreated.Warfare;

public sealed class UCPlayer : IPlayer, IComparable<UCPlayer>, IEquatable<UCPlayer>, IModerationActor
{
    [FormatDisplay(typeof(IPlayer), "Character Name")]
    public const string CHARACTER_NAME_FORMAT = "cn";
    [FormatDisplay(typeof(IPlayer), "Nick Name")]
    public const string NICK_NAME_FORMAT = "nn";
    [FormatDisplay(typeof(IPlayer), "Player Name")]
    public const string PLAYER_NAME_FORMAT = "pn";
    [FormatDisplay(typeof(IPlayer), "Steam64 ID")]
    public const string STEAM_64_FORMAT = "64";
    [FormatDisplay(typeof(IPlayer), "Colored Character Name")]
    public const string COLOR_CHARACTER_NAME_FORMAT = "ccn";
    [FormatDisplay(typeof(IPlayer), "Colored Nick Name")]
    public const string COLOR_NICK_NAME_FORMAT = "cnn";
    [FormatDisplay(typeof(IPlayer), "Colored Player Name")]
    public const string COLOR_PLAYER_NAME_FORMAT = "cpn";
    [FormatDisplay(typeof(IPlayer), "Colored Steam64 ID")]
    public const string COLOR_STEAM_64_FORMAT = "c64";

    private static readonly InstanceGetter<Dictionary<Buff, float>, int> VersionGetter =
        Util.GenerateInstanceGetter<Dictionary<Buff, float>, int>("version", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly IEqualityComparer<UCPlayer> Comparer = new EqualityComparer();
    public static readonly UnturnedUI MutedUI = new UnturnedUI(Gamemode.Config.UIMuted, hasElements: false);
    public static readonly UnturnedUI LoadingUI = new UnturnedUI(Gamemode.Config.UILoading, hasElements: false);
    /*
     * There can never be more than one semaphore per player (even if they've gone offline)
     * as this object will get reused until the finalizer runs, so don't save the semaphore outside of a sync local scope.
     * If you need it to stick around save the UCPlayer instead.
     */
    public readonly UCSemaphore PurchaseSync;
    public readonly UCPlayerKeys Keys;
    public readonly UCPlayerEvents Events;
    public KitMenuUIData KitMenuData;
    public readonly ulong Steam64;
    public volatile bool HasInitedOnce;
    public volatile bool HasDownloadedKitData;
    public volatile bool HasDownloadedXP;
    public volatile bool IsDownloadingXP;
    public volatile bool IsDownloadingKitData;
    public volatile bool Loading;
    public bool PendingCheaterDeathBan;
    public bool Loaded;
    public int SuppliesUnloaded;
    public int LifeCounter;
    public int CachedCredits;
    public bool HasUIHidden = false;
    public float LastSpoken;
    public string CharacterName;
    public string NickName;
    public SqlItem<Kit>? ActiveKit;
    public string? MuteReason;
    public MuteType MuteType;
    public EChatMode LastChatMode = EChatMode.GLOBAL;
    public DateTime TimeUnmuted;
    public Squad? Squad;
    public TeamSelectorData? TeamSelectorData;
    public Coroutine? StorageCoroutine;
    public RankStatus[]? RankData;
    public List<SqlItem<Kit>>? AccessibleKits;
    public List<HotkeyBinding>? HotkeyBindings;
    internal List<LayoutTransformation>? LayoutTransformations;
    public IBuff?[] ActiveBuffs = new IBuff?[BuffUI.MaxBuffs];
    public List<Trait> ActiveTraits = new List<Trait>(8);
    internal Action<byte, ItemJar> SendItemRemove;
    // used to trace items back to their original position in the kit
    internal List<ItemTransformation> ItemTransformations = new List<ItemTransformation>(16);
    internal List<ItemDropTransformation> ItemDropTransformations = new List<ItemDropTransformation>(16);
    internal List<Guid>? CompletedQuests;
    internal bool ModalNeeded;
    // [xp sent][credits sent][xp vis][credits vis][credits][branch][level][xp]
    internal byte PointsDirtyMask = 0b00111111;
    internal bool HasTicketUI = false;
    internal bool HasFOBUI = false;
    internal bool HasModerationUI = false;
    internal int MortarWarningCount = 0;
    private readonly CancellationTokenSource _disconnectTokenSrc;
    private int _multVersion = -1;
    private bool _isTalking;
    private bool _isOnline;
    private bool _lastMuted;
    private float _multCache = 1f;
    private EAdminType? _pLvl;
    private LevelData? _level;
    private PlayerNames _cachedName;
    internal VehicleSwapRequest PendingVehicleSwapRequest;
    internal int CacheLocationIndex = -1;
    internal UCPlayer(CSteamID steamID, Player player, string characterName, string nickName, bool donator, CancellationTokenSource pendingSrc, PlayerSave save, UCSemaphore semaphore, PendingAsyncData data)
    {
        Steam64 = steamID.m_SteamID;
        PurchaseSync = semaphore;
        Squad = null;
        Player = player;
        CSteamID = steamID;
        Save = save;
        ActiveKit = KitManager.GetSingletonQuick()?.FindKit(Save.KitName, default, true).Result;
        Locale = new UCPlayerLocale(this, data.LanguagePreferences);
        if (!Data.OriginalPlayerNames.TryGetValue(Steam64, out _cachedName))
            _cachedName = new PlayerNames(player);
        else Data.OriginalPlayerNames.Remove(Steam64);
        NickName = nickName;
        CharacterName = characterName;
        _isOnline = true;
        IsOtherDonator = donator;
        LifeCounter = 0;
        SuppliesUnloaded = 0;
        CurrentMarkers = new List<SpottedComponent>();
        _disconnectTokenSrc = pendingSrc;
        KitMenuData = new KitMenuUIData { Player = this };
        KitMenuData.Init();
        if (Data.UseFastKits)
        {
            try
            {
                SendItemRemove = (Action<byte, ItemJar>)typeof(PlayerInventory).GetMethod("sendItemRemove", BindingFlags.Instance | BindingFlags.NonPublic)?.CreateDelegate(typeof(Action<byte, ItemJar>), Player.inventory)!;
            }
            catch
            {
                L.LogError("Failed to get PlayerInventory.sendItemRemove for player " + characterName);
                Data.UseFastKits = false;
            }
        }
        Keys = new UCPlayerKeys(this);
        Events = new UCPlayerEvents(this);
        Toasts = new ToastManager(this);

        try
        {
            IPAddress address = player.channel.owner.transportConnection.GetAddress();
            UsingRemotePlay = OffenseManager.IsRemotePlay(address);
        }
        catch (Exception ex)
        {
            L.LogWarning("Error getting IP address.");
            L.LogError(ex);
        }
    }
    ~UCPlayer()
    {
        PlayerManager.DeregisterPlayerSemaphore(Steam64);
        L.LogDebug("Player finalized: [" + Steam64 + "].");
    }
    public enum NameSearch : byte
    {
        CharacterName,
        NickName,
        PlayerName
    }
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is null) goto end;
        if (format.Equals(CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Name.CharacterName;
        if (format.Equals(NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Name.NickName;
        if (format.Equals(PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Name.PlayerName;
        if (format.Equals(STEAM_64_FORMAT, StringComparison.Ordinal))
            return Steam64.ToString(Data.LocalLocale);

        string hex = TeamManager.GetTeamHexColor(this.GetTeam());
        if (format.Equals(COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Name.CharacterName, flags);
        if (format.Equals(COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Name.NickName, flags);
        if (format.Equals(COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Name.PlayerName, flags);
        if (format.Equals(COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Steam64.ToString(Data.LocalLocale), flags);
        end:
        return Name.CharacterName;
    }
    public UCPlayerLocale Locale { get; }
    public ToastManager Toasts { get; }
    public InteractableVehicle? CurrentVehicle => Player.movement.getVehicle();
    public bool IsInVehicle => CurrentVehicle != null;
    public bool IsDriver => CurrentVehicle != null && CurrentVehicle.passengers.Length > 0 && CurrentVehicle.passengers[0].player != null && CurrentVehicle.passengers[0].player.playerID.steamID.m_SteamID == Steam64;
    public bool HasKit => ActiveKit?.Item is not null;
    public Class KitClass => ActiveKit?.Item is { } kit ? kit.Class : Class.None;
    public Branch Branch => ActiveKit?.Item is { } kit ? kit.Branch : Branch.Default;
    bool IEquatable<UCPlayer>.Equals(UCPlayer other) => other == this || other.Steam64 == Steam64; 
    public SteamPlayer SteamPlayer => Player.channel.owner;
    public PlayerSave Save { get; }
    public Player Player { get; internal set; }
    public PlayerSummary? CachedSteamProfile { get; internal set; }
    public CSteamID CSteamID { get; }
    public ITransportConnection Connection => Player.channel.owner.transportConnection!;
    public EffectAsset? LastPing { get; internal set; }
    public ulong? ViewLens { get; set; }
    public bool UsingRemotePlay { get; }
    ulong IPlayer.Steam64 => Steam64;
    public bool IsAdmin => Player.channel.owner.isAdmin;
    public bool IsTeam1 => Player.quests.groupID.m_SteamID == TeamManager.Team1ID;
    public bool IsTeam2 => Player.quests.groupID.m_SteamID == TeamManager.Team2ID;
    public bool IsTalking => !_lastMuted && _isTalking && IsOnline;
    public bool IsLeaving { get; internal set; }
    public bool IsOnline => _isOnline;
    public bool VanishMode { get; set; }
    public bool IsActionMenuOpen { get; internal set; }
    public bool IsOtherDonator { get; set; }
    public bool GodMode { get; set; }
    public CancellationToken DisconnectToken => _disconnectTokenSrc.Token;
    public FactionInfo? Faction => Player.quests.groupID.m_SteamID switch
    {
        TeamManager.Team1ID => TeamManager.Team1Faction,
        TeamManager.Team2ID => TeamManager.Team2Faction,
        TeamManager.AdminID => TeamManager.AdminFaction,
        _ => null
    };

    public Dictionary<Buff, float> ShovelSpeedMultipliers { get; } = new Dictionary<Buff, float>(6);
    public List<SpottedComponent> CurrentMarkers { get; }
    /// <summary><see langword="True"/> if rank order <see cref="OfficerStorage.OFFICER_RANK_ORDER"/> has been completed (Receiving officer pass from discord server).</summary>
    public bool IsOfficer => RankData != null && RankData.Length > RankManager.Config.OfficerRankIndex && RankData[RankManager.Config.OfficerRankIndex].IsCompelete;
    public LevelData Level
    {
        get
        {
            if (!_level.HasValue)
                return default;
            return _level.Value;
        }
    }
    public float ShovelSpeedMultiplier
    {
        get
        {
            int version = VersionGetter(ShovelSpeedMultipliers);
            if (version == _multVersion)
                return _multCache;
            float max = 0f;
            foreach (float fl in ShovelSpeedMultipliers.Values)
                if (fl > max) max = fl;
            if (max <= 0f)
                max = 1f;
            _multVersion = version;
            return _multCache = max;
        }
    }
    public Vector3 Position
    {
        get
        {
            try
            {
                return Player.transform.position;
            }
            catch (NullReferenceException)
            {
                L.LogError("ERROR: Player transform was null");
                L.Log($"Kicking {Name.PlayerName} ({Steam64}) for null transform.", ConsoleColor.Cyan);
                Provider.kick(Player.channel.owner.playerID.steamID, Localization.Translate(T.NullTransformKickMessage, this, UCWarfare.Config.DiscordInviteCode));
                throw new NotSupportedException("Player " + Name.PlayerName + " (" + Steam64 + ") has already been disposed of. Getting the position is not supported.");
            }
        }
    }
    public bool IsOnTeam
    {
        get
        {
            ulong team = this.GetTeam();
            return team is 1 or 2;
        }
    }
    public int CachedXP
    {
        get => Level.TotalXP;
        set => _level = new LevelData(value);
    }
    public PlayerNames Name
    {
        get
        {
            if (_cachedName == PlayerNames.Nil)
                _cachedName = new PlayerNames
                {
                    Steam64 = Steam64,
                    CharacterName = Player.channel.owner.playerID.characterName,
                    NickName = Player.channel.owner.playerID.nickName,
                    PlayerName = Player.channel.owner.playerID.playerName,
                    WasFound = true
                };
            return _cachedName;
        }
    }
    public char Icon
    {
        get
        {
            if (SquadManager.Config.Classes == null || SquadManager.Config.Classes.Length == 0)
                return '±';
            for (int i = 0; i < SquadManager.Config.Classes.Length; ++i)
            {
                ref ClassConfig c = ref SquadManager.Config.Classes[i];
                if (c.Class == KitClass)
                {
                    return c.Icon;
                }
            }

            return SquadManager.Config.Classes[0].Icon;
        }
    }

    public EffectAsset Marker
    {
        get
        {
            EffectAsset asset;
            if (SquadManager.Config.Classes == null || SquadManager.Config.Classes.Length == 0)
                return Assets.find<EffectAsset>(new Guid("28b4d205725c42be9a816346200ba1d8"));
            for (int i = 0; i < SquadManager.Config.Classes.Length; ++i)
            {
                ref ClassConfig c = ref SquadManager.Config.Classes[i];
                if (c.Class == KitClass)
                {
                    if (c.MarkerEffect.ValidReference(out asset))
                        return asset;
                    else break;
                }
            }

            if (SquadManager.Config.Classes[0].MarkerEffect.ValidReference(out asset))
                return asset;
            return Assets.find<EffectAsset>(new Guid("28b4d205725c42be9a816346200ba1d8"));
        }
    }
    public EffectAsset SquadLeaderMarker
    {
        get
        {
            EffectAsset asset;
            if (SquadManager.Config.Classes == null || SquadManager.Config.Classes.Length == 0)
                return Assets.find<EffectAsset>(new Guid("28b4d205725c42be9a816346200ba1d8"));
            for (int i = 0; i < SquadManager.Config.Classes.Length; ++i)
            {
                ref ClassConfig c = ref SquadManager.Config.Classes[i];
                if (c.Class == KitClass)
                {
                    if (c.SquadLeaderMarkerEffect.ValidReference(out asset))
                        return asset;
                    else break;
                }
            }

            if (SquadManager.Config.Classes[0].SquadLeaderMarkerEffect.ValidReference(out asset))
                return asset;
            return Assets.find<EffectAsset>(new Guid("28b4d205725c42be9a816346200ba1d8"));
        }
    }

    public EAdminType PermissionLevel
    {
        get
        {
            if (_pLvl.HasValue) return _pLvl.Value;
            _pLvl = PermissionSaver.Instance.GetPlayerPermissionLevel(Steam64, true);
            return _pLvl.Value;
        }
        set
        {
            if (_pLvl.HasValue && _pLvl.Value == value)
                return;
            PermissionSaver.Instance.SetPlayerPermissionLevel(Steam64, value);
            _pLvl = value;
        }
    }
    public static explicit operator ulong(UCPlayer player) => player.Steam64;
    public static implicit operator CSteamID(UCPlayer player) => player.Player.channel.owner.playerID.steamID;
    public static implicit operator Player(UCPlayer player) => player.Player;
    public static implicit operator SteamPlayer(UCPlayer player) => player.Player.channel.owner;
    internal void SetOffline()
    {
        lock (this)
        {
            _isOnline = false;
            IsLeaving = false;
            _disconnectTokenSrc.Cancel();
            Events.Dispose();
            Keys.Dispose();
            KitMenuData = null!;
        }
    }
    public static UCPlayer? FromID(ulong steamID) => steamID == 0 ? null : PlayerManager.FromID(steamID);
    public static UCPlayer? FromCSteamID(CSteamID steamID) => steamID.m_SteamID == 0 ? null : FromID(steamID.m_SteamID);
    public static UCPlayer? FromPlayer(Player player) => player == null ? null : FromID(player.channel.owner.playerID.steamID.m_SteamID);
    public static UCPlayer? FromSteamPlayer(SteamPlayer player)
    {
        if (player == null) return null;
        return FromID(player.playerID.steamID.m_SteamID);
    }
    public static UCPlayer? FromName(string name, bool includeContains = false)
    {
        if (name == null) return null;
        UCPlayer? player = PlayerManager.OnlinePlayers.Find(
            s =>
            s.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.InvariantCultureIgnoreCase) ||
            s.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.InvariantCultureIgnoreCase) ||
            s.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.InvariantCultureIgnoreCase)
            );
        if (includeContains && player == null)
        {
            player = PlayerManager.OnlinePlayers.Find(s =>
                s.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1);
        }
        return player;
    }
    public static UCPlayer? FromName(string name, bool includeContains, IEnumerable<UCPlayer> selection)
    {
        if (name == null) return null;
        UCPlayer? player = selection.FirstOrDefault(
            s =>
            s.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.InvariantCultureIgnoreCase) ||
            s.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.InvariantCultureIgnoreCase) ||
            s.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.InvariantCultureIgnoreCase)
            );
        if (includeContains && player == null)
        {
            player = selection.FirstOrDefault(s =>
                s.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1);
        }
        return player;
    }

    /// <summary>Slow, use rarely.</summary>
    public static UCPlayer? FromName(string name, NameSearch type) => FromName(name, PlayerManager.OnlinePlayers, type);
    public static UCPlayer? FromName(string name, IEnumerable<UCPlayer> selection, NameSearch type)
    {
        if (type == NameSearch.CharacterName)
        {
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.characterName.Length))
            {
                if (current.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.nickName.Length))
            {
                if (current.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.playerName.Length))
            {
                if (current.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            return null;
        }
        else if (type == NameSearch.NickName)
        {
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.nickName.Length))
            {
                if (current.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.characterName.Length))
            {
                if (current.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.playerName.Length))
            {
                if (current.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            return null;
        }
        else if (type == NameSearch.PlayerName)
        {
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection)
            {
                if (current.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.playerName.Length))
            {
                if (current.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.characterName.Length))
            {
                if (current.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in selection.OrderBy(x => x.Player.channel.owner.playerID.nickName.Length))
            {
                if (current.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return current;
            }
            return null;
        }
        else return FromName(name, NameSearch.CharacterName);
    }
    public bool IsInSameSquadAs(UCPlayer other) => Squad is not null && other.Squad is not null && Squad == other.Squad;
    public bool IsInSameVehicleAs(UCPlayer other)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        InteractableVehicle? veh = CurrentVehicle;
        if (veh == null)
            return false;

        foreach (Passenger passenger in veh.passengers)
        {
            if (passenger.player != null && passenger.player.playerID.steamID.m_SteamID == other.Steam64)
                return true;
        }

        return false;
    }
    public void ActivateMarker(SpottedComponent marker)
    {
        CurrentMarkers.Remove(marker);

        if (CurrentMarkers.Count(x => !x.UAVMode) >= 3)
        {
            SpottedComponent? oldest = CurrentMarkers.LastOrDefault(x => !x.UAVMode);
            if (oldest != null)
            {
                oldest.Deactivate();
                CurrentMarkers.Remove(oldest);
            }
        }

        CurrentMarkers.Insert(0, marker);
    }
    public void DeactivateMarker(SpottedComponent marker) => CurrentMarkers.Remove(marker);
    /// <remarks>Thread Safe</remarks>
    public void ChangeKit(SqlItem<Kit>? kit)
    {
        ItemTransformations.Clear();
        ItemDropTransformations.Clear();
        if (kit?.Item == null)
        {
            ActiveKit = null;
        }
        else
        {
            ActiveKit = kit;
        }

        Apply();
    }
    public EffectAsset GetMarker() => Squad == null || Squad.Leader == null || Squad.Leader.Steam64 != Steam64 ? Marker : SquadLeaderMarker;
    public bool IsSquadLeader()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Squad?.Leader is null)
            return false;

        return Squad.Leader.Steam64 == Steam64;
    }
    public bool IsNearSquadLeader(float distance)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (distance == 0 || Squad is null || Squad.Leader is null || Squad.Leader.Player is null || Squad.Leader.Player.transform is null)
            return false;

        if (Squad.Leader.Steam64 == Steam64)
            return false;

        return (Position - Squad.Leader.Position).sqrMagnitude < distance * distance;
    }
    public bool IsOrIsNearLeader(float distance)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Squad is null || Player.transform is null || Squad.Leader.Player.transform is null)
            return false;

        if (Squad.Leader.Steam64 == Steam64)
            return true;

        if (Player.life.isDead || Squad.Leader.Player.life.isDead)
            return false;

        return (Position - Squad.Leader.Position).sqrMagnitude < Math.Pow(distance, 2);
    }
    public int NearbyMemberBonus(int amount, float distance)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (Player.life.isDead || Player.transform is null)
                return amount;
            if (Squad is null)
                return amount;

            int count = 0;
            for (int i = 0; i < Squad.Members.Count; i++)
            {
                if (Squad.Members[i].Player.transform != null && Squad.Members[i].Steam64 != Steam64 && (Position - Squad.Members[i].Position).sqrMagnitude < Math.Pow(distance, 2))
                    count++;
            }
            return Mathf.RoundToInt(amount * (1 + (float)count / 10));
        }
        catch
        {
            return amount;
        }
    }
    /// <remarks>Thread Safe</remarks>
    public void Apply()
    {
        UCWarfare.RunOnMainThread(ApplyIntl);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyIntl() => PlayerManager.ApplyTo(this);
    public bool IsOnFOB(out IFOB fob) => FOBManager.IsOnFOB(this, out fob);
    public bool IsOnFOB<TFOB>(out TFOB fob) where TFOB : class, IRadiusFOB => FOBManager.IsOnFOB(this, out fob);
    public int CompareTo(UCPlayer obj) => Steam64.CompareTo(obj.Steam64);
    public void SetCosmeticStates(bool state)
    {
        ThreadUtil.assertIsGameThread();
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, state);
    }
    public void RemoveSkillset(EPlayerSpeciality speciality, byte skill)
    {
        ThreadUtil.assertIsGameThread();
        Skill[][] skills = Player.skills.skills;
        if ((int)speciality >= skills.Length)
            throw new ArgumentOutOfRangeException(nameof(speciality), "Speciality index is out of range.");
        if (skill >= skills[(int)speciality].Length)
            throw new ArgumentOutOfRangeException(nameof(skill), "Skill index is out of range.");
        Skill skillObj = skills[(int)speciality][skill];
        Skillset[] def = Skillset.DefaultSkillsets;
        for (int d = 0; d < def.Length; ++d)
        {
            Skillset s = def[d];
            if (s.Speciality == speciality && s.SkillIndex == skill)
            {
                if (s.Level != skillObj.level)
                {
                    L.LogDebug($"Setting server default: {s}.");
                    s.ServerSet(this);
                }
                else
                    L.LogDebug($"Server default already set: {s}.");

                return;
            }
        }
        byte defaultLvl = GetDefaultSkillLevel(speciality, skill);

        if (skillObj.level != defaultLvl)
        {
            Player.skills.ServerSetSkillLevel((int)speciality, skill, defaultLvl);
            L.LogDebug($"Setting game default: {new Skillset(speciality, skill, defaultLvl)}.");
        }
        else
        {
            L.LogDebug($"Game default already set: {new Skillset(speciality, skill, defaultLvl)}.");
        }
    }
    public void EnsureSkillset(Skillset skillset)
    {
        ThreadUtil.assertIsGameThread();
        Skill[][] skills = Player.skills.skills;
        if (skillset.SpecialityIndex >= skills.Length)
            throw new ArgumentOutOfRangeException(nameof(skillset), "Speciality index is out of range.");
        if (skillset.SkillIndex >= skills[skillset.SpecialityIndex].Length)
            throw new ArgumentOutOfRangeException(nameof(skillset), "Skill index is out of range.");
        Skill skill = skills[skillset.SpecialityIndex][skillset.SkillIndex];
        if (skillset.Level != skill.level)
        {
            skillset.ServerSet(this);
        }
    }
    public void EnsureSkillsets(Skillset[] skillsets)
    {
        ThreadUtil.assertIsGameThread();
        Skillset[] def = Skillset.DefaultSkillsets;
        Skill[][] skills = Player.skills.skills;
        for (int specIndex = 0; specIndex < skills.Length; ++specIndex)
        {
            Skill[] specialtyArr = skills[specIndex];
            for (int skillIndex = 0; skillIndex < specialtyArr.Length; ++skillIndex)
            {
                Skill skill = specialtyArr[skillIndex];
                for (int d = 0; d < skillsets.Length; ++d)
                {
                    Skillset s = skillsets[d];
                    if (s.SpecialityIndex == specIndex && s.SkillIndex == skillIndex)
                    {
                        if (s.Level != skill.level)
                        {
                            s.ServerSet(this);
                        }
                        goto c;
                    }
                }
                for (int d = 0; d < def.Length; ++d)
                {
                    Skillset s = def[d];
                    if (s.SpecialityIndex == specIndex && s.SkillIndex == skillIndex)
                    {
                        if (s.Level != skill.level)
                        {
                            s.ServerSet(this);
                        }
                        goto c;
                    }
                }

                byte defaultLvl = GetDefaultSkillLevel((EPlayerSpeciality)specIndex, (byte)skillIndex);

                if (skill.level != defaultLvl)
                {
                    Player.skills.ServerSetSkillLevel(specIndex, skillIndex, defaultLvl);
                }
                c:;
            }
        }
    }
    public byte GetDefaultSkillLevel(EPlayerSpeciality speciality, byte skill)
    {
        Skill[][] skills = Player.skills.skills;
        if ((int)speciality >= skills.Length)
            throw new ArgumentOutOfRangeException(nameof(speciality), "Speciality index is out of range.");
        if (skill >= skills[(int)speciality].Length)
            throw new ArgumentOutOfRangeException(nameof(skill), "Skill index is out of range.");
        int specIndex = (int)speciality;
        if (Provider.modeConfigData.Players.Spawn_With_Max_Skills ||
            specIndex == (int)EPlayerSpeciality.OFFENSE &&
            (EPlayerOffense)skill is
            EPlayerOffense.CARDIO or EPlayerOffense.EXERCISE or
            EPlayerOffense.DIVING or EPlayerOffense.PARKOUR &&
            Provider.modeConfigData.Players.Spawn_With_Stamina_Skills)
        {
            return skills[(int)speciality][skill].max;
        }
        if (SDG.Unturned.Level.getAsset() is { skillRules: { } } asset)
        {
            if (asset.skillRules.Length > specIndex && asset.skillRules[specIndex].Length > skill)
            {
                LevelAsset.SkillRule rule = asset.skillRules[specIndex][skill];
                if (rule != null)
                    return (byte)rule.defaultLevel;
            }
        }

        return 0;
    }

    public override string ToString() => Name.PlayerName + " [" + Steam64.ToString("G17", Data.AdminLocale) + "]";
    internal void ResetPermissionLevel() => _pLvl = null;
    internal void Update()
    {
        Toasts.Update();

        if (_isTalking && Time.realtimeSinceStartup - LastSpoken > 0.5f)
        {
            _isTalking = false;
            if (_lastMuted)
            {
                MutedUI.ClearFromPlayer(Connection);
                _lastMuted = false;
            }
        }
    }
    internal void OnUseVoice(bool isMuted)
    {
        float t = Time.realtimeSinceStartup;
        if (isMuted != _lastMuted)
        {
            if (isMuted)
                MutedUI.SendToPlayer(Connection);
            else
                MutedUI.ClearFromPlayer(Connection);
            _lastMuted = isMuted;
        }
        LastSpoken = t;
        _isTalking = true;
    }
    internal void UpdatePoints(uint xp, uint credits)
    {
        _level = new LevelData((int)xp);
        CachedCredits = (int)credits;
    }
    internal void OnLanguageChanged()
    {
        // todo
    }

    private class EqualityComparer : IEqualityComparer<UCPlayer>
    {
        bool IEqualityComparer<UCPlayer>.Equals(UCPlayer x, UCPlayer y) => x == y || x.Steam64 == y.Steam64;
        int IEqualityComparer<UCPlayer>.GetHashCode(UCPlayer obj) => obj.Steam64.GetHashCode();
    }

    public static void TryApplyViewLens(ref UCPlayer original)
    {
        if (original is { ViewLens: { } lens } && FromID(lens) is { IsOnline: true } vl)
            original = vl;
    }

    bool IModerationActor.Async => true;
    ulong IModerationActor.Id => Steam64;
    ValueTask<string> IModerationActor.GetDisplayName(DatabaseInterface database, CancellationToken token) => new ValueTask<string>(Name.CharacterName);
    async ValueTask<string?> IModerationActor.GetProfilePictureURL(DatabaseInterface database, AvatarSize size, CancellationToken token)
    {
        return await GetProfilePictureURL(size, token);
    }
    public async UniTask<PlayerSummary> GetPlayerSummary(bool allowCache = true, CancellationToken token = default)
    {
        if (allowCache && CachedSteamProfile != null)
            return CachedSteamProfile;

        PlayerSummary? playerSummary = await SteamAPI.GetPlayerSummary(Steam64, token);
        if (playerSummary != null)
            CachedSteamProfile = playerSummary;
#if DEBUG
        ThreadUtil.assertIsGameThread();
#endif

        if (playerSummary != null && UCWarfare.IsLoaded)
        {
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlSmall))
                Data.ModerationSql.UpdateAvatar(Steam64, AvatarSize.Small, playerSummary.AvatarUrlSmall);
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlMedium))
                Data.ModerationSql.UpdateAvatar(Steam64, AvatarSize.Medium, playerSummary.AvatarUrlMedium);
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlFull))
                Data.ModerationSql.UpdateAvatar(Steam64, AvatarSize.Full, playerSummary.AvatarUrlFull);
        }

        return playerSummary ?? new PlayerSummary
        {
            Steam64 = Steam64,
            PlayerName = Name.PlayerName
        };
    }
    public async UniTask<string?> GetProfilePictureURL(AvatarSize size, CancellationToken token = default)
    {
        if (!UCWarfare.IsLoaded)
            throw new SingletonUnloadedException(typeof(UCWarfare));
        if (Data.ModerationSql.TryGetAvatar(Steam64, size, out string url))
            return url;

        PlayerSummary summary = await GetPlayerSummary(token: token);

        return size switch
        {
            AvatarSize.Full => summary.AvatarUrlFull,
            AvatarSize.Medium => summary.AvatarUrlMedium,
            _ => summary.AvatarUrlSmall
        };
    }
}

public interface IPlayer : ITranslationArgument
{
    public ulong Steam64 { get; }
}

public struct OfflinePlayer : IPlayer
{
    private readonly ulong _s64;
    private PlayerNames? _names;
    public ulong Steam64 => _s64;
    public OfflinePlayer(ulong steam64)
    {
        _s64 = steam64;
        _names = null;
    }
    public OfflinePlayer(in PlayerNames names)
    {
        _s64 = names.Steam64;
        _names = names;
    }
    public async ValueTask CacheUsernames(CancellationToken token = default)
    {
        if (!TryCacheLocal())
            _names = await F.GetPlayerOriginalNamesAsync(_s64, token).ConfigureAwait(false);
    }
    public bool TryCacheLocal()
    {
        UCPlayer? pl = UCPlayer.FromID(Steam64);
        if (pl != null)
            _names = pl.Name;
        return _names.HasValue;
    }
    public string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        UCPlayer? pl = UCPlayer.FromID(Steam64);
        if (format is null || !_names.HasValue) goto end;
        PlayerNames names = _names.Value;

        if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return names.CharacterName;
        if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal))
            return names.NickName;
        if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return names.PlayerName;
        if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal))
            goto end;
        string hex = TeamManager.GetTeamHexColor(pl is null || !pl.IsOnline ? (PlayerSave.TryReadSaveFile(_s64, out PlayerSave save) ? save.Team : 0) : pl.GetTeam());
        if (format.Equals(UCPlayer.COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, names.CharacterName, flags);
        if (format.Equals(UCPlayer.COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, names.NickName, flags);
        if (format.Equals(UCPlayer.COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, names.PlayerName, flags);
        if (format.Equals(UCPlayer.COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, _s64.ToString(culture ?? Data.LocalLocale), flags);
        end:
        return _s64.ToString(culture ?? Data.LocalLocale);
    }
}
public class UCPlayerLocale
{
    public static event Action<UCPlayer>? OnLocaleUpdated;

    private PlayerLanguagePreferences _preferences;
    private readonly bool _init;

    public UCPlayer Player { get; }
    public string Language => LanguageInfo.LanguageCode;
    public CultureInfo CultureInfo { get; private set; }
    internal bool PreferencesIsDirty { get; set; }
    public NumberFormatInfo ParseFormat { get; set; }
    public PlayerLanguagePreferences Preferences
    {
        get => _preferences;
        set
        {
            LanguageInfo info = Data.LanguageDataStore.GetInfoCached(value.Language) ?? Localization.GetDefaultLanguage();
            bool updated = false;

            IsDefaultLanguage = info.LanguageCode.Equals(L.Default, StringComparison.OrdinalIgnoreCase);

            if (!(value.CultureCode != null && Localization.TryGetCultureInfo(value.CultureCode, out CultureInfo culture)) &&
                !(info is { DefaultCultureCode: { } defaultCultureName } && Localization.TryGetCultureInfo(defaultCultureName, out culture)))
            {
                culture = Data.LocalLocale;
            }

            if (_init && (CultureInfo == null || !CultureInfo.Name.Equals(culture.Name, StringComparison.Ordinal)))
            {
                L.Log($"Updated culture for {Player}: {CultureInfo?.DisplayName ?? "null"} -> {culture.DisplayName}.");
                updated = true;
            }

            CultureInfo = culture;
            ParseFormat = value.UseCultureForCommandInput ? culture.NumberFormat : Data.LocalLocale.NumberFormat;
            
            if (_init && LanguageInfo != info)
            {
                L.Log($"Updated language for {Player}: {LanguageInfo?.DisplayName ?? "null"} -> {info.DisplayName}.");
                updated = true;
            }

            LanguageInfo = info;

            IsDefaultCulture = CultureInfo.Name.Equals(Data.LocalLocale.Name, StringComparison.Ordinal);

            _preferences = value;

            if (updated)
                InvokeOnLocaleUpdated(Player);
        }
    }

    public LanguageInfo LanguageInfo { get; private set; }
    public bool IsDefaultLanguage { get; private set; }
    public bool IsDefaultCulture { get; private set; }
    public UCPlayerLocale(UCPlayer player, PlayerLanguagePreferences preferences)
    {
        Player = player;
        Preferences = preferences;
        _init = true;
    }
    internal Task Apply(CancellationToken token = default)
    {
        Preferences = Preferences;
        PreferencesIsDirty = false;
        return Data.LanguageDataStore.UpdateLanguagePreferences(Preferences, token);
    }
    internal Task Update(string? language, CultureInfo? culture, bool holdSave = false, CancellationToken token = default)
    {
        bool save = false;
        if (culture != null && !culture.Name.Equals(CultureInfo.Name, StringComparison.Ordinal))
        {
            L.Log($"Updated culture for {Player}: {CultureInfo.DisplayName} -> {culture.DisplayName}.");
            ActionLog.Add(ActionLogType.ChangeCulture, CultureInfo.Name + " >> " + culture.Name, Player);
            CultureInfo = culture;
            Preferences.CultureCode = culture.Name;
            IsDefaultCulture = culture.Name.Equals(Data.LocalLocale.Name, StringComparison.Ordinal);
            ParseFormat = Preferences.UseCultureForCommandInput ? culture.NumberFormat : Data.LocalLocale.NumberFormat;
            save = true;
        }

        if (language != null && Data.LanguageDataStore.GetInfoCached(language) is { } languageInfo && !languageInfo.LanguageCode.Equals(LanguageInfo.LanguageCode, StringComparison.Ordinal))
        {
            L.Log($"Updated language for {Player}: {LanguageInfo.DisplayName} -> {languageInfo.DisplayName}.");
            ActionLog.Add(ActionLogType.ChangeLanguage, LanguageInfo.LanguageCode + " >> " + languageInfo.LanguageCode, Player);
            Preferences.Language = languageInfo.PrimaryKey;
            IsDefaultLanguage = languageInfo.LanguageCode.Equals(L.Default, StringComparison.OrdinalIgnoreCase);
            LanguageInfo = languageInfo;
            save = true;
        }

        if (save)
        {
            Preferences.LastUpdated = DateTimeOffset.UtcNow;
            if (holdSave)
            {
                InvokeOnLocaleUpdated(Player);
                PreferencesIsDirty = true;
            }
            else
            {
                Task task = Data.LanguageDataStore.UpdateLanguagePreferences(Preferences, token);
                InvokeOnLocaleUpdated(Player);
                PreferencesIsDirty = false;
                return task;
            }
        }

        return Task.CompletedTask;
    }

    private static void InvokeOnLocaleUpdated(UCPlayer player)
    {
        if (OnLocaleUpdated == null)
            return;
        // ReSharper disable once ConstantConditionalAccessQualifier
        if (UCWarfare.IsMainThread)
        {
            try
            {
                OnLocaleUpdated.Invoke(player);
            }
            catch (Exception ex)
            {
                L.LogError($"Error updating locale for {player}.");
                L.LogError(ex);
            }
        }
        else
        {
            UCWarfare.RunOnMainThread(() =>
            {
                try
                {
                    OnLocaleUpdated?.Invoke(player);
                }
                catch (Exception ex)
                {
                    L.LogError($"Error updating locale for {player}.");
                    L.LogError(ex);
                }
            });
        }
    }
}

public class PlayerSave
{
    public const uint DataVersion = 5;
    public readonly ulong Steam64;
    [CommandSettable]
    public ulong Team;
    [CommandSettable]
    public string KitName = string.Empty;
    public string SquadName = string.Empty;
    public ulong SquadLeader;
    public bool SquadWasLocked;
    [CommandSettable]
    public bool HasQueueSkip;
    [CommandSettable]
    public long LastGame;
    [CommandSettable]
    public bool ShouldRespawnOnJoin;
    [CommandSettable]
    public bool IsOtherDonator;
    [CommandSettable]
    public bool IMGUI;
    [CommandSettable]
    public bool TrackQuests = true;
    [CommandSettable]
    public bool WasNitroBoosting;
    public PlayerSave(ulong s64)
    {
        Steam64 = s64;
    }
    public PlayerSave(UCPlayer player)
    {
        Steam64 = player.Steam64;
        Apply(player);
    }
    internal void Apply(UCPlayer player)
    {
        if (player.Steam64 != Steam64)
            throw new ArgumentException("Player does not own this save.", nameof(player));

        Team = player.GetTeam();
        KitName = player.ActiveKit?.Item?.Id ?? string.Empty;
        if (player.Squad != null && player.Squad.Leader.Steam64 != Steam64)
        {
            SquadName = player.Squad.Name;
            SquadLeader = player.Squad.Leader.Steam64;
            SquadWasLocked = player.Squad.IsLocked;
        }
        LastGame = Data.Gamemode == null ? 0 : Data.Gamemode.GameID;
    }
    /// <summary>Players / 76561198267927009_0 / Uncreated_S2 / PlayerSave.dat</summary>
    private static string GetPath(ulong steam64) => Path.DirectorySeparatorChar + Path.Combine("Players",
        steam64.ToString(Data.AdminLocale) + "_0", "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.AdminLocale),
        "PlayerSave.dat");
    public static void WriteToSaveFile(PlayerSave save)
    {
        ThreadUtil.assertIsGameThread();
        Block block = new Block();
        block.writeUInt32(DataVersion);
        block.writeByte((byte)save.Team);
        block.writeString(save.KitName);
        block.writeString(save.SquadName);
        block.writeUInt64(save.SquadLeader);
        block.writeBoolean(save.SquadWasLocked);
        block.writeBoolean(save.HasQueueSkip);
        block.writeInt64(save.LastGame);
        block.writeBoolean(save.ShouldRespawnOnJoin);
        block.writeBoolean(save.IsOtherDonator);
        block.writeBoolean(save.IMGUI);
        block.writeBoolean(save.WasNitroBoosting);
        block.writeBoolean(save.TrackQuests);
        ServerSavedata.writeBlock(GetPath(save.Steam64), block);
    }
    public static bool HasPlayerSave(ulong player)
    {
        return PlayerManager.FromID(player) is not null || ServerSavedata.fileExists(GetPath(player));
    }
    public static bool TryReadSaveFile(ulong player, out PlayerSave save)
    {
        ThreadUtil.assertIsGameThread();
        UCPlayer? pl = PlayerManager.FromID(player);
        string path = GetPath(player);
        if (pl?.Save != null)
        {
            save = pl.Save;
            if (!ServerSavedata.fileExists(path))
                WriteToSaveFile(save);
            return true;
        }
        if (!ServerSavedata.fileExists(path))
        {
            save = null!;
            return false;
        }
        Block block = ServerSavedata.readBlock(path, 0);
        uint dv = block.readUInt32();
        save = new PlayerSave(player);
        if (dv > 0)
        {
            save.Team = block.readByte();
            save.KitName = block.readString();
            save.SquadName = block.readString();
            if (dv > 2)
            {
                save.SquadLeader = block.readUInt64();
                save.SquadWasLocked = block.readBoolean();
            }
            save.HasQueueSkip = block.readBoolean();
            save.LastGame = block.readInt64();
            save.ShouldRespawnOnJoin = block.readBoolean();
            save.IsOtherDonator = block.readBoolean();
            if (dv > 1)
            {
                save.IMGUI = block.readBoolean();
                if (dv > 3)
                {
                    save.WasNitroBoosting = block.readBoolean();
                    if (dv > 4)
                    {
                        save.TrackQuests = block.readBoolean();
                    }
                }
            }
        }
        return true;
    }
}
