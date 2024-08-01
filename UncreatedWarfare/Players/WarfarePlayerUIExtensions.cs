using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;

namespace Uncreated.Warfare.Players;
internal static class WarfarePlayerUIExtensions
{
    internal static void SetVisibility(this IElement element, WarfarePlayer player, bool isEnabled)
        => element.Element?.SetVisibility(player.UnturnedPlayer, isEnabled);
    internal static void Show(this IElement element, WarfarePlayer player)
        => element.SetVisibility(player, true);
    internal static void Hide(this IElement element, WarfarePlayer player)
        => element.SetVisibility(player, false);

    internal static void Enable(this IStateElement stateElement, WarfarePlayer player)
        => stateElement.SetState(player, true);
    internal static void Disable(this IStateElement stateElement, WarfarePlayer player)
        => stateElement.SetState(player, false);
    internal static void SetState(this IStateElement stateElement, WarfarePlayer player, bool isEnabled)
        => stateElement.State?.SetVisibility(player.UnturnedPlayer, isEnabled);

    internal static void SetText(this ILabel label, WarfarePlayer player, string text)
        => label.Label?.SetText(player.UnturnedPlayer, text);
    internal static void SetPlaceholder(this IPlaceholderTextBox label, WarfarePlayer player, string text)
        => label.Placeholder?.SetText(player.UnturnedPlayer, text);

    internal static void SetImage(this IImage image, WarfarePlayer player, string url, bool forceRefresh = false)
        => image.Image?.SetImage(player.UnturnedPlayer, url, forceRefresh);

    public static void SetVisibility(this UnturnedUIElement element, WarfarePlayer player, bool isEnabled)
        => element.SetVisibility(player.UnturnedPlayer, isEnabled);
    public static void Show(this UnturnedUIElement element, WarfarePlayer player)
        => element.SetVisibility(player.UnturnedPlayer, true);
    public static void Hide(this UnturnedUIElement element, WarfarePlayer player)
        => element.SetVisibility(player.UnturnedPlayer, false);

    internal static void SetText(this UnturnedLabel label, WarfarePlayer player, string text)
        => label.SetText(player.UnturnedPlayer, text);

    internal static void SetImage(this UnturnedImage image, WarfarePlayer player, string url, bool forceRefresh = false)
        => image.SetImage(player.UnturnedPlayer, url, forceRefresh);
}
