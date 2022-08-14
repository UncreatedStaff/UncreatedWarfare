using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;

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
    protected override void ClearEffect()
    {
        throw new NotImplementedException();
    }

    protected override void StartEffect()
    {
        if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _multiplier))
            _multiplier = 0.75f;
        base.StartEffect();
    }
}
