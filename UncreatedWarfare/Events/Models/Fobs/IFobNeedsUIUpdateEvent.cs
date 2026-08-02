using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Invokes a fob UI update for this FOB.
/// </summary>
public interface IFobNeedsUIUpdateEvent
{
    IFob? Fob { get; }
}