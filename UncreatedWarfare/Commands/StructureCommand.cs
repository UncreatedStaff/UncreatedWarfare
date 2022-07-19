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
                    throw ctx.Reply("structure_saved_already", structure.asset.itemName);
                if (StructureSaver.AddStructure(structure, structure.GetServersideData(), out str))
                {
                    ctx.Reply("structure_saved", structure.asset.itemName);
                    ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
                        $"{str.type}: {structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} at {str.transform} ({str.instance_id})");
                }
                else throw ctx.SendUnknownError();
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                if (StructureSaver.StructureExists(barricade.instanceID, EStructType.BARRICADE, out Structure str))
                    throw ctx.Reply("structure_saved_already", barricade.asset.itemName);
                if (StructureSaver.AddStructure(barricade, barricade.GetServersideData(), out str))
                {
                    ctx.Reply("structure_saved", barricade.asset.itemName);
                    ctx.LogAction(EActionLogType.SAVE_STRUCTURE,
                        $"{str.type}: {barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} at {str.transform} ({str.instance_id})");
                }
                else throw ctx.SendUnknownError();
            }
            else throw ctx.Reply("structure_not_looking");
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
                    ctx.Reply("structure_unsaved", structure.asset.itemName);
                }
                else throw ctx.Reply("structure_unsaved_already", structure.asset.itemName);
            }
            else if (ctx.TryGetTarget(out BarricadeDrop barricade))
            {
                if (StructureSaver.StructureExists(barricade.instanceID, EStructType.BARRICADE, out Structure str))
                {
                    StructureSaver.RemoveStructure(str);
                    ctx.LogAction(EActionLogType.UNSAVE_STRUCTURE,
                        $"{str.type}: {barricade.asset.itemName} / {barricade.asset.id} /" +
                        $" {barricade.asset.GUID:N} at {str.transform} ({str.instance_id})");
                    ctx.Reply("structure_unsaved", barricade.asset.itemName);
                }
                else throw ctx.Reply("structure_unsaved_already", barricade.asset.itemName);
            }
            else throw ctx.Reply("structure_not_looking");
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
                ctx.Reply("structure_popped", vehicle.asset.vehicleName);
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
            else throw ctx.Reply("structure_examine_not_examinable");
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
            player.SendChat("structure_popped", data.barricade.asset.itemName);
        }
        else
        {
            player.SendChat("structure_pop_not_poppable");
        }
    }
    private void DestroyStructure(StructureDrop sdrop, Player player)
    {
        if (sdrop != null && Regions.tryGetCoordinate(sdrop.model.position, out byte x, out byte y))
        {
            StructureData data = sdrop.GetServersideData();
            StructureManager.destroyStructure(sdrop, x, y, Vector3.down);
            player.SendChat("structure_popped", data.structure.asset.itemName);
        }
        else
        {
            player.SendChat("structure_pop_not_poppable");
        }
    }
    private void ExamineVehicle(InteractableVehicle vehicle, Player player, bool sendurl)
    {
        if (vehicle.lockedOwner == default || vehicle.lockedOwner == Steamworks.CSteamID.Nil)
        {
            player.SendChat("structure_examine_not_locked");
        }
        else
        {
            if (Data.Gamemode is ITeams)
            {
                ulong team = vehicle.lockedOwner.m_SteamID.GetTeamFromPlayerSteam64ID();
                if (team == 0)
                    team = vehicle.lockedGroup.m_SteamID.GetTeam();
                string teamname = TeamManager.TranslateName(team, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(Localization.Translate("structure_last_owner_web_prompt", player, out _,
                        Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID).CharacterName, teamname), vehicle.lockedOwner.m_SteamID);
                }
                else
                {
                    string teamcolor = TeamManager.GetTeamHexColor(team);
                    player.SendChat("structure_last_owner_chat",
                        Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID).CharacterName,
                        vehicle.lockedOwner.m_SteamID.ToString(Data.Locale), teamcolor, teamname, teamcolor);
                }
            } 
            else
            {
                Player plr = PlayerTool.getPlayer(vehicle.lockedOwner);
                ulong grp = plr == null ? vehicle.lockedGroup.m_SteamID : plr.quests.groupID.m_SteamID;
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(Localization.Translate("structure_last_owner_web_prompt", player, out _,
                        Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID).CharacterName, grp.ToString()), vehicle.lockedOwner.m_SteamID);
                }
                else
                {
                    string clr = UCWarfare.GetColorHex("neutral");
                    player.SendChat("structure_last_owner_chat",
                        Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID).CharacterName,
                        vehicle.lockedOwner.m_SteamID.ToString(Data.Locale), clr, grp.ToString(), clr);
                }
            }
        }
    }
    private void ExamineBarricade(BarricadeDrop bdrop, Player player, bool sendurl)
    {
        if (bdrop != null)
        {
            SDG.Unturned.BarricadeData data = bdrop.GetServersideData();
            if (data.owner == default || data.owner == 0)
            {
                player.SendChat("structure_examine_not_examinable");
                return;
            }

            if (Data.Gamemode is ITeams)
            {
                string teamname = TeamManager.TranslateName(data.group, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(Localization.Translate("structure_last_owner_web_prompt", player, out _, data.barricade.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, teamname), data.owner);
                }
                else
                {
                    player.SendChat("structure_last_owner_chat", data.barricade.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName,
                        data.owner.ToString(Data.Locale), TeamManager.GetTeamHexColor(data.owner.GetTeamFromPlayerSteam64ID()),
                        teamname, TeamManager.GetTeamHexColor(data.@group.GetTeam()));
                }
            }
            else
            {
                ulong grp = data.group;
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(Localization.Translate("structure_last_owner_web_prompt", player, out _, data.barricade.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, grp.ToString()), data.owner);
                }
                else
                {
                    string clr = UCWarfare.GetColorHex("neutral");
                    player.SendChat("structure_last_owner_chat", data.barricade.asset.itemName,
                        F.GetPlayerOriginalNames(data.owner).CharacterName,
                        data.owner.ToString(Data.Locale), clr, grp.ToString(), clr);
                }
            }
        }
        else
        {
            player.SendChat("structure_examine_not_examinable");
        }
    }
    private void ExamineStructure(StructureDrop sdrop, Player player, bool sendurl)
    {
        if (sdrop != null)
        {
            SDG.Unturned.StructureData data = sdrop.GetServersideData();
            if (data.owner == default || data.owner == 0)
            {
                player.SendChat("structure_examine_not_examinable");
                return;
            }
            if (Data.Gamemode is ITeams)
            {
                string teamname = TeamManager.TranslateName(data.group, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(Localization.Translate("structure_last_owner_web_prompt", player, out _, data.structure.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, teamname), data.owner);
                }
                else
                {
                    player.SendChat("structure_last_owner_chat", data.structure.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName,
                        data.owner.ToString(Data.Locale), TeamManager.GetTeamHexColor(data.owner.GetTeamFromPlayerSteam64ID()),
                        teamname, TeamManager.GetTeamHexColor(data.@group.GetTeam()));
                }
            }
            else
            {
                ulong grp = data.group;
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(Localization.Translate("structure_last_owner_web_prompt", player, out _, data.structure.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, grp.ToString()), data.owner);
                }
                else
                {
                    string clr = UCWarfare.GetColorHex("neutral");
                    player.SendChat("structure_last_owner_chat", data.structure.asset.itemName,
                        F.GetPlayerOriginalNames(data.owner).CharacterName,
                        data.owner.ToString(Data.Locale), clr, grp.ToString(), clr);
                }
            }
        }
        else
        {
            player.SendChat("structure_examine_not_examinable");
        }
    }
}
