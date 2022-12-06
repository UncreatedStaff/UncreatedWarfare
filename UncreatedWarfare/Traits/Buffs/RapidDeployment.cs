using System.Globalization;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Traits.Buffs;

/// <summary>
/// Decrease those affected's deployment cooldown by 25% (* by .75).
/// </summary>
public class RapidDeployment : Buff
{
    private static TraitData? DATA;
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(RapidDeployment),
        NameTranslations = new TranslationList("Rapid Deployment"),
        DescriptionTranslations = new TranslationList("Squad-wide -25% deployment cooldown"),
        CreditCost = 200,
        SquadDistributedMultiplier = 1f,
        SquadLeaderDistributedMultiplier = 1f,
        RequireSquadLeader = true,
        RequireSquad = true,
        Icon = "¦",
        Cooldown = 420,
        EffectDuration = 300,
        UnlockRequirements = new UnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 3 } },
        EffectDistributedToSquad = true,
        Data = "0.75"
    };

    private float _multiplier;
    protected override void StartEffect(bool onStart)
    {
        if (onStart && (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _multiplier)))
            _multiplier = 0.75f;
        base.StartEffect(onStart);
    }
    public static float GetDeployTime(UCPlayer player)
    {
        TraitData? d = DATA ??= TraitManager.GetData(typeof(RapidDeployment));
        if (d != null)
        {
            if (TraitManager.IsAffected(d, player, out Trait trait) && trait is RapidDeployment dep)
            {
                if (player.Steam64 == dep.TargetPlayer.Steam64)
                    return CooldownManager.Config.DeployFOBCooldown * dep._multiplier;
                else if (dep.TargetPlayer.IsSquadLeader())
                    return CooldownManager.Config.DeployFOBCooldown * dep._multiplier * dep.Data.SquadLeaderDistributedMultiplier;
                else
                    return CooldownManager.Config.DeployFOBCooldown * dep._multiplier * dep.Data.SquadDistributedMultiplier;
            }
        }

        return CooldownManager.Config.DeployFOBCooldown;
    }
}
