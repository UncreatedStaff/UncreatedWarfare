using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Actions;

public class ActionManager : BaseSingleton
{
    public static readonly ActionMenuUI ActionMenuUI = new ActionMenuUI();
    public static ActionManager Singleton;

    public override void Load()
    {
        UCPlayerKeys.SubscribeKeyDown(OpenUI, Data.Keys.ActionMenu);

        ActionMenuUI.NeedMedic.OnClicked += NeedMedic;
        ActionMenuUI.NeedAmmo.OnClicked += NeedAmmo;
        ActionMenuUI.NeedRide.OnClicked += NeedRide;
        ActionMenuUI.NeedSupport.OnClicked += NeedSupport;
        ActionMenuUI.HeliPickup.OnClicked += HeliPickup;
        ActionMenuUI.HeliDropoff.OnClicked += HeliDropoff;
        ActionMenuUI.SuppliesBuild.OnClicked += SuppliesBuild;
        ActionMenuUI.SuppliesAmmo.OnClicked += SuppliesAmmo;
        ActionMenuUI.AirSupport.OnClicked += SuppliesBuild;
        ActionMenuUI.ArmorSupport.OnClicked += SuppliesAmmo;

        ActionMenuUI.Cancel.OnClicked += Cancel;
        Singleton = this;
    }

    public override void Unload()
    {
        Singleton = null!;
        UCPlayerKeys.UnsubscribeKeyDown(OpenUI, Data.Keys.ActionMenu);

        ActionMenuUI.NeedMedic.OnClicked -= NeedMedic;
        ActionMenuUI.NeedAmmo.OnClicked -= NeedAmmo;
        ActionMenuUI.NeedRide.OnClicked -= NeedRide;
        ActionMenuUI.NeedSupport.OnClicked -= NeedSupport;
        ActionMenuUI.HeliPickup.OnClicked -= HeliPickup;
        ActionMenuUI.HeliDropoff.OnClicked -= HeliDropoff;
        ActionMenuUI.SuppliesBuild.OnClicked -= SuppliesBuild;
        ActionMenuUI.SuppliesAmmo.OnClicked -= SuppliesAmmo;
        ActionMenuUI.AirSupport.OnClicked -= SuppliesBuild;
        ActionMenuUI.ArmorSupport.OnClicked -= SuppliesAmmo;

        ActionMenuUI.Cancel.OnClicked -= Cancel;
    }
    private static bool _uiWarnSent;
    public static void OpenUI(UCPlayer player, ref bool handled)
    {
        if (!ActionMenuUI.UIAsset.ValidReference(out EffectAsset _))
        {
            if (!_uiWarnSent)
            {
                L.LogWarning("Skipping sending action UI, effect not found.");
                _uiWarnSent = true;
            }
            return;
        }
        if (player.IsActionMenuOpen)
            return;
        ActionMenuUI.SendToPlayer(player.Connection);

        player.IsActionMenuOpen = true;
        if (player.IsSquadLeader())
        {
            ActionMenuUI.SquadSection.SetVisibility(player.Connection, true);
        }
        FOB? fob = FOB.GetNearestFOB(player.Position, EfobRadius.FULL_WITH_BUNKER_CHECK, player.GetTeam());
        if (fob != null)
        {
            ActionMenuUI.LogiSection.SetVisibility(player.Connection, true);
        }

        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
    }
    public static void CloseUI(UCPlayer player)
    {
        player.IsActionMenuOpen = false;
        ActionMenuUI.ClearFromPlayer(player.Connection);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
    }
    public static void Cancel(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        CloseUI(caller);
    }
    public static void NeedMedic(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Medic &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedMedic.Value, Gamemode.Config.EffectActionNearbyMedic.Value, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, T.NeedMedicChat, T.NeedMedicToast);
        action.Start();
        CloseUI(caller);
    }

    public static void NeedAmmo(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Rifleman &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedAmmo.Value, Gamemode.Config.EffectActionNearbyAmmo.Value, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, T.NeedAmmoChat, T.NeedAmmoToast);
        action.Start();
        CloseUI(caller);
    }

    public static void NeedRide(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.IsDriver && 
            p.CurrentVehicle!.TryGetComponent(out VehicleComponent v) && 
            v.CanTransport &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedRide.Value, null, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, T.NeedRideChat, T.NeedRideToast)
        {
            CheckValid = () => !caller.IsInVehicle
        };
        action.Start();
        CloseUI(caller);
    }

    public static void NeedSupport(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedSupport.Value, null, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, T.NeedSupportChat, T.NeedSupportToast);
        action.Start();
        CloseUI(caller);
    }

    public static void HeliPickup(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Pilot) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Pilot &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionHeliPickup.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_POSITION, T.HeliPickupChat, T.HeliPickupToast, squadWide: true)
        {
            CheckValid = () =>
            {
                if (F.IsInMain(caller))
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                    return false;
                }
                if (caller.IsInVehicle)
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInVehicle);
                    return false;
                }
                return true;
            }
        };
        action.Start();
        CloseUI(caller);
    }

    public static void HeliDropoff(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Pilot) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            p.IsDriver &&
            p.IsInSameVehicleAs(caller) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionHeliDropoff.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 150, EActionOrigin.CALLER_MARKER, T.HeliDropoffChat, T.HeliDropoffToast, squadWide: true)
        {
            CheckValid = () =>
            {
                if (!(caller.IsInVehicle && caller.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(EVehicleType.HELI_TRANSPORT)))
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorNotInHeli);
                    return false;
                }
                if (!caller.Player.quests.isMarkerPlaced)
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorNoMarker);
                    return false;
                }
                return true;
            }
        };
        action.LoopCheckComplete = () => (action.InitialPosition?? - caller.Position).sqrMagnitude < Math.Pow(50, 2);
        action.Start();
        CloseUI(caller);
    }
    public static void SuppliesBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(EVehicleType.LOGISTICS)))) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(EVehicleType.LOGISTICS))) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionSuppliesBuild.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 150, EActionOrigin.CALLER_POSITION, T.SuppliesBuildChat, T.SuppliesBuildToast, squadWide: true)
        {
            CheckValid = () =>
            {
                if (F.IsInMain(caller))
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                    return false;
                }
                if (caller.IsInVehicle)
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInVehicle);
                    return false;
                }
                return true;
            }
        };
        action.LoopCheckComplete = () =>
        {
            foreach (UCPlayer? viewer in toastReceivers)
            {
                if ((action.InitialPosition?? - viewer.Position).sqrMagnitude < Math.Pow(50, 2))
                    return true;
            }
            return false;
        };
        action.Start();
        CloseUI(caller);
    }
    public static void SuppliesAmmo(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(EVehicleType.LOGISTICS)))) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(EVehicleType.LOGISTICS))) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionSuppliesAmmo.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_POSITION, T.SuppliesAmmoChat, T.SuppliesAmmoToast, squadWide: true)
        {
            CheckValid = () =>
            {
                if (F.IsInMain(caller))
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                    return false;
                }
                if (caller.IsInVehicle)
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInVehicle);
                    return false;
                }
                return true;
            }
        };
        action.LoopCheckComplete = () =>
        {
            foreach (UCPlayer? viewer in toastReceivers)
            {
                if ((action.InitialPosition?? - viewer.Position).sqrMagnitude < Math.Pow(50, 2))
                    return true;
            }
            return false;
        };
        action.Start();
        CloseUI(caller);
    }
    public static void AirSupport(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
             p.IsInVehicle && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsAssaultAircraft) ||
             p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            (p.IsInVehicle && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsAssaultAircraft) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionSuppliesAmmo.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, T.AirSupportChat, T.AirSupportToast, squadWide: true)
        {
            CheckValid = () =>
            {
                if (F.IsInMain(caller))
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                    return false;
                }
                if (caller.IsInVehicle)
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInVehicle);
                    return false;
                }
                return true;
            }
        };
        action.Start();
        CloseUI(caller);
    }
    public static void ArmorSupport(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsArmor) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            (p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsArmor) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionSuppliesAmmo.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, T.ArmorSupportChat, T.ArmorSupportToast, squadWide: true)
        {
            CheckValid = () =>
            {
                if (F.IsInMain(caller))
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                    return false;
                }
                if (caller.IsInVehicle)
                {
                    Tips.TryGiveTip(caller, 0, T.ActionErrorInVehicle);
                    return false;
                }
                return true;
            }
        };
        action.Start();
        CloseUI(caller);
    }
    
    public static void UnloadBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryUnloadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Build);
        CloseUI(caller);
    }
    public static void UnloadAmmo(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryUnloadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Ammo);
        CloseUI(caller);
    }
    public static void LoadBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryLoadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Build, true);
        CloseUI(caller);
    }
    public static void LoadAmmo(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryLoadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Ammo, false);
        CloseUI(caller);
    }
    
    const int REQUIRED_UNLOAD_AMOUNT_FOR_REWARD = 5;
    private static void TryUnloadSupplies(UCPlayer caller, int amount, JsonAssetReference<ItemAsset>? buildOrAmmo)
    {
        if (buildOrAmmo is null || !buildOrAmmo.Exists)
            return;

        FOB? fob = FOB.GetNearestFOB(caller.Position, EfobRadius.FULL_WITH_BUNKER_CHECK, caller.GetTeam());
        InteractableVehicle? vehicle = caller.CurrentVehicle;
        if (vehicle == null)
            return;
        if (fob != null && vehicle.TryGetComponent(out VehicleComponent c) && c.IsLogistics)
        {
            int removed = 0;

            for (int i = 0; i < vehicle.trunkItems.getItemCount(); i++)
            {
                ItemJar item = vehicle.trunkItems.items[i];
                if (item.item.id == buildOrAmmo.Id)
                {
                    removed++;
                    ItemManager.dropItem(new Item(item.item.id, true), vehicle.transform.position, false, true, true);
                    vehicle.trunkItems.removeItem(vehicle.trunkItems.getIndex(item.x, item.y));
                    i--;
                }
                if (removed > amount)
                    break;
            }

            caller.SuppliesUnloaded += removed;
            int difference = caller.SuppliesUnloaded - REQUIRED_UNLOAD_AMOUNT_FOR_REWARD;
            if (difference >= 0)
            {
                caller.SuppliesUnloaded = -difference;

                int xp = Points.XPConfig.UnloadSuppliesXP * Mathf.CeilToInt(difference / (float)REQUIRED_UNLOAD_AMOUNT_FOR_REWARD);

                if (caller.KitClass == Class.Pilot)
                    xp *= 2;

                Points.AwardXP(caller, xp, T.XPToastSuppliesUnloaded);
            }
        }
    }
    private static void TryLoadSupplies(UCPlayer caller, int amount, JsonAssetReference<ItemAsset>? supplyItem, bool build)
    {
        if (!supplyItem.ValidReference(out ItemAsset itemasset))
            return;

        FOB? fob = FOB.GetNearestFOB(caller.Position, EfobRadius.FULL_WITH_BUNKER_CHECK, caller.GetTeam());
        InteractableVehicle? vehicle = caller.CurrentVehicle;
        if (vehicle != null && fob != null && vehicle.TryGetComponent(out VehicleComponent c) && c.IsLogistics)
        {
            amount = Mathf.Clamp(amount, 0, build ? fob.Build : fob.Ammo);

            int successfullyAdded = 0;

            for (int i = 0; i < amount; i++)
            {
                if (vehicle.trunkItems.tryAddItem(new Item(itemasset.id, true)))
                    successfullyAdded++;
            }
            
            if (build)
                fob.ReduceBuild(successfullyAdded);
            else
                fob.ReduceAmmo(successfullyAdded);

            if (successfullyAdded > 0)
            {
                if ((build ? Gamemode.Config.EffectUnloadBuild : Gamemode.Config.EffectUnloadAmmo).ValidReference(out EffectAsset effect))
                {
                    F.TriggerEffectReliable(effect, EffectManager.MEDIUM, vehicle.transform.position);
                }
            }
        }
    }
}
