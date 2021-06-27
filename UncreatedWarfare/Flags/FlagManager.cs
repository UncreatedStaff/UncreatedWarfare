using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Flags
{
    public delegate void FlagCapturedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
    public delegate void FlagNeutralizedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);

    public class FlagManager : IDisposable
    {
        public const string PROGRESS_CHARS = "¶·¸¹º»:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public const char PLAYER_ICON = '³';
        public List<Flag> FlagRotation { get; private set; }
        public List<Flag> AllFlags { get; private set; }
        const int FLAGS_PER_LEVEL_MAX = 2;
        const int LIST_UI_COUNT = 10;
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
        public bool isScreenUp = false;
        public Dictionary<ulong, int> OnFlag { get; private set; }
        public int ObjectiveT1Index;
        public int ObjectiveT2Index;
        public Flag ObjectiveTeam1 { get => FlagRotation[ObjectiveT1Index]; }
        public Flag ObjectiveTeam2 { get => FlagRotation[ObjectiveT2Index]; }
        private EndScreenLeaderboard EndScreen;
        private string shutdownMessage = string.Empty;
        private bool shutdownAfterGame = false;
        private ulong shutdownPlayer = 0;
        public EState State { get => _state; set
            {
                EState oldState = _state;
                _state = value;
                OnStateChanged?.Invoke(this, new OnStateChangedEventArgs() { NewState = _state, OldState = oldState });
            } 
        }
        internal void ShutdownAfterGame(string reason, ulong player)
        {
            shutdownAfterGame = true;
            shutdownMessage = reason;
            shutdownPlayer = player;
        }
        
        internal void CancelShutdownAfterGame()
        {
            shutdownAfterGame = false;
            shutdownMessage = string.Empty;
            shutdownPlayer = 0;
        }

        private EState _state;
        public FlagManager()
        {
            this._state = EState.LOADING;
            FlagRotation = new List<Flag>();
            AllFlags = new List<Flag>();
            OnFlag = new Dictionary<ulong, int>();
            OnObjectiveChange += OnObjectiveChangeAction;
        }
        private void OnObjectiveChangeAction(object sender, OnObjectiveChangeEventArgs e)
        {
            if(e.Team == 0)
            {
                F.Log("Flag neutralized.");
            } else
            {
                if (Data.GameStats != null)
                    Data.GameStats.totalFlagOwnerChanges++;
                F.Log("Team 1 objective: " + ObjectiveTeam1.Name + ", Team 2 objective: " + ObjectiveTeam2.Name, ConsoleColor.DarkYellow);
                if (UCWarfare.Config.FlagSettings.DiscoveryForesight > 0)
                {
                    if (e.Team == 1)
                    {
                        for (int i = e.newFlagObj.index; i < e.newFlagObj.index + UCWarfare.Config.FlagSettings.DiscoveryForesight; i++)
                        {
                            if (i >= FlagRotation.Count || i < 0) break;
                            FlagRotation[i].Discover(1);
                        }
                    } else if (e.Team == 2)
                    {
                        for (int i = e.newFlagObj.index; i > e.newFlagObj.index - UCWarfare.Config.FlagSettings.DiscoveryForesight; i--)
                        {
                            if (i >= FlagRotation.Count || i < 0) break;
                            FlagRotation[i].Discover(2);
                        }
                    }
                }
            }
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
        public static int FromMax(int cap) => Math.Abs(cap) >= Flag.MaxPoints ? PROGRESS_CHARS.Length - 1 : ((PROGRESS_CHARS.Length - 1) / Flag.MaxPoints) * Math.Abs(cap);
        public void ClearPlayersOnFlag() => OnFlag.Clear();
        public async Task LoadNewFlags()
        {
            if (AllFlags == null) return;
            await ResetFlags();
            OnFlag.Clear();
            if(UCWarfare.Config.FlagSettings.UseAutomaticLevelSensing)
            {
                FlagRotation = ObjectivePathing.CreatePath();
            } else
            {
                List<KeyValuePair<int, List<Flag>>> lvls = new List<KeyValuePair<int, List<Flag>>>();
                for (int i = 0; i < AllFlags.Count; i++)
                {
                    KeyValuePair<int, List<Flag>> flag = lvls.FirstOrDefault(x => x.Key == AllFlags[i].Level);
                    if (flag.Equals(default(KeyValuePair<int, List<Flag>>)))
                        lvls.Add(new KeyValuePair<int, List<Flag>>(AllFlags[i].Level, new List<Flag> { AllFlags[i] }));
                    else
                        flag.Value.Add(AllFlags[i]);
                }
                lvls.Sort((KeyValuePair<int, List<Flag>> a, KeyValuePair<int, List<Flag>> b) => a.Key.CompareTo(b.Key));
                for (int i = 0; i < lvls.Count; i++)
                {
                    int amtToAdd = lvls[i].Value.Count > FLAGS_PER_LEVEL_MAX ? FLAGS_PER_LEVEL_MAX : lvls[i].Value.Count;
                    int counter = 0;
                    while (counter < amtToAdd)
                    {
                        int index = UnityEngine.Random.Range(0, lvls[i].Value.Count - 1);
                        lvls[i].Value[index].index = FlagRotation.Count;
                        FlagRotation.Add(lvls[i].Value[index]);
                        lvls[i].Value.RemoveAt(index);
                        counter++;
                    }
                }
            }
            ObjectiveT1Index = 0;
            ObjectiveT2Index = FlagRotation.Count - 1;
            if(UCWarfare.Config.FlagSettings.DiscoveryForesight < 1)
            {
                F.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            } else
            {
                for (int i = 0; i < UCWarfare.Config.FlagSettings.DiscoveryForesight; i++)
                {
                    if (i >= FlagRotation.Count || i < 0) break;
                    FlagRotation[i].Discover(1);
                }
                for (int i = FlagRotation.Count - 1; i > FlagRotation.Count - 1 - UCWarfare.Config.FlagSettings.DiscoveryForesight; i--)
                {
                    if (i >= FlagRotation.Count || i < 0) break;
                    FlagRotation[i].Discover(2);
                }
            }
            foreach (Flag flag in FlagRotation)
            {
                flag.OnPlayerEntered += PlayerEnteredFlagRadius;
                flag.OnPlayerLeft += PlayerLeftFlagRadius;
                flag.OnOwnerChanged += FlagOwnerChanged;
                flag.OnPointsChanged += FlagPointsChanged;
            }
            foreach (SteamPlayer client in Provider.clients)
            {
                SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                ClearListUI(client.transportConnection);
                SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, client.GetTeam());
                await rtn;
            }
            PrintFlagRotation();
        }
        public void PrintFlagRotation()
        {
            F.Log("Team 1 objective: " + ObjectiveTeam1.Name + ", Team 2 objective: " + ObjectiveTeam2.Name, ConsoleColor.Green);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < FlagRotation.Count; i++)
            {
                sb.Append(i.ToString(Data.Locale) + ") (" + FlagRotation[i].index + ") " + FlagRotation[i].Name + " | Level: " + FlagRotation[i].Level.ToString(Data.Locale) + '\n');
            }
            F.Log(sb.ToString(), ConsoleColor.Green);
        }
        private void LoadAllFlags()
        {
            AllFlags.Clear();
            List<FlagData> flags = JSONMethods.ReadFlags();
            flags.Sort((FlagData a, FlagData b) => a.id.CompareTo(b.id));
            int i;
            for (i = 0; i < flags.Count; i++)
            {
                AllFlags.Add(new Flag(flags[i], this) { index = i });
            }
            F.Log("Loaded " + i.ToString(Data.Locale) + " flags into memory and cleared any existing old flags.", ConsoleColor.Magenta);
        }
        public struct SendUIParameters
        {
            public static readonly SendUIParameters Nil = GetDefault();
            public ulong team;
            public F.EFlagStatus status;
            public string chatTranslation;
            public Color chatColor;
            public int points;
            public bool sendChat;
            public bool sendUI;
            public bool absoluteCap;
            public bool overrideChatConfig;
            public object[] formatting;
            public int team1count;
            public int team2count;
            /// <summary>
            /// <para>Creates a parameter object that will be ignored, kind of the default for the struct.</para>
            /// <para><see cref="F.EFlagStatus.DONT_DISPLAY"/> tells 
            /// <see cref="F.UIOrChat(SendUIParameters, SteamPlayer, ITransportConnection, ulong)"/> not to send the message.</para>
            /// </summary>
            private SendUIParameters(F.EFlagStatus status)
            {
                this.team = 0;
                this.status = status;
                this.chatTranslation = "";
                this.chatColor = UCWarfare.GetColor("default");
                this.points = Flag.MaxPoints;
                this.sendChat = false;
                this.sendUI = false;
                this.absoluteCap = true;
                this.overrideChatConfig = false;
                this.team1count = 0;
                this.team2count = 0;
                this.formatting = new object[0];
            }
            public static SendUIParameters GetDefault() => new SendUIParameters(F.EFlagStatus.DONT_DISPLAY);
            public SendUIParameters(ulong team, F.EFlagStatus status, string chatTranslation, 
                Color chatColor, int points, bool sendChat, bool sendUI, bool absoluteCap, 
                bool overrideChatConfig, int team1count, int team2count, object[] formatting)
            {
                this.team = team;
                this.status = status;
                this.chatTranslation = chatTranslation;
                this.chatColor = chatColor;
                this.points = points;
                this.sendChat = sendChat;
                this.sendUI = sendUI;
                this.absoluteCap = absoluteCap;
                this.overrideChatConfig = overrideChatConfig;
                this.team1count = team1count;
                this.team2count = team2count;
                this.formatting = formatting;
            }
            public SendUIParameters(ulong team, F.EFlagStatus status, string chatTranslation, 
                Color chatColor, int points, int team1count, int team2count, params object[] formatting)
            {
                this.team = team;
                this.status = status;
                this.chatTranslation = chatTranslation;
                this.chatColor = chatColor;
                this.points = points;
                this.sendChat = true;
                this.sendUI = true;
                this.absoluteCap = true;
                this.overrideChatConfig = false;
                this.team1count = team1count;
                this.team2count = team2count;
                this.formatting = formatting;
            }
            public void SendToPlayer(SteamPlayer player, ITransportConnection connection) => F.UIOrChat(this, player, connection, player.playerID.steamID.m_SteamID);
        }
        private SendUIParameters ComputeUI(ulong team, Flag flag)
        {
            if (flag.LastDeltaPoints == 0)
            {
                if(flag.IsContested(out _))
                {
                    return new SendUIParameters(team, F.EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor($"contested_team_{team}_chat"),
                        flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers, flag.Name, flag.TeamSpecificHexColor);
                } else
                {
                    if(flag.IsObj(team))
                        return new SendUIParameters(team, F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_{team}_chat"),
                            flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers, flag.Name, flag.TeamSpecificHexColor);
                    else
                        return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"nocap_team_{team}_chat"),
                            Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers, flag.Name, flag.TeamSpecificHexColor);
                }
            } 
            else if (flag.LastDeltaPoints > 0)
            {
                if (team == 1)
                {
                    if (flag.Points > 0)
                    {
                        if(flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_1_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        } else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_1_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    } else
                    {
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_1_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        } else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_1_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    }
                } else
                {
                    if (flag.Points < 0)
                    {
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        } else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_2_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    }
                    else
                    {
                        if (flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        } else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_2_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    }
                }
            } else
            {
                if (team == 2)
                {
                    if (flag.Points < 0)
                    {
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_2_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_2_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    }
                    else
                    {
                        if (flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_2_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_2_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    }
                }
                else
                {
                    if (flag.Points > 0)
                    {
                        if (flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_1_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    }
                    else
                    {
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"), 
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_1_chat"), 
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                        }
                    }
                }
            }
        }
        public void ClearListUI(ITransportConnection player)
        {
            for (int i = 0; i < LIST_UI_COUNT; i++) unchecked
            {
                EffectManager.askEffectClearByID((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), player);
            }
        }
        public void SendFlagListUI(ITransportConnection player, ulong playerid, ulong team)
        {
            ClearListUI(player);
            if (team == 1 || team == 2)
            {
                for (int i = 0; i < LIST_UI_COUNT; i++)
                {
                    if (FlagRotation.Count <= i)
                    {
                        EffectManager.askEffectClearByID((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), player);
                    } else
                    {
                        int index = team == 1 ? i : FlagRotation.Count - i - 1;
                        if (FlagRotation[i] == default) continue;
                        unchecked
                        {
                            Flag flag = FlagRotation[index];
                            string nameprefix = string.Empty;
                            if (UCWarfare.Config.FlagSettings.ShowObjectives)
                            {
                                if (flag.T1Obj)
                                {
                                    nameprefix += $"<color=#{UCWarfare.GetColorHex("team_1_color")}>•</color>";
                                }
                                if (flag.T2Obj)
                                {
                                    nameprefix += $"<color=#{UCWarfare.GetColorHex("team_2_color")}>•</color>";
                                }
                            }
                            string objective = string.Empty;
                            if (flag.T1Obj)
                            {
                                if (team == 1)
                                    objective = "<color=#ffca61>µ</color>";
                                else if (team == 2 && flag.FullOwner == 2)
                                    objective = "<color=#ba70cc>´</color>";
                            }
                            else if (flag.T2Obj)
                            {
                                if (team == 2)
                                    objective = "<color=#ffca61>µ</color>";
                                else if (team == 1 && flag.FullOwner == 1)
                                    objective = "<color=#ba70cc>´</color>";
                            }


                            EffectManager.sendUIEffect((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), (short)(1000 + i), player, true, flag.Discovered(team) ?
                                $"<color=#{flag.TeamSpecificHexColor}>{nameprefix + flag.Name}</color>" :
                                $"<color=#{flag.TeamSpecificHexColor}>{nameprefix + F.Translate("undiscovered_flag", playerid)}</color>",
                                objective
                                );
                        }
                    }
                }
            } else if (team == 3)
            {
                for (int i = 0; i < LIST_UI_COUNT; i++)
                {
                    if (FlagRotation.Count <= i)
                    {
                        EffectManager.askEffectClearByID((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), player);
                    }
                    else
                    {
                        if (FlagRotation[i] == default) continue;
                        unchecked
                        {
                            Flag flag = FlagRotation[i];
                            string nameprefix = string.Empty;
                            if (UCWarfare.Config.FlagSettings.ShowObjectives)
                            {
                                if (flag.T1Obj)
                                {
                                    nameprefix += $"<color=#{UCWarfare.GetColorHex("team_1_color")}>•</color>";
                                }
                                if (flag.T2Obj)
                                {
                                    nameprefix += $"<color=#{UCWarfare.GetColorHex("team_2_color")}>•</color>";
                                }
                            }
                            string objective = string.Empty;
                            if (flag.T1Obj)
                            {
                                if (team == 1)
                                    objective = "<color=#ffca61>µ</color>";
                                else if (team == 2 && flag.FullOwner == 2)
                                    objective = "<color=#ba70cc>´</color>";
                            }
                            else if (flag.T2Obj)
                            {
                                if (team == 2)
                                    objective = "<color=#ffca61>µ</color>";
                                else if (team == 1 && flag.FullOwner == 1)
                                    objective = "<color=#ba70cc>´</color>";
                            }
                            EffectManager.sendUIEffect((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), (short)(1000 + i), player, true,
                                $"<color=#{flag.TeamSpecificHexColor}>{nameprefix + flag.Name}</color>" +
                                $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}",
                                objective);
                        }
                    }
                }
            }
        }
        private async Task FlagPointsChanged(int NewPoints, int OldPoints, Flag flag)
        {
            if (NewPoints == 0)
            {
                await flag.SetOwner(0);
            }
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            SendUIParameters sendT1;
            if (flag.Team1TotalPlayers > 0)
                sendT1 = ComputeUI(1, flag);
            else sendT1 = default;
            SendUIParameters sendT2;
            if (flag.Team2TotalPlayers > 0)
                sendT2 = ComputeUI(2, flag);
            else sendT2 = default;
            foreach (Player player in flag.PlayersOnFlag)
            {
                byte team = player.GetTeamByte();
                if (team == 1 && !sendT1.Equals(default)) sendT1.SendToPlayer(player.channel.owner, player.channel.owner.transportConnection);
                else if (team == 2 && !sendT2.Equals(default)) sendT2.SendToPlayer(player.channel.owner, player.channel.owner.transportConnection);
            }
            await rtn;
        }
        public class OnTeamWinEventArgs : EventArgs { public ulong team; }
        public class OnObjectiveChangeEventArgs : EventArgs { public Flag oldFlagObj; public Flag newFlagObj; public ulong Team; public int OldObj; public int NewObj; }
        public class OnStateChangedEventArgs : EventArgs { public EState NewState; public EState OldState; }
        public static event EventHandler<OnTeamWinEventArgs> OnTeamWinGame;
        public static event EventHandler<OnObjectiveChangeEventArgs> OnObjectiveChange;
        public static event EventHandler<OnStateChangedEventArgs> OnStateChanged;
        public static event EventHandler OnReady;
        public static event EventHandler OnNewGameStarting;
        public static event FlagCapturedHandler OnFlagCaptured;
        public static event FlagNeutralizedHandler OnFlagNeutralized;

        public async Task DeclareWin(ulong Team, bool showEndScreen = true)
        {
            F.Log(TeamManager.TranslateName(Team, 0) + " just won the game!", ConsoleColor.Cyan);
            foreach (SteamPlayer client in Provider.clients)
                client.SendChat("team_win", UCWarfare.GetColor("team_win"), TeamManager.TranslateName(Team, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(Team));
            this.State = EState.FINISHED;
            OnTeamWinGame?.Invoke(this, new OnTeamWinEventArgs { team = Team });
            if(showEndScreen)
            {
                EndScreen = UCWarfare.I.gameObject.AddComponent<EndScreenLeaderboard>();
                EndScreen.OnLeaderboardExpired += EndScreen_OnLeaderboardExpired;
                EndScreen.winner = Team;
                foreach (SteamPlayer client in Provider.clients)
                {
                    ClearListUI(client.transportConnection);
                }
                EndScreen.ShuttingDown = shutdownAfterGame;
                EndScreen.ShuttingDownMessage = shutdownMessage;
                EndScreen.ShuttingDownPlayer = shutdownPlayer;
                await EndScreen.EndGame();
                isScreenUp = true;
            }
        }
        public async Task StartNextGame()
        {
            F.Log("Loading new game.", ConsoleColor.Cyan);
            await LoadNewFlags();
            State = EState.ACTIVE;
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            EffectManager.ClearEffectByID_AllPlayers(UCWarfare.Config.FlagSettings.UIID);
            OnNewGameStarting?.Invoke(this, EventArgs.Empty);
            await rtn;
        }
        private async Task EndScreen_OnLeaderboardExpired()
        {
            EndScreen.OnLeaderboardExpired -= EndScreen_OnLeaderboardExpired;
            await StartNextGame();
            isScreenUp = false;
        }
        private async Task FlagOwnerChanged(ulong oldowner, ulong newowner, Flag flag)
        {
            F.Log($"Owner changed of flag {flag.Name} ({flag.index}) to Team {newowner} from Team {oldowner}", ConsoleColor.DarkYellow);
            // owner of flag changed (full caputure or loss)
            if (newowner == 1)
            {
                if (ObjectiveT1Index >= FlagRotation.Count - 1) // if t1 just capped the last flag
                {
                    await DeclareWin(newowner);
                    ObjectiveT1Index = FlagRotation.Count - 1;
                }
                else
                {
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    ObjectiveT1Index = flag.index + 1;
                    OnObjectiveChange?.Invoke(this, new OnObjectiveChangeEventArgs
                    { oldFlagObj = flag, newFlagObj = FlagRotation[ObjectiveT1Index], NewObj = ObjectiveT1Index, OldObj = flag.index, Team = newowner });
                    OnFlagCaptured?.Invoke(flag, newowner, oldowner);
                    await rtn;
                }
            }
            else if (newowner == 2)
            {
                if (ObjectiveT2Index <   1) // if t2 just capped the last flag
                {
                    await DeclareWin(newowner);
                    ObjectiveT2Index = 0;
                }
                else
                {
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    ObjectiveT2Index = flag.index - 1;
                    OnObjectiveChange?.Invoke(this, new OnObjectiveChangeEventArgs
                    { oldFlagObj = flag, newFlagObj = FlagRotation[ObjectiveT2Index], NewObj = ObjectiveT2Index, OldObj = flag.index, Team = newowner });
                    OnFlagCaptured?.Invoke(flag, newowner, oldowner);
                    await rtn;
                }
            }
            else if (newowner == 0)
            {
                if (oldowner == 1)
                {
                    int oldindex = ObjectiveT1Index;
                    ObjectiveT1Index = flag.index;
                    if (oldindex != flag.index)
                    {
                        SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                        OnObjectiveChange?.Invoke(this, new OnObjectiveChangeEventArgs
                        { oldFlagObj = FlagRotation[oldindex], newFlagObj = flag, NewObj = flag.index, OldObj = oldindex, Team = 0 });
                        OnFlagNeutralized?.Invoke(flag, 2, 1);
                        await rtn;
                    }
                }
                else if (oldowner == 2)
                {
                    int oldindex = ObjectiveT2Index;
                    ObjectiveT2Index = flag.index;
                    if (oldindex != flag.index)
                    {
                        SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                        OnObjectiveChange?.Invoke(this, new OnObjectiveChangeEventArgs
                        { oldFlagObj = FlagRotation[oldindex], newFlagObj = flag, NewObj = flag.index, OldObj = oldindex, Team = 0 });
                        OnFlagNeutralized?.Invoke(flag, 1, 2);
                        await rtn;
                    }
                }
            }
            SendUIParameters t1;
            if (flag.Team1TotalPlayers > 0)
                t1 = RefreshStaticUI(1, flag);
            else t1 = default;
            SendUIParameters t2;
            if (flag.Team2TotalPlayers > 0)
                t2 = RefreshStaticUI(2, flag);
            else t2 = default;
            if (!t1.Equals(default))
                foreach (Player player in flag.PlayersOnFlagTeam1)
                    t1.SendToPlayer(player.channel.owner, player.channel.owner.transportConnection);
            if (!t2.Equals(default))
                foreach (Player player in flag.PlayersOnFlagTeam2)
                    t2.SendToPlayer(player.channel.owner, player.channel.owner.transportConnection);
            if (newowner == 0)
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    ulong team = client.GetTeam();
                    client.SendChat("flag_neutralized", UCWarfare.GetColor("flag_neutralized"),
                        flag.Discovered(team) ? flag.Name : F.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                    SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team);

                        
                }
            }
            else
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    ulong team = client.GetTeam();
                    client.SendChat("team_capture", UCWarfare.GetColor("team_capture"), TeamManager.TranslateName(newowner, client.playerID.steamID.m_SteamID),
                        TeamManager.GetTeamHexColor(newowner), flag.Discovered(team) ? flag.Name : F.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                    SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team);
                }
            }
        }
        private void PlayerLeftFlagRadius(object sender, PlayerEventArgs e)
        {
            // player walked or tp'd out of flag
            if (sender is Flag flag)
            {
                ITransportConnection Channel = e.player.channel.owner.transportConnection;
                ulong team = e.player.GetTeam();
                F.Log("Player " + e.player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
                e.player.SendChat("left_cap_radius", UCWarfare.GetColor(team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
                if (UCWarfare.Config.FlagSettings.UseUI)
                    EffectManager.askEffectClearByID(UCWarfare.Config.FlagSettings.UIID, Channel);
                foreach (Player player in flag.PlayersOnFlag)
                    RefreshStaticUI(player.GetTeam(), flag).SendToPlayer(player.channel.owner, player.channel.owner.transportConnection);
            }
        }
        public SendUIParameters RefreshStaticUI(ulong team, Flag flag)
        {
            if (team != 1 && team != 2) return SendUIParameters.Nil;
            if (flag.IsAnObj)
            {
                return ComputeUI(team, flag); // if flag is objective send capturing ui.
            }
            else
            {
                if (flag.Owner == team)
                    return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"secured_team_{team}_chat"), 
                        Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                else if(flag.Owner == TeamManager.Other(team))
                    return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor($"notowned_team_{team}_chat"), 
                        Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
                else return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                        Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalPlayers);
            }
        }
        private void PlayerEnteredFlagRadius(object sender, PlayerEventArgs e)
        {
            Flag flag = sender as Flag;
            // player walked into flag
            ulong team = e.player.GetTeam();
            F.Log("Player " + e.player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
            e.player.SendChat("entered_cap_radius", UCWarfare.GetColor(team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            foreach (Player player in flag.PlayersOnFlag)
                RefreshStaticUI(player.GetTeam(), flag).SendToPlayer(player.channel.owner, player.channel.owner.transportConnection);
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
            OnObjectiveChange -= OnObjectiveChangeAction;
        }
        public async Task ResetFlags()
        {
            foreach (Flag flag in FlagRotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
                await flag.ResetFlag();
            }
            FlagRotation.Clear();
        }
        public async Task EvaluatePoints()
        {
            if(State == EState.ACTIVE)
                foreach (Flag flag in FlagRotation.Where(f => f.PlayersOnFlag.Count > 0))
                    await flag.EvaluatePoints();
        }
        public void PlayerJoined(SteamPlayer player)
        {
            if(isScreenUp && EndScreen != null)
            {
                EndScreen.SendScreenToPlayer(player);
            } else
            {
                SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, F.GetTeam(player));
            }
        }
        public void PlayerLeft(SteamPlayer player)
        {
            foreach(Flag flag in FlagRotation)
                flag.RecalcCappers(true);
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