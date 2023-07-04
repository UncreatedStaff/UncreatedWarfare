using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;
public class FlagListUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Header");

    public readonly UnturnedUIElement Parent0 = new UnturnedUIElement("0");
    public readonly UnturnedUIElement Parent1 = new UnturnedUIElement("1");
    public readonly UnturnedUIElement Parent2 = new UnturnedUIElement("2");
    public readonly UnturnedUIElement Parent3 = new UnturnedUIElement("3");
    public readonly UnturnedUIElement Parent4 = new UnturnedUIElement("4");
    public readonly UnturnedUIElement Parent5 = new UnturnedUIElement("5");
    public readonly UnturnedUIElement Parent6 = new UnturnedUIElement("6");
    public readonly UnturnedUIElement Parent7 = new UnturnedUIElement("7");
    public readonly UnturnedUIElement Parent8 = new UnturnedUIElement("8");
    public readonly UnturnedUIElement Parent9 = new UnturnedUIElement("9");

    public readonly UnturnedLabel Name0 = new UnturnedLabel("N0");
    public readonly UnturnedLabel Name1 = new UnturnedLabel("N1");
    public readonly UnturnedLabel Name2 = new UnturnedLabel("N2");
    public readonly UnturnedLabel Name3 = new UnturnedLabel("N3");
    public readonly UnturnedLabel Name4 = new UnturnedLabel("N4");
    public readonly UnturnedLabel Name5 = new UnturnedLabel("N5");
    public readonly UnturnedLabel Name6 = new UnturnedLabel("N6");
    public readonly UnturnedLabel Name7 = new UnturnedLabel("N7");
    public readonly UnturnedLabel Name8 = new UnturnedLabel("N8");
    public readonly UnturnedLabel Name9 = new UnturnedLabel("N9");

    public readonly UnturnedLabel Icon0 = new UnturnedLabel("I0");
    public readonly UnturnedLabel Icon1 = new UnturnedLabel("I1");
    public readonly UnturnedLabel Icon2 = new UnturnedLabel("I2");
    public readonly UnturnedLabel Icon3 = new UnturnedLabel("I3");
    public readonly UnturnedLabel Icon4 = new UnturnedLabel("I4");
    public readonly UnturnedLabel Icon5 = new UnturnedLabel("I5");
    public readonly UnturnedLabel Icon6 = new UnturnedLabel("I6");
    public readonly UnturnedLabel Icon7 = new UnturnedLabel("I7");
    public readonly UnturnedLabel Icon8 = new UnturnedLabel("I8");
    public readonly UnturnedLabel Icon9 = new UnturnedLabel("I9");

    public readonly UnturnedUIElement[] Parents;
    public readonly UnturnedLabel[] Names;
    public readonly UnturnedLabel[] Icons;

    public FlagListUI() : base(Gamemode.Config.UIFlagList)
    {
        Parents = new UnturnedUIElement[]
        {
            Parent0,
            Parent1,
            Parent2,
            Parent3,
            Parent4,
            Parent5,
            Parent6,
            Parent7,
            Parent8,
            Parent9,
        };
        Names = new UnturnedLabel[]
        {
            Name0,
            Name1,
            Name2,
            Name3,
            Name4,
            Name5,
            Name6,
            Name7,
            Name8,
            Name9,
        };
        Icons = new UnturnedLabel[]
        {
            Icon0,
            Icon1,
            Icon2,
            Icon3,
            Icon4,
            Icon5,
            Icon6,
            Icon7,
            Icon8,
            Icon9,
        };
    }
}
