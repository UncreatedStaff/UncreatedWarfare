using Microsoft.Extensions.DependencyInjection;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.NewQuests.Templates;
using Uncreated.Warfare.NewQuests;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Layouts.Teams;
using System.Linq;

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
