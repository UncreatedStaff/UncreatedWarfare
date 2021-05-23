using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UncreatedWarfare.Stats;
using UncreatedWarfare.Teams;
using UnityEngine;
using Random = System.Random;

namespace UncreatedWarfare.Flags
{
    public class FlagManager : IDisposable
    {
        public List<Flag> FlagRotation { get; private set; }
        public List<Flag> AllFlags { get; private set; }
        const int FLAGS_PER_LEVEL_MAX = 2;
        public string Preset {
            get => _preset; 
            set 
            {
                this.Preset = value;
                LoadNewFlags();
            } 
        }
        public const int CounterMax = 1;  // how many detection coroutine go by before points are incremented. (higher the number, longer the flags take to capture)
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
        private readonly string _preset;
        public Dictionary<ulong, int> OnFlag { get; private set; }
        public int ObjectiveT1Index;
        public int ObjectiveT2Index;
        public Flag ObjectiveTeam1 { get => FlagRotation[ObjectiveT1Index]; }
        public Flag ObjectiveTeam2 { get => FlagRotation[ObjectiveT2Index]; }
        private EndScreenLeaderboard EndScreen;
        public EState State { get => _state; set
            {
                EState oldState = _state;
                _state = value;
                OnStateChanged?.Invoke(this, new OnStateChangedEventArgs() { NewState = _state, OldState = oldState });
            } 
        }
        private EState _state;
        public FlagManager(string Preset = "default")
        {
            this._state = EState.LOADING;
            this._preset = Preset;
            FlagRotation = new List<Flag>();
            AllFlags = new List<Flag>();
            OnFlag = new Dictionary<ulong, int>();
            OnObjectiveChange += OnObjectiveChangeAction;
        }
        private void OnObjectiveChangeAction(object sender, OnObjectiveChangeEventArgs e)
        {
            if(Data.GameStats != null)
                Data.GameStats.totalFlagOwnerChanges++;
            F.Broadcast("Objective changed for team " + e.Team.ToString() + " from " + e.oldFlagObj.Name + " to " + e.newFlagObj.Name, UCWarfare.GetColor("default"));
            F.Log("Team 1 objective: " + ObjectiveTeam1.Name + ", Team 2 objective: " + ObjectiveTeam2.Name, ConsoleColor.Magenta);
        }
        public void Load()
        {
            LoadAllFlags();
            this.State = EState.PAUSED;
            OnReady?.Invoke(this, EventArgs.Empty);
        }
        public void AddPlayerOnFlag(Player player, Flag flag) {
            if(OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
            {
                if (OnFlag[player.channel.owner.playerID.steamID.m_SteamID] != flag.ID)
                {
                    Flag oldFlag = FlagRotation.FirstOrDefault(f => f.ID == OnFlag[player.channel.owner.playerID.steamID.m_SteamID]);
                    if(oldFlag == default(Flag)) OnFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                    else RemovePlayerFromFlag(player, oldFlag);
                    OnFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
                }
            } else OnFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
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
            if (AllFlags == null) return;
            ResetFlags();
            OnFlag.Clear();
            List<KeyValuePair<int, List<Flag>>> lvls = new List<KeyValuePair<int, List<Flag>>>();
            for(int i = 0; i < AllFlags.Count; i++)
            {
                KeyValuePair<int, List<Flag>> flag = lvls.FirstOrDefault(x => x.Key == AllFlags[i].Level);
                if (flag.Equals(default(KeyValuePair<int, List<Flag>>)))
                    lvls.Add(new KeyValuePair<int, List<Flag>>(AllFlags[i].Level, new List<Flag> { AllFlags[i] }));
                else
                    flag.Value.Add(AllFlags[i]);
            }
            lvls.Sort((KeyValuePair<int, List<Flag>> a, KeyValuePair<int, List<Flag>> b) => a.Key.CompareTo(b.Key));
            Random r = new Random();
            for (int i = 0; i < lvls.Count; i++)
            {
                int amtToAdd = lvls[i].Value.Count > FLAGS_PER_LEVEL_MAX ? FLAGS_PER_LEVEL_MAX : lvls[i].Value.Count;
                int counter = 0;
                while (counter < amtToAdd)
                {
                    int index = r.Next(0, lvls[i].Value.Count);
                    FlagRotation.Add(lvls[i].Value[index]);
                    lvls[i].Value.RemoveAt(index);
                    counter++;
                }
            }
            ObjectiveT1Index = 0;
            ObjectiveT2Index = FlagRotation.Count - 1;
            F.Log("Team 1 objective: " + ObjectiveTeam1.Name + ", Team 2 objective: " + ObjectiveTeam2.Name, ConsoleColor.Green);
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < FlagRotation.Count; i++)
            {
                FlagRotation[i].OnPlayerEntered += PlayerEnteredFlagRadius;
                FlagRotation[i].OnPlayerLeft += PlayerLeftFlagRadius;
                FlagRotation[i].OnOwnerChanged += FlagOwnerChanged;
                FlagRotation[i].OnPointsChanged += FlagPointsChanged;
                sb.Append(i.ToString() + ") " + FlagRotation[i].Name + " | Level: " + FlagRotation[i].Level.ToString() + '\n');
            }
            F.Log(sb.ToString(), ConsoleColor.Green);
        }
        private void LoadAllFlags()
        {
            AllFlags.Clear();
            List<FlagData> flags = JSONMethods.ReadFlags(Preset);
            flags.Sort((FlagData a, FlagData b) => a.id.CompareTo(b.id));
            int i;
            for (i = 0; i < flags.Count; i++)
            {
                AllFlags.Add(new Flag(flags[i], this));
            }
            F.Log("Loaded " + i.ToString() + " flags into memory and cleared any existing old flags.", ConsoleColor.Magenta);
        }
        private void FlagPointsChanged(object sender, CaptureChangeEventArgs e)
        {
            Flag flag = sender as Flag;
            F.Log("Points changed on flag " + flag.Name + " from " + e.OldPoints.ToString() + " to " + e.NewPoints.ToString(), ConsoleColor.White);
            if (flag.Points < Flag.MaxPoints) // not fully capped by t1
            {
                if(flag.Points > 0) // being capped by t1
                {
                    foreach (Player player in flag.PlayersOnFlag)
                    {
                        ulong team = player.GetTeam();
                        ITransportConnection Channel = player.channel.owner.transportConnection;
                        if (team == 1)
                        {
                            F.UIOrChat(team, F.UIOption.Capturing, "team_capturing", UCWarfare.GetColor(team == 1 ? "capturing_team_1_chat" : "default"), Channel, player.channel.owner, flag.Points, 
                                player.channel.owner.playerID.steamID.m_SteamID,
                                formatting: new object[] { TeamManager.TranslateName(TeamManager.Team1ID, player), TeamManager.Team1ColorHex, flag.Name, flag.TeamSpecificHexColor, Math.Abs(flag.Points), Flag.MaxPoints });
                            //UCWarfare.I.DB.AddXP(EXPGainType.CAP_INCREASE);
                        }
                        else
                        {
                            F.UIOrChat(team, F.UIOption.Losing, "team_losing", UCWarfare.GetColor(team == 1 ? "losing_team_1_chat" : "default"), Channel, player.channel.owner, flag.Points, 
                                player.channel.owner.playerID.steamID.m_SteamID,
                                formatting: new object[] { TeamManager.TranslateName(TeamManager.Team1ID, player), TeamManager.Team1ColorHex, flag.Name, flag.TeamSpecificHexColor, Math.Abs(flag.Points), Flag.MaxPoints });
                        }
                    }
                } else if (flag.Points == 0) // flag uncaptured
                {
                    flag.Owner = 0;
                    F.Broadcast("flag_neutralized", UCWarfare.GetColor("flag_neutralized"), flag.Name, flag.TeamSpecificColor);
                } else if (flag.Points > -Flag.MaxPoints) // not fully capped by t2 but being capped
                {
                    foreach (Player player in flag.PlayersOnFlag)
                    {
                        ulong team = player.GetTeam(); 
                        ITransportConnection Channel = player.channel.owner.transportConnection;
                        if (team == 2)
                        {
                            if(flag.Owner == team)
                            {
                                F.UIOrChat(team, F.UIOption.Clearing, "clearing", UCWarfare.GetColor(team == 1 ? "clearing_team_1_chat" : "default"), Channel, player.channel.owner, flag.Points,
                                    player.channel.owner.playerID.steamID.m_SteamID);
                            } else
                            {
                                F.UIOrChat(team, F.UIOption.Capturing, "capturing", UCWarfare.GetColor(team == 1 ? "capturing_team_1_chat" : "default"), Channel, player.channel.owner, flag.Points,
                                    player.channel.owner.playerID.steamID.m_SteamID);
                            }
                            //UCWarfare.I.DB.AddXP(EXPGainType.CAP_INCREASE);
                        }
                        else
                        {
                            F.UIOrChat(team, F.UIOption.Losing, "losing", UCWarfare.GetColor(team == 2 ? "capturing_team_2_chat" : "default"), Channel, player.channel.owner, flag.Points, 
                                player.channel.owner.playerID.steamID.m_SteamID);
                        }
                    }
                } else // t2 has capped
                {
                    foreach (Player player in flag.PlayersOnFlag)
                    {
                        ulong team = player.GetTeam();
                        ITransportConnection Channel = player.channel.owner.transportConnection;
                        if (team == 2)
                        {
                            F.UIOrChat(team, F.UIOption.Clearing, "secured", UCWarfare.GetColor(team == 1 ? "secured_team_2_chat" : "default"), Channel, player.channel.owner, flag.Points,
                                player.channel.owner.playerID.steamID.m_SteamID);
                            //UCWarfare.I.DB.AddXP(EXPGainType.CAP_INCREASE);
                        }
                        else
                        {
                            F.UIOrChat(team, F.UIOption.NotOwned, "notowned", UCWarfare.GetColor(team == 2 ? "notowned_team_1_chat" : "default"), Channel, player.channel.owner, flag.Points,
                                player.channel.owner.playerID.steamID.m_SteamID);
                        }
                    }
                }
            } else // t1 has capped
            {
                foreach (Player player in flag.PlayersOnFlag)
                {
                    ulong team = player.GetTeam();
                    ITransportConnection Channel = player.channel.owner.transportConnection;
                    if (team == 1)
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "secured", UCWarfare.GetColor(team == 1 ? "secured_team_1_chat" : "default"), Channel, player.channel.owner, flag.Points,
                            player.channel.owner.playerID.steamID.m_SteamID);
                        //UCWarfare.I.DB.AddXP(EXPGainType.CAP_INCREASE);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.NotOwned, "notowned", UCWarfare.GetColor(team == 2 ? "notowned_team_2_chat" : "default"), Channel, player.channel.owner, flag.Points,
                            player.channel.owner.playerID.steamID.m_SteamID);
                    }
                }
            }
        }
        public class OnTeamWinEventArgs : EventArgs { public ulong team; }
        public event EventHandler<OnTeamWinEventArgs> OnTeamWinGame;
        public class OnObjectiveChangeEventArgs : EventArgs { public Flag oldFlagObj; public Flag newFlagObj; public ulong Team; public int OldObj; public int NewObj; }
        public class OnStateChangedEventArgs : EventArgs { public EState NewState; public EState OldState; }
        public event EventHandler<OnObjectiveChangeEventArgs> OnObjectiveChange;
        public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
        public event EventHandler OnReady;
        public event EventHandler OnNewGameStarting;
        public void DeclareWin(ulong Team)
        {
            F.LogWarning(TeamManager.TranslateName(Team, 0) + " just won the game!", ConsoleColor.Green);
            foreach (SteamPlayer client in Provider.clients)
                client.SendChat("team_win", UCWarfare.GetColor("team_win"), TeamManager.TranslateName(Team, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(Team));
            this.State = EState.FINISHED;
            OnTeamWinGame?.Invoke(this, new OnTeamWinEventArgs { team = Team });
            EndScreen = UCWarfare.I.gameObject.AddComponent<EndScreenLeaderboard>();
            EndScreen.OnLeaderboardExpired += EndScreen_OnLeaderboardExpired;
            EndScreen.winner = Team;
            EndScreen.EndGame();

        }
        public void StartNextGame()
        {
            F.Log("Loading new game.", ConsoleColor.Cyan);
            LoadNewFlags();
            State = EState.ACTIVE;
            EffectManager.ClearEffectByID_AllPlayers(UCWarfare.Config.FlagSettings.UIID);
            OnNewGameStarting?.Invoke(this, EventArgs.Empty);
        }
        private void EndScreen_OnLeaderboardExpired(object sender, EventArgs e)
        {
            EndScreen.OnLeaderboardExpired -= EndScreen_OnLeaderboardExpired;
            UnityEngine.Object.Destroy(EndScreen);
            EndScreen = null;
            StartNextGame();
        }

        private void FlagOwnerChanged(object sender, OwnerChangeEventArgs e)
        {
            Flag flag = sender as Flag;
            F.LogWarning("Owner changed of flag " + flag.Name, ConsoleColor.White);
            // owner of flag changed (full caputure or loss)
            if(e.NewOwner == 1)
            {
                if(ObjectiveT1Index >= FlagRotation.Count - 1) // if t1 just capped the last flag
                {
                    DeclareWin(1);
                    ObjectiveT1Index = FlagRotation.Count - 1;
                } else
                {
                    flag.Owner = 1;
                    OnObjectiveChange?.Invoke(this, new OnObjectiveChangeEventArgs 
                    { oldFlagObj = flag, newFlagObj = FlagRotation[ObjectiveT1Index + 1], NewObj = ObjectiveT1Index + 1, OldObj = ObjectiveT1Index, Team = 1 });
                    ObjectiveT1Index++;
                }
            } else if(e.NewOwner == 2)
            {
                if (ObjectiveT2Index <= 1) // if t1 just capped the last flag
                {
                    DeclareWin(1);
                    ObjectiveT2Index = 0;
                }
                else
                {
                    flag.Owner = 2;
                    OnObjectiveChange?.Invoke(this, new OnObjectiveChangeEventArgs 
                    { oldFlagObj = flag, newFlagObj = FlagRotation[ObjectiveT2Index - 1], NewObj = ObjectiveT2Index - 1, OldObj = ObjectiveT2Index, Team = 2 });
                    ObjectiveT2Index--;
                }
            }
            flag.RecalcCappers(true);
            foreach(Player player in flag.PlayersOnFlag)
            {
                RefreshStaticUI(player.GetTeam(), player.channel.owner.transportConnection, player.channel.owner, flag);
            }
            foreach (SteamPlayer client in Provider.clients)
                client.SendChat("team_capture", UCWarfare.GetColor("team_capture"), TeamManager.TranslateName(e.NewOwner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(e.NewOwner), flag.Name, flag.TeamSpecificHexColor);
        }
        private void PlayerLeftFlagRadius(object sender, PlayerEventArgs e)
        {
            Flag flag = sender as Flag;
            // player walked out of flag
            ITransportConnection Channel = e.player.channel.owner.transportConnection;
            ulong team = e.player.GetTeam();
            F.Log("Player " + e.player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
            e.player.SendChat("left_cap_radius", UCWarfare.GetColor(team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            if (UCWarfare.Config.FlagSettings.UseUI)
                EffectManager.askEffectClearByID(UCWarfare.Config.FlagSettings.UIID, Channel);
        }
        public void RefreshStaticUI(ulong team, ITransportConnection Channel, SteamPlayer player, Flag flag)
        {
            if (!flag.T1Obj && team == 1)
            {
                if (flag.FullOwner == 1)
                    F.UIOrChat(team, F.UIOption.Secured, "secured", UCWarfare.GetColor("secured_team_1_chat"), Channel, player, Flag.MaxPoints, player.playerID.steamID.m_SteamID,
                        false, true, sendChatOverride: false);
                else
                    F.UIOrChat(team, F.UIOption.NoCap, "nocap", UCWarfare.GetColor("nocap_team_1_chat"), Channel, player, Flag.MaxPoints, player.playerID.steamID.m_SteamID,
                        false, true, sendChatOverride: false);
            }
            else if (!flag.T2Obj && team == 2)
            {
                if (flag.FullOwner == 2)
                {
                    if (flag.Owner == 2)
                        F.UIOrChat(team, F.UIOption.Secured, "secured", UCWarfare.GetColor("secured_team_2_chat"), Channel, player, Flag.MaxPoints, player.playerID.steamID.m_SteamID,
                            false, true, sendChatOverride: false);
                    else
                        F.UIOrChat(team, F.UIOption.NoCap, "nocap", UCWarfare.GetColor("nocap_team_2_chat"), Channel, player, Flag.MaxPoints, player.playerID.steamID.m_SteamID,
                            false, true, sendChatOverride: false);
                }
            }
            else
            {
                F.UIOrChat(team, F.UIOption.Blank, "", UCWarfare.GetColor($"default"), Channel, player, Flag.MaxPoints, player.playerID.steamID.m_SteamID, false, true, sendChatOverride: false);
            }
            if (flag.ID == ObjectiveTeam1.ID && team == 1)
            {
                if (flag.Team1TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= flag.Team2TotalPlayers || (flag.Team1TotalPlayers > 0 && flag.Team2TotalPlayers == 0))
                // if theres enough t1 players to capture or only t1 players CAPTURING/LOSING
                {
                    if (flag.IsFriendly(player) || flag.IsNeutral())
                    {
                        F.UIOrChat(team, F.UIOption.Capturing, "capturing", UCWarfare.GetColor(team == 1 ? "capturing_team_1_chat" : (team == 2 ? "capturing_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Losing, "losing", UCWarfare.GetColor(team == 1 ? "losing_team_1_chat" : (team == 2 ? "losing_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                }
                else if (flag.Team1TotalPlayers != 0 && flag.Team2TotalPlayers != 0)
                //if there are close to the same amount of players on both teams capturing (controlled by the config option) CONTESTED
                {
                    foreach (Player Capper in flag.PlayersOnFlag)
                    {
                        ulong CapperTeam = Capper.GetTeam();
                        F.UIOrChat(team, F.UIOption.Contested, "contested", UCWarfare.GetColor(CapperTeam == 1 ? "contested_team_1_chat" : (CapperTeam == 2 ? "contested_team_2_chat" : "default")),
                            Capper.channel.owner, flag.Points, player.playerID.steamID.m_SteamID, formatting: new object[] { flag.Name, flag.ColorString });
                    }
                }
                else if (flag.IsFriendly(player))
                {
                    if (flag.Points < Flag.MaxPoints)
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "clearing", UCWarfare.GetColor(team == 1 ? "clearing_team_1_chat" : (team == 2 ? "clearing_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "secured", UCWarfare.GetColor(team == 1 ? "secured_team_1_chat" : (team == 2 ? "secured_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                }
            }
            else if (flag.ID == ObjectiveTeam2.ID && team == 2)
            {
                if (flag.Team2TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= flag.Team1TotalPlayers || (flag.Team2TotalPlayers > 0 && flag.Team1TotalPlayers == 0))
                {
                    if (flag.IsFriendly(player) || flag.IsNeutral())
                    {
                        F.UIOrChat(team, F.UIOption.Capturing, "capturing", UCWarfare.GetColor(team == 1 ? "capturing_team_1_chat" : (team == 2 ? "capturing_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Losing, "losing", UCWarfare.GetColor(team == 1 ? "losing_team_1_chat" : (team == 2 ? "losing_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                }
                else if (flag.Team2TotalPlayers != 0 && flag.Team1TotalPlayers != 0)
                {
                    foreach (Player Capper in flag.PlayersOnFlag)
                    {
                        ulong CapperTeam = Capper.GetTeam();
                        F.UIOrChat(team, F.UIOption.Contested, "contested", UCWarfare.GetColor(CapperTeam == 1 ? "contested_team_1_chat" : (CapperTeam == 2 ? "contested_team_2_chat" : "default")),
                            Capper.channel.owner, flag.Points, player.playerID.steamID.m_SteamID, formatting: new object[] { flag.Name, flag.ColorString });
                    }
                }
                else if (flag.IsFriendly(player))
                {
                    if (flag.Points > -1 * Flag.MaxPoints)
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "clearing", UCWarfare.GetColor(team == 1 ? "clearing_team_1_chat" : (team == 2 ? "clearing_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                    else
                    {
                        F.UIOrChat(team, F.UIOption.Clearing, "secured", UCWarfare.GetColor(team == 1 ? "secured_team_1_chat" : (team == 2 ? "secured_team_2_chat" : "default")), Channel,
                            player, flag.Points, player.playerID.steamID.m_SteamID);
                    }
                }
            }
        }
        private void PlayerEnteredFlagRadius(object sender, PlayerEventArgs e)
        {
            Flag flag = sender as Flag;
            // player walked into flag
            ITransportConnection Channel = e.player.channel.owner.transportConnection;
            ulong team = e.player.GetTeam();
            F.LogWarning("Player " + e.player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
            e.player.SendChat("entered_cap_radius", UCWarfare.GetColor(team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            RefreshStaticUI(team, Channel, e.player.channel.owner, flag);
        }
        public void Dispose()
        {
            DisposeFlags();
            GC.SuppressFinalize(this);
        }
        public void DisposeFlags()
        {
            foreach (Flag flag in FlagRotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
                flag.Dispose();
            }
            FlagRotation.Clear();
        }
        public void ResetFlags()
        {
            foreach (Flag flag in FlagRotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
                flag.ResetFlag();
            }
            FlagRotation.Clear();
        }
        public void EvaluatePoints()
        {
            if(State == EState.ACTIVE)
                foreach (Flag flag in FlagRotation.Where(f => f.PlayersOnFlag.Count > 0))
                    flag.EvaluatePoints();
        }
    }
    public enum EState : byte
    {
        ACTIVE,
        PAUSED,
        FINISHED,
        LOADING
    }
}