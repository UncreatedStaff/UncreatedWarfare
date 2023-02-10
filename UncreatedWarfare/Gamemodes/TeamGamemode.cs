using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes;

/// <summary>Gamemode with 2 teams</summary>
public abstract class TeamGamemode : Gamemode, ITeams
{
    protected TeamSelector _teamSelector;
    private Transform? _blockerBarricadeT1;
    private Transform? _blockerBarricadeT2;
    private readonly List<ulong> _mainCampers = new List<ulong>(24);
    public TeamSelector TeamSelector { get => _teamSelector; }
    public virtual bool UseTeamSelector { get => true; }
    public virtual bool EnableAMC { get => true; }
    protected TeamGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
    {

    }
    protected override Task PreInit(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        if (UseTeamSelector)
            AddSingletonRequirement(ref _teamSelector);
        return TeamManager.ReloadFactions(token);
    }
    protected override Task PreDispose(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        if (HasOnReadyRan)
            DestroyBlockers();

        return Task.CompletedTask;
    }
    protected override async Task PostInit(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        if (UseTeamSelector)
        {
            L.Log("Joining players into menu...");
            TeamSelector.JoinTeamBehavior behaviour;
            if (TeamSelector.ShuffleTeamsNextGame)
            {
                L.Log("Teams are to be SHUFFLED.");
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
        token.CombineIfNeeded(UnloadToken);
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
            TeamManager.EvaluateBases();
    }
    protected override Task OnReady(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
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
        ToastMessage.QueueMessage(player, new ToastMessage(
            T.EnteredEnemyTerritory.Translate(player, Mathf.RoundToInt(Config.GeneralAMCKillTime.Value).GetTimeFromSeconds(player)),
            ToastMessageSeverity.Warning));
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
        player.Player.life.askDamage(byte.MaxValue, Vector3.up / 8f, DeathTracker.MAIN_DEATH, ELimb.SPINE, Provider.server, out _, false, ERagdollEffect.NONE, false, true);
        ActionLog.Add(ActionLogType.MainCampAttempt, $"Player team: {TeamManager.TranslateName(team, 0, false)}, " +
                                                           $"Team: {TeamManager.TranslateName(TeamManager.Other(team), 0, false)}, " +
                                                           $"Location: {player.Position.ToString("0.#", Data.AdminLocale)}", player);
    }
    public void SpawnBlockers()
    {
        SpawnBlockerOnT1();
        SpawnBlockerOnT2();
    }
    public void SpawnBlockerOnT1()
    {
        if (Config.BarricadeZoneBlockerTeam1.ValidReference(out ItemBarricadeAsset asset))
            _blockerBarricadeT1 = BarricadeManager.dropNonPlantedBarricade(new Barricade(asset),
                TeamManager.Team1Main.Center3D + Vector3.up, Quaternion.Euler(BlockerSpawnRotation), 0, 0);
    }
    public void SpawnBlockerOnT2()
    {
        if (Config.BarricadeZoneBlockerTeam2.ValidReference(out ItemBarricadeAsset asset))
            _blockerBarricadeT2 = BarricadeManager.dropNonPlantedBarricade(new Barricade(asset),
                TeamManager.Team2Main.Center3D, Quaternion.Euler(BlockerSpawnRotation), 0, 0);
    }
    public void DestoryBlockerOnT1()
    {
        if (_blockerBarricadeT1 != null && Regions.tryGetCoordinate(_blockerBarricadeT1.position, out byte x, out byte y))
        {
            BarricadeDrop drop = BarricadeManager.regions[x, y].FindBarricadeByRootTransform(_blockerBarricadeT1);
            if (drop != null)
            {
                BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                return;
            }
            _blockerBarricadeT1 = null;
        }

        if (Config.BarricadeZoneBlockerTeam1.ValidReference(out Guid g))
        {
            for (x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                    {
                        BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                        if (d.asset.GUID == g)
                        {
                            BarricadeManager.destroyBarricade(d, x, y, ushort.MaxValue);
                            return;
                        }
                    }
                }
            }
        }
    }
    public void DestoryBlockerOnT2()
    {
        if (_blockerBarricadeT2 != null && Regions.tryGetCoordinate(_blockerBarricadeT2.position, out byte x, out byte y))
        {
            BarricadeDrop drop = BarricadeManager.regions[x, y].FindBarricadeByRootTransform(_blockerBarricadeT2);
            if (drop != null)
            {
                BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                return;
            }
            _blockerBarricadeT2 = null;
        }

        if (Config.BarricadeZoneBlockerTeam2.ValidReference(out Guid g))
        {
            for (x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                    {
                        BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                        if (d.asset.GUID == g)
                        {
                            BarricadeManager.destroyBarricade(d, x, y, ushort.MaxValue);
                            return;
                        }
                    }
                }
            }
        }
    }
    public void DestroyBlockers()
    {
        try
        {
            bool backup = false;
            if (_blockerBarricadeT1 != null && Regions.tryGetCoordinate(_blockerBarricadeT1.position, out byte x, out byte y))
            {
                BarricadeDrop drop = BarricadeManager.regions[x, y].FindBarricadeByRootTransform(_blockerBarricadeT1);
                if (drop != null)
                {
                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                }
                else
                {
                    backup = true;
                }
                _blockerBarricadeT1 = null;
            }
            else backup = true;
            if (_blockerBarricadeT2 != null && Regions.tryGetCoordinate(_blockerBarricadeT2.position, out x, out y))
            {
                BarricadeDrop drop = BarricadeManager.regions[x, y].FindBarricadeByRootTransform(_blockerBarricadeT2);
                if (drop != null)
                {
                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                }
                else
                {
                    backup = true;
                }
                _blockerBarricadeT2 = null;
            }
            else backup = true;
            if (backup)
            {
                if (!Config.BarricadeZoneBlockerTeam1.ValidReference(out Guid g1) || !Config.BarricadeZoneBlockerTeam2.ValidReference(out Guid g2)) return;
                bool l = false;
                for (x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                        {
                            BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                            if (d.asset.GUID == g1 || d.asset.GUID == g2)
                            {
                                BarricadeManager.destroyBarricade(d, x, y, ushort.MaxValue);
                                if (l) return;
                                else l = true;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            L.LogError("Failed to destroy zone blockers in gamemode " + Name);
            L.LogError(ex);
        }
    }
    public override void OnPlayerDeath(PlayerDied e)
    {
        base.OnPlayerDeath(e);
        _mainCampers.Remove(e.Player.Steam64);
        EventFunctions.RemoveDamageMessageTicks(e.Player.Steam64);
    }
    protected override Task PlayerInit(UCPlayer player, bool wasAlreadyOnline, CancellationToken token)
    {
        // token.CombineIfNeeded(UnloadToken, player.DisconnectToken);
        if (!UseTeamSelector)
        {
            ulong team = player.Save.Team;
            if (this is not IEndScreen { IsScreenUp: true } && !TeamManager.IsFriendly(player.Player, team))
            {
                TeamManager.JoinTeam(player, player.Save.Team, player.Save.LastGame != GameID || player.Save.ShouldRespawnOnJoin, true);
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
                tickets.TicketManager.SendUI(player);
            InitUI(player);
        }

        CancellationToken token = player.DisconnectToken;
        token.CombineIfNeeded(UnloadToken);
        UCWarfare.RunTask(async tkn =>
        {
            await UCWarfare.ToUpdate(tkn);
            for (int i = 0; i < this.Singletons.Count; ++i)
            {
                IUncreatedSingleton singleton = Singletons[i];
                if (singleton is IJoinedTeamListener l1)
                    l1.OnJoinTeam(player, team);
                if (singleton is IJoinedTeamListenerAsync l2)
                {
                    Task task = l2.OnJoinTeamAsync(player, team, tkn);
                    if (!task.IsCompleted)
                    {
                        await task.ConfigureAwait(false);
                        await UCWarfare.ToUpdate(tkn);
                    }
                }
            }
        }, token, "Joining team: " + player.Steam64 + ".");
    }
    public override void PlayerLeave(UCPlayer player)
    {
        _mainCampers.Remove(player.Steam64);
        base.PlayerLeave(player);
    }
}
