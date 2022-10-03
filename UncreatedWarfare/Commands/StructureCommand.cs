using SDG.Unturned;
using System;
using Uncreated.Framework;
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

        if (ctx.MatchParameter(0, "save"))
        {
            ctx.AssertGamemode<IStructureSaving>();

            ctx.AssertPermissions(EAdminType.HELPER);

            if (ctx.TryGetTarget(out StructureDrop structure))
            {
                if (StructureSaver.AddStructure(structure, out SavedStructure st))
                {
                    ctx.Reply(T.StructureSaved, st);
                    ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
                        $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {st.Position} ({st.InstanceID})");
                }
                else if (st is not null)
                    throw ctx.Reply(T.StructureAlreadySaved, st);
                else
                    throw ctx.SendUnknownError();
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                if (StructureSaver.AddBarricade(barricade, out SavedStructure st))
                {
                    ctx.Reply(T.StructureSaved, st);
                    ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
                        $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {st.Position} ({st.InstanceID})");
                }
                else if (st is not null)
                    throw ctx.Reply(T.StructureAlreadySaved, st);
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
                if (StructureSaver.SaveExists(structure, out SavedStructure st))
                {
                    if (StructureSaver.RemoveSave(st))
                    {
                        ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                            $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {st.Position} ({st.InstanceID})");
                        ctx.Reply(T.StructureUnsaved, st);
                    }
                    else
                        throw ctx.SendUnknownError();
                }
                else throw ctx.Reply(T.StructureAlreadyUnsaved, structure.asset);
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                if (StructureSaver.SaveExists(barricade, out SavedStructure st))
                {
                    if (StructureSaver.RemoveSave(st))
                    {
                        ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                            $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {st.Position} ({st.InstanceID})");
                        ctx.Reply(T.StructureUnsaved, st);
                    }
                    else
                        throw ctx.SendUnknownError();
                }
                else throw ctx.Reply(T.StructureAlreadyUnsaved, barricade.asset);
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
                if (StructureSaver.SaveExists(structure, out SavedStructure st))
                {
                    if (StructureSaver.RemoveSave(st))
                    {
                        ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                            $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {st.Position} ({st.InstanceID})");
                        ctx.Reply(T.StructureUnsaved, st);
                    }
                    else ctx.SendUnknownError();
                }

                DestroyStructure(structure, ctx.Caller);
                ctx.LogAction(EActionLogType.POP_STRUCTURE,
                    $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                    $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2")} ({structure.instanceID})");
                ctx.Defer();
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                if (StructureSaver.SaveExists(barricade, out SavedStructure st))
                {
                    if (StructureSaver.RemoveSave(st))
                    {
                        ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                            $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {st.Position} ({st.InstanceID})");
                        ctx.Reply(T.StructureUnsaved, st);
                    }
                    else ctx.SendUnknownError();
                }

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

            if (ctx.TryGet(2, out string value) && ctx.TryGet(1, out string property))
            {
                ESetFieldResult result;
                SavedStructure? data;
                if (ctx.TryGetTarget(out StructureDrop structure))
                {
                    StructureSaver.SaveExists(structure, out data);
                }
                else if (ctx.TryGetTarget(out BarricadeDrop barricade))
                {
                    StructureSaver.SaveExists(barricade, out data);
                }
                else throw ctx.Reply(T.StructureNoTarget);
                if (data is null)
                    throw ctx.Reply(T.StructureAlreadyUnsaved, structure.asset);

                result = StructureSaver.Singleton.SetProperty(data, ref property, value);
                switch (result)
                {
                    case ESetFieldResult.SUCCESS:
                        ItemAsset? asset = Assets.find<ItemAsset>(data.ItemGuid);
                        ctx.LogAction(EActionLogType.SET_SAVED_STRUCTURE_PROPERTY, $"{asset?.itemName ?? "null"} / {(asset == null ? 0 : asset.id)} / {data.ItemGuid:N} - SET " + property.ToUpper() + " >> " + value.ToUpper());
                        ctx.Reply(T.StructureSaveSetProperty!, property, asset, value);
                        StructureSaver.Singleton.Check(data);
                        StructureSaver.SaveSingleton();
                        break;
                    default:
                    case ESetFieldResult.OBJECT_NOT_FOUND:
                        ctx.Reply(T.StructureAlreadyUnsaved);
                        break;
                    case ESetFieldResult.FIELD_NOT_FOUND:
                        ctx.Reply(T.StructureSaveInvalidProperty, property);
                        break;
                    case ESetFieldResult.FIELD_NOT_SERIALIZABLE:
                    case ESetFieldResult.INVALID_INPUT:
                        ctx.Reply(T.StructureSaveInvalidSetValue, value, property);
                        break;
                    case ESetFieldResult.FIELD_PROTECTED:
                        ctx.Reply(T.StructureSaveNotJsonSettable, property);
                        break;
                }
            }
            else
                ctx.SendCorrectUsage("/structure <set|s> <group|owner> <value>");
        }
        else
            ctx.SendCorrectUsage(SYNTAX);
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
