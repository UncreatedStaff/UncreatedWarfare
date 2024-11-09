namespace Uncreated.Warfare.Patches;

/// <summary>
/// All objects implementing this are created on startup and patched, then created on shutdown and unpatched.
/// </summary>
public interface IHarmonyPatch
{
    /// <summary>
    /// Apply the patch.
    /// </summary>
    void Patch(ILogger logger, HarmonyLib.Harmony patcher);

    /// <summary>
    /// Undo the patch.
    /// </summary>
    void Unpatch(ILogger logger, HarmonyLib.Harmony patcher);
}