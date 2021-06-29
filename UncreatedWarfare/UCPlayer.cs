﻿using Newtonsoft.Json;
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
    public class UCPlayer
    {
        public readonly ulong Steam64;
        [JsonSettable]
        public ulong Team;
        [JsonSettable]
        public Kit.EClass KitClass;
        [JsonSettable]
        public EBranch Branch;
        [JsonSettable]
        public string KitName;
        [JsonIgnore]
        public Squad Squad;
        [JsonIgnore]
        public Player Player { get; internal set; }
        [JsonIgnore]
        public CSteamID CSteamID { get; internal set; }
        [JsonIgnore]
        public string CharacterName;
        [JsonIgnore]
        public string NickName;
        [JsonIgnore]
        public Rank OfficerRank;
        [JsonIgnore]
        public Vector3 Position { get { return Player.transform.position; } }
        [JsonIgnore]
        public uint cachedXp;

        public static UCPlayer FromID(ulong steamID, ulong team = 0)
        {
            //if (TeamManager.IsTeam1(team))
            //{
            //    return PlayerManager.Team1Players.Find(p => p.Steam64 == steamID);
            //}
            //else if (TeamManager.IsTeam2(team))
            //{w
            //    return PlayerManager.Team2Players.Find(p => p.Steam64 == steamID);
            //}
            //else
            //{
            //    return PlayerManager.OnlinePlayers.Find(p => p.Steam64 == steamID);
            //}
            return PlayerManager.OnlinePlayers.Find(p => p.Steam64 == steamID);
        }
        public static UCPlayer FromCSteamID(CSteamID steamID) => FromID(steamID.m_SteamID);
        public static UCPlayer FromPlayer(Player player) => FromID(player.channel.owner.playerID.steamID.m_SteamID, player.quests.groupID.m_SteamID);
        public static UCPlayer FromUnturnedPlayer(UnturnedPlayer player) => FromID(player.CSteamID.m_SteamID, player.Player.quests.groupID.m_SteamID);
        public static UCPlayer FromSteamPlayer(SteamPlayer player) => FromID(player.playerID.steamID.m_SteamID, player.player.quests.groupID.m_SteamID);
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
            PlayerManager.SaveData();
        }

        public SteamPlayer SteamPlayer()
        {
            using (List<SteamPlayer>.Enumerator enumerator = Provider.clients.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    SteamPlayer current = enumerator.Current;
                    if (Steam64 == current.playerID.steamID.m_SteamID)
                        return current;
                }
            }
            return null;
        }
        public void Message(string text, params object[] formatting) => F.Message(Player, text, formatting);
        public ulong GetTeam() => Player.quests.groupID.m_SteamID;
        public bool IsTeam1() => Player.quests.groupID.m_SteamID == TeamManager.Team1ID;
        public bool IsTeam2() => Player.quests.groupID.m_SteamID == TeamManager.Team2ID;
        public async Task RedownloadCachedXP()
        {
            cachedXp = await Data.DatabaseManager?.GetXP(Steam64, Player.GetTeam());
        }
        public UCPlayer(CSteamID steamID, ulong team, Kit.EClass kitClass, EBranch branch, string kitName, Player player, string characterName, string nickName)
        {
            Steam64 = steamID.m_SteamID;
            Team = team;
            KitClass = kitClass;
            Branch = branch;
            KitName = kitName;
            Squad = null;
            Player = player;
            CSteamID = steamID;
            CharacterName = characterName;
            NickName = nickName;
            OfficerRank = null;
            cachedXp = 0;
        }
        [JsonIgnore]
        public string Icon
        {
            get
            {
                switch (KitClass)
                {
                    default:
                    case Kit.EClass.NONE:
                        return "±";
                    case Kit.EClass.UNARMED:
                        return "±";
                    case Kit.EClass.SQUADLEADER:
                        return "¦";
                    case Kit.EClass.RIFLEMAN:
                        return "¡";
                    case Kit.EClass.MEDIC:
                        return "¢";
                    case Kit.EClass.BREACHER:
                        return "¤";
                    case Kit.EClass.AUTOMATIC_RIFLEMAN:
                        return "¥";
                    case Kit.EClass.GRENADIER:
                        return "¬";
                    case Kit.EClass.MACHINE_GUNNER:
                        return "«";
                    case Kit.EClass.LAT:
                        return "®";
                    case Kit.EClass.HAT:
                        return "¯";
                    case Kit.EClass.MARKSMAN:
                        return "¨";
                    case Kit.EClass.SNIPER:
                        return "£";
                    case Kit.EClass.AP_RIFLEMAN:
                        return "©";
                    case Kit.EClass.COMBAT_ENGINEER:
                        return "ª";
                    case Kit.EClass.CREWMAN:
                        return "§";
                    case Kit.EClass.PILOT:
                        return "°";
                }
            }
        }
    }
}
