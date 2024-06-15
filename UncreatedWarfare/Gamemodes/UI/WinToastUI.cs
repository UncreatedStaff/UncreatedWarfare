using Uncreated.Framework.UI;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Gamemodes.UI;
public class WinToastUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Canvas/Content/Header");
    public readonly UnturnedImage Team1Flag = new UnturnedImage("Canvas/Content/Header/Team1Image");
    public readonly UnturnedImage Team2Flag = new UnturnedImage("Canvas/Content/Header/Team2Image");
    public readonly UnturnedLabel Team1Tickets = new UnturnedLabel("Canvas/Content/Header/Team1Tickets");
    public readonly UnturnedLabel Team2Tickets = new UnturnedLabel("Canvas/Content/Header/Team2Tickets");
    public WinToastUI() : base(Gamemode.Config.UIToastWin.GetId()) { }

    public static void SendToastCallback(UCPlayer player, in ToastMessage message, ToastMessageInfo info, UnturnedUI ui)
    {
        WinToastUI winUi = (WinToastUI)ui;
        winUi.SendToPlayer(player.Connection);
        if (message.Argument != null)
        {
            Gamemode.WinToastUI.Header.SetText(player.Connection, message.Argument);
        }
        else if (message.Arguments is { Length: > 0 })
        {
            Gamemode.WinToastUI.Header.SetText(player.Connection, message.Arguments[0]);
            if (message.Arguments.Length > 2)
            {
                Gamemode.WinToastUI.Team1Tickets.SetText(player.Connection, message.Arguments[1]);
                Gamemode.WinToastUI.Team2Tickets.SetText(player.Connection, message.Arguments[2]);
                if (message.Arguments.Length > 4)
                {
                    Gamemode.WinToastUI.Team1Flag.SetImage(player.Connection, message.Arguments[3]);
                    Gamemode.WinToastUI.Team2Flag.SetImage(player.Connection, message.Arguments[4]);
                }
            }
        }
    }
}
