using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after the main structure of any <see cref="BasePlayableFob"/> subclass is destroyed.
/// </summary>
public class FobDestroyed
{
    /// <summary>
    /// The <see cref="BasePlayableFob"/> that was destroyed.
    /// </summary>
    public required BasePlayableFob Fob { get; init; }
}
