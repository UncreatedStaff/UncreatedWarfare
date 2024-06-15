using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Tickets;
public class TicketUI : UnturnedUI
{
    public readonly UnturnedImage Flag = new UnturnedImage("Flag");
    public readonly UnturnedLabel Tickets = new UnturnedLabel("Tickets");
    public readonly UnturnedLabel Bleed = new UnturnedLabel("Bleed");
    public readonly UnturnedLabel Status = new UnturnedLabel("Status");
    public TicketUI() : base(Gamemode.Config.UITickets.GetId()) { }
}
