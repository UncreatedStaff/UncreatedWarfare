using System;
using System.Collections.Generic;
using System.Text;

namespace Uncreated.Warfare.Events.Models.Items;

[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory" ])]
public class ItemPickupRequested : PlayerEvent
{

}
