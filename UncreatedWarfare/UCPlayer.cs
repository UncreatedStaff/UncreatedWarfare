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
        public Squad Squad;
        public Player Player { get; internal set; }
        public CSteamID CSteamID { get; internal set; }
        public string CharacterName;
        public string NickName;
        public ITransportConnection connection { get { return Player?.channel.owner.transportConnection; } }
        public Coroutine StorageCoroutine;
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
        public string MuteReason;
        public EMuteType MuteType;
        private FPlayerName cachedName = FPlayerName.Nil;
        /// <summary>[Unreliable]</summary>
        private MedalData _medals = MedalData.Nil;
        private Dictionary<EBranch, RankData> _ranks;
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
            Dictionary<EBranch, int> xplevels = Data.DatabaseManager.GetAllXP(Steam64);
            _ranks.Clear();
            foreach (KeyValuePair<EBranch, int> entry in xplevels)
            {
                if (_ranks.ContainsKey(entry.Key))
                    _ranks.Remove(entry.Key);
                _ranks.Add(entry.Key, new RankData(Steam64, entry.Value, entry.Key, this.GetTeam()));
            }
        }
        public void RedownloadMedals()
        {
            _medals.Update(Data.DatabaseManager.GetTeamwork(Steam64));
        }
        public void UpdateRank(EBranch branch, int newXP)
        {
            if (Ranks.TryGetValue(branch, out RankData data))
                data.Update(newXP);
            else
                Ranks.Add(branch, new RankData(Steam64, newXP, branch, this.GetTeam()));
                
        }
        public void UpdateRankTeam(ulong team)
        {
            _ranks = new Dictionary<EBranch, RankData>(6);
            RedownloadRanks();
        }
        public void UpdateMedals(int newTW) => _medals.Update(newTW);

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
       
       
        public static UCPlayer FromID(ulong steamID)
        {
            return PlayerManager.FromID(steamID);
            //return PlayerManager.OnlinePlayers.Find(p => p != null && p.Steam64 == steamID);
        }
        public static UCPlayer FromCSteamID(CSteamID steamID) =>
            steamID == default ? null : FromID(steamID.m_SteamID);
        public static UCPlayer FromPlayer(Player player) => FromID(player.channel.owner.playerID.steamID.m_SteamID);
        public static UCPlayer FromUnturnedPlayer(UnturnedPlayer player) =>
            player == null || player.Player == null || player.CSteamID == default ? null : FromID(player.CSteamID.m_SteamID);
        public static UCPlayer FromSteamPlayer(SteamPlayer player) => FromID(player.playerID.steamID.m_SteamID);
        public static UCPlayer FromIRocketPlayer(IRocketPlayer caller)
        {
            if (caller is not UnturnedPlayer pl)
                if (caller is UCPlayer uc) return uc;
                else return null;
            else return FromUnturnedPlayer(pl);
        }

        public static UCPlayer FromName(string name, bool includeContains = false)
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
        public static UCPlayer FromName(string name, ENameSearchType type)
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
            if (Squad is null)
                return false;

            return Squad.Leader.Steam64 == Steam64;
        }
        public bool IsNearSquadLeader(float distance)
        {
            if (distance == 0 || Squad is null || Squad.Leader is null || Squad.Leader.Player is null || Squad.Leader.Player.transform is null)
                return false;

            if (Squad.Leader.Steam64 == Steam64)
                return false;

            return (Position - Squad.Leader.Position).sqrMagnitude < distance * distance;
        }
        public bool IsOrIsNearLeader(float distance)
        {
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
            UCPlayer pl = PlayerManager.OnlinePlayers?.FirstOrDefault(s => s.Steam64 == Steam64);
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
        public static void Write(ByteWriter W, PlayerSave S)
        {
            W.Write(CURRENT_DATA_VERSION);
            W.Write(S.Steam64);
            W.Write((byte)S.Team);
            W.Write(S.KitName ?? string.Empty);
            W.Write(S.SquadName ?? string.Empty);
            W.Write(S.HasQueueSkip);
            W.Write(S.LastGame);
            W.Write(S.ShouldRespawnOnJoin);
            W.Write(S.IsOtherDonator);
        }
        public static PlayerSave Read(ByteReader R)
        {
            PlayerSave S = new PlayerSave(R.ReadUInt64());
            S.DATA_VERSION = R.ReadUInt32();
            if (S.DATA_VERSION > 0)
            {
                S.Team = R.ReadUInt8();
                S.KitName = R.ReadString();
                S.SquadName = R.ReadString();
                S.HasQueueSkip = R.ReadBool();
                S.LastGame = R.ReadInt64();
                S.ShouldRespawnOnJoin = R.ReadBool();
                S.IsOtherDonator = R.ReadBool();
            }
            else
            {
                S.Team = 0;
                S.KitName = string.Empty;
                S.SquadName = string.Empty;
                S.HasQueueSkip = false;
                S.LastGame = 0;
                S.ShouldRespawnOnJoin = false;
                S.IsOtherDonator = false;
            }
            return S;
        }
        public static void WriteList(ByteWriter W, List<PlayerSave> SL)
        {
            W.Write(SL.Count);
            for (int i = 0; i < SL.Count; i++)
                Write(W, SL[i]);
        }
        public static List<PlayerSave> ReadList(ByteReader R)
        {
            int length = R.ReadInt32();
            List<PlayerSave> saves = new List<PlayerSave>(length);
            for (int i = 0; i < length; i++)
                saves.Add(Read(R));
            return saves;
        }
        public static void WriteMany(ByteWriter W, PlayerSave[] SL)
        {
            W.Write(SL.Length);
            for (int i = 0; i < SL.Length; i++)
                Write(W, SL[i]);
        }
        public static PlayerSave[] ReadMany(ByteReader R)
        {
            int length = R.ReadInt32();
            PlayerSave[] saves = new PlayerSave[length];
            for (int i = 0; i < length; i++)
                saves[i] = Read(R);
            return saves;
        }
    }
}
