using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Traits;
public abstract class Buff : Trait
{
    protected abstract void StartEffect();
    protected abstract void ClearEffect();
    protected abstract void AddPlayer(UCPlayer player);
    protected abstract void RemovePlayer(UCPlayer player);
    protected override void OnActivate()
    {
        base.OnActivate();
        StartEffect();
    }
    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        ClearEffect();
    }
}
/// <summary>Shovel as fast as a combat engineer</summary>
public sealed class Motivated : Buff
{
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(Motivated),
        ClassList = new EClass[] { EClass.COMBAT_ENGINEER },
        ClassListIsBlacklist = true,
        NameTranslations = new TranslationList("Motivated"),
        DescriptionTranslations = new TranslationList("Shovel as fast as a combat engineer (2x faster)."),
        CreditCost = 150,
        EffectDuration = 300,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 4 } },
        DistributedToSquad = false
    };
    public static bool IsAffected(UCPlayer player) => TraitManager.IsAffectedOwner<Motivated>(player);

    protected override void AddPlayer(UCPlayer player)
    {
        throw new NotImplementedException();
    }
    protected override void RemovePlayer(UCPlayer player)
    {
        throw new NotImplementedException();
    }
    protected override void StartEffect()
    {
        throw new NotImplementedException();
    }
    protected override void ClearEffect()
    {
        throw new NotImplementedException();
    }
}