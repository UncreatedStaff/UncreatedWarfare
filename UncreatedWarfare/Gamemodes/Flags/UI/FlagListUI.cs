﻿using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;

[UnturnedUI(BasePath = "Canvas/Parent")]
public class FlagListUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Header");
    public readonly FlagListRow[] Rows = ElementPatterns.CreateArray<FlagListRow>("{0}", 0, to: 9);
    public FlagListUI() : base(Gamemode.Config.UIFlagList.AsAssetContainer()) { }
    public class FlagListRow
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("N{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("I{0}")]
        public UnturnedLabel Icon { get; set; }
    }
}
