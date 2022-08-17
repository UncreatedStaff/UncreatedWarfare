using System.Globalization;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Traits.Buffs;
public class BadOmen : Buff
{
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(BadOmen),
        NameTranslations = new TranslationList("Bad Omen"),
        DescriptionTranslations = new TranslationList("6th sense for incoming shells."),
        CreditCost = 350,
        Icon = "¯",
        EffectDuration = 600,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 6 } },
        EffectDistributedToSquad = false,
        Data = "15" // max notice given in seconds
    };
    private float _maxNotice;
    private float _squadMaxNotice;
    protected override void StartEffect()
    {
        if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _maxNotice))
            _maxNotice = 15f;

        _squadMaxNotice = Data.EffectDistributedToSquad
            ? _maxNotice *
              (TargetPlayer.IsSquadLeader()
                ? Data.SquadLeaderDistributedMultiplier
                : Data.SquadDistributedMultiplier)
            : 0f;
        base.StartEffect();
    }
    internal override void SquadLeaderDemoted()
    {
        if (!Data.EffectDistributedToSquad || TargetPlayer.Squad is null)
            return;
        _squadMaxNotice = _maxNotice * Data.SquadDistributedMultiplier;
    }
    internal override void SquadLeaderPromoted()
    {
        if (!Data.EffectDistributedToSquad || TargetPlayer.Squad is null)
            return;
        _squadMaxNotice = _maxNotice * Data.SquadLeaderDistributedMultiplier;
    }
    public static bool CanSeeIncomingMortars(UCPlayer player)
    {
        TraitData? d = TraitManager.GetData(typeof(BadOmen));
        return d != null && TraitManager.IsAffected(d, player, out _);
    }
    public static void MortarShot(UCPlayer player, Vector3 position, Vector3 direction)
    {

    }
}
