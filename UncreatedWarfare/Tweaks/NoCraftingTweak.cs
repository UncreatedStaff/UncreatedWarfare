using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Disables crafting other than ammo or repairs.
/// </summary>
internal sealed class NoCraftingTweak : IEventListener<CraftItemRequested>
{
    private static readonly Guid AmmoTagGuid = new Guid("d739926736374e5ba34b4ac6ffbb5c8f");
    private static readonly Guid RepairTagGuid = new Guid("732ee6ffeb18418985cf4f9fde33dd11");

    void IEventListener<CraftItemRequested>.HandleEvent(CraftItemRequested e, IServiceProvider serviceProvider)
    {
        TagAsset tag = e.Blueprint.GetCategoryTag();
        if (tag.GUID == AmmoTagGuid || tag.GUID == RepairTagGuid)
            return;

        e.CancelAction();
    }
}