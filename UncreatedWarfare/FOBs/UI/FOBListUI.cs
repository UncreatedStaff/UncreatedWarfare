using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.UI;
public class FobListUI : UnturnedUI
{
    public readonly FobListElement[] FOBs = ElementPatterns.CreateArray<FobListElement>("Canvas/{0}", 0, to: 9);

    public FobListUI(AssetConfiguration assetConfig) : base(assetConfig.GetAssetLink<EffectAsset>("UI:FobList")) { }

    public class FobListElement
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("N{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("R{0}")]
        public UnturnedLabel Resources { get; set; }
    }
}