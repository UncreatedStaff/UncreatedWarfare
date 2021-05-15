using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare.Stats
{
    public class PlaytimeComponent : UnturnedPlayerComponent
    {
        public float CurrentTimeSeconds;
        public PlayerCurrentGameStats stats;
        protected override void Load()
        {
            CurrentTimeSeconds = 0.0f;
            CommandWindow.Log("Started tracking " + Player.Player.channel.owner.playerID.playerName + "'s playtime.");
            base.Load();
        }
        public void Update()
        {
            float dt = Time.deltaTime;
            CurrentTimeSeconds += dt;
            if (stats == null)
            {
                if (!UCWarfare.I.GameStats.TryGetPlayer(Player.Player.channel.owner.playerID.steamID.m_SteamID, out stats))
                {
                    stats = new PlayerCurrentGameStats(Player.Player);
                    UCWarfare.I.GameStats.playerstats.Add(Player.Player.channel.owner.playerID.steamID.m_SteamID, stats);
                }
            }
            if(Player.Player.IsOnFlag())
            {
                stats.AddToTimeOnPoint(dt);
                stats.AddToTimeDeployed(dt);
            }
            else if (!Player.Player.IsInMain())
                stats.AddToTimeDeployed(dt);
            if(Player.IsInVehicle)
            {
                if(Player.CurrentVehicle != null)
                {
                    Player.CurrentVehicle.findPlayerSeat(Player.Player.channel.owner.playerID.steamID, out byte seat);
                    if(seat == 0)
                        stats.AddToTimeDriving(dt);
                }
            }
        }
    }
}
