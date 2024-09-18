using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Vehicles;

[UnturnedUI(BasePath = "Canvas")]
public class VehicleHUD : UnturnedUI
{
    public readonly UnturnedLabel MissileWarning = new UnturnedLabel("VH_MissileWarning");
    public readonly UnturnedLabel MissileWarningDriver = new UnturnedLabel("VH_MissileWarningDriver");
    public readonly UnturnedLabel FlareCount = new UnturnedLabel("VH_FlareCount");

    public VehicleHUD(AssetConfiguration assetConfig) : base(assetConfig.GetAssetLink<EffectAsset>("UI:VehicleHud"))
    {
        
    }
}
