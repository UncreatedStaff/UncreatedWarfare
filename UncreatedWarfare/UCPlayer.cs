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

public class UCPlayer : IPlayer
{
    private class EqualityComparer : IEqualityComparer<UCPlayer>
    {
        bool IEqualityComparer<UCPlayer>.Equals(UCPlayer x, UCPlayer y) => x == y || x.Steam64 == y.Steam64;
        int IEqualityComparer<UCPlayer>.GetHashCode(UCPlayer obj) => obj.Steam64.GetHashCode();
    }

    public static readonly IEqualityComparer<UCPlayer> Comparer = new EqualityComparer();
    public static readonly UnturnedUI MutedUI = new UnturnedUI(15623, Gamemode.Config.UI.MutedUI, false, false);
    public readonly ulong Steam64;
    public EClass KitClass;
    public EBranch Branch;
    public string KitName;
    public Kit? Kit;
    public Squad? Squad;
    public volatile bool HasDownloadedKits;
    public volatile bool IsDownloadingKits;
    public TeamSelectorData? TeamSelectorData;
    public readonly SemaphoreSlim PurchaseSync = new SemaphoreSlim(1, 5);
    public readonly UCPlayerKeys Keys;
    private string? _lang;
    public string Language => _lang ??= Localization.GetLang(Steam64);
    public Player Player { get; internal set; }
    public bool IsTalking => !lastMuted && isTalking && IsOnline;
    public CSteamID CSteamID { get; internal set; }
    public string CharacterName;
    public string NickName;
    public bool HasUIHidden = false;
    ulong IPlayer.Steam64 => Steam64;
    public bool IsAdmin => Player.channel.owner.isAdmin;
    public ITransportConnection Connection => Player.channel.owner.transportConnection;
    public Coroutine? StorageCoroutine;
    public Ranks.RankStatus[]? RankData;
    public FPlayerName Name
    {
        get
        {
            if (cachedName == FPlayerName.Nil)
                cachedName = F.GetPlayerOriginalNames(this.Player);
            return cachedName;
        }
    }
    internal Action<byte, ItemJar> SendItemRemove;
    public DateTime TimeUnmuted;
    public string? MuteReason;
    public EMuteType MuteType;
    private FPlayerName cachedName = FPlayerName.Nil;
    /// <summary><see langword="True"/> if rank order <see cref="OfficerStorage.OFFICER_RANK_ORDER"/> has been completed (Receiving officer pass from discord server).</summary>
    public bool IsOfficer => RankData != null && RankData.Length > OfficerStorage.OFFICER_RANK_ORDER && RankData[OfficerStorage.OFFICER_RANK_ORDER].IsCompelete;
    public int CachedCredits;
    public int CachedXP
    {
        get
        {
            return Rank.TotalXP;
        }
        set
        {
            _rank = new RankData(value);
        }
    }

    public Dictionary<Buff, float> ShovelSpeedMultipliers { get; } = new Dictionary<Buff, float>(6);
    
    private static readonly InstanceGetter<Dictionary<Buff, float>, int> _versionGetter = 
        F.GenerateInstanceGetter<Dictionary<Buff, float>, int>("version", BindingFlags.NonPublic | BindingFlags.Instance);

    private int multVersion = -1;
    private float _multCache = 1f;
    public float ShovelSpeedMultiplier
    {
        get
        {
            int version = _versionGetter(ShovelSpeedMultipliers);
            if (version == multVersion)
                return _multCache;
            float max = 0f;
            foreach (float fl in ShovelSpeedMultipliers.Values)
                if (fl > max) max = fl;
            if (max <= 0f)
                max = 1f;
            multVersion = version;
            return _multCache = max;
        }
    }
    private RankData? _rank;
    public RankData Rank
    {
        get
        {
            if (!_rank.HasValue)
                return default;
            return _rank.Value;
        }
    }
    public List<Kit>? AccessibleKits;
    public IBuff?[] ActiveBuffs = new IBuff?[6];
    public List<Trait> ActiveTraits = new List<Trait>(8);
    internal List<Guid>? _completedQuests;
    public float LastSpoken = 0f;

    private bool _otherDonator;
    public bool IsOtherDonator => _otherDonator;
    public bool GodMode { get => _godMode; set => _godMode = value; }
    private bool _godMode = false;
    public bool VanishMode { get => _vanishMode; set => _vanishMode = value; }
    private bool _vanishMode = false;
    public Vector3 Position
    {
        get
        {
            try
            {
                if (Player.transform == null)
                {
                    L.LogError("ERROR: Player transform was null");
                    L.Log($"Kicking {F.GetPlayerOriginalNames(Player).PlayerName} ({Steam64}) for null transform.", ConsoleColor.Cyan);
                    Provider.kick(Player.channel.owner.playerID.steamID, Localization.Translate(T.NullTransformKickMessage, this, UCWarfare.Config.DiscordInviteCode));
                    return Vector3.zero;
                }
                return Player.transform.position;
            }
            catch (NullReferenceException)
            {
                L.LogError("ERROR: Player transform was null");
                L.Log($"Kicking {F.GetPlayerOriginalNames(Player).PlayerName} ({Steam64}) for null transform.", ConsoleColor.Cyan);
                Provider.kick(Player.channel.owner.playerID.steamID, Localization.Translate(T.NullTransformKickMessage, this, UCWarfare.Config.DiscordInviteCode));
                return Vector3.zero;
            }
        }
    }
    public bool IsOnline;
    public int LifeCounter;

    public bool IsOnTeam
    {
        get
        {
            ulong team = this.GetTeam();
            return team == 1 || team == 2;
        }
    }
    public List<SpottedComponent> CurrentMarkers { get; private set; }
    public void ActivateMarker(SpottedComponent marker)
    {
        CurrentMarkers.Remove(marker);

        if (CurrentMarkers.Count == 3)
        {
            var oldest = CurrentMarkers.Last();
            oldest.Deactivate();
            CurrentMarkers.Remove(oldest);
        }

        CurrentMarkers.Insert(0, marker);
    }
    public void DeactivateMarker(SpottedComponent marker) => CurrentMarkers.Remove(marker);

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
    internal void OnLanguageChanged() => _lang = null;
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
    public enum ENameSearchType : byte
    {
        CHARACTER_NAME,
        NICK_NAME,
        PLAYER_NAME
    }
    public void ChangeKit(Kit kit)
    {
        KitName = kit.Name;
        Kit = kit;
        KitClass = kit.Class;
        PlayerManager.ApplyTo(this);
    }
    public ushort LastPingID { get; internal set; }
    public int SuppliesUnloaded;
    public SteamPlayer SteamPlayer => Player.channel.owner;
    public bool IsTeam1() => Player.quests.groupID.m_SteamID == TeamManager.Team1ID;
    public bool IsTeam2() => Player.quests.groupID.m_SteamID == TeamManager.Team2ID;

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
        _otherDonator = donator;
        LifeCounter = 0;
        SuppliesUnloaded = 0;
        CurrentMarkers = new List<SpottedComponent>();
        if (Data.UseFastKits)
        {
            try
            {
                SendItemRemove = (Action<byte, ItemJar>)(typeof(PlayerInventory).GetMethod("sendItemRemove", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Action<byte, ItemJar>), Player.inventory));
            }
            catch
            {
                L.LogError("Failed to get PlayerInventory.sendItemRemove for player " + characterName);
                Data.UseFastKits = false;
            }
        }
        Keys = new UCPlayerKeys(this);
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
    private EAdminType? _pLvl = null;
    internal void ResetPermissionLevel() => _pLvl = null;
    public ushort GetMarkerID() => Squad == null || Squad.Leader == null || Squad.Leader.Steam64 != Steam64 ? MarkerID : SquadLeaderMarkerID;
    public bool IsSquadLeader()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Squad is null)
            return false;

        return Squad.Leader?.Steam64 == Steam64;
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

    public bool IsNearOtherPlayer(UCPlayer player, float distance)
    {
        if (Player.life.isDead || Player.transform is null || player.Player.life.isDead || player.Player.transform is null)
            return false;

        return (Position - player.Position).sqrMagnitude < Math.Pow(distance, 2);
    }

    /// <summary>Gets some of the values from the playersave again.</summary>
    public static void Refresh(ulong Steam64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? pl = PlayerManager.OnlinePlayers?.FirstOrDefault(s => s.Steam64 == Steam64);
        if (pl == null) return;
        else if (PlayerManager.HasSave(Steam64, out PlayerSave save))
        {
            if (KitManager.KitExists(save.KitName, out Kit kit))
            {
                pl.ChangeKit(kit);
            }
            else
            {
                pl.KitClass = EClass.NONE;
                pl.Branch = EBranch.DEFAULT;
                pl.Kit = null;
            }
            pl.KitName = save.KitName;
            pl._otherDonator = save.IsOtherDonator;
        }
    }
    private bool isTalking = false;
    private bool lastMuted = false;
    
    internal void Update()
    {
        if (isTalking && Time.realtimeSinceStartup - LastSpoken > 0.5f)
        {
            isTalking = false;
            if (lastMuted)
            {
                MutedUI.ClearFromPlayer(Connection);
                lastMuted = false;
            }
        }
    }
    internal void OnUseVoice(bool isMuted)
    {
        float t = Time.realtimeSinceStartup;
        if (isMuted != lastMuted)
        {
            if (isMuted)
                MutedUI.SendToPlayer(Connection);
            else
                MutedUI.ClearFromPlayer(Connection);
            lastMuted = isMuted;
        }
        LastSpoken = t;
        isTalking = true;
    }
    public int CompareTo(object obj) => obj is UCPlayer player ? Steam64.CompareTo(player.Steam64) : -1;

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
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is null) goto end;
        if (format.Equals(CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return Name.CharacterName;
        else if (format.Equals(NICK_NAME_FORMAT, StringComparison.Ordinal))
            return Name.NickName;
        else if (format.Equals(PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return Name.PlayerName;
        else if (format.Equals(STEAM_64_FORMAT, StringComparison.Ordinal))
            return Steam64.ToString(Data.Locale);
        else
        {
            string hex = TeamManager.GetTeamHexColor(this.GetTeam());
            if (format.Equals(COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, Name.CharacterName, flags);
            else if (format.Equals(COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, Name.NickName, flags);
            else if (format.Equals(COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, Name.PlayerName, flags);
            else if (format.Equals(COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, Steam64.ToString(Data.Locale), flags);
        }
    end:
        return Name.CharacterName;
    }
    public async Task DownloadKits()
    {
        if (IsDownloadingKits)
        {
            SpinWait.SpinUntil(() => HasDownloadedKits, 2500);
            return;
        }
        else
        {
            AccessibleKits = await Data.DatabaseManager.GetAccessibleKits(Steam64);
            HasDownloadedKits = true;
            IsDownloadingKits = false;
        }
    }

    public void SetCosmeticStates(bool state)
    {
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, state);
        Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, state);
    }

    internal void UpdatePoints(uint xp, uint credits)
    {
        _rank = new RankData((int)xp);
        CachedCredits = (int)credits;
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
            _names = F.GetPlayerOriginalNames(steam64);
    }
    public OfflinePlayer(in FPlayerName names)
    {
        _s64 = names.Steam64;
        _names = names;
    }
    public async Task CacheUsernames()
    {
        _names = await F.GetPlayerOriginalNamesAsync(_s64);
    }
    public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is null) goto end;
        
        if (format.Equals(UCPlayer.CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerOriginalNames(_s64)).CharacterName;
        else if (format.Equals(UCPlayer.NICK_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerOriginalNames(_s64)).NickName;
        else if (format.Equals(UCPlayer.PLAYER_NAME_FORMAT, StringComparison.Ordinal))
            return (_names ??= F.GetPlayerOriginalNames(_s64)).PlayerName;
        else if (format.Equals(UCPlayer.STEAM_64_FORMAT, StringComparison.Ordinal))
            return _s64.ToString(Data.Locale);
        else
        {
            UCPlayer? pl = UCPlayer.FromID(Steam64);
            string hex = TeamManager.GetTeamHexColor(pl is null || !pl.IsOnline ? (PlayerSave.TryReadSaveFile(_s64, out PlayerSave save) ? save.Team : 0) : pl.GetTeam());
            if (format.Equals(UCPlayer.COLOR_CHARACTER_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, (_names ??= F.GetPlayerOriginalNames(_s64)).CharacterName, flags);
            else if (format.Equals(UCPlayer.COLOR_NICK_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, (_names ??= F.GetPlayerOriginalNames(_s64)).NickName, flags);
            else if (format.Equals(UCPlayer.COLOR_PLAYER_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, (_names ??= F.GetPlayerOriginalNames(_s64)).PlayerName, flags);
            else if (format.Equals(UCPlayer.COLOR_STEAM_64_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(hex, _s64.ToString(Data.Locale), flags);
        }
    end:
        return (_names ??= Data.DatabaseManager.GetUsernames(_s64)).CharacterName;
    }
}
public class PlayerSave
{
    public const uint CURRENT_DATA_VERSION = 1;
    public uint DATA_VERSION = CURRENT_DATA_VERSION;
    public readonly ulong Steam64;
    [JsonSettable]
    public ulong Team;
    [JsonSettable]
    public string KitName;
    [JsonSettable]
    public string SquadName;
    [JsonSettable]
    public bool HasQueueSkip;
    [JsonSettable]
    public long LastGame;
    [JsonSettable]
    public bool ShouldRespawnOnJoin;
    [JsonSettable]
    public bool IsOtherDonator;
    public PlayerSave(ulong Steam64)
    {
        this.Steam64 = Steam64;
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
    public PlayerSave(ulong Steam64, ulong Team, string KitName, string SquadName, bool HasQueueSkip, long LastGame, bool ShouldRespawnOnJoin, bool IsOtherDonator)
    {
        this.Steam64 = Steam64;
        this.Team = Team;
        this.KitName = KitName;
        this.SquadName = SquadName;
        this.HasQueueSkip = HasQueueSkip;
        this.LastGame = LastGame;
        this.ShouldRespawnOnJoin = ShouldRespawnOnJoin;
        this.IsOtherDonator = IsOtherDonator;
    }

    /// <summary>Players / 76561198267927009_0 / Uncreated_S2 / PlayerSave.dat</summary>
    private static string GetPath(ulong steam64) => Path.DirectorySeparatorChar + Path.Combine("Players",
        steam64.ToString(Data.Locale) + "_0", "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale),
        "PlayerSave.dat");
    public static void WriteToSaveFile(PlayerSave save)
    {
        Block block = new Block();
        block.writeUInt32(save.DATA_VERSION);
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
