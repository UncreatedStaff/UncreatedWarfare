using System.Globalization;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Traits.Buffs;
public class RapidDeployment : Buff
{
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(RapidDeployment),
        NameTranslations = new TranslationList("Rapid Deployment"),
        DescriptionTranslations = new TranslationList("You and your squad have a 25% shorter FOB deployment cooldown."),
        CreditCost = 200,
        SquadDistributedMultiplier = 1f,
        SquadLeaderDistributedMultiplier = 1f,
        RequireSquadLeader = true,
        RequireSquad = true,
        Icon = "¦",
        EffectDuration = 300,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 3 } },
        EffectDistributedToSquad = true,
        Data = "0.75"
    };

    private float _multiplier;
    protected override void StartEffect()
    {
        if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _multiplier))
            _multiplier = 0.75f;
        base.StartEffect();
    }
    public static float GetDeployTime(UCPlayer player)
    {
        TraitData? d = TraitManager.GetData(typeof(RapidDeployment));
        if (d != null)
        {
            if (TraitManager.IsAffectedSquad(d, player, out Trait trait) && trait is RapidDeployment dep)
                return CooldownManager.Config.DeployFOBCooldown * dep._multiplier;
        }

        return CooldownManager.Config.DeployFOBCooldown;
    }
}
