using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.UI;
public class NearbyResourceUI : UnturnedUI
{
    public readonly UnturnedLabel BuildLabel = new UnturnedLabel("Canvas/Image/Icon_Build/Build");
    public readonly UnturnedLabel AmmoLabel = new UnturnedLabel("Canvas/Image/Icon_Ammo/Ammo");

    public NearbyResourceUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FobResources")) { }
    public void SetValues(ITransportConnection c, int build, int ammo)
    {
        BuildLabel.SetText(c, build.ToString(Data.LocalLocale));
        AmmoLabel.SetText(c, ammo.ToString(Data.LocalLocale));
    }
}
