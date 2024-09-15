using DanielWillett.ReflectionTools;
using System;
using System.Globalization;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands.VanillaRework;

[Command("vehicle", "v", "veh"), Priority(1)]
[MetadataFile(nameof(GetHelpMetadata))]
public class VehicleCommand : IExecutableCommand
{
    private const string AdminSyntax = "/v <vehicle|kick|give|accept|deny> [player]";
    private const string Syntax = "/v <kick|give|accept|deny> [player]";
    private const string AdminHelp = "Spawn a vehicle in front of you or manage your requested vehicle.";
    private const string Help = "Manage your requested vehicle.";

    private static readonly PermissionLeaf PermissionSpawn = new PermissionLeaf("commands.vehicle.spawn", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionKick  = new PermissionLeaf("commands.vehicle.kick", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionGive  = new PermissionLeaf("commands.vehicle.give", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Spawns a vehicle in front of you.",
            Parameters =
            [
                new CommandParameter("Enter")
                {
                    Permission = PermissionSpawn,
                    FlagName = "e",
                    Description = "Enter the vehicle after it spawns."
                },
                new CommandParameter("Vehicle", typeof(VehicleAsset))
                {
                    Description = "Summon a vehicle in front of you.",
                    Permission = PermissionSpawn
                },
                new CommandParameter("Kick")
                {
                    Aliases = [ "Remove", "K" ],
                    Permission = PermissionKick,
                    Description = "Remove a player from your vehicle. Can not be done while moving unless they are the driver.",
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer), "Driver", "Pilot", "Turret", typeof(byte))
                        {
                            Description = "Player or seat to remove from your vehicle, optional if there is only one option."
                        },
                        new CommandParameter("Force Remove")
                        {
                            FlagName = "k",
                            Aliases = [ "r" ],
                            Description = "Force the player out of the vehicle instead of just finding a different seat."
                        }
                    ]
                },
                new CommandParameter("Accept")
                {
                    Aliases = [ "A", "Acc" ],
                    Permission = PermissionGive,
                    Description = "Accept a vehicle-related request."
                },
                new CommandParameter("Deny")
                {
                    Aliases = [ "D", "Dn" ],
                    Permission = PermissionGive,
                    Description = "Deny a vehicle-related request."
                },
                new CommandParameter("Give")
                {
                    Aliases = [ "Transfer", "G" ],
                    Permission = PermissionGive,
                    Description = "Transfer ownerhip of your vehicle to someone else, they must also have the vehicle unlocked. Will not give credits when abandoned.",
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer)),
                        new CommandParameter("Force Send Request")
                        {
                            FlagName = "r",
                            Aliases = [ "req" ],
                            Description = "Sends a request to give instead of just forcing it over (this behavior is forced when the recepient is in main)."
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, await Context.HasPermission(PermissionSpawn, token) ? (AdminSyntax + " - " + AdminHelp) : (Syntax + " - " + Help));

        Context.AssertRanByPlayer();

        bool kick = Context.MatchParameter(0, "kick", "remove", "k");
        bool enter = !kick && Context.MatchParameter(0, "enter", "swap", "e");
        if (kick || enter || Context.MatchParameter(0, "give", "transfer", "g"))
        {
            if (!Context.HasArgs(2))
                throw Context.SendCorrectUsage(Syntax);

            if (kick || enter)
            {
                await Context.AssertPermissions(PermissionKick, token);
            }
            else
            {
                await Context.AssertPermissions(PermissionGive, token);
            }


            VehicleSpawner spawner = VehicleSpawner.GetSingletonQuick() ?? throw Context.SendGamemodeError();
            if (!Context.TryGetVehicleTarget(out InteractableVehicle? vehicleTarget))
            {
                ulong callerTeam = Context.Player.GetTeam();
                // find one linked vehicle owned by the player
                await spawner.WaitAsync(token);
                try
                {
                    await spawner.WriteWaitAsync(token);
                    try
                    {
                        foreach (SqlItem<Vehicles.VehicleSpawn> proxy in spawner.Items)
                        {
                            if (proxy.Item is not { } spawn)
                                continue;
                            
                            if (spawn.HasLinkedVehicle(out InteractableVehicle linked) && linked.lockedOwner.m_SteamID == Context.CallerId.m_SteamID &&
                                // ground assets can be anywhere, air assets need to be near main
                                (linked.asset.engine is not EEngine.HELICOPTER and not EEngine.PLANE && (Context.Player.Position - linked.transform.position).sqrMagnitude < 300 * 300
                                 || TeamManager.IsInMainOrAMC(callerTeam, Context.Player.Position) && TeamManager.IsInMainOrAMC(callerTeam, linked.transform.position)))
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
                    finally
                    {
                        spawner.WriteRelease();
                    }
                }
                finally
                {
                    spawner.Release();
                }

                await UniTask.SwitchToMainThread(token);
            }

            if (vehicleTarget == null)
                throw Context.Reply(T.VehicleMustBeLookingAtLinkedVehicle);
            bool owned = true;
            ulong team = vehicleTarget.lockedGroup.m_SteamID.GetTeam();
            if (team is not 1 and not 2)
            {
                team = Context.Player.GetTeam();
                if (team is not 1 and not 2)
                    throw Context.Reply(T.NotOnCaptureTeam);
            }
            else if (team != Context.Player.GetTeam())
                throw Context.Reply(T.VehicleNotOnSameTeam, TeamManager.GetFaction(team));
            if (vehicleTarget.lockedOwner.m_SteamID != Context.CallerId.m_SteamID)
            {
                if (!enter)
                {
                    OfflinePlayer pl = new OfflinePlayer(vehicleTarget.lockedOwner.m_SteamID);
                    await pl.CacheUsernames(token).ConfigureAwait(false);
                    throw Context.Reply(T.VehicleLinkedVehicleNotOwnedByCaller, pl);
                }

                owned = Context.Player.OnDuty();
            }

            // vehicle give <player>
            if (!enter && !kick)
            {
                if (!Context.TryGet(1, out _, out UCPlayer? onlinePlayer, TeamManager.EnumerateTeam(team), true, UCPlayer.NameSearch.NickName) || onlinePlayer == null)
                    throw Context.SendPlayerNotFound();

                VehicleManager.ServerSetVehicleLock(vehicleTarget, onlinePlayer.CSteamID, new CSteamID(TeamManager.GetGroupID(team)), true);
                VehicleComponent.TryAddOwnerToHistory(vehicleTarget, onlinePlayer.Steam64);
                if (Gamemode.Config.EffectUnlockVehicle.TryGetAsset(out EffectAsset? effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicleTarget.transform.position);
                onlinePlayer.SendChat(T.VehicleGivenDm, vehicleTarget.asset, Context.Player);
                throw Context.Reply(T.VehicleGiven, vehicleTarget.asset, onlinePlayer);
            }

            // find seat reference
            int seatIndex = -1;
            if (Context.MatchParameter(1, "driver", "pilot", "drive"))
                seatIndex = 0;
            else if (Context.HasArgsExact(1) && Context.MatchParameter(1, "turret", "gunner", "gun"))
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
            else if (Context.TryGetRange(1, out string? val) && (val.StartsWith("turret", StringComparison.InvariantCultureIgnoreCase) || val.StartsWith("gun", StringComparison.InvariantCultureIgnoreCase)))
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
                if (st >= 0 && int.TryParse(val[st..], NumberStyles.Number, Context.Culture, out int seat))
                {
                    if (seat > 0)
                        --seat;

                    if (vehicleTarget.turrets.Length > seat)
                        seatIndex = vehicleTarget.turrets[seat].turret.seatIndex;
                }
            }
            else if (Context.TryGet(1, out _, out UCPlayer? onlinePlayer, TeamManager.EnumerateTeam(team), true, UCPlayer.NameSearch.NickName) && onlinePlayer != null)
            {
                if (onlinePlayer.CurrentVehicle == vehicleTarget)
                    seatIndex = onlinePlayer.Player.movement.getSeat();
                else
                    throw Context.Reply(T.VehicleTargetNotInVehicle, onlinePlayer);
            }
            else if (Context.TryGet(1, out int seat))
            {
                if (seat > 0)
                    --seat;
                if (seat is >= 0 and <= byte.MaxValue)
                    seatIndex = (byte)seat;
            }
            else throw Context.SendCorrectUsage($"/vehicle {(kick ? "kick" : (owned ? "enter" : "swap"))} <player or seat>");

            if (vehicleTarget.passengers.Length <= seatIndex)
                throw Context.Reply(T.VehicleSeatNotValidOutOfRange, seatIndex + 1);
            if (seatIndex == -1)
                throw Context.Reply(T.VehicleSeatNotValidText, Context.Get(1)!);

            Passenger targetSeat = vehicleTarget.passengers[seatIndex];
            UCPlayer? target = targetSeat.player == null ? null : UCPlayer.FromSteamPlayer(targetSeat.player);

            float time = Time.realtimeSinceStartup;
            // vehicle kick <player or seat>
            if (kick)
            {
                if (target is not { IsOnline: true })
                    throw Context.Reply(T.VehicleSeatNotOccupied, seatIndex + 1);
                
                bool wantsFullKick = Context.MatchFlag("r", "k") && (vehicleTarget.asset.engine is not EEngine.PLANE and not EEngine.HELICOPTER ||
                                                            Mathf.Abs(vehicleTarget.ReplicatedSpeed) <= 0.15f || TeamManager.IsInMain(team, vehicleTarget.transform.position));
                if (wantsFullKick || !VehicleUtility.TryMovePlayerToEmptySeat(target.Player))
                {
                    L.LogDebug("Removing target instead of moving.");
                    VehicleManager.forceRemovePlayer(vehicleTarget, target.CSteamID);
                }
                vehicleTarget.lastSeat = time;
                if (CooldownManager.IsLoaded)
                    CooldownManager.StartCooldown(target, CooldownType.InteractVehicleSeats, 5f, vehicleTarget);
                target.SendChat(T.VehicleOwnerKickedDM, vehicleTarget.asset, Context.Player, seatIndex + 1);
                throw Context.Reply(T.VehicleKickedPlayer, vehicleTarget.asset, target, seatIndex + 1);
            }

            throw Context.SendNotImplemented();
        }

        bool deny = Context.MatchParameter(0, "deny", "no", "n");
        if (deny || Context.MatchParameter(0, "accept", "yes", "y"))
        {
            throw Context.SendNotImplemented();
        }

        await Context.AssertPermissions(PermissionSpawn, token);

        enter = Context.MatchFlag("e", "enter");

        Context.AssertArgs(1, AdminSyntax);

        Context.AssertOnDuty();

        if (!Context.TryGet(0, out VehicleAsset? asset, out _, true, allowMultipleResults: true))
            throw Context.ReplyString("<color=#8f9494>Unable to find a vehicle by the name or id: <color=#dddddd>" + Context.GetRange(0) + "</color>.</color>");

        Vector3 ppos = Context.Player.Position;
        Vector3 v = Context.Player.UnturnedPlayer.look.aim.forward.normalized with { y = 0 };
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
            Quaternion.Euler(Context.Player.UnturnedPlayer.look.aim.rotation.eulerAngles with { x = 0, z = 0 }),
            false,
            false,
            false,
            false,
            asset.fuel,
            asset.health,
            10000,
            Context.CallerId,
            Context.Player.UnturnedPlayer.quests.groupID,
            true,
            turrets,
            255);
        
        if (enter)
            VehicleManager.ServerForcePassengerIntoVehicle(Context.Player.UnturnedPlayer, vehicle);

        Context.ReplyString($"Spawned a <color=#dddddd>{vehicle.asset.vehicleName}</color> (<color=#aaaaaa>{vehicle.asset.id}</color>).", "bfb9ac");
    }
}


public struct VehicleSwapRequest(float sendTime, InteractableVehicle vehicle, UCPlayer sender, CancellationTokenSource token)
{
    public readonly float SendTime = sendTime;
    public readonly InteractableVehicle Vehicle = vehicle;
    public readonly UCPlayer Sender = sender;
    public readonly CancellationTokenSource RespondToken = token;
    public bool? IsDenied = null;
}