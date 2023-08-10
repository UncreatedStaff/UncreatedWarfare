using SDG.Unturned;
using Steamworks;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class VCommand : AsyncCommand
{
    private const float SwapRequestDuration = 15f;
    private const string AdminSyntax = "/v <vehicle|kick|give> [player]";
    private const string Syntax = "/v <kick|give> [player]";
    private const string AdminHelp = "Spawn a vehicle in front of you or manage your requested vehicle.";
    private const string Help = "Manage your requested vehicle.";

    public VCommand() : base("vehicle", EAdminType.MEMBER, 1)
    {
        AddAlias("v");
        AddAlias("veh");
        Structure = new CommandStructure
        {
            Description = "Spawns a vehicle in front of you.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Enter")
                {
                    Permission = EAdminType.ADMIN_ON_DUTY,
                    FlagName = "e",
                    Description = "Enter the vehicle after it spawns."
                },
                new CommandParameter("Vehicle", typeof(VehicleAsset))
                {
                    Description = "Summon a vehicle in front of you.",
                    Permission = EAdminType.ADMIN_ON_DUTY
                },
                new CommandParameter("Kick")
                {
                    Aliases = new string[] { "Remove", "K" },
                    Description = "Remove a player from your vehicle. Can not be done while moving unless they are the driver.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer), "Driver", "Pilot", "Turret", typeof(byte))
                        {
                            Description = "Player or seat to remove from your vehicle, optional if there is only one option."
                        },
                        new CommandParameter("Force Remove")
                        {
                            FlagName = "r",
                            Aliases = new string[] { "k" },
                            Description = "Force the player out of the vehicle instead of just finding a different seat."
                        }
                    }
                },
                new CommandParameter("Give")
                {
                    Aliases = new string[] { "Transfer", "G" },
                    Description = "Transfer ownerhip of your vehicle to someone else, they must also have the vehicle unlocked. Will not give credits when abandoned.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                    }
                },
                /*
                new CommandParameter("Enter")
                {
                    Aliases = new string[] { "Swap", "E" },
                    Description = "Swap seats with the player in the specified seat, moving them to the next available seat or kicking them.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Seat", "Driver", "Pilot", "Turret", typeof(byte)),
                        new CommandParameter("Accept")
                        {
                            Aliases = new string[] { "Yes", "Y" },
                            Description = "Accept a swap request from a fellow passenger."
                        },
                        new CommandParameter("Deny")
                        {
                            Aliases = new string[] { "No", "N" },
                            Description = "Deny a swap request from a fellow passenger, done automatically after a cooldown."
                        }
                    }
                }*/
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertHelpCheck(0, ctx.HasPermission(EAdminType.ADMIN_ON_DUTY) ? (AdminSyntax + " - " + AdminHelp) : (Syntax + " - " + Help));

        ctx.AssertRanByPlayer();

        bool kick = ctx.MatchParameter(0, "kick", "remove", "k");
        bool enter = !kick && ctx.MatchParameter(0, "enter", "swap", "e");
        if (kick || enter || ctx.MatchParameter(0, "give", "transfer", "g"))
        {
            if (!ctx.HasArgs(2))
                throw ctx.SendCorrectUsage(Syntax);

            VehicleSpawner? spawner = VehicleSpawner.GetSingletonQuick();
            if (spawner == null)
                throw ctx.SendGamemodeError();
            if (!ctx.TryGetTarget(out InteractableVehicle? vehicleTarget))
            {
                ulong callerTeam = ctx.Caller.GetTeam();
                // find one linked vehicle owned by the player
                await spawner.WaitAsync(token);
                try
                {
                    await spawner.WriteWaitAsync(token);
                    try
                    {
                        foreach (SqlItem<Vehicles.VehicleSpawn> proxy in spawner.Items)
                        {
                            if (proxy.Item is { } spawn)
                            {
                                if (spawn.HasLinkedVehicle(out InteractableVehicle linked) && linked.lockedOwner.m_SteamID == ctx.CallerID &&
                                    // ground assets can be anywhere, air assets need to be near main
                                    (linked.asset.engine is not EEngine.HELICOPTER and not EEngine.PLANE && (ctx.Caller.Position - linked.transform.position).sqrMagnitude < 300 * 300
                                     || TeamManager.IsInMainOrAMC(callerTeam, ctx.Caller.Position) && TeamManager.IsInMainOrAMC(callerTeam, linked.transform.position)))
                                {
                                    if (vehicleTarget == null)
                                        vehicleTarget = linked;
                                    else
                                    {
                                        vehicleTarget = null;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        spawner.WriteRelease();
                    }
                }
                finally
                {
                    spawner.Release();
                }

                await UCWarfare.ToUpdate(token);
            }

            if (vehicleTarget == null)
                throw ctx.Reply(T.VehicleMustBeLookingAtLinkedVehicle);
            bool owned = true;
            ulong team = vehicleTarget.lockedGroup.m_SteamID.GetTeam();
            if (team is not 1 and not 2)
            {
                team = ctx.Caller.GetTeam();
                if (team is not 1 and not 2)
                    throw ctx.Reply(T.NotOnCaptureTeam);
            }
            else if (team != ctx.Caller.GetTeam())
                throw ctx.Reply(T.VehicleNotOnSameTeam, TeamManager.GetFaction(team));
            if (vehicleTarget.lockedOwner.m_SteamID != ctx.CallerID)
            {
                if (!enter)
                {
                    OfflinePlayer pl = new OfflinePlayer(vehicleTarget.lockedOwner.m_SteamID);
                    await pl.CacheUsernames(token).ConfigureAwait(false);
                    throw ctx.Reply(T.VehicleLinkedVehicleNotOwnedByCaller, pl);
                }

                owned = ctx.Caller.OnDuty();
            }

            // vehicle give <player>
            if (!enter && !kick)
            {
                if (!ctx.TryGet(1, out _, out UCPlayer? onlinePlayer, TeamManager.EnumerateTeam(team), true, UCPlayer.NameSearch.NickName) || onlinePlayer == null)
                    throw ctx.SendPlayerNotFound();

                VehicleManager.ServerSetVehicleLock(vehicleTarget, onlinePlayer.CSteamID, new CSteamID(TeamManager.GetGroupID(team)), true);
                VehicleComponent.TryAddOwnerToHistory(vehicleTarget, onlinePlayer.Steam64);
                if (Gamemode.Config.EffectUnlockVehicle.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicleTarget.transform.position);
                onlinePlayer.SendChat(T.VehicleGivenDm, vehicleTarget.asset, ctx.Caller);
                throw ctx.Reply(T.VehicleGiven, vehicleTarget.asset, onlinePlayer);
            }

            // find seat reference
            int seatIndex = -1;
            if (ctx.MatchParameter(1, "driver", "pilot", "drive"))
                seatIndex = 0;
            else if (ctx.HasArgsExact(1) && ctx.MatchParameter(1, "turret", "gunner", "gun"))
            {
                for (int i = 0; i < vehicleTarget.turrets.Length; ++i)
                {
                    Passenger turret = vehicleTarget.turrets[i];
                    if (turret.player != null)
                    {
                        if (seatIndex == -1)
                            seatIndex = turret.turret.seatIndex;
                        else
                        {
                            seatIndex = -1;
                            break;
                        }
                    }
                }
            }
            // turret 1, gun 1, etc.
            else if (ctx.TryGetRange(1, out string val) && (val.StartsWith("turret", StringComparison.InvariantCultureIgnoreCase) || val.StartsWith("gun", StringComparison.InvariantCultureIgnoreCase)))
            {
                int st = -1;
                for (int i = 3; i < val.Length; ++i)
                {
                    if (char.IsDigit(val[i]))
                    {
                        st = i;
                        break;
                    }
                }
                if (st >= 0 && int.TryParse(val.Substring(st), NumberStyles.Number, ctx.CultureInfo, out int seat))
                {
                    if (seat > 0)
                        --seat;

                    if (vehicleTarget.turrets.Length > seat)
                        seatIndex = vehicleTarget.turrets[seat].turret.seatIndex;
                }
            }
            else if (ctx.TryGet(1, out _, out UCPlayer? onlinePlayer, TeamManager.EnumerateTeam(team), true, UCPlayer.NameSearch.NickName) && onlinePlayer != null)
            {
                if (onlinePlayer.CurrentVehicle == vehicleTarget)
                    seatIndex = onlinePlayer.Player.movement.getSeat();
                else
                    throw ctx.Reply(T.VehicleTargetNotInVehicle, onlinePlayer);
            }
            else if (ctx.TryGet(1, out int seat))
            {
                if (seat > 0)
                    --seat;
                if (seat is >= 0 and <= byte.MaxValue)
                    seatIndex = (byte)seat;
            }
            else throw ctx.SendCorrectUsage($"/vehicle {(kick ? "kick" : (owned ? "enter" : "swap"))} <player or seat>");

            if (vehicleTarget.passengers.Length <= seatIndex)
                throw ctx.Reply(T.VehicleSeatNotValidOutOfRange, seatIndex + 1);
            if (seatIndex == -1)
                throw ctx.Reply(T.VehicleSeatNotValidText, ctx.Get(1)!);

            Passenger targetSeat = vehicleTarget.passengers[seatIndex];
            UCPlayer? target = targetSeat.player == null ? null : UCPlayer.FromSteamPlayer(targetSeat.player);

            float time = Time.realtimeSinceStartup;
            // vehicle kick <player or seat>
            if (kick)
            {
                if (target is not { IsOnline: true })
                    throw ctx.Reply(T.VehicleSeatNotOccupied, seatIndex + 1);
                
                bool wantsFullKick = ctx.MatchFlag("r", "k") && (vehicleTarget.asset.engine is not EEngine.PLANE and not EEngine.HELICOPTER ||
                                                            Mathf.Abs(vehicleTarget.speed) <= 0.15f || TeamManager.IsInMain(team, vehicleTarget.transform.position));
                if (wantsFullKick || !UCVehicleManager.TryMovePlayerToEmptySeat(target.Player))
                {
                    L.LogDebug("Removing target instead of moving.");
                    VehicleManager.forceRemovePlayer(vehicleTarget, target.CSteamID);
                }
                vehicleTarget.lastSeat = time;
                if (CooldownManager.IsLoaded)
                    CooldownManager.StartCooldown(target, CooldownType.InteractVehicleSeats, 5f, vehicleTarget);
                target.SendChat(T.VehicleOwnerKickedDM, vehicleTarget.asset, ctx.Caller, seatIndex + 1);
                throw ctx.Reply(T.VehicleKickedPlayer, vehicleTarget.asset, target, seatIndex + 1);
            }

            throw ctx.SendNotImplemented();
            // vehicle enter
            if (time - vehicleTarget.lastSeat < 1f)
            {
                // check vehicle cooldown
                vehicleTarget.lastSeat = time;
                int ms = Mathf.RoundToInt(time - vehicleTarget.lastSeat * 1000);
                if (ms > 0)
                {
                    await Task.Delay(ms, token);
                    await UCWarfare.ToUpdate(token);
                    vehicleTarget.lastSeat = time;
                }
            }
            
            bool isByRequest = false;
            notInVehicle:
            if (target == null || owned)
            {
                InteractableVehicle? currentVehicle = ctx.Caller.CurrentVehicle;
                // not in vehicle
                if (currentVehicle == null && (vehicleTarget.transform.position - ctx.Caller.Position).sqrMagnitude < 12 * 12)
                {
                    if (target != null)
                    {
                        if (!UCVehicleManager.TryMovePlayerToEmptySeat(target))
                        {
                            L.LogDebug("Removing target instead of moving.");
                            VehicleManager.forceRemovePlayer(vehicleTarget, target.CSteamID);
                        }
                    }
                    bool success = UCVehicleManager.TryPutPlayerInVehicle(vehicleTarget, ctx.Caller.Player, (byte)seatIndex);
                    L.LogDebug($"Enter vehicle status: {success}.");
                    if (target != null && success)
                    {
                        if (!isByRequest)
                        {
                            if (CooldownManager.IsLoaded)
                                CooldownManager.StartCooldown(target, CooldownType.InteractVehicleSeats, 5f, vehicleTarget);
                            target.SendChat(T.VehicleOwnerTookSeatDM, vehicleTarget.asset, ctx.Caller, seatIndex + 1);
                        }
                        else
                            target.SendChat(T.VehicleSwappedSeats, ctx.Caller);
                    }
                    
                    throw ctx.Reply(success ? T.VehicleEnterForceSwapped : T.VehicleEnterFailed, vehicleTarget.asset, seatIndex + 1);
                }
                
                // in same vehicle
                if (currentVehicle == vehicleTarget)
                {
                    bool success = UCVehicleManager.TrySwapPlayerInVehicle(ctx.Caller, (byte)seatIndex, true);
                    L.LogDebug($"Swap vehicle status: {success}.");
                    if (success && target != null)
                    {
                        if (!isByRequest)
                        {
                            if (CooldownManager.IsLoaded)
                                CooldownManager.StartCooldown(target, CooldownType.InteractVehicleSeats, 5f, vehicleTarget);
                            target.SendChat(T.VehicleOwnerTookSeatDM, vehicleTarget.asset, ctx.Caller, seatIndex + 1);
                        }
                        else
                        {
                            target.SendChat(T.VehicleSwappedSeats, ctx.Caller);
                            throw ctx.Reply(T.VehicleSwappedSeats, target);
                        }
                    }
                    throw ctx.Reply(success ? T.VehicleEnterForceSwapped : T.VehicleEnterFailed, vehicleTarget.asset, seatIndex + 1);
                }

                throw ctx.Reply(T.VehicleTooFarAway, vehicleTarget.asset);
            }

            // send swap request
            else
            {
                InteractableVehicle? currentVehicle = ctx.Caller.CurrentVehicle;
                if (currentVehicle != vehicleTarget)
                    throw ctx.Reply(T.VehicleSwapRequestNotInSameVehicle, vehicleTarget.asset, target);
                byte fromSeat = ctx.Caller.Player.movement.getSeat();
                if (target.PendingVehicleSwapRequest.Vehicle != null)
                    throw ctx.Reply(T.VehicleSwapRequestAlreadySent, target, Mathf.CeilToInt(SwapRequestDuration - (time - target.PendingVehicleSwapRequest.SendTime)), target.PendingVehicleSwapRequest.Sender!);
                CancellationTokenSource src = new CancellationTokenSource();
                target.PendingVehicleSwapRequest = new VehicleSwapRequest(time, vehicleTarget, ctx.Caller, src);
                target.SendChat(T.VehicleSentSwapRequestDm, ctx.Caller, fromSeat + 1, "/v <accept|deny>");
                ctx.Reply(T.VehicleSwapRequestSent, target, seatIndex + 1, Mathf.CeilToInt(SwapRequestDuration));
                CancellationToken token2 = token;
                token2.CombineIfNeeded(src.Token);
                try
                {
                    await Task.Delay(Mathf.RoundToInt(SwapRequestDuration * 1000f), token2).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token2.IsCancellationRequested) { }
                await UCWarfare.ToUpdate(token);
                if (target.PendingVehicleSwapRequest.Vehicle == vehicleTarget)
                {
                    bool? state = target.PendingVehicleSwapRequest.IsDenied;
                    target.PendingVehicleSwapRequest.RespondToken.Dispose();
                    target.PendingVehicleSwapRequest = default;
                    if (state is true)
                        throw ctx.Reply(T.VehicleSwapRequestDeniedByTarget, target);
                    
                    if (state is false)
                    {
                        ctx.Reply(T.VehicleSwapRequestAcceptedByTarget, target);
                        await Task.Delay(750, token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
                        if (!target.IsOnline || target.CurrentVehicle != vehicleTarget)
                            target = null;
                        if (!isByRequest)
                        {
                            isByRequest = true;
                            owned = true;
                            goto notInVehicle;
                        }

                        return;
                    }
                }

                throw ctx.Reply(T.VehicleSwapRequestTimedOutByTarget, target);
            }
        }

        bool deny = ctx.MatchParameter(0, "deny", "no", "n");
        if (deny || ctx.MatchParameter(0, "accept", "yes", "y"))
        {
            throw ctx.SendNotImplemented();
            if (ctx.Caller.PendingVehicleSwapRequest.Vehicle == null || ctx.Caller.PendingVehicleSwapRequest.IsDenied.HasValue)
                throw ctx.Reply(T.VehicleSwapRequestNotSent);

            ctx.Caller.PendingVehicleSwapRequest.IsDenied = deny;
            ctx.Caller.PendingVehicleSwapRequest.RespondToken.Cancel();
            throw ctx.Reply(deny ? T.VehicleSwapRequestDenied : T.VehicleSwapRequestAccepted, ctx.Caller.PendingVehicleSwapRequest.Sender);
        }

        ctx.AssertPermissions(EAdminType.ADMIN_ON_DUTY);
        enter = ctx.MatchFlag("e", "enter");

        ctx.AssertArgs(1, AdminSyntax);

        ctx.AssertOnDuty();

        if (!ctx.TryGet(0, out VehicleAsset asset, out _, true, allowMultipleResults: true))
            throw ctx.ReplyString("<color=#8f9494>Unable to find a vehicle by the name or id: <color=#dddddd>" + ctx.GetRange(0) + "</color>.</color>");

        Vector3 ppos = ctx.Caller.Position;
        Vector3 v = ctx.Caller.Player.look.aim.forward.normalized with { y = 0 };
        Vector3 targetPos = ppos + v * 6.5f;
        RaycastHit hit;
        targetPos.y += 500f;
        while (!Physics.Raycast(targetPos, Vector3.down, out hit, 500f, RayMasks.GROUND
                                                                       | RayMasks.BARRICADE
                                                                       | RayMasks.STRUCTURE
                                                                       | RayMasks.LARGE
                                                                       | RayMasks.MEDIUM
                                                                       | RayMasks.SMALL
                                                                       | RayMasks.ENVIRONMENT
                                                                       | RayMasks.RESOURCE) && Mathf.Abs(targetPos.y - ppos.y) < 500f)
        {
            targetPos.y -= 50f;
        }

        targetPos.y = (hit.transform == null ? ppos.y : hit.point.y) + 3f;

        // fill all turrets
        byte[][] turrets = new byte[asset.turrets.Length][];

        for (int i = 0; i < asset.turrets.Length; ++i)
        {
            if (Assets.find(EAssetType.ITEM, asset.turrets[i].itemID) is ItemAsset iasset)
                turrets[i] = iasset.getState(true);
            else
                turrets[i] = Array.Empty<byte>();
        }

        InteractableVehicle vehicle = VehicleManager.SpawnVehicleV3(
            asset,
            0,
            0,
            0f,
            targetPos,
            Quaternion.Euler(ctx.Caller.Player.look.aim.rotation.eulerAngles with { x = 0, z = 0 }),
            false,
            false,
            false,
            false,
            asset.fuel,
            asset.health,
            10000,
            ctx.CallerCSteamID,
            ctx.Caller.Player.quests.groupID,
            true,
            turrets,
            255);
        if (enter)
            VehicleManager.ServerForcePassengerIntoVehicle(ctx.Caller.Player, vehicle);
        ctx.ReplyString($"Spawned a <color=#dddddd>{vehicle.asset.vehicleName}</color> (<color=#aaaaaa>{vehicle.asset.id}</color>).", "bfb9ac");
    }
}


public struct VehicleSwapRequest
{
    public readonly float SendTime;
    public readonly InteractableVehicle Vehicle;
    public readonly UCPlayer Sender;
    public readonly CancellationTokenSource RespondToken;
    public bool? IsDenied;
    public VehicleSwapRequest(float sendTime, InteractableVehicle vehicle, UCPlayer sender, CancellationTokenSource token)
    {
        SendTime = sendTime;
        Vehicle = vehicle;
        Sender = sender;
        RespondToken = token;
        IsDenied = null;
    }
}