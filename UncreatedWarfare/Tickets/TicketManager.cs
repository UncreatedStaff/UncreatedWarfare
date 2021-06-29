using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Tickets
{
    public class TicketManager
    {
        public static Config<TicketData> config;

        public static int Team1Tickets;
        public static int Team2Tickets;

        public TicketManager()
        {
            config = new Config<TicketData>(Data.TicketStorage, "config.json");

            Team1Tickets = config.data.StartingTickets;
            Team2Tickets = config.data.StartingTickets;

            FlagManager.OnNewGameStarting += OnNewGameStarting;
            VehicleManager.OnVehicleExploded += OnVehicleExploded;
        }

        public static async Task OnPlayerDeath(UCWarfare.DeathEventArgs eventArgs)
        {
            if (KitManager.HasKit(eventArgs.dead.channel.owner.playerID.steamID, out var kit))
            {
                if (TeamManager.IsTeam1(eventArgs.dead))
                {
                    await AddTeam1Tickets(-1*kit.TicketCost);
                    F.Log($"TICKETS: Subtracted {kit.TicketCost} ticket from Team 1");
                }
                else if (TeamManager.IsTeam2(eventArgs.dead))
                {
                    await AddTeam2Tickets(-1 * kit.TicketCost);
                    F.Log($"TICKETS: Subtracted {kit.TicketCost} ticket from Team 1");
                }
                else
                    F.Log($"TICKETS: player was not on a team");
            }
            else
            {
                F.Log($"TICKETS: player did not have a kit");
            }
            F.Log("Team 1 Tickets: " + Team1Tickets);
            F.Log("Team 2 Tickets: " + Team2Tickets);
        }
        public static async Task OnPlayerSuicide(UCWarfare.SuicideEventArgs eventArgs)
        {
            if (KitManager.HasKit(eventArgs.dead.channel.owner.playerID.steamID, out var kit))
            {
                if (TeamManager.IsTeam1(eventArgs.dead))
                {
                    await AddTeam1Tickets(-1 * kit.TicketCost);
                    F.Log($"TICKETS: Subtracted {kit.TicketCost} ticket from Team 1");
                }
                else if (TeamManager.IsTeam2(eventArgs.dead))
                {
                    await AddTeam2Tickets(-1 * kit.TicketCost);
                    F.Log($"TICKETS: Subtracted {kit.TicketCost} ticket from Team 1");
                }
                else
                    F.Log($"TICKETS: player was not on a team");
            }
            else
            {
                F.Log($"TICKETS: player did not have a kit");
            }
            F.Log("Team 1 Tickets: " + Team1Tickets);
            F.Log("Team 2 Tickets: " + Team2Tickets);
        }

        private static async void OnVehicleExploded(InteractableVehicle vehicle)
        {
            if (VehicleBay.VehicleExists(vehicle.id, out var vehicleData))
            {
                if (TeamManager.IsTeam1(vehicle.lockedGroup))
                {
                    await AddTeam1Tickets(-1 * vehicleData.TicketCost);
                }
                if (TeamManager.IsTeam2(vehicle.lockedGroup))
                {
                    await AddTeam2Tickets(-1 * vehicleData.TicketCost);
                }
            }
        }
        public static void OnPlayerJoined(UCPlayer player)
        {
            ulong team = player.GetTeam();
            GetTeamBleed(team, out int bleed, out var message);
            UpdateUI(player.Player.channel.owner.transportConnection, team, bleed, message);

        }
        public static void OnPlayerLeft(UCPlayer player)
        {
            
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            EffectManager.askEffectClearByID(config.data.Team1TicketUIID, player.transportConnection);
            EffectManager.askEffectClearByID(config.data.Team2TicketUIID, player.transportConnection);
            GetTeamBleed(newGroup, out int bleed, out var message);
            UpdateUI(player.transportConnection, newGroup, bleed, message);
        }

        private static void OnNewGameStarting(object sender, EventArgs e)
        {
            Team1Tickets = config.data.StartingTickets;
            Team2Tickets = config.data.StartingTickets;
            UpdateUITeam1();
            UpdateUITeam2();
        }

        public static async Task AddTeam1Tickets(int number)
        {
            Team1Tickets += number;
            if (Team1Tickets <= 0)
            {
                Team1Tickets = 0;
                await Data.FlagManager.DeclareWin(TeamManager.Team2ID);
            }
            UpdateUITeam1();
        }
        public static async Task AddTeam2Tickets(int number)
        {
            Team2Tickets += number;
            if (Team2Tickets <= 0)
            {
                Team2Tickets = 0;
                await Data.FlagManager.DeclareWin(TeamManager.Team1ID);
            }
            UpdateUITeam2();
        }

        public static void UpdateUI(ITransportConnection connection, ulong team, int bleed, string message)
        {
            ushort UIID = 0;
            int tickets = 0;
            if (TeamManager.IsTeam1(team))
            {
                tickets = Team1Tickets;
                UIID = config.data.Team1TicketUIID;
            }

            else if (TeamManager.IsTeam2(team))
            {
                tickets = Team2Tickets;
                UIID = config.data.Team2TicketUIID;
            }
                
            EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
                tickets.ToString(Data.Locale),
                bleed.ToString(Data.Locale),
                message
                );
        }
        public static void UpdateUITeam1()
        {
            GetTeamBleed(TeamManager.Team1ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam1()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team1ID, bleed, message);
            }
        }
        public static void UpdateUITeam2()
        {
            GetTeamBleed(TeamManager.Team2ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam2()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team2ID, bleed, message);
            }
        }

        public static void GetTeamBleed(ulong team, out int bleed, out string message)
        {
            int friendlyCount = Data.FlagManager.AllFlags.Where(f => f.Owner == team).Count();
            int enemyCount = Data.FlagManager.AllFlags.Where(f => f.Owner != team).Count();

            int diff = friendlyCount - enemyCount;

            if (diff == 0 || diff == 1)
            {
                bleed = 0;
                message = "";
            }
            else if (diff == 2 || diff == 3)
            {
                message = "Your team is in control!";
                bleed = 1;
            }
            else if (diff >= 3 && enemyCount != 0)
            {
                message = "Your team is dominating!";
                bleed = 2;
            }
            else if (diff == -1 || diff == -2)
            {
                message = "The enemy is in control!";
                bleed = -1;
            }
            else if (diff <= -3 && friendlyCount != 0)
            {
                message = "The enemy is dominating!";
                bleed = -2;
            }
            else if (friendlyCount == Data.FlagManager.AllFlags.Count)
            {
                message = "You are victorious!";
                bleed = 3;
            }
            else if (enemyCount == Data.FlagManager.AllFlags.Count)
            {
                message = "You are defeated.";
                bleed = -3;
            }
            else
            {
                bleed = 0;
                message = "";
            }
        }

        public void Dispose()
        {
            FlagManager.OnNewGameStarting -= OnNewGameStarting;
            VehicleManager.OnVehicleExploded -= OnVehicleExploded;
        }
    }
    public class TicketData : ConfigData
    {
        public ushort StartingTickets;
        public ushort FOBCost;
        public ushort Team1TicketUIID;
        public ushort Team2TicketUIID;

        public override void SetDefaults()
        {
            StartingTickets = 600;
            FOBCost = 20;
            Team1TicketUIID = 32390;
            Team1TicketUIID = 32391;
        }
        public TicketData() { }
    }
}
