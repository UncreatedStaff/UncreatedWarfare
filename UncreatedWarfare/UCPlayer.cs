using Newtonsoft.Json;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
using Uncreated.Warfare.Components;
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
        public Kit.EClass KitClass;
        public EBranch Branch;
        public string KitName;
        public Squad Squad;
        public Player Player { get; internal set; }
        public CSteamID CSteamID { get; internal set; }
        public string CharacterName;
        public string NickName;
        public Rank OfficerRank;
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
                    if (Player.transform is null)
                    {
                        F.LogWarning("DEPLOY ERROR: Player transform was null");
                        return new Vector3(0, 0, 0);
                    }
                    return Player.transform.position;
                }
                catch (NullReferenceException)
                {
                    F.LogWarning("DEPLOY ERROR: Player transform was null");
                    return new Vector3(0, 0, 0);
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
            PlayerManager.Save();
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
                KitClass = Kit.EClass.NONE;
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
                else if (SquadManager.config.Data.Classes.TryGetValue(Kit.EClass.NONE, out config))
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
                else if (SquadManager.config.Data.Classes.TryGetValue(Kit.EClass.NONE, out config))
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
                else if (SquadManager.config.Data.Classes.TryGetValue(Kit.EClass.NONE, out config))
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

        public bool IsNearFOB()
        {
            if (Player.life.isDead || Player.transform is null)
                return false;

            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        BarricadeDrop b = region.drops[i];
                        if (b.GetServersideData().barricade.id == FOBs.FOBManager.config.Data.FOBID &&
                            b.GetServersideData().group.GetTeam() == Player.GetTeam() &&
                            (b.model.position - Position).sqrMagnitude <= 20 * 20)
                            return true;
                    }
                }
            }
            return false;
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
                    pl.KitClass = Kit.EClass.NONE;
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
    }
}
