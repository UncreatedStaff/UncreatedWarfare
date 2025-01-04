using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a FOB is deregistered.
/// </summary>
public class FobDeregistered
{
    /// <summary>
    /// The FOB that was deregistered.
    /// </summary>
    public required IFob Fob { get; init; }
}
