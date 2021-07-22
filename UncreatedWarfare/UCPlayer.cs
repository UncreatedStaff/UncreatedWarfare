using Newtonsoft.Json;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
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
        public Vector3 Position
        {
            get
            {
                if (Player.transform is null)
                {
                    F.LogWarning("DEPLOY ERROR: Player transform was null");
                    return new Vector3(0, 0, 0);
                }

                return Player.transform.position;
            }
        }
        public bool IsOnline;
        public int cachedXp = -1;

        public static UCPlayer FromID(ulong steamID)
        {
            return PlayerManager.OnlinePlayers.Find(p => p.Steam64 == steamID);
        }
        public static UCPlayer FromCSteamID(CSteamID steamID) => FromID(steamID.m_SteamID);
        public static UCPlayer FromPlayer(Player player) => FromID(player.channel.owner.playerID.steamID.m_SteamID);
        public static UCPlayer FromUnturnedPlayer(UnturnedPlayer player) => FromID(player.CSteamID.m_SteamID);
        public static UCPlayer FromSteamPlayer(SteamPlayer player) => FromID(player.playerID.steamID.m_SteamID);
        public static UCPlayer FromIRocketPlayer(IRocketPlayer caller)
        {
            if (caller.DisplayName == "Console")
                return null;
            else return FromUnturnedPlayer(caller as UnturnedPlayer);
        }

        public static UCPlayer FromName(string name)
        {
            var player = PlayerManager.OnlinePlayers.Find(
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

        public SteamPlayer SteamPlayer { get => Player.channel.owner; }
        public void Message(string text, params string[] formatting) => Player.Message(text, formatting);
        public ulong GetTeam() => Player.quests.groupID.m_SteamID;
        public bool IsTeam1() => Player.quests.groupID.m_SteamID == TeamManager.Team1ID;
        public bool IsTeam2() => Player.quests.groupID.m_SteamID == TeamManager.Team2ID;
        public async Task RedownloadCachedXP()
        {
            cachedXp = await Data.DatabaseManager?.GetXP(Steam64, Player.GetTeam());
        }
        public UCPlayer(CSteamID steamID, Kit.EClass kitClass, EBranch branch, string kitName, Player player, string characterName, string nickName)
        {
            Steam64 = steamID.m_SteamID;
            KitClass = kitClass;
            Branch = branch;
            KitName = kitName;
            Squad = null;
            Player = player;
            CSteamID = steamID;
            CharacterName = characterName;
            NickName = nickName;
            OfficerRank = null;
            IsOnline = true;
            cachedXp = -1;
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
        public bool IsSquadLeader()
        {
            if (Squad is null)
                return false;

            return Squad.Leader.Steam64 == Steam64;
        }
        public bool IsNearSquadLeader(float distance)
        {
            if (Squad is null)
                return false;

            if (Squad.Leader.Steam64 == Steam64)
                return false;

            return (Position - Squad.Leader.Position).sqrMagnitude < Math.Pow(distance, 2);
        }
        public bool IsOrIsNearLeader(float distance)
        {
            if (Squad is null)
                return false;

            if (Squad.Leader.Steam64 == Steam64)
                return true;

            return (Position - Squad.Leader.Position).sqrMagnitude < Math.Pow(distance, 2);
        }
        public int NearbyMemberBonus(int amount, float distance)
        {
            if (Squad is null)
                return amount;

            int count = 0;
            for (int i = 0; i < Squad.Members.Count; i++)
            {
                if (Squad.Members[i].Steam64 != Steam64 && (Position - Squad.Members[i].Position).sqrMagnitude < Math.Pow(distance, 2))
                    count++;
            }
            return (int)Math.Round(amount * (1 + ((float)count / 10)));
        }

        public bool IsNearFOB()
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.barricades.Count; i++)
                    {
                        BarricadeData b = region.barricades[i];
                        if (b == default) continue;
                        if (b.barricade.id == FOBs.FOBManager.config.Data.FOBID &&
                            b.group == GetTeam() &&
                            (b.point - Position).sqrMagnitude <= 20 * 20)
                            return true;
                    }
                }
            }
            return false;
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
        public Kit.EClass KitClass;
        [JsonSettable]
        public EBranch Branch;
        [JsonSettable]
        public string KitName;
        [JsonSettable]
        public string SquadName;

        public PlayerSave(ulong Steam64)
        {
            this.Steam64 = Steam64;
            Team = 0;
            KitClass = Kit.EClass.NONE;
            Branch = EBranch.DEFAULT;
            KitName = string.Empty;
            SquadName = string.Empty;
        }
        public PlayerSave()
        {
            this.Steam64 = 0;
            Team = 0;
            KitClass = Kit.EClass.NONE;
            Branch = EBranch.DEFAULT;
            KitName = string.Empty;
            SquadName = string.Empty;
        }
    }
}
