using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
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
    public bool IsOnline;
    public float LastSpoken;
    public string CharacterName;
    public string NickName;
    public string KitName;
    public string? MuteReason;
    public EClass KitClass;
    public EBranch Branch;
    public EMuteType MuteType;
    public DateTime TimeUnmuted;
    public Kit? Kit;
    public Squad? Squad;
    public TeamSelectorData? TeamSelectorData;
    public Coroutine? StorageCoroutine;
    public Ranks.RankStatus[]? RankData;
    public List<string>? AccessibleKits;
    public IBuff?[] ActiveBuffs = new IBuff?[6];
    public List<Trait> ActiveTraits = new List<Trait>(8);
    internal bool _isLeaving;
    internal Action<byte, ItemJar> SendItemRemove;
    internal List<Guid>? CompletedQuests;
    private int _multVersion = -1;
    private bool _isTalking;
    private bool _lastMuted;
    private float _multCache = 1f;
    private string? _lang;
    private EAdminType? _pLvl;
    private RankData? _rank;
    private FPlayerName _cachedName = FPlayerName.Nil;
    public UCPlayer(CSteamID steamID, string kitName, Player player, string characterName, string nickName, bool donator)
    {
        Steam64 = steamID.m_SteamID;
        if (KitManager.KitExists(kitName, out Kit kit))
        {
            KitClass = kit.Class;
            Branch = kit.Branch;
        }
        else
        {
            KitClass = EClass.NONE;
            Branch = EBranch.DEFAULT;
        }
        KitName = kitName;
        KitManager.KitExists(kitName, out Kit);
        Squad = null;
        Player = player;
        CSteamID = steamID;
        CharacterName = characterName;
        NickName = nickName;
        IsOnline = true;
        IsOtherDonator = donator;
        LifeCounter = 0;
        SuppliesUnloaded = 0;
        CurrentMarkers = new List<SpottedComponent>();
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
    public enum ENameSearchType : byte
    {
        CHARACTER_NAME,
        NICK_NAME,
        PLAYER_NAME
    }
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is null) goto end;
        if (format.Equals(CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Name.CharacterName;
        if (format.Equals(NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Name.NickName;
        if (format.Equals(PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Name.PlayerName;
        if (format.Equals(STEAM_64_FORMAT, StringComparison.Ordinal))
            return Steam64.ToString(Data.Locale);

        string hex = TeamManager.GetTeamHexColor(this.GetTeam());
        if (format.Equals(COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Name.CharacterName, flags);
        if (format.Equals(COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Name.NickName, flags);
        if (format.Equals(COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Name.PlayerName, flags);
        if (format.Equals(COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, Steam64.ToString(Data.Locale), flags);
        end:
        return Name.CharacterName;
    }
    public InteractableVehicle? CurrentVehicle => Player.movement.getVehicle();
    public bool IsInVehicle => CurrentVehicle != null;
    public bool IsDriver => CurrentVehicle != null && CurrentVehicle.passengers.Length > 0 && CurrentVehicle.passengers[0].player != null && CurrentVehicle.passengers[0].player.playerID.steamID.m_SteamID == Steam64;
    bool IEquatable<UCPlayer>.Equals(UCPlayer other) => other == this || other.Steam64 == Steam64; 
    public SteamPlayer SteamPlayer => Player.channel.owner;
    public Player Player { get; internal set; }
    public CSteamID CSteamID { get; internal set; }
    public ITransportConnection Connection => Player.channel.owner.transportConnection!;
    public ushort LastPingID { get; internal set; }
    ulong IPlayer.Steam64 => Steam64;
    public bool IsAdmin => Player.channel.owner.isAdmin;
    public bool IsTeam1 => Player.quests.groupID.m_SteamID == TeamManager.Team1ID;
    public bool IsTeam2 => Player.quests.groupID.m_SteamID == TeamManager.Team2ID;
    public string Language => _lang ??= Localization.GetLang(Steam64);
    public bool IsTalking => !_lastMuted && _isTalking && IsOnline;
    public bool IsLeaving => _isLeaving;
    public bool VanishMode { get; set; }
    public bool IsActionMenuOpen { get; internal set; }
    public bool IsOtherDonator { get; set; }
    public bool GodMode { get; set; }
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
    public FPlayerName Name
    {
        get
        {
            if (_cachedName == FPlayerName.Nil)
                _cachedName = new FPlayerName
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

    public ushort MarkerID
    {
        get
        {
            EffectAsset asset;
            if (SquadManager.Config.Classes == null || SquadManager.Config.Classes.Length == 0)
                return 36101;
            for (int i = 0; i < SquadManager.Config.Classes.Length; ++i)
            {
                ref ClassConfig c = ref SquadManager.Config.Classes[i];
                if (c.Class == KitClass)
                {
                    if (c.MarkerEffect.ValidReference(out asset))
                        return asset.id;
                    else break;
                }
            }

            if (SquadManager.Config.Classes[0].MarkerEffect.ValidReference(out asset))
                return asset.id;
            return 36101;
        }
    }
    public ushort SquadLeaderMarkerID
    {
        get
        {
            EffectAsset asset;
            if (SquadManager.Config.Classes == null || SquadManager.Config.Classes.Length == 0)
                return 36131;
            for (int i = 0; i < SquadManager.Config.Classes.Length; ++i)
            {
                ref ClassConfig c = ref SquadManager.Config.Classes[i];
                if (c.Class == KitClass)
                {
                    if (c.SquadLeaderMarkerEffect.ValidReference(out asset))
                        return asset.id;
                    else break;
                }
            }

            if (SquadManager.Config.Classes[0].SquadLeaderMarkerEffect.ValidReference(out asset))
                return asset.id;
            return 36131;
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
    public static UCPlayer? FromName(string name, ENameSearchType type)
    {
        if (type == ENameSearchType.CHARACTER_NAME)
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
        else if (type == ENameSearchType.NICK_NAME)
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
        else if (type == ENameSearchType.PLAYER_NAME)
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
        else return FromName(name, ENameSearchType.CHARACTER_NAME);
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

        if (CurrentMarkers.Count(x => !x.UAVMode) == 3)
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

    public void ChangeKit(Kit kit)
    {
        KitName = kit.Name;
        Kit = kit;
        KitClass = kit.Class;
        PlayerManager.ApplyTo(this);
    }
    public ushort GetMarkerID() => Squad == null || Squad.Leader == null || Squad.Leader.Steam64 != Steam64 ? MarkerID : SquadLeaderMarkerID;
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
    public bool IsOnFOB(out FOB fob)
    {
        return FOB.IsOnFOB(this, out fob);
    }
    public int CompareTo(UCPlayer obj) => Steam64.CompareTo(obj.Steam64);
    private bool DownloadKitsSpinWaitCheck() => HasDownloadedKits;
    public async Task DownloadKits(bool @lock)
    {
        if (IsDownloadingKits)
        {
            UCWarfare.SpinWaitUntil(DownloadKitsSpinWaitCheck, 2500);
            return;
        }
        IsDownloadingKits = true;
        if (@lock)
            await PurchaseSync.WaitAsync();
        try
        {
            KitManager singleton = KitManager.GetSingleton();
            List<string> kits = new List<string>();
            await Data.AdminSql.QueryAsync("SELECT `Kit` FROM `kit_access` WHERE `Steam64` = @0;",
                new object[] { Steam64 },
                reader =>
                {
                    if (singleton.Kits.TryGetValue(reader.GetInt32(0), out Kit kit))
                        kits.Add(kit.Name);
                });
            AccessibleKits = kits;
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
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, state);
    }
    public override string ToString() => Name.PlayerName + " [" + Steam64.ToString("G17", Data.Locale) + "]";
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
    internal void OnLanguageChanged() => _lang = null;
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
    private FPlayerName? _names;
    public ulong Steam64 => _s64;
    public OfflinePlayer(ulong steam64, bool cacheUsernames = false)
    {
        _s64 = steam64;
        if (cacheUsernames)
            _names = F.GetPlayerName(steam64);
        else
            _names = null;
    }
    public OfflinePlayer(in FPlayerName names)
    {
        _s64 = names.Steam64;
        _names = names;
    }
    public async Task CacheUsernames()
    {
        _names = await F.GetPlayerOriginalNamesAsync(_s64).ConfigureAwait(false);
    }
    public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is null) goto end;

        if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerName(_s64)).CharacterName;
        if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerName(_s64)).NickName;
        if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerName(_s64)).PlayerName;
        if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal))
            return _s64.ToString(Data.Locale);
        UCPlayer? pl = UCPlayer.FromID(Steam64);
        string hex = TeamManager.GetTeamHexColor(pl is null || !pl.IsOnline ? (PlayerSave.TryReadSaveFile(_s64, out PlayerSave save) ? save.Team : 0) : pl.GetTeam());
        if (format.Equals(UCPlayer.COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, (_names ??= F.GetPlayerName(_s64)).CharacterName, flags);
        if (format.Equals(UCPlayer.COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, (_names ??= F.GetPlayerName(_s64)).NickName, flags);
        if (format.Equals(UCPlayer.COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, (_names ??= F.GetPlayerName(_s64)).PlayerName, flags);
        if (format.Equals(UCPlayer.COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(hex, _s64.ToString(Data.Locale), flags);
        end:
        return (_names ??= Data.DatabaseManager.GetUsernames(_s64)).CharacterName;
    }
}
public class PlayerSave
{
    public const uint CURRENT_DATA_VERSION = 1;
    public readonly ulong Steam64;
    [CommandSettable]
    public ulong Team;
    [CommandSettable]
    public string KitName;
    [CommandSettable]
    public string SquadName;
    [CommandSettable]
    public bool HasQueueSkip;
    [CommandSettable]
    public long LastGame;
    [CommandSettable]
    public bool ShouldRespawnOnJoin;
    [CommandSettable]
    public bool IsOtherDonator;
    public PlayerSave(ulong s64)
    {
        this.Steam64 = s64;
        Team = 0;
        KitName = string.Empty;
        SquadName = string.Empty;
        HasQueueSkip = false;
        LastGame = 0;
        ShouldRespawnOnJoin = false;
        IsOtherDonator = false;
    }
    public PlayerSave()
    {
        this.Steam64 = 0;
        Team = 0;
        KitName = string.Empty;
        SquadName = string.Empty;
        HasQueueSkip = false;
        LastGame = 0;
        ShouldRespawnOnJoin = false;
        IsOtherDonator = false;
    }
    [JsonConstructor]
    public PlayerSave(ulong s64, ulong team, string kitName, string squadName, bool hasQueueSkip, long lastGame, bool respawnOnJoin, bool isOtherDonor)
    {
        this.Steam64 = s64;
        this.Team = team;
        this.KitName = kitName;
        this.SquadName = squadName;
        this.HasQueueSkip = hasQueueSkip;
        this.LastGame = lastGame;
        this.ShouldRespawnOnJoin = respawnOnJoin;
        this.IsOtherDonator = isOtherDonor;
    }

    /// <summary>Players / 76561198267927009_0 / Uncreated_S2 / PlayerSave.dat</summary>
    private static string GetPath(ulong steam64) => Path.DirectorySeparatorChar + Path.Combine("Players",
        steam64.ToString(Data.Locale) + "_0", "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale),
        "PlayerSave.dat");
    public static void WriteToSaveFile(PlayerSave save)
    {
        Block block = new Block();
        block.writeUInt32(CURRENT_DATA_VERSION);
        block.writeByte((byte)save.Team);
        block.writeString(save.KitName);
        block.writeString(save.SquadName);
        block.writeBoolean(save.HasQueueSkip);
        block.writeInt64(save.LastGame);
        block.writeBoolean(save.ShouldRespawnOnJoin);
        block.writeBoolean(save.IsOtherDonator);
        ServerSavedata.writeBlock(GetPath(save.Steam64), block);
    }
    public static bool HasPlayerSave(ulong player) => ServerSavedata.fileExists(GetPath(player));
    public static bool TryReadSaveFile(ulong player, out PlayerSave save)
    {
        string path = GetPath(player);
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
            save.HasQueueSkip = block.readBoolean();
            save.LastGame = block.readInt64();
            save.ShouldRespawnOnJoin = block.readBoolean();
            save.IsOtherDonator = block.readBoolean();
        }
        else
        {
            save.KitName = string.Empty;
            save.SquadName = string.Empty;
        }
        return true;
    }
}
