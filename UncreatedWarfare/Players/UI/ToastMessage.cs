using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players.UI;
public struct ToastMessage
{
    public ToastMessageStyle Style { get; }
    public string? Argument { get; }
    public string[]? Arguments { get; }
    public object? State { get; }
    public float? OverrideDuration { get; set; }
    public bool Resend { get; set; }
    public ToastMessage(ToastMessageStyle style)
    {
        Style = style;
        Argument = null;
        Arguments = null;
    }
    public ToastMessage(ToastMessageStyle style, string argument)
    {
        Style = style;
        Argument = argument;
        Arguments = null;
    }
    public ToastMessage(ToastMessageStyle style, string[] arguments)
    {
        Style = style;
        Arguments = arguments;
        Argument = null;
    }
    public ToastMessage(object? state, ToastMessageStyle style) : this(style)
    {
        State = state;
    }
    public ToastMessage(object? state, ToastMessageStyle style, string argument) : this(style, argument)
    {
        State = state;
    }
    public ToastMessage(object? state, ToastMessageStyle style, string[] arguments) : this(style, arguments)
    {
        State = state;
    }
    public static ToastMessage Popup(string title, string? desc, string? btn1 = null, string? btn2 = null, string? btn3 = null, string? btn4 = null, string? image = null, object? state = null)
    {
        string?[] args =
        {
            title,
            desc,
            btn1,
            btn2,
            btn3,
            btn4,
            image
        };

        return new ToastMessage(state, ToastMessageStyle.Popup, args!);
    }
    public static void QueueMessage(LanguageSet set, in ToastMessage message)
    {
        while (set.MoveNext())
        {
            set.Next.Component<ToastManager>().Queue(in message);
        }
    }
    public static void QueueMessage(WarfarePlayer player, in ToastMessage message)
    {
        player.Component<ToastManager>().Queue(in message);
    }
    public static void QueueMessage(SteamPlayer player, in ToastMessage message)
    {
        // todo if (UCPlayer.FromSteamPlayer(player) is { } pl)
        // todo     pl.Toasts.Queue(in message);
    }
    public static void QueueMessage(Player player, in ToastMessage message)
    {
        // todo if (UCPlayer.FromPlayer(player) is { } pl)
        // todo     pl.Toasts.Queue(in message);
    }
    public static void QueueMessage(ulong steam64, in ToastMessage message)
    {
        // todo if (UCPlayer.FromID(steam64) is { } pl)
        // todo     pl.Toasts.Queue(in message);
    }
    public static void QueueMessage(CSteamID steam64, in ToastMessage message)
    {
        // todo if (UCPlayer.FromCSteamID(steam64) is { } pl)
        // todo     pl.Toasts.Queue(in message);
    }
}
