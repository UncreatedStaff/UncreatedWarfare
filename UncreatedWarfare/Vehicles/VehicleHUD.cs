using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Vehicles;
public class VehicleHUD : UnturnedUI
{
    public readonly UnturnedLabel MissileWarning = new UnturnedLabel("VH_MissileWarning");
    public readonly UnturnedLabel MissileWarningDriver = new UnturnedLabel("VH_MissileWarningDriver");
    public readonly UnturnedLabel FlareCount = new UnturnedLabel("VH_FlareCount");

    public VehicleHUD() : base(Gamemode.Config.UIVehicleHUD)
    {
        
    }
}
