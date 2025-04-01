using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Cooldowns;

public readonly struct Cooldown : ITranslationArgument, IEquatable<Cooldown>, IComparable<Cooldown>
{
    /// <summary>
    /// Time at which the cooldown started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Duration of the cooldown.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Optional data to differentiate the cooldown.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Cooldown config for this cooldown.
    /// </summary>
    public CooldownTypeConfiguration Config { get; }

    public Cooldown(DateTime startTime, TimeSpan duration, CooldownTypeConfiguration config, object? data)
    {
        Duration = duration;
        StartTime = startTime;
        Config = config;
        Data = data;
    }

    /// <summary>
    /// Get the total time left. Will be negative if the cooldown is expired.
    /// </summary>
    public TimeSpan GetTimeLeft()
    {
        return StartTime.Add(Duration) - DateTime.UtcNow;
    }

    /// <summary>
    /// Check if the cooldown is still active (not expired).
    /// </summary>
    public bool IsActive()
    {
        return DateTime.UtcNow - StartTime < Duration;
    }

    /// <inheritdoc />
    public bool Equals(Cooldown other)
    {
        if (Config == null)
            return other.Config == null;
        if (other.Config == null)
            return false;

        if (!string.Equals(other.Config.Type, Config.Type, StringComparison.Ordinal))
            return false;

        if (!Equals(Data, other.Data))
            return false;

        return StartTime.Equals(other.StartTime);
    }

    /// <summary>
    /// Compare the type and data but ignore the date.
    /// </summary>
    public bool TypeEquals(Cooldown other)
    {
        return TypeEquals(other.Config?.Type, other.Data);
    }

    /// <summary>
    /// Compare the type and data but ignore the date.
    /// </summary>
    public bool TypeEquals(string? type, object? data)
    {
        if (Config == null)
            return type == null;
        if (type == null)
            return false;

        if (!string.Equals(type, Config.Type, StringComparison.Ordinal))
            return false;

        return Equals(Data, data);
    }

    /// <inheritdoc />
    public int CompareTo(Cooldown other)
    {
        if (Config == null)
            return other.Config == null ? 0 : -1;
        if (other.Config == null)
            return 1;

        int typeCmp = string.Compare(Config.Type, other.Config.Type, StringComparison.Ordinal);
        if (typeCmp != 0)
            return typeCmp;

        if (!Equals(Data, other.Data))
        {
            if (Data == null)
            {
                return -1;
            }

            if (other.Data == null)
            {
                return 1;
            }

            if (Data is IComparable c1 && other.Data is IComparable c2)
            {
                return Comparer.DefaultInvariant.Compare(c1, c2);
            }

            int cmp = Data.GetHashCode().CompareTo(other.Data.GetHashCode());
            if (cmp != 0)
                return cmp;
        }

        return StartTime.CompareTo(other.StartTime);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Cooldown cooldown)
            return false;

        return Equals(cooldown);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Config?.Type, Data);
    }

    /// <summary>Translated <see cref="ECooldownType"/>.</summary>
    public static readonly SpecialFormat FormatName = new SpecialFormat("Type", "n");

    /// <summary>3 hours and 4 minutes</summary>
    public static readonly SpecialFormat FormatTimeLong = new SpecialFormat("Long Time", "tl1");

    /// <summary>3h 4m 20s</summary>
    public static readonly SpecialFormat FormatTimeShort = new SpecialFormat("Short Time (3h 40m)", "tl2");

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (FormatTimeShort.Match(in parameters))
        {
            return FormattingUtility.ToTimeString(GetTimeLeft(), space: true);
        }

        if (FormatName.Match(in parameters))
        {
            return Config.Type;
        }

        if (FormatTimeLong.Match(in parameters))
        {
            return TimeAddon.ToLongTimeString(
                formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value,
                (int)Math.Ceiling(GetTimeLeft().TotalSeconds),
                parameters.Language
            );
        }

        return ToString();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Config.Type} - {FormattingUtility.ToTimeString(GetTimeLeft(), space: true)}";
    }
}