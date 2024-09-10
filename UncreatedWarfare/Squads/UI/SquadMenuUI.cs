using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Squads.UI;

[UnturnedUI(BasePath = "Canvas")]
public class SquadMenuUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Heading");
    public readonly UnturnedUIElement Lock = new UnturnedUIElement("Locked");

    public readonly OtherSquad[] Squads = ElementPatterns.CreateArray<OtherSquad>("S{0}", 0, to: 7);
    public readonly SquadMember[] Members = ElementPatterns.CreateArray<SquadMember>("M{0}", 0, to: 5);
    public SquadMenuUI() : base(Gamemode.Config.UISquadMenu.AsAssetContainer()) { }
    public class OtherSquad
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("SN{0}")]
        public UnturnedLabel MemberCount { get; set; }
    }
    public class SquadMember
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("MN{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("MI{0}")]
        public UnturnedLabel Icon { get; set; }
    }
}
