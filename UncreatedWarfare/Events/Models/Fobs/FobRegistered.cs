using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after an <see cref="IFob"/> is registered. Includes other types of fobs (including Rally Points)
/// </summary>
public class FobRegistered
{
    /// <summary>
    /// The FOB that was registered.
    /// </summary>
    public required IFob Fob { get; init; }
}
