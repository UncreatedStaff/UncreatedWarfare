using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

namespace Uncreated.Warfare
{
    public static partial class Patches
    {
        [HarmonyPatch]
        public static class LifePatches
        {
            // SDG.Unturned.PlayerLife
            /// <summary>Prefix of <see cref="PlayerLife.askStarve(byte)"/> to invoke prevent starving in main base.</summary>
            [HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.askStarve))]
            [HarmonyPrefix]
            static bool OnPlayerFoodTick(byte amount, PlayerLife __instance) => !UCWarfare.Config.Patches.askStarve || !(Data.Gamemode is TeamGamemode) || !Teams.TeamManager.IsInMainOrLobby(__instance.player);

            // SDG.Unturned.PlayerLife
            /// <summary>Prefix of <see cref="PlayerLife.askDehydrate(byte)"/> to invoke prevent dehydrating in main base.</summary>
            [HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.askDehydrate))]
            [HarmonyPrefix]
            static bool OnPlayerWaterTick(byte amount, PlayerLife __instance) => !UCWarfare.Config.Patches.askDehydrate || !(Data.Gamemode is TeamGamemode) || !Teams.TeamManager.IsInMainOrLobby(__instance.player);

            // SDG.Unturned.PlayerLife
            /// <summary>
            /// Turn off bleeding for the real function.
            /// </summary>
            [HarmonyPatch(typeof(PlayerLife), "simulate")]
            [HarmonyPrefix]
            static bool SimulatePlayerLifePre(uint simulation, PlayerLife __instance, uint ___lastBleed, ref bool ____isBleeding, ref uint ___lastRegenerate)
            {
                if (!UCWarfare.Config.Patches.simulatePlayerLife) return true;
                if (!Data.TryMode(out TeamCTF ctf)) return true;
                if (Provider.isServer)
                {
                    if (Level.info.type == ELevelType.SURVIVAL)
                    {
                        if (__instance.isBleeding)
                        {
                            if (simulation - ___lastBleed > Provider.modeConfigData.Players.Bleed_Damage_Ticks)
                            {
                                if (ctf.ReviveManager != null && ctf.ReviveManager.DownedPlayers.ContainsKey(__instance.player.channel.owner.playerID.steamID.m_SteamID))
                                {
                                    ____isBleeding = false;
                                    ___lastRegenerate = simulation; // reset last regeneration to stop it from regenerating hp since it thinks the player isnt bleeding.
                                }
                            }
                        }
                    }
                }
                return true;
            }
            // SDG.Unturned.PlayerLife
            /// <summary>
            /// Turn back on bleeding and apply fix.
            /// </summary>
            [HarmonyPatch(typeof(PlayerLife), "simulate")]
            [HarmonyPostfix]
            static void SimulatePlayerLifePost(uint simulation, PlayerLife __instance, ref uint ___lastBleed, ref bool ____isBleeding)
            {
                if (!UCWarfare.Config.Patches.simulatePlayerLife) return;
                if (!Data.TryMode(out TeamCTF ctf)) return;
                if (Provider.isServer)
                {
                    if (Level.info.type == ELevelType.SURVIVAL)
                    {
                        if (!__instance.isBleeding)
                        {
                            if (simulation - ___lastBleed > Provider.modeConfigData.Players.Bleed_Damage_Ticks)
                            {
                                if (ctf.ReviveManager != null && ctf.ReviveManager.DownedPlayers.ContainsKey(__instance.player.channel.owner.playerID.steamID.m_SteamID))
                                {
                                    ___lastBleed = simulation;
                                    ____isBleeding = true;
                                    DamagePlayerParameters p = ctf.ReviveManager.DownedPlayers[__instance.player.channel.owner.playerID.steamID.m_SteamID];
                                    __instance.askDamage(1, p.direction, p.cause, p.limb, p.killer, out EPlayerKill _, canCauseBleeding: false, bypassSafezone: true);
                                }
                            }
                        }
                    }
                }
            }
            // SDG.Unturned.PlayerInventory
            /// <summary>
            /// Postfix of <see cref="PlayerInventory.closeStorage()"/> to stop the coroutine that auto-closes storages.
            /// </summary>
            [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.closeStorage))]
            [HarmonyPostfix]
            static void OnStopStoring(PlayerInventory __instance)
            {
                if (!UCWarfare.Config.Patches.closeStorage) return;
                UCPlayer player = UCPlayer.FromPlayer(__instance.player);
                if (player == null) return;
                if (player.StorageCoroutine != null)
                    player.Player.StopCoroutine(player.StorageCoroutine);
                return;
            }
        }
    }
}
