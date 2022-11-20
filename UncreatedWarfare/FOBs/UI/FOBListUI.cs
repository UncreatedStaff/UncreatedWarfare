using Uncreated.Framework.UI;

namespace Uncreated.Warfare.FOBs.UI;
public class FOBListUI : UnturnedUI
{
    public readonly UnturnedUIElement FOBParent0 = new UnturnedUIElement("0");
    public readonly UnturnedUIElement FOBParent1 = new UnturnedUIElement("1");
    public readonly UnturnedUIElement FOBParent2 = new UnturnedUIElement("2");
    public readonly UnturnedUIElement FOBParent3 = new UnturnedUIElement("3");
    public readonly UnturnedUIElement FOBParent4 = new UnturnedUIElement("4");
    public readonly UnturnedUIElement FOBParent5 = new UnturnedUIElement("5");
    public readonly UnturnedUIElement FOBParent6 = new UnturnedUIElement("6");
    public readonly UnturnedUIElement FOBParent7 = new UnturnedUIElement("7");
    public readonly UnturnedUIElement FOBParent8 = new UnturnedUIElement("8");
    public readonly UnturnedUIElement FOBParent9 = new UnturnedUIElement("9");

    public readonly UnturnedLabel FOBName0 = new UnturnedLabel("N0");
    public readonly UnturnedLabel FOBName1 = new UnturnedLabel("N1");
    public readonly UnturnedLabel FOBName2 = new UnturnedLabel("N2");
    public readonly UnturnedLabel FOBName3 = new UnturnedLabel("N3");
    public readonly UnturnedLabel FOBName4 = new UnturnedLabel("N4");
    public readonly UnturnedLabel FOBName5 = new UnturnedLabel("N5");
    public readonly UnturnedLabel FOBName6 = new UnturnedLabel("N6");
    public readonly UnturnedLabel FOBName7 = new UnturnedLabel("N7");
    public readonly UnturnedLabel FOBName8 = new UnturnedLabel("N8");
    public readonly UnturnedLabel FOBName9 = new UnturnedLabel("N9");

    public readonly UnturnedLabel FOBResources0 = new UnturnedLabel("R0");
    public readonly UnturnedLabel FOBResources1 = new UnturnedLabel("R1");
    public readonly UnturnedLabel FOBResources2 = new UnturnedLabel("R2");
    public readonly UnturnedLabel FOBResources3 = new UnturnedLabel("R3");
    public readonly UnturnedLabel FOBResources4 = new UnturnedLabel("R4");
    public readonly UnturnedLabel FOBResources5 = new UnturnedLabel("R5");
    public readonly UnturnedLabel FOBResources6 = new UnturnedLabel("R6");
    public readonly UnturnedLabel FOBResources7 = new UnturnedLabel("R7");
    public readonly UnturnedLabel FOBResources8 = new UnturnedLabel("R8");
    public readonly UnturnedLabel FOBResources9 = new UnturnedLabel("R9");

    public readonly UnturnedUIElement[] FOBParents;
    public readonly UnturnedLabel[] FOBNames;
    public readonly UnturnedLabel[] FOBResources;
    public FOBListUI() : base(12008, Gamemodes.Gamemode.Config.UIFOBList, true, false)
    {
        FOBParents = new UnturnedUIElement[]
        {
            FOBParent0,
            FOBParent1,
            FOBParent2,
            FOBParent3,
            FOBParent4,
            FOBParent5,
            FOBParent6,
            FOBParent7,
            FOBParent8,
            FOBParent9
        };
        FOBNames = new UnturnedLabel[]
        {
            FOBName0,
            FOBName1,
            FOBName2,
            FOBName3,
            FOBName4,
            FOBName5,
            FOBName6,
            FOBName7,
            FOBName8,
            FOBName9
        };
        FOBResources = new UnturnedLabel[]
        {
            FOBResources0,
            FOBResources1,
            FOBResources2,
            FOBResources3,
            FOBResources4,
            FOBResources5,
            FOBResources6,
            FOBResources7,
            FOBResources8,
            FOBResources9
        };
    }
}
