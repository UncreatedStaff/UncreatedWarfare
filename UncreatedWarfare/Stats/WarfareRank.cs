using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Stats;

[RpcSerializable(SerializationHelper.MinimumStringSize * 2 + 8 * 2 + 4 * 2, isFixedSize: false)]
public class WarfareRank : ITranslationArgument, IRpcSerializable
{
    /// <summary>
    /// The name of the rank.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// An abbreviation for the name of the rank.
    /// </summary>
    public string Abbreviation { get; private set; }

    /// <summary>
    /// The experience to get from the this rank to the next rank.
    /// </summary>
    public double Experience { get; private set; }

    /// <summary>
    /// The total experience to get to this rank.
    /// </summary>
    public double CumulativeExperience { get; private set; }

    /// <summary>
    /// The zero-based index of this rank.
    /// </summary>
    public int RankIndex { get; private set; }

    /// <summary>
    /// The one-based level/index of this rank.
    /// </summary>
    public int Level { get; private set; }
    
    /// <summary>
    /// A reference to the next rank.
    /// </summary>
    public WarfareRank? Next { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

    /// <summary>
    /// A reference to the previous rank.
    /// </summary>
    public WarfareRank? Previous { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

    // for serialization
    public WarfareRank()
    {
        Name = string.Empty;
        Abbreviation = string.Empty;
    }

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

    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return 8 * 2 + 4 * 2 + serializer.GetSize(Name) + serializer.GetSize(Abbreviation);
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int index = 0;
        index += serializer.WriteObject(Name, writeTo);
        index += serializer.WriteObject(Abbreviation, writeTo[index..]);
        index += serializer.WriteObject(Experience, writeTo[index..]);
        index += serializer.WriteObject(CumulativeExperience, writeTo[index..]);
        index += serializer.WriteObject(RankIndex, writeTo[index..]);
        index += serializer.WriteObject(Level, writeTo[index..]);
        return index;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        int index = 0;
        Name = serializer.ReadObject<string>(readFrom, out int bytesRead) ?? string.Empty;
        index += bytesRead;

        Abbreviation = serializer.ReadObject<string>(readFrom[index..], out bytesRead) ?? string.Empty;
        index += bytesRead;

        Experience = serializer.ReadObject<double>(readFrom[index..], out bytesRead);
        index += bytesRead;

        CumulativeExperience = serializer.ReadObject<double>(readFrom[index..], out bytesRead);
        index += bytesRead;

        RankIndex = serializer.ReadObject<int>(readFrom[index..], out bytesRead);
        index += bytesRead;

        Level = serializer.ReadObject<int>(readFrom[index..], out bytesRead);
        index += bytesRead;

        return index;
    }
}
