using SDG.Unturned;
using System;
using Uncreated.Warfare.Components;

namespace Uncreated.Warfare;

public readonly struct ToastMessage
{
    public readonly ToastMessageSeverity Severity;
    private readonly long _time;
    public readonly string Message1;
    public readonly string? Message2;
    public readonly string? Message3;
    public const float FullToastTime = 12f;
    public const float MiniToastTime = 4f;
    public const float BigToastTime = 5.5f;
    public readonly uint InstanceID;
    private static uint _lastInstId;
    public readonly bool ResendText = false;
    public static bool operator ==(ToastMessage left, ToastMessage right) => left.InstanceID == right.InstanceID;
    public static bool operator !=(ToastMessage left, ToastMessage right) => !(left == right);
    public override int GetHashCode() => _time.GetHashCode() / 2 + Message1.GetHashCode() / 2;
    public override bool Equals(object obj) => obj is ToastMessage msg && this == msg;
    public ToastMessage(string message1, ToastMessageSeverity severity, bool resend = false)
    {
        this._time = DateTime.UtcNow.Ticks;
        this.Message1 = message1;
        this.Message2 = null;
        this.Message3 = null;
        this.Severity = severity;
        ResendText = resend;
        InstanceID = ++_lastInstId;
    }
    public ToastMessage(string message1, string message2, ToastMessageSeverity severity, bool resend = false) : this(message1, severity, resend)
    {
        this.Message2 = message2;
    }
    public ToastMessage(string message1, string message2, string message3, ToastMessageSeverity severity, bool resend = false) : this(message1, message2, severity, resend)
    {
        this.Message3 = message3;
    }
    public static void QueueMessage(UCPlayer player, ToastMessage message, bool priority = false) => QueueMessage(player.Player, message, priority);
    public static void QueueMessage(SteamPlayer player, ToastMessage message, bool priority = false) => QueueMessage(player.player, message, priority);
    public static void QueueMessage(Player player, ToastMessage message, bool priority = false)
    {
        if (player.TryGetPlayerData(out UCPlayerData c))
            c.QueueMessage(message, priority);
    }
}
public enum ToastMessageSeverity : byte
{
    Info = 0,
    Warning = 1,
    Severe = 2,
    Mini = 3,
    Medium = 4,
    Big = 5,
    Progress = 6,
    Tip = 7
}