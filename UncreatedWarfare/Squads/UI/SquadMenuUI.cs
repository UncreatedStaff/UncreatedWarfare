using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework.UI;
using SDG.Unturned;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Squads.UI;
public class SquadMenuUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Heading");
    public readonly UnturnedUIElement Lock = new UnturnedUIElement("Locked");

    // Other Squads List
    public readonly UnturnedUIElement OtherSquad_0 = new UnturnedUIElement("S0");
    public readonly UnturnedUIElement OtherSquad_1 = new UnturnedUIElement("S1");
    public readonly UnturnedUIElement OtherSquad_2 = new UnturnedUIElement("S2");
    public readonly UnturnedUIElement OtherSquad_3 = new UnturnedUIElement("S3");
    public readonly UnturnedUIElement OtherSquad_4 = new UnturnedUIElement("S4");
    public readonly UnturnedUIElement OtherSquad_5 = new UnturnedUIElement("S5");
    public readonly UnturnedUIElement OtherSquad_6 = new UnturnedUIElement("S6");
    public readonly UnturnedUIElement OtherSquad_7 = new UnturnedUIElement("S7");

    public readonly UnturnedLabel OtherSquad_0_Text = new UnturnedLabel("SN0");
    public readonly UnturnedLabel OtherSquad_1_Text = new UnturnedLabel("SN1");
    public readonly UnturnedLabel OtherSquad_2_Text = new UnturnedLabel("SN2");
    public readonly UnturnedLabel OtherSquad_3_Text = new UnturnedLabel("SN3");
    public readonly UnturnedLabel OtherSquad_4_Text = new UnturnedLabel("SN4");
    public readonly UnturnedLabel OtherSquad_5_Text = new UnturnedLabel("SN5");
    public readonly UnturnedLabel OtherSquad_6_Text = new UnturnedLabel("SN6");
    public readonly UnturnedLabel OtherSquad_7_Text = new UnturnedLabel("SN7");

    public readonly UnturnedUIElement[] OtherSquadParents;
    public readonly UnturnedLabel[] OtherSquadTexts;

    // Member List
    public readonly UnturnedUIElement Member_0 = new UnturnedUIElement("M0");
    public readonly UnturnedUIElement Member_1 = new UnturnedUIElement("M1");
    public readonly UnturnedUIElement Member_2 = new UnturnedUIElement("M2");
    public readonly UnturnedUIElement Member_3 = new UnturnedUIElement("M3");
    public readonly UnturnedUIElement Member_4 = new UnturnedUIElement("M4");
    public readonly UnturnedUIElement Member_5 = new UnturnedUIElement("M5");

    public readonly UnturnedLabel MemberName_0 = new UnturnedLabel("MN0");
    public readonly UnturnedLabel MemberName_1 = new UnturnedLabel("MN1");
    public readonly UnturnedLabel MemberName_2 = new UnturnedLabel("MN2");
    public readonly UnturnedLabel MemberName_3 = new UnturnedLabel("MN3");
    public readonly UnturnedLabel MemberName_4 = new UnturnedLabel("MN4");
    public readonly UnturnedLabel MemberName_5 = new UnturnedLabel("MN5");

    public readonly UnturnedLabel MemberIcon_0 = new UnturnedLabel("MI0");
    public readonly UnturnedLabel MemberIcon_1 = new UnturnedLabel("MI1");
    public readonly UnturnedLabel MemberIcon_2 = new UnturnedLabel("MI2");
    public readonly UnturnedLabel MemberIcon_3 = new UnturnedLabel("MI3");
    public readonly UnturnedLabel MemberIcon_4 = new UnturnedLabel("MI4");
    public readonly UnturnedLabel MemberIcon_5 = new UnturnedLabel("MI5");

    public readonly UnturnedUIElement[] MemberParents;
    public readonly UnturnedLabel[] MemberNames;
    public readonly UnturnedLabel[] MemberIcons;
    public SquadMenuUI() : base(12002, Gamemode.Config.UISquadMenu, true, false)
    {
        OtherSquadParents = new UnturnedUIElement[]
        {
            OtherSquad_0,
            OtherSquad_1,
            OtherSquad_2,
            OtherSquad_3,
            OtherSquad_4,
            OtherSquad_5,
            OtherSquad_6,
            OtherSquad_7,
        };
        OtherSquadTexts = new UnturnedLabel[]
        {
            OtherSquad_0_Text,
            OtherSquad_1_Text,
            OtherSquad_2_Text,
            OtherSquad_3_Text,
            OtherSquad_4_Text,
            OtherSquad_5_Text,
            OtherSquad_6_Text,
            OtherSquad_7_Text,
        };
        MemberParents = new UnturnedUIElement[]
        {
            Member_0,
            Member_1,
            Member_2,
            Member_3,
            Member_4,
            Member_5,
        };
        MemberNames = new UnturnedLabel[]
        {
            MemberName_0,
            MemberName_1,
            MemberName_2,
            MemberName_3,
            MemberName_4,
            MemberName_5,
        };
        MemberIcons = new UnturnedLabel[]
        {
            MemberIcon_0,
            MemberIcon_1,
            MemberIcon_2,
            MemberIcon_3,
            MemberIcon_4,
            MemberIcon_5,
        };
    }
}
