using System.Globalization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits.Buffs;

/// <summary>
/// Shovel buildables at 2x speed. Does not stack with squadmates or with combat engineer (you can not have this while having a <see cref="EClass.COMBAT_ENGINEER"/> kit).
/// </summary>
public sealed class Motivated : Buff
{
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(Motivated),
        ClassList = new EClass[] { EClass.COMBAT_ENGINEER },
        ClassListIsBlacklist = true,
        NameTranslations = new TranslationList("Motivated"),
        DescriptionTranslations = new TranslationList("Build like a combat engineer.\n<#f0a31c>2x Speed</color>"),
        CreditCost = 150,
        SquadDistributedMultiplier = 0.25f,
        SquadLeaderDistributedMultiplier = 0.5f,
        Icon = "ª",
        EffectDuration = 300,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 4 } },
        EffectDistributedToSquad = true,
        Data = "2"
    };
    private float _multiplier;
    private float _squadMultiplier;
    internal override void AddPlayer(UCPlayer player)
    {
        SetSpeedMultiplier(player, player.Steam64 == TargetPlayer.Steam64 ? _multiplier : _squadMultiplier);
        base.AddPlayer(player);
    }
    internal override void RemovePlayer(UCPlayer player)
    {
        player.ShovelSpeedMultipliers.Remove(this);
        base.RemovePlayer(player);
    }
    internal override void SquadLeaderDemoted()
    {
        if (!Data.EffectDistributedToSquad)
            return;
        Squad? sq = TargetPlayer.Squad;
        _squadMultiplier = (_multiplier - 1) * Data.SquadDistributedMultiplier + 1;
        if (sq is null) return;
        for (int i = 0; i < sq.Members.Count; ++i)
        {
            UCPlayer m = sq.Members[i];
            if (m.Steam64 != TargetPlayer.Steam64)
                SetSpeedMultiplier(m, _squadMultiplier);
        }
        base.SquadLeaderDemoted();
    }
    private void SetSpeedMultiplier(UCPlayer player, float value)
    {
        if (!player.ShovelSpeedMultipliers.ContainsKey(this))
            player.ShovelSpeedMultipliers.Add(this, value);
        else
            player.ShovelSpeedMultipliers[this] = value;
    }
    internal override void SquadLeaderPromoted()
    {
        if (!Data.EffectDistributedToSquad || TargetPlayer.Squad is null)
            return;
        Squad sq = TargetPlayer.Squad;
        _squadMultiplier = (_multiplier - 1) * Data.SquadLeaderDistributedMultiplier + 1;
        for (int i = 0; i < sq.Members.Count; ++i)
        {
            UCPlayer m = sq.Members[i];
            if (m.Steam64 != TargetPlayer.Steam64)
                SetSpeedMultiplier(m, _squadMultiplier);
        }
        base.SquadLeaderPromoted();
    }
    protected override void StartEffect()
    {
        if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _multiplier))
            _multiplier = 2f;

        _squadMultiplier = (_multiplier - 1) * (TargetPlayer.IsSquadLeader() ? Data.SquadLeaderDistributedMultiplier : Data.SquadDistributedMultiplier) + 1;

        if (_squadMultiplier < 0f)
            _squadMultiplier = 1f;
        base.StartEffect();
    }
}