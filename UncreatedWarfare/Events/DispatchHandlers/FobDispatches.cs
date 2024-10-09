using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    /// <summary>
    /// Invoked by <see cref="FobManager"/> when a FOB is registered.
    /// </summary>
    public void FobRegistered(IFob fob)
    {
        FobRegistered args = new FobRegistered
        {
            Fob = fob
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }
    /// <summary>
    /// Invoked by <see cref="FobManager"/> when a FOB is deregistered.
    /// </summary>
    public void FobDeregistered(IFob fob)
    {
        FobDeregistered args = new FobDeregistered
        {
            Fob = fob
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }
}