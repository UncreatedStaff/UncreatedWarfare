﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Commands;
public class StructureCommand : AsyncCommand
{
    private const string Syntax = "/structure <save|remove|examine|pop|set>";
    private const string Help = "Managed saved structures.";

    public StructureCommand() : base("structure", EAdminType.MEMBER)
    {
        AddAlias("struct");
        Structure = new CommandStructure
        {
            Description = "Manage barricades and structures.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Save")
                {
                    Description = "Saves a barricade or structure so it will stay around after games.",
                    Permission = EAdminType.HELPER
                },
                new CommandParameter("Remove")
                {
                    Aliases = new string[] { "delete" },
                    Description = "Removes a barricade or structure from being saved between games.",
                    Permission = EAdminType.HELPER
                },
                new CommandParameter("Pop")
                {
                    Aliases = new string[] { "destroy" },
                    Description = "Destroy a barricade or structure.",
                    Permission = EAdminType.HELPER
                },
                new CommandParameter("Set")
                {
                    Aliases = new string[] { "s" },
                    Description = "Set the owner or group of any barricade or structure.",
                    Permission = EAdminType.MODERATOR,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Owner")
                        {
                            Description = "Set the owner of any barricade or structure.",
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Owner", typeof(ulong), "Me")
                            }
                        },
                        new CommandParameter("Group")
                        {
                            Description = "Set the owner of any barricade or structure.",
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Owner", typeof(ulong), "Me")
                            }
                        }
                    }
                },
                new CommandParameter("Examine")
                {
                    Aliases = new string[] { "exam", "wtf" },
                    Description = "Get the owner and team of any barricade or structure."
                }
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        ctx.AssertArgs(1, Syntax);

        ctx.Defer();
        
        await UCWarfare.ToLevelLoad();
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (saver == null)
            throw ctx.SendNotEnabled();

        if (ctx.MatchParameter(0, "save"))
        {
            ctx.AssertGamemode<IStructureSaving>();

            ctx.AssertPermissions(EAdminType.HELPER);

            if (ctx.TryGetTarget(out StructureDrop structure))
            {
                (SqlItem<SavedStructure> item, bool isNew) = await saver.AddStructure(structure, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);

                if (isNew && item.Item != null)
                {
                    ctx.Reply(T.StructureSaved, item.Item);
                    ctx.LogAction(ActionLogType.SaveStructure,
                        $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {item.Item.Position:0:##} ({item.Item.InstanceID})");
                }
                else if (item.Item != null)
                    throw ctx.Reply(T.StructureAlreadySaved, item.Item);
                else
                    throw ctx.SendUnknownError();
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                (SqlItem<SavedStructure> item, bool isNew) = await saver.AddBarricade(barricade, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);

                if (isNew && item.Item != null)
                {
                    ctx.Reply(T.StructureSaved, item.Item);
                    ctx.LogAction(ActionLogType.SaveStructure,
                        $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {item.Item.Position:0:##} ({item.Item.InstanceID})");
                }
                else if (item.Item != null)
                    throw ctx.Reply(T.StructureAlreadySaved, item.Item);
                else
                    throw ctx.SendUnknownError();
            }
            else throw ctx.Reply(T.StructureNoTarget);
        }
        else if (ctx.MatchParameter(0, "remove", "delete"))
        {
            ctx.AssertGamemode<IStructureSaving>();

            ctx.AssertPermissions(EAdminType.HELPER);

            if (ctx.TryGetTarget(out StructureDrop structure))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(structure, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);

                    ctx.LogAction(ActionLogType.UnsaveStructure,
                        $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID})");
                    ctx.Reply(T.StructureUnsaved, oldItem);
                }
                else
                {
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.StructureAlreadyUnsaved, structure.asset);
                }
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(barricade, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);

                    ctx.LogAction(ActionLogType.UnsaveStructure,
                        $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID})");
                    ctx.Reply(T.StructureUnsaved, oldItem);
                }
                else
                {
                    await UCWarfare.ToUpdate(token);
                    throw ctx.Reply(T.StructureAlreadyUnsaved, barricade.asset);
                }
            }
            else throw ctx.Reply(T.StructureNoTarget);
        }
        else if (ctx.MatchParameter(0, "pop", "destroy"))
        {
            ctx.AssertPermissions(EAdminType.HELPER);

            if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                VehicleSpawner.DeleteVehicle(vehicle);

                ctx.LogAction(ActionLogType.PopStructure,
                    $"VEHICLE: {vehicle.asset.vehicleName} / {vehicle.asset.id} /" +
                    $" {vehicle.asset.GUID:N} at {vehicle.transform.position:N2} ({vehicle.instanceID})");
                ctx.Reply(T.StructureDestroyed, vehicle.asset);
            }
            else if (ctx.TryGetTarget(out StructureDrop structure))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(structure, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    ctx.LogAction(ActionLogType.UnsaveStructure,
                        $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID}) (Automatically unsaved before destroy)");
                    ctx.Reply(T.StructureUnsaved, oldItem);
                }
                else await UCWarfare.ToUpdate(token);

                DestroyStructure(structure, ctx.Caller);
                ctx.LogAction(ActionLogType.PopStructure,
                    $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                    $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2", Data.AdminLocale)} ({structure.instanceID})");
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(barricade, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);

                    ctx.LogAction(ActionLogType.UnsaveStructure,
                        $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID}) (Autmoatically unsaved before destroy)");
                    ctx.Reply(T.StructureUnsaved, oldItem);
                }
                else await UCWarfare.ToUpdate(token);

                DestroyBarricade(barricade, ctx.Caller);
                ctx.LogAction(ActionLogType.PopStructure,
                    $"BARRICADE: {barricade.asset.itemName} / {barricade.asset.id} /" +
                    $" {barricade.asset.GUID:N} at {barricade.model.transform.position.ToString("N2", Data.AdminLocale)} ({barricade.instanceID})");
                ctx.Defer();
            }
        }
        else if (ctx.MatchParameter(0, "examine", "exam", "wtf"))
        {
            if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                await ExamineVehicle(vehicle, ctx.Caller, true, token).ConfigureAwait(false);
            }
            else if (ctx.TryGetTarget(out StructureDrop structure))
            {
                await ExamineStructure(structure, ctx.Caller, true, token).ConfigureAwait(false);
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                await ExamineBarricade(barricade, ctx.Caller, true, token).ConfigureAwait(false);
            }
            else throw ctx.Reply(T.StructureExamineNotExaminable);

            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "set", "s"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/structure <set|s> <group|owner> <value> - Sets properties for strcuture saves.");

            if (ctx.HasArg(2))
            {
                bool grp = ctx.MatchParameter(1, "group");
                if (!grp && !ctx.MatchParameter(1, "owner"))
                    throw ctx.SendCorrectUsage("/structure <set|s> <group|owner> <value>");
                SqlItem<SavedStructure>? data;
                BarricadeDrop? barricade = null;
                ItemAsset asset;
                if (ctx.TryGetTarget(out StructureDrop structure))
                {
                    data = await saver.GetSaveItem(structure, token).ConfigureAwait(false);
                    asset = structure.asset;
                }
                else if (ctx.TryGetTarget(out barricade))
                {
                    data = await saver.GetSaveItem(barricade, token).ConfigureAwait(false);
                    asset = barricade.asset;
                }
                else throw ctx.Reply(T.StructureNoTarget);

                await UCWarfare.ToUpdate(token);
                bool saved = data?.Item?.Buildable?.Drop is not null;
                if (!ctx.TryGet(2, out ulong s64) || s64 != 0 && (!grp && !Util.IsValidSteam64Id(s64)))
                {
                    if (ctx.MatchParameter(2, "me"))
                        s64 = grp ? ctx.Caller.Player.quests.groupID.m_SteamID : ctx.CallerID;
                    else throw ctx.SendCorrectUsage("/structure <set|s> <group|owner> <value> - Value must be 'me', '0' or a valid Steam64 ID");
                }
                string str64 = s64.ToString(Data.AdminLocale);

                if (saved)
                {
                    await data!.Enter(token).ConfigureAwait(false);
                    try
                    {
                        await UCWarfare.ToUpdate(token);
                        if (grp)
                            data.Item!.Group = s64;
                        else // owner
                            data.Item!.Owner = s64;

                        data.Item!.Buildable!.SetOwnerOrGroup(data.Item.Owner, data.Item.Group);
                        await data.SaveItem(token).ConfigureAwait(false);
                    }
                    finally
                    {
                        data.Release();
                    }

                    await UCWarfare.ToUpdate(token);
                    ctx.LogAction(ActionLogType.SetSavedStructureProperty, $"{asset?.itemName ?? "null"} / {(asset == null ? 0 : asset.id)} / {data.Item.ItemGuid:N} - SET " + (grp ? "GROUP" : "OWNER") + " >> " + str64);
                }
                else
                {
                    ulong? group = grp ? s64 : null;
                    ulong? owner = grp ? null : s64;
                    if (structure != null)
                        F.SetOwnerOrGroup(structure, owner, group);
                    else if (barricade != null)
                        F.SetOwnerOrGroup(barricade, owner, group);
                    else throw ctx.Reply(T.StructureNoTarget);
                }
                if (grp)
                {
                    FactionInfo? info = TeamManager.GetFactionSafe(s64);
                    if (info != null)
                        str64 = info.GetName(ctx.Caller.Locale.LanguageInfo).Colorize(info.HexColor, ctx.IMGUI);
                }
                ctx.Reply(T.StructureSaveSetProperty!, grp ? "Group" : "Owner", asset, str64);
            }
            else
                ctx.SendCorrectUsage("/structure <set|s> <group|owner> <value>");
        }
        else
            ctx.SendCorrectUsage(Syntax);
    }

    private static readonly List<ISalvageInfo> WorkingSalvageInfo = new List<ISalvageInfo>(2);
    private static void DestroyBarricade(BarricadeDrop bdrop, UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        if (bdrop != null && BarricadeManager.tryGetRegion(bdrop.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            bdrop.model.GetComponents(WorkingSalvageInfo);
            try
            {
                SalvageBarricadeRequested? args = null;
                for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
                {
                    if (args == null)
                    {
                        StructureSaver? saver = StructureSaver.GetSingletonQuick();
                        args = new SalvageBarricadeRequested(player, bdrop, bdrop.GetServersideData(), region, x, y, plant, saver?.GetSaveItemSync(bdrop.instanceID, StructType.Barricade), default, default);
                    }

                    ISalvageInfo salvage = WorkingSalvageInfo[i];
                    salvage.IsSalvaged = true;
                    salvage.Salvager = player.Steam64;
                    if (salvage is ISalvageListener listener)
                    {
                        listener.OnSalvageRequested(args);
                        if (!args.CanContinue)
                            break;
                    }
                }

                if (args is { CanContinue: false })
                {
                    for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
                    {
                        ISalvageInfo salvage = WorkingSalvageInfo[i];
                        salvage.IsSalvaged = false;
                        salvage.Salvager = 0;
                    }

                    player.SendChat(T.WhitelistProhibitedSalvage, bdrop.asset);
                    return;
                }

                BarricadeManager.destroyBarricade(bdrop, x, y, ushort.MaxValue);
                player.SendChat(T.StructureDestroyed, bdrop.asset);
            }
            finally
            {
                WorkingSalvageInfo.Clear();
            }
        }
        else
        {
            player.SendChat(T.StructureNotDestroyable);
        }
    }
    private static void DestroyStructure(StructureDrop sdrop, UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        if (sdrop != null && StructureManager.tryGetRegion(sdrop.model, out byte x, out byte y, out StructureRegion region))
        {
            sdrop.model.GetComponents(WorkingSalvageInfo);
            try
            {
                SalvageStructureRequested? args = null;
                for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
                {
                    if (args == null)
                    {
                        StructureSaver? saver = StructureSaver.GetSingletonQuick();
                        args = new SalvageStructureRequested(player, sdrop, sdrop.GetServersideData(), region, x, y, saver?.GetSaveItemSync(sdrop.instanceID, StructType.Structure), default, default);
                    }
                    
                    ISalvageInfo salvage = WorkingSalvageInfo[i];
                    salvage.IsSalvaged = true;
                    salvage.Salvager = player.Steam64;
                    if (salvage is ISalvageListener listener)
                    {
                        listener.OnSalvageRequested(args);
                        if (!args.CanContinue)
                            break;
                    }
                }
                if (args is { CanContinue: false })
                {
                    for (int i = 0; i < WorkingSalvageInfo.Count; ++i)
                    {
                        ISalvageInfo salvage = WorkingSalvageInfo[i];
                        salvage.IsSalvaged = false;
                        salvage.Salvager = 0;
                    }
                    player.SendChat(T.WhitelistProhibitedSalvage, sdrop.asset);
                    return;
                }
                StructureManager.destroyStructure(sdrop, x, y, Vector3.down);
                player.SendChat(T.StructureDestroyed, sdrop.asset);
            }
            finally
            {
                WorkingSalvageInfo.Clear();
            }
        }
        else
        {
            player.SendChat(T.StructureNotDestroyable);
        }
    }
    private async Task ExamineVehicle(InteractableVehicle vehicle, UCPlayer player, bool sendurl, CancellationToken token = default)
    {
        ThreadUtil.assertIsGameThread();
        if (vehicle.lockedOwner == default || vehicle.lockedOwner == Steamworks.CSteamID.Nil)
        {
            player.SendChat(T.StructureExamineNotLocked);
        }
        else
        {
            ulong team = vehicle.lockedGroup.m_SteamID.GetTeam();
            ulong prevOwner = vehicle.transform.TryGetComponent(out VehicleComponent vcomp) ? vcomp.PreviousOwner : 0ul;
            IPlayer names = await F.GetPlayerOriginalNamesAsync(vehicle.lockedOwner.m_SteamID, token).ConfigureAwait(false);
            string prevOwnerName;
            if (prevOwner != 0ul)
            {
                PlayerNames pl = await F.GetPlayerOriginalNamesAsync(prevOwner, token).ConfigureAwait(false);
                prevOwnerName = pl.PlayerName;
            }
            else prevOwnerName = "None";
            await UCWarfare.ToUpdate(token);
            if (sendurl)
            {
                player.SteamPlayer.SendSteamURL(
                    T.VehicleExamineLastOwnerPrompt.Translate(player, false, vehicle.asset, names,
                    Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(team)! : null!, prevOwnerName, prevOwner), vehicle.lockedOwner.m_SteamID);
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(vehicle.lockedOwner.m_SteamID);
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                player.SendChat(T.VehicleExamineLastOwnerChat,
                    vehicle.asset, names, pl, Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(team)! : null!, prevOwnerName, prevOwner);
            }
        }
    }
    private async Task ExamineBarricade(BarricadeDrop bdrop, UCPlayer player, bool sendurl, CancellationToken token = default)
    {
        ThreadUtil.assertIsGameThread();
        if (bdrop != null)
        {
            BarricadeData data = bdrop.GetServersideData();
            if (data.owner == 0)
            {
                player.SendChat(T.StructureExamineNotExaminable);
                return;
            }

            IPlayer names = await F.GetPlayerOriginalNamesAsync(data.owner, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            if (sendurl)
            {
                player.SteamPlayer.SendSteamURL(T.StructureExamineLastOwnerPrompt.Translate(player, false, data.barricade.asset,
                        names, Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!), data.owner);
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(data.owner);
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                player.SendChat(T.StructureExamineLastOwnerChat, data.barricade.asset, names,
                    pl, Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!);
            }
        }
        else
        {
            player.SendChat(T.StructureExamineNotExaminable);
        }
    }
    private async Task ExamineStructure(StructureDrop sdrop, UCPlayer player, bool sendurl, CancellationToken token = default)
    {
        ThreadUtil.assertIsGameThread();
        if (sdrop != null)
        {
            StructureData data = sdrop.GetServersideData();
            if (data.owner == default)
            {
                player.SendChat(T.StructureExamineNotExaminable);
                return;
            }
            IPlayer names = await F.GetPlayerOriginalNamesAsync(data.owner, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            if (sendurl)
            {
                player.SteamPlayer.SendSteamURL(T.StructureExamineLastOwnerPrompt.Translate(player, false, data.structure.asset, names,
                        Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!), data.owner);
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(data.owner);
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                player.SendChat(T.StructureExamineLastOwnerChat, data.structure.asset, names,
                    pl, Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!);
            }
        }
        else
        {
            player.SendChat(T.StructureExamineNotExaminable);
        }
    }
}
