using SDG.Unturned;
using Steamworks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class RequestCommand : AsyncCommand, ICompoundingCooldownCommand
{
    private static readonly Guid KitSign = new Guid("275dd81d60ae443e91f0655b8b7aa920");
    public float CompoundMultiplier => 2f;
    public float MaxCooldown => 900f; // 15 mins

    private const string Syntax = "/request [save|remove]";
    private const string Help = "Request a kit by targeting a sign or request a vehicle by targeting the vehicle or it's sign while doing /request.";

    public RequestCommand() : base("request", EAdminType.MEMBER, sync: true)
    {
        AddAlias("req");
        Structure = new CommandStructure
        {
            Description = "Request a kit or a vehicle by looking at their respective signs (or the actual vehicle).",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Upgrade")
                {
                    Description = "Use to upgrade old loadouts.",
                    Aliases = new string[] { "update" },
                    IsOptional = true
                }
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        BarricadeDrop? drop;
        string? kitId = null;
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
            if (ctx.MatchParameter(0, "upgrade", "update"))
            {
                if (ctx.TryGetTarget(out drop) || ctx.TryGet(1, out kitId))
                {
                    ctx.AssertRanByPlayer();
                    ulong discordId = await Data.AdminSql.GetDiscordID(ctx.CallerID, token).ConfigureAwait(false);
                    if (discordId == 0)
                    {
                        await UCWarfare.ToUpdate(token);
                        ctx.Reply(T.DiscordNotLinked);
                        throw ctx.Reply(T.DiscordNotLinked2, ctx.Caller);
                    }

                    bool? inDiscordServer = await PlayerManager.IsUserInDiscordServer(discordId).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    if (inDiscordServer.HasValue)
                    {
                        if (!inDiscordServer.Value)
                            throw ctx.Reply(T.RequestUpgradeNotInDiscordServer);
                    }
                    else
                        throw ctx.Reply(T.RequestUpgradeNotConnected);
                    
                    KitManager? manager = KitManager.GetSingletonQuick();
                    if (manager == null)
                        throw ctx.SendGamemodeError();

                    Kit? kit;
                    if (drop != null && kitId == null)
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
                            kit = await manager.GetKitFromSign(drop, ctx.Caller, token);
                        }
                        else throw ctx.Reply(T.RequestNoTarget);
                    }
                    else if (kitId != null)
                    {
                        kit = await manager.FindKit(kitId, token, false);
                    }
                    else throw ctx.SendUnknownError();

                    if (kit == null)
                        throw ctx.Reply(T.KitNotFound, kitId ?? "???");

                    if (kit.Type != KitType.Loadout)
                        throw ctx.Reply(T.RequestUpgradeOnKit, kit);

                    if (!kit.NeedsUpgrade)
                        throw ctx.Reply(T.DoesNotNeedUpgrade, kit);

                    int id = KitEx.ParseStandardLoadoutId(kit.InternalName, out ulong playerId);
                    if (ctx.CallerID != playerId)
                    {
                        // requesting upgrade for a different player's kit
                        ctx.AssertOnDuty();

                        discordId = await Data.AdminSql.GetDiscordID(playerId, token).ConfigureAwait(false);
                        if (discordId == 0)
                        {
                            await UCWarfare.ToUpdate(token);
                            throw ctx.Reply(T.DiscordNotLinked);
                        }
                        inDiscordServer = await PlayerManager.IsUserInDiscordServer(discordId).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
                        if (inDiscordServer.HasValue)
                        {
                            if (!inDiscordServer.Value)
                                throw ctx.Reply(T.RequestUpgradeNotInDiscordServer);
                        }
                        else
                            throw ctx.Reply(T.RequestUpgradeNotConnected);
                    }
                    if (UCWarfare.CanUseNetCall)
                    {
                        RequestResponse response = await KitEx.NetCalls.RequestIsModifyLoadoutTicketOpen.RequestAck(UCWarfare.I.NetClient!, discordId, id, 7500);
                        if (!response.Responded)
                        {
                            await UCWarfare.ToUpdate(token);
                            throw ctx.Reply(T.RequestUpgradeNotConnected);
                        }
                        if (response.ErrorCode is not (int)StandardErrorCode.NotFound)
                        {
                            await UCWarfare.ToUpdate(token);
                            if (response.ErrorCode is (int)StandardErrorCode.Success)
                            {
                                throw ctx.Reply(T.RequestUpgradeAlreadyOpen, kit);
                            }

                            if (response.ErrorCode is (int)StandardErrorCode.NotSupported)
                            {
                                throw ctx.Reply(T.RequestUpgradeTooManyTicketsOpen);
                            }

                            throw ctx.Reply(T.RequestUpgradeError, response.ErrorCode.HasValue ? ((StandardErrorCode)response.ErrorCode.Value).ToString() : "NULL");
                        }
                    }
                    else
                    {
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.RequestUpgradeNotConnected);
                    }
                    if (!kit.NeedsUpgrade)
                    {
                        await UCWarfare.ToUpdate(token);
                        throw ctx.Reply(T.DoesNotNeedUpgrade, kit);
                    }

                    bool success = await KitEx.OpenUpgradeTicket(kit.GetDisplayName(), kit.Class, id, playerId, discordId, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    if (!success)
                        throw ctx.Reply(T.RequestUpgradeNotConnected);

                    throw ctx.Reply(T.TicketOpened, kit);
                }

                throw ctx.Reply(T.RequestNoTarget);
            }

            throw ctx.SendCorrectUsage(Syntax + " - " + Help);
        }

        InteractableVehicle vehicle;
        ctx.AssertRanByPlayer();
        if (ctx.TryGetTarget(out drop))
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
                Kit? kit = Signs.GetKitFromSign(drop, out int loadoutId);
                if (kit != null || loadoutId > 0)
                {
                    ctx.AssertGamemode(out IKitRequests gm);
                    KitManager manager = gm.KitManager;

                    if (loadoutId > 0)
                        await manager.Requests.RequestLoadout(loadoutId, ctx, token).ConfigureAwait(false);
                    else
                    {
                        kit = await manager.GetKit(kit!.PrimaryKey, token, x => KitManager.RequestableSet(x, true)).ConfigureAwait(false);

                        if (kit == null)
                            throw ctx.Reply(T.RequestNoTarget);

                        await manager.Requests.RequestKit(kit, ctx, token).ConfigureAwait(false);
                    }
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

            if (!Data.Is(out IVehicles vgm) || !vgm.VehicleSpawner.TryGetSpawn(drop, out SqlItem<VehicleSpawn> vbsign))
                throw ctx.Reply(T.RequestNoTarget);
            
            VehicleSpawn? spawn = vbsign.Item;
            if (spawn == null)
                throw ctx.Reply(T.RequestNoTarget);
                
            ctx.AssertNotOnPortionCooldown();
            if (!spawn.HasLinkedVehicle(out vehicle))
            {
                ctx.PortionCommandCooldownTime = 5f;
                throw ctx.Reply(T.RequestVehicleDead, spawn.Vehicle?.Item!);
            }

            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay is not { IsLoaded: true })
                throw ctx.SendGamemodeError();

            SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (data?.Item == null)
                throw ctx.Reply(T.RequestNoTarget);

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

        if (ctx.TryGetTarget(out vehicle))
        {
            await UCWarfare.ToUpdate();
            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay is { IsLoaded: true })
            {
                SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                if (data?.Item != null)
                {
                    ctx.AssertNotOnPortionCooldown();
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
        else if (ctx.TryGetTarget(out StructureDrop structure))
        {
            if (Data.Is(out IVehicles vgm) && vgm.VehicleSpawner.TryGetSpawn(structure, out SqlItem<VehicleSpawn> spawnProxy))
            {
                VehicleSpawn? spawn = spawnProxy.Item;
                if (spawn == null)
                    throw ctx.Reply(T.RequestNoTarget);

                ctx.AssertNotOnPortionCooldown();
                if (!spawn.HasLinkedVehicle(out vehicle))
                {
                    ctx.PortionCommandCooldownTime = 5f;
                    throw ctx.Reply(T.RequestVehicleDead, spawn.Vehicle?.Item!);
                }

                VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
                if (bay is { IsLoaded: true })
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
                        // ReSharper disable once RedundantJumpStatement
                        return;
                    }
                }
                else throw ctx.SendGamemodeError();
            }
        }
        else
        {
            // checks for spamming /req where the vehicle will be once it spawns
            if (Gamemode.Config.StructureVehicleBay.ValidReference(out Guid guid) &&
                UCBarricadeManager.IsStructureNearby(guid, 20f, ctx.Caller.Position, out _) &&
                UCBarricadeManager.CountNearbyBarricades(KitSign, 8f, ctx.Caller.Position) < 5)
            {
                ctx.PortionCommandCooldownTime = 5f;
            }

            await UCWarfare.ToUpdate(token);
            throw ctx.Reply(T.RequestNoTarget);
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
        {
            IPlayer names = await F.GetPlayerOriginalNamesAsync(vehicle.lockedOwner.m_SteamID, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            throw ctx.Reply(T.RequestVehicleAlreadyRequested, names);
        }

        if (data.Team != 0 && data.Team != team)
            throw ctx.Reply(T.RequestVehicleOtherTeam, TeamManager.GetFactionSafe(data.Team)!);
        if (data.RequiresSL && ctx.Caller.Squad == null)
            throw ctx.Reply(T.RequestVehicleNotSquadLeader);

        Kit? kit = await ctx.Caller.GetActiveKit(token).ConfigureAwait(false);
        if (kit == null)
            throw ctx.Reply(T.RequestVehicleNoKit);

        await UCWarfare.ToUpdate(token);

        if (ctx.Caller.Level.Level < data.UnlockLevel)
            throw ctx.Reply(T.RequestVehicleMissingLevels, new LevelData(Points.GetLevelXP(data.UnlockLevel)));
        if (data.RequiredClass != Class.None && kit.Class != data.RequiredClass)
            throw ctx.Reply(T.RequestVehicleWrongClass, data.RequiredClass);
        if (ctx.Caller.CachedCredits < data.CreditCost)
            throw ctx.Reply(T.RequestVehicleCantAfford, ctx.Caller.CachedCredits, data.CreditCost);
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
                            throw ctx.Reply(T.RequestVehicleAlreadyOwned, veh);
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
            ctx.PortionCommandCooldownTime = 5f;

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
            AssetBan? assetBan = await Data.ModerationSql.GetActiveAssetBan(ctx.CallerID, data.Type, token: token).ConfigureAwait(false);
            if (assetBan != null)
            {
                if (assetBan.VehicleTypeFilter.Length == 0)
                {
                    if (assetBan.IsPermanent)
                        throw ctx.Reply(T.RequestVehicleAssetBannedGlobalPermanent);

                    throw ctx.Reply(T.RequestVehicleAssetBannedGlobal, assetBan.GetTimeUntilExpiry(false));
                }

                if (assetBan.IsPermanent)
                    throw ctx.Reply(T.RequestVehicleAssetBannedPermanent, assetBan.GetCommaList(false));

                throw ctx.Reply(T.RequestVehicleAssetBanned, assetBan.GetTimeUntilExpiry(false), assetBan.GetCommaList(false));
            }

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
                        ctx.Caller.SendChat(T.RequestVehicleCantAfford, ctx.Caller.CachedCredits, data.CreditCost);
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
            // Stats.StatsManager.ModifyStats(ctx.Caller.Steam64, x => x.VehiclesRequested++, false);
            // Stats.StatsManager.ModifyTeam(team, t => t.VehiclesRequested++, false);
            // Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesRequested++);
            CooldownManager.StartCooldown(ctx.Caller, CooldownType.RequestVehicle, CooldownManager.Config.RequestVehicleCooldown, vehicle.id);
        }
        else
        {
            ctx.Caller.SendChat(T.RequestVehicleAlreadyRequested);
        }
    }

    internal static void GiveVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, VehicleData data)
    {
        VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);
        VehicleComponent.TryAddOwnerToHistory(vehicle, ucplayer.Steam64);

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

        if (!FOBManager.Config.Buildables.Exists(e => e.Type == BuildableType.Emplacement && e.Emplacement != null && e.Emplacement.EmplacementVehicle.MatchGuid(vehicle.asset.GUID)))
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
