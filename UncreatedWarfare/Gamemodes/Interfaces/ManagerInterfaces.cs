using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IRevives
    {
        Revives.ReviveManager ReviveManager { get; }
    }
    public interface IVehicles : IStructureSaving
    {
        Vehicles.VehicleBay VehicleBay { get; }
        Vehicles.VehicleSpawn VehicleSpawn { get; }
        Vehicles.VehicleSigns VehicleSigns { get; }
    }
    public interface IKitRequests
    {
        Kits.RequestSigns RequestSigns { get; }
        Kits.KitManager KitManager { get; }
    }
    public interface ISquads
    {
        Squads.SquadManager SquadManager { get; }
    }
    public interface ITeams
    {
        Teams.TeamManager TeamManager { get; }
        bool UseTeamsUI { get; }
        Teams.JoinManager JoinManager { get; }
    }
    public interface ITickets
    {
        Tickets.TicketManager TicketManager { get; }
    }
    public interface IFlagRotation
    {
        List<Flags.Flag> Rotation { get; }
        Dictionary<ulong, int> OnFlag { get; }
    }
    public interface IFOBs
    {
        FOBs.FOBManager FOBManager { get; }
    }
    public interface IStructureSaving
    {
        Structures.StructureSaver StructureSaver { get; }
    }
}
