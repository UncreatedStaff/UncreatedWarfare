using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;
public class FlagListUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Canvas/Parent/Header");
    public readonly FlagListRow[] Rows = ElementPatterns.CreateArray<FlagListRow>("Canvas/Parent/{0}", 0, to: 9);
    public FlagListUI() : base(Gamemode.Config.UIFlagList.GetId()) { }
    public class FlagListRow
    {
        [Pattern("", Mode = FormatMode.Prefix)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("N{0}", AdditionalPath = "{0}", Mode = FormatMode.Replace)]
        public UnturnedLabel Name { get; set; }

        [Pattern("I{0}", AdditionalPath = "{0}", Mode = FormatMode.Replace)]
        public UnturnedLabel Icon { get; set; }
    }
}
