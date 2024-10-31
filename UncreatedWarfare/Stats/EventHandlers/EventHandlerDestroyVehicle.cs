using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Stats.EventHandlers;

internal class EventHandlerDestroyVehicle : IAsyncEventListener<VehicleExploded>
{
    private readonly PointsService _points;
    private readonly VehicleInfoStore _vehicleInfo;
    private readonly PointsTranslations _translations;

    public EventHandlerDestroyVehicle(PointsService points, VehicleInfoStore vehicleInfo, TranslationInjection<PointsTranslations> translations)
    {
        _points = points;
        _vehicleInfo = vehicleInfo;
        _translations = translations.Value;
    }

    [EventListener(Priority = int.MinValue)]
    public async UniTask HandleEventAsync(VehicleExploded e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        WarfareVehicleInfo? vehicleInfo = _vehicleInfo.GetVehicleInfo(e.Vehicle.asset);

        if (vehicleInfo == null || vehicleInfo.Type <= 0)
            return;

        uint faction = e.InstigatorTeam?.Faction.PrimaryKey ?? 0;

        if (faction == 0)
            return;

        CSteamID instigator = e.InstigatorId;

        if (e.InstigatorTeam!.GroupId.m_SteamID == e.Team)
        {
            EventInfo @event = _points.GetEvent("DestroyFriendlyVehicle:" + vehicleInfo.Type);

            Translation translation = vehicleInfo.Type.IsAircraft()
                ? _translations.XPToastAircraftDestroyed
                : _translations.XPToastVehicleDestroyed;

            await _points.ApplyEvent(instigator, faction, @event.Resolve().WithTranslation(translation, e.Instigator), token).ConfigureAwait(false);
        }
        else
        {
            EventInfo @event = _points.GetEvent("DestroyEnemyVehicle:" + vehicleInfo.Type);

            Translation translation = vehicleInfo.Type.IsAircraft()
                ? _translations.XPToastAircraftDestroyed
                : _translations.XPToastVehicleDestroyed;

            await _points.ApplyEvent(instigator, faction, @event.Resolve().WithTranslation(translation, e.Instigator), token).ConfigureAwait(false);
        }
    }
}