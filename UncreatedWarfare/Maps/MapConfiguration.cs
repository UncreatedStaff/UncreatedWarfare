namespace Uncreated.Warfare.Maps;

/// <summary>
/// Configuration for each map in <see cref="MapScheduler"/>.
/// </summary>
public sealed class MapConfiguration
{
    /// <summary>
    /// Case-sensitive map folder name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Main workshop item.
    /// </summary>
    public required ulong WorkshopId { get; set; }

    /// <summary>
    /// External dependencies required for the map.
    /// </summary>
    public ulong[]? RequiredDependencies { get; set; }


}