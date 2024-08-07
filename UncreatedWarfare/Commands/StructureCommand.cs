using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

[Command("structure", "struct")]
[MetadataFile(nameof(GetHelpMetadata))]
public class StructureCommand : IExecutableCommand
{
    private readonly BuildableSaver _saver;
    private const string Syntax = "/structure <save|remove|examine|pop|set>";
    private const string Help = "Managed saved structures.";

    private static readonly PermissionLeaf PermissionSave    = new PermissionLeaf("commands.structure.save",    unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionRemove  = new PermissionLeaf("commands.structure.remove",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionDestroy = new PermissionLeaf("commands.structure.destroy", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionSet     = new PermissionLeaf("commands.structure.set",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionExamine = new PermissionLeaf("commands.structure.examine", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public StructureCommand(BuildableSaver saver)
    {
        _saver = saver;
    }

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
        
        if (Context.MatchParameter(0, "save"))
        {
            Context.AssertGamemode<IStructureSaving>();

            await Context.AssertPermissions(PermissionSave, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                if (!await _saver.SaveStructureAsync(structure, token))
                {
                    throw Context.Reply(T.StructureAlreadySaved, structure.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.Reply(T.StructureSaved, structure.asset);
                Context.LogAction(ActionLogType.SaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                               $"at {structure.GetServersideData().point:0:##} ({structure.instanceID})");
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                if (!await _saver.SaveBarricadeAsync(barricade, token))
                {
                    throw Context.Reply(T.StructureAlreadySaved, barricade.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.Reply(T.StructureSaved, barricade.asset);
                Context.LogAction(ActionLogType.SaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                               $"at {barricade.GetServersideData().point:0:##} ({barricade.instanceID})");
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
                if (!await _saver.DiscardStructureAsync(structure.instanceID, token))
                {
                    throw Context.Reply(T.StructureAlreadyUnsaved, structure.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                                 $"at {structure.GetServersideData().point} ({structure.instanceID})");
                Context.Reply(T.StructureUnsaved, structure.asset);
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                if (!await _saver.DiscardStructureAsync(barricade.instanceID, token))
                {
                    throw Context.Reply(T.StructureAlreadyUnsaved, barricade.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.LogAction(ActionLogType.UnsaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                                 $"at {barricade.GetServersideData().point} ({barricade.instanceID})");
                Context.Reply(T.StructureUnsaved, barricade.asset);
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
                bool removedSave = await _saver.DiscardStructureAsync(structure.instanceID, token);
                await UniTask.SwitchToMainThread(token);
                if (removedSave)
                {
                    Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                                     $"at {structure.GetServersideData().point} ({structure.instanceID})");
                    Context.Reply(T.StructureUnsaved, structure.asset);
                }

                await DestroyStructure(structure, Context.Player, token);
                Context.LogAction(ActionLogType.PopStructure,
                    $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                    $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2", Data.AdminLocale)} ({structure.instanceID})");
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                bool removedSave = await _saver.DiscardStructureAsync(barricade.instanceID, token);
                await UniTask.SwitchToMainThread(token);
                if (removedSave)
                {
                    Context.LogAction(ActionLogType.UnsaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                                     $"at {barricade.GetServersideData().point} ({barricade.instanceID})");
                    Context.Reply(T.StructureUnsaved, barricade.asset);
                }

                await DestroyBarricade(barricade, Context.Player, token);
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

            BarricadeDrop? barricade = null;
            ItemAsset asset;
            if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                asset = structure.asset;
            }
            else if (Context.TryGetBarricadeTarget(out barricade))
            {
                asset = barricade.asset;
            }
            else throw Context.Reply(T.StructureNoTarget);

            await UniTask.SwitchToMainThread(token);
            if (!Context.TryGet(2, out ulong s64) || s64 != 0 && (!grp && new CSteamID(s64).GetEAccountType() != EAccountType.k_EAccountTypeIndividual))
            {
                if (Context.MatchParameter(2, "me"))
                    s64 = grp ? Context.Player.UnturnedPlayer.quests.groupID.m_SteamID : Context.CallerId.m_SteamID;
                else
                    throw Context.SendCorrectUsage(
                        "/structure <set|s> <group|owner> <value> - Value must be 'me', '0' or a valid Steam64 ID");
            }

            string str64 = s64.ToString(Data.AdminLocale);

            ulong? group = grp ? s64 : null;
            ulong? owner = grp ? null : s64;
            uint instanceId;
            if (structure != null)
            {
                instanceId = structure.instanceID;
                F.SetOwnerOrGroup(structure, owner, group);
            }
            else if (barricade != null)
            {
                instanceId = barricade.instanceID;
                F.SetOwnerOrGroup(barricade, owner, group);
            }
            else
                throw Context.Reply(T.StructureNoTarget);

            bool isSaved = await _saver.IsBuildableSavedAsync(instanceId, structure != null, token);
            if (isSaved)
            {
                if (structure != null)
                    await _saver.SaveStructureAsync(structure, token);
                else
                    await _saver.SaveBarricadeAsync(barricade!, token);

                await UniTask.SwitchToMainThread(token);
                Context.LogAction(ActionLogType.SetSavedStructureProperty, $"{asset?.itemName ?? "null"} / {asset?.id ?? 0} / {asset?.GUID ?? Guid.Empty:N} - " +
                                                                           $"SET {(grp ? "GROUP" : "OWNER")} >> {str64}.");
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

    private async UniTask DestroyBarricade(BarricadeDrop bDrop, UCPlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (bDrop == null || !BarricadeManager.tryGetRegion(bDrop.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            player.SendChat(T.StructureNotDestroyable);
            return;
        }

        ISalvageInfo[] components = bDrop.model.GetComponents<ISalvageInfo>();
        
        if (components.Length == 0)
            return;

        SalvageBarricadeRequested args = new SalvageBarricadeRequested(player, bDrop, bDrop.GetServersideData(),
            region, x, y, plant, await _saver.GetBarricadeSaveAsync(bDrop.instanceID, token), default, default);

        await UniTask.SwitchToMainThread(token);

        for (int i = 0; i < components.Length; ++i)
        {
            ISalvageInfo salvage = components[i];
            salvage.IsSalvaged = true;
            salvage.Salvager = player.Steam64;

            if (salvage is not ISalvageListener listener)
                continue;

            listener.OnSalvageRequested(args);
            if (!args.CanContinue)
                break;
        }

        if (args is { CanContinue: false })
        {
            for (int i = 0; i < components.Length; ++i)
            {
                ISalvageInfo salvage = components[i];
                salvage.IsSalvaged = false;
                salvage.Salvager = 0;
            }

            player.SendChat(T.WhitelistProhibitedSalvage, bDrop.asset);
            return;
        }

        BarricadeManager.destroyBarricade(bDrop, x, y, ushort.MaxValue);
        player.SendChat(T.StructureDestroyed, bDrop.asset);
    }
    private async UniTask DestroyStructure(StructureDrop sDrop, UCPlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (sDrop == null || !StructureManager.tryGetRegion(sDrop.model, out byte x, out byte y, out StructureRegion region))
        {
            player.SendChat(T.StructureNotDestroyable);
            return;
        }

        ISalvageInfo[] components = sDrop.model.GetComponents<ISalvageInfo>();

        if (components.Length == 0)
            return;

        SalvageStructureRequested args = new SalvageStructureRequested(player, sDrop, sDrop.GetServersideData(),
            region, x, y, await _saver.GetStructureSaveAsync(sDrop.instanceID, token), default, default);

        await UniTask.SwitchToMainThread(token);

        for (int i = 0; i < components.Length; ++i)
        {
            ISalvageInfo salvage = components[i];
            salvage.IsSalvaged = true;
            salvage.Salvager = player.Steam64;

            if (salvage is not ISalvageListener listener)
                continue;

            listener.OnSalvageRequested(args);
            if (!args.CanContinue)
                break;
        }

        if (args is { CanContinue: false })
        {
            for (int i = 0; i < components.Length; ++i)
            {
                ISalvageInfo salvage = components[i];
                salvage.IsSalvaged = false;
                salvage.Salvager = 0;
            }

            player.SendChat(T.WhitelistProhibitedSalvage, sDrop.asset);
            return;
        }

        StructureManager.destroyStructure(sDrop, x, y, Vector3.Reflect(sDrop.GetServersideData().point - player.Position, Vector3.up).normalized * 4);
        player.SendChat(T.StructureDestroyed, sDrop.asset);
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
