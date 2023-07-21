using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
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
        ActionMenuUI.ThankYou.OnClicked += ThankYou;
        ActionMenuUI.Sorry.OnClicked += Sorry;
        ActionMenuUI.HeliPickup.OnClicked += HeliPickup;
        ActionMenuUI.HeliDropoff.OnClicked += HeliDropoff;
        ActionMenuUI.SuppliesBuild.OnClicked += SuppliesBuild;
        ActionMenuUI.SuppliesAmmo.OnClicked += SuppliesAmmo;
        ActionMenuUI.AirSupport.OnClicked += AirSupport;
        ActionMenuUI.ArmorSupport.OnClicked += ArmorSupport;

        ActionMenuUI.LoadBuild.OnClicked += LoadBuild;
        ActionMenuUI.LoadAmmo.OnClicked += LoadAmmo;
        ActionMenuUI.UnloadBuild.OnClicked += UnloadBuild;
        ActionMenuUI.UnloadAmmo.OnClicked += UnloadAmmo;

        ActionMenuUI.Attack.OnClicked += Attack;
        ActionMenuUI.Defend.OnClicked += Defend;
        ActionMenuUI.Move.OnClicked += Move;
        ActionMenuUI.Build.OnClicked += Build;

        ActionMenuUI.AttackMarker.OnClicked += AttackMarker;
        ActionMenuUI.DefendMarker.OnClicked += DefendMarker;
        ActionMenuUI.MoveMarker.OnClicked += MoveMarker;
        ActionMenuUI.BuildMarker.OnClicked += BuildMarker;

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
        ActionMenuUI.ThankYou.OnClicked -= ThankYou;
        ActionMenuUI.Sorry.OnClicked -= Sorry;
        ActionMenuUI.HeliPickup.OnClicked -= HeliPickup;
        ActionMenuUI.HeliDropoff.OnClicked -= HeliDropoff;
        ActionMenuUI.SuppliesBuild.OnClicked -= SuppliesBuild;
        ActionMenuUI.SuppliesAmmo.OnClicked -= SuppliesAmmo;
        ActionMenuUI.AirSupport.OnClicked -= AirSupport;
        ActionMenuUI.ArmorSupport.OnClicked -= ArmorSupport;

        ActionMenuUI.LoadBuild.OnClicked -= LoadBuild;
        ActionMenuUI.LoadAmmo.OnClicked -= LoadAmmo;
        ActionMenuUI.UnloadBuild.OnClicked -= UnloadBuild;
        ActionMenuUI.UnloadAmmo.OnClicked -= UnloadAmmo;

        ActionMenuUI.Attack.OnClicked -= Attack;
        ActionMenuUI.Defend.OnClicked -= Defend;
        ActionMenuUI.Move.OnClicked -= Move;
        ActionMenuUI.Build.OnClicked -= Build;

        ActionMenuUI.AttackMarker.OnClicked -= AttackMarker;
        ActionMenuUI.DefendMarker.OnClicked -= DefendMarker;
        ActionMenuUI.MoveMarker.OnClicked -= MoveMarker;
        ActionMenuUI.BuildMarker.OnClicked -= BuildMarker;

        ActionMenuUI.Cancel.OnClicked -= Cancel;
    }
    private static bool _uiWarnSent;
    private static void OpenUI(UCPlayer player, ref bool handled)
    {
        if (!ActionMenuUI.Asset.ValidReference(out EffectAsset _))
        {
            if (!_uiWarnSent)
            {
                L.LogWarning("Skipping sending action UI, effect not found.");
                _uiWarnSent = true;
            }
            return;
        }
        if (player.IsActionMenuOpen || Data.Gamemode.LeaderboardUp())
            return;
        ActionMenuUI.SendToPlayer(player.Connection);

        player.IsActionMenuOpen = true;
        if (player.IsSquadLeader())
        {
            ActionMenuUI.SquadSection.SetVisibility(player.Connection, true);
        }
        
        FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(player.Position, player.GetTeam());
        if (fob != null)
        {
            // TODO: fully test this later
            ActionMenuUI.LogiSection.SetVisibility(player.Connection, true);
        }

        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
    }
    private static void CloseUI(UCPlayer player)
    {
        player.IsActionMenuOpen = false;
        ActionMenuUI.ClearFromPlayer(player.Connection);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
    }
    private static void Cancel(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        CloseUI(caller);
    }
    private static void NeedMedic(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Medic &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedMedic.Value, Gamemode.Config.EffectActionNearbyMedic.Value, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, EActionType.SIMPLE_REQUEST, T.NeedMedicChat, T.NeedMedicToast);
        action.Start();
        CloseUI(caller);
    }
    private static void NeedAmmo(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Rifleman &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedAmmo.Value, Gamemode.Config.EffectActionNearbyAmmo.Value, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, EActionType.SIMPLE_REQUEST, T.NeedAmmoChat, T.NeedAmmoToast);
        action.Start();
        CloseUI(caller);
    }
    private static void NeedRide(UnturnedButton button, Player player)
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

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedRide.Value, null, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, EActionType.SIMPLE_REQUEST, T.NeedRideChat, T.NeedRideToast)
        {
            CheckValid = () => !caller.IsInVehicle
        };
        action.Start();
        CloseUI(caller);
    }
    private static void NeedSupport(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionNeedSupport.Value, null, viewers, updateFrequency: 0.5f, lifeTime: 10, EActionOrigin.FOLLOW_CALLER, EActionType.SIMPLE_REQUEST, T.NeedSupportChat, T.NeedSupportToast);
        action.Start();
        CloseUI(caller);
    }
    private static void ThankYou(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;

        Action.SayTeam(caller, T.ThankYouChat);

        CloseUI(caller);
    }
    private static void Sorry(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;

        Action.SayTeam(caller, T.SorryChat);

        CloseUI(caller);
    }
    private static void HeliPickup(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, Gamemode.Config.EffectActionHeliPickup.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_POSITION, EActionType.SQUADLEADER_REQUEST, T.HeliPickupChat, T.HeliPickupToast, squadWide: true)
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
    private static void HeliDropoff(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, Gamemode.Config.EffectActionHeliDropoff.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 150, EActionOrigin.CALLER_MARKER, EActionType.SQUADLEADER_REQUEST, T.HeliDropoffChat, T.HeliDropoffToast, squadWide: true)
        {
            CheckValid = () =>
            {
                if (!(caller.IsInVehicle && caller.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(VehicleType.TransportAir)))
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
    private static void SuppliesBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(VehicleType.LogisticsGround)))) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(VehicleType.LogisticsGround))) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionSuppliesBuild.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 150, EActionOrigin.CALLER_POSITION, EActionType.SQUADLEADER_REQUEST, T.SuppliesBuildChat, T.SuppliesBuildToast, squadWide: true)
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
    private static void SuppliesAmmo(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(VehicleType.LogisticsGround)))) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            (p.KitClass == Class.Pilot || (p.IsDriver && p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsType(VehicleType.LogisticsGround))) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionSuppliesAmmo.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_POSITION, EActionType.SQUADLEADER_REQUEST, T.SuppliesAmmoChat, T.SuppliesAmmoToast, squadWide: true)
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
    private static void AirSupport(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, Gamemode.Config.EffectActionAirSupport.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, EActionType.SQUADLEADER_REQUEST, T.AirSupportChat, T.AirSupportToast, squadWide: true)
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
    private static void ArmorSupport(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            (p.GetTeam() == caller.GetTeam() &&
            p.CurrentVehicle != null &&
            p.CurrentVehicle.TryGetComponent(out VehicleComponent c) && c.IsArmor) ||
            p.IsInSameSquadAs(caller));

        IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p =>
            p.CurrentVehicle != null &&
            (p.CurrentVehicle!.TryGetComponent(out VehicleComponent c) && c.IsArmor) &&
            p.Player != caller);

        Action action = new Action(caller, Gamemode.Config.EffectActionArmorSupport.Value, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, EActionType.SQUADLEADER_REQUEST, T.ArmorSupportChat, T.ArmorSupportToast, squadWide: true)
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
    private static void UnloadBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryUnloadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Build);
        CloseUI(caller);
    }
    private static void UnloadAmmo(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryUnloadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Ammo);
        CloseUI(caller);
    }
    private static void LoadBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryLoadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Build, true);
        CloseUI(caller);
    }
    private static void LoadAmmo(UnturnedButton button, Player player) // WIP
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

        FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(caller.Position, caller.GetTeam());
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

                if (Points.PointsConfig.XPData.TryGetValue(XPReward.UnloadSupplies, out PointsConfig.XPRewardData data))
                {
                    int xp = data.Amount * Mathf.CeilToInt(difference / (float)REQUIRED_UNLOAD_AMOUNT_FOR_REWARD);

                    if (caller.KitClass == Class.Pilot)
                        xp *= 2;

                    Points.AwardXP(caller, XPReward.UnloadSupplies, xp);
                }
            }
        }
    }
    private static void TryLoadSupplies(UCPlayer caller, int amount, JsonAssetReference<ItemAsset>? supplyItem, bool build)
    {
        if (!supplyItem.ValidReference(out ItemAsset itemasset))
            return;

        FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(caller.Position, caller.GetTeam());
        InteractableVehicle? vehicle = caller.CurrentVehicle;
        if (vehicle != null && fob != null && vehicle.TryGetComponent(out VehicleComponent c) && c.IsLogistics)
        {
            amount = Mathf.Clamp(amount, 0, build ? fob.BuildSupply : fob.AmmoSupply);

            int successfullyAdded = 0;

            for (int i = 0; i < amount; i++)
            {
                if (vehicle.trunkItems.tryAddItem(new Item(itemasset, EItemOrigin.ADMIN)))
                    successfullyAdded++;
            }
            
            if (build)
            {
                fob.ModifyBuild(-successfullyAdded);
                FOBManager.ShowResourceToast(new LanguageSet(caller), build: -successfullyAdded, message: T.FOBResourceToastLoadSupplies.Translate(caller));
            }
            else
            {
                fob.ModifyAmmo(-successfullyAdded);
                FOBManager.ShowResourceToast(new LanguageSet(caller), ammo: -successfullyAdded, message: T.FOBResourceToastLoadSupplies.Translate(caller));
            }

            if (successfullyAdded > 0)
            {
                if ((build ? Gamemode.Config.EffectUnloadBuild : Gamemode.Config.EffectUnloadAmmo).ValidReference(out EffectAsset effect))
                {
                    F.TriggerEffectReliable(effect, EffectManager.MEDIUM, vehicle.transform.position);
                }
            }
        }
    }
    private static void Attack(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionAttack.Value, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, EActionType.ORDER, null, T.AttackToast, squadWide: true);
        action.CheckValid = () => !F.IsInMain(caller);
        action.Start();
        CloseUI(caller);
    }
    private static void Defend(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionDefend.Value, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, EActionType.ORDER, null, T.DefendToast, squadWide: true);
        action.CheckValid = () => !F.IsInMain(caller);
        action.Start();
        CloseUI(caller);
    }
    private static void Move(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionMove.Value, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, EActionType.ORDER, null, T.MoveToast, squadWide: true);
        action.CheckValid = () => !F.IsInMain(caller);
        action.Start();
        CloseUI(caller);
    }
    private static void Build(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionBuild, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_LOOK, EActionType.ORDER, null, T.BuildToast, squadWide: true);
        action.CheckValid = () => !F.IsInMain(caller);
        action.Start();
        CloseUI(caller);
    }
    private static void AttackMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionAttack.Value, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_MARKER, EActionType.ORDER, null, T.AttackToast, squadWide: true);
        action.CheckValid = () =>
        {
            if (!caller.Player.quests.isMarkerPlaced)
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorNoMarker);
                return false;
            }
            if (F.IsInMain(caller.Player.quests.markerPosition))
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                return false;
            }
            return true;
        };
        
        action.Start();
        CloseUI(caller);
    }
    private static void DefendMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionDefend.Value, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_MARKER, EActionType.ORDER, null, T.DefendToast, squadWide: true);
        action.CheckValid = () =>
        {
            if (!caller.Player.quests.isMarkerPlaced)
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorNoMarker);
                return false;
            }
            if (F.IsInMain(caller.Player.quests.markerPosition))
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                return false;
            }
            return true;
        };
        action.Start();
        CloseUI(caller);
    }
    private static void MoveMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionMove.Value, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_MARKER, EActionType.ORDER, null, T.MoveToast, squadWide: true);
        action.CheckValid = () =>
        {
            if (!caller.Player.quests.isMarkerPlaced)
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorNoMarker);
                return false;
            }
            if (F.IsInMain(caller.Player.quests.markerPosition))
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                return false;
            }
            return true;
        };
        action.Start();
        CloseUI(caller);
    }
    private static void BuildMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        var viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        var toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        var action = new Action(caller, Gamemode.Config.EffectActionBuild, null, viewers, updateFrequency: 1, lifeTime: 120, EActionOrigin.CALLER_MARKER, EActionType.ORDER, null, T.BuildToast, squadWide: true);
        action.CheckValid = () =>
        {
            if (!caller.Player.quests.isMarkerPlaced)
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorNoMarker);
                return false;
            }
            if (F.IsInMain(caller.Player.quests.markerPosition))
            {
                Tips.TryGiveTip(caller, 0, T.ActionErrorInMain);
                return false;
            }
            return true;
        };
        action.Start();
        CloseUI(caller);
    }
    private static void UAV(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        Squads.Commander.UAV.RequestUAV(caller);

        CloseUI(caller);
    }
}
