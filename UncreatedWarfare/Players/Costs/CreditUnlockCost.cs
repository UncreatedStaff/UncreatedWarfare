using Uncreated.Warfare.Levels;

namespace Uncreated.Warfare.Players.Costs;

#if false
/// <summary>
/// TODO I want to rewrite how credits are queried to make it safer and faster.
/// </summary>
public class CreditUnlockCost : UnlockCost
{
    public int Credits { get; set; }
    public TranslationList? Message { get; set; }
    public override async UniTask<bool> CanApply(WarfarePlayer player, ulong team, CancellationToken token = default)
    {
        int creds = await Data.AdminSql.GetCredits(player.Steam64, team, token);
        return creds >= Credits;
    }

    public override async UniTask Undo(WarfarePlayer player, ulong team, CancellationToken token = default)
    {
        int amt = Credits;
        await Points.AwardCreditsAsync(new CreditsParameters(player, team, -amt, message: null, isPunishment: false), token);
    }

    public override async UniTask<bool> TryApply(WarfarePlayer player, ulong team, CancellationToken token = default)
    {
        int startingCredits = await Data.AdminSql.GetCredits(player.Steam64, team, token);

        int amt = Credits;
        await Points.AwardCreditsAsync(new CreditsParameters(player, team, amt, message: null, isPunishment: false), token);

        int endingCredits = await Data.AdminSql.GetCredits(player.Steam64, team, token);

        return startingCredits - amt <= endingCredits;
    }

    public override object Clone()
    {
        return new CreditUnlockCost { Credits = Credits, Message = Message?.Clone() };
    }
}
#endif