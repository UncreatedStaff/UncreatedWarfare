using DanielWillett.ReflectionTools;
using StackCleaner;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uncreated.Warfare.Util;
public static class ExceptionFormatter
{
    public static string FormatException(Exception ex, StackTraceCleaner stackCleaner)
    {
        StringBuilder sb = new StringBuilder(256);
        FormatException(ex, sb, stackCleaner);
        return sb.ToString();
    }

    public static void FormatException(Exception ex, StringBuilder builder, StackTraceCleaner stackCleaner)
    {
        FormatSingleExceptionIntl(ex, builder, stackCleaner);
    }

    private static void FormatSingleExceptionIntl(Exception ex, StringBuilder builder, StackTraceCleaner stackCleaner)
    {
        builder.Append(Accessor.Formatter.Format(ex.GetType())).Append(" - ").AppendLine(ex.Message ?? "[ no message ]");

        builder.AppendLine(stackCleaner.GetString(ex));

        int ct = GetInnerExceptionCount(ex);
        if (ct == 0)
            return;

        builder.Append(ct == 1 ? "Inner Exception: " : $"Inner Exceptions ({ct}): ");
        foreach (Exception inner in EnumerateInnerExceptions(ex))
        {
            FormatSingleExceptionIntl(inner, builder, stackCleaner);
        }
    }

    private static int GetInnerExceptionCount(Exception ex)
    {
        switch (ex)
        {
            case AggregateException aggregate:
                return aggregate.InnerExceptions.Count;
        }

        return ex.InnerException != null ? 1 : 0;
    }

    private static IEnumerable<Exception> EnumerateInnerExceptions(Exception ex)
    {
        switch (ex)
        {
            case AggregateException aggregate:
                foreach (Exception ex2 in aggregate.InnerExceptions)
                    yield return ex2;
                yield break;
        }

        if (ex.InnerException != null)
            yield return ex.InnerException;
    }
}