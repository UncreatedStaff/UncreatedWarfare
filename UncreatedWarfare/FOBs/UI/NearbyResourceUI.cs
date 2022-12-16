using SDG.NetTransport;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.FOBs.UI;
public class NearbyResourceUI : UnturnedUI
{
    public readonly UnturnedLabel BuildLabel = new UnturnedLabel("Build");
    public readonly UnturnedLabel AmmoLabel = new UnturnedLabel("Ammo");

    public NearbyResourceUI() : base(12009, Gamemodes.Gamemode.Config.UINearbyResources, true, false)
    {

    }

    public void SetValues(ITransportConnection c, int build, int ammo)
    {
        BuildLabel.SetText(c, build.ToString(Data.LocalLocale));
        AmmoLabel.SetText(c, ammo.ToString(Data.LocalLocale));
    }
}
