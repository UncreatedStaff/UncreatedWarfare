using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Tickets
{
    public class TicketManager : JSONSaver<TicketManagerConfig>, IDisposable
    {
        public static TicketManagerConfig config;

        public static int Team1Tickets;
        public static int Team2Tickets;

        public TicketManager()
            : base(Data.TeamStorage + "tickets.json")
        {
            Team1Tickets = config.StartingTickets;
            Team2Tickets = config.StartingTickets;

            if (GetExistingObjects().Count == 0)
            {
                LoadDefaults();
            }

            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            VehicleManager.OnVehicleExploded += OnVehicleExploded;
            Data.FlagManager.OnNewGameStarting += OnNewGameStarting;
        }
        protected override string LoadDefaults()
        {
            TicketManagerConfig defaults = new TicketManagerConfig();

            WriteSingleObject(defaults);
            config = defaults;
            return "";
        }
        public static void ReloadConfig() => GetExistingObjects().FirstOrDefault();
        public static void SaveConfig() => WriteSingleObject(config);

        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (KitManager.HasKit(player.CSteamID, out var kit))
            {
                if (TeamManager.IsTeam1(player))
                {
                    AddTeam1Tickets(-1*kit.TicketCost);
                }
                if (TeamManager.IsTeam2(player))
                {
                    AddTeam2Tickets(-1 * kit.TicketCost);
                }
            }
        }

        private void OnVehicleExploded(InteractableVehicle vehicle)
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
            Team1Tickets = config.StartingTickets;
            Team2Tickets = config.StartingTickets;
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
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            VehicleManager.OnVehicleExploded -= OnVehicleExploded;
            Data.FlagManager.OnNewGameStarting -= OnNewGameStarting;
        }
    }
    public class TicketManagerConfig
    {
        public ushort StartingTickets;
        public ushort FOBTicketCost;

        public TicketManagerConfig()
        {
            StartingTickets = 600;
            FOBTicketCost = 20;
        }
    }
}
