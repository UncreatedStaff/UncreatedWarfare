using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags;

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
    public interface ISquads : ITeams
    {
        Squads.SquadManager SquadManager { get; }
    }
    public interface ITeams : IGamemode
    {
        bool UseJoinUI { get; }
        Teams.JoinManager JoinManager { get; }
        void OnJoinTeam(UCPlayer player, ulong newTeam);
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
        bool IsAttackSite(ulong team, Flag flag);
        bool IsDefenseSite(ulong team, Flag flag);
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
