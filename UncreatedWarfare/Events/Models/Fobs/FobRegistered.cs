using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a FOB is registered.
/// </summary>
public class FobRegistered
{
    /// <summary>
    /// The FOB that was registered.
    /// </summary>
    public required IFob Fob { get; init; }
}
