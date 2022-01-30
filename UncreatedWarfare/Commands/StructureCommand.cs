using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Structure = Uncreated.Warfare.Structures.Structure;
using UnityEngine;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Kits;
using System;

namespace Uncreated.Warfare.Commands
{
    public class StructureCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "structure";
        public string Help => "Managed saved structures.";
        public string Syntax => "/structure";
        public List<string> Aliases => new List<string>(1) { "struct" };
        public List<string> Permissions => new List<string>(1) { "uc.structure" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            string action = command[0].ToLower();
            if (command.Length > 0)
            {
                if (action == "save")
                {
                    if (player.HasPermission("uc.structure.save"))
                    {
                        if (!(Data.Gamemode is IStructureSaving))
                        {
                            player.Message("command_e_gamemode");
                            return;
                        }
                        Transform hit = UCBarricadeManager.GetTransformFromLook(player.Player.look, RayMasks.BARRICADE | RayMasks.STRUCTURE);
                        StructureDrop structure = StructureManager.FindStructureByRootTransform(hit);
                        if (structure != null)
                        {
                            if (!StructureSaver.StructureExists(structure.instanceID, EStructType.STRUCTURE, out Structure structexists))
                            {
                                if (StructureSaver.AddStructure(structure, structure.GetServersideData(), out Structure structureaded))
                                {
                                    player.SendChat("structure_saved", structureaded.Asset.itemName);
                                }
                                else
                                {
                                    player.SendChat("structure_not_looking");
                                }
                            }
                            else
                            {
                                player.SendChat("structure_saved_already",
                                    structexists == default ? "unknown" : structexists.Asset.itemName);
                            }
                            return;
                        }
                        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(hit);
                        if (barricade != null)
                        {
                            if (!StructureSaver.StructureExists(barricade.instanceID, EStructType.BARRICADE, out Structure structureexists))
                            {
                                if (StructureSaver.AddStructure(barricade, barricade.GetServersideData(), out Structure structureaded))
                                {
                                    player.Player.SendChat("structure_saved", structureaded.Asset.itemName);
                                    player.Player.SendChat($"Barricade: {barricade.model.position}");
                                    player.Player.SendChat($"Barricade (Server): {barricade.GetServersideData().point}");
                                    player.Player.SendChat($"Saved Structure{structureaded.transform.position}");

                                }
                                else
                                {
                                    player.SendChat("structure_not_looking");
                                }
                            }
                            else
                            {
                                player.SendChat("structure_saved_already",
                                    structureexists == default ? "unknown" : structureexists.Asset.itemName);
                            }
                        }
                        else player.SendChat("structure_not_looking");
                        return;
                    }
                    else
                        player.Player.SendChat("no_permissions");
                    return;
                }
                else if (action == "remove")
                {
                    if (player.HasPermission("uc.structure.remove"))
                    {
                        if (!(Data.Gamemode is IStructureSaving))
                        {
                            player.Message("command_e_gamemode");
                            return;
                        }
                        Transform hit = UCBarricadeManager.GetTransformFromLook(player.Player.look, RayMasks.BARRICADE | RayMasks.STRUCTURE);
                        StructureDrop structure = StructureManager.FindStructureByRootTransform(hit);
                        if (structure != null)
                        {
                            if (StructureSaver.StructureExists(structure.instanceID, EStructType.STRUCTURE, out Structure structureaded))
                            {
                                StructureSaver.RemoveStructure(structureaded);
                                player.Player.SendChat("structure_unsaved", structure.asset.itemName);
                            }
                            else
                            {
                                player.SendChat("structure_unsaved_already", structure.asset.itemName);
                            }
                            return;
                        }
                        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(hit);
                        if (barricade != null)
                        {
                            if (StructureSaver.StructureExists(barricade.instanceID, EStructType.BARRICADE, out Structure structureaded))
                            {
                                StructureSaver.RemoveStructure(structureaded);
                                player.Player.SendChat("structure_unsaved", barricade.asset.itemName);
                            }
                            else
                            {
                                player.SendChat("structure_unsaved_already", barricade.asset.itemName);
                            }
                        }
                        else player.SendChat("structure_not_looking");
                        return;
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (action == "pop" || action == "destroy")
                {
                    if (player.HasPermission("uc.structure.pop"))
                    {
                        Transform hit = UCBarricadeManager.GetTransformFromLook(player.Player.look, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE);
                        StructureDrop structure = StructureManager.FindStructureByRootTransform(hit);
                        if (structure != null)
                        {
                            DestroyStructure(structure, player.Player);
                            return;
                        }
                        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(hit);
                        if (barricade != null)
                        {
                            DestroyBarricade(barricade, player.Player);
                            return;
                        }
                        if (hit.TryGetComponent(out InteractableVehicle veh))
                        {
                            VehicleBay.DeleteVehicle(veh);

                            player.SendChat("structure_popped", veh.asset.vehicleName);
                            if (Vehicles.VehicleSpawner.HasLinkedSpawn(veh.instanceID, out Vehicles.VehicleSpawn spawn))
                                spawn.StartVehicleRespawnTimer();
                        }
                        else player.Player.SendChat("structure_pop_not_poppable");
                        return;
                    }
                    else player.Player.SendChat("no_permissions");
                }
                else if (action == "examine" || action == "wtf")
                {
                    if (player.HasPermission("uc.structure.examine"))
                    {
                        Transform hit = UCBarricadeManager.GetTransformFromLook(player.Player.look, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE);
                        StructureDrop structure = StructureManager.FindStructureByRootTransform(hit);
                        if (structure != null)
                        {
                            ExamineStructure(structure, player.Player, true);
                            return;
                        }
                        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(hit);
                        if (barricade != null)
                        {
                            if (barricade.interactable is InteractableTrap trap)
                                ExamineTrap(trap, player.Player, true);
                            else
                                ExamineBarricade(barricade, player.Player, true);
                            return;
                        }
                        if (hit.TryGetComponent(out InteractableVehicle veh))
                        {
                            ExamineVehicle(veh, player.Player, true);
                        }
                        else player.Player.SendChat("structure_examine_not_examinable");
                        return;
                    }
                    else player.Player.SendChat("no_permissions");
                }
                else if (action == "savemain" || action == "sm")
                {
                    var barricadesInMain = UCBarricadeManager.GetBarricadesWhere(b => F.IsInMain(b.model.position));

                    foreach (var barricade in barricadesInMain)
                    {
                        //if (barricade.interactable is InteractableSign || barricade.interactable is InteractableStorage)
                        //{
                        //    byte[] state = barricade.GetServersideData().barricade.state;
                        //    byte[] newstate = new byte[state.Length];
                        //    Buffer.BlockCopy(BitConverter.GetBytes(player.CSteamID.m_SteamID), 0, newstate, 0, sizeof(ulong));
                        //    Buffer.BlockCopy(BitConverter.GetBytes(3ul), 0, newstate, sizeof(ulong), sizeof(ulong));
                        //    Buffer.BlockCopy(state, sizeof(ulong) * 2, newstate, sizeof(ulong) * 2, state.Length - sizeof(ulong) * 2);
                        //    BarricadeManager.updateReplicatedState(barricade.model, newstate, newstate.Length);
                        //}


                        //BarricadeManager.changeOwnerAndGroup(barricade.model, player.CSteamID.m_SteamID, 3);

                        if (!(barricade.interactable is InteractableSign sign && RequestSigns.SignExists(sign, out var rs)))
                        {
                            if (StructureSaver.StructureExists(barricade.instanceID, EStructType.BARRICADE, out Structure existing))
                                StructureSaver.RemoveStructure(existing);
                            StructureSaver.AddStructure(barricade, barricade.GetServersideData(), out Structure added);
                        }
                    }

                    player.Player.Message($"Saved/Updated {barricadesInMain.Count} barricades in main.".Colorize("ebd0ab"));
                }
                else if (action == "removemain" || action == "rm")
                {
                    for (int i = RequestSigns.ActiveObjects.Count - 1; i >= 0; i--)
                    {
                        RequestSign sign = RequestSigns.ActiveObjects[i];
                        if (!TeamManager.IsInAnyMain(sign.transform.position.Vector3))
                        {
                            RequestSigns.RemoveRequestSign(sign);
                        }
                    }
                    for (int i = VehicleSpawner.ActiveObjects.Count - 1; i >= 0; i--)
                    {
                        Vehicles.VehicleSpawn spawn = VehicleSpawner.ActiveObjects[i];
                        if (!TeamManager.IsInAnyMain(spawn.SpawnpadLocation.position.Vector3))
                        {
                            VehicleSpawner.DeleteSpawn(spawn.SpawnPadInstanceID, spawn.type);
                        }
                    }
                    for (int i = StructureSaver.ActiveObjects.Count - 1; i >= 0; i--)
                    {
                        Structures.Structure structure = StructureSaver.ActiveObjects[i];
                        if (!TeamManager.IsInAnyMain(structure.transform.position.Vector3))
                        {
                            StructureSaver.RemoveStructure(structure);
                        }
                    }
                    List<BarricadeDrop> drops = UCBarricadeManager.GetBarricadesWhere(x => !TeamManager.IsInAnyMain(x.model.transform.position));
                    for (int i = drops.Count - 1; i >= 0; --i)
                    {
                        if (Regions.tryGetCoordinate(drops[i].model.transform.position, out byte x, out byte y))
                            BarricadeManager.destroyBarricade(drops[i], x, y, ushort.MaxValue);
                    }

                    player.Player.Message($"Unsaved any barricades outside of main.".Colorize("ebd0ab"));
                }
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

                SDG.Unturned.BarricadeData data = bdrop.GetServersideData();
                player.SendChat("structure_popped", data.barricade.asset.itemName);
                BarricadeManager.destroyBarricade(bdrop, x, y, ushort.MaxValue);
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
                SDG.Unturned.StructureData data = sdrop.GetServersideData();
                player.SendChat("structure_popped", data.structure.asset.itemName);
                StructureManager.destroyStructure(sdrop, x, y, UnityEngine.Vector3.down);
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
                if (Data.Gamemode is TeamGamemode)
                {
                    ulong team = vehicle.lockedOwner.m_SteamID.GetTeamFromPlayerSteam64ID();
                    string teamname = TeamManager.TranslateName(team, player);
                    if (sendurl)
                    {
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _,
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
                    ulong grp = plr == null ? 0 : plr.quests.groupID.m_SteamID;
                    if (sendurl)
                    {
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _,
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

                if (Data.Gamemode is TeamGamemode)
                {
                    string teamname = TeamManager.TranslateName(data.group, player);
                    if (sendurl)
                    {
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _, data.barricade.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, teamname), data.owner);
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
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _, data.barricade.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, grp.ToString()), data.owner);
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
                if (Data.Gamemode is TeamGamemode)
                {
                    string teamname = TeamManager.TranslateName(data.group, player);
                    if (sendurl)
                    {
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _, data.structure.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, teamname), data.owner);
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
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _, data.structure.asset.itemName, F.GetPlayerOriginalNames(data.owner).CharacterName, grp.ToString()), data.owner);
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
        private void ExamineTrap(InteractableTrap trap, Player player, bool sendurl)
        {
            if (trap.transform.TryGetComponent(out Components.BarricadeComponent data))
            {
                if (data.Owner == 0)
                {
                    player.SendChat("structure_examine_not_examinable");
                    return;
                }

                SteamPlayer owner = PlayerTool.getSteamPlayer(data.Owner);
                if (Data.Gamemode is TeamGamemode)
                {
                    ulong team = owner == null ? (PlayerManager.HasSave(data.Owner, out PlayerSave save) ? save.Team : 0) : owner.GetTeam();
                    string teamname = TeamManager.TranslateName(team, player);
                    if (sendurl)
                    {
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _,
                            Assets.find(data.BarricadeGUID) is ItemAsset asset ? asset.itemName : data.BarricadeGUID.ToString("N"),
                            F.GetPlayerOriginalNames(data.Owner).CharacterName, teamname), data.Owner);
                    }
                    else
                    {
                        Players.FPlayerName ownerName = F.GetPlayerOriginalNames(data.Owner);
                        player.SendChat("structure_last_owner_chat",
                            Assets.find(data.BarricadeGUID) is ItemAsset asset ? asset.itemName : data.BarricadeGUID.ToString("N"),
                            ownerName.CharacterName,
                            ownerName.PlayerName,
                            TeamManager.GetTeamHexColor(team),
                            teamname, TeamManager.GetTeamHexColor(team));
                    }
                } 
                else
                {
                    ulong grp = owner == null ? 0 : owner.player.quests.groupID.m_SteamID;
                    if (sendurl)
                    {
                        player.channel.owner.SendSteamURL(Translation.Translate("structure_last_owner_web_prompt", player, out _,
                            Assets.find(data.BarricadeGUID) is ItemAsset asset ? asset.itemName : data.BarricadeGUID.ToString("N"),
                            F.GetPlayerOriginalNames(data.Owner).CharacterName, grp.ToString()), data.Owner);
                    }
                    else
                    {
                        string clr = UCWarfare.GetColorHex("neutral");
                        Players.FPlayerName ownerName = F.GetPlayerOriginalNames(data.Owner);
                        player.SendChat("structure_last_owner_chat",
                            Assets.find(data.BarricadeGUID) is ItemAsset asset ? asset.itemName : data.BarricadeGUID.ToString("N"),
                            ownerName.CharacterName,
                            ownerName.PlayerName,
                            clr, grp.ToString(), clr);
                    }
                }
            }
            else
            {
                player.SendChat("structure_examine_not_examinable");
            }
        }
    }
}
