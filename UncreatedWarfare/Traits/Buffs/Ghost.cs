using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits.Buffs;

/// <summary>
/// Makes the player invisible to UAVs and laser designators. Doesn't work when they're in a vehicle.
/// </summary>
public class Ghost : Buff
{
    private static TraitData? DATA;
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(Ghost),
        NameTranslations = new TranslationList("Ghost"),
        DescriptionTranslations = new TranslationList("Hide from laser designators and UAVs."),
        CreditCost = 500,
        RequireSquadLeader = false,
        RequireSquad = false,
        ClassList = new Class[] { Class.Pilot, Class.Crewman },
        ClassListIsBlacklist = true,
        Icon = "£",
        Cooldown = 900,
        EffectDuration = 600,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 5 } },
        EffectDistributedToSquad = false,
        Data = string.Empty
    };

    public static bool IsHidden(UCPlayer player)
    {
        if (player is null || !TraitManager.Loaded || player.Player.movement.getVehicle() != null) return false;
        if (player.Player.life.isDead) return true;
        TraitData? d = DATA ??= TraitManager.GetData(typeof(Ghost));
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Ghost ghost && ghost.IsActivated)
                return true;
        }

        Squad? s;
        if (d is null || !d.EffectDistributedToSquad || (s = player.Squad) is null) return false;

        for (int i = 0; i < s.Members.Count; ++i)
        {
            UCPlayer member = s.Members[i];
            if (member.Steam64 == player.Steam64) continue;
            for (int j = 0; j < member.ActiveTraits.Count; ++j)
            {
                if (member.ActiveTraits[j] is Ghost ghost && ghost.IsActivated)
                    return true;
            }
        }

        return false;
    }
}
