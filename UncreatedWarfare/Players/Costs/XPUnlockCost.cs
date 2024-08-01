using System;
using Uncreated.Warfare.Levels;

namespace Uncreated.Warfare.Players.Costs;

/// <summary>
/// TODO I want to rewrite how XP is queried to make it safer and faster.
/// </summary>
public class XPUnlockCost : UnlockCost
{
    public int? XP { get; set; }
    public XPReward? RewardType { get; set; }
    public double? RewardMultiplier { get; set; }
    public TranslationList? Message { get; set; }
    public override async UniTask<bool> CanApply(UCPlayer player, ulong team, CancellationToken token = default)
    {
        int xp = await Data.AdminSql.GetXP(player.Steam64, team, token);
        return xp >= CalculateActualXPAmount();
    }

    public override async UniTask Undo(UCPlayer player, ulong team, CancellationToken token = default)
    {
        int amt = CalculateActualXPAmount();
        await Points.AwardXPAsync(new XPParameters(player, team, -amt, message: null, awardCredits: false), token);
    }

    public override async UniTask<bool> TryApply(UCPlayer player, ulong team, CancellationToken token = default)
    {
        int xp = await Data.AdminSql.GetXP(player.Steam64, team, token);

        int amt = CalculateActualXPAmount();
        await Points.AwardXPAsync(new XPParameters(player, team, amt, Message?.Translate(player.Locale.LanguageInfo), awardCredits: false), token);

        int xp2 = await Data.AdminSql.GetXP(player.Steam64, team, token);

        return xp - amt <= xp2;
    }

    private int CalculateActualXPAmount()
    {
        if (XP.HasValue)
            return -XP.Value;

        if (!RewardType.HasValue || !Points.PointsConfig.XPData.TryGetValue(RewardType.Value.ToString(), out PointsConfig.XPRewardData data))
        {
            return 0;
        }

        int amt = data.Amount;
        if (RewardMultiplier.HasValue)
        {
            amt = (int)Math.Round(RewardMultiplier.Value * amt);
        }

        return amt;
    }

    public override object Clone()
    {
        return new XPUnlockCost { XP = XP, Message = Message?.Clone(), RewardMultiplier = RewardMultiplier, RewardType = RewardType };
    }
}