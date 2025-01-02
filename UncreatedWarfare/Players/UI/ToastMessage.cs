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

    public static ToastMessage Popup(
        string title,
        string? desc,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn1 = null,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn2 = null,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn3 = null,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn4 = null,
        string? imageUrl = null,
        object? state = null)
    {
        int nullFlags = (string.IsNullOrEmpty(desc) ? 1 : 0) * (1 << 1)
                        | (string.IsNullOrEmpty(btn1) ? 1 : 0) * (1 << 2)
                        | (string.IsNullOrEmpty(btn2) ? 1 : 0) * (1 << 3)
                        | (string.IsNullOrEmpty(btn3) ? 1 : 0) * (1 << 4)
                        | (string.IsNullOrEmpty(btn4) ? 1 : 0) * (1 << 5)
                        | (string.IsNullOrEmpty(imageUrl) ? 1 : 0) * (1 << 6);
        
        string?[] args;
        switch (nullFlags)
        {
            case >= 1 << 6:
                args = [ title, desc, btn1, btn2, btn3, btn4, imageUrl ];
                break;

            case >= 1 << 5:
                args = [ title, desc, btn1, btn2, btn3, btn4 ];
                break;

            case >= 1 << 4:
                args = [ title, desc, btn1, btn2, btn3 ];
                break;

            case >= 1 << 3:
                args = [ title, desc, btn1, btn2 ];
                break;

            case >= 1 << 2:
                args = [ title, desc, btn1 ];
                break;

            case >= 1 << 1:
                args = [ title, desc ];
                break;

            default:
                return new ToastMessage(state, ToastMessageStyle.Popup, title);
        }

        return new ToastMessage(state, ToastMessageStyle.Popup, args!);
    }
}
