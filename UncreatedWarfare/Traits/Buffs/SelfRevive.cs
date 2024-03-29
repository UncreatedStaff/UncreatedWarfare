﻿using System.Globalization;
using Uncreated.Warfare.Players.Unlocks;

namespace Uncreated.Warfare.Traits.Buffs;

/// <summary>
/// Gives the player a trait that allows them to revive themselves once.
/// </summary>
public class SelfRevive : Buff
{
    public static TraitData DefaultData = new TraitData()
    {
        TypeName = nameof(SelfRevive),
        NameTranslations = new TranslationList("Self-Revive"),
        DescriptionTranslations = new TranslationList("Revive yourself. Expires on death."),
        CreditCost = 1000,
        Icon = "¢",
        Cooldown = 600,
        LastsUntilDeath = true,
        UnlockRequirements = new UnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 8 } },
        EffectDistributedToSquad = false,
        Data = "5" // self revive cooldown after being downed
    };

    public float Cooldown;
    protected override void StartEffect(bool onStart)
    {
        if (onStart && (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.AdminLocale, out Cooldown)))
            Cooldown = 5f;
        base.StartEffect(onStart);
    }
    public void Consume()
    {
        TargetPlayer.SendChat(T.TraitUsedSelfRevive, Data);
        Destroy(this);
    }
    public static bool HasSelfRevive(UCPlayer player, out SelfRevive buff)
    {
        buff = null!;
        if (player is null || !TraitManager.Loaded) return false;
        if (player.Player.life.isDead) return true;
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is SelfRevive sr && sr.IsActivated)
            {
                buff = sr;
                return true;
            }
        }

        return false;
    }
}
