using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Tweaks;

public class MapMarkerTweaks : IEventListener<PlayerDropMarkerRequested>, IEventListener<SquadMemberLeft>, IEventListener<SquadLeaderUpdated>
{
    public void HandleEvent(PlayerDropMarkerRequested e, IServiceProvider serviceProvider)
    {
        if (e.Player.IsOnDuty || !e.IsNewMarkerBeingPlaced)
            return;

        if (!e.Player.IsSquadLeader())
        {
            ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
            RangeCommandTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<RangeCommandTranslations>>().Value;
            chatService.Send(e.Player, translations.DropMarkerNotSquadleader);
            e.Cancel();
            return;
        }

        e.MarkerDisplayText = e.Player.GetSquad()!.Name;
    }

    [EventListener(MustRunInstantly = true)]
    public void HandleEvent(SquadLeaderUpdated e, IServiceProvider serviceProvider)
    {
        if (!e.OldLeader.IsOnline
            || !e.OldLeader.UnturnedPlayer.quests.isMarkerPlaced
            || e.OldLeader.UnturnedPlayer.quests.markerTextOverride == null)
        {
            return;
        }

        // transition old marker to new leader
        Vector3 oldMarker = e.OldLeader.UnturnedPlayer.quests.markerPosition;
        e.OldLeader.UnturnedPlayer.quests.replicateSetMarker(false, Vector3.zero);
        e.NewLeader.UnturnedPlayer.quests.replicateSetMarker(true, oldMarker, e.Squad.Name);
    }

    [EventListener(MustRunInstantly = true)]
    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        // remove marker from old leader
        if (e.Player.IsOnline && e.Player.UnturnedPlayer.quests.isMarkerPlaced && e.Player.UnturnedPlayer.quests.markerTextOverride != null)
        {
            e.Player.UnturnedPlayer.quests.replicateSetMarker(false, Vector3.zero);
        }
    }
}