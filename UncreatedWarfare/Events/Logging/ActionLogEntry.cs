using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Logging;

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
}