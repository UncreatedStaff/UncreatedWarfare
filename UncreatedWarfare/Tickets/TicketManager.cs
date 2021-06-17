using Rocket.Unturned;
using Rocket.Unturned.Player;
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
            config = new Config<TicketData>(Data.TicketStorage + "config.json");

            Team1Tickets = config.data.StartingTickets;
            Team2Tickets = config.data.StartingTickets;

            FlagManager.OnNewGameStarting += OnNewGameStarting;
            VehicleManager.OnVehicleExploded += OnVehicleExploded;
        }

        public static void OnPlayerDeath(UCWarfare.DeathEventArgs eventArgs)
        {
            if (KitManager.HasKit(eventArgs.dead.channel.owner.playerID.steamID, out var kit))
            {
                if (TeamManager.IsTeam1(eventArgs.dead))
                {
                    AddTeam1Tickets(-1*kit.TicketCost);
                }
                if (TeamManager.IsTeam2(eventArgs.dead))
                {
                    AddTeam2Tickets(-1 * kit.TicketCost);
                }
            }
        }

        private static void OnVehicleExploded(InteractableVehicle vehicle)
        {
            if (VehicleBay.VehicleExists(vehicle.id, out var vehicleData))
            {
                if (TeamManager.IsTeam1(vehicle.lockedGroup))
                {
                    AddTeam1Tickets(-1 * vehicleData.TicketCost);
                }
                if (TeamManager.IsTeam2(vehicle.lockedGroup))
                {
                    AddTeam2Tickets(-1 * vehicleData.TicketCost);
                }
            }
        }

        private void OnNewGameStarting(object sender, EventArgs e)
        {
            Team1Tickets = config.data.StartingTickets;
            Team2Tickets = config.data.StartingTickets;
        }

        public static void AddTeam1Tickets(int number)
        {
            Team1Tickets += number;
            if (Team1Tickets <= 0)
            {
                Team1Tickets = 0;
                Data.FlagManager.DeclareWin(TeamManager.Team2ID);
            }
        }
        public static void AddTeam2Tickets(int number)
        {
            Team2Tickets += number;
            if (Team2Tickets <= 0)
            {
                Team2Tickets = 0;
                Data.FlagManager.DeclareWin(TeamManager.Team1ID);
            }
        }

        public void UpdateUI(CSteamID steamID)
        {
            // TODO:
        }
        public void UpdateUIAll()
        {
            foreach (var steamPlayer in Provider.clients)
            {
                UpdateUI(steamPlayer.playerID.steamID);
            }
        }
        public void UpdateUITeam1()
        {
            foreach (var steamPlayer in Provider.clients.Where(sp => TeamManager.IsTeam1(sp.playerID.steamID)))
            {
                UpdateUI(steamPlayer.playerID.steamID);
            }
        }
        public void UpdateUITeam2()
        {
            foreach (var steamPlayer in Provider.clients.Where(sp => TeamManager.IsTeam2(sp.playerID.steamID)))
            {
                UpdateUI(steamPlayer.playerID.steamID);
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
            Team1TicketUIID = 30050;
            Team1TicketUIID = 30051;
        }

        public TicketData() { }
    }
}
