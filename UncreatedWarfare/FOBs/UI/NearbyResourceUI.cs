using SDG.NetTransport;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.FOBs.UI;
public class NearbyResourceUI : UnturnedUI
{
    public readonly UnturnedLabel BuildLabel = new UnturnedLabel("Canvas/Image/Icon_Build/Build");
    public readonly UnturnedLabel AmmoLabel = new UnturnedLabel("Canvas/Image/Icon_Ammo/Ammo");

    public NearbyResourceUI() : base(Gamemodes.Gamemode.Config.UINearbyResources.GetId(), reliable: false) { }
    public void SetValues(ITransportConnection c, int build, int ammo)
    {
        BuildLabel.SetText(c, build.ToString(Data.LocalLocale));
        AmmoLabel.SetText(c, ammo.ToString(Data.LocalLocale));
    }
}
