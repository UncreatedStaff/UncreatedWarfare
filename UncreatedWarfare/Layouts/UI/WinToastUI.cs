using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "Canvas/Content/Header")]
public class WinToastUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("~/Canvas/Content/Header");
    public readonly UnturnedImage Team1Flag = new UnturnedImage("Team1Image");
    public readonly UnturnedImage Team2Flag = new UnturnedImage("Team2Image");
    public readonly UnturnedLabel Team1Tickets = new UnturnedLabel("Team1Tickets");
    public readonly UnturnedLabel Team2Tickets = new UnturnedLabel("Team2Tickets");
    public WinToastUI(AssetConfiguration assetConfig) : base(assetConfig.GetAssetLink<EffectAsset>("UI:Toasts:GameOver")) { }

    public static void SendToastCallback(WarfarePlayer player, in ToastMessage message, ToastMessageInfo info, UnturnedUI ui)
    {
        WinToastUI winUi = (WinToastUI)ui;
        winUi.SendToPlayer(player.Connection);
        if (message.Argument != null)
        {
            winUi.Header.SetText(player.Connection, message.Argument);
        }
        else if (message.Arguments is { Length: > 0 })
        {
            winUi.Header.SetText(player.Connection, message.Arguments[0]);
            if (message.Arguments.Length <= 2)
                return;

            winUi.Team1Tickets.SetText(player.Connection, message.Arguments[1]);
            winUi.Team2Tickets.SetText(player.Connection, message.Arguments[2]);
            if (message.Arguments.Length <= 4)
                return;

            winUi.Team1Flag.SetImage(player.Connection, message.Arguments[3]);
            winUi.Team2Flag.SetImage(player.Connection, message.Arguments[4]);
        }
    }
}
