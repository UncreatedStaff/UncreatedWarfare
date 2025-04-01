using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Uncreated.Warfare.Util;

public static class StringUtility
{
    private const LevenshteinOptions IgnoreAny = (LevenshteinOptions)(1 << 1);

    /// <summary>
    /// Computes the number of edits required to turn one string to another.
    /// </summary>
    /// <remarks>Based on https://en.wikipedia.org/wiki/Levenshtein_distance#Iterative_with_two_matrix_rows.</remarks>
    public static unsafe int LevenshteinDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, CultureInfo formatProvider, LevenshteinOptions options = default)
    {
        if (a == b)
            return 0;

        fixed (char* lpA = a)
        fixed (char* lpB = b)
        {
            return LevenshteinDistance(lpA, a.Length, lpB, b.Length, formatProvider, options);
        }
    }

    /// <summary>
    /// Computes the number of edits required to turn one string to another.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <remarks>Based on https://en.wikipedia.org/wiki/Levenshtein_distance#Iterative_with_two_matrix_rows.</remarks>
    public static unsafe int LevenshteinDistance(string a, string b, CultureInfo formatProvider, LevenshteinOptions options = default)
    {
        if (a == null)
            throw new ArgumentNullException(nameof(a));
        if (b == null)
            throw new ArgumentNullException(nameof(b));

        if (ReferenceEquals(a, b) || a.Length == 0 && b.Length == 0)
            return 0;

        fixed (char* lpA = a)
        fixed (char* lpB = b)
        {
            return LevenshteinDistance(lpA, a.Length, lpB, b.Length, formatProvider, options);
        }
    }

    /// <summary>
    /// Computes the number of edits required to turn one string to another.
    /// </summary>
    /// <remarks>Based on https://en.wikipedia.org/wiki/Levenshtein_distance#Iterative_with_two_matrix_rows.</remarks>
    public static unsafe int LevenshteinDistance(char* a, int aChars, char* b, int bChars, CultureInfo formatProvider, LevenshteinOptions options = default)
    {
        if (a == b || aChars == 0 && bChars == 0)
        {
            return 0;
        }

        bool ignoreCase = (options & LevenshteinOptions.IgnoreCase) != 0;
        // remove trailing and leading ignored characters
        if ((options & IgnoreAny) != 0)
        {
            while (aChars > 0 && IsIgnored(a[aChars - 1], options)) { --aChars; }
            while (bChars > 0 && IsIgnored(b[bChars - 1], options)) { --bChars; }

            while (aChars > 0 && IsIgnored(a[0], options)) { ++a; --aChars; }
            while (bChars > 0 && IsIgnored(b[0], options)) { ++b; --bChars; }

            bool canUnIgnoreCase = false;
            if (aChars > 2)
            {
                int ct = 2;
                for (int i = 1; i < aChars - 1; ++i)
                {
                    if (!IsIgnored(a[i], options))
                        ++ct;
                }

                if (ct != aChars)
                {
                    char* newPtr = stackalloc char[ct];
                    int index = -1;
                    for (int i = 0; i < aChars; ++i)
                    {
                        char c = a[i];
                        if (!IsIgnored(c, options))
                        {
                            newPtr[++index] = ignoreCase ? char.ToLower(c, formatProvider) : c;
                        }
                    }

                    a = newPtr;
                    aChars = ct;
                    canUnIgnoreCase = true;
                }
            }

            if (bChars > 2)
            {
                int ct = 2;
                for (int i = 1; i < bChars - 1; ++i)
                {
                    if (!IsIgnored(b[i], options))
                        ++ct;
                }

                if (ct != bChars)
                {
                    char* newPtr = stackalloc char[ct];
                    int index = -1;
                    for (int i = 0; i < bChars; ++i)
                    {
                        char c = b[i];
                        if (!IsIgnored(c, options))
                            newPtr[++index] = ignoreCase ? char.ToLower(c, formatProvider) : c;
                    }

                    b = newPtr;
                    bChars = ct;
                    if (canUnIgnoreCase)
                        ignoreCase = false;
                }
            }
        }

        if (aChars == 0)
            return bChars;
        if (bChars == 0)
            return aChars;

        int* prev = stackalloc int[bChars + 1];
        int* curr = stackalloc int[bChars + 1];

        for (int i = 0; i <= bChars; ++i)
            prev[i] = i;

        bool autocomplete = (options & LevenshteinOptions.AutoComplete) != 0 && aChars > bChars;
        for (int i = 0; i < aChars; ++i)
        {
            curr[0] = i + 1;

            for (int j = 0; j < bChars; ++j)
            {
                int deletionCost = prev[j + 1] + 1;
                int insertionCost = curr[j] + 1;

                bool isDifferent = ignoreCase
                    ? char.ToLower(a[i], formatProvider) != char.ToLower(b[j], formatProvider)
                    : a[i] != b[j];

                int substitutionCost = (isDifferent ? 1 : 0) + prev[j];

                if (autocomplete && j == bChars - 1 && deletionCost < insertionCost && deletionCost < substitutionCost)
                {
                    return prev[bChars];
                }

                curr[j + 1] = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);
            }

            int* temp = prev;
            prev = curr;
            curr = temp;
        }

        return prev[bChars];
    }

    private static bool IsIgnored(char c, LevenshteinOptions options)
    {
        if ((options & LevenshteinOptions.IgnoreWhitespace) == LevenshteinOptions.IgnoreWhitespace)
        {
            if (char.IsWhiteSpace(c))
                return true;
        }
        if ((options & LevenshteinOptions.IgnorePunctuation) == LevenshteinOptions.IgnorePunctuation)
        {
            if (char.IsPunctuation(c))
                return true;
        }

        return c == '\0';
    }

    /// <summary>
    /// Truncates a string if it's over a certain <paramref name="length"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(str))]
    public static string? Truncate(this string? str, int length)
    {
        if (str is null)
            return null;

        return str.Length <= length ? str : str[..length];
    }

    /// <summary>
    /// Truncates a string if it's over a certain <paramref name="length"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(str))]
    public static string? TruncateWithEllipses(this string? str, int length)
    {
        if (str is null || str.Length <= length)
            return str;

        if (length <= 3)
            return new string('.', length);

        return string.Create(length, str, (span, state) =>
        {
            state.AsSpan(0, span.Length - 3).CopyTo(span);
            span.Slice(span.Length - 3, 3).Fill('.');
        });
    }

    /// <summary>
    /// Truncates <paramref name="text"/> so that it's UTF-8 byte count is less than or equal to <paramref name="maximumBytes"/>.
    /// </summary>
    /// <param name="byteLength">The length in UTF-8 bytes of the truncated text.</param>
    public static ReadOnlySpan<char> TruncateUtf8Bytes(ReadOnlySpan<char> text, int maximumBytes, out int byteLength)
    {
        if (maximumBytes < 0)
        {
            byteLength = Encoding.UTF8.GetByteCount(text);
            return text;
        }

        if (maximumBytes == 0)
        {
            byteLength = 0;
            return default;
        }

        int byteCt = Encoding.UTF8.GetByteCount(text);
        if (byteCt <= maximumBytes)
        {
            byteLength = byteCt;
            return text;
        }

        Encoder encoder = Encoding.UTF8.GetEncoder();
        byte[] buffer = new byte[maximumBytes];
        encoder.Convert(text, buffer, false, out int charsUsed, out byteLength, out _);
        return text.Slice(0, charsUsed);
    }

    /// <summary>
    /// Remove any occurances of any character in <paramref name="replaceables"/> from <paramref name="source"/> and return it as a <see cref="string"/>.
    /// </summary>
    /// <remarks>If <paramref name="source"/> is <see langword="null"/>, it will just return <see langword="null"/>.</remarks>
    [return: NotNullIfNotNull(nameof(source))]
    public static string? RemoveMany(string? source, bool caseSensitive, params ReadOnlySpan<char> replaceables)
    {
        if (source == null)
            return null;

        if (replaceables.Length == 0)
            return source;

        return source.Length == 0 ? string.Empty : RemoveManyIntl(source, caseSensitive, replaceables, source);
    }

    /// <summary>
    /// Remove any occurances of any character in <paramref name="replaceables"/> from <paramref name="source"/> and return it as a <see cref="string"/>.
    /// </summary>
    public static string RemoveMany(ReadOnlySpan<char> source, bool caseSensitive, params ReadOnlySpan<char> replaceables)
    {
        if (replaceables.Length == 0)
            return new string(source);

        return source.Length == 0 ? string.Empty : RemoveManyIntl(source, caseSensitive, replaceables, null);
    }

#pragma warning disable CS8500
    private static unsafe string RemoveManyIntl(ReadOnlySpan<char> source, bool caseSensitive, ReadOnlySpan<char> replaceables, string? noOpReturn)
    {
        int occurances = 0;
        int lastIndex = -1;
        while (lastIndex + 1 < source.Length)
        {
            int nextIndex = GetNextIndex(caseSensitive, source, lastIndex, replaceables);

            if (nextIndex < 0)
                break;

            lastIndex += nextIndex + 1;
            ++occurances;
        }

        if (occurances == 0)
            return noOpReturn ?? new string(source);

        int newLength = source.Length - occurances;

        if (newLength == 0)
            return string.Empty;

        CreateState state = default;
        state.CaseSensitive = caseSensitive;
        state.Replaceables = &replaceables;
        state.Source = &source;

        return string.Create(newLength, state, static (span, state) =>
        {
            ReadOnlySpan<char> source = *state.Source;
            ReadOnlySpan<char> replaceables = *state.Replaceables;

            int stringIndex = 0;
            while (source.Length > 0)
            {
                int nextIndex = GetNextIndex(state.CaseSensitive, source, -1, replaceables);

                if (nextIndex < 0)
                    break;

                if (nextIndex != 0)
                    source.Slice(0, nextIndex).CopyTo(span.Slice(stringIndex));
                stringIndex += nextIndex;

                if (nextIndex + 1 < source.Length)
                    source = source.Slice(nextIndex + 1);
                else
                    source = default;
            }

            if (source.Length > 0)
            {
                source.CopyTo(span.Slice(stringIndex));
            }
        });

        static int GetNextIndex(bool caseSensitive, ReadOnlySpan<char> source, int lastIndex, ReadOnlySpan<char> replaceables)
        {
            if (caseSensitive)
            {
                return source.Slice(lastIndex + 1).IndexOfAny(replaceables);
            }

            for (int i = lastIndex + 1; i < source.Length; ++i)
            {
                if (replaceables.IndexOf(source.Slice(i, 1), StringComparison.InvariantCultureIgnoreCase) < 0)
                    continue;

                return i - (lastIndex + 1);
            }

            return -1;
        }
    }

    public unsafe struct CreateState
    {
        public bool CaseSensitive;
        public ReadOnlySpan<char>* Source;
        public ReadOnlySpan<char>* Replaceables;
    }

#pragma warning restore CS8500

}

[Flags]
public enum LevenshteinOptions
{
    /// <summary>
    /// Ignores the casing of the two strings when comparing characters.
    /// </summary>
    IgnoreCase = 1,

    /// <summary>
    /// Ignores any characters that fall under <see cref="char.IsWhiteSpace(char)"/>.
    /// </summary>
    IgnoreWhitespace = (1 << 1) | (1 << 2),

    /// <summary>
    /// Ignores any characters that fall under <see cref="char.IsPunctuation(char)"/>.
    /// </summary>
    IgnorePunctuation = (1 << 1) | (1 << 3),

    /// <summary>
    /// A will be treated as a value being searched for by B. Treats extra letters after B as untyped instead of missing (they don't count towards the distance).
    /// </summary>
    AutoComplete = 1 << 8
}