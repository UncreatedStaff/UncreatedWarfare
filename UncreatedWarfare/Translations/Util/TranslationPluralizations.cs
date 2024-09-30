using System;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations.Util;
internal static class TranslationPluralizations
{
    /// <summary>
    /// Convert a word to it's plural form. Currently only supported in english but more languages could be added later.
    /// </summary>
    /// <remarks>
    /// Also supports some present tense verbs that get switched to past tense when pluralized.
    /// 'a ' or 'an ' (with the space) can be pluralized to an empty string to go from 'a apple' to 'apples', for example.
    /// </remarks>
    internal static string Pluralize(ReadOnlySpan<char> word, LanguageInfo language)
    {
        if (word.Length < 3)
            return new string(word);

        if (!language.Code.Equals(Languages.Languages.EnglishUS, StringComparison.OrdinalIgnoreCase))
        {
            return new string(word);
        }

        if (word.Equals("is", StringComparison.InvariantCulture))
            return "are";
        if (word.Equals("was", StringComparison.InvariantCulture))
            return "were";
        if (word.Equals("did", StringComparison.InvariantCulture))
            return "do";
        if (word.Equals("comes", StringComparison.InvariantCulture))
            return "come";
        if (word.Equals("it", StringComparison.InvariantCulture))
            return "they";
        if (word.Equals("a ", StringComparison.InvariantCulture) || word.Equals(" a", StringComparison.InvariantCulture))
            return string.Empty;
        if (word.Equals("an ", StringComparison.InvariantCulture) || word.Equals(" an", StringComparison.InvariantCulture))
            return string.Empty;

        // split input into words
        int size = word.Count(' ') + 1;

        Span<Range> ranges = stackalloc Range[size];

        ranges = ranges[..word.Split(ranges, ' ', true, true, StringSplitOptions.RemoveEmptyEntries)];

        bool hasOtherWords = ranges.Length > 1;
        ReadOnlySpan<char> otherWords = default;

        scoped ReadOnlySpan<char> str = hasOtherWords ? word[ranges[^1]] : word;

        if (str.Length < 2)
            return new string(word);

        if (hasOtherWords)
            otherWords = word.Slice(0, ranges[^1].Start.GetOffset(word.Length));

        bool isPCaps = char.IsUpper(str[0]);
        Span<char> lower = stackalloc char[str.Length];
        lower = lower[..str.ToLowerInvariant(lower)];
        str = lower;

        // exceptions
        if (str.Equals("child", StringComparison.OrdinalIgnoreCase))
            return word.Concat("ren");
        if (str.Equals("bunker", StringComparison.OrdinalIgnoreCase))
            return word.Concat("s");
        if (str.Equals("goose", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Geese" : "geese");
        if (str.Equals("wall", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Wall" : "wall");
        if (str.Equals("tooth", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Teeth" : "teeth");
        if (str.Equals("foot", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Feet" : "feet");
        if (str.Equals("mouse", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Mice" : "mice");
        if (str.Equals("die", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Dice" : "dice");
        if (str.Equals("person", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "People" : "people");
        if (str.Equals("axis", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Axes" : "axes");
        if (str.Equals("ammo", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Ammo" : "ammo");
        if (str.Equals("radio", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Radios" : "radios");
        if (str.Equals("mortar", StringComparison.OrdinalIgnoreCase))
            return otherWords.Concat(isPCaps ? "Mortars" : "mortars");

        if (str.EndsWith("man", StringComparison.OrdinalIgnoreCase))
            return str[..^2].Concat(char.IsUpper(str[^2]) ? "E" : "e") + str[^1];

        char last = str[^1];
        if (char.IsDigit(last))
            return word.Concat("s");

        char slast = str[^2];

        if (last is 's' or 'x' or 'z' || (last is 'h' && slast is 's' or 'c'))
            return word.Concat("es");

        if (str.Equals("roof", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("belief", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("chef", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("chief", StringComparison.OrdinalIgnoreCase)
           )
            goto justAddS;

        if (last is 'f')
            return word[..^1].Concat("ves");

        if (last is 'e' && slast is 'f')
            return word[..^2].Concat("ves");

        if (last is 'y')
            if (!(slast is 'a' or 'e' or 'i' or 'o' or 'u'))
                return word[..^1].Concat("ies");
            else goto justAddS;

        if (str.Equals("photo", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("piano", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("halo", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("volcano", StringComparison.OrdinalIgnoreCase)
           )
            goto justAddS;

        if (last is 'o')
            return word.Concat("es");

        if (last is 's' && slast is 'u')
            return word[..^2].Concat("i");

        if (last is 's' && slast is 'i')
            return word[..^2].Concat("es");

        // identity pluralizations
        if (str.Equals("sheep", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("series", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("species", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("moose", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("fish", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("swine", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("buffalo", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("shrimp", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("trout", StringComparison.OrdinalIgnoreCase) ||
            (str.EndsWith("craft", StringComparison.OrdinalIgnoreCase) && str.Length > 5) || // aircraft, etc
            str.Equals("deer", StringComparison.OrdinalIgnoreCase))
            return new string(word);

        justAddS:
        return word.Concat("s");
    }

    public static bool IsOne(object? obj) => obj is IConvertible conv && IsOne(conv);
    public static bool IsOne(IConvertible conv)
    {
        TypeCode tc = conv.GetTypeCode();
        return tc switch
        {
            TypeCode.Boolean => (bool)conv,
            TypeCode.Char => (char)conv == 1,
            TypeCode.SByte => (sbyte)conv == 1,
            TypeCode.Byte => (byte)conv == 1,
            TypeCode.Int16 => (short)conv == 1,
            TypeCode.UInt16 => (ushort)conv == 1,
            TypeCode.Int32 => (int)conv == 1,
            TypeCode.UInt32 => (uint)conv == 1,
            TypeCode.Int64 => (long)conv == 1,
            TypeCode.UInt64 => (ulong)conv == 1,
            TypeCode.Single => Math.Abs((float)conv - 1) <= float.Epsilon,
            TypeCode.Double => Math.Abs((double)conv - 1) <= double.Epsilon,
            TypeCode.Decimal => ((decimal)conv).Equals(1m),
            TypeCode.DateTime => ((DateTime)conv).Ticks == 1,
            TypeCode.String => ((string)conv).Equals("1", StringComparison.InvariantCultureIgnoreCase) ||
                               ((string)conv).Equals("one", StringComparison.InvariantCultureIgnoreCase),
            _ => false
        };
    }
}