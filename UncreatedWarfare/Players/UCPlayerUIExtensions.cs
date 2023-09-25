using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;

namespace Uncreated.Warfare.Players;
internal static class UCPlayerUIExtensions
{
    internal static void SetVisibility(this IElement element, UCPlayer player, bool isEnabled)
        => element.Element?.SetVisibility(player.Player, isEnabled);
    internal static void Show(this IElement element, UCPlayer player)
        => element.SetVisibility(player, true);
    internal static void Hide(this IElement element, UCPlayer player)
        => element.SetVisibility(player, false);

    internal static void Enable(this IStateElement stateElement, UCPlayer player)
        => stateElement.SetState(player, true);
    internal static void Disable(this IStateElement stateElement, UCPlayer player)
        => stateElement.SetState(player, false);
    internal static void SetState(this IStateElement stateElement, UCPlayer player, bool isEnabled)
        => stateElement.State?.SetVisibility(player.Player, isEnabled);

    internal static void SetText(this ILabel label, UCPlayer player, string text)
        => label.Label?.SetText(player.Player, text);
    internal static void SetPlaceholder(this IPlaceholderTextBox label, UCPlayer player, string text)
        => label.Placeholder?.SetText(player.Player, text);

    internal static void SetImage(this IImage image, UCPlayer player, string url, bool forceRefresh = false)
        => image.Image?.SetImage(player.Player, url, forceRefresh);

    public static void SetVisibility(this UnturnedUIElement element, UCPlayer player, bool isEnabled)
        => element.SetVisibility(player.Player, isEnabled);
    public static void Show(this UnturnedUIElement element, UCPlayer player)
        => element.SetVisibility(player.Player, true);
    public static void Hide(this UnturnedUIElement element, UCPlayer player)
        => element.SetVisibility(player.Player, false);

    internal static void SetText(this UnturnedLabel label, UCPlayer player, string text)
        => label.SetText(player.Player, text);

    internal static void SetImage(this UnturnedImage image, UCPlayer player, string url, bool forceRefresh = false)
        => image.SetImage(player.Player, url, forceRefresh);
}
