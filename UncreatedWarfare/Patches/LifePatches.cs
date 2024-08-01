using HarmonyLib;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Uncreated.Warfare.Harmony;

public static partial class Patches
{
    [HarmonyPatch]
    public static class LifePatches
    {
        internal static bool IsSettingReputation = false;

        // SDG.Unturned.PlayerLife
        /// <summary>Prefix of <see cref="PlayerSkills.askRep(int)"/> to prevent vanilla player killed reputation calls.</summary>
        [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askRep))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool DisableVanillaKillReputation(PlayerSkills __instance, int rep)
        {
            CSteamID recentKiller = Data.GetRecentKiller == null ? new CSteamID(1) : Data.GetRecentKiller(__instance.player.life);
            // recent killer is set between death and when the player presses revive.
            return recentKiller == CSteamID.Nil || IsSettingReputation;
        }

        // SDG.Unturned.PlayerLife
        /// <summary>Prefix of <see cref="PlayerLife.askStarve(byte)"/> to prevent starving in main base.</summary>
        [HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.askStarve))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool OnPlayerFoodTick(byte amount, PlayerLife __instance) => !Data.Is<ITeams>() || !Teams.TeamManager.IsInAnyMainOrLobby(__instance.player);

        // SDG.Unturned.PlayerLife
        /// <summary>Prefix of <see cref="PlayerLife.askDehydrate(byte)"/> to prevent dehydrating in main base.</summary>
        [HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.askDehydrate))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool OnPlayerWaterTick(byte amount, PlayerLife __instance) => !Data.Is<ITeams>() || !Teams.TeamManager.IsInAnyMainOrLobby(__instance.player);

        // SDG.Unturned.PlayerLife
        /// <summary>Prefix of <see cref="PlayerLife.askTire(byte)"/> to prevent tiring in main base.</summary>
        [HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.askTire))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool OnPlayerTireTick(byte amount, PlayerLife __instance) => !Data.Is<ITeams>() || !Teams.TeamManager.IsInAnyMainOrLobby(__instance.player);

        // SDG.Unturned.PlayerLife
        /// <summary>
        /// Turn off bleeding for the real function.
        /// </summary>
        [HarmonyPatch(typeof(PlayerLife), "simulate")]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool SimulatePlayerLifePre(uint simulation, PlayerLife __instance, uint ___lastBleed, ref bool ____isBleeding, ref uint ___lastRegenerate)
        {
            if (!Data.Is(out IRevives r)) return true;
            if (Provider.isServer)
            {
                if (Level.info.type == ELevelType.SURVIVAL)
                {
                    if (__instance.isBleeding)
                    {
                        if (simulation - ___lastBleed > Provider.modeConfigData.Players.Bleed_Damage_Ticks)
                        {
                            if (r.ReviveManager != null && r.ReviveManager.IsInjured(__instance.player.channel.owner.playerID.steamID.m_SteamID))
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
        [UsedImplicitly]
        static void SimulatePlayerLifePost(uint simulation, PlayerLife __instance, ref uint ___lastBleed, ref bool ____isBleeding)
        {
            if (!Data.Is(out IRevives r)) return;
            if (Provider.isServer)
            {
                if (Level.info.type == ELevelType.SURVIVAL)
                {
                    if (!__instance.isBleeding)
                    {
                        if (simulation - ___lastBleed > Provider.modeConfigData.Players.Bleed_Damage_Ticks)
                        {
                            if (r.ReviveManager != null && r.ReviveManager.IsInjured(__instance.player.channel.owner.playerID.steamID.m_SteamID))
                            {
                                ___lastBleed = simulation;
                                ____isBleeding = true;
                                DamagePlayerParameters p = r.ReviveManager.GetParameters(__instance.player.channel.owner.playerID.steamID.m_SteamID);
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
        [UsedImplicitly]
        static void OnStopStoring(PlayerInventory __instance)
        {
            UCPlayer? player = UCPlayer.FromPlayer(__instance.player);
            if (player == null) return;
            if (player.StorageCoroutine != null)
                player.Player.StopCoroutine(player.StorageCoroutine);
            return;
        }
    }
}
