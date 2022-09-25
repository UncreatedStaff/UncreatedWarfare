using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Squads.UI;
public class SquadListUI : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Header");

    public readonly UnturnedUIElement Squad_0 = new UnturnedUIElement("0");
    public readonly UnturnedUIElement Squad_1 = new UnturnedUIElement("1");
    public readonly UnturnedUIElement Squad_2 = new UnturnedUIElement("2");
    public readonly UnturnedUIElement Squad_3 = new UnturnedUIElement("3");
    public readonly UnturnedUIElement Squad_4 = new UnturnedUIElement("4");
    public readonly UnturnedUIElement Squad_5 = new UnturnedUIElement("5");
    public readonly UnturnedUIElement Squad_6 = new UnturnedUIElement("6");
    public readonly UnturnedUIElement Squad_7 = new UnturnedUIElement("7");

    public readonly UnturnedLabel SquadName_0 = new UnturnedLabel("N0");
    public readonly UnturnedLabel SquadName_1 = new UnturnedLabel("N1");
    public readonly UnturnedLabel SquadName_2 = new UnturnedLabel("N2");
    public readonly UnturnedLabel SquadName_3 = new UnturnedLabel("N3");
    public readonly UnturnedLabel SquadName_4 = new UnturnedLabel("N4");
    public readonly UnturnedLabel SquadName_5 = new UnturnedLabel("N5");
    public readonly UnturnedLabel SquadName_6 = new UnturnedLabel("N6");
    public readonly UnturnedLabel SquadName_7 = new UnturnedLabel("N7");

    public readonly UnturnedLabel SquadMemberCount_0 = new UnturnedLabel("M0");
    public readonly UnturnedLabel SquadMemberCount_1 = new UnturnedLabel("M1");
    public readonly UnturnedLabel SquadMemberCount_2 = new UnturnedLabel("M2");
    public readonly UnturnedLabel SquadMemberCount_3 = new UnturnedLabel("M3");
    public readonly UnturnedLabel SquadMemberCount_4 = new UnturnedLabel("M4");
    public readonly UnturnedLabel SquadMemberCount_5 = new UnturnedLabel("M5");
    public readonly UnturnedLabel SquadMemberCount_6 = new UnturnedLabel("M6");
    public readonly UnturnedLabel SquadMemberCount_7 = new UnturnedLabel("M7");

    public readonly UnturnedUIElement[] Squads;
    public readonly UnturnedLabel[] SquadNames;
    public readonly UnturnedLabel[] SquadMemberCounts;

    public SquadListUI() : base(12001, Gamemode.Config.UISquadList, true, false)
    {
        Squads = new UnturnedUIElement[]
        {
            Squad_0,
            Squad_1,
            Squad_2,
            Squad_3,
            Squad_4,
            Squad_5,
            Squad_6,
            Squad_7,
        };
        SquadNames = new UnturnedLabel[]
        {
            SquadName_0,
            SquadName_1,
            SquadName_2,
            SquadName_3,
            SquadName_4,
            SquadName_5,
            SquadName_6,
            SquadName_7,
        };
        SquadMemberCounts = new UnturnedLabel[]
        {
            SquadMemberCount_0,
            SquadMemberCount_1,
            SquadMemberCount_2,
            SquadMemberCount_3,
            SquadMemberCount_4,
            SquadMemberCount_5,
            SquadMemberCount_6,
            SquadMemberCount_7,
        };
    }
}
