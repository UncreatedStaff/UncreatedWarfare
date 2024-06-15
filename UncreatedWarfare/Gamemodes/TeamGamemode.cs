using JetBrains.Annotations;
using SDG.Unturned;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes;

/// <summary>Gamemode with 2 teams</summary>
public abstract class TeamGamemode : Gamemode, ITeams
{
    protected TeamSelector _teamSelector;
    private bool _shouldHaveBlockerT1;
    private bool _shouldHaveBlockerT2;
    private readonly List<ulong> _mainCampers = new List<ulong>(24);
    public TeamSelector TeamSelector { get => _teamSelector; }
    public virtual bool UseTeamSelector { get => true; }
    public virtual bool EnableAMC { get => true; }
    protected TeamGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
    {

    }
    [UsedImplicitly]
    private void FixedUpdate()
    {
        if (State != State.Staging)
            return;

        TeamManager.EvaluateBases();
    }
    public override void Subscribe()
    {
        TeamManager.OnPlayerLeftMainBase += OnLeftMainBase;
        base.Subscribe();
    }
    public override Task UnloadAsync(CancellationToken token)
    {
        TeamManager.OnPlayerLeftMainBase -= OnLeftMainBase;
        return base.UnloadAsync(token);
    }
    public virtual bool CanLeaveMainInStaging(ulong team) => team == 1ul && !_shouldHaveBlockerT1 || team == 2ul && !_shouldHaveBlockerT2;
    private void OnLeftMainBase(UCPlayer player, ulong team)
    {
        if (player.OnDuty())
            return;

        if (State != State.Staging || CanLeaveMainInStaging(team))
            return;

        TeamManager.RubberbandPlayer(player, team);
        TeamManager.EvaluateBases();
    }
    protected override Task PreInit(CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        if (UseTeamSelector)
            AddSingletonRequirement(ref _teamSelector);
        return TeamManager.ReloadFactions(token);
    }
    protected override Task PreDispose(CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        if (HasOnReadyRan)
            DestroyBlockers();

        return Task.CompletedTask;
    }
    protected override async Task PostInit(CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        if (UseTeamSelector)
        {
            L.LogDebug("Joining players into menu...");
            TeamSelector.JoinTeamBehavior behaviour;
            if (TeamSelector.ShuffleTeamsNextGame)
            {
                L.LogDebug("Teams are to be SHUFFLED.");
                behaviour = TeamSelector.JoinTeamBehavior.Shuffle;
            }
            else
                behaviour = TeamSelector.JoinTeamBehavior.KeepTeam;

            TeamSelector.ShuffleTeamsNextGame = false;

            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                if (PlayerManager.OnlinePlayers[i].TeamSelectorData is not { IsSelecting: true })
                    TeamSelector.JoinSelectionMenu(PlayerManager.OnlinePlayers[i], behaviour);
            }

            TeamSelector.ShuffleTeamsNextGame = false;
        }
        Task task = base.PostInit(token);
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
        }
    }
    protected override Task PreGameStarting(bool isOnLoad, CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        if (UseTeamSelector)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                _teamSelector.JoinSelectionMenu(PlayerManager.OnlinePlayers[i]);
        }

        return base.PreGameStarting(isOnLoad, token);
    }
    protected override void EventLoopAction()
    {
        if (EveryXSeconds(Config.GeneralMainCheckSeconds))
        {
            if (State == State.Staging)
            {
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer player = PlayerManager.OnlinePlayers[i];
                    ulong team = player.GetTeam();
                    if (CanLeaveMainInStaging(team) || TeamManager.InMainCached(player) || player.OnDuty())
                        continue;

                    TeamManager.RubberbandPlayer(player, team);
                }
            }
            else
                TeamManager.EvaluateBases();
        }

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.Player.life.oxygen < 100 && player.GetTeam() == 0 && player.TeamSelectorData is { IsSelecting: true, IsOptionsOnly: false })
            {
                player.Player.life.serverModifyHealth(100);
                player.Player.life.simulatedModifyOxygen(100);
            }
        }
        base.EventLoopAction();
    }
    protected override Task OnReady(CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        TeamManager.CheckGroups();
        return base.OnReady(token);
    }
    protected void CheckMainCampZones()
    {
        if (!Config.GeneralAMCKillTime.HasValue || Config.GeneralAMCKillTime.Value < 0)
            return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            ulong team = player.GetTeam();
            if (!player.IsOnline || team is not 1 and not 2 || player.OnDuty() || player.Player.life.isDead)
                goto notInMain;
            Vector3 pos = player.Position;
            if (team == 1 && !TeamManager.Team2AMC.IsInside(pos) || team == 2 && !TeamManager.Team1AMC.IsInside(pos))
                goto notInMain;
            if (!_mainCampers.Contains(player.Steam64))
            {
                _mainCampers.Add(player.Steam64);
                OnPlayerMainCamping(player);
            }
            continue;
        notInMain:
            _mainCampers.Remove(player.Steam64);
        }
    }
    private void OnPlayerMainCamping(UCPlayer player)
    {
        ToastMessage.QueueMessage(player, new ToastMessage(ToastMessageStyle.FlashingWarning,
            T.EnteredEnemyTerritory.Translate(player, false, Localization.GetTimeFromSeconds(Mathf.RoundToInt(Config.GeneralAMCKillTime.Value), player))) { OverrideDuration = Config.GeneralAMCKillTime.Value } );
        player.Player.StartCoroutine(PlayerMainCampingCoroutine(player));
    }
    private IEnumerator PlayerMainCampingCoroutine(UCPlayer player)
    {
        ulong team = player.GetTeam();
        if (Config.GeneralAMCKillTime.Value != 0)
            yield return new WaitForSecondsRealtime(Config.GeneralAMCKillTime.Value);
        if (player.Player == null || !_mainCampers.Contains(player.Steam64) || player.Player.life.isDead || player.OnDuty())
            yield break;
        player.Player.movement.forceRemoveFromVehicle();
        yield return null;
        player.Player.life.askDamage(byte.MaxValue, Vector3.up / 8f, DeathTracker.InEnemyMainDeathCause, ELimb.SPINE, Provider.server, out _, false, ERagdollEffect.NONE, false, true);
        ActionLog.Add(ActionLogType.MainCampAttempt, $"Player team: {TeamManager.TranslateName(team, false)}, " +
                                                           $"Team: {TeamManager.TranslateName(TeamManager.Other(team), false)}, " +
                                                           $"Location: {player.Position.ToString("0.#", Data.AdminLocale)}", player);
    }
    public void SpawnBlockers()
    {
        SpawnBlockerOnT1();
        SpawnBlockerOnT2();
    }
    public void SpawnBlockerOnT1()
    {
        _shouldHaveBlockerT1 = true;
    }
    public void SpawnBlockerOnT2()
    {
        _shouldHaveBlockerT2 = true;
    }
    public void DestoryBlockerOnT1()
    {
        _shouldHaveBlockerT1 = false;
    }
    public void DestoryBlockerOnT2()
    {
        _shouldHaveBlockerT2 = false;
    }
    public void DestroyBlockers()
    {
        _shouldHaveBlockerT1 = false;
        _shouldHaveBlockerT2 = false;
    }
    public override void OnPlayerDeath(PlayerDied e)
    {
        base.OnPlayerDeath(e);
        _mainCampers.Remove(e.Player.Steam64);
    }
    protected override Task PlayerInit(UCPlayer player, bool wasAlreadyOnline, CancellationToken token)
    {
        // token.CombineIfNeeded(UnloadToken, player.DisconnectToken);
        if (!UseTeamSelector)
        {
            ulong team = player.Save.Team;
            if (this is not IEndScreen { IsScreenUp: true } && !TeamManager.IsFriendly(player.Player, team))
            {
                TeamManager.JoinTeam(player, player.Save.Team, player.Save.LastGame != GameId || player.Save.ShouldRespawnOnJoin, true);
            }
            OnJoinTeam(player, player.Save.Team);
        }
        else if (!TeamSelector.IsSelecting(player))
            InitUI(player);
        return base.PlayerInit(player, wasAlreadyOnline, token);
    }
    protected override void ReloadUI(UCPlayer player) => InitUI(player);
    protected abstract void InitUI(UCPlayer player);
    public virtual void OnJoinTeam(UCPlayer player, ulong team)
    {
        if (team is 1 or 2 && State == State.Staging)
            ShowStagingUI(player);
        if (this is IGameStats gs)
            gs.GameStats.OnPlayerJoin(player);
        if (this is IEndScreen { IsScreenUp: true })
        {
            if (this is IImplementsLeaderboard<BasePlayerStats, BaseStatTracker<BasePlayerStats>> impl && impl.Leaderboard != null)
                impl.Leaderboard.OnPlayerJoined(player);
        }
        else
        {
            if (this is ITickets tickets)
                tickets.TicketManager.ShowUI(player);
            if (!UCWarfare.Config.DisableDailyQuests)
                DailyQuests.TrackDailyQuest(player);
            InitUI(player);
        }

        CancellationToken token = player.DisconnectToken;
        CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        UCWarfare.RunTask(async tokens =>
        {
            try
            {
                await UCWarfare.ToUpdate(tokens.Token);
                for (int i = 0; i < Singletons.Count; ++i)
                {
                    IUncreatedSingleton singleton = Singletons[i];
                    if (singleton is IJoinedTeamListener l1)
                        l1.OnJoinTeam(player, team);
                    if (singleton is IJoinedTeamListenerAsync l2)
                    {
                        Task task = l2.OnJoinTeamAsync(player, team, tokens.Token);
                        if (!task.IsCompleted)
                        {
                            await task.ConfigureAwait(false);
                            await UCWarfare.ToUpdate(tokens.Token);
                        }
                    }
                }
            }
            finally
            {
                tokens.Dispose();
            }
        }, tokens, "Joining team: " + player.Steam64 + ".");
    }
    public override void PlayerLeave(UCPlayer player)
    {
        _mainCampers.Remove(player.Steam64);
        base.PlayerLeave(player);
    }
}
