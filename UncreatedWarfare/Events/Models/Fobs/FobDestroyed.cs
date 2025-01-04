using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after the main structure of any <see cref="IBuildableFob"/> subclass is destroyed.
/// </summary>
public class FobDestroyed
{
    /// <summary>
    /// The <see cref="IBuildableFob"/> that was destroyed.
    /// </summary>
    public required IBuildableFob Fob { get; init; }

    /// <summary>
    /// The event that caused the FOB to be destroyed.
    /// </summary>
    public required IBuildableDestroyedEvent Event { get; init; }
}
