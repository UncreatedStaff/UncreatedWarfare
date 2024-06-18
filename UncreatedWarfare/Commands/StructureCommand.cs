using Cysharp.Threading.Tasks;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Commands;

[Command("structure", "struct")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class StructureCommand : IExecutableCommand
{
    private const string Syntax = "/structure <save|remove|examine|pop|set>";
    private const string Help = "Managed saved structures.";

    private static readonly PermissionLeaf PermissionSave    = new PermissionLeaf("commands.structure.save",    unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionRemove  = new PermissionLeaf("commands.structure.remove",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionDestroy = new PermissionLeaf("commands.structure.destroy", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionSet     = new PermissionLeaf("commands.structure.set",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionExamine = new PermissionLeaf("commands.structure.examine", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Manage barricades and structures.",
            Parameters =
            [
                new CommandParameter("Save")
                {
                    Description = "Saves a barricade or structure so it will stay around after games.",
                    Permission = PermissionSave
                },
                new CommandParameter("Remove")
                {
                    Aliases = [ "delete" ],
                    Description = "Removes a barricade or structure from being saved between games.",
                    Permission = PermissionRemove
                },
                new CommandParameter("Destroy")
                {
                    Aliases = [ "pop" ],
                    Description = "Destroy a barricade or structure.",
                    Permission = PermissionDestroy
                },
                new CommandParameter("Set")
                {
                    Aliases = [ "s" ],
                    Description = "Set the owner or group of any barricade or structure.",
                    Permission = PermissionSet,
                    Parameters =
                    [
                        new CommandParameter("Owner")
                        {
                            Description = "Set the owner of any barricade or structure.",
                            Parameters =
                            [
                                new CommandParameter("Owner", typeof(ulong), "Me")
                            ]
                        },
                        new CommandParameter("Group")
                        {
                            Description = "Set the owner of any barricade or structure.",
                            Parameters =
                            [
                                new CommandParameter("Owner", typeof(ulong), "Me")
                            ]
                        }
                    ]
                },
                new CommandParameter("Examine")
                {
                    Aliases = [ "exam", "wtf" ],
                    Description = "Get the owner and team of any barricade or structure.",
                    Permission = PermissionExamine
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertArgs(1, Syntax);

        Context.Defer();
        
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (saver == null)
            throw Context.SendNotEnabled();

        if (Context.MatchParameter(0, "save"))
        {
            Context.AssertGamemode<IStructureSaving>();

            await Context.AssertPermissions(PermissionSave, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                (SqlItem<SavedStructure> item, bool isNew) = await saver.AddStructure(structure, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (isNew && item.Item != null)
                {
                    Context.Reply(T.StructureSaved, item.Item);
                    Context.LogAction(ActionLogType.SaveStructure,
                        $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {item.Item.Position:0:##} ({item.Item.InstanceID})");
                }
                else if (item.Item != null)
                    throw Context.Reply(T.StructureAlreadySaved, item.Item);
                else
                    throw Context.SendUnknownError();
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                (SqlItem<SavedStructure> item, bool isNew) = await saver.AddBarricade(barricade, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (isNew && item.Item != null)
                {
                    Context.Reply(T.StructureSaved, item.Item);
                    Context.LogAction(ActionLogType.SaveStructure,
                        $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {item.Item.Position:0:##} ({item.Item.InstanceID})");
                }
                else if (item.Item != null)
                    throw Context.Reply(T.StructureAlreadySaved, item.Item);
                else
                    throw Context.SendUnknownError();
            }
            else throw Context.Reply(T.StructureNoTarget);
        }
        else if (Context.MatchParameter(0, "remove", "delete"))
        {
            Context.AssertGamemode<IStructureSaving>();

            await Context.AssertPermissions(PermissionRemove, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(structure, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);

                    Context.LogAction(ActionLogType.UnsaveStructure,
                        $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID})");
                    Context.Reply(T.StructureUnsaved, oldItem);
                }
                else
                {
                    await UniTask.SwitchToMainThread(token);
                    throw Context.Reply(T.StructureAlreadyUnsaved, structure.asset);
                }
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(barricade, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);

                    Context.LogAction(ActionLogType.UnsaveStructure,
                        $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID})");
                    Context.Reply(T.StructureUnsaved, oldItem);
                }
                else
                {
                    await UniTask.SwitchToMainThread(token);
                    throw Context.Reply(T.StructureAlreadyUnsaved, barricade.asset);
                }
            }
            else throw Context.Reply(T.StructureNoTarget);
        }
        else if (Context.MatchParameter(0, "destroy", "pop"))
        {
            await Context.AssertPermissions(PermissionDestroy, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            {
                VehicleSpawner.DeleteVehicle(vehicle);

                Context.LogAction(ActionLogType.PopStructure,
                    $"VEHICLE: {vehicle.asset.vehicleName} / {vehicle.asset.id} /" +
                    $" {vehicle.asset.GUID:N} at {vehicle.transform.position:N2} ({vehicle.instanceID})");
                Context.Reply(T.StructureDestroyed, vehicle.asset);
            }
            else if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(structure, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    Context.LogAction(ActionLogType.UnsaveStructure,
                        $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID}) (Automatically unsaved before destroy)");
                    Context.Reply(T.StructureUnsaved, oldItem);
                }
                else await UniTask.SwitchToMainThread(token);

                DestroyStructure(structure, Context.Player);
                Context.LogAction(ActionLogType.PopStructure,
                    $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                    $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2", Data.AdminLocale)} ({structure.instanceID})");
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                SqlItem<SavedStructure>? item = await saver.GetSaveItem(barricade, token).ConfigureAwait(false);
                if (item?.Item != null)
                {
                    SavedStructure oldItem = item.Item;
                    await item.Delete(token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);

                    Context.LogAction(ActionLogType.UnsaveStructure,
                        $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID}) (Autmoatically unsaved before destroy)");
                    Context.Reply(T.StructureUnsaved, oldItem);
                }
                else await UniTask.SwitchToMainThread(token);

                DestroyBarricade(barricade, Context.Player);
                Context.LogAction(ActionLogType.PopStructure,
                    $"BARRICADE: {barricade.asset.itemName} / {barricade.asset.id} /" +
                    $" {barricade.asset.GUID:N} at {barricade.model.transform.position.ToString("N2", Data.AdminLocale)} ({barricade.instanceID})");
                Context.Defer();
            }
        }
        else if (Context.MatchParameter(0, "examine", "exam", "wtf"))
        {
            await Context.AssertPermissions(PermissionExamine, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            {
                await ExamineVehicle(vehicle, Context.Player, true, token).ConfigureAwait(false);
            }
            else if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                await ExamineStructure(structure, Context.Player, true, token).ConfigureAwait(false);
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                await ExamineBarricade(barricade, Context.Player, true, token).ConfigureAwait(false);
            }
            else throw Context.Reply(T.StructureExamineNotExaminable);

            Context.Defer();
        }
        else if (Context.MatchParameter(0, "set", "s"))
        {
            await Context.AssertPermissions(PermissionSet, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/structure <set|s> <group|owner> <value> - Sets properties for strcuture saves.");

            if (!Context.HasArgs(3))
            {
                throw Context.SendCorrectUsage("/structure <set|s> <group|owner> <value>");
            }

            bool grp = Context.MatchParameter(1, "group");
            if (!grp && !Context.MatchParameter(1, "owner"))
                throw Context.SendCorrectUsage("/structure <set|s> <group|owner> <value>");
            SqlItem<SavedStructure>? data;
            BarricadeDrop? barricade = null;
            ItemAsset asset;
            if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                data = await saver.GetSaveItem(structure, token).ConfigureAwait(false);
                asset = structure.asset;
            }
            else if (Context.TryGetBarricadeTarget(out barricade))
            {
                data = await saver.GetSaveItem(barricade, token).ConfigureAwait(false);
                asset = barricade.asset;
            }
            else throw Context.Reply(T.StructureNoTarget);

            await UniTask.SwitchToMainThread(token);
            bool saved = data?.Item?.Buildable?.Drop is not null;
            if (!Context.TryGet(2, out ulong s64) || s64 != 0 && (!grp && new CSteamID(s64).GetEAccountType() != EAccountType.k_EAccountTypeIndividual))
            {
                if (Context.MatchParameter(2, "me"))
                    s64 = grp ? Context.Player.Player.quests.groupID.m_SteamID : Context.CallerId.m_SteamID;
                else
                    throw Context.SendCorrectUsage(
                        "/structure <set|s> <group|owner> <value> - Value must be 'me', '0' or a valid Steam64 ID");
            }

            string str64 = s64.ToString(Data.AdminLocale);

            if (saved)
            {
                await data!.Enter(token).ConfigureAwait(false);
                try
                {
                    await UniTask.SwitchToMainThread(token);
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

                await UniTask.SwitchToMainThread(token);
                Context.LogAction(ActionLogType.SetSavedStructureProperty,
                    $"{asset?.itemName ?? "null"} / {(asset == null ? 0 : asset.id)} / {data.Item.ItemGuid:N} - SET " +
                    (grp ? "GROUP" : "OWNER") + " >> " + str64);
            }
            else
            {
                ulong? group = grp ? s64 : null;
                ulong? owner = grp ? null : s64;
                if (structure != null)
                    F.SetOwnerOrGroup(structure, owner, group);
                else if (barricade != null)
                    F.SetOwnerOrGroup(barricade, owner, group);
                else throw Context.Reply(T.StructureNoTarget);
            }

            if (grp)
            {
                FactionInfo? info = TeamManager.GetFactionSafe(s64);
                if (info != null)
                    str64 = info.GetName(Context.Language).Colorize(info.HexColor, Context.IMGUI);
            }

            Context.Reply(T.StructureSaveSetProperty!, grp ? "Group" : "Owner", asset, str64);
        }
        else
            Context.SendCorrectUsage(Syntax);
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
            await UniTask.SwitchToMainThread(token);
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
                await UniTask.SwitchToMainThread(token);
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
            await UniTask.SwitchToMainThread(token);
            if (sendurl)
            {
                player.SteamPlayer.SendSteamURL(T.StructureExamineLastOwnerPrompt.Translate(player, false, data.barricade.asset,
                        names, Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!), data.owner);
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(data.owner);
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
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
            await UniTask.SwitchToMainThread(token);
            if (sendurl)
            {
                player.SteamPlayer.SendSteamURL(T.StructureExamineLastOwnerPrompt.Translate(player, false, data.structure.asset, names,
                        Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!), data.owner);
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(data.owner);
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
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
