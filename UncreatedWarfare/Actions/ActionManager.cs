using Cysharp.Threading.Tasks;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Actions;

/// <summary>
/// Handles the action menu.
/// </summary>
public class ActionManager : ISessionHostedService
{
    private readonly ActionMenuUI _ui;
    public ActionManager()
    {
        _ui = new ActionMenuUI();

        _ui.NeedMedic.OnClicked += NeedMedic;
        _ui.NeedAmmo.OnClicked += NeedAmmo;
        _ui.NeedRide.OnClicked += NeedRide;
        _ui.NeedSupport.OnClicked += NeedSupport;
        _ui.ThankYou.OnClicked += ThankYou;
        _ui.Sorry.OnClicked += Sorry;
        _ui.HeliPickup.OnClicked += HeliPickup;
        _ui.HeliDropoff.OnClicked += HeliDropoff;
        _ui.SuppliesBuild.OnClicked += SuppliesBuild;
        _ui.SuppliesAmmo.OnClicked += SuppliesAmmo;
        _ui.AirSupport.OnClicked += AirSupport;
        _ui.ArmorSupport.OnClicked += ArmorSupport;

        _ui.LoadBuild.OnClicked += LoadBuild;
        _ui.LoadAmmo.OnClicked += LoadAmmo;
        _ui.UnloadBuild.OnClicked += UnloadBuild;
        _ui.UnloadAmmo.OnClicked += UnloadAmmo;

        _ui.Attack.OnClicked += Attack;
        _ui.Defend.OnClicked += Defend;
        _ui.Move.OnClicked += Move;
        _ui.Build.OnClicked += Build;

        _ui.AttackMarker.OnClicked += AttackMarker;
        _ui.DefendMarker.OnClicked += DefendMarker;
        _ui.MoveMarker.OnClicked += MoveMarker;
        _ui.BuildMarker.OnClicked += BuildMarker;

        _ui.Cancel.OnClicked += Cancel;
    }

    public async UniTask StartAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread();

        UCPlayerKeys.SubscribeKeyDown(OpenUI, Data.Keys.ActionMenu);
    }

    public async UniTask StopAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread();

        UCPlayerKeys.UnsubscribeKeyDown(OpenUI, Data.Keys.ActionMenu);
        
        Provider.clients.ForEach(_ui.ClearFromPlayer);
    }

    private void OpenUI(UCPlayer player, ref bool handled)
    {
        if (!_ui.HasAssetOrId)
            return;
        
        if (player.IsActionMenuOpen || Data.Gamemode.LeaderboardUp())
            return;

        _ui.SendToPlayer(player.Connection);

        player.IsActionMenuOpen = true;
        if (player.IsSquadLeader())
        {
            _ui.SquadSection.SetVisibility(player.Connection, true);
        }

        FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(player.Position, player.GetTeam());
        if (fob != null)
        {
            // TODO: fully test this later
            _ui.LogiSection.SetVisibility(player.Connection, true);
        }

        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
    }
    private void CloseUI(UCPlayer player)
    {
        player.IsActionMenuOpen = false;
        _ui.ClearFromPlayer(player.Connection);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
    }
    private void Cancel(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);

        if (caller != null)
            CloseUI(caller);
    }
    private void NeedMedic(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Medic &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionNeedMedic, GamemodeOld.Config.EffectActionNearbyMedic, viewers, updateFrequency: 0.5f, lifeTime: 10, ActionOrigin.FollowCaller, ActionType.SimpleRequest, T.NeedMedicChat, T.NeedMedicToast);
        action.Start();
        CloseUI(caller);
    }
    private void NeedAmmo(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            p.KitClass == Class.Rifleman &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionNeedAmmo, GamemodeOld.Config.EffectActionNearbyAmmo, viewers, updateFrequency: 0.5f, lifeTime: 10, ActionOrigin.FollowCaller, ActionType.SimpleRequest, T.NeedAmmoChat, T.NeedAmmoToast);
        action.Start();
        CloseUI(caller);
    }
    private void NeedRide(UnturnedButton button, Player player)
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

        Action action = new Action(caller, GamemodeOld.Config.EffectActionNeedRide, null, viewers, updateFrequency: 0.5f, lifeTime: 10, ActionOrigin.FollowCaller, ActionType.SimpleRequest, T.NeedRideChat, T.NeedRideToast)
        {
            CheckValid = () => !caller.IsInVehicle
        };
        action.Start();
        CloseUI(caller);
    }
    private void NeedSupport(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p =>
            p.GetTeam() == caller.GetTeam() &&
            (p.Position - caller.Position).sqrMagnitude < Math.Pow(100, 2) &&
            p.Player != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionNeedSupport, null, viewers, updateFrequency: 0.5f, lifeTime: 10, ActionOrigin.FollowCaller, ActionType.SimpleRequest, T.NeedSupportChat, T.NeedSupportToast);
        action.Start();
        CloseUI(caller);
    }
    private void ThankYou(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;

        Action.SayTeam(caller, T.ThankYouChat);

        CloseUI(caller);
    }
    private void Sorry(UnturnedButton button, Player player)
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;

        Action.SayTeam(caller, T.SorryChat);

        CloseUI(caller);
    }
    private void HeliPickup(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, GamemodeOld.Config.EffectActionHeliPickup, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerPosition, ActionType.SquadleaderRequest, T.HeliPickupChat, T.HeliPickupToast, squadWide: true)
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
    private void HeliDropoff(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, GamemodeOld.Config.EffectActionHeliDropoff, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 150, ActionOrigin.AtCallerWaypoint, ActionType.SquadleaderRequest, T.HeliDropoffChat, T.HeliDropoffToast, squadWide: true)
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
        
        if (action.InitialPosition.HasValue)
        {
            action.LoopCheckComplete = () => (action.InitialPosition.Value - caller.Position).sqrMagnitude < 50 * 50;
        }

        action.Start();
        CloseUI(caller);
    }
    private void SuppliesBuild(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, GamemodeOld.Config.EffectActionSuppliesBuild, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 150, ActionOrigin.AtCallerPosition, ActionType.SquadleaderRequest, T.SuppliesBuildChat, T.SuppliesBuildToast, squadWide: true)
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
    private void SuppliesAmmo(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, GamemodeOld.Config.EffectActionSuppliesAmmo, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerPosition, ActionType.SquadleaderRequest, T.SuppliesAmmoChat, T.SuppliesAmmoToast, squadWide: true)
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
    private void AirSupport(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, GamemodeOld.Config.EffectActionAirSupport, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerLookTarget, ActionType.SquadleaderRequest, T.AirSupportChat, T.AirSupportToast, squadWide: true)
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
    private void ArmorSupport(UnturnedButton button, Player player) // WIP
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

        Action action = new Action(caller, GamemodeOld.Config.EffectActionArmorSupport, null, viewers, toastReceivers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerLookTarget, ActionType.SquadleaderRequest, T.ArmorSupportChat, T.ArmorSupportToast, squadWide: true)
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
    private void UnloadBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryUnloadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Build);
    }
    private void UnloadAmmo(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryUnloadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Ammo);
    }
    private void LoadBuild(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryLoadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Build, true);
    }
    private void LoadAmmo(UnturnedButton button, Player player) // WIP
    {
        UCPlayer? caller = UCPlayer.FromPlayer(player);
        if (caller == null)
            return;
        TryLoadSupplies(caller, 5, TeamManager.GetFaction(caller.GetTeam()).Ammo, false);
    }
    
    const int RequiredUnloadAmountForReward = 5;
    private static void TryUnloadSupplies(UCPlayer caller, int amount, IAssetLink<ItemAsset>? supplyItem)
    {
        if (!supplyItem.TryGetId(out ushort supplyId))
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
                if (item.item.id == supplyId)
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
            int difference = caller.SuppliesUnloaded - RequiredUnloadAmountForReward;
            if (difference >= 0)
            {
                caller.SuppliesUnloaded = -difference;

                if (Points.PointsConfig.XPData.TryGetValue(nameof(XPReward.UnloadSupplies), out PointsConfig.XPRewardData data))
                {
                    int xp = data.Amount * Mathf.CeilToInt(difference / (float)RequiredUnloadAmountForReward);

                    if (caller.KitClass == Class.Pilot)
                        xp *= 2;

                    Points.AwardXP(caller, XPReward.UnloadSupplies, xp);
                }
            }
        }
    }
    private static void TryLoadSupplies(UCPlayer caller, int amount, IAssetLink<ItemAsset>? supplyItem, bool build)
    {
        if (!supplyItem.TryGetAsset(out ItemAsset? supplyAsset))
            return;

        FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(caller.Position, caller.GetTeam());
        InteractableVehicle? vehicle = caller.CurrentVehicle;
        if (vehicle != null && fob != null && vehicle.TryGetComponent(out VehicleComponent c) && c.IsLogistics)
        {
            amount = Mathf.Clamp(amount, 0, build ? fob.BuildSupply : fob.AmmoSupply);

            int successfullyAdded = 0;

            for (int i = 0; i < amount; i++)
            {
                if (vehicle.trunkItems.tryAddItem(new Item(supplyAsset, EItemOrigin.ADMIN)))
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

            if (successfullyAdded > 0 && (build ? GamemodeOld.Config.EffectUnloadBuild : GamemodeOld.Config.EffectUnloadAmmo).TryGetAsset(out EffectAsset? effect))
            {
                F.TriggerEffectReliable(effect, EffectManager.MEDIUM, vehicle.transform.position);
            }
        }
    }
    private void Attack(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionAttack, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerLookTarget, ActionType.Order, null, T.AttackToast, squadWide: true)
        {
            CheckValid = () => !F.IsInMain(caller)
        };

        action.Start();
        CloseUI(caller);
    }
    private void Defend(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionDefend, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerLookTarget, ActionType.Order, null, T.DefendToast, squadWide: true)
        {
            CheckValid = () => !F.IsInMain(caller)
        };

        action.Start();
        CloseUI(caller);
    }
    private void Move(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionMove, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerLookTarget, ActionType.Order, null, T.MoveToast, squadWide: true)
        {
            CheckValid = () => !F.IsInMain(caller)
        };

        action.Start();
        CloseUI(caller);
    }
    private void Build(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionBuild, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerLookTarget, ActionType.Order, null, T.BuildToast, squadWide: true)
        {
            CheckValid = () => !F.IsInMain(caller)
        };

        action.Start();
        CloseUI(caller);
    }
    private void AttackMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionAttack, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerWaypoint, ActionType.Order, null, T.AttackToast, squadWide: true)
        {
            CheckValid = () =>
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
            }
        };

        action.Start();
        CloseUI(caller);
    }
    private void DefendMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionDefend, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerWaypoint, ActionType.Order, null, T.DefendToast, squadWide: true)
        {
            CheckValid = () =>
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
            }
        };

        action.Start();
        CloseUI(caller);
    }
    private void MoveMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionMove, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerWaypoint, ActionType.Order, null, T.MoveToast, squadWide: true)
        {
            CheckValid = () =>
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
            }
        };

        action.Start();
        CloseUI(caller);
    }
    private void BuildMarker(UnturnedButton button, Player player)
    {
        UCPlayer caller = UCPlayer.FromPlayer(player)!;

        IEnumerable<UCPlayer> viewers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller));
        //IEnumerable<UCPlayer> toastReceivers = PlayerManager.OnlinePlayers.Where(p => p.IsInSameSquadAs(caller) && p != caller);

        Action action = new Action(caller, GamemodeOld.Config.EffectActionBuild, null, viewers, updateFrequency: 1, lifeTime: 120, ActionOrigin.AtCallerWaypoint, ActionType.Order, null, T.BuildToast, squadWide: true)
        {
            CheckValid = () =>
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
            }
        };

        action.Start();
        CloseUI(caller);
    }
}