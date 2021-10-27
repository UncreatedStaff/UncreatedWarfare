using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    internal static class EventHandlers
    {
        internal static TeamCTF Gamemode = null;
        internal static void OnStructureDestroyed(SDG.Unturned.StructureData data, StructureDrop drop, uint instanceID)
        {
            Gamemode.VehicleSpawner.OnStructureDestroyed(data, drop, instanceID);
        }
        internal static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            FOBManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            RallyManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            RepairManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            Gamemode.VehicleSpawner.OnBarricadeDestroyed(data, drop, instanceID, plant);
            Gamemode.VehicleSigns.OnBarricadeDestroyed(data, drop, instanceID, plant);
        }
        internal static void OnPostHealedPlayer(Player instigator, Player target)
        {
            Gamemode.ReviveManager.ClearInjuredMarker(instigator.channel.owner.playerID.steamID.m_SteamID, instigator.GetTeam());
            Gamemode.ReviveManager.OnPlayerHealed(instigator, target);
        }
        internal static void OnPluginKeyPressed(Player player, uint simulation, byte key, bool state)
        {
            if (state == false || key != 2 || player == null) return;
            Gamemode.ReviveManager.GiveUp(player);
        }
    }
}
