using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Tickets;

[UnturnedUI(BasePath = "Canvas/Image")]
public class TicketUI : UnturnedUI
{
    public readonly UnturnedImage Flag = new UnturnedImage("Flag");
    public readonly UnturnedLabel Tickets = new UnturnedLabel("Tickets");
    public readonly UnturnedLabel Bleed = new UnturnedLabel("Bleed");
    public readonly UnturnedLabel Status = new UnturnedLabel("Status");
    public TicketUI() : base(Gamemode.Config.UITickets.AsAssetContainer()) { }
}
