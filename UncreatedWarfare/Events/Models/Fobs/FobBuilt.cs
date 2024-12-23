using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a <see cref="BunkerFob"/> is built up.
/// </summary>
public class FobBuilt
{
    /// <summary>
    /// The <see cref="BunkerFob"/> that was built up.
    /// </summary>
    public required BunkerFob Fob { get; init; }
}
