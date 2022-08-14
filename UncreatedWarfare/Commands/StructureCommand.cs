using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using Structure = Uncreated.Warfare.Structures.Structure;

namespace Uncreated.Warfare.Commands;
public class StructureCommand : Command
{
    private const string SYNTAX = "/structure <save|remove|pop|examine>";
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
                if (StructureSaver.StructureExists(structure.instanceID, EStructType.STRUCTURE, out Structure str))
                    throw ctx.Reply(T.StructureAlreadySaved, str);
                if (StructureSaver.AddStructure(structure, structure.GetServersideData(), out str))
                {
                    ctx.Reply(T.StructureSaved, str);
                    ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
                        $"{str.type}: {structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {str.transform} ({str.instance_id})");
                }
                else throw ctx.SendUnknownError();
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                if (StructureSaver.StructureExists(barricade.instanceID, EStructType.BARRICADE, out Structure str))
                    throw ctx.Reply(T.StructureAlreadySaved, str);
                if (StructureSaver.AddStructure(barricade, barricade.GetServersideData(), out str))
                {
                    ctx.Reply(T.StructureSaved, str);
                    ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
                        $"{str.type}: {barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {str.transform} ({str.instance_id})");
                }
                else throw ctx.SendUnknownError();
            }
            else throw ctx.Reply(T.StructureNoTarget);
        }
        else if (ctx.MatchParameter(0, "remove", "delete"))
        {
            ctx.AssertGamemode<IStructureSaving>();

            ctx.AssertPermissions(EAdminType.HELPER);

            if (ctx.TryGetTarget(out StructureDrop structure))
            {
                if (StructureSaver.StructureExists(structure.instanceID, EStructType.STRUCTURE, out Structure str))
                {
                    StructureSaver.RemoveStructure(str);
                    ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                        $"{str.type}: {structure.asset.itemName} / {structure.asset.id} /" +
                        $" {structure.asset.GUID:N} at {str.transform} ({str.instance_id})");
                    ctx.Reply(T.StructureUnsaved, str);
                }
                else throw ctx.Reply(T.StructureAlreadyUnsaved, structure.asset);
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                if (StructureSaver.StructureExists(barricade.instanceID, EStructType.BARRICADE, out Structure str))
                {
                    StructureSaver.RemoveStructure(str);
                    ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                        $"{str.type}: {barricade.asset.itemName} / {barricade.asset.id} /" +
                        $" {barricade.asset.GUID:N} at {str.transform} ({str.instance_id})");
                    ctx.Reply(T.StructureUnsaved, str);
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
                DestroyStructure(structure, ctx.Caller);
                ctx.LogAction(EActionLogType.POP_STRUCTURE,
                    $"STRUCTURE: {structure.asset.itemName} / {structure.asset.id} /" +
                    $" {structure.asset.GUID:N} at {structure.model.transform.position.ToString("N2")} ({structure.instanceID})");
                ctx.Defer();
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
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
    }
    private void DestroyBarricade(BarricadeDrop bdrop, Player player)
    {
        if (bdrop != null && Regions.tryGetCoordinate(bdrop.model.position, out byte x, out byte y))
        {
            if (bdrop.model.TryGetComponent(out Components.FOBComponent f))
            {
                f.parent.IsWipedByAuthority = true;
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
