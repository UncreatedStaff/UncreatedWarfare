using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Layouts;

namespace Uncreated.Warfare.Kits.Items;

/// <summary>
/// Helps keep up with where items have been moved to track held item's back to their original kit item.
/// </summary>
internal class ItemTrackingPlayerComponent : IPlayerComponent, IEventListener<ItemDropped>
{
    private KitManager _kitManager = null!;
    internal List<ItemTransformation> ItemTransformations = new List<ItemTransformation>(16);
    internal List<ItemDropTransformation> ItemDropTransformations = new List<ItemDropTransformation>(16);
    public WarfarePlayer Player { get; private set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
    }

    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item != null)
        {
            ItemDropTransformations.Add(new ItemDropTransformation(e.OldPage, e.OldX, e.OldY, e.Item));
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
