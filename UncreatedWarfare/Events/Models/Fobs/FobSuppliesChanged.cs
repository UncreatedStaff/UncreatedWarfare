using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.SupplyCrates;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after supplies are added or removed from a <see cref="IResourceFob"/>.
/// </summary>
internal class FobSuppliesChanged
{
    /// <summary>
    /// The <see cref="IResourceFob"/> where supplies were added or removed.
    /// </summary>
    public required IResourceFob Fob { get; init; }
    /// <summary>
    /// The number of supplies that were added (positive) or removed (negative).
    /// </summary>
    public required int AmountDelta { get; init; }
    /// <summary>
    /// The type of supplies that were added or removed.
    /// </summary>
    public required SupplyType SupplyType { get; init; }
    /// <summary>
    /// The reason the supplies were added or removed.
    /// </summary>
    public required SupplyChangeReason ChangeReason { get; init; }
}
