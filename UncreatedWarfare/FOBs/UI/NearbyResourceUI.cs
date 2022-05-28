using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.FOBs.UI;
public class NearbyResourceUI : UnturnedUI
{
    public readonly UnturnedLabel BuildLabel = new UnturnedLabel("Build");
    public readonly UnturnedLabel AmmoLabel = new UnturnedLabel("Ammo");

    public NearbyResourceUI() : base(12009, Gamemodes.Gamemode.Config.UI.NearbyResourcesGUID, true, false)
    {

    }

    public void SetValues(ITransportConnection c, int build, int ammo)
    {
        BuildLabel.SetText(c, build.ToString(Data.Locale));
        AmmoLabel.SetText(c, ammo.ToString(Data.Locale));
    }
}
