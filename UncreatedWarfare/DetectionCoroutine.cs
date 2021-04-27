using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UncreatedWarfare.Flags;
using SDG.NetTransport;
using Flag = UncreatedWarfare.Flags.Flag;

namespace UncreatedWarfare
{
    partial class UCWarfare
    {
        internal List<IEnumerator<WaitForSeconds>> Coroutines;
        public void StartAllCoroutines()
        {
            foreach(IEnumerator<WaitForSeconds> coroutine in Coroutines)
            {
                StartCoroutine(coroutine);
            } 
        }
        internal IEnumerator<WaitForSeconds> CheckPlayers()
        {
            List<SteamPlayer> OnlinePlayers = Provider.clients;
            foreach(Flag flag in FlagManager.FlagRotation)
            {
                List<SteamPlayer> Cappers = OnlinePlayers.Where(player => flag.PlayerInRange(player)).ToList();

                List<SteamPlayer> Team1Cappers = Cappers.Where(player => player.player.quests.groupID.m_SteamID == T1.ID).ToList();
                int Team1TotalPlayers = Team1Cappers.Count;
                List<SteamPlayer> Team2Cappers = Cappers.Where(player => player.player.quests.groupID.m_SteamID == T2.ID).ToList();
                int Team2TotalPlayers = Team2Cappers.Count;
                foreach(SteamPlayer player in OnlinePlayers)
                {
                    ITransportConnection Channel = player.player.channel.owner.transportConnection;
                    ulong team = player.GetTeam();
                    if (flag.PlayerInRange(player))
                    {
                        if (!FlagManager.OnFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                        {
                            FlagManager.AddPlayerOnFlag(player.player, flag);
                            player.SendChat("entered_cap_radius", Colors[team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")], flag.Name, flag.ColorString);
                            F.UIOrChat(team, F.UIOption.Blank, "", Colors["default"], Channel, player, 0, false, true);
                            if (flag.ID == FlagManager.ObjectiveTeam1.ID && team == 1)
                            {
                                if (Team1TotalPlayers - Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers || (Team1TotalPlayers > 0 && Team2TotalPlayers == 0))
                                // if theres enough t1 players to capture or only t1 players CAPTURING/LOSING
                                {
                                    if (flag.IsFriendly(player) || flag.IsNeutral()) {
                                        F.UIOrChat(team, F.UIOption.Capturing, "capturing", Colors[team == 1 ? "capturing_team_1_chat" : (team == 2 ? "capturing_team_2_chat" : "default")], Channel, player, flag.Points);
                                    } else {
                                        F.UIOrChat(team, F.UIOption.Losing, "losing", Colors[team == 1 ? "losing_team_1_chat" : (team == 2 ? "losing_team_2_chat" : "default")], Channel, player, flag.Points);
                                    }
                                } else if (Team1TotalPlayers != 0 && Team2TotalPlayers != 0)
                                //if there are close to the same amount of players on both teams capturing (controlled by the config option) CONTESTED
                                {
                                    foreach (SteamPlayer Capper in Cappers)
                                    {
                                        ulong CapperTeam = Capper.GetTeam();
                                        F.UIOrChat(team, F.UIOption.Contested, "contested", Colors[team == 1 ? "contested_team_1_chat" : (team == 2 ? "contested_team_2_chat" : "default")], Capper, flag.Points, formatting: new object[] { flag.Name, flag.ColorString });
                                    }
                                } else if (flag.IsFriendly(player))
                                {
                                    if (flag.Points < Flag.MaxPoints) {
                                        F.UIOrChat(team, F.UIOption.Clearing, "clearing", Colors[team == 1 ? "clearing_team_1_chat" : (team == 2 ? "clearing_team_2_chat" : "default")], Channel, player, flag.Points);
                                    } else {
                                        F.UIOrChat(team, F.UIOption.Clearing, "secured", Colors[team == 1 ? "secured_team_1_chat" : (team == 2 ? "secured_team_2_chat" : "default")], Channel, player, flag.Points);
                                    }
                                }
                            } else if (flag.ID == FlagManager.ObjectiveTeam2.ID && team == 2)
                            {
                                if (Team2TotalPlayers - Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team1TotalPlayers || (Team2TotalPlayers > 0 && Team1TotalPlayers == 0))
                                {
                                    if (flag.IsFriendly(player) || flag.IsNeutral()) {
                                        F.UIOrChat(team, F.UIOption.Capturing, "capturing", Colors[team == 1 ? "capturing_team_1_chat" : (team == 2 ? "capturing_team_2_chat" : "default")], Channel, player, flag.Points);
                                    } else {
                                        F.UIOrChat(team, F.UIOption.Losing, "losing", Colors[team == 1 ? "losing_team_1_chat" : (team == 2 ? "losing_team_2_chat" : "default")], Channel, player, flag.Points);
                                    }
                                } else if (Team2TotalPlayers != 0 && Team1TotalPlayers != 0)
                                {
                                    foreach (SteamPlayer Capper in Cappers)
                                    {
                                        ulong CapperTeam = Capper.GetTeam();
                                        F.UIOrChat(team, F.UIOption.Contested, "contested", Colors[team == 1 ? "contested_team_1_chat" : (team == 2 ? "contested_team_2_chat" : "default")], Capper, flag.Points, formatting: new object[] { flag.Name, flag.ColorString });
                                    }
                                } else if (flag.IsFriendly(player))
                                {
                                    if (flag.Points > -1 * Flag.MaxPoints) {
                                        F.UIOrChat(team, F.UIOption.Clearing, "clearing", Colors[team == 1 ? "clearing_team_1_chat" : (team == 2 ? "clearing_team_2_chat" : "default")], Channel, player, flag.Points);
                                    }
                                    else {
                                        F.UIOrChat(team, F.UIOption.Clearing, "secured", Colors[team == 1 ? "secured_team_1_chat" : (team == 2 ? "secured_team_2_chat" : "default")], Channel, player, flag.Points);
                                    }
                                }
                            }
                        }
                    } else if (FlagManager.OnFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                    {
                        if(FlagManager.OnFlag[player.playerID.steamID.m_SteamID] == flag.ID)
                        {
                            player.SendChat("left_cap_radius", Colors[team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")], flag.Name, flag.ColorString);
                            FlagManager.RemovePlayerFromFlag(player.player, flag);
                            if (Config.FlagSettings.UseUI)
                                EffectManager.askEffectClearByID(Config.FlagSettings.UIID, Channel);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(Config.FlagSettings.PlayerCheckSpeedSeconds);
            StartCoroutine(CheckPlayers());
        }
    }
}
