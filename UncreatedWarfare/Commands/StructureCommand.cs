using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
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
                                    StructureDrop drop = StructureManager.FindStructureByRootTransform(structure.transform);
                                    if (drop != null)
                                    {
                                        if (!StructureSaver.StructureExists(drop.instanceID, EStructType.STRUCTURE, out Structure structexists))
                                        {
                                            if (StructureSaver.AddStructure(drop, drop.GetServersideData(), out Structure structureaded))
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
                                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricade2.transform);
                                if (drop != null)
                                {
                                    if (!StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out Structure structureexists))
                                    {
                                        if (StructureSaver.AddStructure(drop, drop.GetServersideData(), out Structure structureaded))
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
                            else
                            {
                                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricade.transform);
                                if (drop != null)
                                {
                                    if (!StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out Structure structureexists))
                                    {
                                        if (StructureSaver.AddStructure(drop, drop.GetServersideData(), out Structure structureaded))
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
                                    StructureDrop drop = StructureManager.FindStructureByRootTransform(structure.transform);
                                    if (drop != null)
                                    {
                                        if (StructureSaver.StructureExists(drop.instanceID, EStructType.STRUCTURE, out Structure structureaded))
                                        {
                                            StructureSaver.RemoveStructure(structureaded);
                                            player.Player.SendChat("structure_unsaved", structureaded.Asset.itemName);
                                        }
                                        else
                                        {
                                            string itemname;
                                            if (structure is Interactable2SalvageStructure str)
                                            {
                                                StructureData data = drop.GetServersideData();
                                                if (data != default) itemname = Assets.find(EAssetType.ITEM, data.structure.id) is ItemAsset iasset ? iasset.itemName : data.structure.id.ToString(Data.Locale);
                                                else itemname = str.name;
                                            }
                                            else if (structure is Interactable2SalvageBarricade bar)
                                            {
                                                BarricadeDrop bdrop = BarricadeManager.FindBarricadeByRootTransform(bar.transform);
                                                if (bdrop != null)
                                                {
                                                    BarricadeData data = bdrop.GetServersideData();
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
                                BarricadeDrop bdrop = BarricadeManager.FindBarricadeByRootTransform(barricade2.transform);
                                if (bdrop != null)
                                {
                                    if (StructureSaver.StructureExists(bdrop.instanceID, EStructType.BARRICADE, out Structure structureaded))
                                    {
                                        StructureSaver.RemoveStructure(structureaded);
                                        player.Player.SendChat("structure_unsaved", structureaded.Asset.itemName);
                                    }
                                    else
                                    {
                                        string itemname;
                                        BarricadeData data = bdrop.GetServersideData();
                                        if (data != default) itemname = Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset iasset ? iasset.itemName : data.barricade.id.ToString(Data.Locale);
                                        else itemname = barricade2.name;
                                        player.SendChat("structure_unsaved_already",
                                            structureaded == default ? "unknown" : itemname);
                                    }
                                }
                            }
                        }
                        else
                        {
                            BarricadeDrop bdrop = BarricadeManager.FindBarricadeByRootTransform(barricade.transform);
                            if (bdrop != null)
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
                                    if (StructureSaver.StructureExists(bdrop.instanceID, EStructType.BARRICADE, out Structure structureaded))
                                    {
                                        StructureSaver.RemoveStructure(structureaded);
                                        player.Player.SendChat("structure_unsaved", structureaded.Asset.itemName);
                                    }
                                    else
                                    {
                                        string itemname;
                                        BarricadeData data = bdrop.GetServersideData();
                                        if (data != default) itemname = Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset iasset ? iasset.itemName : data.barricade.id.ToString(Data.Locale);
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
                }
                else if (action == "pop" || action == "destroy")
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
                        }
                        else
                        {
                            Interactable2 i2 = UCBarricadeManager.GetInteractable2FromLook<Interactable2>(player.Player.look, RayMasks.STRUCTURE | RayMasks.BARRICADE);
                            if (i2 != default)
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
                }
                else if (action == "examine" || action == "wtf")
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
            BarricadeDrop bdrop = BarricadeManager.FindBarricadeByRootTransform(i.transform);
            if (bdrop != null && Regions.tryGetCoordinate(bdrop.model.position, out byte x, out byte y))
            {
                BarricadeData data = bdrop.GetServersideData();
                player.SendChat("structure_popped",
                    Assets.find(EAssetType.ITEM, data.barricade.id) is ItemAsset asset ? asset.itemName : data.barricade.id.ToString(Data.Locale));
                BarricadeManager.destroyBarricade(bdrop, x, y, ushort.MaxValue);
            }
            else
            {
                player.SendChat("structure_pop_not_poppable");
            }
        }
        private void DestroyStructure(UnityEngine.MonoBehaviour i, Player player)
        {
            StructureDrop sdrop = StructureManager.FindStructureByRootTransform(i.transform);
            if (sdrop != null && Regions.tryGetCoordinate(sdrop.model.position, out byte x, out byte y))
            {
                StructureData data = sdrop.GetServersideData();
                player.SendChat("structure_popped",
                    Assets.find(EAssetType.ITEM, data.structure.id) is ItemAsset asset ? asset.itemName : data.structure.id.ToString(Data.Locale));
                StructureManager.destroyStructure(sdrop, x, y, UnityEngine.Vector3.down);
            }
            else
            {
                player.SendChat("structure_pop_not_poppable");
            }
        }
        private void DestroyVehicle(InteractableVehicle vehicle, Player player)
        {
            vehicle.forceRemoveAllPlayers();
            BarricadeRegion reg = BarricadeManager.getRegionFromVehicle(vehicle);
            if (reg != null)
                for (int b = 0; b < reg.drops.Count; b++)
                {
                    if (reg.drops[b].interactable is InteractableStorage storage)
                    {
                        storage.despawnWhenDestroyed = true;
                    }
                }
            VehicleManager.askVehicleDestroy(vehicle);
            player.SendChat("structure_popped",
                Assets.find(EAssetType.VEHICLE, vehicle.id) is VehicleAsset asset ? asset.vehicleName : vehicle.id.ToString(Data.Locale));
            if (Vehicles.VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out Vehicles.VehicleSpawn spawn))
                spawn.StartVehicleRespawnTimer();
        }
        private void ExamineVehicle(InteractableVehicle vehicle, Player player, bool sendurl)
        {
            if (vehicle.lockedOwner == default || vehicle.lockedOwner == Steamworks.CSteamID.Nil)
            {
                player.SendChat("structure_examine_not_locked");
            }
            else
            {
                ulong team = F.GetTeamFromPlayerSteam64ID(vehicle.lockedOwner.m_SteamID);
                string teamname = TeamManager.TranslateName(team, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(F.Translate("structure_last_owner_web_prompt", player, out _,
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
        }
        private void ExamineBarricade(UnityEngine.MonoBehaviour i, Player player, bool sendurl)
        {
            BarricadeDrop bdrop = BarricadeManager.FindBarricadeByRootTransform(i.transform);
            if (bdrop != null)
            {
                BarricadeData data = bdrop.GetServersideData();
                if (data.owner == default || data.owner == 0)
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
            StructureDrop sdrop = StructureManager.FindStructureByRootTransform(i.transform);
            if (sdrop != null)
            {
                StructureData data = sdrop.GetServersideData();
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
            if (i.TryGetComponent(out Components.BarricadeComponent data))
            {
                if (data.Owner == 0)
                {
                    player.SendChat("structure_examine_not_examinable");
                    return;
                }
                SteamPlayer owner = PlayerTool.getSteamPlayer(data.Owner);
                ulong team = owner == null ? (PlayerManager.HasSave(data.Owner, out PlayerSave save) ? save.Team : 0) : owner.GetTeam();
                string teamname = TeamManager.TranslateName(team, player);
                if (sendurl)
                {
                    player.channel.owner.SendSteamURL(F.Translate("structure_last_owner_web_prompt", player, out _,
                        Assets.find(EAssetType.ITEM, data.BarricadeID) is ItemAsset asset ? asset.itemName : data.BarricadeID.ToString(Data.Locale),
                        F.GetPlayerOriginalNames(data.Owner).CharacterName, teamname), data.Owner);
                }
                else
                {
                    Players.FPlayerName ownerName = F.GetPlayerOriginalNames(data.Owner);
                    player.SendChat("structure_last_owner_chat",
                        Assets.find(EAssetType.ITEM, data.BarricadeID) is ItemAsset asset ? asset.itemName : data.BarricadeID.ToString(Data.Locale),
                        ownerName.CharacterName,
                        ownerName.PlayerName,
                        TeamManager.GetTeamHexColor(team),
                        teamname, TeamManager.GetTeamHexColor(team));
                }
            }
            else
            {
                player.SendChat("structure_examine_not_examinable");
            }
        }
    }
}
