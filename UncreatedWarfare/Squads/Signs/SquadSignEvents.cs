using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Squads.UI;

namespace Uncreated.Warfare.Squads.Signs;

public class SquadSignEvents : 
    IEventListener<SquadCreated>,
    IEventListener<SquadDisbanded>,
    IEventListener<SquadMemberJoined>,
    IEventListener<SquadMemberLeft>,
    IEventListener<SquadLeaderUpdated>,
    IRequestHandler<SquadSignInstanceProvider, Squad>
{
    private readonly SignInstancer _signInstancer;
    private readonly SquadManager _squadManager;
    private readonly SquadMenuUI _squadMenu;

    public SquadSignEvents(SignInstancer signInstancer, SquadManager squadManager, SquadMenuUI squadMenu)
    {
        _signInstancer = signInstancer;
        _squadManager = squadManager;
        _squadMenu = squadMenu;
    }

    /// <inheritdoc />
    public Task<bool> RequestAsync(WarfarePlayer player,
        [NotNullWhen(true)] SquadSignInstanceProvider? requestable,
        IRequestResultHandler resultHandler,
        CancellationToken token = default)
    {
        if (requestable == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return Task.FromResult(false);
        }

        Squad? squad = _squadManager.Squads.FirstOrDefault(x => x.Team == requestable.Team && x.TeamIdentificationNumber == requestable.SquadNumber);
        if (squad == null || squad.Team != player.Team)
        {
            _squadMenu.OpenUI(player);
            return Task.FromResult(false);
        }

        if (player.IsInSquad())
        {
            _squadMenu.OpenUI(player);
            return Task.FromResult(false);
        }

        squad.TryAddMember(player);
        return Task.FromResult(true);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadCreated e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantTeam(e.Squad.Team);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadDisbanded e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantTeam(e.Squad.Team);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantSquad(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        UpdateSignsForRelevantSquad(e.Squad);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
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