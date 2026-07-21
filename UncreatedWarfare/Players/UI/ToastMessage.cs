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

    /// <summary>
    /// Create an instance of <see cref="PopupUI"/> as a toast.
    /// </summary>
    /// <param name="title">The text to display as the title.</param>
    /// <param name="desc">The text to display in the description section.</param>
    /// <param name="btn1">The text of the first button, or <see langword="null"/> to default to <see cref="PopupUI.Okay"/>.</param>
    /// <param name="btn2">The text of the second button, or <see langword="null"/> to not display this button.</param>
    /// <param name="btn3">The text of the third button, or <see langword="null"/> to not display this button.</param>
    /// <param name="btn4">The text of the fourth button, or <see langword="null"/> to not display this button.</param>
    /// <param name="imageUrl">The URL of the image to display in the top right corner.</param>
    /// <param name="callbacks">Callbacks to run for each button.</param>
    /// <returns>A message that can be sent using <see cref="ToastPlayerExtensions.SendToast"/>.</returns>
    public static ToastMessage Popup(
        string title,
        string? desc,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn1 = null,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn2 = null,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn3 = null,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn4 = null,
        string? imageUrl = null,
        PopupCallbacks callbacks = default)
    {
        int nullFlags = (string.IsNullOrEmpty(desc) ? 1 : 0) * (1 << 1)
                        | (string.IsNullOrEmpty(btn1) ? 1 : 0) * (1 << 2)
                        | (string.IsNullOrEmpty(btn2) ? 1 : 0) * (1 << 3)
                        | (string.IsNullOrEmpty(btn3) ? 1 : 0) * (1 << 4)
                        | (string.IsNullOrEmpty(btn4) ? 1 : 0) * (1 << 5)
                        | (string.IsNullOrEmpty(imageUrl) ? 1 : 0) * (1 << 6);

        object? state = callbacks.Button1 != null
                        || callbacks.Button2 != null
                        || callbacks.Button3 != null
                        || callbacks.Button4 != null
            ? callbacks
            : null;

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

    /// <summary>
    /// Create an instance of <see cref="CopyPopupUI"/> as a toast.
    /// </summary>
    /// <param name="copyText">The text to be copied. This will display in a read-only input box that the player can select and copy from.</param>
    /// <param name="title">The text to display as the title.</param>
    /// <param name="desc">The text to display in the description section. The value "<c>&lt;copy/&gt;</c>" will be replaced with the keybind for copy depending on the player's OS.</param>
    /// <param name="btn">The text of the close button, or <see langword="null"/> to default to <see cref="PopupUI.Okay"/>.</param>
    /// <param name="callbacks">Callbacks to run for each button.</param>
    /// <returns>A message that can be sent using <see cref="ToastPlayerExtensions.SendToast"/>.</returns>
    public static ToastMessage CopyPopup(
        string copyText,
        string? title,
        string? desc,
        [ValueProvider("Uncreated.Warfare.Players.UI.PopupUI")] string? btn = null,
        CopyPopupCallbacks callbacks = default)
    {
        object? state = callbacks.Button != null
                        || callbacks.Text != null
            ? callbacks
            : null;

        string?[] args;
        if (btn != null)
            args = [ copyText, title, desc, btn ];
        else if (desc != null)
            args = [ copyText, title, desc ];
        else if (title != null)
            args = [ copyText, title ];
        else
            return new ToastMessage(state, ToastMessageStyle.CopyPopup, copyText);

        return new ToastMessage(state, ToastMessageStyle.CopyPopup, args!);
    }
}