using System.Globalization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits.Buffs;

/// <summary>
/// Increases the accuracy of ground AA.
/// </summary>
public class GuidedByGod : Buff
{
    private static TraitData? DATA;
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(GuidedByGod),
        NameTranslations = new TranslationList("Guided by God"),
        DescriptionTranslations = new TranslationList("More accurate ground AA."),
        CreditCost = 500,
        RequireSquadLeader = false,
        RequireSquad = false,
        ClassList = new Class[] { Class.Pilot },
        ClassListIsBlacklist = true,
        Icon = "£",
        Cooldown = 1200,
        EffectDuration = 600,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 5 } },
        EffectDistributedToSquad = false,
        Data = "0.15"
    };

    private float _multiplier = -1f;
    private float _squadMultiplier = -1f;
    protected override void StartEffect(bool onStart)
    {
        if (onStart)
        {
            if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _multiplier))
                _multiplier = 0.15f;

            _squadMultiplier = Data.EffectDistributedToSquad
                ? _multiplier *
                  (TargetPlayer.IsSquadLeader()
                      ? Data.SquadLeaderDistributedMultiplier
                      : Data.SquadDistributedMultiplier)
                : 0f;
        }
        base.StartEffect(onStart);
    }
    public static float GetMultiplier(UCPlayer player)
    {
        if (player is null || !TraitManager.Loaded || player.Player.life.isDead) return 1f;
        TraitData? d = DATA ??= TraitManager.GetData(typeof(GuidedByGod));
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is GuidedByGod guided && guided.IsActivated)
                return 1f + guided._multiplier;
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
                if (player.ActiveTraits[i] is GuidedByGod guided && guided.IsActivated)
                {
                    if (max < guided._squadMultiplier)
                        max = guided._squadMultiplier;
                }
            }
        }

        return max == -1f ? 1f : (1 + max);
    }
}