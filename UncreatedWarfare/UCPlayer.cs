using Newtonsoft.Json;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Networking.Encoding;
using Uncreated.Networking.Encoding.IO;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.XP;
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
        public Rank OfficerRank;
        public ITransportConnection connection { get { return Player?.channel.owner.transportConnection; } }
        public Coroutine StorageCoroutine;
        /// <summary>[Unreliable]</summary>
        public Rank XPRank()
        {
            if (CachedXp == -1)
                RedownloadCachedXP();
            if (_rank == null || isXpDirty)
            {
                _rank = XPManager.GetRank(CachedXp, out _, out _);
                isXpDirty = _rank == null;
            }
            return _rank;
        }
        private bool _otherDonator;
        /// <summary>Slow, loops through all kits, only use once.</summary>
        public bool IsDonator
        {
            get => _otherDonator || KitManager.ActiveObjects.Exists(x => ((x.IsPremium && x.PremiumCost > 0f) || x.IsLoadout) && x.AllowedUsers.Contains(Steam64));
        }
        private Rank _rank;
        public Vector3 Position
        {
            get
            {
                try
                {
                    if (Player.transform == null)
                    {
                        F.LogWarning("ERROR: Player transform was null");
                        F.Log($"Kicking {F.GetPlayerOriginalNames(Player).PlayerName} ({Steam64}) for null transform.", ConsoleColor.Cyan);
                        Provider.kick(Player.channel.owner.playerID.steamID, F.Translate("null_transform_kick_message", Player, UCWarfare.Config.DiscordInviteCode));
                        return Vector3.zero;
                    }
                    return Player.transform.position;
                }
                catch (NullReferenceException)
                {
                    F.LogWarning("ERROR: Player transform was null");
                    F.Log($"Kicking {F.GetPlayerOriginalNames(Player).PlayerName} ({Steam64}) for null transform.", ConsoleColor.Cyan);
                    Provider.kick(Player.channel.owner.playerID.steamID, F.Translate("null_transform_kick_message", Player, UCWarfare.Config.DiscordInviteCode));
                    return Vector3.zero;
                }
            }
        }
        public bool IsOnline;
        public int LifeCounter;
        private int _cachedOfp = -1;
        public int CachedOfp
        {
            get => _cachedOfp;
            set
            {
                _cachedOfp = value;
            }
        }
        private int _cachedXp = -1;
        private bool isXpDirty = false;
        public int CachedXp
        {
            get => _cachedXp;
            set
            {
                _cachedXp = value;
                isXpDirty = true;
            }
        }
        public static UCPlayer FromID(ulong steamID)
        {
            return PlayerManager.OnlinePlayers.Find(p => p != null && p.Steam64 == steamID);
        }
        public static UCPlayer FromCSteamID(CSteamID steamID) =>
            steamID == default ? null : FromID(steamID.m_SteamID);
        public static UCPlayer FromPlayer(Player player) => FromID(player.channel.owner.playerID.steamID.m_SteamID);
        public static UCPlayer FromUnturnedPlayer(UnturnedPlayer player) =>
            player == null || player.Player == null || player.CSteamID == default ? null : FromID(player.CSteamID.m_SteamID);
        public static UCPlayer FromSteamPlayer(SteamPlayer player) => FromID(player.playerID.steamID.m_SteamID);
        public static UCPlayer FromIRocketPlayer(IRocketPlayer caller)
        {
            if (caller == null || caller.DisplayName == "Console")
                return null;
            else return FromUnturnedPlayer(caller as UnturnedPlayer);
        }

        public static UCPlayer FromName(string name)
        {
            if (name == null) return null;
            UCPlayer player = PlayerManager.OnlinePlayers.Find(
                s =>
                s.Player.channel.owner.playerID.characterName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                s.Player.channel.owner.playerID.nickName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                s.Player.channel.owner.playerID.playerName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                s.Player.channel.owner.playerID.characterName.Replace(" ", "").ToLower().Contains(name) ||
                s.Player.channel.owner.playerID.nickName.Replace(" ", "").ToLower().Contains(name) ||
                s.Player.channel.owner.playerID.playerName.Replace(" ", "").ToLower().Contains(name)
                );

            return player;
        }

        public void ChangeKit(Kit kit)
        {
            KitName = kit.Name;
            KitClass = kit.Class;
            PlayerManager.ApplyToOnline();
        }
        public ushort LastPingID { get; internal set; }
        public SteamPlayer SteamPlayer { get => Player.channel.owner; }
        public void Message(string text, params string[] formatting) => Player.Message(text, formatting);
        public bool IsTeam1() => Player.quests.groupID.m_SteamID == TeamManager.Team1ID;
        public bool IsTeam2() => Player.quests.groupID.m_SteamID == TeamManager.Team2ID;
        public void RedownloadCachedXP()
        {
            CachedXp = Data.DatabaseManager.GetXP(Steam64);
        }
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
            OfficerRank = null;
            IsOnline = true;
            _cachedXp = -1;
            _cachedOfp = -1;
            _otherDonator = donator;
            LifeCounter = 0;
        }
        public char Icon
        {
            get
            {
                if (SquadManager.config.Data.Classes.TryGetValue(KitClass, out ClassConfig config))
                    return config.Icon;
                else if (SquadManager.config.Data.Classes.TryGetValue(EClass.NONE, out config))
                    return config.Icon;
                else return '±';
            }
        }

        public ushort MarkerID
        {
            get
            {
                if (SquadManager.config.Data.Classes.TryGetValue(KitClass, out ClassConfig config))
                    return config.MarkerEffect;
                else if (SquadManager.config.Data.Classes.TryGetValue(EClass.NONE, out config))
                    return config.MarkerEffect;
                else return 0;
            }
        }
        public ushort SquadLeaderMarkerID
        {
            get
            {
                if (SquadManager.config.Data.Classes.TryGetValue(KitClass, out ClassConfig config))
                    return config.SquadLeaderMarkerEffect;
                else if (SquadManager.config.Data.Classes.TryGetValue(EClass.NONE, out config))
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
        public uint DATA_VERSION;
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
            W.Write(S.DATA_VERSION);
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
            uint DATA_VERSION = R.ReadUInt32();
            PlayerSave S = new PlayerSave(R.ReadUInt64()) { DATA_VERSION = DATA_VERSION };
            if (DATA_VERSION > 0)
            {
                S.Team = R.ReadUInt8();
                S.KitName = R.ReadString();
                S.SquadName = R.ReadString();
                S.HasQueueSkip = R.ReadBool();
                S.LastGame = R.ReadInt32();
                S.ShouldRespawnOnJoin = R.ReadBool();
                S.IsOtherDonator = R.ReadBool();
            } else
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
            {
                PlayerSave S = SL[i];
                W.Write(S.DATA_VERSION);
                W.Write(S.Steam64);
                W.Write((byte)S.Team);
                W.Write(S.KitName ?? string.Empty);
                W.Write(S.SquadName ?? string.Empty);
                W.Write(S.HasQueueSkip);
                W.Write(S.LastGame);
                W.Write(S.ShouldRespawnOnJoin);
                W.Write(S.IsOtherDonator);
            }
        }
        public static List<PlayerSave> ReadList(ByteReader R)
        {
            int length = R.ReadInt32();
            List<PlayerSave> saves = new List<PlayerSave>(length);
            for (int i = 0; i < length; i++)
            {
                uint DATA_VERSION = R.ReadUInt32();
                PlayerSave S = new PlayerSave(R.ReadUInt64()) { DATA_VERSION = DATA_VERSION };
                if (DATA_VERSION > 0)
                {
                    S.Team = R.ReadUInt8();
                    S.KitName = R.ReadString();
                    S.SquadName = R.ReadString();
                    S.HasQueueSkip = R.ReadBool();
                    S.LastGame = R.ReadInt32();
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
                saves.Add(S);
            }
            return saves;
        }
    }
}
