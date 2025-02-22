using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Signs;

namespace Uncreated.Warfare.Squads.Signs;

public class SquadSignEvents : 
    IEventListener<SquadCreated>,
    IEventListener<SquadDisbanded>,
    IEventListener<SquadMemberJoined>,
    IEventListener<SquadMemberLeft>,
    IEventListener<SquadLeaderUpdated>
{
    private readonly SignInstancer _signInstancer;

    public SquadSignEvents(SignInstancer signInstancer)
    {
        _signInstancer = signInstancer;
    }
    public void HandleEvent(SquadCreated e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantTeam(e.Squad.Team);
    }

    public void HandleEvent(SquadDisbanded e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantTeam(e.Squad.Team);
    }

    public void HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantSquad(e.Squad);
    }

    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantSquad(e.Squad);
    }
    
    public void HandleEvent(SquadLeaderUpdated e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantSquad(e.Squad);
    }

    private void UpdateSignsForRelevantTeam(Team team)
    {
        _signInstancer.UpdateSigns<SquadSignInstanceProvider>((_, provider) => provider.Team == team);
    }
    private void UpdateSignsForRelevantSquad(Squad squad)
    {
        _signInstancer.UpdateSigns<SquadSignInstanceProvider>((_, provider) => provider.Team == squad.Team && provider.SquadNumber == squad.TeamIdentificationNumber);
    }
}