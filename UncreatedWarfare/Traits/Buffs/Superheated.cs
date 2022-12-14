using System.Globalization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits.Buffs;

/// <summary>
/// Increases the influence that flares have on heat-seeking missiles.
/// </summary>
public class Superheated : Buff
{
    private static TraitData? DATA;
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(Superheated),
        NameTranslations = new TranslationList("Superheated"),
        DescriptionTranslations = new TranslationList("Flares are more distracting."),
        CreditCost = 600,
        RequireSquadLeader = false,
        RequireSquad = false,
        ClassList = new Class[] { Class.Pilot },
        ClassListIsBlacklist = false,
        Icon = "£",
        Cooldown = 900,
        EffectDuration = 600,
        UnlockRequirements = new UnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 7 } },
        EffectDistributedToSquad = false,
        Data = "0.2"
    };

    private float _multiplier = -1f;
    private float _squadMultiplier = -1f;
    protected override void StartEffect(bool onStart)
    {
        if (onStart)
        {
            if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _multiplier))
                _multiplier = 0.2f;

            _squadMultiplier = Data.EffectDistributedToSquad
                ? _multiplier *
                  (TargetPlayer.IsSquadLeader()
                      ? Data.SquadLeaderDistributedMultiplier
                      : Data.SquadDistributedMultiplier)
                : 0f;
        }
        base.StartEffect(onStart);
    }
    internal override void SquadLeaderDemoted()
    {
        if (!Data.EffectDistributedToSquad)
            return;
        _squadMultiplier = _multiplier * Data.SquadDistributedMultiplier;
    }
    internal override void SquadLeaderPromoted()
    {
        if (!Data.EffectDistributedToSquad || TargetPlayer.Squad is null)
            return;
        _squadMultiplier = _multiplier * Data.SquadLeaderDistributedMultiplier;
    }
    public static float GetMultiplier(UCPlayer player)
    {
        if (player is null || !TraitManager.Loaded || player.Player.life.isDead) return 1f;
        TraitData? d = DATA ??= TraitManager.GetData(typeof(Superheated));
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Superheated sh && sh.IsActivated)
                return 1f + sh._multiplier;
        }

        Squad? s;
        if (d is null || !d.EffectDistributedToSquad || (s = player.Squad) is null) return 1f;

        float max = -1f;
        for (int i = 0; i < s.Members.Count; ++i)
        {
            UCPlayer member = s.Members[i];
            if (member.Steam64 == player.Steam64) continue;
            for (int j = 0; j < member.ActiveTraits.Count; ++j)
            {
                if (player.ActiveTraits[i] is Superheated sh && sh.IsActivated)
                {
                    if (max < sh._squadMultiplier)
                        max = sh._squadMultiplier;
                }
            }
        }

        return max == -1f ? 1f : (1 + max);
    }
}