using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Gamemodes.UI;
public class WinToastUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Header");
    public readonly UnturnedImage Team1Flag = new UnturnedImage("Team1Image");
    public readonly UnturnedImage Team2Flag = new UnturnedImage("Team2Image");
    public readonly UnturnedLabel Team1Tickets = new UnturnedLabel("Team1Tickets");
    public readonly UnturnedLabel Team2Tickets = new UnturnedLabel("Team2Tickets");
    public WinToastUI() : base(12012, Gamemode.Config.UIToastWin) { }
}
