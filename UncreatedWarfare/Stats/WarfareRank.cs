using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Stats;
public class WarfareRank : ITranslationArgument
{
    /// <summary>
    /// The name of the rank.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// An abbreviation for the name of the rank.
    /// </summary>
    public string Abbreviation { get; }

    /// <summary>
    /// The experience to get from the this rank to the next rank.
    /// </summary>
    public double Experience { get; }

    /// <summary>
    /// The total experience to get to this rank.
    /// </summary>
    public double CumulativeExperience { get; }

    /// <summary>
    /// The zero-based index of this rank.
    /// </summary>
    public int RankIndex { get; }

    /// <summary>
    /// The one-based level/index of this rank.
    /// </summary>
    public int Level { get; }
    
    /// <summary>
    /// A reference to the next rank.
    /// </summary>
    public WarfareRank? Next { get; }

    /// <summary>
    /// A reference to the previous rank.
    /// </summary>
    public WarfareRank? Previous { get; }

    internal WarfareRank(WarfareRank? previous, IEnumerator<IConfigurationSection> configEnumerator, int index, ref int ct)
    {
        ++ct;
        IConfigurationSection section = configEnumerator.Current!;

        RankIndex = index;
        Level = index + 1;
        Name = section.GetValue<string>("Name") ?? throw new InvalidOperationException($"Rank {index} missing Name.");
        Abbreviation = section.GetValue<string>("Abbreviation") ?? throw new InvalidOperationException($"Rank {index} missing Abbreviation.");
        Experience = section.GetValue("Experience", 0d);
        if (Experience < 0d)
        {
            throw new InvalidOperationException($"Rank {index} has invalid Experience.");
        }

        Previous = previous;
        Next = configEnumerator.MoveNext() ? new WarfareRank(this, configEnumerator, index, ref ct) : null;

        CumulativeExperience = Previous != null ? Previous.CumulativeExperience + Previous.Experience : 0;
        if (Experience == 0d && Next != null)
        {
            throw new InvalidOperationException($"Rank {index} has missing or invalid Experience.");
        }
    }

    /// <summary>
    /// Calculates the progress that a player with a certain amount of total <paramref name="xp"/> would be to getting to the next rank.
    /// </summary>
    /// <returns>An unclamped percentage on the [0, 1] scale.</returns>
    public double GetProgress(double xp)
    {
        return (xp - CumulativeExperience) / Experience;
    }


    public static readonly SpecialFormat FormatNumeric = new SpecialFormat("Numeric", "x");

    public static readonly SpecialFormat FormatLPrefixedNumeric = new SpecialFormat("L-Prefixed Numeric", "Lx");

    public static readonly SpecialFormat FormatAbbreviation = new SpecialFormat("Abbreviation", "a");

    public static readonly SpecialFormat FormatName = new SpecialFormat("Name", "n");


    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (FormatNumeric.Match(in parameters))
            return Level.ToString(parameters.Culture);

        if (FormatLPrefixedNumeric.Match(in parameters))
            return "L " + Level.ToString(parameters.Culture);

        if (FormatAbbreviation.Match(in parameters))
            return Abbreviation;

        return Name;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"L{Level} - {Name} ({Abbreviation})";
    }
}
