using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Disables crafting other than ammo or repairs.
/// </summary>
internal sealed class NoCraftingTweak : IEventListener<CraftItemRequested>
{
    void IEventListener<CraftItemRequested>.HandleEvent(CraftItemRequested e, IServiceProvider serviceProvider)
    {
        if (e.Blueprint.type is EBlueprintType.AMMO or EBlueprintType.REPAIR)
            return;

        e.CancelAction();
    }
}