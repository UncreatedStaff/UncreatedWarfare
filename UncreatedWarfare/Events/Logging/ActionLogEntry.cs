using System;
using System.Globalization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Logging;

public readonly ref struct ParsedActionLogEntry
{
    [ValueProvider("Uncreated.Warfare.Events.Logging.ActionLogTypes")]
    public readonly ActionLogType Type;
    public readonly ReadOnlySpan<char> Message;
    public readonly ulong Player;
    public readonly DateTime Time;

    public ParsedActionLogEntry(ActionLogType type, ReadOnlySpan<char> message, ulong player, DateTime time)
    {
        Type = type;
        Message = message;
        Player = player;
        Time = time;
    }
}

public readonly struct ActionLogEntry
{
    [ValueProvider("Uncreated.Warfare.Events.Logging.ActionLogTypes")]
    public readonly ActionLogType Type;
    public readonly string Message;
    public readonly ulong Player;

    public ActionLogEntry([ValueProvider("Uncreated.Warfare.Events.Logging.ActionLogTypes")] ActionLogType type, string message, ulong player)
    {
        Type = type;
        Message = message;
        Player = player;
    }
    public ActionLogEntry([ValueProvider("Uncreated.Warfare.Events.Logging.ActionLogTypes")] ActionLogType type, string message, CSteamID player)
    {
        Type = type;
        Message = message;
        Player = player.IsIndividualRef() ? player.m_SteamID : 0;
    }
    public ActionLogEntry([ValueProvider("Uncreated.Warfare.Events.Logging.ActionLogTypes")] ActionLogType type, string message, WarfarePlayer? player)
    {
        Type = type;
        Message = message;
        Player = player?.Steam64.m_SteamID ?? 0;
    }

    public static ParsedActionLogEntry FromLine(ReadOnlySpan<char> line)
    {
        int bracket1 = line.IndexOf('[');
        if (bracket1 < 0)
            return default;
        int bracket2 = line.IndexOf(']', bracket1 + 1);
        if (bracket1 < 0 || bracket1 == bracket2 - 1)
            return default;

        ReadOnlySpan<char> valueSpan = line.Slice(bracket1 + 1, bracket2 - bracket1 - 1);
        if (!DateTime.TryParseExact(valueSpan, ActionLoggerService.DateLineFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dt))
            return default;

        bracket1 = line.IndexOf('[', bracket2 + 1);
        if (bracket1 < 0)
            return default;
        bracket2 = line.IndexOf(']', bracket1 + 1);
        if (bracket1 < 0 || bracket1 == bracket2 - 1)
            return default;

        valueSpan = line.Slice(bracket1 + 1, bracket2 - bracket1 - 1);
        if (!ulong.TryParse(valueSpan, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64))
            return default;

        bracket1 = line.IndexOf('[', bracket2 + 1);
        if (bracket1 < 0)
            return default;
        bracket2 = line.IndexOf(']', bracket1 + 1);
        if (bracket1 < 0 || bracket1 == bracket2 - 1)
            return default;

        valueSpan = line.Slice(bracket1 + 1, bracket2 - bracket1 - 1);
        if (!ActionLogTypes.TryParse(valueSpan, out ActionLogType? type))
            return default;

        ReadOnlySpan<char> message = ReadOnlySpan<char>.Empty;
        if (bracket2 + 1 < line.Length)
        {
            message = line.Slice(bracket2 + 1);
            if (message.IsWhiteSpace())
                message = ReadOnlySpan<char>.Empty;
        }

        return new ParsedActionLogEntry(type, message, steam64, dt);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return !string.IsNullOrEmpty(Message) ? $"{Player} | {Type.LogName} | \"{Message}\"" : $"{Player} | {Type.LogName}";
    }
}