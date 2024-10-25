using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Squads;
public partial class SquadManager : ILayoutHostedService
{
    private readonly SquadConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly TranslationInjection<SquadTranslations> _translations;
    private readonly ChatService _chatService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SquadManager> _logger;
    private readonly TrackingList<Squad> _squads;

    /// <summary>
    /// List of all active Squads in the current game.
    /// </summary>
    public ReadOnlyTrackingList<Squad> Squads { get; }
    public UniTask StartAsync(CancellationToken token) => UniTask.CompletedTask;

    public UniTask StopAsync(CancellationToken token) => UniTask.CompletedTask;
    public SquadManager(IServiceProvider serviceProvider, ILogger<SquadManager> logger)
    {
        _configuration = serviceProvider.GetRequiredService<SquadConfiguration>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<SquadTranslations>>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _serviceProvider = serviceProvider;
        _logger = logger;
        _squads = new TrackingList<Squad>(16);
        Squads = new ReadOnlyTrackingList<Squad>(_squads);
    }
    public Squad CreateSquad(WarfarePlayer squadLeader, string squadName)
    {
        Squad squad = new Squad(squadLeader, squadName);
        _squads.Add(squad);
        _logger.LogDebug("Created new Squad: " + squad);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadCreated { Squad = squad });
        return squad;
    }
    public bool DisbandSquad(Squad squad)
    {
        Squad? existing = _squads.FindAndRemove(s => s == squad);
        if (existing == null)
            return false;

        existing.DisbandMembers();

        _logger.LogDebug("Disbanded squad: " + squad);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadDisbanded { Squad = squad });
        return true;
    }
    public bool AddMemberToSquad(WarfarePlayer player, Squad squad) => squad.AddMember(player);
    public bool RemoveMemberFromSquad(WarfarePlayer player, Squad squad)
    {
        if (!squad.RemoveMember(player))
            return false;

        if (squad.Members.Count == 0)
            DisbandSquad(squad);

        return true;
    }
}
