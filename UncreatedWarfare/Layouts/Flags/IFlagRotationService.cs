using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Flags;
public interface IFlagRotationService : IFlagListUIProvider
{
    IReadOnlyList<FlagObjective> ActiveFlags { get; }
    IEnumerable<FlagObjective> EnumerateObjectives();
    FlagObjective? GetObjective(Team team);
    ElectricalGridBehaivor GridBehaivor { get; }
}

public interface IFlagListUIProvider
{
    IEnumerable<FlagListUIEntry> EnumerateFlagListEntries(LanguageSet set);
}

public readonly struct FlagListUIEntry
{
    public readonly string Text;
    public readonly string Icon;
    public FlagListUIEntry(string text, FlagIcon icon)
    {
        Text = text;
        Icon = icon switch
        {
            FlagIcon.Attack => "<#ff8963>µ</color>",
            FlagIcon.Defend => "<#a962ff>´</color>",
            FlagIcon.Locked => "<#c2c2c2>²</color>",
            _ => ""
        };
    }
    public FlagListUIEntry(string text, string icon)
    {
        Text = text;
        Icon = icon;
    }
}

public enum FlagIcon : byte
{
    None,
    Attack,
    Defend,
    Locked
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