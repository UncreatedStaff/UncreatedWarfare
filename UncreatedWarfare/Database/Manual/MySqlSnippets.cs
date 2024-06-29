﻿using System;
using System.Globalization;
using System.Text;

namespace Uncreated.Warfare.Database.Manual;
public static class MySqlSnippets
{
    /// <summary>
    /// Check if the current time is within <paramref name="colDuration"/> after <paramref name="colTime"/>. A -1 duration assumes permanent. 
    /// </summary>
    /// <param name="tableDurations">Table or alias of the table with the duration column.</param>
    /// <param name="tableTime">Table or alias of the table with the time column.</param>
    /// <param name="colDuration">Column with an effective duration.</param>
    /// <param name="colTime">Column with a UTC timestamp.</param>
    public static string BuildCheckDurationClause(string tableDurations, string tableTime, string colDuration, string colTime) => $"(`{tableDurations}`.`{colDuration}` < 0 OR " +
        $"TIME_TO_SEC(TIMEDIFF(`{tableTime}`.`{colTime}`, UTC_TIMESTAMP())) * -1 < `{tableDurations}`.`{colDuration}`)";

    /// <summary>
    /// Check if the current time is within <paramref name="colDuration"/> after <paramref name="colTime"/> or <paramref name="colTimeFallback"/> if <paramref name="colTime"/> is <see cref="DBNull"/>. A -1 duration assumes permanent. 
    /// </summary>
    /// <param name="tableDurations">Table or alias of the table with the duration column.</param>
    /// <param name="tableTime">Table or alias of the table with the time column.</param>
    /// <param name="colDuration">Column with an effective duration.</param>
    /// <param name="colTime">Column with a UTC timestamp.</param>
    /// <param name="colTimeFallback">Column with a fallback UTC timestamp if <paramref name="colTime"/> is <see cref="DBNull"/>.</param>
    public static string BuildCheckDurationClause(string tableDurations, string tableTime, string colDuration, string colTime, string colTimeFallback) => $"(`{tableDurations}`.`{colDuration}` < 0 OR " +
        $"TIME_TO_SEC(TIMEDIFF(IF(`{tableTime}`.`{colTime}` IS NULL, `{tableTime}`.`{colTimeFallback}`, `{tableTime}`.`{colTime}`), UTC_TIMESTAMP())) * -1 < `{tableDurations}`.`{colDuration}`)";
    
    /// <summary>
    /// Returns a list of parameters formatted: <c>@0,@1,@2,...</c>.
    /// </summary>
    /// <param name="startIndex">First index.</param>
    /// <param name="length">Number of parameters.</param>
    public static string ParameterList(int startIndex, int length)
    {
        if (length == 0)
            return string.Empty;

        Span<char> newStr = stackalloc char[CountDigits((uint)(length + startIndex)) + (length * 2 - 1)];

        int index = 0;
        for (int i = 0; i < length; ++i)
        {
            if (i != 0)
            {
                newStr[index] = ',';
                ++index;
            }

            newStr[index] = '@';
            ((uint)(i + startIndex)).TryFormat(newStr.Slice(index + 1), out int charsWritten, "F0", CultureInfo.InvariantCulture);
            index += charsWritten + 1;
        }

        return new string(newStr.Slice(0, index));
    }

    /// <summary>
    /// Appends a list of parameters formatted: <c>@0,@1,@2,...</c>.
    /// </summary>
    /// <param name="startIndex">First index.</param>
    /// <param name="length">Number of parameters.</param>
    public static void AppendParameterList(StringBuilder builder, int startIndex, int length)
    {
        builder.EnsureCapacity(builder.Length + CountDigits((uint)(length + startIndex)) + (length * 2 - 1));
        for (int i = 0; i < length; ++i)
        {
            if (i != 0)
                builder.Append(',');
            builder.Append("@").Append(i + startIndex);
        }
    }

    /// <summary>
    /// Creates a list of columns to be updated from a parameter list.
    /// </summary>
    public static string ColumnUpdateList(int startIndex, params string[] p) => ColumnUpdateList(startIndex, 0, p);

    /// <summary>
    /// Creates a list of columns to be updated from a parameter list.
    /// </summary>
    public static string ColumnUpdateList(int startIndex, int skip, params string[] p)
    {
        if (p.Length == 0)
            return string.Empty;
        int ttlSize = p.Length - skip - 1;
        for (int i = skip; i < p.Length; ++i)
        {
            ttlSize += CountDigits((uint)(i - skip + startIndex)) + 4 + p[i].Length;
        }

        return string.Create(ttlSize, new ValueTuple<int, int, string[]>(startIndex, skip, p), (span, state) =>
        {
            int startIndex = state.Item1;
            int skip = state.Item2;
            string[] p = state.Item3;
            int index = 0;
            for (int i = skip; i < p.Length; ++i)
            {
                if (i != skip)
                {
                    span[index] = ',';
                    ++index;
                }

                ReadOnlySpan<char> arg = p[i];

                span[index] = '`';
                arg.CopyTo(span.Slice(index + 1));
                index += arg.Length + 1;
                span[index] = '`';
                span[index + 1] = '=';
                span[index + 2] = '@';
                index += 3;
                ((uint)(i - skip + startIndex)).TryFormat(span.Slice(index), out int charsWritten, "F0", CultureInfo.InvariantCulture);
                index += charsWritten;
            }
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p)
    {
        return string.Create(p.Length + 2, p, (span, state) =>
        {
            state.AsSpan().CopyTo(span[1..]);
            span[0] = '`';
            span[^1] = '`';
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2)
    {
        return string.Create(p1.Length + p2.Length + 5,
            new ValueTuple<string, string>(p1, p2), (span, state) =>
        {
            state.Item1.AsSpan().CopyTo(span[1..]);
            state.Item2.AsSpan().CopyTo(span.Slice(state.Item1.Length + 4));
            span[0] = '`';
            WriteMidSection(span, state.Item1.Length + 1);
            span[^1] = '`';
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + 8,
            new ValueTuple<string, string, string>(p1, p2, p3), (span, state) =>
        {
            span[0] = '`';
            state.Item1.AsSpan().CopyTo(span[1..]);
            int index = state.Item1.Length + 1;
            index += WriteMidSection(span, index);
            state.Item2.AsSpan().CopyTo(span[index..]);
            index += state.Item2.Length;
            index += WriteMidSection(span, index);
            state.Item3.AsSpan().CopyTo(span[index..]);
            span[^1] = '`';
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + 11,
            new ValueTuple<string, string, string, string>(p1, p2, p3, p4), (span, state) =>
        {
            span[0] = '`';
            state.Item1.AsSpan().CopyTo(span[1..]);
            int index = state.Item1.Length + 1;
            index += WriteMidSection(span, index);
            state.Item2.AsSpan().CopyTo(span[index..]);
            index += state.Item2.Length;
            index += WriteMidSection(span, index);
            state.Item3.AsSpan().CopyTo(span[index..]);
            index += state.Item3.Length;
            index += WriteMidSection(span, index);
            state.Item4.AsSpan().CopyTo(span[index..]);
            span[^1] = '`';
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + 14,
            new ValueTuple<string, string, string, string, string>(p1, p2, p3, p4, p5), (span, state) =>
        {
            span[0] = '`';
            state.Item1.AsSpan().CopyTo(span[1..]);
            int index = state.Item1.Length + 1;
            index += WriteMidSection(span, index);
            state.Item2.AsSpan().CopyTo(span[index..]);
            index += state.Item2.Length;
            index += WriteMidSection(span, index);
            state.Item3.AsSpan().CopyTo(span[index..]);
            index += state.Item3.Length;
            index += WriteMidSection(span, index);
            state.Item4.AsSpan().CopyTo(span[index..]);
            index += state.Item4.Length;
            index += WriteMidSection(span, index);
            state.Item5.AsSpan().CopyTo(span[index..]);
            span[^1] = '`';
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5, string p6)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + 17,
            new ValueTuple<string, string, string, string, string, string>(p1, p2, p3, p4, p5, p6), (span, state) =>
        {
            span[0] = '`';
            state.Item1.AsSpan().CopyTo(span[1..]);
            int index = state.Item1.Length + 1;
            index += WriteMidSection(span, index);
            state.Item2.AsSpan().CopyTo(span[index..]);
            index += state.Item2.Length;
            index += WriteMidSection(span, index);
            state.Item3.AsSpan().CopyTo(span[index..]);
            index += state.Item3.Length;
            index += WriteMidSection(span, index);
            state.Item4.AsSpan().CopyTo(span[index..]);
            index += state.Item4.Length;
            index += WriteMidSection(span, index);
            state.Item5.AsSpan().CopyTo(span[index..]);
            index += state.Item5.Length;
            index += WriteMidSection(span, index);
            state.Item6.AsSpan().CopyTo(span[index..]);
            span[^1] = '`';
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5, string p6, string p7)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + 20,
            new ValueTuple<string, string, string, string, string, string, string>(p1, p2, p3, p4, p5, p6, p7), (span, state) =>
        {
            span[0] = '`';
            state.Item1.AsSpan().CopyTo(span[1..]);
            int index = state.Item1.Length + 1;
            index += WriteMidSection(span, index);
            state.Item2.AsSpan().CopyTo(span[index..]);
            index += state.Item2.Length;
            index += WriteMidSection(span, index);
            state.Item3.AsSpan().CopyTo(span[index..]);
            index += state.Item3.Length;
            index += WriteMidSection(span, index);
            state.Item4.AsSpan().CopyTo(span[index..]);
            index += state.Item4.Length;
            index += WriteMidSection(span, index);
            state.Item5.AsSpan().CopyTo(span[index..]);
            index += state.Item5.Length;
            index += WriteMidSection(span, index);
            state.Item6.AsSpan().CopyTo(span[index..]);
            index += state.Item6.Length;
            index += WriteMidSection(span, index);
            state.Item7.AsSpan().CopyTo(span[index..]);
            span[^1] = '`';
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + 23,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string>>(p1, p2, p3, p4, p5, p6, p7, new ValueTuple<string>(p8)), (span, state) =>
            {
                span[0] = '`';
                state.Item1.AsSpan().CopyTo(span[1..]);
                int index = state.Item1.Length + 1;
                index += WriteMidSection(span, index);
                state.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Item2.Length;
                index += WriteMidSection(span, index);
                state.Item3.AsSpan().CopyTo(span[index..]);
                index += state.Item3.Length;
                index += WriteMidSection(span, index);
                state.Item4.AsSpan().CopyTo(span[index..]);
                index += state.Item4.Length;
                index += WriteMidSection(span, index);
                state.Item5.AsSpan().CopyTo(span[index..]);
                index += state.Item5.Length;
                index += WriteMidSection(span, index);
                state.Item6.AsSpan().CopyTo(span[index..]);
                index += state.Item6.Length;
                index += WriteMidSection(span, index);
                state.Item7.AsSpan().CopyTo(span[index..]);
                index += state.Item7.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item1.AsSpan().CopyTo(span[index..]);
                span[^1] = '`';
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + 26,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string>>(p1, p2, p3, p4, p5, p6, p7, new ValueTuple<string, string>(p8, p9)), (span, state) =>
            {
                span[0] = '`';
                state.Item1.AsSpan().CopyTo(span[1..]);
                int index = state.Item1.Length + 1;
                index += WriteMidSection(span, index);
                state.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Item2.Length;
                index += WriteMidSection(span, index);
                state.Item3.AsSpan().CopyTo(span[index..]);
                index += state.Item3.Length;
                index += WriteMidSection(span, index);
                state.Item4.AsSpan().CopyTo(span[index..]);
                index += state.Item4.Length;
                index += WriteMidSection(span, index);
                state.Item5.AsSpan().CopyTo(span[index..]);
                index += state.Item5.Length;
                index += WriteMidSection(span, index);
                state.Item6.AsSpan().CopyTo(span[index..]);
                index += state.Item6.Length;
                index += WriteMidSection(span, index);
                state.Item7.AsSpan().CopyTo(span[index..]);
                index += state.Item7.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item1.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item1.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item2.AsSpan().CopyTo(span[index..]);
                span[^1] = '`';
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9, string p10)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + p10.Length + 29,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string>>(p1, p2, p3, p4, p5, p6, p7, new ValueTuple<string, string, string>(p8, p9, p10)), (span, state) =>
            {
                span[0] = '`';
                state.Item1.AsSpan().CopyTo(span[1..]);
                int index = state.Item1.Length + 1;
                index += WriteMidSection(span, index);
                state.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Item2.Length;
                index += WriteMidSection(span, index);
                state.Item3.AsSpan().CopyTo(span[index..]);
                index += state.Item3.Length;
                index += WriteMidSection(span, index);
                state.Item4.AsSpan().CopyTo(span[index..]);
                index += state.Item4.Length;
                index += WriteMidSection(span, index);
                state.Item5.AsSpan().CopyTo(span[index..]);
                index += state.Item5.Length;
                index += WriteMidSection(span, index);
                state.Item6.AsSpan().CopyTo(span[index..]);
                index += state.Item6.Length;
                index += WriteMidSection(span, index);
                state.Item7.AsSpan().CopyTo(span[index..]);
                index += state.Item7.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item1.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item1.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item2.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item3.AsSpan().CopyTo(span[index..]);
                span[^1] = '`';
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9, string p10, string p11)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + p10.Length + p11.Length + 32,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string, string>>(p1, p2, p3, p4, p5, p6, p7, new ValueTuple<string, string, string, string>(p8, p9, p10, p11)), (span, state) =>
            {
                span[0] = '`';
                state.Item1.AsSpan().CopyTo(span[1..]);
                int index = state.Item1.Length + 1;
                index += WriteMidSection(span, index);
                state.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Item2.Length;
                index += WriteMidSection(span, index);
                state.Item3.AsSpan().CopyTo(span[index..]);
                index += state.Item3.Length;
                index += WriteMidSection(span, index);
                state.Item4.AsSpan().CopyTo(span[index..]);
                index += state.Item4.Length;
                index += WriteMidSection(span, index);
                state.Item5.AsSpan().CopyTo(span[index..]);
                index += state.Item5.Length;
                index += WriteMidSection(span, index);
                state.Item6.AsSpan().CopyTo(span[index..]);
                index += state.Item6.Length;
                index += WriteMidSection(span, index);
                state.Item7.AsSpan().CopyTo(span[index..]);
                index += state.Item7.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item1.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item1.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item2.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item3.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item3.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item4.AsSpan().CopyTo(span[index..]);
                span[^1] = '`';
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9, string p10, string p11, string p12)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + p10.Length + p11.Length + p12.Length + 35,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string, string, string>>(p1, p2, p3, p4, p5, p6, p7, new ValueTuple<string, string, string, string, string>(p8, p9, p10, p11, p12)), (span, state) =>
            {
                span[0] = '`';
                state.Item1.AsSpan().CopyTo(span[1..]);
                int index = state.Item1.Length + 1;
                index += WriteMidSection(span, index);
                state.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Item2.Length;
                index += WriteMidSection(span, index);
                state.Item3.AsSpan().CopyTo(span[index..]);
                index += state.Item3.Length;
                index += WriteMidSection(span, index);
                state.Item4.AsSpan().CopyTo(span[index..]);
                index += state.Item4.Length;
                index += WriteMidSection(span, index);
                state.Item5.AsSpan().CopyTo(span[index..]);
                index += state.Item5.Length;
                index += WriteMidSection(span, index);
                state.Item6.AsSpan().CopyTo(span[index..]);
                index += state.Item6.Length;
                index += WriteMidSection(span, index);
                state.Item7.AsSpan().CopyTo(span[index..]);
                index += state.Item7.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item1.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item1.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item2.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item2.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item3.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item3.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item4.AsSpan().CopyTo(span[index..]);
                index += state.Rest.Item4.Length;
                index += WriteMidSection(span, index);
                state.Rest.Item5.AsSpan().CopyTo(span[index..]);
                span[^1] = '`';
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with back-ticks surrounding them.
    /// </summary>
    public static string ColumnList(params string[] p)
    {
        switch (p.Length)
        {
            case 0:  return string.Empty;
            case 1:  return ColumnList(p[0]);
            case 2:  return ColumnList(p[0], p[1]);
            case 3:  return ColumnList(p[0], p[1], p[2]);
            case 4:  return ColumnList(p[0], p[1], p[2], p[3]);
            case 5:  return ColumnList(p[0], p[1], p[2], p[3], p[4]);
            case 6:  return ColumnList(p[0], p[1], p[2], p[3], p[4], p[5]);
            case 7:  return ColumnList(p[0], p[1], p[2], p[3], p[4], p[5], p[6]);
            case 8:  return ColumnList(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7]);
            case 9:  return ColumnList(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8]);
            case 10: return ColumnList(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9]);
            case 11: return ColumnList(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10]);
            case 12: return ColumnList(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11]);
            default:
                int ttlLen = p.Length - 1 + p.Length * 2;

                for (int i = 0; i < p.Length; ++i)
                    ttlLen += p[i].Length;

                return string.Create(ttlLen, p, (span, state) =>
                {
                    span[0] = '`';
                    span[^1] = '`';
                    int index = 1;
                    for (int i = 0; i < state.Length; ++i)
                    {
                        if (i != 0)
                            index += WriteMidSection(span, index);
                        string arg = state[i];
                        arg.AsSpan().CopyTo(span[index..]);
                        index += arg.Length;
                    }
                });
        }
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p)
    {
        return string.Create(p.Length + 2, new ValueTuple<string, string>(alias, p), (span, state) =>
        {
            int index = 0;
            WriteAliasedName(span, state.Item1, state.Item2, ref index);
        });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2)
    {
        return string.Create(p1.Length + p2.Length + 5,
            new ValueTuple<string, string, string>(alias, p1, p2), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + 8,
            new ValueTuple<string, string, string, string>(alias, p1, p2, p3), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + 11,
            new ValueTuple<string, string, string, string, string>(alias, p1, p2, p3, p4), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + 14,
            new ValueTuple<string, string, string, string, string, string>(alias, p1, p2, p3, p4, p5), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5, string p6)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + 17,
            new ValueTuple<string, string, string, string, string, string, string>(alias, p1, p2, p3, p4, p5, p6), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item7, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5, string p6, string p7)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + 20,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string>>(alias, p1, p2, p3, p4, p5, p6, new ValueTuple<string>(p7)), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item7, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item1, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + 23,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string>>(alias, p1, p2, p3, p4, p5, p6, new ValueTuple<string, string>(p7, p8)), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item7, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item1, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item2, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + 26,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string>>(alias, p1, p2, p3, p4, p5, p6, new ValueTuple<string, string, string>(p7, p8, p9)), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item7, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item1, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item3, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9, string p10)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + p10.Length + 29,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string, string>>(alias, p1, p2, p3, p4, p5, p6, new ValueTuple<string, string, string, string>(p7, p8, p9, p10)), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item7, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item1, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item4, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9, string p10, string p11)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + p10.Length + p11.Length + 32,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string, string, string>>(alias, p1, p2, p3, p4, p5, p6, new ValueTuple<string, string, string, string, string>(p7, p8, p9, p10, p11)), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item7, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item1, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item5, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9, string p10, string p11, string p12)
    {
        return string.Create(p1.Length + p2.Length + p3.Length + p4.Length + p5.Length + p6.Length + p7.Length + p8.Length + p9.Length + p10.Length + p11.Length + p12.Length + 35,
            new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string, string, string, string>>(alias, p1, p2, p3, p4, p5, p6, new ValueTuple<string, string, string, string, string, string>(p7, p8, p9, p10, p11, p12)), (span, state) =>
            {
                string alias = state.Item1;
                int index = 0;
                WriteAliasedName(span, alias, state.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item6, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Item7, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item1, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item2, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item3, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item4, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item5, ref index);
                span[index] = ',';
                ++index;
                WriteAliasedName(span, alias, state.Rest.Item6, ref index);
            });
    }

    /// <summary>
    /// Create a list of comma-separated columns with their alias name with back-ticks surrounding the column and alias name.
    /// </summary>
    public static string AliasedColumnList(string alias, params string[] p)
    {
        switch (p.Length)
        {
            case 0:  return string.Empty;
            case 1:  return AliasedColumnList(alias, p[0]);
            case 2:  return AliasedColumnList(alias, p[0], p[1]);
            case 3:  return AliasedColumnList(alias, p[0], p[1], p[2]);
            case 4:  return AliasedColumnList(alias, p[0], p[1], p[2], p[3]);
            case 5:  return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4]);
            case 6:  return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4], p[5]);
            case 7:  return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4], p[5], p[6]);
            case 8:  return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7]);
            case 9:  return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8]);
            case 10: return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9]);
            case 11: return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10]);
            case 12: return AliasedColumnList(alias, p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11]);
            default:
                int ttlLen = p.Length - 1 + p.Length * (alias.Length + 5);

                for (int i = 0; i < p.Length; ++i)
                    ttlLen += p[i].Length;

                return string.Create(ttlLen, new ValueTuple<string, string[]>(alias, p), (span, state) =>
                {
                    int index = 0;
                    string[] p = state.Item2;
                    string alias = state.Item1;
                    for (int i = 0; i < p.Length; ++i)
                    {
                        if (i != 0)
                        {
                            span[index] = ',';
                            ++index;
                        }
                        WriteAliasedName(span, alias, p[i], ref index);
                    }
                });
        }
    }
    private static int WriteMidSection(Span<char> span, int ind)
    {
        span[ind] = '`';
        span[ind + 1] = ',';
        span[ind + 2] = '`';
        return 3;
    }
    private static void WriteAliasedName(Span<char> span, string alias, string arg, ref int index)
    {
        span[index] = '`';
        alias.AsSpan().CopyTo(span.Slice(index + 1));
        index += alias.Length + 1;
        span[index] = '`';
        span[index + 1] = '.';
        span[index + 2] = '`';
        index += 3;
        arg.AsSpan().CopyTo(span.Slice(index + 1));
        index += arg.Length + 1;
    }
    private static int CountDigits(uint num)
    {
        int c = num switch
        {
            <= 9 => 1,
            <= 99 => 2,
            <= 999 => 3,
            <= 9999 => 4,
            <= 99999 => 5,
            <= 999999 => 6,
            <= 9999999 => 7,
            <= 99999999 => 8,
            <= 999999999 => 9,
            _ => 10
        };
        return c;
    }
}