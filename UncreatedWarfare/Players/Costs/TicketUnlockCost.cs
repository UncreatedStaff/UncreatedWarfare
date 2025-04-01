namespace Uncreated.Warfare.Players.Costs;

#if false
public class TicketUnlockCost : UnlockCost
{
    public int Tickets { get; set; }
    public override UniTask<bool> CanApply(UCPlayer player, ulong team, CancellationToken token = default)
    {
        return UniTask.FromResult(Data.Is<ITickets>() && team is 1 or 2);
    }

    public override UniTask Undo(UCPlayer player, ulong team, CancellationToken token = default)
    {
        if (!Data.Is(out ITickets? tickets))
            return UniTask.CompletedTask;

        switch (team)
        {
            case 1:
                tickets.TicketManager.Team1Tickets += Tickets;
                break;

            case 2:
                tickets.TicketManager.Team2Tickets += Tickets;
                break;
        }

        return UniTask.CompletedTask;
    }

    public override UniTask<bool> TryApply(UCPlayer player, ulong team, CancellationToken token = default)
    {
        if (!Data.Is(out ITickets? tickets))
            return UniTask.FromResult(false);

        switch (team)
        {
            case 1:
                tickets.TicketManager.Team1Tickets -= Tickets;
                return UniTask.FromResult(true);

            case 2:
                tickets.TicketManager.Team2Tickets -= Tickets;
                return UniTask.FromResult(true);

            default:
                return UniTask.FromResult(false);
        }
    }

    public override object Clone()
    {
        return new TicketUnlockCost { Credits = Credits, Message = Message?.Clone() };
    }
}
#endif