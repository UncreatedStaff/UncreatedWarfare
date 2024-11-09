﻿using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

public class RequestCommand2 : ICompoundingCooldownCommand
{
    private static readonly IAssetLink<ItemBarricadeAsset> KitSign = AssetLink.Create<ItemBarricadeAsset>("275dd81d60ae443e91f0655b8b7aa920");

    private static readonly PermissionLeaf PermissionUpgrade    = new PermissionLeaf("commands.request.upgrade", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionRequest    = new PermissionLeaf("commands.request.look", unturned: false, warfare: true);
    public float CompoundMultiplier => 2f;
    public float MaxCooldown => 900f; // 15 mins

    private const string Syntax = "/request [upgrade]";
    private const string Help = "Request a kit or a vehicle by looking at their respective signs (or the actual vehicle).";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Upgrade")
                {
                    Permission = PermissionUpgrade,
                    Description = "Use to upgrade old loadouts.",
                    Aliases = [ "update" ],
                    IsOptional = true
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
#if false
        Context.AssertHelpCheck(0, Syntax + " - " + Help);
        BarricadeDrop? drop;
        string? kitId = null;

        await Context.AssertPermissions(PermissionRequest, token);
        await UniTask.SwitchToMainThread(token);

        InteractableVehicle? vehicle;
        Context.AssertRanByPlayer();
        if (Context.TryGetBarricadeTarget(out drop))
        {
            StructureSaver? saver = StructureSaver.GetSingletonQuick();
            InteractableSign? sign = drop.interactable as InteractableSign;
            if (saver != null && await saver.GetSave(drop, token).ConfigureAwait(false) == null)
            {
                await UniTask.SwitchToMainThread(token);
                throw Context.Reply(T.RequestKitNotRegistered);
            }
            await UniTask.SwitchToMainThread(token);

            if (!Data.Is(out IVehicles vgm) || !vgm.VehicleSpawner.TryGetSpawn(drop, out SqlItem<VehicleSpawn> vbsign))
                throw Context.Reply(T.RequestNoTarget);
            
            VehicleSpawn? spawn = vbsign.Item;
            if (spawn == null)
                throw Context.Reply(T.RequestNoTarget);
                
            Context.AssertCommandNotOnIsolatedCooldown();
            if (!spawn.HasLinkedVehicle(out vehicle))
            {
                Context.IsolatedCommandCooldownTime = 5f;
                throw Context.Reply(T.RequestVehicleDead, spawn.Vehicle?.Item!);
            }

            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay is not { IsLoaded: true })
                throw Context.SendGamemodeError();

            SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (data?.Item == null)
                throw Context.Reply(T.RequestNoTarget);

            await data.Enter(token).ConfigureAwait(false);
            try
            {
                await RequestVehicle(Context, vehicle, data.Item, token);
                Context.Defer();
            }
            finally
            {
                data.Release();
            }
            return;

        }

        if (Context.TryGetVehicleTarget(out vehicle))
        {
            await UniTask.SwitchToMainThread(token);
            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay is { IsLoaded: true })
            {
                SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                if (data?.Item != null)
                {
                    Context.AssertCommandNotOnIsolatedCooldown();
                    await data.Enter(token).ConfigureAwait(false);
                    try
                    {
                        await RequestVehicle(Context, vehicle, data.Item, token).ConfigureAwait(false);
                        Context.Defer();
                    }
                    finally
                    {
                        data.Release();
                    }
                }
                else throw Context.Reply(T.RequestNoTarget);
            }
            else throw Context.SendGamemodeError();
        }
        else if (Context.TryGetStructureTarget(out StructureDrop? structure))
        {
            if (Data.Is(out IVehicles vgm) && vgm.VehicleSpawner.TryGetSpawn(structure, out SqlItem<VehicleSpawn> spawnProxy))
            {
                VehicleSpawn? spawn = spawnProxy.Item;
                if (spawn == null)
                    throw Context.Reply(T.RequestNoTarget);

                Context.AssertCommandNotOnIsolatedCooldown();
                if (!spawn.HasLinkedVehicle(out vehicle))
                {
                    Context.IsolatedCommandCooldownTime = 5f;
                    throw Context.Reply(T.RequestVehicleDead, spawn.Vehicle?.Item!);
                }

                VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
                if (bay is { IsLoaded: true })
                {
                    SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    if (data?.Item != null)
                    {
                        await data.Enter(token).ConfigureAwait(false);
                        try
                        {
                            await RequestVehicle(Context, vehicle, data.Item, token);
                            Context.Defer();
                        }
                        finally
                        {
                            data.Release();
                        }
                        // ReSharper disable once RedundantJumpStatement
                        return;
                    }
                }
                else throw Context.SendGamemodeError();
            }
        }
        else
        {
            // checks for spamming /req where the vehicle will be once it spawns
            if (StructureUtility.IsStructureInRange(Context.Player.Position, 20f, Gamemode.Config.StructureVehicleBay, horizontalDistanceOnly: true) &&
                BarricadeUtility.CountBarricadesInRange(Context.Player.Position, 8f, KitSign, max: 5) < 5)
            {
                Context.IsolatedCommandCooldownTime = 5f;
            }

            await UniTask.SwitchToMainThread(token);
            throw Context.Reply(T.RequestNoTarget);
        }
    }
    /// <remarks>Thread Safe</remarks>
    internal UniTask RequestVehicle(InteractableVehicle vehicle, VehicleData data, CancellationToken token = default) => RequestVehicle(vehicle, data, Context.Player.GetTeam(), token);
    /// <remarks>Thread Safe</remarks>
    internal async UniTask RequestVehicle(InteractableVehicle vehicle, VehicleData data, ulong team, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
        {
            IPlayer names = await F.GetPlayerOriginalNamesAsync(vehicle.lockedOwner.m_SteamID, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            throw Context.Reply(T.RequestVehicleAlreadyRequested, names);
        }

        if (data.Team != 0 && data.Team != team)
            throw Context.Reply(T.RequestVehicleOtherTeam, TeamManager.GetFactionSafe(data.Team)!);
        if (data.RequiresSL && Context.Player.Squad == null)
            throw Context.Reply(T.RequestVehicleNotSquadLeader);

        Kit? kit = await Context.Player.GetActiveKit(token).ConfigureAwait(false);
        if (kit == null)
            throw Context.Reply(T.RequestVehicleNoKit);

        await UniTask.SwitchToMainThread(token);

        if (Context.Player.Level.Level < data.UnlockLevel)
            throw Context.Reply(T.RequestVehicleMissingLevels, new LevelData(Points.GetLevelXP(data.UnlockLevel)));
        if (data.RequiredClass != Class.None && kit.Class != data.RequiredClass)
            throw Context.Reply(T.RequestVehicleWrongClass, data.RequiredClass);
        if (Context.Player.CachedCredits < data.CreditCost)
            throw Context.Reply(T.RequestVehicleCantAfford, Context.Player.CachedCredits, data.CreditCost);
        if (CooldownManager.HasCooldown(Context.Player, CooldownType.RequestVehicle, out Cooldown cooldown, vehicle.id))
            throw Context.Reply(T.RequestVehicleCooldown, cooldown);
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
                        if (veh.lockedOwner.m_SteamID == Context.CallerId.m_SteamID && (veh.transform.position - vehicle.transform.position).sqrMagnitude <
                            UCWarfare.Config.MaxVehicleAbandonmentDistance * UCWarfare.Config.MaxVehicleAbandonmentDistance)
                            throw Context.Reply(T.RequestVehicleAlreadyOwned, veh);
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
            Context.IsolatedCommandCooldownTime = 5f;

            Localization.SendDelayRequestText(in delay, Context.Player, team, Localization.DelayTarget.VehicleBay);
            Context.Defer();
            return;
        }

        for (int i = 0; i < data.UnlockRequirements.Length; i++)
        {
            UnlockRequirement req = data.UnlockRequirements[i];
            if (req.CanAccess(Context.Player))
                continue;
            throw req.RequestVehicleFailureToMeet(Context, data);
        }

        if (!vehicle.asset.canBeLocked)
        {
            Context.Player.SendChat(T.RequestVehicleAlreadyRequested);
            return;
        }

        AssetBan? assetBan = await Data.ModerationSql.GetActiveAssetBan(Context.CallerId.m_SteamID, data.Type, token: token).ConfigureAwait(false);
        if (assetBan != null)
        {
            if (assetBan.VehicleTypeFilter.Length == 0)
            {
                if (assetBan.IsPermanent)
                    throw Context.Reply(T.RequestVehicleAssetBannedGlobalPermanent);

                throw Context.Reply(T.RequestVehicleAssetBannedGlobal, assetBan.GetTimeUntilExpiry(false));
            }

            if (assetBan.IsPermanent)
                throw Context.Reply(T.RequestVehicleAssetBannedPermanent, assetBan.GetCommaList(false));

            throw Context.Reply(T.RequestVehicleAssetBanned, assetBan.GetTimeUntilExpiry(false),
                assetBan.GetCommaList(false));
        }

        if (data.CreditCost > 0)
        {
            await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await Points.UpdatePointsAsync(Context.Player, false, token).ConfigureAwait(false);
                if (Context.Player.CachedCredits >= data.CreditCost)
                {
                    await Points.AwardCreditsAsync(Context.Player, -data.CreditCost, isPurchase: true, @lock: false,
                        token: token).ConfigureAwait(false);
                }
                else
                {
                    await UniTask.SwitchToMainThread(token);
                    Context.Player.SendChat(T.RequestVehicleCantAfford, Context.Player.CachedCredits,
                        data.CreditCost);
                    return;
                }
            }
            finally
            {
                Context.Player.PurchaseSync.Release();
            }
        }

        await UniTask.SwitchToMainThread(token);
        GiveVehicle(Context.Player, vehicle, data);

        // Stats.StatsManager.ModifyStats(Context.CallerId.m_SteamID, x => x.VehiclesRequested++, false);
        // Stats.StatsManager.ModifyTeam(team, t => t.VehiclesRequested++, false);
        // Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesRequested++);

        CooldownManager.StartCooldown(Context.Player, CooldownType.RequestVehicle, CooldownManager.Config.RequestVehicleCooldown, vehicle.id);
#endif
    }
#if false
    internal static void GiveVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, VehicleData data)
    {
        VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);
        VehicleComponent.TryAddOwnerToHistory(vehicle, ucplayer.Steam64);

        if (Data.Is(out IVehicles vgm) && vgm.VehicleSpawner.TryGetSpawn(vehicle, out SqlItem<VehicleSpawn> spawn))
        {
            if (spawn.Item?.Structure?.Item?.Buildable?.Model?.GetComponent<VehicleBayComponent>() is { } comp)
            {
                comp.OnRequest();
                ActionLog.Add(ActionLogType.RequestVehicle, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N} at spawn {comp.gameObject.transform.position.ToString("N2", CultureInfo.InvariantCulture)}", ucplayer);
            }
            else
                ActionLog.Add(ActionLogType.RequestVehicle, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);
            Data.Reporter?.OnVehicleRequest(ucplayer.Steam64, vehicle.asset.GUID, spawn.PrimaryKey);
        }
        else
            ActionLog.Add(ActionLogType.RequestVehicle, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N}", ucplayer);

        vehicle.updateVehicle();
        vehicle.updatePhysics();

        if (Gamemode.Config.EffectUnlockVehicle.TryGetAsset(out EffectAsset? effect))
            EffectUtility.TriggerEffect(effect, EffectManager.SMALL, vehicle.transform.position, true);

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
#endif
}