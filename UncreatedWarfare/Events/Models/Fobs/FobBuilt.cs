using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a <see cref="BuildableFob"/> is built up.
/// </summary>
public class FobBuilt
{
    /// <summary>
    /// The <see cref="BuildableFob"/> that was built up.
    /// </summary>
    public required BuildableFob Fob { get; init; }
}
