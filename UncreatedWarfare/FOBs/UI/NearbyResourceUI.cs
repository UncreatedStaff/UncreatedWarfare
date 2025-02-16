using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.FOBs.UI;
public class NearbyResourceUI : UnturnedUI
{
    public readonly UnturnedLabel BuildLabel = new UnturnedLabel("Canvas/Image/Icon_Build/Build");
    public readonly UnturnedLabel AmmoLabel = new UnturnedLabel("Canvas/Image/Icon_Ammo/Ammo");

    public NearbyResourceUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FobResources"), staticKey: true) { }
    
    public void SetValues(LanguageSet players, int build, int ammo)
    {
        string buildStr = build.ToString(players.Culture);
        string ammoStr = build.ToString(players.Culture);

        while (players.MoveNext())
        {
            ITransportConnection c = players.Next.Connection;
            BuildLabel.SetText(c, buildStr);
            AmmoLabel.SetText(c, ammoStr);
        }
    }
}
