using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Players.Events;
public class PlayerEvents : IEventListener<GroupChanged>, IEventListener<PlayerJoined>
{
    PlayerEvents? p;

    public void HandleEvent(GroupChanged e, IServiceProvider serviceProvider)
    {
        IReadOnlyList<Team> possibleTeams = serviceProvider.GetService<Layout>()?.TeamManager?.AllTeams ?? new List<Team>();

        Team newTeam = possibleTeams.FirstOrDefault(f => f.GroupId.m_SteamID == e.NewGroup) ?? Team.NoTeam;

        

    }

    public void HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}
