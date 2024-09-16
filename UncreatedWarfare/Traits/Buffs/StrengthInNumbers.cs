using System;
using System.Globalization;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits.Buffs;

/// <summary>
/// Increases player armor by 10% when fighting near the rest of their squad.
/// </summary>
public class StrengthInNumbers : Buff
{
    private static TraitData? DATA;
    public static TraitData DefaultData = new TraitData()
    {
        TypeName = nameof(StrengthInNumbers),
        NameTranslations = new TranslationList("Strength in\nNumbers"),
        DescriptionTranslations = new TranslationList("+10% armor when fighting with your squad."),
        CreditCost = 300,
        SquadDistributedMultiplier = 0.75f,
        SquadLeaderDistributedMultiplier = 1f,
        RequireSquadLeader = true,
        RequireSquad = true,
        Icon = "¦",
        Cooldown = 330,
        EffectDuration = 300,
        UnlockRequirements = new UnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 2 } },
        EffectDistributedToSquad = true,
        // half of armor increase is base, other half is multiplied by nearby squadmembers count
        Data = "250,0.1" // distance threshold, armor increase % (from 0)
    };

    private float _distance = -1f;
    private float _armorMultiplier = -1f;
    private float _squadArmorMultiplier;

    internal override void SquadLeaderDemoted()
    {
        if (!Data.EffectDistributedToSquad)
            return;
        _squadArmorMultiplier = _armorMultiplier * Data.SquadDistributedMultiplier;
    }
    internal override void SquadLeaderPromoted()
    {
        if (!Data.EffectDistributedToSquad || TargetPlayer.Squad is null)
            return;
        _squadArmorMultiplier = _armorMultiplier * Data.SquadLeaderDistributedMultiplier;
    }
    internal static void OnPlayerDamageRequested(ref DamagePlayerParameters parameters)
    {
        if (!TraitManager.Loaded) return;
        UCPlayer? damaged = UCPlayer.FromPlayer(parameters.player);
        if (damaged is null || damaged.Squad is null) return;
        int nearby;
        for (int i = 0; i < damaged.ActiveTraits.Count; ++i)
        {
            if (damaged.ActiveTraits[i] is StrengthInNumbers num && num.IsActivated)
            {
                nearby = GetNearbySquadMembers(damaged, num._distance);
                if (nearby > 0)
                {
#if DEBUG
                    float old = parameters.damage;
#endif
                    parameters.damage /= 1f + (nearby * (num._armorMultiplier / 2f) + num._armorMultiplier / 2f);
#if DEBUG
                    L.LogDebug("Adjusted damage from " + old + " to " + parameters.damage);
#endif
                    return;
                }
            }
        }
        Squad? sq = damaged.Squad;
        if (sq is null) return;
        TraitData? data = DATA ??= TraitManager.GetData(typeof(StrengthInNumbers));
        if (data is null || !data.EffectDistributedToSquad) return;
        float dividend = -1f;
        nearby = -1;
        for (int i = 0; i < sq.Members.Count; ++i)
        {
            UCPlayer member = sq.Members[i];
            if (member.Steam64 == damaged.Steam64) continue;
            for (int j = 0; j < member.ActiveTraits.Count; ++j)
            {
                if (member.ActiveTraits[j] is StrengthInNumbers num && num.IsActivated)
                {
                    if (nearby == -1)
                        nearby = GetNearbySquadMembers(damaged, num._distance);
                    if (num._squadArmorMultiplier > dividend)
                        dividend = num._squadArmorMultiplier;
                }
            }
        }
        if (nearby > 0 && dividend != -1f)
        {
#if DEBUG
            float old = parameters.damage;
#endif
            parameters.damage /= 1f + (nearby * (dividend / 2f) + dividend / 2f);
#if DEBUG
            L.LogDebug("Adjusted damage from " + old + " to " + parameters.damage);
#endif
        }
    }

    private static int GetNearbySquadMembers(UCPlayer damaged, float distance)
    {
        if (damaged?.Squad is null) return 0;
        distance *= distance;
        int ct = 0;
        Vector3 pos = damaged.Position;
        for (int i = 0; i < damaged.Squad.Members.Count; ++i)
        {
            UCPlayer member = damaged.Squad.Members[i];
            if (member.Steam64 == damaged.Steam64) continue;
            if ((pos - member.Position).sqrMagnitude <= distance)
                ++ct;
        }
        return ct;
    }

    protected override void StartEffect(bool onStart)
    {
        if (onStart)
        {
            string[] datas = Data.Data is null ? Array.Empty<string>() : Data.Data.Split(DataSplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (datas.Length > 0)
            {
                float.TryParse(datas[0], NumberStyles.Number, Warfare.CultureInfo.InvariantCulture, out _distance);
                if (datas.Length > 1)
                    float.TryParse(datas[1], NumberStyles.Number, Warfare.CultureInfo.InvariantCulture, out _armorMultiplier);
            }

            if (_distance == -1f)
                _distance = 250f;
            if (_armorMultiplier == -1f)
                _armorMultiplier = 0.1f;
            _squadArmorMultiplier = _armorMultiplier * (TargetPlayer.IsSquadLeader() ? Data.SquadLeaderDistributedMultiplier : Data.SquadDistributedMultiplier);
        }
        base.StartEffect(onStart);
    }
}
