using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes
{
    /// <summary>Gamemode with 2 teams</summary>
    public abstract class TeamGamemode : Gamemode, ITeams
    {
        protected const int AMC_TIME = 10;
        protected TeamManager _teamManager;
        public TeamManager TeamManager { get => _teamManager; }
        protected JoinManager _joinManager;
        public JoinManager JoinManager { get => _joinManager; }

        public virtual bool UseJoinUI { get => true; }
        public virtual bool EnableAMC { get => true; }
        public List<ulong> InAMC = new List<ulong>();
        private Transform _blockerBarricadeT1;
        private Transform _blockerBarricadeT2;
        protected TeamGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        {

        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline, bool shouldRespawn)
        {
            base.OnPlayerJoined(player, wasAlreadyOnline, shouldRespawn);
            if (UseJoinUI)
            {
                _joinManager.OnPlayerConnected(player, shouldRespawn);
            }
        }
        public override void Init()
        {
            if (UseJoinUI)
            {
                _joinManager = gameObject.AddComponent<JoinManager>();
                _joinManager.Initialize();
            }
            base.Init();
        }
        public override void OnLevelLoaded()
        {
            _teamManager = new TeamManager();
            base.OnLevelLoaded();
        }
        public override void Dispose()
        {
            base.Dispose();
            DestroyBlockers();
            _joinManager?.Dispose();
            Destroy(_joinManager);
        }
        protected void CheckPlayersAMC()
        {
            if (EnableAMC)
            {
                IEnumerator<SteamPlayer> players = Provider.clients.GetEnumerator();
                while (players.MoveNext())
                {
                    ulong team = players.Current.GetTeam();
                    UCPlayer player = UCPlayer.FromSteamPlayer(players.Current);
                    try
                    {
                        if (!player.OnDutyOrAdmin() && !players.Current.player.life.isDead && ((team == 1 && TeamManager.Team2AMC.IsInside(players.Current.player.transform.position)) ||
                            (team == 2 && TeamManager.Team1AMC.IsInside(players.Current.player.transform.position))))
                        {
                            if (!InAMC.Contains(players.Current.playerID.steamID.m_SteamID))
                            {
                                InAMC.Add(players.Current.playerID.steamID.m_SteamID);
                                int a = Mathf.RoundToInt(AMC_TIME);
                                ToastMessage.QueueMessage(players.Current,
                                    new ToastMessage(Translation.Translate("entered_enemy_territory", players.Current.playerID.steamID.m_SteamID, a.ToString(Data.Locale), a.S()),
                                    EToastMessageSeverity.WARNING));
                                UCWarfare.I.StartCoroutine(KillPlayerInEnemyTerritory(players.Current));
                            }
                        }
                        else
                        {
                            InAMC.Remove(players.Current.playerID.steamID.m_SteamID);
                        }
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error checking for duty players on player " + players.Current.playerID.playerName);
                        if (UCWarfare.Config.Debug)
                            L.LogError(ex);
                    }
                }
                players.Dispose();
            }
        }
        public IEnumerator<WaitForSeconds> KillPlayerInEnemyTerritory(SteamPlayer player)
        {
            yield return new WaitForSeconds(AMC_TIME);
            if (player != null && !player.player.life.isDead && InAMC.Contains(player.playerID.steamID.m_SteamID))
            {
                player.player.movement.forceRemoveFromVehicle();
                player.player.life.askDamage(byte.MaxValue, Vector3.zero, EDeathCause.ACID, ELimb.SKULL, Provider.server, out _, false, ERagdollEffect.NONE, false, true);
            }
        }

        public void SpawnBlockerOnT1()
        {
            if (Assets.find(Config.MapConfig.T1ZoneBlocker) is ItemBarricadeAsset t1)
                _blockerBarricadeT1 = BarricadeManager.dropNonPlantedBarricade(new Barricade(t1),
                    TeamManager.Team1Main.Center3DAbove, Quaternion.Euler(BLOCKER_SPAWN_ROTATION), 0, 0);
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
            for (x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                    {
                        BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                        if (d.asset.GUID == Config.MapConfig.T1ZoneBlocker)
                        {
                            BarricadeManager.destroyBarricade(d, x, y, ushort.MaxValue);
                            return;
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
            for (x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                    {
                        BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                        if (d.asset.GUID == Config.MapConfig.T2ZoneBlocker)
                        {
                            BarricadeManager.destroyBarricade(d, x, y, ushort.MaxValue);
                            return;
                        }
                    }
                }
            }
        }
        public void DestroyBlockers()
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
                bool l = false;
                for (x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                        {
                            BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                            if (d.asset.GUID == Config.MapConfig.T1ZoneBlocker || d.asset.GUID == Config.MapConfig.T2ZoneBlocker)
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
        public void SpawnBlockers()
        {
            if (Assets.find(Config.MapConfig.T1ZoneBlocker) is ItemBarricadeAsset t1)
                _blockerBarricadeT1 = BarricadeManager.dropNonPlantedBarricade(new Barricade(t1),
                    TeamManager.Team1Main.Center3DAbove, Quaternion.Euler(BLOCKER_SPAWN_ROTATION), 0, 0);
            if (Assets.find(Config.MapConfig.T2ZoneBlocker) is ItemBarricadeAsset t2)
                _blockerBarricadeT2 = BarricadeManager.dropNonPlantedBarricade(new Barricade(t2),
                    TeamManager.Team2Main.Center3DAbove, Quaternion.Euler(BLOCKER_SPAWN_ROTATION), 0, 0);
        }
        public void SpawnBlockerOnT2()
        {
            if (Assets.find(Config.MapConfig.T2ZoneBlocker) is ItemBarricadeAsset t2)
                _blockerBarricadeT2 = BarricadeManager.dropNonPlantedBarricade(new Barricade(t2),
                    TeamManager.Team2Main.Center3DAbove, Quaternion.Euler(BLOCKER_SPAWN_ROTATION), 0, 0);
        }
        public override void OnPlayerDeath(UCWarfare.DeathEventArgs args)
        {
            base.OnPlayerDeath(args);
            InAMC.Remove(args.dead.channel.owner.playerID.steamID.m_SteamID);
            EventFunctions.RemoveDamageMessageTicks(args.dead.channel.owner.playerID.steamID.m_SteamID);
        }
        public override void StartNextGame(bool onLoad = false)
        {
            base.StartNextGame(onLoad);
            if (UseJoinUI && _joinManager != null)
                _joinManager.OnNewGameStarting();
        }
    }
}
