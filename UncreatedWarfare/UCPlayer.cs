using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Networking.Encoding;
using Uncreated.Networking.Encoding.IO;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare
{
    public class UCPlayer : IRocketPlayer
    {
        public readonly ulong Steam64;
        public string Id => Steam64.ToString();
        public string DisplayName => Player.channel.owner.playerID.playerName;
        public bool IsAdmin => Player.channel.owner.isAdmin;
        public EClass KitClass;
        public EBranch Branch;
        public string KitName;
        public Squad? Squad;
        public Player Player { get; internal set; }
        public CSteamID CSteamID { get; internal set; }
        public string CharacterName;
        public string NickName;
        public ITransportConnection connection => Player.channel.owner.transportConnection;
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
        public DateTime TimeUnmuted;
        public string? MuteReason;
        public EMuteType MuteType;
        private FPlayerName cachedName = FPlayerName.Nil;
        /// <summary>[Unreliable]</summary>
        private MedalData _medals = MedalData.Nil;
        private Dictionary<EBranch, RankData>? _ranks;
        public Dictionary<EBranch, RankData> Ranks
        {
            get
            {
                if (_ranks == null)
                {
                    _ranks = new Dictionary<EBranch, RankData>(6);
                    RedownloadRanks();
                }
                return _ranks;
            }
        }
        public RankData CurrentRank
        {
            get
            {
                if (_ranks == null)
                {
                    _ranks = new Dictionary<EBranch, RankData>(6);
                    RedownloadRanks();
                }
                if (_ranks.TryGetValue(Branch, out RankData rank))
                    return rank;
                else
                {
                    RankData data = new RankData(Steam64, 0, Branch, this.GetTeam());
                    _ranks.Add(Branch, data);
                    return data;
                }
            }
        }
        public MedalData Medals
        {
            get
            {
                if (_medals.IsNil)
                    RedownloadMedals();
                return _medals;
            }
        }
        public bool IsOfficer { get => CurrentRank.OfficerTier > 0; }
        public void RedownloadRanks()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Dictionary<EBranch, int> xplevels = Data.DatabaseManager.GetAllXP(Steam64);
            if (_ranks != null)
                _ranks.Clear();
            else _ranks = new Dictionary<EBranch, RankData>(6);
            foreach (KeyValuePair<EBranch, int> entry in xplevels)
            {
                if (_ranks.ContainsKey(entry.Key))
                    _ranks.Remove(entry.Key);
                _ranks.Add(entry.Key, new RankData(Steam64, entry.Value, entry.Key, this.GetTeam()));
            }
        }
        internal List<Guid>? _completedQuests;
        public void RedownloadMedals()
        {
            _medals.Update(Data.DatabaseManager.GetTeamwork(Steam64));
        }
        public void UpdateRank(EBranch branch, int newXP)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Ranks.TryGetValue(branch, out RankData data))
                data.Update(newXP);
            else
                Ranks.Add(branch, new RankData(Steam64, newXP, branch, this.GetTeam()));
                
        }
        public void UpdateRankTeam(ulong team)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            _ranks = new Dictionary<EBranch, RankData>(6);
            RedownloadRanks();
        }
        public void UpdateMedals(int newTW) => _medals.Update(newTW);
        public float LastSpoken = 0f;

        private bool _otherDonator;
        /// <summary>Slow, loops through all kits, only use once.</summary>
        public bool IsDonator
        {
            get => _otherDonator || KitManager.ActiveObjects.Exists(x => ((x.IsPremium && x.PremiumCost > 0f) || x.IsLoadout) && x.AllowedUsers.Contains(Steam64));
        }
        public Vector3 Position
        {
            get
            {
                try
                {
                    if (Player.transform == null)
                    {
                        L.LogWarning("ERROR: Player transform was null");
                        L.Log($"Kicking {F.GetPlayerOriginalNames(Player).PlayerName} ({Steam64}) for null transform.", ConsoleColor.Cyan);
                        Provider.kick(Player.channel.owner.playerID.steamID, Translation.Translate("null_transform_kick_message", Player, UCWarfare.Config.DiscordInviteCode));
                        return Vector3.zero;
                    }
                    return Player.transform.position;
                }
                catch (NullReferenceException)
                {
                    L.LogWarning("ERROR: Player transform was null");
                    L.Log($"Kicking {F.GetPlayerOriginalNames(Player).PlayerName} ({Steam64}) for null transform.", ConsoleColor.Cyan);
                    Provider.kick(Player.channel.owner.playerID.steamID, Translation.Translate("null_transform_kick_message", Player, UCWarfare.Config.DiscordInviteCode));
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
       
        public static UCPlayer? FromID(ulong steamID)
        {
            return PlayerManager.FromID(steamID);
            //return PlayerManager.OnlinePlayers.Find(p => p != null && p.Steam64 == steamID);
        }
        public static UCPlayer? FromCSteamID(CSteamID steamID) =>
            steamID == default ? null : FromID(steamID.m_SteamID);
        public static UCPlayer? FromPlayer(Player player) => FromID(player.channel.owner.playerID.steamID.m_SteamID);
        public static UCPlayer? FromUnturnedPlayer(UnturnedPlayer player) =>
            player == null || player.Player == null || player.CSteamID == default ? null : FromID(player.CSteamID.m_SteamID);
        public static UCPlayer? FromSteamPlayer(SteamPlayer player)
        {
            if (player == null) return null;
            return FromID(player.playerID.steamID.m_SteamID);
        }
        public static UCPlayer? FromIRocketPlayer(IRocketPlayer caller)
        {
            if (caller is not UnturnedPlayer pl)
                if (caller is UCPlayer uc) return uc;
                else return null;
            else return FromUnturnedPlayer(pl);
        }

        public static UCPlayer? FromName(string name, bool includeContains = false)
        {
            if (name == null) return null;
            UCPlayer player = PlayerManager.OnlinePlayers.Find(
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
            KitClass = kit.Class;
            PlayerManager.ApplyToOnline();
        }
        public ushort LastPingID { get; internal set; }
        public int SuppliesUnloaded;
        public SteamPlayer SteamPlayer { get => Player.channel.owner; }
        public void Message(string text, params string[] formatting) => Player.Message(text, formatting);
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
            Squad = null;
            Player = player;
            CSteamID = steamID;
            CharacterName = characterName;
            NickName = nickName;
            IsOnline = true;
            _otherDonator = donator;
            LifeCounter = 0;
            SuppliesUnloaded = 0;
        }
        public char Icon
        {
            get
            {
                if (SquadManager.config.data.Classes.TryGetValue(KitClass, out ClassConfig config))
                    return config.Icon;
                else if (SquadManager.config.data.Classes.TryGetValue(EClass.NONE, out config))
                    return config.Icon;
                else return '±';
            }
        }

        public ushort MarkerID
        {
            get
            {
                if (SquadManager.config.data.Classes.TryGetValue(KitClass, out ClassConfig config))
                    return config.MarkerEffect;
                else if (SquadManager.config.data.Classes.TryGetValue(EClass.NONE, out config))
                    return config.MarkerEffect;
                else return 0;
            }
        }
        public ushort SquadLeaderMarkerID
        {
            get
            {
                if (SquadManager.config.data.Classes.TryGetValue(KitClass, out ClassConfig config))
                    return config.SquadLeaderMarkerEffect;
                else if (SquadManager.config.data.Classes.TryGetValue(EClass.NONE, out config))
                    return config.SquadLeaderMarkerEffect;
                else return 0;
            }
        }
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
                return (int)Math.Round(amount * (1 + ((float)count / 10)));
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
        public bool IsInLobby()
        {
            return Data.Gamemode is Gamemodes.Interfaces.ITeams teammode && teammode.JoinManager.IsInLobby(this);
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
                    pl.KitClass = kit.Class;
                    pl.Branch = kit.Branch;
                }
                else
                {
                    pl.KitClass = EClass.NONE;
                    pl.Branch = EBranch.DEFAULT;
                }
                pl.KitName = save.KitName;
                pl._otherDonator = save.IsOtherDonator;
            }
        }
        public int CompareTo(object obj) => obj is UCPlayer player ? Steam64.CompareTo(player.Steam64) : -1;
    }

    public class PlayerSave
    {
        public const uint CURRENT_DATA_VERSION = 1;
        public uint DATA_VERSION = CURRENT_DATA_VERSION;
        [JsonSettable]
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
        private static string GetPath(ulong steam64) => "\\Players\\" + steam64.ToString(Data.Locale) +
                                                        "_0\\Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale) + "\\PlayerSave.dat";
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
        public static bool TryReadSaveFile(ulong player, out PlayerSave? save)
        {
            string path = GetPath(player);
            if (!ServerSavedata.fileExists(path))
            {
                save = null;
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
}
