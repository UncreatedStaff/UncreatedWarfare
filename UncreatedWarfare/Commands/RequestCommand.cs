using SDG.Unturned;
using Steamworks;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class RequestCommand : AsyncCommand
{
    private const string SYNTAX = "/request [save|remove]";
    private const string HELP = "Request a kit by targeting a sign or request a vehicle by targeting the vehicle or it's sign while doing /request.";

    public RequestCommand() : base("request", EAdminType.MEMBER)
    {
        AddAlias("req");
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);
        
        if (ctx.HasArg(0))
        {
            if (ctx.MatchParameter(0, "save"))
            {
                ctx.AssertGamemode<IKitRequests>();
                ctx.AssertPermissions(EAdminType.STAFF);

                if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
                {
                    // todo redo request signs
                    if (RequestSigns.AddRequestSign(sign, out RequestSign signadded))
                    {
                        ctx.SendUnknownError();
                        ctx.LogAction(EActionLogType.SAVE_REQUEST_SIGN, signadded.KitName);
                    }
                    else throw ctx.Reply(T.RequestSignAlreadySaved); // sign already registered
                }
                else throw ctx.Reply(T.RequestNoTarget);
            }
            else if (ctx.MatchParameter(0, "remove", "delete"))
            {
                ctx.AssertGamemode<IKitRequests>();
                ctx.AssertPermissions(EAdminType.STAFF);

                // todo redo request signs
                if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
                {
                    if (RequestSigns.SignExists(sign, out RequestSign requestsign))
                    {
                        ctx.SendUnknownError();
                        RequestSigns.RemoveRequestSign(requestsign);
                        ctx.LogAction(EActionLogType.UNSAVE_REQUEST_SIGN, requestsign.KitName);
                    }
                    else throw ctx.Reply(T.RequestSignNotSaved);
                }
                else throw ctx.Reply(T.RequestNoTarget);
            }
            else throw ctx.SendCorrectUsage(SYNTAX + " - " + HELP);
        }
        else
        {
            if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
            {
                if (RequestSigns.Loaded && RequestSigns.SignExists(sign, out RequestSign kitsign))
                {
                    ctx.AssertGamemode(out IKitRequests gm);
                    KitManager manager = gm.KitManager;
                    
                    if (kitsign.KitName.StartsWith(Signs.LOADOUT_PREFIX, StringComparison.OrdinalIgnoreCase))
                    {
                        if (byte.TryParse(kitsign.KitName.Substring(8), NumberStyles.Number, Data.AdminLocale, out byte loadoutId))
                        {
                            if (loadoutId > 0)
                            {
                                await manager.RequestLoadout(loadoutId, ctx, token).ConfigureAwait(false);
                            }
                            else throw ctx.Reply(T.RequestLoadoutNotOwned);
                        }
                        else throw ctx.Reply(T.RequestLoadoutNotOwned);
                    }
                    else
                    {
                        SqlItem<Kit>? proxy = await manager.FindKit(kitsign.KitName, token).ConfigureAwait(false);
                        if (proxy?.Item == null)
                            throw ctx.Reply(T.KitNotFound, kitsign.KitName);
                        await manager.RequestKit(proxy, ctx, token).ConfigureAwait(false);
                    }
                }
                else if (VehicleSigns.Loaded && VehicleSigns.SignExists(sign, out VehicleSign vbsign))
                {
                    ctx.AssertGamemode<IVehicles>();

                    if (vbsign.VehicleBay.HasLinkedVehicle(out InteractableVehicle vehicle))
                    {
                        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
                        if (bay != null && bay.IsLoaded)
                        {
                            SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate();
                            if (data?.Item != null)
                            {
                                await data.Enter(token).ConfigureAwait(false);
                                try
                                {
                                    await RequestVehicle(ctx, vehicle, data.Item, token);
                                    ctx.Defer();
                                }
                                finally
                                {
                                    data.Release();
                                }
                                return;
                            }
                        }
                        else throw ctx.SendGamemodeError();
                    }
                    throw ctx.Reply(T.RequestNoTarget);
                }
                else if (TraitManager.Loaded && sign.text.StartsWith(TraitSigns.TRAIT_SIGN_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.AssertGamemode<ITraits>();

                    if (!TraitManager.Loaded)
                        throw ctx.SendGamemodeError();

                    TraitData? d = TraitManager.GetData(sign.text.Substring(TraitSigns.TRAIT_SIGN_PREFIX.Length));
                    if (d == null)
                        throw ctx.Reply(T.RequestNoTarget);

                    TraitManager.RequestTrait(d, ctx);
                }
                else throw ctx.Reply(T.RequestNoTarget);
            }
            else if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
                if (bay != null && bay.IsLoaded)
                {
                    SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (data?.Item != null)
                    {
                        await data.Enter(token).ConfigureAwait(false);
                        try
                        {
                            await RequestVehicle(ctx, vehicle, data.Item, token).ConfigureAwait(false);
                            ctx.Defer();
                        }
                        finally
                        {
                            data.Release();
                        }
                    }
                    else throw ctx.Reply(T.RequestNoTarget);
                }
                else throw ctx.SendGamemodeError();
            }
            else throw ctx.Reply(T.RequestNoTarget);
        }
    }
    /// <remarks>Thread Safe</remarks>
    internal Task RequestVehicle(CommandInteraction ctx, InteractableVehicle vehicle, VehicleData data, CancellationToken token = default) => RequestVehicle(ctx, vehicle, data, ctx.Caller.GetTeam(), token);
    /// <remarks>Thread Safe</remarks>
    internal async Task RequestVehicle(CommandInteraction ctx, InteractableVehicle vehicle, VehicleData data, ulong team, CancellationToken token = default)
    {
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate();
        if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
            throw ctx.Reply(T.RequestVehicleAlreadyRequested, await F.GetPlayerOriginalNamesAsync(vehicle.lockedOwner.m_SteamID, token).ThenToUpdate(token));

        if (data.Team != 0 && data.Team != team)
            throw ctx.Reply(T.RequestVehicleOtherTeam, TeamManager.GetFactionSafe(data.Team)!);
        if (data.RequiresSL && ctx.Caller.Squad == null)
            throw ctx.Reply(T.RequestVehicleNotSquadLeader);

        SqlItem<Kit>? proxy = ctx.Caller.ActiveKit;
        if (proxy?.Item == null)
            throw ctx.Reply(T.RequestVehicleNoKit);

        await proxy.Enter(token).ThenToUpdate(token);
        try
        {
            if (proxy.Item == null)
                throw ctx.Reply(T.RequestVehicleNoKit);

            Kit kit = proxy.Item;
            if (data.RequiredClass != Class.None && kit.Class != data.RequiredClass)
                throw ctx.Reply(T.RequestVehicleWrongClass, data.RequiredClass);
            if (ctx.Caller.CachedCredits < data.CreditCost)
                throw ctx.Reply(T.RequestVehicleCantAfford, data.CreditCost - ctx.Caller.CachedCredits, data.CreditCost);
            if (CooldownManager.HasCooldown(ctx.Caller, ECooldownType.REQUEST_VEHICLE, out Cooldown cooldown, vehicle.id))
                throw ctx.Reply(T.RequestVehicleCooldown, cooldown);

            if (VehicleSpawner.Loaded) // check if an owned vehicle is nearby
            {
                foreach (VehicleSpawn spawn in VehicleSpawner.Spawners)
                {
                    if (spawn is not null && spawn.HasLinkedVehicle(out InteractableVehicle veh))
                    {
                        if (veh == null || veh.isDead) continue;
                        if (veh.lockedOwner.m_SteamID == ctx.CallerID &&
                            (veh.transform.position - vehicle.transform.position).sqrMagnitude < UCWarfare.Config.MaxVehicleAbandonmentDistance * UCWarfare.Config.MaxVehicleAbandonmentDistance)
                            throw ctx.Reply(T.RequestVehicleAlreadyOwned, data);
                    }
                }
            }
            if (data.IsDelayed(out Delay delay) && delay.Type != DelayType.None)
            {
                Localization.SendDelayRequestText(in delay, ctx.Caller, team, Localization.DelayTarget.VehicleBay);
                ctx.Defer();
                return;
            }

            for (int i = 0; i < data.UnlockRequirements.Length; i++)
            {
                UnlockRequirement req = data.UnlockRequirements[i];
                if (req.CanAccess(ctx.Caller))
                    continue;
                throw req.RequestVehicleFailureToMeet(ctx, data);
            }

            if (vehicle.asset.canBeLocked)
            {
                if (data.CreditCost > 0)
                {
                    await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        await Points.UpdatePointsAsync(ctx.Caller, false, token).ConfigureAwait(false);
                        if (ctx.Caller.CachedCredits >= data.CreditCost)
                        {
                            await Points.AwardCreditsAsync(ctx.Caller, -data.CreditCost, isPurchase: true, @lock: false, token: token).ConfigureAwait(false);
                        }
                        else
                        {
                            await UCWarfare.ToUpdate(token);
                            ctx.Caller.SendChat(T.RequestVehicleCantAfford, data.CreditCost - ctx.Caller.CachedCredits, data.CreditCost);
                            return;
                        }
                    }
                    finally
                    {
                        ctx.Caller.PurchaseSync.Release();
                    }
                }

                await UCWarfare.ToUpdate(token);
                GiveVehicle(ctx.Caller, vehicle, data);
                Stats.StatsManager.ModifyStats(ctx.Caller.Steam64, x => x.VehiclesRequested++, false);
                Stats.StatsManager.ModifyTeam(team, t => t.VehiclesRequested++, false);
                Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesRequested++);
                CooldownManager.StartCooldown(ctx.Caller, ECooldownType.REQUEST_VEHICLE, CooldownManager.Config.RequestVehicleCooldown, vehicle.id);
            }
            else
            {
                ctx.Caller.SendChat(T.RequestVehicleAlreadyRequested);
            }
        }
        finally
        {
            proxy.Release();
        }
    }

    internal static void GiveVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, VehicleData data)
    {
        vehicle.tellLocked(ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

        VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

        if (VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
        {
            VehicleBayComponent? comp = spawn.Component;
            if (comp != null)
            {
                comp.OnRequest();
                ActionLogger.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N} at spawn {comp.gameObject.transform.position.ToString("N2", Data.AdminLocale)}", ucplayer);
            }
            else
                ActionLogger.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);
            Data.Reporter?.OnVehicleRequest(ucplayer.Steam64, vehicle.asset.GUID, spawn.InstanceId);
        }
        else
            ActionLogger.Add(EActionLogType.REQUEST_VEHICLE, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);

        vehicle.updateVehicle();
        vehicle.updatePhysics();

        if (Gamemode.Config.EffectUnlockVehicle.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);

        ucplayer.SendChat(T.RequestVehicleSuccess, data);

        if (!FOBManager.Config.Buildables.Exists(e => e.Type == EBuildableType.EMPLACEMENT && e.Emplacement != null && e.Emplacement.EmplacementVehicle.MatchGuid(vehicle.asset.GUID)))
        {
            ItemManager.dropItem(new Item(28, true), ucplayer.Position, true, true, true); // gas can
            ItemManager.dropItem(new Item(277, true), ucplayer.Position, true, true, true); // car jack
        }
        foreach (Guid item in data.Items)
        {
            if (Assets.find(item) is ItemAsset a)
                ItemManager.dropItem(new Item(a.id, true), ucplayer.Position, true, true, true);
        }
    }
}
