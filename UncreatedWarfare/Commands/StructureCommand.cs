using System;
using System.Globalization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("structure", "struct")]
[MetadataFile(nameof(GetHelpMetadata))]
public class StructureCommand : IExecutableCommand
{
    // todo split
    private readonly BuildableSaver _saver;
    private readonly VehicleService _vehicleService;
    private readonly EventDispatcher2 _eventDispatcher;
    private readonly StructureTranslations _translations;
    private const string Syntax = "/structure <save|remove|examine|pop|set>";
    private const string Help = "Managed saved structures.";

    private static readonly PermissionLeaf PermissionSave    = new PermissionLeaf("commands.structure.save",    unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionRemove  = new PermissionLeaf("commands.structure.remove",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionDestroy = new PermissionLeaf("commands.structure.destroy", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionSet     = new PermissionLeaf("commands.structure.set",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionExamine = new PermissionLeaf("commands.structure.examine", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public StructureCommand(BuildableSaver saver, VehicleService vehicleService, TranslationInjection<StructureTranslations> translations, EventDispatcher2 eventDispatcher)
    {
        _saver = saver;
        _vehicleService = vehicleService;
        _eventDispatcher = eventDispatcher;
        _translations = translations.Value;
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

        Context.AssertArgs(1, Syntax);

        Context.Defer();
        
        if (Context.MatchParameter(0, "save"))
        {
            await Context.AssertPermissions(PermissionSave, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                if (!await _saver.SaveStructureAsync(structure, token))
                {
                    throw Context.Reply(_translations.StructureAlreadySaved, structure.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.Reply(_translations.StructureSaved, structure.asset);
                Context.LogAction(ActionLogType.SaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                               $"at {structure.GetServersideData().point:0:##} ({structure.instanceID})");
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                if (!await _saver.SaveBarricadeAsync(barricade, token))
                {
                    throw Context.Reply(_translations.StructureAlreadySaved, barricade.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.Reply(_translations.StructureSaved, barricade.asset);
                Context.LogAction(ActionLogType.SaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                               $"at {barricade.GetServersideData().point:0:##} ({barricade.instanceID})");
            }
            else throw Context.Reply(_translations.StructureNoTarget);
        }
        else if (Context.MatchParameter(0, "remove", "delete"))
        {
            await Context.AssertPermissions(PermissionRemove, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                if (!await _saver.DiscardStructureAsync(structure.instanceID, token))
                {
                    throw Context.Reply(_translations.StructureAlreadyUnsaved, structure.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                                 $"at {structure.GetServersideData().point} ({structure.instanceID})");
                Context.Reply(_translations.StructureUnsaved, structure.asset);
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                if (!await _saver.DiscardStructureAsync(barricade.instanceID, token))
                {
                    throw Context.Reply(_translations.StructureAlreadyUnsaved, barricade.asset);
                }

                await UniTask.SwitchToMainThread(token);

                Context.LogAction(ActionLogType.UnsaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                                 $"at {barricade.GetServersideData().point} ({barricade.instanceID})");
                Context.Reply(_translations.StructureUnsaved, barricade.asset);
            }
            else throw Context.Reply(_translations.StructureNoTarget);
        }
        else if (Context.MatchParameter(0, "destroy", "pop"))
        {
            await Context.AssertPermissions(PermissionDestroy, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            {
                await _vehicleService.DeleteVehicleAsync(vehicle, token);

                Context.LogAction(ActionLogType.PopStructure,
                    $"VEHICLE: {vehicle.asset.vehicleName} / {vehicle.asset.id} /" +
                    $" {vehicle.asset.GUID:N} at {vehicle.transform.position:N2} ({vehicle.instanceID})");
                Context.Reply(_translations.StructureDestroyed, vehicle.asset);
            }
            else if (Context.TryGetStructureTarget(out StructureDrop? structure))
            {
                bool removedSave = await _saver.DiscardStructureAsync(structure.instanceID, token);
                await UniTask.SwitchToMainThread(token);
                if (removedSave)
                {
                    Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                                     $"at {structure.GetServersideData().point} ({structure.instanceID})");
                    Context.Reply(_translations.StructureUnsaved, structure.asset);
                }

                await DestroyStructure(structure, Context.Player, token);
                Context.LogAction(ActionLogType.PopStructure,
                    $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                    $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2", CultureInfo.InvariantCulture)} ({structure.instanceID})");
            }
            else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                bool removedSave = await _saver.DiscardStructureAsync(barricade.instanceID, token);
                await UniTask.SwitchToMainThread(token);
                if (removedSave)
                {
                    Context.LogAction(ActionLogType.UnsaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                                     $"at {barricade.GetServersideData().point} ({barricade.instanceID})");
                    Context.Reply(_translations.StructureUnsaved, barricade.asset);
                }

                await DestroyBarricade(barricade, Context.Player, token);
                Context.LogAction(ActionLogType.PopStructure,
                    $"BARRICADE: {barricade.asset.itemName} / {barricade.asset.id} /" +
                    $" {barricade.asset.GUID:N} at {barricade.model.transform.position.ToString("N2", CultureInfo.InvariantCulture)} ({barricade.instanceID})");
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
            else throw Context.Reply(_translations.StructureExamineNotExaminable);

            Context.Defer();
        }
        else if (Context.MatchParameter(0, "set", "s"))
        {
            await Context.AssertPermissions(PermissionSet, token);
            await UniTask.SwitchToMainThread(token);
            
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
            else throw Context.Reply(_translations.StructureNoTarget);

            await UniTask.SwitchToMainThread(token);
            if (!Context.TryGet(2, out CSteamID s64) || s64 != CSteamID.Nil && (!grp && s64.GetEAccountType() != EAccountType.k_EAccountTypeIndividual))
            {
                if (!Context.MatchParameter(2, "me"))
                    throw Context.SendHelp();
                
                // self
                s64 = grp ? Context.Player.UnturnedPlayer.quests.groupID : Context.CallerId;
            }

            string str64 = s64.m_SteamID.ToString(CultureInfo.InvariantCulture);

            CSteamID? group = grp ? s64 : null;
            CSteamID? owner = grp ? null : s64;
            uint instanceId;
            if (structure != null)
            {
                instanceId = structure.instanceID;
                StructureUtility.SetOwnerOrGroup(structure, owner, group);
            }
            else if (barricade != null)
            {
                instanceId = barricade.instanceID;
                BarricadeUtility.SetOwnerOrGroup(barricade, Context.ServiceProvider, owner, group);
            }
            else
                throw Context.Reply(_translations.StructureNoTarget);

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

            //if (grp)
            //{
            //    FactionInfo? info = TeamManager.GetFactionSafe(s64);
            //    if (info != null)
            //        str64 = info.GetName(Context.Language).Colorize(info.Color, Context.IMGUI);
            //}

            Context.Reply(_translations.StructureSaveSetProperty!, grp ? "Group" : "Owner", asset, str64);
        }
        else
            Context.SendCorrectUsage(Syntax);
    }

    private async UniTask DestroyBarricade(BarricadeDrop bDrop, WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (bDrop == null || !BarricadeManager.tryGetRegion(bDrop.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            Context.Reply(_translations.StructureNotDestroyable);
            return;
        }

        // simulate salvaging the barricade
        SalvageBarricadeRequested args = new SalvageBarricadeRequested(region)
        {
            Player = player,
            InstanceId = bDrop.instanceID,
            Barricade = bDrop,
            ServersideData = bDrop.GetServersideData(),
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant
        };

        BuildableExtensions.SetDestroyInfo(bDrop.model, args, null);
        bool shouldAllow = true;
        try
        {
            bool shouldAllowTemp = shouldAllow;
            BuildableExtensions.SetSalvageInfo(bDrop.model, Context.CallerId, true, salvageInfo =>
            {
                if (salvageInfo is not ISalvageListener listener)
                    return true;

                listener.OnSalvageRequested(args);

                if (args.IsActionCancelled)
                    shouldAllowTemp = false;

                return !args.IsCancelled;
            });

            shouldAllow = shouldAllowTemp;

            EventContinuations.Dispatch(args, _eventDispatcher, token, out shouldAllow, continuation: args =>
            {
                if (args.ServersideData.barricade.isDead)
                    return;

                // simulate BarricadeDrop.ReceiveSalvageRequest
                ItemBarricadeAsset asset = args.Barricade.asset;
                if (asset.isUnpickupable)
                    return;

                // re-apply ISalvageInfo components
                BuildableExtensions.SetSalvageInfo(args.Transform, args.Steam64, true, null);

                if (!BarricadeManager.tryGetRegion(args.Barricade.model, out byte x, out byte y, out ushort plant, out _))
                {
                    x = args.RegionPosition.x;
                    y = args.RegionPosition.y;
                    plant = args.VehicleRegionIndex;
                }

                BarricadeManager.destroyBarricade(args.Barricade, x, y, plant);
            });
        }
        finally
        {
            // undo setting this if the task needs continuing, it'll be re-set later
            if (!shouldAllow)
            {
                BuildableExtensions.SetSalvageInfo(bDrop.model, null, false, null);
            }
        }

        BarricadeManager.destroyBarricade(bDrop, x, y, ushort.MaxValue);
        Context.Reply(_translations.StructureDestroyed, bDrop.asset);
    }
    private async UniTask DestroyStructure(StructureDrop sDrop, WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (sDrop == null || !StructureManager.tryGetRegion(sDrop.model, out byte x, out byte y, out StructureRegion region))
        {
            Context.Reply(_translations.StructureNotDestroyable);
            return;
        }

        // simulate salvaging the structure
        SalvageStructureRequested args = new SalvageStructureRequested(region)
        {
            Player = player,
            InstanceId = sDrop.instanceID,
            Structure = sDrop,
            ServersideData = sDrop.GetServersideData(),
            RegionPosition = new RegionCoord(x, y)
        };

        BuildableExtensions.SetDestroyInfo(sDrop.model, args, null);

        bool shouldAllow = true;
        try
        {
            BuildableExtensions.SetSalvageInfo(sDrop.model, Context.CallerId, true, salvageInfo =>
            {
                if (salvageInfo is not ISalvageListener listener)
                    return true;

                listener.OnSalvageRequested(args);

                if (args.IsActionCancelled)
                    shouldAllow = false;

                return !args.IsCancelled;
            });

            EventContinuations.Dispatch(args, _eventDispatcher, token, out shouldAllow, continuation: args =>
            {
                if (args.ServersideData.structure.isDead)
                    return;

                // simulate StructureDrop.ReceiveSalvageRequest
                ItemStructureAsset? asset = args.Structure.asset;
                if (asset is { isUnpickupable: true })
                    return;

                // re-apply ISalvageInfo components
                BuildableExtensions.SetSalvageInfo(args.Transform, args.Steam64, true, null);

                if (!StructureManager.tryGetRegion(args.Structure.model, out byte x, out byte y, out _))
                {
                    x = args.RegionPosition.x;
                    y = args.RegionPosition.y;
                }

                StructureManager.destroyStructure(sDrop, x, y, Vector3.Reflect(sDrop.GetServersideData().point - player.Position, Vector3.up).normalized * 4);
                Context.Reply(_translations.StructureDestroyed, sDrop.asset);
            });
        }
        finally
        {
            if (!shouldAllow)
            {
                BuildableExtensions.SetSalvageInfo(sDrop.model, null, false, null);
            }
        }

        if (!shouldAllow)
            return;

        StructureManager.destroyStructure(sDrop, x, y, Vector3.Reflect(sDrop.GetServersideData().point - player.Position, Vector3.up).normalized * 4);
        Context.Reply(_translations.StructureDestroyed, sDrop.asset);
    }

    private async Task ExamineVehicle(InteractableVehicle vehicle, WarfarePlayer player, bool sendurl, CancellationToken token = default)
    {
        GameThread.AssertCurrent();
        if (vehicle.lockedOwner == default || vehicle.lockedOwner == Steamworks.CSteamID.Nil)
        {
            Context.Reply(_translations.StructureExamineNotLocked);
        }
        else
        {
            Team team = Team.NoTeam; // todo vehicle.lockedGroup.m_SteamID.GetTeam();
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
                Context.ReplySteamProfileUrl(_translations.VehicleExamineLastOwnerPrompt
                        .Translate(vehicle.asset, names, team.Faction, prevOwnerName, prevOwner, player, canUseIMGUI: true), vehicle.lockedOwner);
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(vehicle.lockedOwner);
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(_translations.VehicleExamineLastOwnerChat, vehicle.asset, names, pl, team.Faction, prevOwnerName, prevOwner);
            }
        }
    }
    private async Task ExamineBarricade(BarricadeDrop bdrop, WarfarePlayer player, bool sendurl, CancellationToken token = default)
    {
        GameThread.AssertCurrent();
        if (bdrop != null)
        {
            BarricadeData data = bdrop.GetServersideData();
            if (data.owner == 0)
            {
                Context.Reply(_translations.StructureExamineNotExaminable);
                return;
            }
            Team team = Team.NoTeam; // todo data.owner.GetTeamFromPlayerSteam64ID()
            IPlayer names = await F.GetPlayerOriginalNamesAsync(data.owner, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (sendurl)
            {
                Context.ReplySteamProfileUrl(_translations.StructureExamineLastOwnerPrompt.Translate(data.barricade.asset, names, team.Faction, player, canUseIMGUI: true),
                    new CSteamID(data.owner));
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(new CSteamID(data.owner));
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(_translations.StructureExamineLastOwnerChat, data.barricade.asset, names, pl, team.Faction);
            }
        }
        else
        {
            Context.Reply(_translations.StructureExamineNotExaminable);
        }
    }
    private async Task ExamineStructure(StructureDrop sdrop, WarfarePlayer player, bool sendurl, CancellationToken token = default)
    {
        GameThread.AssertCurrent();
        if (sdrop != null)
        {
            StructureData data = sdrop.GetServersideData();
            if (data.owner == default)
            {
                Context.Reply(_translations.StructureExamineNotExaminable);
                return;
            }
            Team team = Team.NoTeam; // todo data.owner.GetTeamFromPlayerSteam64ID()
            IPlayer names = await F.GetPlayerOriginalNamesAsync(data.owner, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (sendurl)
            {
                Context.ReplySteamProfileUrl(_translations.StructureExamineLastOwnerPrompt.Translate(data.structure.asset, names, team.Faction, player, canUseIMGUI: true), new CSteamID(data.owner));
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(new CSteamID(data.owner));
                await pl.CacheUsernames(token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(_translations.StructureExamineLastOwnerChat, data.structure.asset, names, pl, team.Faction);
            }
        }
        else
        {
            Context.Reply(_translations.StructureExamineNotExaminable);
        }
    }
}

public class StructureTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Structure Command";
    
    [TranslationData]
    public readonly Translation StructureNoTarget = new Translation("<#ff8c69>You must be looking at a barricade, structure, or vehicle.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureSaved = new Translation<ItemAsset>("<#e6e3d5>Saved <#c6d4b8>{0}</color>.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureAlreadySaved = new Translation<ItemAsset>("<#e6e3d5><#c6d4b8>{0}</color> is already saved.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureUnsaved = new Translation<ItemAsset>("<#e6e3d5>Removed <#c6d4b8>{0}</color> save.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<ItemAsset> StructureAlreadyUnsaved = new Translation<ItemAsset>("<#ff8c69><#c6d4b8>{0}</color> is not saved.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<Asset> StructureDestroyed = new Translation<Asset>("<#e6e3d5>Destroyed <#c6d4b8>{0}</color>.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation StructureNotDestroyable = new Translation("<#ff8c69>That object can not be destroyed.");
    
    [TranslationData]
    public readonly Translation StructureExamineNotExaminable = new Translation("<#ff8c69>That object can not be examined.");
    
    [TranslationData]
    public readonly Translation StructureExamineNotLocked = new Translation("<#ff8c69>This vehicle is not locked.");
    
    [TranslationData]
    public readonly Translation<Asset, IPlayer, FactionInfo> StructureExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo>("Last owner of {0}: {1}, Team: {2}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg1Fmt: WarfarePlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);
    
    [TranslationData]
    public readonly Translation<Asset, IPlayer, IPlayer, FactionInfo> StructureExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo>("<#c6d4b8>Last owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg0Fmt: RarityColorAddon.Instance, arg1Fmt: WarfarePlayer.FormatColoredPlayerName, arg2Fmt: WarfarePlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);
    
    [TranslationData]
    public readonly Translation<Asset, IPlayer, FactionInfo, string, ulong> VehicleExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo, string, ulong>("Owner of {0}: {1}, Team: {2}. Previous Owner: {3} ({4}).", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg1Fmt: WarfarePlayer.FormatPlayerName, arg2Fmt: FactionInfo.FormatDisplayName);
    
    [TranslationData]
    public readonly Translation<Asset, IPlayer, IPlayer, FactionInfo, string, ulong> VehicleExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo, string, ulong>("<#c6d4b8>Owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}. Previous Owner: {4} <i>({5})</i>.", TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg0Fmt: RarityColorAddon.Instance, arg1Fmt: WarfarePlayer.FormatColoredPlayerName, arg2Fmt: WarfarePlayer.FormatSteam64, arg3Fmt: FactionInfo.FormatColorDisplayName);
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> StructureSaveInvalidProperty = new Translation<string>("<#ff8c69>{0} isn't a valid a structure property. Try putting 'owner' or 'group'.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string, string> StructureSaveInvalidSetValue = new Translation<string, string>("<#ff8c69><#ddd>{0}</color> isn't a valid value for structure property: <#a0ad8e>{1}</color>.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> StructureSaveNotJsonSettable = new Translation<string>("<#ff8c69><#a0ad8e>{0}</color> is not marked as settable.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string, ItemAsset, string> StructureSaveSetProperty = new Translation<string, ItemAsset, string>("<#a0ad8e>Set <#8ce4ff>{0}</color> for {1} save to: <#ffffff>{2}</color>.", arg1Fmt: RarityColorAddon.Instance);
}