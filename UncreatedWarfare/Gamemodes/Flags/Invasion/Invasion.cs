using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Linq;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion;

public class Invasion :
    CTFBaseMode<InvasionLeaderboard, BaseCTFStats, InvasionTracker, InvasionTicketProvider>,
    IAttackDefense
{
    protected ulong _attackTeam;
    protected ulong _defenseTeam;
    protected SpecialFOB? _vcp;
    public override string DisplayName => "Invasion";
    public override EGamemode GamemodeType => EGamemode.INVASION;
    public ulong AttackingTeam => _attackTeam;
    public ulong DefendingTeam => _defenseTeam;
    public SpecialFOB? FirstPointFOB => _vcp;
    public Invasion() : base(nameof(Invasion), Config.AASEvaluateTime) { }
    protected override void PostDispose()
    {
        foreach (SteamPlayer player in Provider.clients)
        {
            CTFUI.ClearFlagList(player.transportConnection);
        }
        CTFUI.CaptureUI.ClearFromAllPlayers();
        base.PostDispose();
    }
    protected override void PreGameStarting(bool isOnLoad)
    {
        PickTeams();
        base.PreGameStarting(isOnLoad);
    }
    protected override void PostGameStarting(bool isOnLoad)
    {
        base.PostGameStarting(isOnLoad);
        Flag? firstFlag = null;
        if (DefendingTeam == 1)
            firstFlag = Rotation.LastOrDefault();
        else if (DefendingTeam == 2)
            firstFlag = Rotation.FirstOrDefault();

        if (_attackTeam == 1)
            SpawnBlockerOnT1();
        else
            SpawnBlockerOnT2();
        if (firstFlag != null)
            _vcp = FOBManager.RegisterNewSpecialFOB(Config.InvasionSpecialFOBName, new Vector3(firstFlag.ZoneData.Center.x, F.GetHeight(firstFlag.ZoneData.Center, firstFlag.ZoneData.MinHeight) + 2f, firstFlag.ZoneData.Center.y), _defenseTeam, UCWarfare.GetColorHex("invasion_special_fob"), true);
        StartStagingPhase(Config.InvasionStagingTime);
    }
    protected void PickTeams()
    {
        _attackTeam = (ulong)UnityEngine.Random.Range(1, 3);
        if (_attackTeam == 1)
            _defenseTeam = 2;
        else if (_attackTeam == 2)
            _defenseTeam = 1;
        L.Log("Attack: " + TeamManager.TranslateName(_attackTeam, 0) + ", Defense: " + TeamManager.TranslateName(_defenseTeam, 0), ConsoleColor.Green);
    }
    public override void LoadRotation()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_allFlags == null || _allFlags.Count == 0) return;
        LoadFlagsIntoRotation();
        if (_rotation.Count < 1)
        {
            L.LogError("No flags were put into rotation!!");
        }
        if (_attackTeam == 1)
        {
            _objectiveT1Index = 0;
            _objectiveT2Index = -1;
        }
        else
        {
            _objectiveT1Index = -1;
            _objectiveT2Index = _rotation.Count - 1;
        }
        if (Config.InvasionDiscoveryForesight < 1)
        {
            L.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
        }
        else
        {
            for (int i = 0; i < _rotation.Count; i++)
            {
                _rotation[i].Discover(_defenseTeam);
            }
            if (_attackTeam == 1)
            {
                for (int i = 0; i < Config.InvasionDiscoveryForesight; i++)
                {
                    if (i >= _rotation.Count || i < 0) break;
                    _rotation[i].Discover(1);
                }
            }
            else if (_attackTeam == 2)
            {
                for (int i = _rotation.Count - 1; i > _rotation.Count - 1 - Config.InvasionDiscoveryForesight; i--)
                {
                    if (i >= _rotation.Count || i < 0) break;
                    _rotation[i].Discover(2);
                }
            }
        }
        for (int i = 0; i < _rotation.Count; i++)
        {
            InitFlag(_rotation[i]); //subscribe to abstract events.
        }
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            InvasionUI.SendFlagList(pl);
        }
        PrintFlagRotation();
        EvaluatePoints();
    }
    public override void InitFlag(Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        base.InitFlag(flag);
        flag.EvaluatePointsOverride = FlagCheck;
        flag.IsContestedOverride = ContestedCheck;
        flag.SetOwner(_defenseTeam, false);
        flag.SetPoints(_attackTeam == 2 ? Flag.MAX_POINTS : -Flag.MAX_POINTS, true, true);
    }
    private void FlagCheck(Flag flag, bool overrideInactiveCheck = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (State == EState.ACTIVE || overrideInactiveCheck)
        {
            if (flag.ID == (AttackingTeam == 1ul ? ObjectiveTeam1!.ID : ObjectiveTeam2!.ID))
            {
                //bool atkOnFlag = (AttackingTeam == 1ul && flag.Team1TotalCappers > 0) || (AttackingTeam == 2ul && flag.Team2TotalCappers > 0);
                if (!flag.IsContested(out ulong winner))
                {
                    if (winner == AttackingTeam || AttackingTeam != flag.Owner)
                    {
                        flag.Cap(winner, flag.GetCaptureAmount(Config.InvasionCaptureScale, winner));
                    }
                    else
                    {
                        // invoke points updated method to show secured.
                        flag.SetPoints(flag.Points);
                    }
                }
                else
                {
                    // invoke points updated method to show contested.
                    flag.SetPoints(flag.Points);
                }
            }
        }
    }
    private bool ContestedCheck(Flag flag, out ulong winner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (flag.IsObj(_attackTeam))
        {
            if (flag.Team1TotalCappers == 0 && flag.Team2TotalCappers == 0)
            {
                winner = 0;
                return false;
            }
            else if (flag.Team1TotalCappers == flag.Team2TotalCappers)
            {
                winner = Intimidation.CheckSquadsForContestBoost(flag);
            }
            else if (flag.Team1TotalCappers == 0 && flag.Team2TotalCappers > 0)
            {
                winner = 2;
            }
            else if (flag.Team2TotalCappers == 0 && flag.Team1TotalCappers > 0)
            {
                winner = 1;
            }
            else if (flag.Team1TotalCappers > flag.Team2TotalCappers)
            {
                if (flag.Team1TotalCappers - Config.AASRequiredCapturingPlayerDifference >= flag.Team2TotalCappers)
                {
                    winner = 1;
                }
                else
                {
                    winner = Intimidation.CheckSquadsForContestBoost(flag);
                }
            }
            else
            {
                if (flag.Team2TotalCappers - Config.AASRequiredCapturingPlayerDifference >= flag.Team1TotalCappers)
                {
                    winner = 2;
                }
                else
                {
                    winner = Intimidation.CheckSquadsForContestBoost(flag);
                }
            }
            return winner == 0;
        }
        else
        {
            if (flag.ObjectivePlayerCountCappers == 0) winner = 0;
            else winner = flag.WhosObj();
            if (!flag.IsObj(winner)) winner = 0;
            return false;
        }
    }
    protected override void PlayerEnteredFlagRadius(Flag flag, Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        player.SendChat(T.EnteredCaptureRadius, flag);
        UpdateFlag(flag);
    }
    private void UpdateFlag(Flag flag)
    {
        CaptureUIParameters t1 = default;
        CaptureUIParameters t2 = default;
        CaptureUIParameters t1v = default;
        CaptureUIParameters t2v = default;
        if (flag.Team1TotalCappers > 0)
            t1 = InvasionUI.RefreshStaticUI(1, flag, false, _attackTeam);
        if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
            t1v = InvasionUI.RefreshStaticUI(1, flag, true, _attackTeam);
        if (flag.Team2TotalCappers > 0)
            t2 = InvasionUI.RefreshStaticUI(2, flag, false, _attackTeam);
        if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
            t2v = InvasionUI.RefreshStaticUI(2, flag, true, _attackTeam);
        for (int i = 0; i < flag.PlayersOnFlag.Count; i++)
        {
            Player capper = flag.PlayersOnFlag[i];
            ulong t = capper.GetTeam();

            if (t == 1)
            {
                if (capper.movement.getVehicle() == null)
                    CTFUI.CaptureUI.Send(capper, in t1);
                else
                    CTFUI.CaptureUI.Send(capper, in t1v);
            }
            else if (t == 2)
            {
                if (capper.movement.getVehicle() == null)
                    CTFUI.CaptureUI.Send(capper, in t2);
                else
                    CTFUI.CaptureUI.Send(capper, in t2v);
            }
        }
    }
    protected override void PlayerLeftFlagRadius(Flag flag, Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        player.SendChat(T.LeftCaptureRadius, flag);
        CTFUI.ClearCaptureUI(player.channel.owner.transportConnection);
        UpdateFlag(flag);
    }
    protected override void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (NewOwner == 1)
        {
            ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(1, 0) + (_attackTeam == 1 ? " ATTACK" : " DEFENSE"));
            if (_attackTeam == 1 && _objectiveT1Index >= _rotation.Count - 1) // if t1 just capped the last flag
            {
                DeclareWin(1);
                _objectiveT1Index = 0;
                return;
            }
            else if (_attackTeam == 1)
            {
                _objectiveT1Index = flag.index + 1;
                InvokeOnObjectiveChanged(flag, _rotation[ObjectiveT1Index], NewOwner, flag.index, ObjectiveT1Index);
                InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
                for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
                {
                    if (flag.PlayersOnFlagTeam1[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
                        fg.AddCapture();
                }
            }
            else if (DefendingTeam == 1)
            {
                InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
            }
        }
        else if (NewOwner == 2)
        {
            ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(2, 0) + (_attackTeam == 2 ? " ATTACK" : " DEFENSE"));
            if (_attackTeam == 2 && ObjectiveT2Index < 1) // if t2 just capped the last flag
            {
                DeclareWin(2);
                _objectiveT2Index = _rotation.Count - 1;
                return;
            }
            else if (_attackTeam == 2)
            {
                _objectiveT2Index = flag.index - 1;
                InvokeOnObjectiveChanged(flag, _rotation[ObjectiveT2Index], NewOwner, flag.index, ObjectiveT2Index);
                InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
                for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
                {
                    if (flag.PlayersOnFlagTeam2[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
                        fg.AddCapture();
                }
            }
            else if (DefendingTeam == 2)
            {
                InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
            }
        }
        else
        {
            if (OldOwner == DefendingTeam)
            {
                if (OldOwner == 1)
                {
                    //int oldindex = ObjectiveT1Index;
                    //_objectiveT1Index = flag.index;
                    //if (oldindex != flag.index)
                    //{
                    //    //InvokeOnObjectiveChanged(flag, flag, 0, oldindex, flag.index);
                    //    InvokeOnFlagNeutralized(flag, 2, 1);
                    //}
                    InvokeOnFlagNeutralized(flag, 2, 1);
                }
                else if (OldOwner == 2)
                {
                    //int oldindex = ObjectiveT2Index;
                    //_objectiveT2Index = flag.index;
                    //if (oldindex != flag.index)
                    //{
                    //    //InvokeOnObjectiveChanged(_rotation[oldindex], flag, 0, oldindex, flag.index);
                    //    InvokeOnFlagNeutralized(flag, 1, 2);
                    //}
                    InvokeOnFlagNeutralized(flag, 1, 2);
                }
            }
        }
        UpdateFlag(flag);
        if (NewOwner == 0)
        {
            Chat.Broadcast(LanguageSet.OnTeam(1), T.FlagNeutralized, flag);
            Chat.Broadcast(LanguageSet.OnTeam(2), T.FlagNeutralized, flag);
        }
        else
        {
            FactionInfo info = TeamManager.GetFaction(NewOwner);
            Chat.Broadcast(LanguageSet.OnTeam(1), T.TeamCaptured, info, flag);
            Chat.Broadcast(LanguageSet.OnTeam(2), T.TeamCaptured, info, flag);
        }
    }
    protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (NewPoints == 0)
            flag.SetOwner(0);
        UpdateFlag(flag);
    }
    protected override void InvokeOnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        base.InvokeOnFlagCaptured(flag, capturedTeam, lostTeam);
        InvasionUI.ReplicateFlagUpdate(flag, true);
    }
    protected override void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        base.InvokeOnFlagNeutralized(flag, capturedTeam, lostTeam);
        InvasionUI.ReplicateFlagUpdate(flag, true);
    }
    public override void OnGroupChanged(GroupChanged e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CTFUI.ClearFlagList(e.Player);
        if (_onFlag.TryGetValue(e.Player.Steam64, out int id))
        {
            CaptureUIParameters p = InvasionUI.RefreshStaticUI(e.NewTeam, _rotation.FirstOrDefault(x => x.ID == id)
                                                                          ?? _rotation[0], e.Player.Player.movement.getVehicle() != null, AttackingTeam);
            CTFUI.CaptureUI.Send(e.Player, in p);
        }

        InvasionUI.SendFlagList(e.Player);
        base.OnGroupChanged(e);
    }
    protected override void InitUI(UCPlayer player)
    {
        InvasionUI.SendFlagList(player);
    }
    public override void ShowStagingUI(UCPlayer player)
    {
        Flag? obj = null;
        if (AttackingTeam == 1) obj = ObjectiveTeam1;
        else if (AttackingTeam == 2) obj = ObjectiveTeam2;
        if (obj == null) return;
        ITransportConnection c = player.Connection;
        CTFUI.StagingUI.SendToPlayer(c);
        if (player.GetTeam() == AttackingTeam)
            CTFUI.StagingUI.Top.SetText(c, T.PhaseBreifingInvasionAttack.Translate(player));
        else if (player.GetTeam() == DefendingTeam)
            CTFUI.StagingUI.Top.SetText(c, T.PhaseBreifingInvasionDefense.Translate(player, obj));
    }
    protected override void EndStagingPhase()
    {
        base.EndStagingPhase();
        if (_attackTeam == 1)
            DestoryBlockerOnT1();
        else
            DestoryBlockerOnT2();
    }
    public override bool IsAttackSite(ulong team, Flag flag) => flag.IsObj(team);
    public override bool IsDefenseSite(ulong team, Flag flag) => flag.IsObj(_attackTeam) && flag.Owner == _defenseTeam && team == _defenseTeam;
}

public class InvasionLeaderboard : BaseCTFLeaderboard<BaseCTFStats, InvasionTracker>
{

}

public sealed class InvasionTicketProvider : BaseCTFTicketProvider, IFlagCapturedListener
{
    public override int GetTeamBleed(ulong team)
    {
        if (!Data.Is(out IFlagRotation fg) || !Data.Is(out IAttackDefense ad)) return 0;
        if (team == ad.AttackingTeam)
        {
            int defenderFlags = fg.Rotation.Count(f => f.Owner == ad.DefendingTeam);

            if (defenderFlags == fg.Rotation.Count)
                return -1;
        }

        return 0;
    }
    public override void OnGameStarting(bool isOnLoaded)
    {
        if (!Data.Is(out IFlagRotation fg) || !Data.Is(out IAttackDefense ad)) return;
        int attack = Gamemode.Config.InvasionAttackStartingTickets;
        int defense = Gamemode.Config.InvasionAttackStartingTickets + fg.Rotation.Count * Gamemode.Config.InvasionTicketsFlagCaptured;

        if (ad.AttackingTeam == 1)
        {
            Manager.Team1Tickets = attack;
            Manager.Team2Tickets = defense;
        }
        else if (ad.AttackingTeam == 2)
        {
            Manager.Team2Tickets = attack;
            Manager.Team1Tickets = defense;
        }
    }
    public void OnFlagCaptured(Flag flag, ulong newOwner, ulong oldOwner)
    {
        if (newOwner == 1)
            Manager.Team1Tickets += Gamemode.Config.InvasionTicketsFlagCaptured;
        else if (newOwner == 2)
            Manager.Team2Tickets += Gamemode.Config.InvasionTicketsFlagCaptured;

        if (oldOwner == 1)
            Manager.Team1Tickets += Gamemode.Config.AASTicketsFlagLost;
        else if (oldOwner == 2)
            Manager.Team2Tickets += Gamemode.Config.AASTicketsFlagLost;
    }
}