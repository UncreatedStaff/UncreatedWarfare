using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags.MedicBags;

public class PlacedMedicBagComponent : PlacedBagComponent, ICombatSupply
{
    public override IAssetLink<EffectAsset> GetIconEffect(AssetConfiguration assetConfig) => assetConfig.GetAssetLink<EffectAsset>("Effects:Fobs:Ammo");
    public float HealingPointsCount { get; private set; }

    public void Init(WarfarePlayer warfarePlayer, IBuildable buildable, Team team, IServiceProvider serviceProvider,
        float startingHealingPoints)
    {
        InitBase(warfarePlayer, buildable, team, serviceProvider);
        HealingPointsCount = startingHealingPoints;
    }
    
    public void SubtractHealingPoints(float healingPointsCount)
    {
        HealingPointsCount -= healingPointsCount;
        
        if (HealingPointsCount <= 0)
        {
            HealingPointsCount = 0;
            DestroyNextFrame();
        }
    }
    /// <inheritdoc />
    public override string ToString()
    {
        return AssetLink.ToDisplayString(_Buildable.Asset) + $" ({HealingPointsCount:F2} ammo)";
    }

    public void HealPlayerRoutine(WarfarePlayer player, float rearmCost, KitRequestService kitRequestService, ChatService chatService, MedicBagTranslations translations, AssetConfiguration assetConfig)
    {
        Func<ItemAsset, bool> medicalItemFilter = itemAsset => itemAsset is ItemMedicalAsset;
            
        // STEPS:
        // add 16 HP every 1 second
        // stop if medic

        if (player.IsDisconnected || player.UnturnedPlayer.life.isDead || player.IsInjured() ||
            !MathUtility.WithinRange(player.Position, transform.position, HealRadius))
        {
            // stop routine and cancel healing 
            return;
        }
        
        int healthBefore = player.UnturnedPlayer.life.health;
        player.UnturnedPlayer.life.serverModifyHealth(HealthPerTick);
        int healthChange = player.UnturnedPlayer.life.health - healthBefore;
        
        SubtractHealingPoints(healthChange);

        if (player.UnturnedPlayer.life.health == Provider.modeConfigData.Players.Health_Default || HealingPointsCount <= 0)
        {
            if (healthChange == 0 && rearmCost == 0)
            {
                // "You are already full health, and are not missing any medical equipment."
            }
            
            if (rearmCost > 0)
            {
                SubtractHealingPoints(MedicItemAmmoCostMultiplier);
                // silently resupply meds
                kitRequestService.RestockMatchingEquipmentAsync(player, medicalItemFilter, resupplyAmmoBags: false);
            }

            if (healthChange > 0 && rearmCost > 0)
            {
                chatService.Send(e.Player, translations.AmmoResuppliedKitInfinite);
                player.SendToast(new ToastMessage(ToastMessageStyle.Tip, translationService.ToastLoseMedicBagResuppliedMeds.Translate(rearmCost, player)));

                // "You have been healed to full health. Missing field dressings and medical items have been resupplied. Medic Bag: {}/{} health supplies left"
            }
            else if (healthChange > 0 && rearmCost == 0)
            {
                // "You have been healed to full health. Medic Bag: {}/{} health supplies left"
            }
            else if (healthChange == 0 && rearmCost == 0)
            {
                // "Field dressings and medical items has been resupplied."
            }
            
            EffectUtility.TriggerEffect(
                assetConfig.GetAssetLink<EffectAsset>("Effects:ResupplyMedicBag").GetAssetOrFail(),
                EffectManager.SMALL,
                player.Position,
                true
            );
        }
    }

    private const float HealRadius = 5;
    private const float HealthPerTick = 16;
    private const float MedicItemAmmoCostMultiplier = 100; // decides how many HealingPoints are used up by resupplying bandages.
}