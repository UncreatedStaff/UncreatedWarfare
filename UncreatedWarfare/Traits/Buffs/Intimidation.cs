using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Traits.Buffs;
/// <summary>
/// If there are more players with <see cref="Intimidation"/> on one team than the other when a <see cref="Flag"/> would've been contested, that team still captures.
/// </summary>
public class Intimidation : Buff
{
    private static TraitData? DATA;
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(Intimidation),
        NameTranslations = new TranslationList("Intimidation"),
        DescriptionTranslations = new TranslationList("Sway contested flags in your squad's favor."),
        CreditCost = 400,
        SquadDistributedMultiplier = 1f,
        SquadLeaderDistributedMultiplier = 1f,
        RequireSquadLeader = false,
        RequireSquad = true,
        Icon = "µ",
        Cooldown = 900,
        EffectDuration = 450,
        UnlockRequirements = new UnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 6 } },
        EffectDistributedToSquad = true,
        Data = null!
    };
    public static bool IsContestBoosted(UCPlayer player)
    {
        TraitData? d = DATA ??= TraitManager.GetData(typeof(Intimidation));
        return d != null && TraitManager.IsAffected(d, player, out _);
    }
    public static ulong CheckSquadsForContestBoost(Flag flag)
    {
        List<UCPlayer> t1 = flag.PlayersOnFlagTeam1;
        List<UCPlayer> t2 = flag.PlayersOnFlagTeam2;
        int t1Intim = 0, t2Intim = 0;
        for (int i = 0; i < t1.Count; ++i)
        {
            UCPlayer p = t1[i];
            if (p.Squad is not null && IsContestBoosted(p))
                ++t1Intim;
        }
        for (int i = 0; i < t2.Count; ++i)
        {
            UCPlayer p = t2[i];
            if (p.Squad is not null && IsContestBoosted(p))
                ++t2Intim;
        }

        if (t1Intim == t2Intim) return 0ul;
        if (t1Intim == 0 && t2Intim > 0) return 2ul;
        if (t2Intim == 0 && t1Intim > 0) return 1ul;
        if (t1Intim > t2Intim)
        {
            if (t1Intim - Gamemode.Config.AASRequiredCapturingPlayerDifference >= t2Intim)
                return 1ul;
            else return 0ul;
        }
        else
        {
            if (t2Intim - Gamemode.Config.AASRequiredCapturingPlayerDifference >= t1Intim)
                return 2ul;
            else return 0ul;
        }
    }
}
