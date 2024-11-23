namespace Uncreated.Warfare.Layouts.Tickets;
public enum TicketBleedSeverity
{
    None,
    Minor,
    Major,
    Drastic,
    Catastrophic
}
public static class TicketBleedSeverityExtensions
{
    public static string GetBleedMessage(this TicketBleedSeverity bleedSeverity)
    {
        switch (bleedSeverity)
        {
            case TicketBleedSeverity.None:
                return "";
            case TicketBleedSeverity.Minor:
                return "-1 per minute";
            case TicketBleedSeverity.Major:
                return "-2 per minute";
            case TicketBleedSeverity.Drastic:
                return "-3 per minute";
            case TicketBleedSeverity.Catastrophic:
                return "-60 per minute";
            default:
                return "";
        }
    }
}
