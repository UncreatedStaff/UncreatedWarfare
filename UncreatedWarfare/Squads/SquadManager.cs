using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads.Commander;
using Uncreated.Warfare.Squads.UI;
using UnityEngine;

namespace Uncreated.Warfare.Squads;

public class SquadManager : ConfigSingleton<SquadsConfig, SquadConfigData>, IDeclareWinListener, IJoinedTeamListener, IUIListener, IGameTickListener
{
    public const int SQUAD_MAX_MEMBERS = 6;
    
    public SquadManager() : base("squad") { }

    public new static SquadConfigData Config => _singleton.IsLoaded() ? _singleton.ConfigurationFile.Data : null!;
    public static readonly List<Squad> Squads = new List<Squad>(12);
    private static SquadManager _singleton;
    public static readonly SquadMenuUI MenuUI = new SquadMenuUI();
    public static readonly SquadListUI ListUI = new SquadListUI();
    public static readonly UnturnedUI RallyUI = new UnturnedUI(Gamemode.Config.UIRally, hasElements: false);
    public static readonly string[] SquadNames =
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
    public static bool Loaded => _singleton.IsLoaded();
    public static SquadManager Singleton => _singleton;
    public Commanders Commanders;

    /// <summary>
    /// Called when the player joins, leaves, or is promoted to leader in a squad.
    /// </summary>
    public static event SquadUpdated? SquadStatusUpdated;
    public override void Load()
    {
        base.Load();
        Squads.Clear();
        KitManager.OnKitChanged += OnKitChanged;
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.PlayerLeaving += OnPlayerLeaving;
        Commanders = new Commanders();
        _singleton = this;
    }
    public override void Reload()
    {
        ClearSquads();
        base.Reload();
    }
    public override void Unload()
    {
        _singleton = null!;
        base.Unload();
        ClearSquads();
        EventDispatcher.PlayerLeaving -= OnPlayerLeaving;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        KitManager.OnKitChanged -= OnKitChanged;
        Commanders = null!;
    }
    private void ClearSquads()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            pl.Squad = null;
            ClearMenu(pl);
        }

        RallyManager.WipeAllRallies();

        Squads.Clear();
    }
    private static void OnKitChanged(UCPlayer player, Kit? kit, Kit? oldKit)
    {
        _singleton.IsLoaded();
        ReplicateKitChange(player);
        ulong team = player.GetTeam();
        UCPlayer? cmd = _singleton.Commanders.GetCommander(team);
        if (cmd != null && cmd.Steam64 == player.Steam64 && kit is not { SquadLevel: SquadLevel.Commander })
        {
            if (team == 1ul)
                _singleton.Commanders.ActiveCommanderTeam1 = null;
            else if (team == 2ul)
                _singleton.Commanders.ActiveCommanderTeam2 = null;
            L.LogDebug($"{cmd} is no longer the commander of team {team}.");
        }
    }
    private void OnPlayerLeaving(PlayerEvent e)
    {
        ulong team = e.Player.GetTeam();
        UCPlayer? cmd = _singleton.Commanders.GetCommander(team);
        if (cmd != null && cmd.Steam64 == e.Steam64)
        {
            if (team == 1ul)
                _singleton.Commanders.ActiveCommanderTeam1 = null;
            else if (team == 2ul)
                _singleton.Commanders.ActiveCommanderTeam2 = null;
        }
        if (e.Player.Squad != null)
            LeaveSquad(e.Player, e.Player.Squad);
    }
    void IDeclareWinListener.OnWinnerDeclared(ulong winner)
    {
        _singleton.Commanders.ActiveCommanderTeam1 = null;
        _singleton.Commanders.ActiveCommanderTeam2 = null;
    }
    private void OnGroupChanged(GroupChanged e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.Player.Squad != null)
        {
            LeaveSquad(e.Player, e.Player.Squad);
        }
        ulong team = e.NewGroup.GetTeam();
        if (team is > 0 and < 3)
            SendSquadList(e.Player, team);
        else
            ClearAll(e.Player);
    }
    public static void ClearAll(Player player)
    {
        MenuUI.ClearFromPlayer(player.channel.owner.transportConnection);
        ListUI.ClearFromPlayer(player.channel.owner.transportConnection);
        RallyUI.ClearFromPlayer(player.channel.owner.transportConnection);
    }
    public static void ClearList(Player player)
    {
        ListUI.ClearFromPlayer(player.channel.owner.transportConnection);
    }
    public static void ClearMenu(Player player)
    {
        MenuUI.ClearFromPlayer(player.channel.owner.transportConnection);
    }
    public static void ClearRally(Player player)
    {
        RallyUI.ClearFromPlayer(player.channel.owner.transportConnection);
    }
    public static void SendSquadMenu(UCPlayer player, Squad squad, bool holdMemberCountUpdate = false)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ITransportConnection c = player.Player.channel.owner.transportConnection;
        MenuUI.SendToPlayer(c);
        MenuUI.Header.SetText(c, T.SquadsUIHeaderPlayerCount.Translate(player, false, squad, squad.Members.Count, MenuUI.MemberParents.Length));
        MenuUI.Lock.SetVisibility(c, squad.IsLocked);
        int i = 0;
        int num = Math.Min(squad.Members.Count, MenuUI.MemberParents.Length);
        for (; i < num; i++)
        {
            MenuUI.MemberParents[i].SetVisibility(c, true);
            MenuUI.MemberNames[i].SetText(c, squad.Members[i].Name.NickName);
            MenuUI.MemberIcons[i].SetText(c, new string(squad.Members[i].Icon, 1));
        }
        for (; i < MenuUI.MemberParents.Length; i++)
        {
            MenuUI.MemberParents[i].SetVisibility(c, false);
        }
        if (!holdMemberCountUpdate)
        {
            int s2 = 0;
            MenuUI.OtherSquad_0.SetVisibility(c, true);
            MenuUI.OtherSquad_0_Text.SetText(c, T.SquadUIExpanded.Translate(player));
            for (int s = 0; s < Squads.Count; s++)
            {
                int sq;
                if (Squads[s] == squad || Squads[s].Team != squad.Team) continue;
                if ((sq = s2 + 1) >= MenuUI.OtherSquadParents.Length) break;

                MenuUI.OtherSquadParents[sq].SetVisibility(c, true);
                MenuUI.OtherSquadTexts[sq].SetText(c,
                    (Squads[s].IsLocked ? T.SquadsUIPlayerCountSmallLocked : T.SquadsUIPlayerCountSmall).Translate(player, false, Squads[s].Members.Count, MenuUI.MemberParents.Length));
                s2++;
            }
            for (; s2 < MenuUI.OtherSquadParents.Length - 1; s2++)
            {
                MenuUI.OtherSquadParents[s2 + 1].SetVisibility(c, false);
            }
        }
    }
    // assumes ui is already on screen
    public static void UpdateUIMemberCount(ulong team)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.GetTeam() != team) continue;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            if (player.Squad is not null) // if the player's in a squad update the squad menu other squad's list, else update the main squad list.
            {
                int s2 = 0;
                MenuUI.OtherSquad_0.SetVisibility(c, true);
                MenuUI.OtherSquad_0_Text.SetText(c, T.SquadUIExpanded.Translate(player));
                for (int s = 0; s < Squads.Count; s++)
                {
                    int sq;
                    if (Squads[s] == player.Squad || Squads[s].Team != team) continue;
                    if ((sq = s2 + 1) >= MenuUI.OtherSquadParents.Length) break;

                    MenuUI.OtherSquadParents[sq].SetVisibility(c, true);
                    MenuUI.OtherSquadTexts[sq].SetText(c,
                        (Squads[s].IsLocked ? T.SquadsUIPlayerCountSmallLocked : T.SquadsUIPlayerCountSmall).Translate(player, false, Squads[s].Members.Count, MenuUI.MemberParents.Length));
                    s2++;
                }
                for (; s2 < MenuUI.OtherSquadParents.Length - 1; s2++)
                {
                    MenuUI.OtherSquadParents[s2 + 1].SetVisibility(c, false);
                }
            }
            else
            {
                int s2 = 0;
                for (int s = 0; s < Squads.Count; s++)
                {
                    if (Squads[s].Team != team) continue;
                    if (s2 >= ListUI.Squads.Length) break;
                    Squad sq = Squads[s];
                    ListUI.Squads[s2].SetVisibility(c, true);
                    ListUI.SquadNames[s2].SetText(c, sq.HasRally ? sq.Name.Colorize("5eff87") : sq.Name);
                    ListUI.SquadMemberCounts[s2].SetText(player.Connection,
                        sq.IsLocked ?
                            T.SquadsUIPlayerCountListLocked.Translate(player, false, sq.Members.Count, MenuUI.MemberParents.Length, Gamemode.Config.UIIconLocked) :
                            T.SquadsUIPlayerCountList.Translate(player, false, sq.Members.Count, MenuUI.MemberParents.Length));
                    s2++;
                }
                for (; s2 < ListUI.Squads.Length; s2++)
                {
                    ListUI.Squads[s2].SetVisibility(c, false);
                }
            }
        }
    }
    void IJoinedTeamListener.OnJoinTeam(UCPlayer player, ulong team)
    {
        _singleton.IsLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player.Save.LastGame == Data.Gamemode.GameId && player.Save.SquadLeader != 0ul && !string.IsNullOrEmpty(player.Save.SquadName))
        {
            string sn = player.Save.SquadName;
            ulong pl = player.Save.SquadLeader;
            Squad squad = Squads.Find(s => s.Team == team && s.Name.Equals(sn, StringComparison.Ordinal) && s.Leader.Steam64 == pl);

            if (squad is not null && !squad.IsFull() && (!squad.IsLocked || player.Save.SquadLockedId == squad.LockedId))
            {
                JoinSquad(player, squad);
            }

            return;
        }
        
        SendSquadList(player, team);
    }
    public static void SendSquadListToTeam(ulong team)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            if (PlayerManager.OnlinePlayers[i].GetTeam() == team && PlayerManager.OnlinePlayers[i].Squad == null)
                SendSquadList(PlayerManager.OnlinePlayers[i], team);
        }
    }
    public static void SendSquadList(UCPlayer player) => SendSquadList(player, player.GetTeam());
    public static void SendSquadList(UCPlayer player, ulong team)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ITransportConnection c = player.Player.channel.owner.transportConnection;
        ListUI.SendToPlayer(c);
        int s2 = 0;
        for (int s = 0; s < Squads.Count; s++)
        {
            Squad sq = Squads[s];
            if (sq is null)
                Squads.RemoveAt(s--);
            else
            {
                if (sq.Team != team) continue;
                if (s2 == 0)
                    ListUI.Header.SetVisibility(c, true);
                ListUI.Squads[s2].SetVisibility(c, true);
                ListUI.SquadNames[s2].SetText(c, sq.HasRally ? sq.Name.Colorize(UCWarfare.GetColorHex("rally")) : sq.Name);
                ListUI.SquadMemberCounts[s2].SetText(player.Connection,
                    sq.IsLocked ?
                        T.SquadsUIPlayerCountListLocked.Translate(player, false, sq.Members.Count, MenuUI.MemberParents.Length, Gamemode.Config.UIIconLocked) :
                        T.SquadsUIPlayerCountList.Translate(player, false, sq.Members.Count, MenuUI.MemberParents.Length));
                s2++;
            }
        }
        for (; s2 < ListUI.Squads.Length; s2++)
        {
            if (s2 == 0)
                ListUI.Header.SetVisibility(c, false);
            ListUI.Squads[s2].SetVisibility(c, false);
        }
    }
    public static void ReplicateLockSquad(Squad squad)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int index = 0;
        for (int i = 0; i < Squads.Count; i++)
        {
            if (Squads[i].Team != squad.Team) continue;
            if (Squads[i] == squad) break;
            index++;
        }
        for (int i = 0; i < squad.Members.Count; i++)
        {
            MenuUI.Lock.SetVisibility(squad.Members[i].Connection, squad.IsLocked);
        }
        if (index < ListUI.Squads.Length)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer player = PlayerManager.OnlinePlayers[i];
                if (squad.Team != player.GetTeam() || squad == player.Squad) continue;
                if (player.Squad is null)
                {
                    ListUI.SquadMemberCounts[index].SetText(player.Connection,
                        squad.IsLocked ?
                            T.SquadsUIPlayerCountListLocked.Translate(player, false, squad.Members.Count, MenuUI.MemberParents.Length, Gamemode.Config.UIIconLocked) :
                            T.SquadsUIPlayerCountList.Translate(player, false, squad.Members.Count, MenuUI.MemberParents.Length));
                }
            }
        }
        for (int i = 0; i < Squads.Count; ++i)
        {
            Squad s = Squads[i];
            if (s == squad) continue;
            index = 0;
            for (int s2 = 0; s2 < Squads.Count; ++s2)
            {
                Squad s3 = Squads[s2];
                if (s.Team != s3.Team || s3 == s) continue;
                ++index;
                if (squad == s) break;
            }
            if (index >= MenuUI.OtherSquadParents.Length) continue;
            for (int m = 0; m < s.Members.Count; ++m)
            {
                UCPlayer pl = s.Members[m];
                MenuUI.OtherSquadTexts[index].SetText(pl.Connection,
                    (squad.IsLocked ? T.SquadsUIPlayerCountSmallLocked : T.SquadsUIPlayerCountSmall)
                        .Translate(pl, false, squad.Members.Count, MenuUI.MemberParents.Length));
            }
        }
    }
    public static void ReplicateKitChange(UCPlayer player)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Squad? squad = player.Squad;
        if (squad is null) return;
        int plInd = -1;
        for (int i = 0; i < squad.Members.Count; i++)
        {
            if (squad.Members[i].Steam64 == player.Steam64)
            {
                plInd = i;
                break;
            }
        }
        if (plInd > -1 && plInd < MenuUI.MemberIcons.Length)
        {
            string newIcon = new string(player.Icon, 1);
            for (int i = 0; i < squad.Members.Count; ++i)
            {
                MenuUI.MemberIcons[plInd].SetText(squad.Members[i].Player.channel.owner.transportConnection, newIcon);
            }
        }
    }
    public static void UpdateMemberList(Squad squad)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int m = 0; m < squad.Members.Count; m++)
        {
            UCPlayer player = squad.Members[m];
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            MenuUI.Header.SetText(c, T.SquadsUIHeaderPlayerCount.Translate(player, false, squad, squad.Members.Count, MenuUI.MemberParents.Length));
            int i = 0;
            int num = Math.Min(squad.Members.Count, MenuUI.MemberParents.Length);
            for (; i < num; i++)
            {
                MenuUI.MemberParents[i].SetVisibility(c, true);
                UCPlayer member = squad.Members[i];
                MenuUI.MemberNames[i].SetText(c, member.Name.NickName);
                MenuUI.MemberIcons[i].SetText(c, new string(member.Icon, 1));
            }
            for (; i < MenuUI.MemberParents.Length; i++)
            {
                MenuUI.MemberParents[i].SetVisibility(c, false);
            }
        }
    }
    public static string FindUnusedSquadName(ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int n = 0; n < SquadNames.Length; n++)
        {
            string name = SquadNames[n];
            for (int i = 0; i < Squads.Count; i++)
            {
                if (Squads[i].Team == team)
                {
                    if (name.Equals(Squads[i].Name, StringComparison.Ordinal))
                        goto next;
                }
            }
            return name;
            next:;
        }
        return SquadNames[SquadNames.Length - 1];
    }
    public static Squad CreateSquad(UCPlayer leader, ulong team)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string name = FindUnusedSquadName(team);
        Squad squad = new Squad(name, leader, team, leader.KitBranch);
        Squads.Add(squad);
        SortSquadNames();
        Squad? oldSquad = leader.Squad;
        leader.Squad = squad;
        Traits.TraitManager.OnPlayerJoinSquad(leader, squad);

        ClearList(leader.Player);
        SendSquadMenu(leader, squad);

        UpdateUIMemberCount(team);

        ActionLog.Add(ActionLogType.CreatedSquad, squad.Name + " on team " + Teams.TeamManager.TranslateName(team), leader);

        SquadStatusUpdated?.Invoke(leader, oldSquad, squad, false, true);
        return squad;
    }
    private static void SortSquadNames()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Squads.Sort((a, b) => a.Name[0].CompareTo(b.Name[0]));
    }
    public static void JoinSquad(UCPlayer player, Squad squad)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (UCPlayer p in squad.Members)
        {
            if (p.Steam64 != player.Steam64)
                p.SendChat(T.SquadPlayerJoined, player);
            else
                p.SendChat(T.SquadJoined, squad);
        }

        Traits.TraitManager.OnPlayerJoinSquad(player, squad);
        squad.Members.Add(player);
        SortMembers(squad);
        Squad? oldSquad = player.Squad;
        player.Squad = squad;

        ClearList(player.Player);
        SendSquadMenu(player, squad, holdMemberCountUpdate: true);

        SendSquadListToTeam(squad.Team);
        UpdateMemberList(squad);
        UpdateUIMemberCount(squad.Team);

        ActionLog.Add(ActionLogType.JoinedSquad, squad.Name + " on team " + Teams.TeamManager.TranslateName(squad.Team) + " owned by " + squad.Leader.Steam64.ToString(Data.AdminLocale), player);

        if (squad.HasRally)
            squad.RallyPoint!.ShowUIForSquad();

        PlayerManager.ApplyTo(player);
        SquadStatusUpdated?.Invoke(player, oldSquad, squad, false, false);
    }
    private static void SortMembers(Squad squad)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        squad.Members.Sort((a, b) => b.CachedXP.CompareTo(a.CachedXP));
        if (squad.Leader is { IsOnline: true, IsLeaving: false })
        {
            squad.Members.RemoveAll(x => x.Steam64 == squad.Leader.Steam64);
            squad.Members.Insert(0, squad.Leader);
        }
    }
    public static void LeaveSquad(UCPlayer player, Squad squad)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.SendChat(T.SquadLeft, squad);

        bool wasLeader = squad.Leader.Steam64 == player.Steam64;
        bool willNeedNewLeader = squad.Leader == null || wasLeader;
        player.Squad = null;
        ClearMenu(player.Player);
        squad.Members.RemoveAll(p => p.Steam64 == player.Steam64);
        Traits.TraitManager.OnPlayerLeftSquad(player, squad);
        if (squad.Members.Count == 0)
        {
            Squads.Remove(squad);
            squad.Disbanded = true;
            if (squad.Leader is { IsOnline: true, IsLeaving: false })
            {
                squad.Leader.SendChat(T.SquadDisbanded, squad);
                if (squad.Leader.KitClass == Class.Squadleader)
                {
                    KitManager? manager = KitManager.GetSingletonQuick();
                    if (manager != null)
                        UCWarfare.RunTask(manager.TryGiveUnarmedKit, squad.Leader, true, ctx: "Unequipping squadleader kit after leaving squad.");
                }
                PlayerManager.ApplyTo(squad.Leader);
            }

            UpdateUIMemberCount(squad.Team);

            ActionLog.Add(ActionLogType.DisbandedSquad, squad.Name + " on team " + Teams.TeamManager.TranslateName(squad.Team), player);

            if (squad.HasRally)
            {
                squad.RallyPoint!.Destroy();
            }

            if (player is { IsOnline: true, IsLeaving: false })
                SendSquadList(player);

            SquadStatusUpdated?.Invoke(player, squad, null, wasLeader, false);

            return;
        }

        ActionLog.Add(ActionLogType.JoinedSquad, squad.Name + " on team " + Teams.TeamManager.TranslateName(squad.Team) + " owned by " + (squad.Leader == null ? "0" : squad.Leader.Steam64.ToString(Data.AdminLocale)), player);

        if (willNeedNewLeader)
        {
            squad.Leader = null!; // need to set leader to null before sorting, otherwise old leader will get added back
        }
        SortMembers(squad);
        if (willNeedNewLeader)
        {
            squad.Leader = squad.Members[0]; // goes to the best officer, then the best xp
            squad.Members.RemoveAll(p => p.Steam64 == player.Steam64);
            squad.Leader.SendChat(T.SquadPromoted, squad);
        }
        for (int i = 0; i < squad.Members.Count; i++)
        {
            UCPlayer p = squad.Members[i];
            if (p.Steam64 != player.Steam64 && p is { IsOnline: true, IsLeaving: false })
                p.SendChat(T.SquadPlayerLeft, player);
        }
        UpdateMemberList(squad);
        UpdateUIMemberCount(squad.Team);

        if (player is { IsOnline: true, IsLeaving: false })
        {
            if (squad.HasRally)
                squad.RallyPoint!.ClearUIForPlayer(player);

            SendSquadList(player);
        }

        PlayerManager.ApplyTo(player);

        SquadStatusUpdated?.Invoke(player, squad, null, wasLeader, false);

        if (willNeedNewLeader)
            SquadStatusUpdated?.Invoke(squad.Leader!, squad, squad, false, true);
    }
    public static void DisbandSquad(Squad squad)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Squads.Remove(squad);
        squad.Disbanded = true;

        ActionLog.Add(ActionLogType.DisbandedSquad, squad.Name + " on team " + Teams.TeamManager.TranslateName(squad.Team), squad.Leader);

        Traits.TraitManager.OnSquadDisbanded(squad);

        for (int i = 0; i < squad.Members.Count; i++)
        {
            UCPlayer member = squad.Members[i];
            member.Squad = null;

            member.SendChat(T.SquadDisbanded, squad);
            ClearMenu(member.Player);
            PlayerManager.ApplyTo(member);
        }
        SendSquadListToTeam(squad.Team);
        UpdateUIMemberCount(squad.Team);

        if (squad.HasRally)
            squad.RallyPoint!.Destroy();

        for (int i = 0; i < squad.Members.Count; ++i)
            SquadStatusUpdated?.Invoke(squad.Members[i], squad, null, squad.Leader.Steam64 == squad.Members[i].Steam64, false);
    }
    public static void KickPlayerFromSquad(UCPlayer player, Squad squad)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == null || squad == null || squad.Members.Count < 2)
            return;

        if (squad.Members.Remove(player))
            player.SendChat(T.SquadKicked, squad);

        Traits.TraitManager.OnPlayerLeftSquad(player, squad);

        SortMembers(squad);
        for (int i = 0; i < squad.Members.Count; i++)
        {
            UCPlayer p = squad.Members[i];
            if (p.Steam64 != player.Steam64)
                p.SendChat(T.SquadPlayerKicked, player);
        }
        UpdateMemberList(squad);
        player.Squad = null;
        ClearMenu(player.Player);
        SendSquadListToTeam(squad.Team);
        UpdateUIMemberCount(squad.Team);

        if (squad.HasRally)
            squad.RallyPoint!.ClearUIForPlayer(player);

        PlayerManager.ApplyTo(player);
        SquadStatusUpdated?.Invoke(player, squad, null, false, false);
    }
    public static void PromoteToLeader(Squad squad, UCPlayer newLeader)
    {
        _singleton.AssertLoaded();
        if (newLeader == squad.Leader)
            return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (squad.Leader.KitClass == Class.Squadleader)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
                UCWarfare.RunTask(manager.TryGiveUnarmedKit, squad.Leader, true, ctx: "Unequipping squadleader kit after someone else was promoted to leader.");
        }

        Traits.TraitManager.OnPlayerPromotedSquadleader(newLeader, squad);

        UCPlayer oldLeader = squad.Leader;
        squad.Leader = newLeader;

        for (int i = 0; i < squad.Members.Count; i++)
        {
            UCPlayer p = squad.Members[i];
            if (p.CSteamID != squad.Leader.CSteamID)
                p.SendChat(T.SquadPlayerPromoted, newLeader);
            else
                p.SendChat(T.SquadPromoted, squad);
        }

        SortMembers(squad);
        UpdateMemberList(squad);
        SquadStatusUpdated?.Invoke(oldLeader, squad, squad, true, false);
        SquadStatusUpdated?.Invoke(newLeader, squad, squad, false, true);
    }
    public static bool FindSquad(string input, ulong teamID, out Squad squad)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<Squad> friendlySquads = Squads.Where(s => s.Team == teamID).ToList();
        string name = input.ToLower();
        if (name.Length == 1)
        {
            char let = char.ToLower(name[0]);
            if (let >= 'a' && let <= 'h')
            {
                name = let switch
                {
                    'a' => SquadNames[0],
                    'b' => SquadNames[1],
                    'c' => SquadNames[2],
                    'd' => SquadNames[3],
                    'e' => SquadNames[4],
                    'f' => SquadNames[5],
                    'g' => SquadNames[6],
                    'h' => SquadNames[7],
                    _ => name
                };
            }
        }
        squad = friendlySquads.Find(s => s.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1);
        return squad != null;
    }
    public static void SetLocked(Squad squad, bool value)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ActionLog.Add(value ? ActionLogType.LockedSquad : ActionLogType.UnlockedSquad, squad.Name + " on team " + Teams.TeamManager.TranslateName(squad.Team), squad.Leader);
        squad.LockOrUnlock(value);
        ReplicateLockSquad(squad);
    }
    public static bool MaxSquadsReached(ulong team)
    {
        int squadsCount = Squads.Count(x => x.Team == team);

        return squadsCount >= ListUI.Squads.Length;
    }
    public static bool AreSquadLimited(ulong team, out int requiredTeammatesForMoreSquads)
    {
        int squadsCount = Squads.Count(x => x.Team == team);

        float friendlyCount = PlayerManager.OnlinePlayers.Count(p => p.GetTeam() == team);
        int maxSquads = Mathf.CeilToInt((friendlyCount + 3) / SQUAD_MAX_MEMBERS);

        requiredTeammatesForMoreSquads = SQUAD_MAX_MEMBERS * maxSquads - 3 + 1;

        return squadsCount >= maxSquads;
    }
    void IUIListener.HideUI(UCPlayer player)
    {
        ClearList(player.Player);
        ClearMenu(player.Player);
    }
    void IUIListener.ShowUI(UCPlayer player) => ((IUIListener)this).UpdateUI(player);
    void IUIListener.UpdateUI(UCPlayer player)
    {
        if (player.Squad == null)
            SendSquadList(player);
        else
        {
            SendSquadMenu(player, player.Squad);
            UpdateMemberList(player.Squad);
            if (player.Squad.HasRally)
                player.Squad.RallyPoint!.ShowUIForPlayer(player);
        }
    }

    private float _timeSinceSquadCheck;
    public void Tick()
    {
        if (Time.time - _timeSinceSquadCheck > 30)
        {
            foreach (Squad squad in Squads)
            {
                bool wrongKit = !(squad.Leader.KitClass == Class.Squadleader ||
                    squad.Leader.KitClass == Class.Crewman ||
                    squad.Leader.KitClass == Class.Pilot);

                bool noSquadMates = squad.Members.Count == 1 && PlayerManager.OnlinePlayers.Count(x => x.GetTeam() == squad.Team) > 3;

                if (wrongKit || noSquadMates)
                {
                    squad.DisbandStrikes++;
                }
                else
                    squad.DisbandStrikes = 0;

                if (squad.DisbandStrikes == 3)
                {
                    DisbandSquad(squad);
                    break;
                }

                if (wrongKit)
                {
                    ToastMessage toast = new ToastMessage(ToastMessageStyle.Tip, T.SquadWarningWrongKit.Translate(squad.Leader));
                    ToastMessage.QueueMessage(squad.Leader, in toast);
                }
                else if (noSquadMates)
                {
                    ToastMessage toast = new ToastMessage(ToastMessageStyle.Tip, T.SquadWarningNoMembers.Translate(squad.Leader));
                    ToastMessage.QueueMessage(squad.Leader, in toast);
                }
            }

            _timeSinceSquadCheck = Time.time;
        }
    }
}

public class Squad : IEnumerable<UCPlayer>, ITranslationArgument
{
    public string Name;
    public ulong Team;
    public Branch Branch;
    public bool IsLocked;
    public UCPlayer Leader;
    public List<UCPlayer> Members;
    public bool Disbanded;
    public RallyPoint? RallyPoint;
    public int DisbandStrikes;
    /// <summary>
    /// Increments every time the squad is locked or unlocked to differentiate between locks if a player leaves.
    /// </summary>
    public byte LockedId;
    public bool HasRally => RallyPoint != null;
    /// <summary><see langword="true"/> if this <see cref="Squad"/>'s <seealso cref="Leader"/> is a commander.</summary>
    public bool IsCommandingSquad
    {
        get
        {
            if (Leader is not null && Leader.IsOnline && SquadManager.Loaded)
            {
                UCPlayer? cmd = SquadManager.Singleton.Commanders.GetCommander(Team);
                if (cmd is not null && cmd.Steam64 == Leader.Steam64)
                    return true;
            }
            return false;
        }
    }

    public Squad(string name, UCPlayer leader, ulong team, Branch branch)
    {
        Name = name;
        Team = team;
        Branch = branch;
        Leader = leader;
        IsLocked = false;
        LockedId = 1;
        Members = new List<UCPlayer>(6) { leader };
        DisbandStrikes = 0;
    }
    public void Lock() => LockOrUnlock(true);
    public void Unlock() => LockOrUnlock(false);
    public IEnumerator<UCPlayer> GetEnumerator() => Members.GetEnumerator();
    public bool IsFull() => Members.Count >= SquadManager.SQUAD_MAX_MEMBERS;
    public bool IsNotSolo() => Members.Count > 1;
    public bool ContainsMember(IPlayer player)
    {
        for (int i = 0; i < Members.Count; ++i)
        {
            if (Members[i].Steam64 == player.Steam64)
                return true;
        }

        return false;
    }
    public void LockOrUnlock(bool isLocked)
    {
        if (isLocked == IsLocked)
            return;

        if (LockedId == byte.MaxValue)
            LockedId = 1;
        else
            ++LockedId;
        IsLocked = isLocked;
    }
    IEnumerator IEnumerable.GetEnumerator() => Members.GetEnumerator();
    public IEnumerator<ITransportConnection> EnumerateMembers()
    {
        IEnumerator<UCPlayer> players = Members.GetEnumerator();
        while (players.MoveNext())
            yield return players.Current!.Player.channel.owner.transportConnection;
        players.Dispose();
    }
    [FormatDisplay("Colored Squad Name")]
    public const string FormatColorName = "c";
    [FormatDisplay("Squad Name")]
    public const string FormatName = "n";

    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags) =>
        FormatColorName.Equals(format, StringComparison.Ordinal)
            ? Localization.Colorize(Teams.TeamManager.GetTeamHexColor(Team), Name, flags)
            : Name;
}

public delegate void SquadUpdated(UCPlayer player, Squad? oldSquad, Squad? newSquad, bool oldIsLeader, bool newIsLeader);