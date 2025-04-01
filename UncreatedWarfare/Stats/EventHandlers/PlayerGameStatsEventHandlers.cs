using System;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Stats.EventHandlers;

internal sealed class PlayerGameStatsEventHandlers :
    IEventListener<PlayerDied>,
    IEventListener<VehicleExploded>,
    IEventListener<FlagCaptured>
{
    private readonly WarfareModule _module;

    public PlayerGameStatsEventHandlers(WarfareModule module)
    {
        _module = module;
    }

    private LeaderboardPhase? GetPhase()
    {
        if (_module.IsLayoutActive() && _module.GetActiveLayout().Phases.OfType<LeaderboardPhase>().FirstOrDefault() is { } phase)
            return phase;

        return null;
    }

    [EventListener(RequireActiveLayout = true)]
    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        e.Player.ComponentOrNull<PlayerGameStatsComponent>()?.AddToStat(KnownStatNames.Deaths, 1);
        string killStat = e.WasTeamkill ? KnownStatNames.Teamkills : KnownStatNames.Kills;
        if (e.Killer != null && e.Killer.Team == e.KillerTeam)
        {
            e.Killer.ComponentOrNull<PlayerGameStatsComponent>()?.AddToStat(killStat, 1);
        }
        else if (e.KillerTeam is { IsValid: true })
        {
            LeaderboardPhase? phase = GetPhase();
            phase?.AddToOfflineStat(phase.GetStatIndex(killStat), 1d, e.Instigator, e.KillerTeam);
        }
    }

    [EventListener(RequireActiveLayout = true)]
    void IEventListener<VehicleExploded>.HandleEvent(VehicleExploded e, IServiceProvider serviceProvider)
    {
        WarfareVehicleInfo vehicleInfo = e.Vehicle.Info;
        CSteamID instigator = e.InstigatorId;
        if (vehicleInfo == null || instigator.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
        {
            return;
        }

        bool isAircraft = vehicleInfo.Type.IsAircraft(),
             isGround = vehicleInfo.Type.IsGroundVehicle();

        string? stat = isAircraft ? KnownStatNames.AirVehicleKills : isGround ? KnownStatNames.GroundVehicleKills : null;
        if (e.Instigator != null && e.Instigator.Team == e.InstigatorTeam)
        {
            PlayerGameStatsComponent? comp = e.Instigator.ComponentOrNull<PlayerGameStatsComponent>();
            if (comp != null)
            {
                comp.AddToStat(KnownStatNames.VehicleKills, 1);
                comp.AddToStat(stat, 1);
            }
        }
        else if (e.InstigatorTeam is { IsValid: true })
        {
            LeaderboardPhase? phase = GetPhase();
            if (phase != null)
            {
                phase.AddToOfflineStat(phase.GetStatIndex(KnownStatNames.VehicleKills), 1d, instigator, e.InstigatorTeam);
                phase.AddToOfflineStat(phase.GetStatIndex(stat), 1d, instigator, e.InstigatorTeam);
            }
        }
    }

    [EventListener(RequireActiveLayout = true)]
    void IEventListener<FlagCaptured>.HandleEvent(FlagCaptured e, IServiceProvider serviceProvider)
    {
        LeaderboardPhase? phase = GetPhase();
        if (phase == null)
            return;

        int statIndex = phase.GetStatIndex(KnownStatNames.Objectives);
        foreach (WarfarePlayer player in e.Flag.Players)
        {
            if (!player.Team.IsFriendly(e.Capturer))
                continue;

            player.ComponentOrNull<PlayerGameStatsComponent>()?.AddToStat(statIndex, 1);
        }
    }
}
