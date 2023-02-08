﻿using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using UnityEngine;

namespace Uncreated.Warfare;

public sealed class UCPlayer : IPlayer, IComparable<UCPlayer>, IEquatable<UCPlayer>
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
    public static readonly UnturnedUI MutedUI = new UnturnedUI(15623, Gamemode.Config.UIMuted, false, false);
    public static readonly UnturnedUI LoadingUI = new UnturnedUI(15624, Gamemode.Config.UILoading, false, false, false);
    public readonly SemaphoreSlim PurchaseSync = new SemaphoreSlim(1, 1);
    public readonly UCPlayerKeys Keys;
    public readonly UCPlayerEvents Events;
    public KitMenuUIData KitMenuData;
    public readonly ulong Steam64;
    public volatile bool HasInitedOnce;
    public volatile bool HasDownloadedKits;
    public volatile bool HasDownloadedXP;
    public volatile bool IsDownloadingXP;
    public volatile bool IsDownloadingKits;
    public volatile bool Loading;
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
    public Class KitClass;
    public Branch Branch;
    public EMuteType MuteType;
    public DateTime TimeUnmuted;
    public Squad? Squad;
    public TeamSelectorData? TeamSelectorData;
    public Coroutine? StorageCoroutine;
    public Ranks.RankStatus[]? RankData;
    public List<SqlItem<Kit>>? AccessibleKits;
    public IBuff?[] ActiveBuffs = new IBuff?[BuffUI.MaxBuffs];
    public List<Trait> ActiveTraits = new List<Trait>(8);
    internal Action<byte, ItemJar> SendItemRemove;
    internal List<Guid>? CompletedQuests;
    internal bool ModalNeeded;
    private readonly CancellationTokenSource _disconnectTokenSrc;
    private int _multVersion = -1;
    private bool _isTalking;
    private bool _isOnline;
    private bool _lastMuted;
    private float _multCache = 1f;
    private string? _lang;
    private CultureInfo? _locale;
    private EAdminType? _pLvl;
    private RankData? _rank;
    private PlayerNames _cachedName;
    public UCPlayer(CSteamID steamID, Player player, string characterName, string nickName, bool donator, CancellationTokenSource pendingSrc, PlayerSave save)
    {
        Steam64 = steamID.m_SteamID;
        Squad = null;
        Player = player;
        CSteamID = steamID;
        Save = save;
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
    }
    ~UCPlayer()
    {
        PurchaseSync.Dispose();
    }
    public enum NameSearch : byte
    {
        CharacterName,
        NickName,
        PlayerName
    }
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
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
    public InteractableVehicle? CurrentVehicle => Player.movement.getVehicle();
    public bool IsInVehicle => CurrentVehicle != null;
    public bool IsDriver => CurrentVehicle != null && CurrentVehicle.passengers.Length > 0 && CurrentVehicle.passengers[0].player != null && CurrentVehicle.passengers[0].player.playerID.steamID.m_SteamID == Steam64;
    public bool HasKit => ActiveKit?.Item is not null;
    bool IEquatable<UCPlayer>.Equals(UCPlayer other) => other == this || other.Steam64 == Steam64; 
    public SteamPlayer SteamPlayer => Player.channel.owner;
    public PlayerSave Save { get; }
    public Player Player { get; internal set; }
    public CSteamID CSteamID { get; internal set; }
    public ITransportConnection Connection => Player.channel.owner.transportConnection!;
    public EffectAsset? LastPing { get; internal set; }
    ulong IPlayer.Steam64 => Steam64;
    public bool IsAdmin => Player.channel.owner.isAdmin;
    public bool IsTeam1 => Player.quests.groupID.m_SteamID == TeamManager.Team1ID;
    public bool IsTeam2 => Player.quests.groupID.m_SteamID == TeamManager.Team2ID;
    public string Language => _lang ??= Localization.GetLang(Steam64);
    public CultureInfo Culture => _locale ??= LanguageAliasSet.GetCultureInfo(Language);
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
    public bool IsOfficer => RankData != null && RankData.Length > OfficerStorage.OFFICER_RANK_ORDER && RankData[OfficerStorage.OFFICER_RANK_ORDER].IsCompelete;
    public RankData Rank
    {
        get
        {
            if (!_rank.HasValue)
                return default;
            return _rank.Value;
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
        get => Rank.TotalXP;
        set => _rank = new RankData(value);
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
            PurchaseSync.Dispose();
        }
    }
    public static UCPlayer? FromID(ulong steamID)
    {
        if (steamID == 0) return null;
        return PlayerManager.FromID(steamID);
        //return PlayerManager.OnlinePlayers.Find(p => p != null && p.Steam64 == steamID);
    }
    public static UCPlayer? FromCSteamID(CSteamID steamID) =>
        steamID == default ? null : FromID(steamID.m_SteamID);
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
            s.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            s.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            s.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
        if (includeContains && player == null)
        {
            player = PlayerManager.OnlinePlayers.Find(s =>
                s.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1);
        }
        return player;
    }
    public static UCPlayer? FromName(string name, bool includeContains, IEnumerable<UCPlayer> selection)
    {
        if (name == null) return null;
        UCPlayer? player = selection.FirstOrDefault(
            s =>
            s.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            s.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            s.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
        if (includeContains && player == null)
        {
            player = selection.FirstOrDefault(s =>
                s.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1 ||
                s.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1);
        }
        return player;
    }
    /// <summary>Slow, use rarely.</summary>
    public static UCPlayer? FromName(string name, NameSearch type)
    {
        if (type == NameSearch.CharacterName)
        {
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.characterName.Length))
            {
                if (current.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.nickName.Length))
            {
                if (current.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.playerName.Length))
            {
                if (current.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            return null;
        }
        else if (type == NameSearch.NickName)
        {
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.nickName.Length))
            {
                if (current.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.characterName.Length))
            {
                if (current.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.playerName.Length))
            {
                if (current.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            return null;
        }
        else if (type == NameSearch.PlayerName)
        {
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.playerName.Length))
            {
                if (current.Player.channel.owner.playerID.playerName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.nickName.Length))
            {
                if (current.Player.channel.owner.playerID.nickName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                    return current;
            }
            foreach (UCPlayer current in PlayerManager.OnlinePlayers.OrderBy(x => x.Player.channel.owner.playerID.characterName.Length))
            {
                if (current.Player.channel.owner.playerID.characterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
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
        if (kit?.Item == null)
        {
            ActiveKit = null;
            KitClass = Class.None;
        }
        else
        {
            ActiveKit = kit;
            KitClass = kit.Item.Class;
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
        if (UCWarfare.IsMainThread)
            ApplyIntl();
        else
            UCWarfare.RunOnMainThread(ApplyIntl);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyIntl() => PlayerManager.ApplyTo(this);
    public bool IsOnFOB(out FOB fob)
    {
        return FOB.IsOnFOB(this, out fob);
    }
    public int CompareTo(UCPlayer obj) => Steam64.CompareTo(obj.Steam64);
    private bool DownloadKitsSpinWaitCheck() => HasDownloadedKits;
    public async Task DownloadKits(bool @lock, CancellationToken token = default)
    {
        if (IsDownloadingKits)
        {
            UCWarfare.SpinWaitUntil(DownloadKitsSpinWaitCheck, 2500);
            return;
        }
        IsDownloadingKits = true;
        if (@lock)
            await PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            KitManager? singleton = KitManager.GetSingletonQuick();
            if (singleton == null)
                throw new SingletonUnloadedException(typeof(KitManager));
            List<int> kits = new List<int>();
            await Data.AdminSql.QueryAsync("SELECT `Kit` FROM `kit_access` WHERE `Steam64` = @0;",
                new object[] { Steam64 },
                reader =>
                {
                    kits.Add(reader.GetInt32(0));
                }, token).ConfigureAwait(false);
            AccessibleKits = new List<SqlItem<Kit>>(kits.Count);
            await singleton.WaitAsync(token).ConfigureAwait(false);
            try
            {
                for (int i = 0; i < kits.Count; ++i)
                {
                    SqlItem<Kit>? proxy = singleton.FindProxyNoLock(kits[i]);
                    if (proxy is not null)
                        AccessibleKits.Add(proxy);
                }
            }
            finally
            {
                singleton.Release();
            }
        }
        finally
        {
            HasDownloadedKits = true;
            IsDownloadingKits = false;
            if (@lock)
                PurchaseSync.Release();
        }
    }

    public void SetCosmeticStates(bool state)
    {
        ThreadUtil.assertIsGameThread();
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, state);
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
                            L.LogDebug($"Setting override: {s}.");
                            s.ServerSet(this);
                        }
                        else
                            L.LogDebug($"Override already set: {s}.");
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
                            L.LogDebug($"Setting server default: {s}.");
                            s.ServerSet(this);
                        }
                        else
                            L.LogDebug($"Server default already set: {s}.");
                        goto c;
                    }
                }

                byte defaultLvl = 0;
                if (Provider.modeConfigData.Players.Spawn_With_Max_Skills ||
                    specIndex == (int)EPlayerSpeciality.OFFENSE &&
                    (EPlayerOffense)skillIndex is
                    EPlayerOffense.CARDIO or EPlayerOffense.EXERCISE or
                    EPlayerOffense.DIVING or EPlayerOffense.PARKOUR &&
                    Provider.modeConfigData.Players.Spawn_With_Stamina_Skills)
                {
                    defaultLvl = skill.max;
                }
                else if (Level.getAsset() is { skillRules: { } } asset)
                {
                    if (asset.skillRules.Length > specIndex && asset.skillRules[specIndex].Length > skillIndex)
                    {
                        LevelAsset.SkillRule rule = asset.skillRules[specIndex][skillIndex];
                        if (rule != null)
                            defaultLvl = (byte)rule.defaultLevel;
                    }
                }

                if (skill.level != defaultLvl)
                {
                    Player.skills.ServerSetSkillLevel(specIndex, skillIndex, defaultLvl);
                    L.LogDebug($"Setting game default: {new Skillset((EPlayerSpeciality)specIndex, (byte)skillIndex, defaultLvl)}.");
                }
                else
                {
                    L.LogDebug($"Game default already set: {new Skillset((EPlayerSpeciality)specIndex, (byte)skillIndex, defaultLvl)}.");
                }
                c:;
            }
        }
    }
    public override string ToString() => Name.PlayerName + " [" + Steam64.ToString("G17", Data.AdminLocale) + "]";
    internal void ResetPermissionLevel() => _pLvl = null;
    internal void Update()
    {
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
        _rank = new RankData((int)xp);
        CachedCredits = (int)credits;
    }
    internal void OnLanguageChanged()
    {
        _lang = null;
        _locale = null;
    }

    private class EqualityComparer : IEqualityComparer<UCPlayer>
    {
        bool IEqualityComparer<UCPlayer>.Equals(UCPlayer x, UCPlayer y) => x == y || x.Steam64 == y.Steam64;
        int IEqualityComparer<UCPlayer>.GetHashCode(UCPlayer obj) => obj.Steam64.GetHashCode();
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
    public OfflinePlayer(ulong steam64, bool cacheUsernames = false)
    {
        _s64 = steam64;
        if (cacheUsernames)
            _names = F.GetPlayerName(steam64);
        else
            _names = null;
    }
    public OfflinePlayer(in PlayerNames names)
    {
        _s64 = names.Steam64;
        _names = names;
    }
    public async Task CacheUsernames()
    {
        _names = await F.GetPlayerOriginalNamesAsync(_s64).ConfigureAwait(false);
    }
    public string Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is null) goto end;

        if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerName(_s64)).CharacterName;
        if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerName(_s64)).NickName;
        if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerName(_s64)).PlayerName;
        if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal))
            return _s64.ToString(Data.AdminLocale);
        UCPlayer? pl = UCPlayer.FromID(Steam64);
        string hex = TeamManager.GetTeamHexColor(pl is null || !pl.IsOnline ? (PlayerSave.TryReadSaveFile(_s64, out PlayerSave save) ? save.Team : 0) : pl.GetTeam());
        if (format.Equals(UCPlayer.COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, (_names ??= F.GetPlayerName(_s64)).CharacterName, flags);
        if (format.Equals(UCPlayer.COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, (_names ??= F.GetPlayerName(_s64)).NickName, flags);
        if (format.Equals(UCPlayer.COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, (_names ??= F.GetPlayerName(_s64)).PlayerName, flags);
        if (format.Equals(UCPlayer.COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, _s64.ToString(Data.AdminLocale), flags);
        end:
        return (_names ??= Data.DatabaseManager.GetUsernames(_s64)).CharacterName;
    }
}
public class UCPlayerLocale // todo implement
{
    public UCPlayer Player { get; }
    public string Language { get; private set; }
    public IFormatProvider Format { get; private set; }
    public UCPlayerLocale(UCPlayer player, string language)
    {
        Player = player;
        if (Localization.TryGetLangData(language, out string langName, out IFormatProvider format))
        {
            this.Format = format;
            this.Language = langName;
        }
    }
    public UCPlayerLocale(UCPlayer player) : this(player, L.Default) { }
    internal void Update(string language)
    {
        if (Localization.TryGetLangData(language, out string langName, out IFormatProvider format))
        {
            this.Format = format;
            this.Language = langName;
        }
    }
}

public class PlayerSave
{
    public const uint DataVersion = 3;
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
    public PlayerSave(ulong s64)
    {
        this.Steam64 = s64;
    }
    public PlayerSave(UCPlayer player)
    {
        this.Steam64 = player.Steam64;
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
        ServerSavedata.writeBlock(GetPath(save.Steam64), block);
    }
    public static bool HasPlayerSave(ulong player)
    {
        return PlayerManager.FromID(player) is not null || ServerSavedata.fileExists(GetPath(player));
    }
    public static bool TryReadSaveFile(ulong player, out PlayerSave save)
    {
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
        ThreadUtil.assertIsGameThread();
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
            }
        }
        return true;
    }
}