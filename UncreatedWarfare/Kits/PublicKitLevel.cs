using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Uniquely identifies a type of kit class and level, such as "Rifleman 1".
/// </summary>
/// <param name="Class">The class of the kit.</param>
/// <param name="Level">The one-based level of the kit.</param>
public readonly record struct PublicKitLevel(Class Class, int Level)
{
    /// <summary>
    /// Check whether the given <paramref name="kit"/> matches this level.
    /// </summary>
    public bool AppliesTo(Kit kit)
    {
        return kit.Class == Class && AppliesTo(kit.Id);
    }

    /// <summary>
    /// Check whether the given <paramref name="kitId"/> matches this level.
    /// </summary>
    public bool AppliesTo(string kitId)
    {
        ReadOnlySpan<char> digits = SliceDigitsFromKitId(kitId);

        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int level) || level != Level)
            return false;

        string? abbr = Class.GetKitIdAbbreviation();
        if (abbr == null)
            return false;

        ReadOnlySpan<char> remainder = kitId.AsSpan(0, kitId.Length - digits.Length);
        if (!remainder.EndsWith(abbr, StringComparison.OrdinalIgnoreCase))
            return false;

        if (Class == Class.AutomaticRifleman)
        {
            // special case for 'mar' since it also ends with 'ar'.
            return !remainder.EndsWith(Class.Marksman.GetKitIdAbbreviation());
        }

        return true;
    }

    /// <summary>
    /// Parse a <see cref="PublicKitLevel"/> from a kit ID.
    /// </summary>
    public static bool TryParseFromKitId([NotNullWhen(true)] string? kitId, out PublicKitLevel lvl)
    {
        Unsafe.SkipInit(out lvl);

        if (kitId == null)
            return false;

        ReadOnlySpan<char> digits = SliceDigitsFromKitId(kitId);

        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int level) || level == 0)
            return false;

        if (!ClassExtensions.TryGetClassFromKitIdAbbreviation(kitId.AsSpan(0, kitId.Length - digits.Length), out Class @class))
            return false;

        lvl = new PublicKitLevel(@class, level);
        return true;
    }
    /// <summary>
    /// Create a <see cref="PublicKitLevel"/> from an existing kit.
    /// </summary>
    /// <exception cref="ArgumentException">Not a valid public kit following the <c>[faction][class][number]</c> pattern, such as <c>usrif1</c>.</exception>
    /// <exception cref="ArgumentNullException"/>
    public static PublicKitLevel FromKit(Kit kit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));

        if (kit.Type != KitType.Public)
            throw new ArgumentException("Expected a public kit.", nameof(kit));

        Class @class = kit.Class;
        string? abbreviation = @class.GetKitIdAbbreviation();
        if (abbreviation == null)
            throw new ArgumentException("Expected a kit with a normal class.", nameof(kit));

        ReadOnlySpan<char> digits = SliceDigitsFromKitId(kit.Id);

        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int level) || level == 0)
            throw new ArgumentException("Unknown level.", nameof(kit));

        return new PublicKitLevel(@class, level);
    }

    private static ReadOnlySpan<char> SliceDigitsFromKitId(string kitId)
    {
        ReadOnlySpan<char> digits = kitId.AsSpan();
        for (int i = digits.Length - 1; i >= 0; --i)
        {
            if (char.IsDigit(digits[i]))
                continue;

            digits = digits.Slice(i + 1);
            break;
        }

        return digits;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (int)Class * 4 + Level;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Class} {Level}";
    }
}