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
        new SpecialFormat("HH:MM:SS",   "c2", useForToString: false)
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
                : FormattingUtility.ToTimeString(ts)
        };
    }

    /// <summary>
    /// Create a time format using the given format style.
    /// </summary>
    public static ArgumentFormat Create(TimeFormatType formatType) =>
        (int)formatType < Formats.Length && formatType >= 0
            ? new ArgumentFormat(Formats[(int)formatType], InstanceArray)
            : new ArgumentFormat(Formats[0], InstanceArray);

    public static implicit operator ArgumentFormat(TimeAddon addon) => ReferenceEquals(addon, Instance) ? new ArgumentFormat(InstanceArray) : new ArgumentFormat(addon);

    internal static string ToLongTimeString(TimeTranslations timeTranslations, int seconds, LanguageInfo language)
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
/// Describes how time spans are formatted.
/// </summary>
public enum TimeFormatType
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
}