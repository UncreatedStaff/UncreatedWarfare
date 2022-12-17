using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes.Flags;
public abstract class BaseCTFTicketProvider : BaseTicketProvider
{
    public override void GetDisplayInfo(ulong team, out string message, out string tickets, out string bleed)
    {
        int intlBld = GetTeamBleed(team);
        tickets = (team switch { 1 => Manager.Team1Tickets, 2 => Manager.Team2Tickets, _ => 0 }).ToString(Data.LocalLocale);
        if (intlBld < 0)
        {
            message = $"{intlBld} per minute".Colorize("eb9898");
            bleed = intlBld.ToString(Data.LocalLocale);
        }
        else
            bleed = message = string.Empty;
    }
    public override void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI)
    {
        if (oldValue > 0 && newValue <= 0)
            UCWarfare.RunTask(Data.Gamemode.DeclareWin, TeamManager.Other(team), default, ctx: "Lose game, tickets reached 0.");
    }
    public override void Tick()
    {
        if (Data.Gamemode != null && Data.Gamemode.State == State.Active)
        {
            if (Data.Gamemode.EveryMinute)
            {
                int t1B = GetTeamBleed(1);
                int t2B = GetTeamBleed(2);

                if (t1B < 0)
                    Manager.Team1Tickets += t1B;
                if (t2B < 0)
                    Manager.Team2Tickets += t2B;
            }
        }
    }
}
