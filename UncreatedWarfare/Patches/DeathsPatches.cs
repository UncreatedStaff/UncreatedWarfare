using HarmonyLib;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Uncreated.Warfare.Harmony;

public static partial class Patches
{
    [HarmonyPatch]
    public static class DeathsPatches
    {
        internal static GameObject? lastProjected;
#if false


        // SDG.Unturned.Bumper
        /// <summary>Adds the id of the vehicle that hit the player to their pt component.</summary>
        [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
        [HarmonyPatch(typeof(Bumper), "OnTriggerEnter")]
        [HarmonyPrefix]
        private static bool TriggerEnterBumper(Collider other, InteractableVehicle ___vehicle)
        {
            if (other == null || !Provider.isServer || ___vehicle == null || ___vehicle.asset == null || other.isTrigger || other.CompareTag("Debris"))
                return false;
            if (other.transform.CompareTag("Player"))
            {
                if (___vehicle.isDriven)
                {
                    Player driver = ___vehicle.passengers[0].player.player;
                    if (driver.TryGetPlayerData(out UCPlayerData c))
                    {
                        c.LastVehicleHitBy = ___vehicle.asset.GUID;
                    }
                    if (___vehicle.asset.engine != EEngine.HELICOPTER && ___vehicle.asset.engine != EEngine.PLANE)
                    {
                        Player hit = DamageTool.getPlayer(other.transform);
                        if (hit == null || driver == null || hit.movement.getVehicle() != null || !DamageTool.isPlayerAllowedToDamagePlayer(driver, hit)) return true;
                    }
                    else if (___vehicle.speed <= 10.0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
        [UsedImplicitly]
        private static void OnPreProject(UseableGun gun, ItemMagazineAsset magazine)
        {
            if (gun.player.TryGetPlayerData(out UCPlayerData data))
            {
                data.LastProjectedAmmoType = magazine;
            }
        }

        [HarmonyPatch(typeof(UseableGun))]
        [HarmonyPatch("fire")]
        internal static class ProjectTranspiler
        {
            private static readonly MethodInfo? _info = typeof(DeathsPatches).GetMethod("OnPreProject", BindingFlags.Static | BindingFlags.NonPublic);

            [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.IsStloc() && instruction.operand is LocalBuilder builder && builder.LocalType == typeof(ItemMagazineAsset))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc, builder);
                        yield return new CodeInstruction(OpCodes.Call, _info);
                    }
                }
            }
        }
#endif
    }
}
