using System;

namespace Uncreated.Warfare.Util;
public static class SpanExtensions
{
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    /// <summary>
    /// Concat 2 spans without allocating extra memory.
    /// </summary>
    public static unsafe string Concat(this ReadOnlySpan<char> span, ReadOnlySpan<char> span2)
    {
        Concat2SpanState state = default;
        state.Span1Ptr = &span;
        state.Span2Ptr = &span2;
        return string.Create(span.Length + span2.Length, state, (span, state) =>
        {
            state.Span1Ptr->CopyTo(span);
            state.Span2Ptr->CopyTo(span[state.Span1Ptr->Length..]);
        });
    }

    private unsafe struct Concat2SpanState
    {
        public ReadOnlySpan<char>* Span1Ptr;
        public ReadOnlySpan<char>* Span2Ptr;
        public char Combine;
    }

    /// <summary>
    /// Concat 3 spans without allocating extra memory.
    /// </summary>
    public static unsafe string Concat(this ReadOnlySpan<char> span, ReadOnlySpan<char> span2, ReadOnlySpan<char> span3)
    {
        Concat3SpanState state = default;
        state.Span1Ptr = &span;
        state.Span2Ptr = &span2;
        state.Span3Ptr = &span3;
        return string.Create(span.Length + span2.Length + span3.Length, state, (span, state) =>
        {
            state.Span1Ptr->CopyTo(span);
            int len1 = state.Span1Ptr->Length;
            state.Span2Ptr->CopyTo(span[len1..]);
            state.Span3Ptr->CopyTo(span[(len1 + state.Span2Ptr->Length)..]);
        });
    }

    /// <summary>
    /// Concat 3 spans without allocating extra memory.
    /// </summary>
    public static unsafe string Concat(this ReadOnlySpan<char> span, char span2, ReadOnlySpan<char> span3, ReadOnlySpan<char> span4)
    {
        Concat3SpanState state = default;
        state.Span1Ptr = &span;
        state.Span2Ptr = &span3;
        state.Span3Ptr = &span4;
        state.Combine = span2;
        return string.Create(span.Length + span3.Length + span4.Length + 1, state, (span, state) =>
        {
            state.Span1Ptr->CopyTo(span);
            int len1 = state.Span1Ptr->Length;
            span[len1] = state.Combine;
            ++len1;
            state.Span2Ptr->CopyTo(span[len1..]);
            state.Span3Ptr->CopyTo(span[(len1 + state.Span2Ptr->Length)..]);
        });
    }

    private unsafe struct Concat3SpanState
    {
        public ReadOnlySpan<char>* Span1Ptr;
        public ReadOnlySpan<char>* Span2Ptr;
        public ReadOnlySpan<char>* Span3Ptr;
        public char Combine;
    }

    /// <summary>
    /// Concat 2 spans without allocating extra memory.
    /// </summary>
    public static unsafe string Concat(this ReadOnlySpan<char> span, char combine, ReadOnlySpan<char> span2)
    {
        Concat2SpanState state = default;
        state.Span1Ptr = &span;
        state.Span2Ptr = &span2;
        state.Combine = combine;
        return string.Create(span.Length + span2.Length, state, (span, state) =>
        {
            state.Span1Ptr->CopyTo(span);
            int len1 = state.Span1Ptr->Length;
            span[len1] = state.Combine;
            state.Span2Ptr->CopyTo(span[(len1 + 1)..]);
        });
    }

    /// <summary>
    /// Concat a span and string without allocating extra memory.
    /// </summary>
    public static unsafe string Concat(this ReadOnlySpan<char> span, string str)
    {
        ConcatSpan2StringState state = default;
        state.Span = &span;
        state.String = str;
        return string.Create(span.Length + str.Length, state, (span, state) =>
        {
            state.Span->CopyTo(span);
            state.String.AsSpan().CopyTo(span[state.Span->Length..]);
        });
    }

    private unsafe struct ConcatSpan2StringState
    {
        public ReadOnlySpan<char>* Span;
        public string String;
    }

#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    /// <summary>
    /// Gets the index of an object in a span with an offset.
    /// </summary>
    public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value, int startIndex) where T : IEquatable<T>
    {
        int index = span[startIndex..].IndexOf(value);
        if (index < 0)
            return -1;
        return index + startIndex;
    }

    /// <summary>
    /// Gets the index of an object in a span with an offset.
    /// </summary>
    public static int IndexOf<T>(this ReadOnlySpan<T> span, T value, int startIndex) where T : IEquatable<T>
    {
        int index = span[startIndex..].IndexOf(value);
        if (index < 0)
            return -1;
        return index + startIndex;
    }

    /// <summary>
    /// Counts the number of occurences of <paramref name="value"/> in <paramref name="span"/>.
    /// </summary>
    public static int Count<T>(
#if !NET8_0_OR_GREATER
        this 
#endif
            ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
    {
        int amt = 0;
        int lastIndex = -value.Length;
        while ((lastIndex = span.IndexOf(value, lastIndex + value.Length)) >= 0)
        {
            ++amt;
            if (lastIndex + value.Length >= span.Length)
                break;
        }

        return amt;
    }

    /// <summary>
    /// Counts the number of occurences of <paramref name="value"/> in <paramref name="span"/>.
    /// </summary>
    public static int Count<T>(
#if !NET8_0_OR_GREATER
        this 
#endif
            ReadOnlySpan<T> span, T value) where T : IEquatable<T>
    {
        int amt = 0;
        int lastIndex = -1;
        while ((lastIndex = span.IndexOf(value, lastIndex + 1)) >= 0)
        {
            ++amt;
            if (lastIndex + 1 >= span.Length)
                break;
        }

        return amt;
    }

    /// <summary>
    /// Span implementation of <see cref="string.Split(char,StringSplitOptions)"/>. (this exists but only in .net 8+)
    /// </summary>
    /// <param name="trimOuter">Trim white-space off the input string before splitting.</param>
    /// <param name="trimEachEntry">Trim white-space off each split entry.</param>
    /// <remarks>Will return as many ranges as <paramref name="ranges"/> can fit. To get a maximum, use <c>span.Count(<paramref name="separator"/>) + 1</c>.</remarks>
    public static int Split(this ReadOnlySpan<char> span, Span<Range> ranges, char separator, bool trimOuter = false, bool trimEachEntry = false, StringSplitOptions options = StringSplitOptions.None)
    {
        int startIndex = 0;
        int endIndex = span.Length - 1;

        // higher .NET versions have a StringSplitOptions.TrimEntries = 2 value.
        trimEachEntry |= (options & (StringSplitOptions)2) != 0;

        if (trimOuter)
        {
            for (int i = 0; i < span.Length; ++i)
            {
                if (char.IsWhiteSpace(span[i]))
                    continue;

                startIndex = i;
                break;
            }

            for (int i = span.Length - 1; i >= 0; --i)
            {
                if (char.IsWhiteSpace(span[i]))
                    continue;

                endIndex = i;
                break;
            }
        }

        int rangeInd = -1;
        int startInd, endInd;
        int lastEndInd = startIndex - 1;
        for (int i = startIndex; i <= endIndex; ++i)
        {
            if (span[i] != separator)
                continue;

            startInd = lastEndInd + 1;
            endInd = i - 1;
            lastEndInd = i;

            if (startInd > endInd && (startInd != endInd + 1 || (options & StringSplitOptions.RemoveEmptyEntries) != 0))
                continue;

            if (!trimEachEntry)
            {
                if ((options & StringSplitOptions.RemoveEmptyEntries) != 0 && startInd == endInd + 1)
                    continue;

                if (ranges.Length <= rangeInd + 1)
                    return ranges.Length;

                ranges[++rangeInd] = new Range(new Index(startInd), new Index(endInd + 1));
                continue;
            }

            for (int j = startInd; j <= endInd; ++j)
            {
                if (char.IsWhiteSpace(span[j]))
                    continue;

                startInd = j;
                break;
            }

            for (int j = endInd; j >= startIndex; --j)
            {
                if (char.IsWhiteSpace(span[j]))
                    continue;

                endInd = j;
                break;
            }

            if (startInd > endInd && (startInd != endInd + 1 || (options & StringSplitOptions.RemoveEmptyEntries) != 0))
                continue;

            if (ranges.Length <= rangeInd + 1)
                return ranges.Length;

            ranges[++rangeInd] = new Range(new Index(startInd), new Index(endInd + 1));
        }

        startInd = lastEndInd + 1;
        endInd = endIndex;

        if (!trimEachEntry)
        {
            if ((options & StringSplitOptions.RemoveEmptyEntries) != 0 && startInd == endInd + 1)
                return rangeInd + 1;

            if (ranges.Length <= rangeInd + 1)
                return ranges.Length;

            ranges[++rangeInd] = new Range(new Index(startInd), new Index(endInd + 1));
            return rangeInd + 1;
        }

        for (int j = startInd; j <= endInd; ++j)
        {
            if (char.IsWhiteSpace(span[j]))
                continue;

            startInd = j;
            break;
        }

        for (int j = endInd; j >= startInd; --j)
        {
            if (char.IsWhiteSpace(span[j]))
                continue;

            endInd = j;
            break;
        }

        if (startInd > endInd && (startInd != endInd + 1 || (options & StringSplitOptions.RemoveEmptyEntries) != 0))
            return rangeInd + 1;

        if (ranges.Length <= rangeInd + 1)
            return ranges.Length;

        ranges[++rangeInd] = new Range(new Index(startInd), new Index(endInd + 1));
        return rangeInd + 1;
    }
}
