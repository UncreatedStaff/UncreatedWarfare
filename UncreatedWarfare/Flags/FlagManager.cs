using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using UncreatedWarfare.Teams;

namespace UncreatedWarfare.Flags
{
    public class FlagManager
    {
        public List<Flag> FlagRotation { get; private set; }
        public string Preset {
            get => _preset; 
            set 
            {
                this.Preset = value;
                LoadNewFlags();
            } 
        }
        public const int CounterMax = 6;
        public bool TimeToCheck
        {
            get
            {
                if (_counter > CounterMax)
                {
                    _counter = 0;
                    return true;
                }
                else
                {
                    _counter++;
                    return false;
                }

            }
        }
        private int _counter;
        private string _preset;
        public Dictionary<ulong, int> OnFlag { get; private set; }
        public int ObjectiveT1Index;
        public int ObjectiveT2Index;
        public Flag ObjectiveTeam1 { get => FlagRotation[ObjectiveT1Index]; }
        public Flag ObjectiveTeam2 { get => FlagRotation[ObjectiveT2Index]; }
        public FlagManager(string Preset = "default")
        {
            this._preset = Preset;
            FlagRotation = new List<Flag>();
            OnFlag = new Dictionary<ulong, int>();
            LoadNewFlags();
        }
        public void AddPlayerOnFlag(Player player, Flag flag) { 
            OnFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
            flag.EnterPlayer(player);
        }
        public void RemovePlayerFromFlag(Player player, Flag flag)
        {
            if (OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID) && OnFlag[player.channel.owner.playerID.steamID.m_SteamID] == flag.ID)
            {
                OnFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                flag.ExitPlayer(player);
            }
        }
        public static int FromMax(int cap) => Math.Abs(cap) >= Flag.MaxPoints ? UCWarfare.Config.FlagSettings.charactersForUI.Length - 1 : ((UCWarfare.Config.FlagSettings.charactersForUI.Length - 1) / Flag.MaxPoints) * Math.Abs(cap);
        public void ClearPlayersOnFlag() => OnFlag.Clear();
        public void LoadNewFlags()
        {
            FlagRotation.Clear();
            OnFlag.Clear();
            List<FlagData> flags = JSONMethods.ReadFlags(Preset);
            int i;
            flags.Sort((FlagData a, FlagData b) => a.id.CompareTo(b.id));
            for (i = 0; i < flags.Count; i++)
            {
                Flag flag = new Flag(flags[i]);
                flag.OnPlayerEntered += PlayerEnteredFlagRadius;
                flag.OnPlayerLeft += PlayerLeftFlagRadius;
                flag.OnOwnerChanged += FlagOwnerChanged;
                flag.OnPointsChanged += FlagPointsChanged;
                FlagRotation.Add(flag);
            }
            CommandWindow.Log("Loaded " + i.ToString() + " flags into memory and cleared any existing old lists.");
            ObjectiveT1Index = 0;
            ObjectiveT2Index = FlagRotation.Count - 1;
        }

        private void FlagPointsChanged(object sender, CaptureChangeEventArgs e)
        {
            Flag flag = sender as Flag;
            CommandWindow.LogWarning("Points changed on flag " + flag.Name + " from " + e.OldPoints.ToString() + " to " + e.NewPoints.ToString());
            if (flag.Points < Flag.MaxPoints)
            {
                if(flag.Points > 0)
                {
                    foreach (Player player in flag.PlayersOnFlag)
                    {
                        ulong team = player.GetTeam();
                        ITransportConnection Channel = player.channel.owner.transportConnection;
                        if (team == 1)
                        {
                            F.UIOrChat(team, F.UIOption.Capturing, "team_capturing", UCWarfare.I.Colors[team == 1 ? "capturing_team_1_chat" : "default"], Channel, player.channel.owner, flag.Points, 
                                formatting: new object[] { UCWarfare.I.TeamManager.T1.LocalizedName, UCWarfare.I.TeamManager.T1.TeamColorHex, flag.Name, flag.TeamSpecificColor, Math.Abs(flag.Points), Flag.MaxPoints  });
                            UCWarfare.I.DB.AddXP(EXPGainType.CAP_INCREASE);
                        }
                        else
                        {
                            F.UIOrChat(team, F.UIOption.Losing, "team_capturing", UCWarfare.I.Colors[team == 1 ? "capturing_team_1_chat" : "default"], Channel, player.channel.owner, flag.Points,
                                formatting: new object[] { UCWarfare.I.TeamManager.T1.LocalizedName, UCWarfare.I.TeamManager.T1.TeamColorHex, flag.Name, flag.TeamSpecificColor, Math.Abs(flag.Points), Flag.MaxPoints });
                        }
                    }
                } else if (flag.Points == 0)
                {
                    flag.Owner = TeamManager.Neutral;
                    F.Broadcast("flag_neutralized", UCWarfare.I.Colors["flag_neutralized"], flag.Name, flag.TeamSpecificColor);
                } else
                {
                    foreach (Player player in flag.PlayersOnFlag)
                    {
                        ulong team = player.GetTeam(); 
                        ITransportConnection Channel = player.channel.owner.transportConnection;
                        if (team == 1)
                        {
                            F.UIOrChat(team, F.UIOption.Clearing, "clearing", UCWarfare.I.Colors[team == 1 ? "capturing_team_1_chat" : "default"], Channel, player.channel.owner, flag.Points);
                            UCWarfare.I.DB.AddXP(EXPGainType.CAP_INCREASE);
                        }
                        else
                        {
                            F.UIOrChat(team, F.UIOption.Losing, "losing", UCWarfare.I.Colors[team == 2 ? "capturing_team_2_chat" : "default"], Channel, player.channel.owner, flag.Points);
                        }
                    }
                }
            }
        }
        private void FlagOwnerChanged(object sender, OwnerChangeEventArgs e)
        {
            Flag flag = sender as Flag;
            CommandWindow.LogWarning("Owner changed of flag " + flag.Name);
            // owner of flag changed (full caputure or loss)
        }
        private void PlayerLeftFlagRadius(object sender, PlayerEventArgs e)
        {
            Flag flag = sender as Flag;
            // player walked out of flag
            ITransportConnection Channel = e.player.channel.owner.transportConnection;
            ulong team = e.player.GetTeam();
            CommandWindow.LogWarning("Player " + e.player.channel.owner.playerID.playerName + " left flag " + flag.Name);
            e.player.SendChat("left_cap_radius", UCWarfare.I.Colors[team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")], flag.Name, flag.ColorString);
            if (UCWarfare.Config.FlagSettings.UseUI)
                EffectManager.askEffectClearByID(UCWarfare.Config.FlagSettings.UIID, Channel);
        }
        private void PlayerEnteredFlagRadius(object sender, PlayerEventArgs e)
        {
            Flag flag = sender as Flag;
            // player walked into flag
            ITransportConnection Channel = e.player.channel.owner.transportConnection;
            ulong team = e.player.GetTeam();
            CommandWindow.LogWarning("Player " + e.player.channel.owner.playerID.playerName + " entered flag " + flag.Name);
            e.player.SendChat("entered_cap_radius", UCWarfare.I.Colors[team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")], flag.Name, flag.ColorString);
            F.UIOrChat(team, F.UIOption.Blank, "", UCWarfare.I.Colors["default"], Channel, e.player.channel.owner, 0, false, true);
            if (flag.ID == ObjectiveTeam1.ID && team == 1)
            {
                if (flag.Team1TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= flag.Team2TotalPlayers || (flag.Team1TotalPlayers > 0 && flag.Team2TotalPlayers == 0))
                // if theres enough t1 players to capture or only t1 players CAPTURING/LOSING
                {
                    if (flag.IsFriendly(e.player) || flag.IsNeutral())
                    {
                        F.UIOrChat(team, F.UIOption.Capturing, "capturing", UCWarfare.I.Colors[team == 1 ? "capturing_team_1_chat" : (team == 2 ? "capturing_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Losing, "losing", UCWarfare.I.Colors[team == 1 ? "losing_team_1_chat" : (team == 2 ? "losing_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                }
                else if (flag.Team1TotalPlayers != 0 && flag.Team2TotalPlayers != 0)
                //if there are close to the same amount of players on both teams capturing (controlled by the config option) CONTESTED
                {
                    foreach (Player Capper in flag.PlayersOnFlag)
                    {
                        ulong CapperTeam = Capper.GetTeam();
                        F.UIOrChat(team, F.UIOption.Contested, "contested", UCWarfare.I.Colors[CapperTeam == 1 ? "contested_team_1_chat" : (CapperTeam == 2 ? "contested_team_2_chat" : "default")], Capper.channel.owner, flag.Points, formatting: new object[] { flag.Name, flag.ColorString });
                    }
                }
                else if (flag.IsFriendly(e.player))
                {
                    if (flag.Points < Flag.MaxPoints)
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "clearing", UCWarfare.I.Colors[team == 1 ? "clearing_team_1_chat" : (team == 2 ? "clearing_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "secured", UCWarfare.I.Colors[team == 1 ? "secured_team_1_chat" : (team == 2 ? "secured_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                }
            }
            else if (flag.ID == ObjectiveTeam2.ID && team == 2)
            {
                if (flag.Team2TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= flag.Team1TotalPlayers || (flag.Team2TotalPlayers > 0 && flag.Team1TotalPlayers == 0))
                {
                    if (flag.IsFriendly(e.player) || flag.IsNeutral())
                    {
                        F.UIOrChat(team, F.UIOption.Capturing, "capturing", UCWarfare.I.Colors[team == 1 ? "capturing_team_1_chat" : (team == 2 ? "capturing_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Losing, "losing", UCWarfare.I.Colors[team == 1 ? "losing_team_1_chat" : (team == 2 ? "losing_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                }
                else if (flag.Team2TotalPlayers != 0 && flag.Team1TotalPlayers != 0)
                {
                    foreach (Player Capper in flag.PlayersOnFlag)
                    {
                        ulong CapperTeam = Capper.GetTeam();
                        F.UIOrChat(team, F.UIOption.Contested, "contested", UCWarfare.I.Colors[CapperTeam == 1 ? "contested_team_1_chat" : (CapperTeam == 2 ? "contested_team_2_chat" : "default")], Capper.channel.owner, flag.Points, formatting: new object[] { flag.Name, flag.ColorString });
                    }
                }
                else if (flag.IsFriendly(e.player))
                {
                    if (flag.Points > -1 * Flag.MaxPoints)
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "clearing", UCWarfare.I.Colors[team == 1 ? "clearing_team_1_chat" : (team == 2 ? "clearing_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "secured", UCWarfare.I.Colors[team == 1 ? "secured_team_1_chat" : (team == 2 ? "secured_team_2_chat" : "default")], Channel, e.player.channel.owner, flag.Points);
                    }
                }
            }
        }
        public void Dispose()
        {
            foreach(Flag flag in FlagRotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
            }
            FlagRotation.Clear();
            GC.SuppressFinalize(this);
        }
        public void EvaluatePoints(List<SteamPlayer> OnlinePlayers)
        {
            foreach (Flag flag in FlagRotation.Where(f => f.PlayersOnFlag.Count > 0))
            {
                flag.EvaluatePoints();
            }
        }
    }
}