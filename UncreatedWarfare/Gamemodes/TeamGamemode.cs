using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes;

/// <summary>Gamemode with 2 teams</summary>
public abstract class TeamGamemode : Gamemode, ITeams
{
    protected TeamSelector _teamSelector;
    private Transform? _blockerBarricadeT1;
    private Transform? _blockerBarricadeT2;
    private readonly List<ulong> mainCampers = new List<ulong>(24);
    public TeamSelector TeamSelector { get => _teamSelector; }
    public virtual bool UseTeamSelector { get => true; }
    public virtual bool EnableAMC { get => true; }
    protected TeamGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
    {

    }
    protected override void PreInit()
    {
        if (UseTeamSelector)
            AddSingletonRequirement(ref _teamSelector);
    }
    protected override Task PreDispose()
    {
        ThreadUtil.assertIsGameThread();
        if (HasOnReadyRan)
            DestroyBlockers();

        return Task.CompletedTask;
    }
    protected override Task PostInit()
    {
        ThreadUtil.assertIsGameThread();
        if (UseTeamSelector)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                TeamSelector.JoinSelectionMenu(PlayerManager.OnlinePlayers[i]);
        }

        return Task.CompletedTask;
    }
    protected override Task PreGameStarting(bool isOnLoad)
    {
        ThreadUtil.assertIsGameThread();
        if (UseTeamSelector)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                _teamSelector.JoinSelectionMenu(PlayerManager.OnlinePlayers[i]);
        }

        return base.PreGameStarting(isOnLoad);
    }
    protected override Task OnReady()
    {
        ThreadUtil.assertIsGameThread();
        TeamManager.CheckGroups();
        return Task.CompletedTask;
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
            if (!mainCampers.Contains(player.Steam64))
            {
                mainCampers.Add(player.Steam64);
                OnPlayerMainCamping(player);
            }
            continue;
        notInMain:
            mainCampers.Remove(player.Steam64);
        }
    }
    private void OnPlayerMainCamping(UCPlayer player)
    {
        ToastMessage.QueueMessage(player, new ToastMessage(
            T.EnteredEnemyTerritory.Translate(player, Mathf.RoundToInt(Config.GeneralAMCKillTime.Value).GetTimeFromSeconds(player)),
            EToastMessageSeverity.WARNING));
        player.Player.StartCoroutine(PlayerMainCampingCoroutine(player));
    }
    private IEnumerator PlayerMainCampingCoroutine(UCPlayer player)
    {
        ulong team = player.GetTeam();
        if (Config.GeneralAMCKillTime.Value != 0)
            yield return new WaitForSecondsRealtime(Config.GeneralAMCKillTime.Value);
        if (player.Player == null || !mainCampers.Contains(player.Steam64) || player.Player.life.isDead || player.OnDuty())
            yield break;
        player.Player.movement.forceRemoveFromVehicle();
        yield return null;
        player.Player.life.askDamage(byte.MaxValue, Vector3.up / 8f, DeathTracker.MAIN_DEATH, ELimb.SPINE, Provider.server, out _, false, ERagdollEffect.NONE, false, true);
        ActionLogger.Add(EActionLogType.MAIN_CAMP_ATTEMPT, $"Player team: {TeamManager.TranslateName(team, 0, false)}, " +
                                                           $"Team: {TeamManager.TranslateName(TeamManager.Other(team), 0, false)}, " +
                                                           $"Location: {player.Position.ToString("0.#", Data.Locale)}", player);
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
                TeamManager.Team1Main.Center3D + Vector3.up, Quaternion.Euler(BLOCKER_SPAWN_ROTATION), 0, 0);
    }
    public void SpawnBlockerOnT2()
    {
        if (Config.BarricadeZoneBlockerTeam2.ValidReference(out ItemBarricadeAsset asset))
            _blockerBarricadeT2 = BarricadeManager.dropNonPlantedBarricade(new Barricade(asset),
                TeamManager.Team2Main.Center3D, Quaternion.Euler(BLOCKER_SPAWN_ROTATION), 0, 0);
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
        mainCampers.Remove(e.Player.Steam64);
        EventFunctions.RemoveDamageMessageTicks(e.Player.Steam64);
    }
    public override async Task PlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        ThreadUtil.assertIsGameThread();
        await base.PlayerInit(player, wasAlreadyOnline);
        await UCWarfare.ToUpdate();
        ThreadUtil.assertIsGameThread();
        if (UseTeamSelector)
            _teamSelector.JoinSelectionMenu(player);
    }
    public virtual void OnJoinTeam(UCPlayer player, ulong team)
    {
        if (team is 1 or 2 && _state == EState.STAGING)
            ShowStagingUI(player);
    }
    public override void PlayerLeave(UCPlayer player)
    {
        mainCampers.Remove(player.Steam64);
        base.PlayerLeave(player);
    }
}
