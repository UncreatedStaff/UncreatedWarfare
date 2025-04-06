using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Throwables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Vehicle;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public class ThrowableSupplyCrateTweaks : IEventListener<ThrowableSpawned>
{
    public void HandleEvent(ThrowableSpawned e,  IServiceProvider serviceProvider)
    {
        FobManager? fobManager = serviceProvider.GetService<FobManager>();
        if (fobManager == null)
            return;
        
        bool isInMain = serviceProvider.GetService<ZoneStore>()?.IsInMainBase(e.Object.transform.position) ?? false;

        ThrownAmmoBagInfo? thrownAmmoBagInfo = fobManager.Configuration.ThrowableAmmoBags.FirstOrDefault(t => t.ThrowableItemAsset.MatchAsset(e.Asset));
        if (thrownAmmoBagInfo != null)
        {
            new ThrownAmmoBag(e.Object, e.Player, e.Asset, thrownAmmoBagInfo.AmmoBagBarricadeAsset.GetAssetOrFail(), serviceProvider, thrownAmmoBagInfo.StartingAmmo, isInMain);
        }
        ThrownVehicleCrateInfo? thrownVehicleCrateInfo = fobManager.Configuration.ThrowableVehicleSupplyCrates.FirstOrDefault(t => t.ThrowableItemAsset.MatchAsset(e.Asset));
        if (thrownVehicleCrateInfo != null)
        {
            AmmoTranslations ammoTranslations = serviceProvider.GetRequiredService<TranslationInjection<AmmoTranslations>>().Value;
            ZoneStore? zoneStore = serviceProvider.GetService<ZoneStore>();
            
            new ThrownVehicleCrate(e.Object, e.Player, e.Asset, thrownVehicleCrateInfo.ResupplyEffect.GetAssetOrFail(), fobManager, zoneStore, ammoTranslations);
        }
    }
}