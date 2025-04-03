using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Vehicles.Events;

internal class VehicleLockRequestedHandler : IEventListener<ChangeVehicleLockRequested>
{
    private readonly ChatService _chatService;
    private readonly PlayersTranslations _translations;

    public VehicleLockRequestedHandler(ChatService chatService, TranslationInjection<PlayersTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;
    }

    void IEventListener<ChangeVehicleLockRequested>.HandleEvent(ChangeVehicleLockRequested e, IServiceProvider serviceProvider)
    {
        if (e.IsLocking || e.Vehicle.Vehicle.isDead || e.Player.IsOnDuty)
            return;

        // unlocking

        _chatService.Send(e.Player, _translations.UnlockVehicleNotAllowed);
        e.Cancel();
    }
}