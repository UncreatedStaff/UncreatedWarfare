using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IRevives : IGamemode
    {
        Revives.ReviveManager ReviveManager { get; }
    }
    public interface IVehicles : IStructureSaving, IGamemode
    {
        Vehicles.VehicleBay VehicleBay { get; }
        Vehicles.VehicleSpawner VehicleSpawner { get; }
        Vehicles.VehicleSigns VehicleSigns { get; }
    }
    public interface IKitRequests : IGamemode
    {
        Kits.RequestSigns RequestSigns { get; }
        Kits.KitManager KitManager { get; }
    }
    public interface ISquads : IGamemode
    {
        Squads.SquadManager SquadManager { get; }
    }
    public interface ITeams : IGamemode
    {
        Teams.TeamManager TeamManager { get; }
        bool UseJoinUI { get; }
        Teams.JoinManager JoinManager { get; }
    }
    public interface ITickets : IGamemode
    {
        Tickets.TicketManager TicketManager { get; }
    }
    public interface IGameStats : IGamemode
    {
        object GameStats { get; }
    }
    public interface IFlagRotation : IGamemode
    {
        List<Flags.Flag> Rotation { get; }
        List<Flags.Flag> LoadedFlags { get; }
        Dictionary<ulong, int> OnFlag { get; }
    }
    public interface IFOBs : IGamemode
    {
        FOBs.FOBManager FOBManager { get; }
    }
    public interface IStructureSaving : IGamemode
    {
        Structures.StructureSaver StructureSaver { get; }
    }
}
