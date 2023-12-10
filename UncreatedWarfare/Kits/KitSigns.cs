using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Kits;
public class KitSigns(KitManager manager)
{
    public KitManager Manager { get; } = manager;

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns()
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(null));
    }
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(player));
    }
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(Kit kit)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, null));
    }
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(Kit kit, UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, player));
    }
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(string kitId)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kitId, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kitId, null));
    }
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(string kitId, UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kitId, player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kitId, player));
    }
    private static void UpdateSignsIntl(UCPlayer? player)
    {
        Signs.UpdateKitSigns(player, null);
    }
    private static void UpdateSignsIntl(Kit kit, UCPlayer? player)
    {
        if (kit.Type == KitType.Loadout)
        {
            Signs.UpdateLoadoutSigns(player);
        }
        else
        {
            Signs.UpdateKitSigns(player, kit.InternalName);
        }
    }
    private static void UpdateSignsIntl(string kitId, UCPlayer? player)
    {
        Signs.UpdateKitSigns(player, kitId);
    }
}
