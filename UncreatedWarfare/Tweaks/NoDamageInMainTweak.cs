using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Disables crafting other than ammo or repairs.
/// </summary>
internal sealed class NoDamageInMainTweak(ZoneStore zoneStore) : IEventListener<DamagePlayerRequested>
{
    void IEventListener<DamagePlayerRequested>.HandleEvent(DamagePlayerRequested e, IServiceProvider serviceProvider)
    {
        if (e.Parameters.cause != EDeathCause.KILL && zoneStore.IsInMainBase(e.Player))
            e.Cancel();
    }
}