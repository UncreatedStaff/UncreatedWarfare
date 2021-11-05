using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes
{
    /// <summary>Gamemode with 2 teams</summary>
    public abstract class TeamGamemode : Gamemode, ITeams//, IStructureSaving, IFOBs, IKitRequests, IRevives, ISquads, IImplementsLeaderboard
    {
        const int AMC_TIME = 10;
        protected TeamManager _teamManager;
        public TeamManager TeamManager { get => _teamManager; }
        protected JoinManager _joinManager;
        public JoinManager JoinManager { get => _joinManager; }

        public virtual bool UseJoinUI { get => true; }
        public virtual bool EnableAMC { get => true; }
        public List<ulong> InAMC = new List<ulong>();

        protected TeamGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        {

        }

        public override void Init()
        {
            if (UseJoinUI)
            {
                _joinManager = gameObject.AddComponent<JoinManager>();
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
                                    F.Translate("entered_enemy_territory", players.Current.playerID.steamID.m_SteamID, a.ToString(Data.Locale), a.S()),
                                    ToastMessageSeverity.WARNING);
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
                        F.LogError("Error checking for duty players on player " + players.Current.playerID.playerName);
                        if (UCWarfare.Config.Debug)
                            F.LogError(ex);
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
    }
}
