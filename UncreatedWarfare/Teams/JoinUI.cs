using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Teams;
public class JoinUI : UnturnedUI
{
    public readonly UnturnedLabel Heading = new UnturnedLabel("Heading");

    public readonly UnturnedLabel Team1Name = new UnturnedLabel("Team1Name");
    public readonly UnturnedUIElement Team1Highlight = new UnturnedUIElement("Team1Highlight");
    public readonly UnturnedLabel Team1PlayerCount = new UnturnedLabel("Team1PlayerCount");
    public readonly UnturnedLabel Team1Select = new UnturnedLabel("Team1Select");
    public readonly UnturnedImage Team1Image = new UnturnedImage("Team1Image");

    public readonly UnturnedLabel Team2Name = new UnturnedLabel("Team2Name");
    public readonly UnturnedUIElement Team2Highlight = new UnturnedUIElement("Team2Highlight");
    public readonly UnturnedLabel Team2PlayerCount = new UnturnedLabel("Team2PlayerCount");
    public readonly UnturnedLabel Team2Select = new UnturnedLabel("Team2Select");
    public readonly UnturnedImage Team2Image = new UnturnedImage("Team2Image");

    public readonly UnturnedLabel ConfirmText = new UnturnedLabel("ConfirmText");

    public readonly UnturnedUIElement GameStartingParent = new UnturnedUIElement("GameStarting");
    public readonly UnturnedLabel GameStartingSeconds = new UnturnedLabel("GameStartingSeconds");
    public readonly UnturnedLabel GameStartingCircle = new UnturnedLabel("GameStartingCircleForeground");

    public readonly UnturnedButton Team1Button = new UnturnedButton("Team1Button");
    public readonly UnturnedButton Team2Button = new UnturnedButton("Team2Button");
    public readonly UnturnedButton ConfirmButton = new UnturnedButton("Confirm");
    public readonly UnturnedUIElement CloseButton = new UnturnedUIElement("X");


    public readonly UnturnedLabel Team1Player0 = new UnturnedLabel("T1P1");
    public readonly UnturnedLabel Team1Player1 = new UnturnedLabel("T1P2");
    public readonly UnturnedLabel Team1Player2 = new UnturnedLabel("T1P3");
    public readonly UnturnedLabel Team1Player3 = new UnturnedLabel("T1P4");
    public readonly UnturnedLabel Team1Player4 = new UnturnedLabel("T1P5");
    public readonly UnturnedLabel Team1Player5 = new UnturnedLabel("T1P6");
    public readonly UnturnedLabel Team1Player6 = new UnturnedLabel("T1P7");
    public readonly UnturnedLabel Team1Player7 = new UnturnedLabel("T1P8");
    public readonly UnturnedLabel Team1Player8 = new UnturnedLabel("T1P9");
    public readonly UnturnedLabel Team1Player9 = new UnturnedLabel("T1P10");
    public readonly UnturnedLabel Team1Player10 = new UnturnedLabel("T1P11");
    public readonly UnturnedLabel Team1Player11 = new UnturnedLabel("T1P12");
    public readonly UnturnedLabel Team1Player12 = new UnturnedLabel("T1P13");
    public readonly UnturnedLabel Team1Player13 = new UnturnedLabel("T1P14");
    public readonly UnturnedLabel Team1Player14 = new UnturnedLabel("T1P15");
    public readonly UnturnedLabel Team1Player15 = new UnturnedLabel("T1P16");
    public readonly UnturnedLabel Team1Player16 = new UnturnedLabel("T1P17");
    public readonly UnturnedLabel Team1Player17 = new UnturnedLabel("T1P18");
    public readonly UnturnedLabel Team1Player18 = new UnturnedLabel("T1P19");
    public readonly UnturnedLabel Team1Player19 = new UnturnedLabel("T1P20");
    public readonly UnturnedLabel Team1Player20 = new UnturnedLabel("T1P21");
    public readonly UnturnedLabel Team1Player21 = new UnturnedLabel("T1P22");
    public readonly UnturnedLabel Team1Player22 = new UnturnedLabel("T1P23");
    public readonly UnturnedLabel Team1Player23 = new UnturnedLabel("T1P24");
    public readonly UnturnedLabel Team1Player24 = new UnturnedLabel("T1P25");
    public readonly UnturnedLabel Team1Player25 = new UnturnedLabel("T1P26");
    public readonly UnturnedLabel Team1Player26 = new UnturnedLabel("T1P27");
    public readonly UnturnedLabel Team1Player27 = new UnturnedLabel("T1P28");
    public readonly UnturnedLabel Team1Player28 = new UnturnedLabel("T1P29");
    public readonly UnturnedLabel Team1Player29 = new UnturnedLabel("T1P30");
    public readonly UnturnedLabel Team1Player30 = new UnturnedLabel("T1P31");
    public readonly UnturnedLabel Team1Player31 = new UnturnedLabel("T1P32");

    public readonly UnturnedLabel[] Team1Players;

    public readonly UnturnedLabel Team2Player0 = new UnturnedLabel("T2P1");
    public readonly UnturnedLabel Team2Player1 = new UnturnedLabel("T2P2");
    public readonly UnturnedLabel Team2Player2 = new UnturnedLabel("T2P3");
    public readonly UnturnedLabel Team2Player3 = new UnturnedLabel("T2P4");
    public readonly UnturnedLabel Team2Player4 = new UnturnedLabel("T2P5");
    public readonly UnturnedLabel Team2Player5 = new UnturnedLabel("T2P6");
    public readonly UnturnedLabel Team2Player6 = new UnturnedLabel("T2P7");
    public readonly UnturnedLabel Team2Player7 = new UnturnedLabel("T2P8");
    public readonly UnturnedLabel Team2Player8 = new UnturnedLabel("T2P9");
    public readonly UnturnedLabel Team2Player9 = new UnturnedLabel("T2P10");
    public readonly UnturnedLabel Team2Player10 = new UnturnedLabel("T2P11");
    public readonly UnturnedLabel Team2Player11 = new UnturnedLabel("T2P12");
    public readonly UnturnedLabel Team2Player12 = new UnturnedLabel("T2P13");
    public readonly UnturnedLabel Team2Player13 = new UnturnedLabel("T2P14");
    public readonly UnturnedLabel Team2Player14 = new UnturnedLabel("T2P15");
    public readonly UnturnedLabel Team2Player15 = new UnturnedLabel("T2P16");
    public readonly UnturnedLabel Team2Player16 = new UnturnedLabel("T2P17");
    public readonly UnturnedLabel Team2Player17 = new UnturnedLabel("T2P18");
    public readonly UnturnedLabel Team2Player18 = new UnturnedLabel("T2P19");
    public readonly UnturnedLabel Team2Player19 = new UnturnedLabel("T2P20");
    public readonly UnturnedLabel Team2Player20 = new UnturnedLabel("T2P21");
    public readonly UnturnedLabel Team2Player21 = new UnturnedLabel("T2P22");
    public readonly UnturnedLabel Team2Player22 = new UnturnedLabel("T2P23");
    public readonly UnturnedLabel Team2Player23 = new UnturnedLabel("T2P24");
    public readonly UnturnedLabel Team2Player24 = new UnturnedLabel("T2P25");
    public readonly UnturnedLabel Team2Player25 = new UnturnedLabel("T2P26");
    public readonly UnturnedLabel Team2Player26 = new UnturnedLabel("T2P27");
    public readonly UnturnedLabel Team2Player27 = new UnturnedLabel("T2P28");
    public readonly UnturnedLabel Team2Player28 = new UnturnedLabel("T2P29");
    public readonly UnturnedLabel Team2Player29 = new UnturnedLabel("T2P30");
    public readonly UnturnedLabel Team2Player30 = new UnturnedLabel("T2P31");
    public readonly UnturnedLabel Team2Player31 = new UnturnedLabel("T2P32");

    public readonly UnturnedLabel[] Team2Players;
    public JoinUI() : base(29000, Gamemode.Config.UITeamSelector, true, false)
    {
        Team1Players = new UnturnedLabel[]
        {
            Team1Player0,
            Team1Player1,
            Team1Player2,
            Team1Player3,
            Team1Player4,
            Team1Player5,
            Team1Player6,
            Team1Player7,
            Team1Player8,
            Team1Player9,
            Team1Player10,
            Team1Player11,
            Team1Player12,
            Team1Player13,
            Team1Player14,
            Team1Player15,
            Team1Player16,
            Team1Player17,
            Team1Player18,
            Team1Player19,
            Team1Player20,
            Team1Player21,
            Team1Player22,
            Team1Player23,
            Team1Player24,
            Team1Player25,
            Team1Player26,
            Team1Player27,
            Team1Player28,
            Team1Player29,
            Team1Player30,
            Team1Player31
        };
        Team2Players = new UnturnedLabel[]
        {
            Team2Player0,
            Team2Player1,
            Team2Player2,
            Team2Player3,
            Team2Player4,
            Team2Player5,
            Team2Player6,
            Team2Player7,
            Team2Player8,
            Team2Player9,
            Team2Player10,
            Team2Player11,
            Team2Player12,
            Team2Player13,
            Team2Player14,
            Team2Player15,
            Team2Player16,
            Team2Player17,
            Team2Player18,
            Team2Player19,
            Team2Player20,
            Team2Player21,
            Team2Player22,
            Team2Player23,
            Team2Player24,
            Team2Player25,
            Team2Player26,
            Team2Player27,
            Team2Player28,
            Team2Player29,
            Team2Player30,
            Team2Player31
        };
    }
}
