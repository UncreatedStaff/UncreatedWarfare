using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Gamemodes.Interfaces;

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
public interface ITraits : IGamemode
{
    Traits.TraitManager TraitManager { get; }
}

public interface ITeams : IGamemode, IJoinedTeamListener
{
    bool UseTeamSelector { get; }
    Teams.TeamSelector? TeamSelector { get; }
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
    List<Flag> Rotation { get; }
    List<Flag> LoadedFlags { get; }
    Dictionary<ulong, int> OnFlag { get; }
    bool AllowPassengersToCapture { get; }
    bool IsAttackSite(ulong team, Flag flag);
    bool IsDefenseSite(ulong team, Flag flag);
}
public interface IFlagTeamObjectiveGamemode : IFlagRotation
{
    Flag? ObjectiveTeam1 { get; }
    Flag? ObjectiveTeam2 { get; }
    int ObjectiveT1Index { get; }
    int ObjectiveT2Index { get; }
}
public interface IFOBs : IGamemode
{
    FOBs.FOBManager FOBManager { get; }
}
public interface IStructureSaving : IGamemode
{
    Structures.StructureSaver StructureSaver { get; }
}
public interface IAttackDefense : ITeams
{
    ulong AttackingTeam { get; }
    ulong DefendingTeam { get; }
}
public interface ITeamScore : ITeams
{
    int Team1Score { get; }
    int Team2Score { get; }
}