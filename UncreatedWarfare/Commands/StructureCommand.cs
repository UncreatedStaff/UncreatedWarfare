using System;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Structure = Uncreated.Warfare.Structures.Structure;

namespace Uncreated.Warfare.Commands
{
    public class StructureCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "structure";
        public string Help => "Managed saved structures.";
        public string Syntax => "/structure";
        public List<string> Aliases => new List<string>() { "struct" };
        public List<string> Permissions => new List<string>() { "uc.structure" };
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
                        Interactable barricade = UCBarricadeManager.GetInteractableFromLook<Interactable>(player.Player.look, RayMasks.BARRICADE | RayMasks.VEHICLE);
                        if (barricade == default)
                        {
                            Interactable2 barricade2 = UCBarricadeManager.GetInteractable2FromLook<Interactable2>(player.Player.look, RayMasks.BARRICADE);
                            if (barricade2 == default)
                            {
                                Interactable2 structure = UCBarricadeManager.GetInteractable2FromLook<Interactable2>(player.Player.look, RayMasks.STRUCTURE);
                                if (structure == default) player.SendChat("structure_not_looking");
                                else
                                {
                                    if (StructureManager.tryGetInfo(structure.transform, out _, out _, out ushort index, out StructureRegion region) && region != default)
                                    {
                                        if (!StructureSaver.StructureExists(region.drops[index].instanceID, EStructType.STRUCTURE, out Structure structexists))
                                        {
                                            if (StructureSaver.AddStructure(region.drops[index], region.structures[index], out Structure structureaded))
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
                                    }
                                }
                            }
                            else
                            {
                                if (BarricadeManager.tryGetInfo(barricade2.transform, out _, out _, out _, out ushort index, out BarricadeRegion region) && region != default)
                                {
                                    if (!StructureSaver.StructureExists(region.drops[index].instanceID, EStructType.BARRICADE, out Structure structureexists))
                                    {
                                        if (StructureSaver.AddStructure(region.drops[index], region.barricades[index], out Structure structureaded))
                                        {
                                            player.Player.SendChat("structure_saved", structureaded.Asset.itemName);
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
                            }
                        }
                        else
                        {
                            if (barricade is InteractableVehicle)
                            {
                                player.SendChat("structure_saved_not_vehicle");
                            }
                            else if (barricade is InteractableForage)
                            {
                                player.SendChat("structure_saved_not_bush");
                            }
                            else if (BarricadeManager.tryGetInfo(barricade.transform, out _, out _, out _, out ushort index, out BarricadeRegion region) && region != default)
                            {
                                if (!StructureSaver.StructureExists(region.drops[index].instanceID, EStructType.BARRICADE, out Structure structureexists))
                                {
                                    if (StructureSaver.AddStructure(region.drops[index], region.barricades[index], out Structure structureaded))
                                    {
                                        player.Player.SendChat("structure_saved", structureaded.Asset.itemName);
                                    } else
                                    {
                                        player.SendChat("structure_not_looking");
                                    }
                                } else
                                {
                                    player.SendChat("structure_saved_already",
                                        structureexists == default ? "unknown" : structureexists.Asset.itemName);
                                }
                            }
                            else player.SendChat("structure_not_looking");
                        }
                    }
                    else
                        player.Player.SendChat("no_permissions");
                    return;
                }
                else if (action == "remove")
                {
                    if (player.HasPermission("uc.structure.remove"))
                    {
                        Interactable barricade = UCBarricadeManager.GetInteractableFromLook<Interactable>(player.Player.look, RayMasks.BARRICADE | RayMasks.VEHICLE);
                        if (barricade == default)
                        {
                            Interactable2 barricade2 = UCBarricadeManager.GetInteractable2FromLook<Interactable2>(player.Player.look, RayMasks.BARRICADE);
                            if (barricade2 == default)
                            {
                                Interactable2 structure = UCBarricadeManager.GetInteractable2FromLook<Interactable2>(player.Player.look, RayMasks.STRUCTURE);
                                if (structure == default) player.SendChat("structure_not_looking");
                                else
                                {
                                    if (StructureManager.tryGetInfo(structure.transform, out _, out _, out ushort index, out StructureRegion region) && region != default)
                                    {
                                        if (StructureSaver.StructureExists(region.drops[index].instanceID, EStructType.STRUCTURE, out Structure structureaded))
                                        {
                                            StructureSaver.RemoveStructure(structureaded);
                                            player.Player.SendChat("structure_unsaved", structureaded.Asset.itemName);
                                        }
                                        else
                                        {
                                            string itemname;
                                            if (structure is Interactable2SalvageStructure str)
                                            {
                                                if (StructureManager.tryGetInfo(str.transform, out _, out _, out ushort index2, out StructureRegion region2))
                                                {
                                                    StructureData data = region2.structures[index2];
                                                    if (data != default) itemname = Assets.find(EAssetType.ITEM, data.structure.id) is ItemAsset iasset ? iasset.itemName : data.structure.id.ToString(Data.Locale);
                                                    else itemname = str.name;
                                                }
                                                else itemname = str.name;
                                            }
                                            else if (structure is Interactable2SalvageBarricade bar)
                                            {
                                                if (BarricadeManager.tryGetInfo(bar.transform, out _, out _, out _, out ushort index2, out BarricadeRegion region2))
                                                {
                                                    BarricadeData data = region2.barricades[index2];
                                                    if (data != default) itemname = Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset iasset ? iasset.itemName : data.barricade.id.ToString(Data.Locale);
                                                    else itemname = bar.name;
                                                }
                                                else itemname = bar.name;
                                            }
                                            else itemname = structure.name;
                                            player.SendChat("structure_unsaved_already",
                                                structureaded == default ? "unknown" : itemname);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (BarricadeManager.tryGetInfo(barricade2.transform, out _, out _, out _, out ushort index, out BarricadeRegion region) && region != default)
                                {
                                    if (StructureSaver.StructureExists(region.drops[index].instanceID, EStructType.BARRICADE, out Structure structureaded))
                                    {
                                        StructureSaver.RemoveStructure(structureaded);
                                        player.Player.SendChat("structure_unsaved", structureaded.Asset.itemName);
                                    }
                                    else
                                    {
                                        string itemname;
                                        if (BarricadeManager.tryGetInfo(barricade2.transform, out _, out _, out _, out ushort index2, out BarricadeRegion region2))
                                        {
                                            BarricadeData data = region2.barricades[index2];
                                            if (data != default) itemname = Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset iasset ? iasset.itemName : data.barricade.id.ToString(Data.Locale);
                                            else itemname = barricade2.name;
                                        }
                                        else itemname = barricade2.name;
                                        player.SendChat("structure_unsaved_already",
                                            structureaded == default ? "unknown" : itemname);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (BarricadeManager.tryGetInfo(barricade.transform, out _, out _, out _, out ushort index, out BarricadeRegion region) && region != default)
                            {
                                if (barricade is InteractableVehicle)
                                {
                                    player.SendChat("structure_unsaved_not_vehicle");
                                }
                                else if (barricade is InteractableForage)
                                {
                                    player.SendChat("structure_unsaved_not_bush");
                                }
                                else
                                {
                                    if (StructureSaver.StructureExists(region.drops[index].instanceID, EStructType.BARRICADE, out Structure structureaded))
                                    {
                                        StructureSaver.RemoveStructure(structureaded);
                                        player.Player.SendChat("structure_unsaved", structureaded.Asset.itemName);
                                    }
                                    else
                                    {
                                        string itemname;
                                        if (BarricadeManager.tryGetInfo(barricade.transform, out _, out _, out _, out ushort index2, out BarricadeRegion region2))
                                        {
                                            BarricadeData data = region2.barricades[index2];
                                            if (data != default) itemname = Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset iasset ? iasset.itemName : data.barricade.id.ToString(Data.Locale);
                                            else itemname = barricade.name;
                                        }
                                        else itemname = barricade.name;
                                        player.SendChat("structure_unsaved_already",
                                            structureaded == default ? "unknown" : itemname);
                                    }
                                }
                            }
                        }
                    }
                    else
                        player.Player.SendChat("no_permissions");
                } else if (action == "pop" || action == "destroy")
                {
                    if (player.HasPermission("uc.structure.pop"))
                    {
                        Interactable i = UCBarricadeManager.GetInteractableFromLook<Interactable>(player.Player.look, RayMasks.BARRICADE | RayMasks.VEHICLE);
                        if (i != default)
                        {
                            if (i is InteractableVehicle veh)
                            {
                                DestroyVehicle(veh, player.Player);
                            }
                            else if (!(i is InteractableForage || i is InteractableObject))
                            {
                                DestroyBarricade(i, player.Player);
                            }
                            else
                            {
                                player.Player.SendChat("structure_pop_not_poppable");
                            }
                        } else
                        {
                            Interactable2 i2 = UCBarricadeManager.GetInteractable2FromLook<Interactable2>(player.Player.look, RayMasks.STRUCTURE | RayMasks.BARRICADE);
                            if(i2 != default)
                            {
                                if (i2 is Interactable2SalvageBarricade)
                                {
                                    DestroyBarricade(i2, player.Player);
                                }
                                else if (i2 is Interactable2SalvageStructure)
                                {
                                    DestroyStructure(i2, player.Player);
                                }
                                else
                                {
                                    player.Player.SendChat("structure_pop_not_poppable");
                                }
                            }
                            else player.SendChat("structure_not_looking");
                        }
                    }
                    else player.Player.SendChat("no_permissions");
                } else if (action == "examine" || action == "wtf")
                {
                    if (player.HasPermission("uc.structure.examine"))
                    {
                        Interactable i = UCBarricadeManager.GetInteractableFromLook<Interactable>(player.Player.look, RayMasks.BARRICADE | RayMasks.VEHICLE);
                        if (i != default)
                        {
                            if (i is InteractableVehicle veh)
                            {
                                ExamineVehicle(veh, player.Player, true);
                            }
                            else if (i is InteractableTrap)
                            {
                                ExamineTrap(i.transform, player.Player, true);
                            }
                            else if (!(i is InteractableForage || i is InteractableObject))
                            {
                                ExamineBarricade(i, player.Player, true);
                            }
                            else
                            {
                                player.Player.SendChat("structure_examine_not_examinable");
                            }
                        }
                        else
                        {
                            Interactable2 i2 = UCBarricadeManager.GetInteractable2FromLook<Interactable2>(player.Player.look, RayMasks.STRUCTURE | RayMasks.STRUCTURE);
                            if (i2 != default)
                            {
                                if (i2 is Interactable2SalvageBarricade)
                                {
                                    ExamineBarricade(i2, player.Player, true);
                                }
                                else if (i2 is Interactable2SalvageStructure)
                                {
                                    ExamineStructure(i2, player.Player, true);
                                }
                                else
                                {
                                    player.Player.SendChat("structure_examine_not_examinable");
                                }
                            }
                            else player.SendChat("structure_not_looking");
                        }
                    }
                    else player.Player.SendChat("no_permissions");
                }
            }
        }
        private void DestroyBarricade(UnityEngine.MonoBehaviour i, Player player)
        {
            if (BarricadeManager.tryGetInfo(i.transform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
            {
                BarricadeData data = region.barricades[index];
                player.SendChat("structure_popped",
                    Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset asset ? asset.itemName : data.barricade.id.ToString(Data.Locale));
                BarricadeManager.destroyBarricade(region, x, y, plant, index);
            }
            else
            {
                player.SendChat("structure_pop_not_poppable");
            }
        }
        private void DestroyStructure(UnityEngine.MonoBehaviour i, Player player)
        {
            if (StructureManager.tryGetInfo(i.transform, out byte x, out byte y, out ushort index, out StructureRegion region))
            {
                StructureData data = region.structures[index];
                player.SendChat("structure_popped",
                    Assets.find(EAssetType.ITEM, data.structure.id) is ItemAsset asset ? asset.itemName : data.structure.id.ToString(Data.Locale));
                StructureManager.destroyStructure(region, x, y, index, i.transform.position);
            }
            else
            {
                player.SendChat("structure_pop_not_poppable");
            }
        }
        private void DestroyVehicle(InteractableVehicle vehicle, Player player)
        {
            vehicle.forceRemoveAllPlayers();
            VehicleManager.askVehicleDestroy(vehicle);
            player.SendChat("structure_popped",
                Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale));
            if (Vehicles.VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out Vehicles.VehicleSpawn spawn))
                spawn.StartVehicleRespawnTimer();
        }
        private void ExamineVehicle(InteractableVehicle vehicle, Player player, bool sendurl)
        {
            if(vehicle.lockedOwner == default || vehicle.lockedOwner == Steamworks.CSteamID.Nil)
            {
                player.SendChat("structure_examine_not_locked");
            } else
            {
                ulong team = F.GetTeamFromPlayerSteam64ID(vehicle.lockedOwner.m_SteamID);
                string teamname = TeamManager.TranslateName(team, player);
                if(sendurl)
                {
                    player.channel.owner.SendSteamURL(F.Translate("structure_last_owner_web_prompt", player, out _,
                        Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID).CharacterName, teamname), vehicle.lockedOwner.m_SteamID);
                } else
                {
                    string teamcolor = TeamManager.GetTeamHexColor(team);
                    player.SendChat("structure_last_owner_chat",
                        Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(vehicle.lockedOwner.m_SteamID).CharacterName,
                        vehicle.lockedOwner.m_SteamID.ToString(Data.Locale), teamcolor, teamname, teamcolor);
                }
            }
        }
        private void ExamineBarricade(UnityEngine.MonoBehaviour i, Player player, bool sendurl)
        {
            if (BarricadeManager.tryGetInfo(i.transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
            {
                BarricadeData data = region.barricades[index]; 
                if(data.owner == default || data.owner == 0)
                {
                    player.SendChat("structure_examine_not_examinable");
                    return;
                }
                string teamname = TeamManager.TranslateName(data.group, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(F.Translate("structure_last_owner_web_prompt", player, out _,
                        Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset asset ? asset.itemName : data.barricade.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(data.owner).CharacterName, teamname), data.owner);
                }
                else
                {
                    player.SendChat("structure_last_owner_chat",
                        Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset asset ? asset.itemName : data.barricade.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(data.owner).CharacterName,
                        data.owner.ToString(Data.Locale), TeamManager.GetTeamHexColor(F.GetTeamFromPlayerSteam64ID(data.owner)), 
                        teamname, TeamManager.GetTeamHexColor(F.GetTeam(data.group)));
                }
            }
            else
            {
                player.SendChat("structure_examine_not_examinable");
            }
        }
        private void ExamineStructure(UnityEngine.MonoBehaviour i, Player player, bool sendurl)
        {
            if (StructureManager.tryGetInfo(i.transform, out _, out _, out ushort index, out StructureRegion region))
            {
                StructureData data = region.structures[index];
                if (data.owner == default || data.owner == 0)
                {
                    player.SendChat("structure_examine_not_examinable");
                    return;
                }
                string teamname = TeamManager.TranslateName(data.group, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(F.Translate("structure_last_owner_web_prompt", player, out _,
                        Assets.find(EAssetType.ITEM, data.structure.id) is ItemAsset asset ? asset.itemName : data.structure.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(data.owner).CharacterName, teamname), data.owner);
                }
                else
                {
                    player.SendChat("structure_last_owner_chat",
                        Assets.find(EAssetType.ITEM, data.structure.id) is ItemAsset asset ? asset.itemName : data.structure.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(data.owner).CharacterName,
                        data.owner.ToString(Data.Locale), TeamManager.GetTeamHexColor(F.GetTeamFromPlayerSteam64ID(data.owner)),
                        teamname, TeamManager.GetTeamHexColor(F.GetTeam(data.group)));
                }
            }
            else
            {
                player.SendChat("structure_examine_not_examinable");
            }
        }
        private void ExamineTrap(UnityEngine.Transform i, Player player, bool sendurl)
        {
            if(i.TryGetComponent(out Components.BarricadeOwnerDataComponent data))
            {
                if (data.ownerID == default || data.ownerID == 0)
                {
                    player.SendChat("structure_examine_not_examinable");
                    return;
                }
                string teamname = TeamManager.TranslateName(data.group, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(F.Translate("structure_last_owner_web_prompt", player, out _,
                        Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset asset ? asset.itemName : data.barricade.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(data.ownerID).CharacterName, teamname), data.ownerID);
                }
                else
                {
                    player.SendChat("structure_last_owner_chat",
                        Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset asset ? asset.itemName : data.barricade.id.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(data.ownerID).CharacterName,
                        data.owner == null ? data.ownerID.ToString(Data.Locale) : F.GetPlayerOriginalNames(data.owner).PlayerName, 
                        TeamManager.GetTeamHexColor(F.GetTeamFromPlayerSteam64ID(data.ownerID)),
                        teamname, TeamManager.GetTeamHexColor(F.GetTeam(data.group)));
                }
            }
            else
            {
                player.SendChat("structure_examine_not_examinable");
            }
        }
    }
}
