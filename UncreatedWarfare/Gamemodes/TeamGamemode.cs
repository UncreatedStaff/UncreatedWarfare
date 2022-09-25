using SDG.Unturned;
using System;
using System.Collections.Generic;
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
    protected const int AMC_TIME = 10;
    protected TeamSelector _teamSelector;
    public List<ulong> InAMC = new List<ulong>();
    private Transform? _blockerBarricadeT1;
    private Transform? _blockerBarricadeT2;
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
    protected override void PreDispose()
    {
        if (HasOnReadyRan)
            DestroyBlockers();
    }
    protected override void PostInit()
    {
        if (UseTeamSelector)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                TeamSelector.JoinSelectionMenu(PlayerManager.OnlinePlayers[i]);
        }
    }
    protected override void PreGameStarting(bool isOnLoad)
    {
        if (UseTeamSelector)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                _teamSelector.JoinSelectionMenu(PlayerManager.OnlinePlayers[i]);
        }

        base.PreGameStarting(isOnLoad);
    }
    public override void PlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        base.PlayerInit(player, wasAlreadyOnline);
    }
    protected override void OnReady()
    {
        TeamManager.CheckGroups();
    }
    protected void CheckPlayersAMC()
    {
        if (EnableAMC)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer cnt = PlayerManager.OnlinePlayers[i];
                ulong team = cnt.GetTeam();
                try
                {
                    if (!cnt.OnDutyOrAdmin() && !cnt.Player.life.isDead && ((team == 1 && TeamManager.Team2AMC.IsInside(cnt.Player.transform.position)) ||
                                                                            (team == 2 && TeamManager.Team1AMC.IsInside(cnt.Player.transform.position))))
                    {
                        if (!InAMC.Contains(cnt.Steam64))
                        {
                            InAMC.Add(cnt.Steam64);
                            int a = Mathf.RoundToInt(AMC_TIME);
                            ToastMessage.QueueMessage(cnt,
                                new ToastMessage(T.EnteredEnemyTerritory.Translate(cnt, a.GetTimeFromSeconds(cnt.Steam64)),
                                    EToastMessageSeverity.WARNING));
                            UCWarfare.I.StartCoroutine(KillPlayerInEnemyTerritory(cnt));
                        }
                    }
                    else
                    {
                        InAMC.Remove(cnt.Steam64);
                    }
                }
                catch (Exception ex)
                {
                    L.LogError("Error checking for duty players on player " + cnt.Name.PlayerName + " (" + cnt.Steam64 + ").");
                    if (UCWarfare.Config.Debug)
                        L.LogError(ex);
                }
            }
        }
    }
    public IEnumerator<WaitForSeconds> KillPlayerInEnemyTerritory(SteamPlayer player)
    {
        yield return new WaitForSeconds(AMC_TIME);
        if (player != null && !player.player.life.isDead && InAMC.Contains(player.playerID.steamID.m_SteamID))
        {
            player.player.movement.forceRemoveFromVehicle();
            player.player.life.askDamage(byte.MaxValue, Vector3.zero, DeathTracker.MAIN_DEATH, ELimb.SKULL, Provider.server, out _, false, ERagdollEffect.NONE, false, true);
        }
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
        InAMC.Remove(e.Player.Steam64);
        EventFunctions.RemoveDamageMessageTicks(e.Player.Steam64);
    }
    protected override void OnAsyncInitComplete(UCPlayer player)
    {
        if (UseTeamSelector)
            _teamSelector.JoinSelectionMenu(player);

        base.OnAsyncInitComplete(player);
    }

    public virtual void OnJoinTeam(UCPlayer player, ulong team)
    {
        if (team is 1 or 2 && _state == EState.STAGING)
            ShowStagingUI(player);
    }
}
