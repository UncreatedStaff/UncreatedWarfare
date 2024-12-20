using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Flags;
public interface IFlagRotationService
{
    IReadOnlyList<FlagObjective> ActiveFlags { get; }
    IEnumerable<FlagObjective> EnumerateObjectives();
    FlagObjective? GetObjective(Team team);
    ElectricalGridBehaivor GridBehaivor { get; }
}

public enum ElectricalGridBehaivor : byte
{
    /// <summary>
    /// The electrical grid is not used.
    /// </summary>
    Disabled,

    /// <summary>
    /// All objects are able to be used.
    /// </summary>
    AllEnabled,

    /// <summary>
    /// All objects connected to the objective are able to be used.
    /// </summary>
    EnabledWhenObjective,

    /// <summary>
    /// All objects connected to a flag in rotation are able to be used.
    /// </summary>
    EnabledWhenInRotation
}