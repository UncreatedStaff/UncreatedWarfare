using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Squads
{
    public class SquadManager : IDisposable
    {
        public static Config<SquadConfigData> config;
        public static List<Squad> Squads;
        internal static ushort squadListID = 0;
        internal const short squadListKey = 12001;
        internal static ushort squadMenuID = 0;
        internal const short squadMenuKey = 12002;
        internal static ushort rallyID = 0;
        internal static ushort orderID = 0;
        internal const short rallyKey = 12003;

        // temporary until effects are upgraded to using GUIDs
        internal static void TempCacheEffectIDs()
        {
            if (Assets.find(Gamemode.Config.UI.SquadListGUID) is EffectAsset squadList)
                squadListID = squadList.id;
            if (Assets.find(Gamemode.Config.UI.SquadMenuGUID) is EffectAsset squadMenu)
                squadMenuID = squadMenu.id;
            if (Assets.find(Gamemode.Config.UI.RallyGUID) is EffectAsset rally)
                rallyID = rally.id;
            if (Assets.find(Gamemode.Config.UI.OrderUI) is EffectAsset order)
                orderID = order.id;
            L.Log("Found squad UIs: " + squadListID + ", " + squadMenuID + ", " + rallyID + ", " + orderID);
        }
        public static readonly string[] NAMES =
        {
            "ALPHA",
            "BRAVO",
            "CHARLIE",
            "DELTA",
            "ECHO",
            "FOXTROT",
            "GOLF",
            "HOTEL"
        };
        public SquadManager()
        {
            config = new Config<SquadConfigData>(Data.SquadStorage, "config.json");

            Squads = new List<Squad>();
            KitManager.OnKitChanged += OnKitChanged;
        }
        private static void OnKitChanged(UCPlayer player, Kit kit, string oldkit)
        {
            if (player.Squad != null)
            {
                ReplicateKitChange(player);
            }
        }
        public static void OnGroupChanged(SteamPlayer steamplayer, ulong oldGroup, ulong newGroup)
        {
            UCPlayer player = UCPlayer.FromSteamPlayer(steamplayer);
            if (player == null) return;
            if (player.Squad != null)
            {
                LeaveSquad(player, player.Squad);
            }
            ulong team = newGroup.GetTeam();
            if (team == 1 || team == 2)
                SendSquadList(player, team);
        }
        public static void ClearAll(Player player)
        {
            EffectManager.askEffectClearByID(squadListID, player.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(squadMenuID, player.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(rallyID, player.channel.owner.transportConnection);
        }
        public static void ClearList(Player player)
        {
            EffectManager.askEffectClearByID(squadListID, player.channel.owner.transportConnection);
        }
        public static void ClearMenu(Player player)
        {
            EffectManager.askEffectClearByID(squadMenuID, player.channel.owner.transportConnection);
        }
        public static void ClearRally(Player player)
        {
            EffectManager.askEffectClearByID(rallyID, player.channel.owner.transportConnection);
        }
        public static void SendSquadMenu(UCPlayer player, Squad squad, bool holdMemberCountUpdate = false)
        {
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            EffectManager.sendUIEffect(squadMenuID, squadMenuKey, c, true);
            EffectManager.sendUIEffectText(squadMenuKey, c, true, "Heading", Translation.Translate($"squad_ui_header_name", player, squad.Name, squad.Members.Count.ToString(Data.Locale)));
            EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "Locked", squad.IsLocked);
            int i = 0;
            for (; i < squad.Members.Count; i++)
            {
                string i2 = i.ToString();
                EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "M" + i2, true);
                EffectManager.sendUIEffectText(squadMenuKey, c, true, "MN" + i2,
                    Translation.Translate("squad_ui_player_name", player, F.GetPlayerOriginalNames(squad.Members[i]).NickName));
                EffectManager.sendUIEffectText(squadMenuKey, c, true, "MI" + i2, squad.Members[i].Icon.ToString());
            }
            for (; i < Gamemode.Config.UI.MaxSquadMembers; i++)
            {
                EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "M" + i.ToString(), false);
            }
            if (!holdMemberCountUpdate)
            {
                int s2 = 1;
                EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "S0", true);
                EffectManager.sendUIEffectText(squadMenuKey, c, true, "SN0", Translation.Translate("squad_ui_expanded", player));
                for (int s = 0; s < Squads.Count; s++)
                {
                    if (Squads[s] == squad || Squads[s].Team != squad.Team) continue;
                    string s22 = s2.ToString();
                    EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "S" + s22, true);
                    EffectManager.sendUIEffectText(squadMenuKey, c, true, "SN" + s22,
                        Translation.Translate(Squads[s].IsLocked ? "squad_ui_player_count_small_locked" : "squad_ui_player_count_small", player, Squads[s].Members.Count.ToString(Data.Locale)));
                    s2++;
                }
                for (; s2 < Gamemode.Config.UI.MaxSquads; s2++)
                {
                    EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "S" + s2.ToString(), false);
                }
            }
        }
        // assumes ui is already on screen
        public static void UpdateUIMemberCount(ulong team)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer player = PlayerManager.OnlinePlayers[i];
                if (player.GetTeam() != team) continue;
                ITransportConnection c = player.Player.channel.owner.transportConnection;
                if (player.Squad != null) // if the player's in a squad update the squad menu other squad's list, else update the main squad list.
                {
                    int s2 = 1;
                    EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "S0", true);                                //  ... box
                    EffectManager.sendUIEffectText(squadMenuKey, c, true, "S0", Translation.Translate("squad_ui_expanded", player));  //  "     "
                    for (int s = 0; s < Squads.Count; s++)
                    {
                        if (Squads[s] == player.Squad) continue;
                        string s22 = s2.ToString();
                        EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "S" + s22, true);
                        EffectManager.sendUIEffectText(squadMenuKey, c, true, "SN" + s22,
                            Translation.Translate(Squads[s].IsLocked ? "squad_ui_player_count_small_locked" : "squad_ui_player_count_small", player, Squads[s].Members.Count.ToString(Data.Locale)));
                        s2++;
                    }
                    for (; s2 < Gamemode.Config.UI.MaxSquads; s2++)
                    {
                        EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "S" + s2.ToString(), false);
                    }
                }
                else
                {
                    int s2 = 0;
                    for (int s = 0; s < Squads.Count; s++)
                    {
                        if (Squads[s].Team != team) continue;
                        Squad sq = Squads[s];
                        string s22 = s2.ToString();
                        EffectManager.sendUIEffectVisibility(squadListKey, c, true, s22, true);
                        EffectManager.sendUIEffectText(squadListKey, c, true, "N" + s22,
                            RallyManager.HasRally(sq, out _) ? Translation.Translate("squad_ui_leader_name", player, sq.Name).Colorize("5eff87") : Translation.Translate("squad_ui_leader_name", player, sq.Name));
                        EffectManager.sendUIEffectText(squadListKey, c, true, "M" + s22, 
                            Translation.Translate("squad_ui_player_count", player, sq.IsLocked ? Gamemode.Config.UI.LockIcon + "  " : "", sq.Members.Count.ToString(Data.Locale)));
                        s2++;
                    }
                    for (; s2 < Gamemode.Config.UI.MaxSquads; s2++)
                    {
                        EffectManager.sendUIEffectVisibility(squadListKey, c, true, s2.ToString(), false);
                    }
                }
            }
        }

        public static void OnPlayerJoined(UCPlayer player, string squadName)
        {
            ulong team = player.GetTeam();
            Squad squad = Squads.Find(s => s.Name == squadName && s.Team == team);

            if (squad != null && !squad.IsFull())
            {
                JoinSquad(player, squad);
            }
            else
            {
                SendSquadList(player, team);
            }
        }
        public static void SendSquadList(UCPlayer player) => SendSquadList(player, player.GetTeam());
        public static void SendSquadList(UCPlayer player, ulong team)
        {
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            EffectManager.sendUIEffect(squadListID, squadListKey, c, true);
            int s2 = 0;
            for (int s = 0; s < Squads.Count; s++)
            {
                if (Squads[s].Team != team) continue;
                if (s2 == 0)
                    EffectManager.sendUIEffectVisibility(squadListKey, c, true, "Header", true);
                Squad sq = Squads[s];
                string s22 = s2.ToString();
                EffectManager.sendUIEffectVisibility(squadListKey, c, true, s22, true);
                EffectManager.sendUIEffectText(squadListKey, c, true, "N" + s22, 
                    RallyManager.HasRally(sq, out _) ? Translation.Translate("squad_ui_leader_name", player, sq.Name).Colorize("5eff87") : Translation.Translate("squad_ui_leader_name", player, sq.Name));
                EffectManager.sendUIEffectText(squadListKey, c, true, "M" + s22,
                    Translation.Translate("squad_ui_player_count", player, sq.IsLocked ? Gamemode.Config.UI.LockIcon + "  " : "", sq.Members.Count.ToString(Data.Locale)));
                s2++;
            }
            for (; s2 < Gamemode.Config.UI.MaxSquads; s2++)
            {
                if (s2 == 0)
                    EffectManager.sendUIEffectVisibility(squadListKey, c, true, "Header", false);
                EffectManager.sendUIEffectVisibility(squadListKey, c, true, s2.ToString(), false);
            }
        }
        public static void ReplicateLockSquad(Squad squad)
        {
            int index = 0;
            for (int i = 0; i < Squads.Count; i++)
            {
                if (Squads[i].Team != squad.Team) continue;
                if (Squads[i] == squad) break;
                index++;
            }
            string m = "M" + index.ToString();
            int index2 = 1;
            for (int i = 0; i < Squads.Count; i++)
            {
                if (Squads[i].Team != squad.Team) continue;
                if (Squads[i] != squad)
                    index2++;
            }
            string sn = "SN" + index2.ToString();
            for (int i = 0; i < squad.Members.Count; i++)
            {
                EffectManager.sendUIEffectVisibility(squadMenuKey, squad.Members[i].Player.channel.owner.transportConnection, true, "Locked", squad.IsLocked);
            }
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (squad.Team != PlayerManager.OnlinePlayers[i].GetTeam() || squad == PlayerManager.OnlinePlayers[i].Squad) continue;
                UCPlayer player = PlayerManager.OnlinePlayers[i];
                ITransportConnection c = player.Player.channel.owner.transportConnection;
                if (player.Squad == null)
                {
                    EffectManager.sendUIEffectText(squadListKey, c, true, m,
                        Translation.Translate("squad_ui_player_count", player, squad.IsLocked ? Gamemode.Config.UI.LockIcon + "  " : "", squad.Members.Count.ToString(Data.Locale)));
                }
                else
                {
                    EffectManager.sendUIEffectText(squadMenuKey, c, true, sn,
                        Translation.Translate(squad.IsLocked ? "squad_ui_player_count_small_locked" : "squad_ui_player_count_small", player, squad.Members.Count.ToString(Data.Locale)));
                }
            }
        }
        public static void ReplicateKitChange(UCPlayer player)
        {
            for (int i = 0; i < player.Squad.Members.Count; i++)
            {
                EffectManager.sendUIEffectText(squadMenuKey, player.Squad.Members[i].Player.channel.owner.transportConnection, true, "MI" + i.ToString(), player.Squad.Members[i].Icon.ToString());
            }
        }
        public static void UpdateMemberList(Squad squad)
        {
            for (int m = 0; m < squad.Members.Count; m++)
            {
                ITransportConnection c = squad.Members[m].Player.channel.owner.transportConnection;
                int i = 0;
                for (; i < squad.Members.Count; i++)
                {
                    string i2 = i.ToString();
                    EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "M" + i2, true);
                    EffectManager.sendUIEffectText(squadMenuKey, c, true, "MN" + i2,
                        Translation.Translate("squad_ui_player_name", squad.Members[m], F.GetPlayerOriginalNames(squad.Members[i]).NickName));
                    EffectManager.sendUIEffectText(squadMenuKey, c, true, "MI" + i2, squad.Members[i].Icon.ToString());
                }
                for (; i < Gamemode.Config.UI.MaxSquadMembers; i++)
                {
                    EffectManager.sendUIEffectVisibility(squadMenuKey, c, true, "M" + i.ToString(), false);
                }
            }
        }
        public static void OnPlayerDisconnected(UCPlayer player)
        {
            if (player.Squad != null)
                LeaveSquad(player, player.Squad);
        }

        public static string FindUnusedSquadName(ulong team)
        {
            for (int n = 0; n < NAMES.Length; n++)
            {
                string name = NAMES[n];
                for (int i = 0; i < Squads.Count; i++)
                {
                    if (Squads[i].Team == team)
                    {
                        if (name == Squads[i].Name)
                            break;
                        else
                            return name;
                    }
                }
            }
            return NAMES[NAMES.Length - 1];
        }

        public static Squad CreateSquad(UCPlayer leader, ulong team, EBranch branch)
        {
            string name = FindUnusedSquadName(team);
            Squad squad = new Squad(name, leader, team, branch);
            Squads.Add(squad);
            SortSquadListABC();
            leader.Squad = squad;

            ClearList(leader.Player);
            SendSquadMenu(leader, squad);
            return squad;
        }
        private static void SortSquadListABC()
        {
            Squads.Sort((a, b) => a.Name[0].CompareTo(b.Name[0]));
        }
        public static void JoinSquad(UCPlayer player, Squad squad)
        {
            foreach (UCPlayer p in squad.Members)
            {
                if (p.Steam64 != player.Steam64)
                    p.Message("squad_player_joined", player.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_joined", squad.Name);
            }

            squad.Members.Add(player);
            SortMembers(squad);

            player.Squad = squad;

            ClearList(player.Player);
            SendSquadMenu(player, squad, holdMemberCountUpdate: true);
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out RallyPoint rally))
                rally.ShowUIForSquad();

            PlayerManager.ApplyToOnline();
        }
        public static void SortMembers(Squad squad)
        {
            squad.Members.Sort(delegate (UCPlayer a, UCPlayer b)
            {
                int o = b.Medals.TotalTW.CompareTo(a.Medals.TotalTW); // sort players by their officer status
                return o == 0 ? b.CurrentRank.TotalXP.CompareTo(a.CurrentRank.TotalXP) : o;
            });
            if (squad.Leader != null)
            {
                squad.Members.RemoveAll(x => x.Steam64 == squad.Leader.Steam64);
                squad.Members.Insert(0, squad.Leader);
            }
        }
        public static void LeaveSquad(UCPlayer player, Squad squad)
        {
            player.Message("squad_left");

            squad.Members.Remove(player);
            bool willNeedNewLeader = squad.Leader == null || squad.Leader.CSteamID.m_SteamID == player.CSteamID.m_SteamID;
            player.Squad = null;
            ClearMenu(player.Player);

            if (squad.Members.Count == 0)
            {
                Squads.Remove(squad);

                if (squad.Leader != null)
                {
                    squad.Leader.Message("squad_disbanded");
                    if (squad.Leader.KitClass == EClass.SQUADLEADER)
                        KitManager.TryGiveUnarmedKit(squad.Leader);
                }

                UpdateUIMemberCount(squad.Team);

                if (RallyManager.HasRally(squad, out RallyPoint rally1))
                {
                    if (rally1.drop != null && Regions.tryGetCoordinate(rally1.drop.model.position, out byte x, out byte y))
                        BarricadeManager.destroyBarricade(rally1.drop, x, y, ushort.MaxValue);

                    RallyManager.TryDeleteRallyPoint(rally1.structure.instanceID);
                }

                PlayerManager.ApplyToOnline();

                return;
            }
            SortMembers(squad);
            if (willNeedNewLeader)
            {
                squad.Leader = squad.Members[0]; // goes to the best officer, then the best xp
                squad.Leader.Message("squad_squadleader", squad.Leader.SteamPlayer.playerID.nickName);
            }


            for (int i = 0; i < squad.Members.Count; i++)
            {
                UCPlayer p = squad.Members[i];
                if (p.Steam64 != player.Steam64)
                    p.Message("squad_player_left", player.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_left", squad.Name);
            }
            UpdateMemberList(squad);
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out RallyPoint rally2))
                rally2.ClearUIForPlayer(player);

            SendSquadList(player);

            PlayerManager.ApplyToOnline();
        }
        public static void DisbandSquad(Squad squad)
        {
            Squads.RemoveAll(s => s.Name == squad.Name);

            for (int i = 0; i < squad.Members.Count; i++)
            {
                UCPlayer member = squad.Members[i];
                member.Squad = null;

                member.Message("squad_disbanded");
                ClearMenu(member.Player);
                SendSquadList(member);
            }
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out RallyPoint rally))
            {
                if (rally.drop != null && Regions.tryGetCoordinate(rally.drop.model.position, out byte x, out byte y))
                    BarricadeManager.destroyBarricade(rally.drop, x, y, ushort.MaxValue);

                RallyManager.TryDeleteRallyPoint(rally.structure.instanceID);
            }

            PlayerManager.ApplyToOnline();
        }
        public static void KickPlayerFromSquad(UCPlayer player, Squad squad)
        {
            if (player == null || squad == null || squad.Members.Count < 2)
                return;

            if (squad.Members.Remove(player))
                player.Message("squad_kicked", squad.Name);

            SortMembers(squad);
            for (int i = 0; i < squad.Members.Count; i++)
            {
                UCPlayer p = squad.Members[i];
                if (p.Steam64 != player.Steam64)
                    p.Message("squad_player_kicked", F.GetPlayerOriginalNames(player).NickName);
            }
            UpdateMemberList(squad);
            player.Squad = null;
            ClearMenu(player.Player);
            SendSquadList(player);
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out RallyPoint rally))
                rally.ClearUIForPlayer(player);

            PlayerManager.ApplyToOnline();
        }
        public static void PromoteToLeader(Squad squad, UCPlayer newLeader)
        {
            if (squad.Leader.KitClass == EClass.SQUADLEADER)
                KitManager.TryGiveUnarmedKit(squad.Leader);

            squad.Leader = newLeader;

            for (int i = 0; i < squad.Members.Count; i++)
            {
                UCPlayer p = squad.Members[i];
                if (p.CSteamID != squad.Leader.CSteamID)
                    p.Message("squad_player_promoted", newLeader.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_promoted", squad.Leader.SteamPlayer.playerID.nickName);
            }

            SortMembers(squad);
            UpdateMemberList(squad);
        }
        public static bool FindSquad(string input, ulong teamID, out Squad squad)
        {
            List<Squad> friendlySquads = Squads.Where(s => s.Team == teamID).ToList();
            string name = input.ToLower();
            if (name.Length == 1)
            {
                char let = char.ToLower(name[0]);
                if (let >= 'a' && let <= 'h')
                {
                    if (name[0] == 'a')
                    {
                        name = NAMES[0];
                    }
                    else if (name[0] == 'b')
                    {
                        name = NAMES[1];
                    }
                    else if (name[0] == 'c')
                    {
                        name = NAMES[2];
                    }
                    else if (name[0] == 'd')
                    {
                        name = NAMES[3];
                    }
                    else if (name[0] == 'e')
                    {
                        name = NAMES[4];
                    }
                    else if (name[0] == 'f')
                    {
                        name = NAMES[5];
                    }
                    else if (name[0] == 'g')
                    {
                        name = NAMES[6];
                    }
                    else if (name[0] == 'h')
                    {
                        name = NAMES[7];
                    }
                }
            }
            squad = friendlySquads.Find(s => name == s.Name.ToLower() || s.Name.ToLower().Contains(name.ToLower()));
            return squad != null;
        }
        public static void SetLocked(Squad squad, bool value)
        {
            squad.IsLocked = value;
            ReplicateLockSquad(squad);
        }
        public void Dispose()
        {
            KitManager.OnKitChanged -= OnKitChanged;
        }
    }

    public class Squad : IEnumerable<UCPlayer>
    {
        public string Name;
        public ulong Team;
        public EBranch Branch;
        public bool IsLocked;
        public UCPlayer Leader;
        public List<UCPlayer> Members;
        public Squad(string name, UCPlayer leader, ulong team, EBranch branch)
        {
            Name = name;
            Team = team;
            Branch = branch;
            Leader = leader;
            IsLocked = false;
            Members = new List<UCPlayer> { leader };
        }

        public IEnumerator<UCPlayer> GetEnumerator() => Members.GetEnumerator();

        public bool IsFull() => Members.Count >= 6;
        public bool IsNotSolo() => Members.Count > 1;

        IEnumerator IEnumerable.GetEnumerator() => Members.GetEnumerator();
        public IEnumerator<ITransportConnection> EnumerateMembers()
        {
            IEnumerator<UCPlayer> players = Members.GetEnumerator();
            while (players.MoveNext())
                yield return players.Current.Player.channel.owner.transportConnection;
            players.Dispose();
        }
    }

    public class SquadConfigData : ConfigData
    {
        public ushort RallyTimer;
        public float RallyDespawnDistance;
        public int SquadDisconnectTime;
        public Dictionary<EClass, ClassConfig> Classes;
        public ushort EmptyMarker;
        public ushort SquadLeaderEmptyMarker;
        public ushort MortarMarker;
        public ushort InjuredMarker;
        public ushort MedicMarker;
        public float MedicRange;
        public int MaxSquadNameLength;

        public override void SetDefaults()
        {
            RallyTimer = 45;
            RallyDespawnDistance = 30;
            EmptyMarker = 36100;
            SquadLeaderEmptyMarker = 36130;
            MortarMarker = 36120;
            InjuredMarker = 36121;
            MedicMarker = 36122;
            MedicRange = 300f;
            SquadDisconnectTime = 120;
            MaxSquadNameLength = 16;
            Classes = new Dictionary<EClass, ClassConfig>
            {
                { EClass.NONE, new ClassConfig('±', 36101, 36131) },
                { EClass.UNARMED, new ClassConfig('±', 36101, 36131) },
                { EClass.SQUADLEADER, new ClassConfig('¦', 36102, 36132) },
                { EClass.RIFLEMAN, new ClassConfig('¡', 36103, 36133) },
                { EClass.MEDIC, new ClassConfig('¢', 36104, 36134) },
                { EClass.BREACHER, new ClassConfig('¤', 36105, 36135) },
                { EClass.AUTOMATIC_RIFLEMAN, new ClassConfig('¥', 36106, 36136) },
                { EClass.GRENADIER, new ClassConfig('¬', 36107, 36137) },
                { EClass.MACHINE_GUNNER, new ClassConfig('«', 36108, 36138) },
                { EClass.LAT, new ClassConfig('®', 36109, 36139) },
                { EClass.HAT, new ClassConfig('¯', 36110, 36140) },
                { EClass.MARKSMAN, new ClassConfig('¨', 36111, 36141) },
                { EClass.SNIPER, new ClassConfig('£', 36112, 36142) },
                { EClass.AP_RIFLEMAN, new ClassConfig('©', 36113, 36143) },
                { EClass.COMBAT_ENGINEER, new ClassConfig('ª', 36114, 36144) },
                { EClass.CREWMAN, new ClassConfig('§', 36115, 36145) },
                { EClass.PILOT, new ClassConfig('°', 36116, 36146) },
                { EClass.SPEC_OPS, new ClassConfig('À', 36117, 36147) },
            };
        }

        public SquadConfigData() { }
    }

    public class ClassConfig
    {
        public char Icon;
        public ushort MarkerEffect;
        public ushort SquadLeaderMarkerEffect;
        [Newtonsoft.Json.JsonConstructor]
        public ClassConfig(char Icon, ushort MarkerEffect, ushort SquadLeaderMarkerEffect)
        {
            this.Icon = Icon;
            this.MarkerEffect = MarkerEffect;
            this.SquadLeaderMarkerEffect = SquadLeaderMarkerEffect;
        }
    }
}
