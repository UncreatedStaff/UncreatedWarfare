using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations.Addons;

/// <summary>
/// Converts arguments to time strings.
/// </summary>
public sealed class TimeAddon : IArgumentAddon
{
    public static TimeAddon Instance { get; } = new TimeAddon();

    private static readonly IArgumentAddon[] InstanceArray = [ Instance ];

    private static readonly SpecialFormat[] Formats =
    [
        // TimeSpan
        new SpecialFormat("Short Time", "st", useForToString: false),
        new SpecialFormat("Long Time",  "lt", useForToString: false),
        new SpecialFormat("Seconds",    "s",  useForToString: false),
        new SpecialFormat("Minutes",    "m",  useForToString: false),
        new SpecialFormat("Hours",      "h",  useForToString: false),
        new SpecialFormat("Days",       "d",  useForToString: false),
        new SpecialFormat("Weeks",      "w",  useForToString: false),
        new SpecialFormat("Months",     "mo", useForToString: false),
        new SpecialFormat("Years",      "y",  useForToString: false),
        new SpecialFormat("MM:SS",      "c1", useForToString: false),
        new SpecialFormat("HH:MM:SS",   "c2", useForToString: false),

        // DateTime[Offset]
        new SpecialFormat("Long Date/Time",        "F"),
        new SpecialFormat("Short Date/Time",       "g"),
        new SpecialFormat("Long Date",             "D"),
        new SpecialFormat("Short Date",            "d"),
        new SpecialFormat("Long Time",             "T"),
        new SpecialFormat("Short Time",            "t"),
        new SpecialFormat("Relative (Short Time)", "rels", useForToString: false),
        new SpecialFormat("Relative (Long Time)",  "rell", useForToString: false),
    ];

    public string DisplayName => "Asset Rarity Color";
    private TimeAddon() { }
    public string ApplyAddon(ITranslationValueFormatter formatter, string text, TypedReference value, in ValueFormatParameters args)
    {
        TimeSpan ts;

        if (__reftype(value) == typeof(TimeSpan))
        {
            ts = __refvalue(value, TimeSpan);
        }
        else if (__reftype(value) == typeof(DateTime))
        {
            DateTime dt = __refvalue(value, DateTime);
            if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();
            return args.Format.Format switch
            {
                "rels" => FormatDateTimeRelative(false, dt, formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value, args.Language, args.Culture),
                "rell" => FormatDateTimeRelative(true, dt, formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value, args.Language, args.Culture),
                _ => TryFormatDateTime(dt, args.Format.Format, args.Culture, args.TimeZone)
            };
        }
        else if (__reftype(value) == typeof(DateTimeOffset))
        {
            DateTime dt = __refvalue(value, DateTimeOffset).UtcDateTime;
            return args.Format.Format switch
            {
                "rels" => FormatDateTimeRelative(false, dt, formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value, args.Language, args.Culture),
                "rell" => FormatDateTimeRelative(true, dt, formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value, args.Language, args.Culture),
                _ => TryFormatDateTime(dt, args.Format.Format, args.Culture, args.TimeZone)
            };
        }
        else if (TypedReference.ToObject(value) is IConvertible c)
        {
            try
            {
                ts = TimeSpan.FromSeconds(c.ToDouble(CultureInfo.InvariantCulture));
            }
            catch (InvalidCastException)
            {
                return text;
            }
        }
        else return text;

        return args.Format.Format switch
        {
            "lt" => ToLongTimeString(formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value, (int)ts.TotalSeconds, args.Language),
            "s" => ((int)ts.TotalSeconds).ToString(args.Culture),
            "m" => ((int)ts.TotalMinutes).ToString(args.Culture),
            "h" => ((int)ts.TotalHours).ToString(args.Culture),
            "d" => ((int)ts.TotalDays).ToString(args.Culture),
            "w" => ((int)ts.TotalDays / 7).ToString(args.Culture),
            "mo" => ((int)(ts.TotalSeconds / 2629800)).ToString(args.Culture),
            "y" => ((int)(ts.TotalSeconds / 31536000)).ToString(args.Culture),
            "c1" => FormattingUtility.ToCountdownString((int)ts.TotalSeconds, false),
            "c2" => FormattingUtility.ToCountdownString((int)ts.TotalSeconds, true),
            _ => ts.Ticks < 0
                ? formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value.TimePermanent.Translate(args.Language)
                : FormattingUtility.ToTimeString(ts, space: true)
        };
    }

    private static string TryFormatDateTime(DateTime dateTime, string? format, CultureInfo culture, TimeZoneInfo timeZone)
    {
        dateTime = TimeZoneInfo.ConvertTime(dateTime, dateTime.Kind == DateTimeKind.Local ? TimeZoneInfo.Local : TimeZoneInfo.Utc, timeZone);

        try
        {
            return dateTime.ToString(format, culture);
        }
        catch (FormatException)
        {
            return dateTime.ToString("G", culture);
        }
    }

    internal static string FormatDateTimeRelative(bool isLong, DateTime dateTime, TimeTranslations translations, LanguageInfo language, CultureInfo culture)
    {
        DateTime now = DateTime.UtcNow;

        if (dateTime <= now.AddSeconds(1d) && dateTime >= now.AddSeconds(-1d))
        {
            return translations.RelativeTimeNow.Translate(language);
        }

        string prefix, suffix;
        TimeSpan difference;
        if (dateTime < now)
        {
            prefix = translations.RelativeTimePastPrefix.Translate(language);
            suffix = translations.RelativeTimePastSuffix.Translate(language);
            difference = now - dateTime;
        }
        else
        {
            prefix = translations.RelativeTimeFuturePrefix.Translate(language);
            suffix = translations.RelativeTimeFutureSuffix.Translate(language);
            difference = dateTime - now;
        }

        string timeSpan = isLong
            ? ToLongTimeString(translations, (int)Math.Round(difference.TotalSeconds, MidpointRounding.AwayFromZero), language)
            : FormattingUtility.ToTimeString(difference, 2, space: true);

        int length = timeSpan.Length;
        if (prefix.Length > 0)
            length += prefix.Length + 1;
        if (suffix.Length > 0)
            length += suffix.Length + 1;

        CreateRelativeTimeStringState state = default;
        state.Prefix = prefix;
        state.Suffix = suffix;
        state.TimeSpan = timeSpan;

        return string.Create(length, state, (span, state) =>
        {
            int index = 0;
            if (state.Prefix.Length > 0)
            {
                state.Prefix.AsSpan().CopyTo(span);
                index += state.Prefix.Length;
                span[index] = ' ';
                ++index;
            }

            state.TimeSpan.AsSpan().CopyTo(span[index..]);

            if (state.Suffix.Length == 0)
                return;

            index += state.TimeSpan.Length;
            span[index] = ' ';
            ++index;

            state.Suffix.AsSpan().CopyTo(span[index..]);
        });
    }

    private struct CreateRelativeTimeStringState
    {
        public string Prefix, Suffix, TimeSpan;
    }

    /// <summary>
    /// Create a <see cref="TimeSpan"/> format using the given format style.
    /// </summary>
    public static ArgumentFormat Create(TimeSpanFormatType spanFormatType) =>
        (int)spanFormatType < 11 && spanFormatType >= 0
            ? new ArgumentFormat(Formats[(int)spanFormatType], InstanceArray)
            : new ArgumentFormat(Formats[0], InstanceArray);

    /// <summary>
    /// Create a <see cref="DateTime"/> format using the given format style.
    /// </summary>
    public static ArgumentFormat Create(DateTimeFormatType spanFormatType) =>
        (int)spanFormatType < Formats.Length && spanFormatType >= 0
            ? new ArgumentFormat(Formats[(int)spanFormatType + 11], InstanceArray)
            : new ArgumentFormat(Formats[12], InstanceArray);

    public static implicit operator ArgumentFormat(TimeAddon addon) => ReferenceEquals(addon, Instance) ? new ArgumentFormat(InstanceArray) : new ArgumentFormat(addon);

    /// <summary>
    /// Converts to a string in the form of '1 hour and 10 minutes', etc. It will provide up to two figures.
    /// </summary>
    public static string ToLongTimeString(TimeTranslations timeTranslations, int seconds, LanguageInfo language)
    {
        // tested 09/13/2024
        int high, low;
        Translation? highSuffixTranslation, lowSuffixTranslation;

        switch (seconds)
        {
            case < 0:
                return timeTranslations.TimePermanent.Translate(language);

            // < 1 minute
            case < 60:
                high = Math.Max(seconds, 1);
                low = 0;
                highSuffixTranslation = high == 1 ? timeTranslations.TimeSecondSingle : timeTranslations.TimeSecondPlural;
                lowSuffixTranslation = null;
                break;

            // < 1 hour
            case < 3600:
                high = seconds / 60;
                low = seconds % 60;
                highSuffixTranslation = high == 1 ? timeTranslations.TimeMinuteSingle : timeTranslations.TimeMinutePlural;
                lowSuffixTranslation = low == 1 ? timeTranslations.TimeSecondSingle : timeTranslations.TimeSecondPlural;
                break;

            // < 1 day
            case < 86400:
                high = seconds / 3600;
                low = seconds / 60 % 60;
                highSuffixTranslation = high == 1 ? timeTranslations.TimeHourSingle : timeTranslations.TimeHourPlural;
                lowSuffixTranslation = low == 1 ? timeTranslations.TimeMinuteSingle : timeTranslations.TimeMinutePlural;
                break;

            // < 1 month (29.6875 days) (365.25/12)
            case < 2629800:
                high = seconds / 86400;
                low = seconds / 3600 % 24;
                highSuffixTranslation = high == 1 ? timeTranslations.TimeDaySingle : timeTranslations.TimeDayPlural;
                lowSuffixTranslation = low == 1 ? timeTranslations.TimeHourSingle : timeTranslations.TimeHourPlural;
                break;

            // < 1 year
            case < 31536000:
                high = seconds / 2629800;
                low = (int)(seconds / 86400d % (365.25d / 12d));
                highSuffixTranslation = high == 1 ? timeTranslations.TimeMonthSingle : timeTranslations.TimeMonthPlural;
                lowSuffixTranslation = low == 1 ? timeTranslations.TimeDaySingle : timeTranslations.TimeDayPlural;
                break;

            // < 1 year
            default:
                high = seconds / 31536000;
                low = (seconds / 2628000) % 12;
                highSuffixTranslation = high == 1 ? timeTranslations.TimeYearSingle : timeTranslations.TimeYearPlural;
                lowSuffixTranslation = low == 1 ? timeTranslations.TimeMonthSingle : timeTranslations.TimeMonthPlural;
                break;
        }


        int highDigits = MathUtility.CountDigits(high);
        int lowDigits = MathUtility.CountDigits(low);
        string highSuffix = highSuffixTranslation.Translate(language);
        string lowSuffix = lowSuffixTranslation == null || low == 0 ? string.Empty : lowSuffixTranslation.Translate(language);
        string and = timeTranslations.TimeAnd.Translate(language);

        LongTimeState state = default;
        state.High = high;
        state.Low = low;
        state.HighSuffix = highSuffix;
        state.LowSuffix = lowSuffix;
        state.And = and;

        int len = highDigits + 1 + highSuffix.Length;
        if (low != 0)
        {
            len += 3 + and.Length + lowDigits + lowSuffix.Length;
        }

        return string.Create(len, state, (span, state) =>
        {
            state.High.TryFormat(span, out int index, "D", CultureInfo.InvariantCulture);
            span[index] = ' ';
            ++index;
            state.HighSuffix.AsSpan().CopyTo(span[index..]);
            index += state.HighSuffix.Length;
            if (state.Low == 0)
                return;
            span[index] = ' ';
            ++index;
            state.And.AsSpan().CopyTo(span[index..]);
            index += state.And.Length;
            span[index] = ' ';
            ++index;
            state.Low.TryFormat(span[index..], out int charsWritten, "D", CultureInfo.InvariantCulture);
            index += charsWritten;
            span[index] = ' ';
            ++index;
            state.LowSuffix.AsSpan().CopyTo(span[index..]);
        });
    }

    private struct LongTimeState
    {
        public int High;
        public int Low;
        public string HighSuffix;
        public string LowSuffix;
        public string And;
    }
}

/// <summary>
/// Describes how <see cref="DateTime"/> and <see cref="DateTimeOffset"/>'s are formatted.
/// </summary>
public enum DateTimeFormatType
{
    /// <summary>
    /// Monday, June 15, 2009 1:45 PM
    /// </summary>
    /// <remarks><c>"F"</c></remarks>
    LongDateTime,

    /// <summary>
    /// 6/15/2009 1:45 PM
    /// </summary>
    /// <remarks><c>"g"</c></remarks>
    ShortDateTime,

    /// <summary>
    /// Monday, June 15, 2009
    /// </summary>
    /// <remarks><c>"D"</c></remarks>
    LongDate,

    /// <summary>
    /// 6/15/2009
    /// </summary>
    /// <remarks><c>"d"</c></remarks>
    ShortDate,

    /// <summary>
    /// 1:45:30 PM
    /// </summary>
    /// <remarks><c>"T"</c></remarks>
    LongTime,

    /// <summary>
    /// 1:45 PM
    /// </summary>
    /// <remarks><c>"t"</c></remarks>
    ShortTime,

    /// <summary>
    /// <c>in {time span}</c> or <c>{time span} ago</c> or <c>now</c>. Where <c>time span</c> is 3d2h.
    /// </summary>
    RelativeShort,

    /// <summary>
    /// <c>in {time span}</c> or <c>{time span} ago</c> or <c>now</c>. Where <c>time span</c> is 3 days and 2 hours.
    /// </summary>
    RelativeLong
}

/// <summary>
/// Describes how time spans are formatted.
/// </summary>
public enum TimeSpanFormatType
{
    /// <summary>
    /// 5d20h10m50s, permanent, etc.
    /// </summary>
    Short,

    /// <summary>
    /// 5 days and 20 hours
    /// </summary>
    Long,

    /// <summary>
    /// Number of seconds.
    /// </summary>
    Seconds,

    /// <summary>
    /// Number of minutes.
    /// </summary>
    Minutes,

    /// <summary>
    /// Number of hours.
    /// </summary>
    Hours,

    /// <summary>
    /// Number of days.
    /// </summary>
    Days,
    
    /// <summary>
    /// Number of weeks.
    /// </summary>
    Weeks,
    
    /// <summary>
    /// Number of months.
    /// </summary>
    Months,
    
    /// <summary>
    /// Number of years.
    /// </summary>
    Years,

    /// <summary>
    /// Formatted MM:SS.
    /// </summary>
    CountdownMinutesSeconds,

    /// <summary>
    /// Formatted HH:MM:SS.
    /// </summary>
    CountdownHoursMinutesSeconds
}

public sealed class TimeTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Time";

    [TranslationData("Permanent time, lasts forever.")]
    public readonly Translation TimePermanent = new Translation("permanent", TranslationOptions.UnityUINoReplace);

    [TranslationData("1 second (singular).")]
    public readonly Translation TimeSecondSingle = new Translation("second", TranslationOptions.UnityUINoReplace);

    [TranslationData("X seconds (plural).")]
    public readonly Translation TimeSecondPlural = new Translation("seconds", TranslationOptions.UnityUINoReplace);

    [TranslationData("1 minute (singular).")]
    public readonly Translation TimeMinuteSingle = new Translation("minute", TranslationOptions.UnityUINoReplace);

    [TranslationData("X minutes (plural).")]
    public readonly Translation TimeMinutePlural = new Translation("minutes", TranslationOptions.UnityUINoReplace);

    [TranslationData("1 hour (singular).")]
    public readonly Translation TimeHourSingle = new Translation("hour", TranslationOptions.UnityUINoReplace);

    [TranslationData("X hours (plural).")]
    public readonly Translation TimeHourPlural = new Translation("hours", TranslationOptions.UnityUINoReplace);

    [TranslationData("1 day (singular).")]
    public readonly Translation TimeDaySingle = new Translation("day", TranslationOptions.UnityUINoReplace);

    [TranslationData("X days (plural).")]
    public readonly Translation TimeDayPlural = new Translation("days", TranslationOptions.UnityUINoReplace);

    [TranslationData("1 week (singular).")]
    public readonly Translation TimeWeekSingle = new Translation("week", TranslationOptions.UnityUINoReplace);

    [TranslationData("X weeks (plural).")]
    public readonly Translation TimeWeekPlural = new Translation("weeks", TranslationOptions.UnityUINoReplace);

    [TranslationData("1 month (singular).")]
    public readonly Translation TimeMonthSingle = new Translation("month", TranslationOptions.UnityUINoReplace);

    [TranslationData("X months (plural).")]
    public readonly Translation TimeMonthPlural = new Translation("months", TranslationOptions.UnityUINoReplace);

    [TranslationData("1 year (singular).")]
    public readonly Translation TimeYearSingle = new Translation("year", TranslationOptions.UnityUINoReplace);

    [TranslationData("X years (plural).")]
    public readonly Translation TimeYearPlural = new Translation("years", TranslationOptions.UnityUINoReplace);

    [TranslationData("Joining keyword (1 hour \"and\" 30 minutes).")]
    public readonly Translation TimeAnd = new Translation("and", TranslationOptions.UnityUINoReplace);

    [TranslationData("Relative time in future prefix (\"in\" 1 hour).")]
    public readonly Translation RelativeTimeFuturePrefix = new Translation("in", TranslationOptions.UnityUINoReplace);

    [TranslationData("Relative time in future suffix (in 1 hour \"\"). Unused in English.")]
    public readonly Translation RelativeTimeFutureSuffix = new Translation(string.Empty, TranslationOptions.UnityUINoReplace);

    [TranslationData("Relative time in past prefix (\"\" 1 hour ago). Unused in English.")]
    public readonly Translation RelativeTimePastPrefix = new Translation(string.Empty, TranslationOptions.UnityUINoReplace);

    [TranslationData("Relative time in past suffix (1 hour \"ago\").")]
    public readonly Translation RelativeTimePastSuffix = new Translation("ago", TranslationOptions.UnityUINoReplace);

    [TranslationData("Relative time when time is almost equal to now.")]
    public readonly Translation RelativeTimeNow = new Translation("now", TranslationOptions.UnityUINoReplace);
}