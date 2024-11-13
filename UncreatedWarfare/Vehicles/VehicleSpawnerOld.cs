#if false
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaver))]
public class VehicleSpawnerOld : ListSqlSingleton<VehicleSpawn>, ILevelStartListenerAsync, IGameStartListener, IStagingPhaseOverListener, ITimeSyncListener, IFlagCapturedListener, IFlagNeutralizedListener, ICacheDiscoveredListener, ICacheDestroyedListener
{
    private static readonly List<InteractableVehicle> NearbyTempOutput = new List<InteractableVehicle>(4);
    public const ushort MaxBatteryCharge = 10000;
    public const float VehicleHeightOffset = 5f;
    
    public override MySqlDatabase Sql => Data.AdminSql;
    public override bool AwaitLoad => true;
    public VehicleSpawner() : base("vehiclespawns", SCHEMAS) { }
    public static VehicleSpawner? GetSingletonQuick() => Data.Is(out IVehicles r) ? r.VehicleSpawner : null;
    public override Task PreLoad(CancellationToken token)
    {
        EventDispatcher.EnterVehicleRequested += OnVehicleEnterRequested;
        EventDispatcher.VehicleSwapSeatRequested += OnVehicleSwapSeatRequested;
        EventDispatcher.ExitVehicleRequested += OnVehicleExitRequested;
        EventDispatcher.ExitVehicle += OnVehicleExit;
        TeamManager.OnPlayerEnteredMainBase += OnPlayerEnterMain;
        TeamManager.OnPlayerLeftMainBase += OnPlayerLeftMain;
        EventDispatcher.BarricadeDestroyed += OnBarricadeDestroyed;
        EventDispatcher.StructureDestroyed += OnStructureDestroyed;
        EventDispatcher.VehicleDestroyed += OnVehicleDestroyed;
        UCPlayerKeys.SubscribeKeyDown(DropFlaresStart, Data.Keys.SpawnCountermeasures);
        UCPlayerKeys.SubscribeKeyUp(DropFlaresStop, Data.Keys.SpawnCountermeasures);
        return base.PreLoad(token);
    }
    public override Task PreUnload(CancellationToken token)
    {
        UCPlayerKeys.UnsubscribeKeyDown(DropFlaresStart, Data.Keys.SpawnCountermeasures);
        UCPlayerKeys.UnsubscribeKeyUp(DropFlaresStop, Data.Keys.SpawnCountermeasures);
        EventDispatcher.VehicleDestroyed -= OnVehicleDestroyed;
        EventDispatcher.StructureDestroyed -= OnStructureDestroyed;
        EventDispatcher.BarricadeDestroyed -= OnBarricadeDestroyed;
        TeamManager.OnPlayerLeftMainBase -= OnPlayerLeftMain;
        TeamManager.OnPlayerEnteredMainBase -= OnPlayerEnterMain;
        EventDispatcher.ExitVehicle -= OnVehicleExit;
        EventDispatcher.ExitVehicleRequested -= OnVehicleExitRequested;
        EventDispatcher.VehicleSwapSeatRequested -= OnVehicleSwapSeatRequested;
        EventDispatcher.EnterVehicleRequested -= OnVehicleEnterRequested;
        return base.PreUnload(token);
    }
    public static bool CanUseCountermeasures(InteractableVehicle vehicle) => true;
    private static readonly List<BarricadeDrop> WorkingToUpdate = new List<BarricadeDrop>(32);
    private void OnPlayerEnterMain(UCPlayer player, ulong team)
    {
        GameThread.AssertCurrent();
        try
        {
            WriteWait();
            try
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    SavedStructure? sign = Items[i].Item?.Sign?.Item;
                    if (sign?.Buildable?.Drop is not BarricadeDrop drop)
                        continue;
                    if (team == 1 && TeamManager.Team1Main.IsInside(sign.Position) || team == 2 && TeamManager.Team2Main.IsInside(sign.Position))
                        WorkingToUpdate.Add(drop);
                }
            }
            finally
            {
                WriteRelease();
            }
            for (int i = 0; i < WorkingToUpdate.Count; ++i)
                Signs.SendSignUpdate(WorkingToUpdate[i], player, false);
        }
        finally
        {
            WorkingToUpdate.Clear();
        }
        InteractableVehicle? vehicle = player.CurrentVehicle;
        if (vehicle == null)
            return;
        VehicleBay? bay = VehicleBay.GetSingletonQuick();
        if (bay == null)
            return;
        VehicleData? data = bay.GetDataSync(vehicle.asset.GUID);
        if ((data == null || !VehicleBay.CanSoloVehicle(data)) && IsOnlyPassenger(player, out byte seat))
        {
            ActionLog.Add(ActionLogType.SoloRTB, (seat == 0 ? "Driver of " : "Passenger of ") + ActionLog.AsAsset(vehicle.asset) +
                                                      "." + (seat == 0 ? string.Empty : " Seat: " + seat.ToString(CultureInfo.InvariantCulture) + "."), player);
        }
    }
    private static void OnPlayerLeftMain(UCPlayer player, ulong team)
    {
        EnsureVehicleLocked(player);
        InteractableVehicle? vehicle = player.CurrentVehicle;
        if (vehicle == null)
            return;
        VehicleBay? bay = VehicleBay.GetSingletonQuick();
        if (bay == null)
            return;
        VehicleData? data = bay.GetDataSync(vehicle.asset.GUID);
        if ((data == null || !VehicleBay.CanSoloVehicle(data)) && IsOnlyPassenger(player, out byte seat))
        {
            ActionLog.Add(ActionLogType.PossibleSolo, (seat == 0 ? "Driver of " : "Passenger of ") + ActionLog.AsAsset(vehicle.asset) +
                                                           "." + (seat == 0 ? string.Empty : " Seat: " + seat.ToString(CultureInfo.InvariantCulture) + "."), player);
        }
    }

    private void UpdateFlagSigns()
    {
        GameThread.AssertCurrent();
        Signs.UpdateVehicleBaySigns(null);
    }
    private void DropFlaresStart(UCPlayer player, /*float timeDown, */ref bool handled)
    {
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null &&
            player.Player.movement.getSeat() == 0 &&
            (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && CanUseCountermeasures(vehicle) &&
            vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.TryDropFlares();
        }
    }
    private void DropFlaresStop(UCPlayer player, float timeDown, ref bool handled)
    {
#if false
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null &&
            player.Player.movement.getSeat() == 0 &&
            (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && CanUseCountermeasures(vehicle) &&
            vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            // TODO: this method isn't really needed anymore
        }
#endif
    }

    public static bool IsOnlyPassenger(UCPlayer player, out byte playerSeat, byte inSeat = byte.MaxValue)
    {
        playerSeat = byte.MaxValue;
        if (!player.IsOnline)
        {
            return false;
        }

        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle == null || vehicle.isDead)
        {
            return false;
        }

        byte seat = player.Player.movement.getSeat();
        if (inSeat != byte.MaxValue && seat != inSeat)
        {
            return false;
        }

        for (int i = 0; i < vehicle.passengers.Length; ++i)
        {
            if (i != seat && vehicle.passengers[i].player != null)
                return false;
        }

        playerSeat = seat;
        return true;
    }
    public static bool IsVehicleFull(InteractableVehicle vehicle, bool excludeDriver = false)
    {
        GameThread.AssertCurrent();
        for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            if (seat == 0 && excludeDriver)
                continue;

            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player == null)
            {
                return true;
            }
        }
        return true;
    }
    public static bool TryGetFirstNonCrewSeat(InteractableVehicle vehicle, VehicleData data, out byte seat)
    {
        GameThread.AssertCurrent();
        for (seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player == null && !data.CrewSeats.Contains(seat))
            {
                return true;
            }
        }
        seat = 0;
        return false;
    }
    public static bool TryGetFirstNonDriverSeat(InteractableVehicle vehicle, out byte seat)
    {
        GameThread.AssertCurrent();
        seat = 0;
        do
        {
            if (++seat >= vehicle.passengers.Length)
                return false;
        } while (vehicle.passengers[seat].player != null);
        return true;
    }
    public static bool IsOwnerInVehicle(InteractableVehicle vehicle, UCPlayer owner)
    {
        GameThread.AssertCurrent();
        if (vehicle.lockedOwner == CSteamID.Nil || owner == null) return false;

        foreach (Passenger passenger in vehicle.passengers)
        {
            if (passenger.player != null && owner.Steam64 == passenger.player.playerID.steamID.m_SteamID)
                return true;
        }
        return false;
    }
    public static int CountCrewmen(InteractableVehicle vehicle, VehicleData data)
    {
        GameThread.AssertCurrent();
        int count = 0;
        for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (data.CrewSeats.Contains(seat) && passenger.player != null)
            {
                count++;
            }
        }
        return count;
    }
    private static void EnsureVehicleLocked(UCPlayer player)
    {
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null &&
            !vehicle.isDead &&
            vehicle.checkDriver(player.CSteamID) &&
            vehicle.TryGetComponent(out VehicleComponent c) &&
            c.Data?.Item is { } &&
            (vehicle.lockedOwner.m_SteamID == 0 ||
             !vehicle.isLocked ||
             UCPlayer.FromID(vehicle.lockedOwner.m_SteamID) is not { IsOnline: true } ||
             vehicle.lockedGroup.m_SteamID is not 1ul and not 2ul
             ))
        {
            VehicleManager.ServerSetVehicleLock(vehicle, player.CSteamID, new CSteamID(TeamManager.GetGroupID(player.GetTeam())), true);
        }
    }
    private static void OnVehicleExit(ExitVehicle e)
    {
        GameThread.AssertCurrent();
        if (e.OldPassengerIndex == 0 && e.Vehicle.transform.TryGetComponent(out VehicleComponent comp))
            comp.LastDriverTime = Time.realtimeSinceStartup;
        if (KitDefaults<WarfareDbContext>.ShouldDequipOnExitVehicle(e.Player.KitClass))
            e.Player.Player.equipment.dequip();
    }
    private static void OnVehicleExitRequested(ExitVehicleRequested e)
    {
        GameThread.AssertCurrent();
        if (!e.Player.OnDuty() && e.ExitLocation.y - F.GetHeightAt2DPoint(e.ExitLocation.x, e.ExitLocation.z) > UCWarfare.Config.MaxVehicleHeightToLeave)
        {
            if (!FOBManager.Config.Buildables.Exists(v => v.Type == BuildableType.Emplacement && v.Emplacement is not null && v.Emplacement.EmplacementVehicle is not null && v.Emplacement.EmplacementVehicle.Guid == e.Vehicle.asset.GUID))
            {
                e.Player.SendChat(T.VehicleTooHigh);
                e.Cancel();
            }
        }
    }
    private void OnVehicleEnterRequested(EnterVehicleRequested e)
    {
        GameThread.AssertCurrent();
        if (!VehicleUtility.IgnoreSwapCooldown && CooldownManager.IsLoaded && CooldownManager.HasCooldown(e.Player, CooldownType.InteractVehicleSeats, out _, e.Vehicle))
        {
            e.Cancel();
            return;
        }
        if (Data.Gamemode.State != State.Active && Data.Gamemode.State != State.Staging)
        {
            e.Player.SendChat(T.VehicleStaging, e.Vehicle.asset);
            e.Cancel();
            return;
        }
        if (!e.Vehicle.asset.canBeLocked) return;
        if (!e.Player.OnDuty() && Data.Gamemode.State == State.Staging && Data.Is<IStagingPhase>(out _) && (!Data.Is(out IAttackDefense? atk) || e.Player.GetTeam() == atk.AttackingTeam))
        {
            e.Player.SendChat(T.VehicleStaging, e.Vehicle.asset);
            e.Cancel();
            return;
        }
        if (Data.Is(out IRevives? r) && r.ReviveManager.IsInjured(e.Player.Steam64))
        {
            e.Cancel();
            return;
        }

        if (!e.Player.HasKit)
        {
            e.Player.SendChat(T.VehicleNoKit);
            e.Cancel();
        }
        if (e.Vehicle.transform.TryGetComponent(out VehicleComponent vc) && 
            (vc.Data?.Item?.IsDelayed(out Delay delay) ?? false) && 
            delay.Type == DelayType.Teammates
            && e.Player.Player.IsInMain())
        {
            e.Player.SendChat(T.RequestVehicleTeammatesDelay, Mathf.FloorToInt(delay.Value));
            e.Cancel();
        }
    }
    private void OnVehicleSwapSeatRequested(VehicleSwapSeatRequested e)
    {
        GameThread.AssertCurrent();
        if (!VehicleUtility.IgnoreSwapCooldown && CooldownManager.IsLoaded && CooldownManager.HasCooldown(e.Player, CooldownType.InteractVehicleSeats, out _, e.Vehicle))
        {
            e.Cancel();
            return;
        }
        if (!e.Vehicle.TryGetComponent(out VehicleComponent c))
            return;
        if (c.IsEmplacement && e.FinalSeat == 0)
        {
            e.Cancel();
        }
        else
        {
            if (!e.Player.HasKit)
            {
                e.Player.SendChat(T.VehicleNoKit);
                e.Cancel();
                return;
            }

            UCPlayer? owner = UCPlayer.FromCSteamID(e.Vehicle.lockedOwner);
            VehicleData? data = c.Data?.Item;
            if (data != null &&
                data.RequiredClass != Class.None) // vehicle requires crewman or pilot
            {
                if (!VehicleUtility.AllowEnterDriverSeat
                    && c.IsAircraft &&
                    e.InitialSeat == 0 &&
                    e.FinalSeat != 0 &&
                    e.Vehicle.transform.position.y - LevelGround.getHeight(e.Vehicle.transform.position) > 30 &&
                    !e.Player.OnDuty())
                {
                    e.Player.SendChat(T.VehicleAbandoningPilot);
                    e.Cancel();
                }
                else if (data.CrewSeats.ArrayContains(e.FinalSeat)) // seat is for crewman only
                {
                    if ((e.Player.KitClass == data.RequiredClass) || e.Player.OnDuty())
                    {
                        if (e.FinalSeat == 0) // if a crewman is trying to enter the driver's seat
                        {
                            FOBManager? manager = Data.Singletons.GetSingleton<FOBManager>();
                            bool canEnterDriverSeat = VehicleUtility.AllowEnterDriverSeat ||
                                                      owner == null ||
                                e.Player == owner ||
                                e.Player.OnDuty() ||
                                IsOwnerInVehicle(e.Vehicle, owner) ||
                                (owner != null && owner.Squad != null && owner.Squad.Members.Contains(e.Player) ||
                                (owner!.Position - e.Vehicle.transform.position).sqrMagnitude > Math.Pow(200, 2)) ||
                                (data.Type == VehicleType.LogisticsGround && manager != null && manager.FindNearestFOB<FOB>(e.Vehicle.transform.position, e.Vehicle.lockedGroup.m_SteamID.GetTeam()) != null);

                            if (!canEnterDriverSeat)
                            {
                                if (owner?.Squad is null)
                                {
                                    OfflinePlayer pl = new OfflinePlayer(e.Vehicle.lockedOwner);
                                    if (owner != null || pl.TryCacheLocal())
                                    {
                                        e.Player.SendChat(T.VehicleWaitForOwner, owner ?? pl as IPlayer);
                                    }
                                    else
                                    {
                                        UCWarfare.RunTask(async token =>
                                        {
                                            OfflinePlayer pl2 = pl;
                                            await pl2.CacheUsernames(token).ConfigureAwait(false);
                                            await UniTask.SwitchToMainThread(token);
                                            e.Player.SendChat(T.VehicleWaitForOwner, pl);
                                        }, UCWarfare.UnloadCancel);
                                    }
                                }
                                else
                                    e.Player.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);
                                e.Cancel();
                            }
                        }
                        else // if the player is trying to switch to a gunner's seat
                        {
                            if (!(F.IsInMain(e.Vehicle.transform.position) || e.Player.OnDuty())) // if player is trying to switch to a gunner's seat outside of main
                            {
                                if (e.Vehicle.passengers.Length == 0 || e.Vehicle.passengers[0].player is null) // if they have no driver
                                {
                                    e.Player.SendChat(T.VehicleDriverNeeded);
                                    e.Cancel();
                                }
                                else if (e.Player.Steam64 == e.Vehicle.passengers[0].player.playerID.steamID.m_SteamID) // if they are the driver
                                {
                                    e.Player.SendChat(T.VehicleAbandoningDriver);
                                    e.Cancel();
                                }
                            }
                        }
                    }
                    else
                    {
                        e.Player.SendChat(T.VehicleMissingKit, data.RequiredClass);
                        e.Cancel();
                    }
                }                
            }
            else
            {
                if (e.FinalSeat == 0)
                {
                    bool canEnterDriverSeat = owner is null || e.Player.Steam64 == owner.Steam64 || e.Player.OnDuty() || IsOwnerInVehicle(e.Vehicle, owner) || (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(e.Player));

                    if (!canEnterDriverSeat)
                    {
                        if (owner!.Squad == null)
                            e.Player.SendChat(T.VehicleWaitForOwner, owner);
                        else
                            e.Player.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);

                        e.Cancel();
                    }
                }
            }
        }
    }

}
#endif