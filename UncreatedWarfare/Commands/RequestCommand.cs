using SDG.Unturned;
using Steamworks;
using System;
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
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class RequestCommand : AsyncCommand
{
    private const string Syntax = "/request [save|remove]";
    private const string Help = "Request a kit by targeting a sign or request a vehicle by targeting the vehicle or it's sign while doing /request.";

    public RequestCommand() : base("request", EAdminType.MEMBER, sync: true)
    {
        AddAlias("req");
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        
        if (ctx.HasArg(0))
        {
            if (ctx.MatchParameter(0, "save"))
            {
                ctx.AssertPermissions(EAdminType.STAFF);
                throw ctx.ReplyString("Saving request signs should now be done using the structure command.");
            }
            if (ctx.MatchParameter(0, "remove", "delete"))
            {
                ctx.AssertPermissions(EAdminType.STAFF);
                throw ctx.ReplyString("Removing request signs should now be done using the structure command.");
            }
            throw ctx.SendCorrectUsage(Syntax + " - " + Help);
        }
        if (ctx.TryGetTarget(out BarricadeDrop drop))
        {
            StructureSaver? saver = StructureSaver.GetSingletonQuick();
            InteractableSign? sign = drop.interactable as InteractableSign;
            if (saver != null && await saver.GetSave(drop, token).ConfigureAwait(false) == null)
            {
                await UCWarfare.ToUpdate(token);
                throw ctx.Reply(T.RequestKitNotRegistered);
            }
            await UCWarfare.ToUpdate(token);
            if (sign != null)
            {
                SqlItem<Kit>? proxy = Signs.GetKitFromSign(drop, out int loadoutId);
                if (proxy?.Item != null || loadoutId > 0)
                {
                    ctx.AssertGamemode(out IKitRequests gm);
                    KitManager manager = gm.KitManager;

                    if (loadoutId > 0)
                        await manager.RequestLoadout(loadoutId, ctx, token).ConfigureAwait(false);
                    else
                        await manager.RequestKit(proxy!, ctx, token).ConfigureAwait(false);
                    return;
                }
                if (TraitManager.Loaded && sign.text.StartsWith(Signs.Prefix + Signs.TraitPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.AssertGamemode<ITraits>();

                    if (!TraitManager.Loaded)
                        throw ctx.SendGamemodeError();

                    TraitData? d = TraitManager.GetData(sign.text.Substring(Signs.Prefix.Length + Signs.TraitPrefix.Length));
                    if (d == null)
                        throw ctx.Reply(T.RequestNoTarget);

                    TraitManager.RequestTrait(d, ctx);
                    return;
                }
            }

            if (Data.Is(out IVehicles vgm) && vgm.VehicleSpawner.TryGetSpawn(drop, out SqlItem<VehicleSpawn> vbsign))
            {
                if (vbsign.Item != null && vbsign.Item.HasLinkedVehicle(out InteractableVehicle vehicle))
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
                else throw ctx.Reply(T.RequestVehicleDead, vbsign.Item?.Vehicle?.Item!);
            }
            throw ctx.Reply(T.RequestNoTarget);
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
    /// <remarks>Thread Safe</remarks>
    internal Task RequestVehicle(CommandInteraction ctx, InteractableVehicle vehicle, VehicleData data, CancellationToken token = default) => RequestVehicle(ctx, vehicle, data, ctx.Caller.GetTeam(), token);
    /// <remarks>Thread Safe</remarks>
    internal async Task RequestVehicle(CommandInteraction ctx, InteractableVehicle vehicle, VehicleData data, ulong team, CancellationToken token = default)
    {
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate();
        if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
        {
            IPlayer names = await F.GetPlayerOriginalNamesAsync(vehicle.lockedOwner.m_SteamID, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            throw ctx.Reply(T.RequestVehicleAlreadyRequested, names);
        }

        if (data.Team != 0 && data.Team != team)
            throw ctx.Reply(T.RequestVehicleOtherTeam, TeamManager.GetFactionSafe(data.Team)!);
        if (data.RequiresSL && ctx.Caller.Squad == null)
            throw ctx.Reply(T.RequestVehicleNotSquadLeader);

        SqlItem<Kit>? proxy = ctx.Caller.ActiveKit;
        if (proxy?.Item == null)
            throw ctx.Reply(T.RequestVehicleNoKit);

        await proxy.Enter(token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        try
        {
            if (proxy.Item == null)
                throw ctx.Reply(T.RequestVehicleNoKit);

            Kit kit = proxy.Item;
            if (data.RequiredClass != Class.None && kit.Class != data.RequiredClass)
                throw ctx.Reply(T.RequestVehicleWrongClass, data.RequiredClass);
            if (ctx.Caller.CachedCredits < data.CreditCost)
                throw ctx.Reply(T.RequestVehicleCantAfford, data.CreditCost - ctx.Caller.CachedCredits, data.CreditCost);
            if (CooldownManager.HasCooldown(ctx.Caller, CooldownType.RequestVehicle, out Cooldown cooldown, vehicle.id))
                throw ctx.Reply(T.RequestVehicleCooldown, cooldown);

            // check if an owned vehicle is nearby
            if (Data.Is(out IVehicles vgm))
            {
                vgm.VehicleSpawner.WriteWait();
                try
                {
                    for (int i = 0; i < vgm.VehicleSpawner.Items.Count; ++i)
                    {
                        if (vgm.VehicleSpawner.Items[i]?.Item is { } item && item.HasLinkedVehicle(out InteractableVehicle veh))
                        {
                            if (veh == null || veh.isDead || veh.isExploded)
                                continue;
                            if (veh.lockedOwner.m_SteamID == ctx.CallerID && (veh.transform.position - vehicle.transform.position).sqrMagnitude <
                                UCWarfare.Config.MaxVehicleAbandonmentDistance * UCWarfare.Config.MaxVehicleAbandonmentDistance)
                                throw ctx.Reply(T.RequestVehicleAlreadyOwned, data);
                        }
                    }
                }
                finally
                {
                    vgm.VehicleSpawner.WriteRelease();
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
                CooldownManager.StartCooldown(ctx.Caller, CooldownType.RequestVehicle, CooldownManager.Config.RequestVehicleCooldown, vehicle.id);
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

        if (Data.Is(out IVehicles vgm) && vgm.VehicleSpawner.TryGetSpawn(vehicle, out SqlItem<VehicleSpawn> spawn))
        {
            if (spawn.Item?.Structure?.Item?.Buildable?.Model?.GetComponent<VehicleBayComponent>() is { } comp)
            {
                comp.OnRequest();
                ActionLog.Add(ActionLogType.RequestVehicle, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N} at spawn {comp.gameObject.transform.position.ToString("N2", Data.AdminLocale)}", ucplayer);
            }
            else
                ActionLog.Add(ActionLogType.RequestVehicle, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);
            Data.Reporter?.OnVehicleRequest(ucplayer.Steam64, vehicle.asset.GUID, spawn.PrimaryKey);
        }
        else
            ActionLog.Add(ActionLogType.RequestVehicle, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);

        vehicle.updateVehicle();
        vehicle.updatePhysics();

        if (Gamemode.Config.EffectUnlockVehicle.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);

        ucplayer.SendChat(T.RequestVehicleSuccess, data);

        if (!FOBManager.Config.Buildables.Exists(e => e.Type == EBuildableType.EMPLACEMENT && e.Emplacement != null && e.Emplacement.EmplacementVehicle.MatchGuid(vehicle.asset.GUID)))
        {
            ItemManager.dropItem(new Item(28, true), ucplayer.Position, true, true, true);  // gas can
            ItemManager.dropItem(new Item(277, true), ucplayer.Position, true, true, true); // car jack
        }
        foreach (Guid item in data.Items)
        {
            if (Assets.find(item) is ItemAsset a)
                ItemManager.dropItem(new Item(a.id, true), ucplayer.Position, true, true, true);
        }
    }
}
