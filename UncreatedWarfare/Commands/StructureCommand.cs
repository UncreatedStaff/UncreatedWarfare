using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class StructureCommand : Command
{
    private const string SYNTAX = "/structure <save|remove|examine|pop|set>";
    private const string HELP = "Managed saved structures.";

    public StructureCommand() : base("structure", EAdminType.MEMBER)
    {
        AddAlias("struct");
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertArgs(1, SYNTAX);

        ctx.Defer();
        
        Task.Run(async () =>
        {
            try
            {
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
                        (SqlItem<SavedStructure> item, bool isNew) = await saver.AddStructure(structure).ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                        if (isNew && item.Item != null)
                        {
                            ctx.Reply(T.StructureSaved, item.Item);
                            ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
                                $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {item.Item.Position:0:##} ({item.Item.InstanceID})");
                        }
                        else if (item.Item != null)
                            throw ctx.Reply(T.StructureAlreadySaved, item.Item);
                        else
                            throw ctx.SendUnknownError();
                    }
                    else if (ctx.TryGetTarget(out BarricadeDrop barricade))
                    {
                        (SqlItem<SavedStructure> item, bool isNew) = await saver.AddBarricade(barricade).ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                        if (isNew && item.Item != null)
                        {
                            ctx.Reply(T.StructureSaved, item.Item);
                            ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
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
                        SqlItem<SavedStructure>? item = await saver.GetSaveItem(structure).ConfigureAwait(false);
                        if (item?.Item != null)
                        {
                            await item.Enter().ConfigureAwait(false);
                            try
                            {
                                SavedStructure oldItem = item.Item;
                                await item.Delete().ConfigureAwait(false);
                                await UCWarfare.ToUpdate();
                                ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                                    $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID})");
                                ctx.Reply(T.StructureUnsaved, oldItem);
                            }
                            finally
                            {
                                item.Release();
                            }
                        }
                        else
                        {
                            await UCWarfare.ToUpdate();
                            throw ctx.Reply(T.StructureAlreadyUnsaved, structure.asset);
                        }
                    }
                    else if (ctx.TryGetTarget(out BarricadeDrop barricade))
                    {
                        SqlItem<SavedStructure>? item = await saver.GetSaveItem(barricade).ConfigureAwait(false);
                        if (item?.Item != null)
                        {
                            await item.Enter().ConfigureAwait(false);
                            try
                            {
                                SavedStructure oldItem = item.Item;
                                await item.Delete().ConfigureAwait(false);
                                await UCWarfare.ToUpdate();
                                ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                                    $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID})");
                                ctx.Reply(T.StructureUnsaved, oldItem);
                            }
                            finally
                            {
                                item.Release();
                            }
                        }
                        else
                        {
                            await UCWarfare.ToUpdate();
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
                        VehicleBay.DeleteVehicle(vehicle);

                        ctx.LogAction(EActionLogType.POP_STRUCTURE,
                            $"VEHICLE: {vehicle.asset.vehicleName} / {vehicle.asset.id} /" +
                            $" {vehicle.asset.GUID:N} at {vehicle.transform.position:N2} ({vehicle.instanceID})");
                        ctx.Reply(T.StructureDestroyed, vehicle.asset);
                    }
                    else if (ctx.TryGetTarget(out StructureDrop structure))
                    {
                        SqlItem<SavedStructure>? item = await saver.GetSaveItem(structure).ConfigureAwait(false);
                        if (item?.Item != null)
                        {
                            await item.Enter().ConfigureAwait(false);
                            try
                            {
                                SavedStructure oldItem = item.Item;
                                await item.Delete().ConfigureAwait(false);
                                await UCWarfare.ToUpdate();
                                ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                                    $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID}) (Autmoatically unsaved before destroy)");
                                ctx.Reply(T.StructureUnsaved, oldItem);
                            }
                            finally
                            {
                                item.Release();
                            }
                        }
                        else await UCWarfare.ToUpdate();

                        DestroyStructure(structure, ctx.Caller);
                        ctx.LogAction(EActionLogType.POP_STRUCTURE,
                            $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                            $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2")} ({structure.instanceID})");
                    }
                    else if (ctx.TryGetTarget(out BarricadeDrop barricade))
                    {
                        SqlItem<SavedStructure>? item = await saver.GetSaveItem(barricade).ConfigureAwait(false);
                        if (item?.Item != null)
                        {
                            await item.Enter().ConfigureAwait(false);
                            try
                            {
                                SavedStructure oldItem = item.Item;
                                await item.Delete().ConfigureAwait(false);
                                await UCWarfare.ToUpdate();
                                ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                                    $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {oldItem.Position} ({oldItem.InstanceID}) (Autmoatically unsaved before destroy)");
                                ctx.Reply(T.StructureUnsaved, oldItem);
                            }
                            finally
                            {
                                item.Release();
                            }
                        }
                        else await UCWarfare.ToUpdate();

                        DestroyBarricade(barricade, ctx.Caller);
                        ctx.LogAction(EActionLogType.POP_STRUCTURE,
                            $"BARRICADE: {barricade.asset.itemName} / {barricade.asset.id} /" +
                            $" {barricade.asset.GUID:N} at {barricade.model.transform.position.ToString("N2")} ({barricade.instanceID})");
                        ctx.Defer();
                    }
                }
                else if (ctx.MatchParameter(0, "examine", "exam", "wtf"))
                {
                    if (ctx.TryGetTarget(out InteractableVehicle vehicle))
                    {
                        ExamineVehicle(vehicle, ctx.Caller, true);
                        ctx.Defer();
                    }
                    else if (ctx.TryGetTarget(out StructureDrop structure))
                    {
                        ExamineStructure(structure, ctx.Caller, true);
                        ctx.Defer();
                    }
                    else if (ctx.TryGetTarget(out BarricadeDrop barricade))
                    {
                        ExamineBarricade(barricade, ctx.Caller, true);
                        ctx.Defer();
                    }
                    else throw ctx.Reply(T.StructureExamineNotExaminable);
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
                            data = await saver.GetSaveItem(structure);
                            asset = structure.asset;
                        }
                        else if (ctx.TryGetTarget(out barricade))
                        {
                            data = await saver.GetSaveItem(barricade!);
                            asset = barricade.asset;
                        }
                        else throw ctx.Reply(T.StructureNoTarget);

                        await UCWarfare.ToUpdate();
                        bool saved = data?.Item?.Buildable?.Drop is not null;
                        if (!ctx.TryGet(2, out ulong s64) || s64 != 0 && (!grp && !OffenseManager.IsValidSteam64ID(s64)))
                        {
                            if (ctx.MatchParameter(2, "me"))
                                s64 = grp ? ctx.Caller.Player.quests.groupID.m_SteamID : ctx.CallerID;
                            else throw ctx.SendCorrectUsage("/structure <set|s> <group|owner> <value> - Value must be 'me', '0' or a valid Steam64 ID");
                        }
                        string s64s = s64.ToString(Data.AdminLocale);

                        if (saved)
                        {
                            await data!.Enter().ConfigureAwait(false);
                            try
                            {
                                await UCWarfare.ToUpdate();
                                if (grp)
                                    data.Item!.Group = s64;
                                else // owner
                                    data.Item!.Owner = s64;

                                data.Item!.Buildable!.SetOwnerOrGroup(data.Item.Owner, data.Item.Group);
                                await data.SaveItem().ConfigureAwait(false);
                            }
                            finally
                            {
                                data.Release();
                            }

                            await UCWarfare.ToUpdate();
                            ctx.LogAction(EActionLogType.SET_SAVED_STRUCTURE_PROPERTY, $"{asset?.itemName ?? "null"} / {(asset == null ? 0 : asset.id)} / {data.Item.ItemGuid:N} - SET " + (grp ? "GROUP" : "OWNER") + " >> " + s64s);
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
                                s64s = info.GetName(ctx.Caller.Language).ColorizeTMPro(info.HexColor);
                        }
                        ctx.Reply(T.StructureSaveSetProperty!, grp ? "Group" : "Owner", asset, s64s);
                    }
                    else
                        ctx.SendCorrectUsage("/structure <set|s> <group|owner> <value>");
                }
                else
                    ctx.SendCorrectUsage(SYNTAX);
            }
            catch (BaseCommandInteraction)
            { }
            catch (Exception ex)
            {
                L.LogError("Error in structure command.");
                L.LogError(ex);
            }
        });
    }

    private void DestroyBarricade(BarricadeDrop bdrop, Player player)
    {
        if (bdrop != null && Regions.tryGetCoordinate(bdrop.model.position, out byte x, out byte y))
        {
            if (bdrop.model.TryGetComponent(out Components.FOBComponent f))
            {
                f.Parent.IsWipedByAuthority = true;
                //f.parent.Destroy();
            }

            BarricadeData data = bdrop.GetServersideData();
            BarricadeManager.destroyBarricade(bdrop, x, y, ushort.MaxValue);
            player.SendChat(T.StructureDestroyed, bdrop.asset);
        }
        else
        {
            player.SendChat(T.StructureNotDestroyable);
        }
    }
    private void DestroyStructure(StructureDrop sdrop, Player player)
    {
        if (sdrop != null && Regions.tryGetCoordinate(sdrop.model.position, out byte x, out byte y))
        {
            StructureData data = sdrop.GetServersideData();
            StructureManager.destroyStructure(sdrop, x, y, Vector3.down);
            player.SendChat(T.StructureDestroyed, sdrop.asset);
        }
        else
        {
            player.SendChat(T.StructureNotDestroyable);
        }
    }
    private void ExamineVehicle(InteractableVehicle vehicle, Player player, bool sendurl)
    {
        if (vehicle.lockedOwner == default || vehicle.lockedOwner == Steamworks.CSteamID.Nil)
        {
            player.SendChat(T.StructureExamineNotLocked);
        }
        else
        {
            ulong team = vehicle.lockedGroup.m_SteamID.GetTeam();
            if (sendurl)
            {
                player.channel.owner.SendSteamURL(
                    T.StructureExamineLastOwnerPrompt.Translate(player.channel.owner.playerID.steamID.m_SteamID, vehicle.asset,
                    F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID), Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(team)! : null!), vehicle.lockedOwner.m_SteamID);
            }
            else
            {
                player.SendChat(T.StructureExamineLastOwnerChat,
                    vehicle.asset,
                    F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID),
                    new OfflinePlayer(vehicle.lockedOwner.m_SteamID), Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(team)! : null!);
            }
        }
    }
    private void ExamineBarricade(BarricadeDrop bdrop, Player player, bool sendurl)
    {
        if (bdrop != null)
        {
            BarricadeData data = bdrop.GetServersideData();
            if (data.owner == 0)
            {
                player.SendChat(T.StructureExamineNotExaminable);
                return;
            }

            if (sendurl)
            {
                player.channel.owner.SendSteamURL(
                    T.StructureExamineLastOwnerPrompt.Translate(player.channel.owner.playerID.steamID.m_SteamID, data.barricade.asset,
                        F.GetPlayerOriginalNames(data.owner), Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!), data.owner);
            }
            else
            {
                player.SendChat(T.StructureExamineLastOwnerChat,
                    data.barricade.asset,
                    F.GetPlayerOriginalNames(data.owner),
                    new OfflinePlayer(data.owner), Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!);
            }
        }
        else
        {
            player.SendChat(T.StructureExamineNotExaminable);
        }
    }
    private void ExamineStructure(StructureDrop sdrop, Player player, bool sendurl)
    {
        if (sdrop != null)
        {
            SDG.Unturned.StructureData data = sdrop.GetServersideData();
            if (data.owner == default)
            {
                player.SendChat(T.StructureExamineNotExaminable);
                return;
            }
            if (sendurl)
            {
                player.channel.owner.SendSteamURL(
                    T.StructureExamineLastOwnerPrompt.Translate(player.channel.owner.playerID.steamID.m_SteamID, data.structure.asset,
                        F.GetPlayerOriginalNames(data.owner), Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!), data.owner);
            }
            else
            {
                player.SendChat(T.StructureExamineLastOwnerChat,
                    data.structure.asset,
                    F.GetPlayerOriginalNames(data.owner),
                    new OfflinePlayer(data.owner), Data.Gamemode is ITeams ? TeamManager.GetFactionSafe(data.owner.GetTeamFromPlayerSteam64ID())! : null!);
            }
        }
        else
        {
            player.SendChat(T.StructureExamineNotExaminable);
        }
    }
}
