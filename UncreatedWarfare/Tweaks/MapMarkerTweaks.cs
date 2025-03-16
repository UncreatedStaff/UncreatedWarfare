using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Tweaks;

public class MapMarkerTweaks : IEventListener<PlayerDropMarkerRequested>
{
    public void HandleEvent(PlayerDropMarkerRequested e, IServiceProvider serviceProvider)
    {
        if (!e.Player.IsSquadLeader())
        {
            ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
            RangeCommandTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<RangeCommandTranslations>>().Value;
            chatService.Send(e.Player, translations.DropMarkerNotSquadleader);
            e.Cancel();
            return;
        }

        e.MarkerDisplayText = e.Player.GetSquad()?.Name ?? "Unknown Squad";
    }
}