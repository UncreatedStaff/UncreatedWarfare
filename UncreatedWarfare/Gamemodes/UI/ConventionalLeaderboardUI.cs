using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.UI;
public class ConventionalLeaderboardUI : UnturnedUI
{
    private const string STAT_TIME_FORMAT = "h\\:mm\\:ss";
    private const string STAT_FLOAT_FORMAT = "F0";
    private const string STAT_PRECISION_FLOAT_FORMAT = "0.##";
    public readonly UnturnedLabel Title = new UnturnedLabel("TitleWinner");
    public readonly UnturnedLabel Gamemode = new UnturnedLabel("TitleGamemode");

    public readonly UnturnedLabel NextGameStartLabel = new UnturnedLabel("NextGameStartsIn");
    public readonly UnturnedLabel NextGameSeconds = new UnturnedLabel("NextGameSeconds");
    public readonly UnturnedLabel NextGameSecondsCircle = new UnturnedLabel("NextGameCircleForeground");

    public readonly UnturnedLabel Team1Header0 = new UnturnedLabel("1Kills");
    public readonly UnturnedLabel Team1Header1 = new UnturnedLabel("1Deaths");
    public readonly UnturnedLabel Team1Header2 = new UnturnedLabel("1XP");
    public readonly UnturnedLabel Team1Header3 = new UnturnedLabel("1OFP");
    public readonly UnturnedLabel Team1Header4 = new UnturnedLabel("1Caps");
    public readonly UnturnedLabel Team1Header5 = new UnturnedLabel("1Damage");

    public readonly UnturnedLabel Team2Header0 = new UnturnedLabel("2Kills");
    public readonly UnturnedLabel Team2Header1 = new UnturnedLabel("2Deaths");
    public readonly UnturnedLabel Team2Header2 = new UnturnedLabel("2XP");
    public readonly UnturnedLabel Team2Header3 = new UnturnedLabel("2OFP");
    public readonly UnturnedLabel Team2Header4 = new UnturnedLabel("2Caps");
    public readonly UnturnedLabel Team2Header5 = new UnturnedLabel("2Damage");

    #region Player List
    public readonly UnturnedLabel Team1Name = new UnturnedLabel("1N0");
    public readonly UnturnedLabel Team1Kills = new UnturnedLabel("1K0");
    public readonly UnturnedLabel Team1Deaths = new UnturnedLabel("1D0");
    public readonly UnturnedLabel Team1XP = new UnturnedLabel("1X0");
    public readonly UnturnedLabel Team1Credits = new UnturnedLabel("1F0");
    public readonly UnturnedLabel Team1Captures = new UnturnedLabel("1C0");
    public readonly UnturnedLabel Team1Damage = new UnturnedLabel("1T0");

    public readonly UnturnedLabel Team1Player0Name = new UnturnedLabel("1N1");
    public readonly UnturnedLabel Team1Player0Kills = new UnturnedLabel("1K1");
    public readonly UnturnedLabel Team1Player0Deaths = new UnturnedLabel("1D1");
    public readonly UnturnedLabel Team1Player0XP = new UnturnedLabel("1X1");
    public readonly UnturnedLabel Team1Player0Credits = new UnturnedLabel("1F1");
    public readonly UnturnedLabel Team1Player0Captures = new UnturnedLabel("1C1");
    public readonly UnturnedLabel Team1Player0Damage = new UnturnedLabel("1T1");
    public readonly UnturnedUIElement Team1Player0VC = new UnturnedUIElement("1VC1");

    public readonly UnturnedLabel Team1Player1Name = new UnturnedLabel("1N2");
    public readonly UnturnedLabel Team1Player1Kills = new UnturnedLabel("1K2");
    public readonly UnturnedLabel Team1Player1Deaths = new UnturnedLabel("1D2");
    public readonly UnturnedLabel Team1Player1XP = new UnturnedLabel("1X2");
    public readonly UnturnedLabel Team1Player1Credits = new UnturnedLabel("1F2");
    public readonly UnturnedLabel Team1Player1Captures = new UnturnedLabel("1C2");
    public readonly UnturnedLabel Team1Player1Damage = new UnturnedLabel("1T2");
    public readonly UnturnedUIElement Team1Player1VC = new UnturnedUIElement("1VC2");

    public readonly UnturnedLabel Team1Player2Name = new UnturnedLabel("1N3");
    public readonly UnturnedLabel Team1Player2Kills = new UnturnedLabel("1K3");
    public readonly UnturnedLabel Team1Player2Deaths = new UnturnedLabel("1D3");
    public readonly UnturnedLabel Team1Player2XP = new UnturnedLabel("1X3");
    public readonly UnturnedLabel Team1Player2Credits = new UnturnedLabel("1F3");
    public readonly UnturnedLabel Team1Player2Captures = new UnturnedLabel("1C3");
    public readonly UnturnedLabel Team1Player2Damage = new UnturnedLabel("1T3");
    public readonly UnturnedUIElement Team1Player2VC = new UnturnedUIElement("1VC3");

    public readonly UnturnedLabel Team1Player3Name = new UnturnedLabel("1N4");
    public readonly UnturnedLabel Team1Player3Kills = new UnturnedLabel("1K4");
    public readonly UnturnedLabel Team1Player3Deaths = new UnturnedLabel("1D4");
    public readonly UnturnedLabel Team1Player3XP = new UnturnedLabel("1X4");
    public readonly UnturnedLabel Team1Player3Credits = new UnturnedLabel("1F4");
    public readonly UnturnedLabel Team1Player3Captures = new UnturnedLabel("1C4");
    public readonly UnturnedLabel Team1Player3Damage = new UnturnedLabel("1T4");
    public readonly UnturnedUIElement Team1Player3VC = new UnturnedUIElement("1VC4");

    public readonly UnturnedLabel Team1Player4Name = new UnturnedLabel("1N5");
    public readonly UnturnedLabel Team1Player4Kills = new UnturnedLabel("1K5");
    public readonly UnturnedLabel Team1Player4Deaths = new UnturnedLabel("1D5");
    public readonly UnturnedLabel Team1Player4XP = new UnturnedLabel("1X5");
    public readonly UnturnedLabel Team1Player4Credits = new UnturnedLabel("1F5");
    public readonly UnturnedLabel Team1Player4Captures = new UnturnedLabel("1C5");
    public readonly UnturnedLabel Team1Player4Damage = new UnturnedLabel("1T5");
    public readonly UnturnedUIElement Team1Player4VC = new UnturnedUIElement("1VC5");

    public readonly UnturnedLabel Team1Player5Name = new UnturnedLabel("1N6");
    public readonly UnturnedLabel Team1Player5Kills = new UnturnedLabel("1K6");
    public readonly UnturnedLabel Team1Player5Deaths = new UnturnedLabel("1D6");
    public readonly UnturnedLabel Team1Player5XP = new UnturnedLabel("1X6");
    public readonly UnturnedLabel Team1Player5Credits = new UnturnedLabel("1F6");
    public readonly UnturnedLabel Team1Player5Captures = new UnturnedLabel("1C6");
    public readonly UnturnedLabel Team1Player5Damage = new UnturnedLabel("1T6");
    public readonly UnturnedUIElement Team1Player5VC = new UnturnedUIElement("1VC6");

    public readonly UnturnedLabel Team1Player6Name = new UnturnedLabel("1N7");
    public readonly UnturnedLabel Team1Player6Kills = new UnturnedLabel("1K7");
    public readonly UnturnedLabel Team1Player6Deaths = new UnturnedLabel("1D7");
    public readonly UnturnedLabel Team1Player6XP = new UnturnedLabel("1X7");
    public readonly UnturnedLabel Team1Player6Credits = new UnturnedLabel("1F7");
    public readonly UnturnedLabel Team1Player6Captures = new UnturnedLabel("1C7");
    public readonly UnturnedLabel Team1Player6Damage = new UnturnedLabel("1T7");
    public readonly UnturnedUIElement Team1Player6VC = new UnturnedUIElement("1VC7");

    public readonly UnturnedLabel Team1Player7Name = new UnturnedLabel("1N8");
    public readonly UnturnedLabel Team1Player7Kills = new UnturnedLabel("1K8");
    public readonly UnturnedLabel Team1Player7Deaths = new UnturnedLabel("1D8");
    public readonly UnturnedLabel Team1Player7XP = new UnturnedLabel("1X8");
    public readonly UnturnedLabel Team1Player7Credits = new UnturnedLabel("1F8");
    public readonly UnturnedLabel Team1Player7Captures = new UnturnedLabel("1C8");
    public readonly UnturnedLabel Team1Player7Damage = new UnturnedLabel("1T8");
    public readonly UnturnedUIElement Team1Player7VC = new UnturnedUIElement("1VC8");

    public readonly UnturnedLabel Team1Player8Name = new UnturnedLabel("1N9");
    public readonly UnturnedLabel Team1Player8Kills = new UnturnedLabel("1K9");
    public readonly UnturnedLabel Team1Player8Deaths = new UnturnedLabel("1D9");
    public readonly UnturnedLabel Team1Player8XP = new UnturnedLabel("1X9");
    public readonly UnturnedLabel Team1Player8Credits = new UnturnedLabel("1F9");
    public readonly UnturnedLabel Team1Player8Captures = new UnturnedLabel("1C9");
    public readonly UnturnedLabel Team1Player8Damage = new UnturnedLabel("1T9");
    public readonly UnturnedUIElement Team1Player8VC = new UnturnedUIElement("1VC9");

    public readonly UnturnedLabel Team1Player9Name = new UnturnedLabel("1N10");
    public readonly UnturnedLabel Team1Player9Kills = new UnturnedLabel("1K10");
    public readonly UnturnedLabel Team1Player9Deaths = new UnturnedLabel("1D10");
    public readonly UnturnedLabel Team1Player9XP = new UnturnedLabel("1X10");
    public readonly UnturnedLabel Team1Player9Credits = new UnturnedLabel("1F10");
    public readonly UnturnedLabel Team1Player9Captures = new UnturnedLabel("1C10");
    public readonly UnturnedLabel Team1Player9Damage = new UnturnedLabel("1T10");
    public readonly UnturnedUIElement Team1Player9VC = new UnturnedUIElement("1VC10");

    public readonly UnturnedLabel Team1Player10Name = new UnturnedLabel("1N11");
    public readonly UnturnedLabel Team1Player10Kills = new UnturnedLabel("1K11");
    public readonly UnturnedLabel Team1Player10Deaths = new UnturnedLabel("1D11");
    public readonly UnturnedLabel Team1Player10XP = new UnturnedLabel("1X11");
    public readonly UnturnedLabel Team1Player10Credits = new UnturnedLabel("1F11");
    public readonly UnturnedLabel Team1Player10Captures = new UnturnedLabel("1C11");
    public readonly UnturnedLabel Team1Player10Damage = new UnturnedLabel("1T11");
    public readonly UnturnedUIElement Team1Player10VC = new UnturnedUIElement("1VC11");

    public readonly UnturnedLabel Team1Player11Name = new UnturnedLabel("1N12");
    public readonly UnturnedLabel Team1Player11Kills = new UnturnedLabel("1K12");
    public readonly UnturnedLabel Team1Player11Deaths = new UnturnedLabel("1D12");
    public readonly UnturnedLabel Team1Player11XP = new UnturnedLabel("1X12");
    public readonly UnturnedLabel Team1Player11Credits = new UnturnedLabel("1F12");
    public readonly UnturnedLabel Team1Player11Captures = new UnturnedLabel("1C12");
    public readonly UnturnedLabel Team1Player11Damage = new UnturnedLabel("1T12");
    public readonly UnturnedUIElement Team1Player11VC = new UnturnedUIElement("1VC12");

    public readonly UnturnedLabel Team1Player12Name = new UnturnedLabel("1N13");
    public readonly UnturnedLabel Team1Player12Kills = new UnturnedLabel("1K13");
    public readonly UnturnedLabel Team1Player12Deaths = new UnturnedLabel("1D13");
    public readonly UnturnedLabel Team1Player12XP = new UnturnedLabel("1X13");
    public readonly UnturnedLabel Team1Player12Credits = new UnturnedLabel("1F13");
    public readonly UnturnedLabel Team1Player12Captures = new UnturnedLabel("1C13");
    public readonly UnturnedLabel Team1Player12Damage = new UnturnedLabel("1T13");
    public readonly UnturnedUIElement Team1Player12VC = new UnturnedUIElement("1VC13");

    public readonly UnturnedLabel Team1Player13Name = new UnturnedLabel("1N14");
    public readonly UnturnedLabel Team1Player13Kills = new UnturnedLabel("1K14");
    public readonly UnturnedLabel Team1Player13Deaths = new UnturnedLabel("1D14");
    public readonly UnturnedLabel Team1Player13XP = new UnturnedLabel("1X14");
    public readonly UnturnedLabel Team1Player13Credits = new UnturnedLabel("1F14");
    public readonly UnturnedLabel Team1Player13Captures = new UnturnedLabel("1C14");
    public readonly UnturnedLabel Team1Player13Damage = new UnturnedLabel("1T14");
    public readonly UnturnedUIElement Team1Player13VC = new UnturnedUIElement("1VC14");

    public readonly UnturnedLabel Team2Name = new UnturnedLabel("2N0");
    public readonly UnturnedLabel Team2Kills = new UnturnedLabel("2K0");
    public readonly UnturnedLabel Team2Deaths = new UnturnedLabel("2D0");
    public readonly UnturnedLabel Team2XP = new UnturnedLabel("2X0");
    public readonly UnturnedLabel Team2Credits = new UnturnedLabel("2F0");
    public readonly UnturnedLabel Team2Captures = new UnturnedLabel("2C0");
    public readonly UnturnedLabel Team2Damage = new UnturnedLabel("2T0");

    public readonly UnturnedLabel Team2Player0Name = new UnturnedLabel("2N1");
    public readonly UnturnedLabel Team2Player0Kills = new UnturnedLabel("2K1");
    public readonly UnturnedLabel Team2Player0Deaths = new UnturnedLabel("2D1");
    public readonly UnturnedLabel Team2Player0XP = new UnturnedLabel("2X1");
    public readonly UnturnedLabel Team2Player0Credits = new UnturnedLabel("2F1");
    public readonly UnturnedLabel Team2Player0Captures = new UnturnedLabel("2C1");
    public readonly UnturnedLabel Team2Player0Damage = new UnturnedLabel("2T1");
    public readonly UnturnedUIElement Team2Player0VC = new UnturnedUIElement("2VC1");

    public readonly UnturnedLabel Team2Player1Name = new UnturnedLabel("2N2");
    public readonly UnturnedLabel Team2Player1Kills = new UnturnedLabel("2K2");
    public readonly UnturnedLabel Team2Player1Deaths = new UnturnedLabel("2D2");
    public readonly UnturnedLabel Team2Player1XP = new UnturnedLabel("2X2");
    public readonly UnturnedLabel Team2Player1Credits = new UnturnedLabel("2F2");
    public readonly UnturnedLabel Team2Player1Captures = new UnturnedLabel("2C2");
    public readonly UnturnedLabel Team2Player1Damage = new UnturnedLabel("2T2");
    public readonly UnturnedUIElement Team2Player1VC = new UnturnedUIElement("2VC2");

    public readonly UnturnedLabel Team2Player2Name = new UnturnedLabel("2N3");
    public readonly UnturnedLabel Team2Player2Kills = new UnturnedLabel("2K3");
    public readonly UnturnedLabel Team2Player2Deaths = new UnturnedLabel("2D3");
    public readonly UnturnedLabel Team2Player2XP = new UnturnedLabel("2X3");
    public readonly UnturnedLabel Team2Player2Credits = new UnturnedLabel("2F3");
    public readonly UnturnedLabel Team2Player2Captures = new UnturnedLabel("2C3");
    public readonly UnturnedLabel Team2Player2Damage = new UnturnedLabel("2T3");
    public readonly UnturnedUIElement Team2Player2VC = new UnturnedUIElement("2VC3");

    public readonly UnturnedLabel Team2Player3Name = new UnturnedLabel("2N4");
    public readonly UnturnedLabel Team2Player3Kills = new UnturnedLabel("2K4");
    public readonly UnturnedLabel Team2Player3Deaths = new UnturnedLabel("2D4");
    public readonly UnturnedLabel Team2Player3XP = new UnturnedLabel("2X4");
    public readonly UnturnedLabel Team2Player3Credits = new UnturnedLabel("2F4");
    public readonly UnturnedLabel Team2Player3Captures = new UnturnedLabel("2C4");
    public readonly UnturnedLabel Team2Player3Damage = new UnturnedLabel("2T4");
    public readonly UnturnedUIElement Team2Player3VC = new UnturnedUIElement("2VC4");

    public readonly UnturnedLabel Team2Player4Name = new UnturnedLabel("2N5");
    public readonly UnturnedLabel Team2Player4Kills = new UnturnedLabel("2K5");
    public readonly UnturnedLabel Team2Player4Deaths = new UnturnedLabel("2D5");
    public readonly UnturnedLabel Team2Player4XP = new UnturnedLabel("2X5");
    public readonly UnturnedLabel Team2Player4Credits = new UnturnedLabel("2F5");
    public readonly UnturnedLabel Team2Player4Captures = new UnturnedLabel("2C5");
    public readonly UnturnedLabel Team2Player4Damage = new UnturnedLabel("2T5");
    public readonly UnturnedUIElement Team2Player4VC = new UnturnedUIElement("2VC5");

    public readonly UnturnedLabel Team2Player5Name = new UnturnedLabel("2N6");
    public readonly UnturnedLabel Team2Player5Kills = new UnturnedLabel("2K6");
    public readonly UnturnedLabel Team2Player5Deaths = new UnturnedLabel("2D6");
    public readonly UnturnedLabel Team2Player5XP = new UnturnedLabel("2X6");
    public readonly UnturnedLabel Team2Player5Credits = new UnturnedLabel("2F6");
    public readonly UnturnedLabel Team2Player5Captures = new UnturnedLabel("2C6");
    public readonly UnturnedLabel Team2Player5Damage = new UnturnedLabel("2T6");
    public readonly UnturnedUIElement Team2Player5VC = new UnturnedUIElement("2VC6");

    public readonly UnturnedLabel Team2Player6Name = new UnturnedLabel("2N7");
    public readonly UnturnedLabel Team2Player6Kills = new UnturnedLabel("2K7");
    public readonly UnturnedLabel Team2Player6Deaths = new UnturnedLabel("2D7");
    public readonly UnturnedLabel Team2Player6XP = new UnturnedLabel("2X7");
    public readonly UnturnedLabel Team2Player6Credits = new UnturnedLabel("2F7");
    public readonly UnturnedLabel Team2Player6Captures = new UnturnedLabel("2C7");
    public readonly UnturnedLabel Team2Player6Damage = new UnturnedLabel("2T7");
    public readonly UnturnedUIElement Team2Player6VC = new UnturnedUIElement("2VC7");

    public readonly UnturnedLabel Team2Player7Name = new UnturnedLabel("2N8");
    public readonly UnturnedLabel Team2Player7Kills = new UnturnedLabel("2K8");
    public readonly UnturnedLabel Team2Player7Deaths = new UnturnedLabel("2D8");
    public readonly UnturnedLabel Team2Player7XP = new UnturnedLabel("2X8");
    public readonly UnturnedLabel Team2Player7Credits = new UnturnedLabel("2F8");
    public readonly UnturnedLabel Team2Player7Captures = new UnturnedLabel("2C8");
    public readonly UnturnedLabel Team2Player7Damage = new UnturnedLabel("2T8");
    public readonly UnturnedUIElement Team2Player7VC = new UnturnedUIElement("2VC8");

    public readonly UnturnedLabel Team2Player8Name = new UnturnedLabel("2N9");
    public readonly UnturnedLabel Team2Player8Kills = new UnturnedLabel("2K9");
    public readonly UnturnedLabel Team2Player8Deaths = new UnturnedLabel("2D9");
    public readonly UnturnedLabel Team2Player8XP = new UnturnedLabel("2X9");
    public readonly UnturnedLabel Team2Player8Credits = new UnturnedLabel("2F9");
    public readonly UnturnedLabel Team2Player8Captures = new UnturnedLabel("2C9");
    public readonly UnturnedLabel Team2Player8Damage = new UnturnedLabel("2T9");
    public readonly UnturnedUIElement Team2Player8VC = new UnturnedUIElement("2VC9");

    public readonly UnturnedLabel Team2Player9Name = new UnturnedLabel("2N10");
    public readonly UnturnedLabel Team2Player9Kills = new UnturnedLabel("2K10");
    public readonly UnturnedLabel Team2Player9Deaths = new UnturnedLabel("2D10");
    public readonly UnturnedLabel Team2Player9XP = new UnturnedLabel("2X10");
    public readonly UnturnedLabel Team2Player9Credits = new UnturnedLabel("2F10");
    public readonly UnturnedLabel Team2Player9Captures = new UnturnedLabel("2C10");
    public readonly UnturnedLabel Team2Player9Damage = new UnturnedLabel("2T10");
    public readonly UnturnedUIElement Team2Player9VC = new UnturnedUIElement("2VC10");

    public readonly UnturnedLabel Team2Player10Name = new UnturnedLabel("2N11");
    public readonly UnturnedLabel Team2Player10Kills = new UnturnedLabel("2K11");
    public readonly UnturnedLabel Team2Player10Deaths = new UnturnedLabel("2D11");
    public readonly UnturnedLabel Team2Player10XP = new UnturnedLabel("2X11");
    public readonly UnturnedLabel Team2Player10Credits = new UnturnedLabel("2F11");
    public readonly UnturnedLabel Team2Player10Captures = new UnturnedLabel("2C11");
    public readonly UnturnedLabel Team2Player10Damage = new UnturnedLabel("2T11");
    public readonly UnturnedUIElement Team2Player10VC = new UnturnedUIElement("2VC11");

    public readonly UnturnedLabel Team2Player11Name = new UnturnedLabel("2N12");
    public readonly UnturnedLabel Team2Player11Kills = new UnturnedLabel("2K12");
    public readonly UnturnedLabel Team2Player11Deaths = new UnturnedLabel("2D12");
    public readonly UnturnedLabel Team2Player11XP = new UnturnedLabel("2X12");
    public readonly UnturnedLabel Team2Player11Credits = new UnturnedLabel("2F12");
    public readonly UnturnedLabel Team2Player11Captures = new UnturnedLabel("2C12");
    public readonly UnturnedLabel Team2Player11Damage = new UnturnedLabel("2T12");
    public readonly UnturnedUIElement Team2Player11VC = new UnturnedUIElement("2VC12");

    public readonly UnturnedLabel Team2Player12Name = new UnturnedLabel("2N13");
    public readonly UnturnedLabel Team2Player12Kills = new UnturnedLabel("2K13");
    public readonly UnturnedLabel Team2Player12Deaths = new UnturnedLabel("2D13");
    public readonly UnturnedLabel Team2Player12XP = new UnturnedLabel("2X13");
    public readonly UnturnedLabel Team2Player12Credits = new UnturnedLabel("2F13");
    public readonly UnturnedLabel Team2Player12Captures = new UnturnedLabel("2C13");
    public readonly UnturnedLabel Team2Player12Damage = new UnturnedLabel("2T13");
    public readonly UnturnedUIElement Team2Player12VC = new UnturnedUIElement("2VC13");

    public readonly UnturnedLabel Team2Player13Name = new UnturnedLabel("2N14");
    public readonly UnturnedLabel Team2Player13Kills = new UnturnedLabel("2K14");
    public readonly UnturnedLabel Team2Player13Deaths = new UnturnedLabel("2D14");
    public readonly UnturnedLabel Team2Player13XP = new UnturnedLabel("2X14");
    public readonly UnturnedLabel Team2Player13Credits = new UnturnedLabel("2F14");
    public readonly UnturnedLabel Team2Player13Captures = new UnturnedLabel("2C14");
    public readonly UnturnedLabel Team2Player13Damage = new UnturnedLabel("2T14");
    public readonly UnturnedUIElement Team2Player13VC = new UnturnedUIElement("2VC14");
    #endregion

    #region War Stats
    public readonly UnturnedLabel TeamStatsHeader = new UnturnedLabel("WarHeader");

    public readonly UnturnedLabel TeamDurationLabel = new UnturnedLabel("lblDuration");
    public readonly UnturnedLabel TeamT1CasualtiesLabel = new UnturnedLabel("lblCasualtiesT1");
    public readonly UnturnedLabel TeamT2CasualtiesLabel = new UnturnedLabel("lblCasualtiesT2");
    public readonly UnturnedLabel TeamFlagCapturesLabel = new UnturnedLabel("lblOwnerChangedCount");
    public readonly UnturnedLabel TeamT1AveragePlayersLabel = new UnturnedLabel("lblAveragePlayerCountT1");
    public readonly UnturnedLabel TeamT2AveragePlayersLabel = new UnturnedLabel("lblAveragePlayerCountT2");

    public readonly UnturnedLabel TeamDuration = new UnturnedLabel("DurationValue");
    public readonly UnturnedLabel TeamT1Casualties = new UnturnedLabel("CasualtiesValueT1");
    public readonly UnturnedLabel TeamT2Casualties = new UnturnedLabel("CasualtiesValueT2");
    public readonly UnturnedLabel TeamFlagCaptures = new UnturnedLabel("FlagCapturesValue");
    public readonly UnturnedLabel TeamT1AveragePlayers = new UnturnedLabel("AveragePlayerCountsT1Value");
    public readonly UnturnedLabel TeamT2AveragePlayers = new UnturnedLabel("AveragePlayerCountsT2Value");

    public readonly UnturnedLabel TeamT1FOBsPlacedLabel = new UnturnedLabel("lblFOBsPlacedT1");
    public readonly UnturnedLabel TeamT2FOBsPlacedLabel = new UnturnedLabel("lblFOBsPlacedT2");
    public readonly UnturnedLabel TeamT1FOBsDestroyedLabel = new UnturnedLabel("lblFOBsDestroyedT1");
    public readonly UnturnedLabel TeamT2FOBsDestroyedLabel = new UnturnedLabel("lblFOBsDestroyedT2");
    public readonly UnturnedLabel TeamTeamkillsLabel = new UnturnedLabel("lblTeamkillingCasualties");
    public readonly UnturnedLabel TeamLongestShotLabel = new UnturnedLabel("lblTopRankingOfficer");

    public readonly UnturnedLabel TeamT1FOBsPlaced = new UnturnedLabel("FOBsPlacedT1Value");
    public readonly UnturnedLabel TeamT2FOBsPlaced = new UnturnedLabel("FOBsPlacedT2Value");
    public readonly UnturnedLabel TeamT1FOBsDestroyed = new UnturnedLabel("FOBsDestroyedT1Value");
    public readonly UnturnedLabel TeamT2FOBsDestroyed = new UnturnedLabel("FOBsDestroyedT2Value");
    public readonly UnturnedLabel TeamTeamkills = new UnturnedLabel("TeamkillingCasualtiesValue");
    public readonly UnturnedLabel TeamLongestShot = new UnturnedLabel("TopRankingOfficerValue");
    #endregion

    #region Personal Stats
    public readonly UnturnedLabel PlayerStatsHeader = new UnturnedLabel("playerstats_header");

    public readonly UnturnedLabel PersonalStats0Label = new UnturnedLabel("playerstats_0");
    public readonly UnturnedLabel PersonalStats1Label = new UnturnedLabel("playerstats_1");
    public readonly UnturnedLabel PersonalStats2Label = new UnturnedLabel("playerstats_2");
    public readonly UnturnedLabel PersonalStats3Label = new UnturnedLabel("playerstats_3");
    public readonly UnturnedLabel PersonalStats4Label = new UnturnedLabel("playerstats_4");
    public readonly UnturnedLabel PersonalStats5Label = new UnturnedLabel("playerstats_5");

    public readonly UnturnedLabel PersonalStats0 = new UnturnedLabel("playerstats_0_v");
    public readonly UnturnedLabel PersonalStats1 = new UnturnedLabel("playerstats_1_v");
    public readonly UnturnedLabel PersonalStats2 = new UnturnedLabel("playerstats_2_v");
    public readonly UnturnedLabel PersonalStats3 = new UnturnedLabel("playerstats_3_v");
    public readonly UnturnedLabel PersonalStats4 = new UnturnedLabel("playerstats_4_v");
    public readonly UnturnedLabel PersonalStats5 = new UnturnedLabel("playerstats_5_v");

    public readonly UnturnedLabel PersonalStats6Label = new UnturnedLabel("playerstats_6");
    public readonly UnturnedLabel PersonalStats7Label = new UnturnedLabel("playerstats_7");
    public readonly UnturnedLabel PersonalStats8Label = new UnturnedLabel("playerstats_8");
    public readonly UnturnedLabel PersonalStats9Label = new UnturnedLabel("playerstats_9");
    public readonly UnturnedLabel PersonalStats10Label = new UnturnedLabel("playerstats_10");
    public readonly UnturnedLabel PersonalStats11Label = new UnturnedLabel("playerstats_11");

    public readonly UnturnedLabel PersonalStats6 = new UnturnedLabel("playerstats_6_v");
    public readonly UnturnedLabel PersonalStats7 = new UnturnedLabel("playerstats_7_v");
    public readonly UnturnedLabel PersonalStats8 = new UnturnedLabel("playerstats_8_v");
    public readonly UnturnedLabel PersonalStats9 = new UnturnedLabel("playerstats_9_v");
    public readonly UnturnedLabel PersonalStats10 = new UnturnedLabel("playerstats_10_v");
    public readonly UnturnedLabel PersonalStats11 = new UnturnedLabel("playerstats_11_v");
    #endregion

    public readonly UnturnedLabel[] Team1PlayerNames;
    public readonly UnturnedLabel[] Team1PlayerKills;
    public readonly UnturnedLabel[] Team1PlayerDeaths;
    public readonly UnturnedLabel[] Team1PlayerXP;
    public readonly UnturnedLabel[] Team1PlayerCredits;
    public readonly UnturnedLabel[] Team1PlayerCaptures;
    public readonly UnturnedLabel[] Team1PlayerDamage;
    public readonly UnturnedUIElement[] Team1PlayerVCs;

    public readonly UnturnedLabel[] Team2PlayerNames;
    public readonly UnturnedLabel[] Team2PlayerKills;
    public readonly UnturnedLabel[] Team2PlayerDeaths;
    public readonly UnturnedLabel[] Team2PlayerXP;
    public readonly UnturnedLabel[] Team2PlayerCredits;
    public readonly UnturnedLabel[] Team2PlayerCaptures;
    public readonly UnturnedLabel[] Team2PlayerDamage;
    public readonly UnturnedUIElement[] Team2PlayerVCs;

    public ConventionalLeaderboardUI() : base(12007, Gamemodes.Gamemode.Config.UIConventionalLeaderboard, true, false)
    {
        Team1PlayerNames = new UnturnedLabel[]
        {
            Team1Player0Name,
            Team1Player1Name,
            Team1Player2Name,
            Team1Player3Name,
            Team1Player4Name,
            Team1Player5Name,
            Team1Player6Name,
            Team1Player7Name,
            Team1Player8Name,
            Team1Player9Name,
            Team1Player10Name,
            Team1Player11Name,
            Team1Player12Name,
            Team1Player13Name,
        };
        Team1PlayerKills = new UnturnedLabel[]
        {
            Team1Player0Kills,
            Team1Player1Kills,
            Team1Player2Kills,
            Team1Player3Kills,
            Team1Player4Kills,
            Team1Player5Kills,
            Team1Player6Kills,
            Team1Player7Kills,
            Team1Player8Kills,
            Team1Player9Kills,
            Team1Player10Kills,
            Team1Player11Kills,
            Team1Player12Kills,
            Team1Player13Kills,
        };
        Team1PlayerDeaths = new UnturnedLabel[]
        {
            Team1Player0Deaths,
            Team1Player1Deaths,
            Team1Player2Deaths,
            Team1Player3Deaths,
            Team1Player4Deaths,
            Team1Player5Deaths,
            Team1Player6Deaths,
            Team1Player7Deaths,
            Team1Player8Deaths,
            Team1Player9Deaths,
            Team1Player10Deaths,
            Team1Player11Deaths,
            Team1Player12Deaths,
            Team1Player13Deaths,
        };
        Team1PlayerXP = new UnturnedLabel[]
        {
            Team1Player0XP,
            Team1Player1XP,
            Team1Player2XP,
            Team1Player3XP,
            Team1Player4XP,
            Team1Player5XP,
            Team1Player6XP,
            Team1Player7XP,
            Team1Player8XP,
            Team1Player9XP,
            Team1Player10XP,
            Team1Player11XP,
            Team1Player12XP,
            Team1Player13XP,
        };
        Team1PlayerCredits = new UnturnedLabel[]
        {
            Team1Player0Credits,
            Team1Player1Credits,
            Team1Player2Credits,
            Team1Player3Credits,
            Team1Player4Credits,
            Team1Player5Credits,
            Team1Player6Credits,
            Team1Player7Credits,
            Team1Player8Credits,
            Team1Player9Credits,
            Team1Player10Credits,
            Team1Player11Credits,
            Team1Player12Credits,
            Team1Player13Credits,
        };
        Team1PlayerCaptures = new UnturnedLabel[]
        {
            Team1Player0Captures,
            Team1Player1Captures,
            Team1Player2Captures,
            Team1Player3Captures,
            Team1Player4Captures,
            Team1Player5Captures,
            Team1Player6Captures,
            Team1Player7Captures,
            Team1Player8Captures,
            Team1Player9Captures,
            Team1Player10Captures,
            Team1Player11Captures,
            Team1Player12Captures,
            Team1Player13Captures,
        };
        Team1PlayerDamage = new UnturnedLabel[]
        {
            Team1Player0Damage,
            Team1Player1Damage,
            Team1Player2Damage,
            Team1Player3Damage,
            Team1Player4Damage,
            Team1Player5Damage,
            Team1Player6Damage,
            Team1Player7Damage,
            Team1Player8Damage,
            Team1Player9Damage,
            Team1Player10Damage,
            Team1Player11Damage,
            Team1Player12Damage,
            Team1Player13Damage,
        };
        Team1PlayerVCs = new UnturnedUIElement[]
        {
            Team1Player0VC,
            Team1Player1VC,
            Team1Player2VC,
            Team1Player3VC,
            Team1Player4VC,
            Team1Player5VC,
            Team1Player6VC,
            Team1Player7VC,
            Team1Player8VC,
            Team1Player9VC,
            Team1Player10VC,
            Team1Player11VC,
            Team1Player12VC,
            Team1Player13VC,
        };
        Team2PlayerNames = new UnturnedLabel[]
        {
            Team2Player0Name,
            Team2Player1Name,
            Team2Player2Name,
            Team2Player3Name,
            Team2Player4Name,
            Team2Player5Name,
            Team2Player6Name,
            Team2Player7Name,
            Team2Player8Name,
            Team2Player9Name,
            Team2Player10Name,
            Team2Player11Name,
            Team2Player12Name,
            Team2Player13Name,
        };
        Team2PlayerKills = new UnturnedLabel[]
        {
            Team2Player0Kills,
            Team2Player1Kills,
            Team2Player2Kills,
            Team2Player3Kills,
            Team2Player4Kills,
            Team2Player5Kills,
            Team2Player6Kills,
            Team2Player7Kills,
            Team2Player8Kills,
            Team2Player9Kills,
            Team2Player10Kills,
            Team2Player11Kills,
            Team2Player12Kills,
            Team2Player13Kills,
        };
        Team2PlayerDeaths = new UnturnedLabel[]
        {
            Team2Player0Deaths,
            Team2Player1Deaths,
            Team2Player2Deaths,
            Team2Player3Deaths,
            Team2Player4Deaths,
            Team2Player5Deaths,
            Team2Player6Deaths,
            Team2Player7Deaths,
            Team2Player8Deaths,
            Team2Player9Deaths,
            Team2Player10Deaths,
            Team2Player11Deaths,
            Team2Player12Deaths,
            Team2Player13Deaths,
        };
        Team2PlayerXP = new UnturnedLabel[]
        {
            Team2Player0XP,
            Team2Player1XP,
            Team2Player2XP,
            Team2Player3XP,
            Team2Player4XP,
            Team2Player5XP,
            Team2Player6XP,
            Team2Player7XP,
            Team2Player8XP,
            Team2Player9XP,
            Team2Player10XP,
            Team2Player11XP,
            Team2Player12XP,
            Team2Player13XP,
        };
        Team2PlayerCredits = new UnturnedLabel[]
        {
            Team2Player0Credits,
            Team2Player1Credits,
            Team2Player2Credits,
            Team2Player3Credits,
            Team2Player4Credits,
            Team2Player5Credits,
            Team2Player6Credits,
            Team2Player7Credits,
            Team2Player8Credits,
            Team2Player9Credits,
            Team2Player10Credits,
            Team2Player11Credits,
            Team2Player12Credits,
            Team2Player13Credits,
        };
        Team2PlayerCaptures = new UnturnedLabel[]
        {
            Team2Player0Captures,
            Team2Player1Captures,
            Team2Player2Captures,
            Team2Player3Captures,
            Team2Player4Captures,
            Team2Player5Captures,
            Team2Player6Captures,
            Team2Player7Captures,
            Team2Player8Captures,
            Team2Player9Captures,
            Team2Player10Captures,
            Team2Player11Captures,
            Team2Player12Captures,
            Team2Player13Captures,
        };
        Team2PlayerDamage = new UnturnedLabel[]
        {
            Team2Player0Damage,
            Team2Player1Damage,
            Team2Player2Damage,
            Team2Player3Damage,
            Team2Player4Damage,
            Team2Player5Damage,
            Team2Player6Damage,
            Team2Player7Damage,
            Team2Player8Damage,
            Team2Player9Damage,
            Team2Player10Damage,
            Team2Player11Damage,
            Team2Player12Damage,
            Team2Player13Damage,
        };
        Team2PlayerVCs = new UnturnedUIElement[]
        {
            Team2Player0VC,
            Team2Player1VC,
            Team2Player2VC,
            Team2Player3VC,
            Team2Player4VC,
            Team2Player5VC,
            Team2Player6VC,
            Team2Player7VC,
            Team2Player8VC,
            Team2Player9VC,
            Team2Player10VC,
            Team2Player11VC,
            Team2Player12VC,
            Team2Player13VC,
        };
    }
    public void SendCTFLeaderboard<Stats, StatTracker>(LanguageSet set, in LongestShot info, List<Stats>? t1Stats, List<Stats>? t2Stats, StatTracker tracker, string? shutdownReason, ulong winner) where Stats : BaseCTFStats where StatTracker : BaseCTFTracker<Stats>
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FactionInfo t1 = TeamManager.GetFaction(1), t2 = TeamManager.GetFaction(2);
        string color = TeamManager.GetTeamHexColor(winner);
        string lang = set.Language;
        int len = 47;
        if (t1Stats is not null)
            len += Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1) * 7;
        if (t2Stats is not null)
            len += Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1) * 7;
        string[] values = new string[len];
        int secondsLeft = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);

        values[0] = T.WinnerTitle.Translate(lang, TeamManager.GetFactionSafe(winner)!);
        values[1] = shutdownReason is null ?
            T.StartingSoon.Translate(lang) :
            T.NextGameShutdown.Translate(lang, shutdownReason);

        values[2] = TimeSpan.FromSeconds(secondsLeft).ToString("mm\\:ss", Data.Locale);
        values[3] = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[0], 1);
        values[4] = T.WarstatsHeader.Translate(lang, TeamManager.GetFaction(1), TeamManager.GetFaction(2));

        values[5] = T.CTFWarStats0.Translate(lang);
        values[6] = T.CTFWarStats1.Translate(lang, t1);
        values[7] = T.CTFWarStats2.Translate(lang, t2);
        values[8] = T.CTFWarStats3.Translate(lang);
        values[9] = T.CTFWarStats4.Translate(lang, t1);
        values[10] = T.CTFWarStats5.Translate(lang, t2);
        values[11] = T.CTFWarStats6.Translate(lang, t1);
        values[12] = T.CTFWarStats7.Translate(lang, t2);
        values[13] = T.CTFWarStats8.Translate(lang, t1);
        values[14] = T.CTFWarStats9.Translate(lang, t2);
        values[15] = T.CTFWarStats10.Translate(lang);
        values[16] = T.CTFWarStats11.Translate(lang);

        values[17] = T.CTFPlayerStats0.Translate(lang);
        values[18] = T.CTFPlayerStats1.Translate(lang);
        values[19] = T.CTFPlayerStats2.Translate(lang);
        values[20] = T.CTFPlayerStats3.Translate(lang);
        values[21] = T.CTFPlayerStats4.Translate(lang);
        values[22] = T.CTFPlayerStats5.Translate(lang);
        values[23] = T.CTFPlayerStats6.Translate(lang);
        values[24] = T.CTFPlayerStats7.Translate(lang);
        values[25] = T.CTFPlayerStats8.Translate(lang);
        values[26] = T.CTFPlayerStats9.Translate(lang);
        values[27] = T.CTFPlayerStats10.Translate(lang);
        values[28] = T.CTFPlayerStats11.Translate(lang);

        values[41] = T.CTFHeader0.Translate(lang);
        values[42] = T.CTFHeader1.Translate(lang);
        values[43] = T.CTFHeader2.Translate(lang);
        values[44] = T.CTFHeader3.Translate(lang);
        values[45] = T.CTFHeader4.Translate(lang);
        values[46] = T.CTFHeader5.Translate(lang);

        if (tracker is not null)
        {
            values[29] = tracker.Duration.ToString(STAT_TIME_FORMAT, Data.Locale);
            values[30] = tracker.casualtiesT1.ToString(Data.Locale);
            values[31] = tracker.casualtiesT2.ToString(Data.Locale);
            values[32] = tracker.flagOwnerChanges.ToString(Data.Locale);
            values[33] = tracker.AverageTeam1Size.ToString(STAT_FLOAT_FORMAT, Data.Locale);
            values[34] = tracker.AverageTeam2Size.ToString(STAT_FLOAT_FORMAT, Data.Locale);
            values[35] = tracker.fobsPlacedT1.ToString(Data.Locale);
            values[36] = tracker.fobsPlacedT2.ToString(Data.Locale);
            values[37] = tracker.fobsDestroyedT1.ToString(Data.Locale);
            values[38] = tracker.fobsDestroyedT2.ToString(Data.Locale);
            values[39] = (tracker.teamkillsT1 + tracker.teamkillsT2).ToString(Data.Locale);
            values[40] = !info.IsValue ? LeaderboardEx.NO_PLAYER_NAME_PLACEHOLDER :
                T.LongestShot.Translate(lang, info.Distance,
                    Assets.find<ItemAsset>(info.Gun),
                    UCPlayer.FromID(info.Player) as IPlayer ?? F.GetPlayerOriginalNames(info.Player));
        }
        else
        {
            for (int i = 29; i < 47; ++i)
                values[i] = LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER;
        }

        int index = 46;
        if (t1Stats is not null && t1Stats.Count > 0)
        {
            int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                Stats stats = t1Stats[i];
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(1, lang, true).ToUpperInvariant() : F.GetPlayerOriginalNames(stats.Steam64).CharacterName;
                values[++index] = stats.kills.ToString(Data.Locale);
                values[++index] = stats.deaths.ToString(Data.Locale);
                values[++index] = stats.XPGained.ToString(Data.Locale);
                values[++index] = stats.Credits.ToString(Data.Locale);
                values[++index] = stats.Captures.ToString(Data.Locale);
                values[++index] = stats.DamageDone.ToString(Data.Locale);
            }
        }

        if (t2Stats is not null && t2Stats.Count > 0)
        {
            int num = Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                Stats stats = t2Stats[i];
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(2, lang, true).ToUpperInvariant() : F.GetPlayerOriginalNames(stats.Steam64).CharacterName;
                values[++index] = stats.kills.ToString(Data.Locale);
                values[++index] = stats.deaths.ToString(Data.Locale);
                values[++index] = stats.XPGained.ToString(Data.Locale);
                values[++index] = stats.Credits.ToString(Data.Locale);
                values[++index] = stats.Captures.ToString(Data.Locale);
                values[++index] = stats.DamageDone.ToString(Data.Locale);
            }
        }

        while (set.MoveNext())
        {
            UCPlayer pl = set.Next;
            ulong team = pl.GetTeam();
            Stats? stats = team switch
            {
                1 => t1Stats?.Find(x => x.Steam64 == pl.Steam64),
                2 => t2Stats?.Find(x => x.Steam64 == pl.Steam64),
                _ => null
            };
            ITransportConnection c = pl.Connection;
            FPlayerName names = F.GetPlayerOriginalNames(pl);

            SendToPlayer(c);

            Title.SetText(c, values[0]);
            if (Data.Gamemode is not null)
                Gamemode.SetText(c, Data.Gamemode.DisplayName);
            else
                Gamemode.SetText(c, string.Empty);

            NextGameStartLabel.SetText(c, values[1]);
            NextGameSeconds.SetText(c, values[2]);
            NextGameSecondsCircle.SetText(c, values[3]);
            TeamStatsHeader.SetText(c, values[4]);

            TeamDurationLabel.SetText(c, values[5]);
            TeamT1CasualtiesLabel.SetText(c, values[6]);
            TeamT2CasualtiesLabel.SetText(c, values[7]);
            TeamFlagCapturesLabel.SetText(c, values[8]);
            TeamT1AveragePlayersLabel.SetText(c, values[9]);
            TeamT2AveragePlayersLabel.SetText(c, values[10]);
            TeamT1FOBsPlacedLabel.SetText(c, values[11]);
            TeamT2FOBsPlacedLabel.SetText(c, values[12]);
            TeamT1FOBsDestroyedLabel.SetText(c, values[13]);
            TeamT2FOBsDestroyedLabel.SetText(c, values[14]);
            TeamTeamkillsLabel.SetText(c, values[15]);
            TeamLongestShotLabel.SetText(c, values[16]);

            PersonalStats0Label.SetText(c, values[17]);
            PersonalStats1Label.SetText(c, values[18]);
            PersonalStats2Label.SetText(c, values[19]);
            PersonalStats3Label.SetText(c, values[20]);
            PersonalStats4Label.SetText(c, values[21]);
            PersonalStats5Label.SetText(c, values[22]);
            PersonalStats6Label.SetText(c, values[23]);
            PersonalStats7Label.SetText(c, values[24]);
            PersonalStats8Label.SetText(c, values[25]);
            PersonalStats9Label.SetText(c, values[26]);
            PersonalStats10Label.SetText(c, values[27]);
            PersonalStats11Label.SetText(c, values[28]);

            if (stats is not null)
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, tracker is not null ? tracker.GetPresence(stats) : 0f));
                PersonalStats0.SetText(c, stats.Kills.ToString(Data.Locale));
                PersonalStats1.SetText(c, stats.Deaths.ToString(Data.Locale));
                PersonalStats2.SetText(c, stats.KDR.ToString(STAT_PRECISION_FLOAT_FORMAT, Data.Locale));
                PersonalStats3.SetText(c, stats.KillsOnPoint.ToString(Data.Locale));
                PersonalStats4.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(STAT_TIME_FORMAT, Data.Locale));
                PersonalStats5.SetText(c, stats.XPGained.ToString(Data.Locale));
                PersonalStats6.SetText(c, TimeSpan.FromSeconds(stats.timeonpoint).ToString(STAT_TIME_FORMAT, Data.Locale));
                PersonalStats7.SetText(c, stats.Captures.ToString(Data.Locale));
                PersonalStats8.SetText(c, stats.DamageDone.ToString(Data.Locale));
                PersonalStats9.SetText(c, stats.Teamkills.ToString(Data.Locale));
                PersonalStats10.SetText(c, stats.FOBsDestroyed.ToString(Data.Locale));
                PersonalStats11.SetText(c, stats.Credits.ToString(Data.Locale));
            }
            else
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, 0f));
                PersonalStats0.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats1.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats2.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats3.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats4.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats5.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats6.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats7.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats8.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats9.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats10.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats11.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
            }

            TeamDuration.SetText(c, values[29]);
            TeamT1Casualties.SetText(c, values[30]);
            TeamT2Casualties.SetText(c, values[31]);
            TeamFlagCaptures.SetText(c, values[32]);
            TeamT1AveragePlayers.SetText(c, values[33]);
            TeamT2AveragePlayers.SetText(c, values[34]);
            TeamT1FOBsPlaced.SetText(c, values[35]);
            TeamT2FOBsPlaced.SetText(c, values[36]);
            TeamT1FOBsDestroyed.SetText(c, values[37]);
            TeamT2FOBsDestroyed.SetText(c, values[38]);
            TeamTeamkills.SetText(c, values[39]);
            TeamLongestShot.SetText(c, values[40]);

            Team1Header0.SetText(c, values[41]);
            Team2Header0.SetText(c, values[41]);
            Team1Header1.SetText(c, values[42]);
            Team2Header1.SetText(c, values[42]);
            Team1Header2.SetText(c, values[43]);
            Team2Header2.SetText(c, values[43]);
            Team1Header3.SetText(c, values[44]);
            Team2Header3.SetText(c, values[44]);
            Team1Header4.SetText(c, values[45]);
            Team2Header4.SetText(c, values[45]);
            Team1Header5.SetText(c, values[46]);
            Team2Header5.SetText(c, values[46]);

            index = 46;
            if (t1Stats is not null && t1Stats.Count > 0)
            {
                int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team1Name.SetText(c, values[++index]);
                        Team1Kills.SetText(c, values[++index]);
                        Team1Deaths.SetText(c, values[++index]);
                        Team1XP.SetText(c, values[++index]);
                        Team1Credits.SetText(c, values[++index]);
                        Team1Captures.SetText(c, values[++index]);
                        Team1Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t1Stats[i].Steam64 == pl.Steam64)
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index]);
                            Team1PlayerKills[i3].SetText(c, values[++index]);
                            Team1PlayerDeaths[i3].SetText(c, values[++index]);
                            Team1PlayerXP[i3].SetText(c, values[++index]);
                            Team1PlayerCredits[i3].SetText(c, values[++index]);
                            Team1PlayerCaptures[i3].SetText(c, values[++index]);
                            Team1PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team1PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
            if (t2Stats is not null && t2Stats.Count > 0)
            {
                int num = Math.Min(t2Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team2Name.SetText(c, values[++index]);
                        Team2Kills.SetText(c, values[++index]);
                        Team2Deaths.SetText(c, values[++index]);
                        Team2XP.SetText(c, values[++index]);
                        Team2Credits.SetText(c, values[++index]);
                        Team2Captures.SetText(c, values[++index]);
                        Team2Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t2Stats[i].Steam64 == pl.Steam64)
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index]);
                            Team2PlayerKills[i3].SetText(c, values[++index]);
                            Team2PlayerDeaths[i3].SetText(c, values[++index]);
                            Team2PlayerXP[i3].SetText(c, values[++index]);
                            Team2PlayerCredits[i3].SetText(c, values[++index]);
                            Team2PlayerCaptures[i3].SetText(c, values[++index]);
                            Team2PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team2PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
        }
    }
    public void SendInsurgencyLeaderboard(LanguageSet set, in LongestShot info, List<InsurgencyPlayerStats>? t1Stats, List<InsurgencyPlayerStats>? t2Stats, InsurgencyTracker tracker, string? shutdownReason, ulong winner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FactionInfo t1 = TeamManager.GetFaction(1), t2 = TeamManager.GetFaction(2);
        string color = TeamManager.GetTeamHexColor(winner);
        string lang = set.Language;
        int len = 47;
        if (t1Stats is not null)
            len += Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1) * 7;
        if (t2Stats is not null)
            len += Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1) * 7;
        string[] values = new string[len];
        int secondsLeft = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);

        values[0] = T.WinnerTitle.Translate(lang, TeamManager.GetFactionSafe(winner)!);
        values[1] = shutdownReason is null ?
            T.StartingSoon.Translate(lang) :
            T.NextGameShutdown.Translate(lang, shutdownReason);

        values[2] = TimeSpan.FromSeconds(secondsLeft).ToString("mm\\:ss", Data.Locale);
        values[3] = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[0], 1);
        values[4] = T.WarstatsHeader.Translate(lang, TeamManager.GetFaction(1), TeamManager.GetFaction(2));

        values[5] = T.InsurgencyWarStats0.Translate(lang);
        values[6] = T.InsurgencyWarStats1.Translate(lang, t1);
        values[7] = T.InsurgencyWarStats2.Translate(lang, t2);
        values[8] = T.InsurgencyWarStats3.Translate(lang);
        values[9] = T.InsurgencyWarStats4.Translate(lang, t1);
        values[10] = T.InsurgencyWarStats5.Translate(lang, t2);
        values[11] = T.InsurgencyWarStats6.Translate(lang, t1);
        values[12] = T.InsurgencyWarStats7.Translate(lang, t2);
        values[13] = T.InsurgencyWarStats8.Translate(lang, t1);
        values[14] = T.InsurgencyWarStats9.Translate(lang, t2);
        values[15] = T.InsurgencyWarStats10.Translate(lang);
        values[16] = T.InsurgencyWarStats11.Translate(lang);

        values[17] = T.InsurgencyPlayerStats0.Translate(lang);
        values[18] = T.InsurgencyPlayerStats1.Translate(lang);
        values[19] = T.InsurgencyPlayerStats2.Translate(lang);
        values[20] = T.InsurgencyPlayerStats3.Translate(lang);
        values[21] = T.InsurgencyPlayerStats4.Translate(lang);
        values[22] = T.InsurgencyPlayerStats5.Translate(lang);
        values[23] = T.InsurgencyPlayerStats6.Translate(lang);
        values[24] = T.InsurgencyPlayerStats7.Translate(lang);
        values[25] = T.InsurgencyPlayerStats8.Translate(lang);
        values[26] = T.InsurgencyPlayerStats9.Translate(lang);
        values[27] = T.InsurgencyPlayerStats10.Translate(lang);
        values[28] = T.InsurgencyPlayerStats11.Translate(lang);

        values[41] = T.InsurgencyHeader0.Translate(lang);
        values[42] = T.InsurgencyHeader1.Translate(lang);
        values[43] = T.InsurgencyHeader2.Translate(lang);
        values[44] = T.InsurgencyHeader3.Translate(lang);
        values[45] = T.InsurgencyHeader4.Translate(lang);
        values[46] = T.InsurgencyHeader5.Translate(lang);

        if (tracker is not null)
        {
            values[29] = tracker.Duration.ToString(STAT_TIME_FORMAT, Data.Locale);
            values[30] = tracker.casualtiesT1.ToString(Data.Locale);
            values[31] = tracker.casualtiesT2.ToString(Data.Locale);
            values[32] = tracker.intelligenceGathered.ToString(Data.Locale);
            values[33] = tracker.AverageTeam1Size.ToString(STAT_FLOAT_FORMAT, Data.Locale);
            values[34] = tracker.AverageTeam2Size.ToString(STAT_FLOAT_FORMAT, Data.Locale);
            values[35] = tracker.fobsPlacedT1.ToString(Data.Locale);
            values[36] = tracker.fobsPlacedT2.ToString(Data.Locale);
            values[37] = tracker.fobsDestroyedT1.ToString(Data.Locale);
            values[38] = tracker.fobsDestroyedT2.ToString(Data.Locale);
            values[39] = (tracker.teamkillsT1 + tracker.teamkillsT2).ToString(Data.Locale);
            values[40] = !info.IsValue ? LeaderboardEx.NO_PLAYER_NAME_PLACEHOLDER :
                T.LongestShot.Translate(lang, info.Distance,
                    Assets.find<ItemAsset>(info.Gun),
                    UCPlayer.FromID(info.Player) as IPlayer ?? F.GetPlayerOriginalNames(info.Player));
        }
        else
        {
            for (int i = 29; i < 47; ++i)
                values[i] = LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER;
        }

        int index = 46;
        if (t1Stats is not null && t1Stats.Count > 0)
        {
            int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                InsurgencyPlayerStats stats = t1Stats[i];
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(1, lang, true).ToUpperInvariant() : F.GetPlayerOriginalNames(stats.Steam64).CharacterName;
                values[++index] = stats.kills.ToString(Data.Locale);
                values[++index] = stats.deaths.ToString(Data.Locale);
                values[++index] = stats.XPGained.ToString(Data.Locale);
                values[++index] = stats.Credits.ToString(Data.Locale);
                values[++index] = stats.KDR.ToString(Data.Locale);
                values[++index] = stats.DamageDone.ToString(Data.Locale);
            }
        }

        if (t2Stats is not null && t2Stats.Count > 0)
        {
            int num = Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                InsurgencyPlayerStats stats = t2Stats[i];
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(2, lang, true).ToUpperInvariant() : F.GetPlayerOriginalNames(stats.Steam64).CharacterName;
                values[++index] = stats.kills.ToString(Data.Locale);
                values[++index] = stats.deaths.ToString(Data.Locale);
                values[++index] = stats.XPGained.ToString(Data.Locale);
                values[++index] = stats.Credits.ToString(Data.Locale);
                values[++index] = stats.KDR.ToString(Data.Locale);
                values[++index] = stats.DamageDone.ToString(Data.Locale);
            }
        }

        while (set.MoveNext())
        {
            UCPlayer pl = set.Next;
            ulong team = pl.GetTeam();
            InsurgencyPlayerStats? stats = team switch
            {
                1 => t1Stats?.Find(x => x.Steam64 == pl.Steam64),
                2 => t2Stats?.Find(x => x.Steam64 == pl.Steam64),
                _ => null
            };
            ITransportConnection c = pl.Connection;
            FPlayerName names = F.GetPlayerOriginalNames(pl);

            SendToPlayer(c);

            Title.SetText(c, values[0]);
            if (Data.Gamemode is not null)
                Gamemode.SetText(c, Data.Gamemode.DisplayName);
            else
                Gamemode.SetText(c, string.Empty);

            NextGameStartLabel.SetText(c, values[1]);
            NextGameSeconds.SetText(c, values[2]);
            NextGameSecondsCircle.SetText(c, values[3]);
            TeamStatsHeader.SetText(c, values[4]);

            TeamDurationLabel.SetText(c, values[5]);
            TeamT1CasualtiesLabel.SetText(c, values[6]);
            TeamT2CasualtiesLabel.SetText(c, values[7]);
            TeamFlagCapturesLabel.SetText(c, values[8]);
            TeamT1AveragePlayersLabel.SetText(c, values[9]);
            TeamT2AveragePlayersLabel.SetText(c, values[10]);
            TeamT1FOBsPlacedLabel.SetText(c, values[11]);
            TeamT2FOBsPlacedLabel.SetText(c, values[12]);
            TeamT1FOBsDestroyedLabel.SetText(c, values[13]);
            TeamT2FOBsDestroyedLabel.SetText(c, values[14]);
            TeamTeamkillsLabel.SetText(c, values[15]);
            TeamLongestShotLabel.SetText(c, values[16]);

            PersonalStats0Label.SetText(c, values[17]);
            PersonalStats1Label.SetText(c, values[18]);
            PersonalStats2Label.SetText(c, values[19]);
            PersonalStats3Label.SetText(c, values[20]);
            PersonalStats4Label.SetText(c, values[21]);
            PersonalStats5Label.SetText(c, values[22]);
            PersonalStats6Label.SetText(c, values[23]);
            PersonalStats7Label.SetText(c, values[24]);
            PersonalStats8Label.SetText(c, values[25]);
            PersonalStats9Label.SetText(c, values[26]);
            PersonalStats10Label.SetText(c, values[27]);
            PersonalStats11Label.SetText(c, values[28]);

            if (stats is not null)
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, tracker is not null ? tracker.GetPresence(stats) : 0f));
                PersonalStats0.SetText(c, stats.Kills.ToString(Data.Locale));
                PersonalStats1.SetText(c, stats.Deaths.ToString(Data.Locale));
                PersonalStats2.SetText(c, stats.DamageDone.ToString(STAT_FLOAT_FORMAT, Data.Locale));
                if (Data.Gamemode is IAttackDefense iad)
                    PersonalStats3.SetText(c, (team == iad.AttackingTeam ? stats.KillsAttack : stats.KillsDefense).ToString(Data.Locale));
                else
                    PersonalStats3.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats4.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(STAT_TIME_FORMAT, Data.Locale));
                PersonalStats5.SetText(c, stats.XPGained.ToString(Data.Locale));
                PersonalStats6.SetText(c, stats._intelligencePointsCollected.ToString(Data.Locale));
                PersonalStats7.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER /* todo */);
                PersonalStats8.SetText(c, stats._cachesDestroyed.ToString(Data.Locale));
                PersonalStats9.SetText(c, stats.Teamkills.ToString(Data.Locale));
                PersonalStats10.SetText(c, stats.FOBsDestroyed.ToString(Data.Locale));
                PersonalStats11.SetText(c, stats.Credits.ToString(Data.Locale));
            }
            else
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, 0f));
                PersonalStats0.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats1.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats2.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats3.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats4.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats5.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats6.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats7.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats8.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats9.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats10.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats11.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
            }

            TeamDuration.SetText(c, values[29]);
            TeamT1Casualties.SetText(c, values[30]);
            TeamT2Casualties.SetText(c, values[31]);
            TeamFlagCaptures.SetText(c, values[32]);
            TeamT1AveragePlayers.SetText(c, values[33]);
            TeamT2AveragePlayers.SetText(c, values[34]);
            TeamT1FOBsPlaced.SetText(c, values[35]);
            TeamT2FOBsPlaced.SetText(c, values[36]);
            TeamT1FOBsDestroyed.SetText(c, values[37]);
            TeamT2FOBsDestroyed.SetText(c, values[38]);
            TeamTeamkills.SetText(c, values[39]);
            TeamLongestShot.SetText(c, values[40]);


            Team1Header0.SetText(c, values[41]);
            Team2Header0.SetText(c, values[41]);
            Team1Header1.SetText(c, values[42]);
            Team2Header1.SetText(c, values[42]);
            Team1Header2.SetText(c, values[43]);
            Team2Header2.SetText(c, values[43]);
            Team1Header3.SetText(c, values[44]);
            Team2Header3.SetText(c, values[44]);
            Team1Header4.SetText(c, values[45]);
            Team2Header4.SetText(c, values[45]);
            Team1Header5.SetText(c, values[46]);
            Team2Header5.SetText(c, values[46]);

            index = 46;
            if (t1Stats is not null && t1Stats.Count > 0)
            {
                int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team1Name.SetText(c, values[++index]);
                        Team1Kills.SetText(c, values[++index]);
                        Team1Deaths.SetText(c, values[++index]);
                        Team1XP.SetText(c, values[++index]);
                        Team1Credits.SetText(c, values[++index]);
                        Team1Captures.SetText(c, values[++index]);
                        Team1Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t1Stats[i].Steam64 == pl.Steam64)
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index]);
                            Team1PlayerKills[i3].SetText(c, values[++index]);
                            Team1PlayerDeaths[i3].SetText(c, values[++index]);
                            Team1PlayerXP[i3].SetText(c, values[++index]);
                            Team1PlayerCredits[i3].SetText(c, values[++index]);
                            Team1PlayerCaptures[i3].SetText(c, values[++index]);
                            Team1PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team1PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
            if (t2Stats is not null && t2Stats.Count > 0)
            {
                int num = Math.Min(t2Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team2Name.SetText(c, values[++index]);
                        Team2Kills.SetText(c, values[++index]);
                        Team2Deaths.SetText(c, values[++index]);
                        Team2XP.SetText(c, values[++index]);
                        Team2Credits.SetText(c, values[++index]);
                        Team2Captures.SetText(c, values[++index]);
                        Team2Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t2Stats[i].Steam64 == pl.Steam64)
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index]);
                            Team2PlayerKills[i3].SetText(c, values[++index]);
                            Team2PlayerDeaths[i3].SetText(c, values[++index]);
                            Team2PlayerXP[i3].SetText(c, values[++index]);
                            Team2PlayerCredits[i3].SetText(c, values[++index]);
                            Team2PlayerCaptures[i3].SetText(c, values[++index]);
                            Team2PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team2PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
        }
    }
    public void SendConquestLeaderboard(LanguageSet set, in LongestShot info, List<ConquestStats>? t1Stats, List<ConquestStats>? t2Stats, ConquestStatTracker tracker, string? shutdownReason, ulong winner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FactionInfo t1 = TeamManager.GetFaction(1), t2 = TeamManager.GetFaction(2);
        string color = TeamManager.GetTeamHexColor(winner);
        string lang = set.Language;
        int len = 47;
        if (t1Stats is not null)
            len += Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1) * 7;
        if (t2Stats is not null)
            len += Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1) * 7;
        string[] values = new string[len];
        int secondsLeft = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);

        values[0] = T.WinnerTitle.Translate(lang, TeamManager.GetFactionSafe(winner)!);
        values[1] = shutdownReason is null ?
            T.StartingSoon.Translate(lang) :
            T.NextGameShutdown.Translate(lang, shutdownReason);

        values[2] = TimeSpan.FromSeconds(secondsLeft).ToString("m\\:ss", Data.Locale);
        values[3] = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[0], 1);
        values[4] = T.WarstatsHeader.Translate(lang, TeamManager.GetFaction(1), TeamManager.GetFaction(2));

        values[5] = T.ConquestWarStats0.Translate(lang);
        values[6] = T.ConquestWarStats1.Translate(lang, t1);
        values[7] = T.ConquestWarStats2.Translate(lang, t2);
        values[8] = T.ConquestWarStats3.Translate(lang);
        values[9] = T.ConquestWarStats4.Translate(lang, t1);
        values[10] = T.ConquestWarStats5.Translate(lang, t2);
        values[11] = T.ConquestWarStats6.Translate(lang, t1);
        values[12] = T.ConquestWarStats7.Translate(lang, t2);
        values[13] = T.ConquestWarStats8.Translate(lang, t1);
        values[14] = T.ConquestWarStats9.Translate(lang, t2);
        values[15] = T.ConquestWarStats10.Translate(lang);
        values[16] = T.ConquestWarStats11.Translate(lang);

        values[17] = T.ConquestPlayerStats0.Translate(lang);
        values[18] = T.ConquestPlayerStats1.Translate(lang);
        values[19] = T.ConquestPlayerStats2.Translate(lang);
        values[20] = T.ConquestPlayerStats3.Translate(lang);
        values[21] = T.ConquestPlayerStats4.Translate(lang);
        values[22] = T.ConquestPlayerStats5.Translate(lang);
        values[23] = T.ConquestPlayerStats6.Translate(lang);
        values[24] = T.ConquestPlayerStats7.Translate(lang);
        values[25] = T.ConquestPlayerStats8.Translate(lang);
        values[26] = T.ConquestPlayerStats9.Translate(lang);
        values[27] = T.ConquestPlayerStats10.Translate(lang);
        values[28] = T.ConquestPlayerStats11.Translate(lang);

        values[41] = T.ConquestHeader0.Translate(lang);
        values[42] = T.ConquestHeader1.Translate(lang);
        values[43] = T.ConquestHeader2.Translate(lang);
        values[44] = T.ConquestHeader3.Translate(lang);
        values[45] = T.ConquestHeader4.Translate(lang);
        values[46] = T.ConquestHeader5.Translate(lang);


        if (tracker is not null)
        {
            values[29] = tracker.Duration.ToString(STAT_TIME_FORMAT, Data.Locale);
            values[30] = tracker.casualtiesT1.ToString(Data.Locale);
            values[31] = tracker.casualtiesT2.ToString(Data.Locale);
            values[32] = tracker.flagOwnerChanges.ToString(Data.Locale);
            values[33] = tracker.AverageTeam1Size.ToString(STAT_FLOAT_FORMAT, Data.Locale);
            values[34] = tracker.AverageTeam2Size.ToString(STAT_FLOAT_FORMAT, Data.Locale);
            values[35] = tracker.fobsPlacedT1.ToString(Data.Locale);
            values[36] = tracker.fobsPlacedT2.ToString(Data.Locale);
            values[37] = tracker.fobsDestroyedT1.ToString(Data.Locale);
            values[38] = tracker.fobsDestroyedT2.ToString(Data.Locale);
            values[39] = (tracker.teamkillsT1 + tracker.teamkillsT2).ToString(Data.Locale);
            values[40] = !info.IsValue ? LeaderboardEx.NO_PLAYER_NAME_PLACEHOLDER :
                T.LongestShot.Translate(lang, info.Distance,
                    Assets.find<ItemAsset>(info.Gun),
                    UCPlayer.FromID(info.Player) as IPlayer ?? F.GetPlayerOriginalNames(info.Player));
        }
        else
        {
            for (int i = 29; i < 47; ++i)
                values[i] = LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER;
        }

        int index = 46;
        if (t1Stats is not null && t1Stats.Count > 0)
        {
            int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                ConquestStats stats = t1Stats[i];
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(1, lang, true).ToUpperInvariant() : F.GetPlayerOriginalNames(stats.Steam64).CharacterName;
                values[++index] = stats.kills.ToString(Data.Locale);
                values[++index] = stats.deaths.ToString(Data.Locale);
                values[++index] = stats.XPGained.ToString(Data.Locale);
                values[++index] = stats.Credits.ToString(Data.Locale);
                values[++index] = stats.KDR.ToString(Data.Locale);
                values[++index] = stats.DamageDone.ToString(Data.Locale);
            }
        }

        if (t2Stats is not null && t2Stats.Count > 0)
        {
            int num = Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                ConquestStats stats = t2Stats[i];
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(2, lang, true).ToUpperInvariant() : F.GetPlayerOriginalNames(stats.Steam64).CharacterName;
                values[++index] = stats.kills.ToString(Data.Locale);
                values[++index] = stats.deaths.ToString(Data.Locale);
                values[++index] = stats.XPGained.ToString(Data.Locale);
                values[++index] = stats.Credits.ToString(Data.Locale);
                values[++index] = stats.KDR.ToString(Data.Locale);
                values[++index] = stats.DamageDone.ToString(Data.Locale);
            }
        }

        while (set.MoveNext())
        {
            L.LogWarning(set.ToString());
            UCPlayer pl = set.Next;
            ulong team = pl.GetTeam();
            ConquestStats? stats = team switch
            {
                1 => t1Stats?.Find(x => x.Steam64 == pl.Steam64),
                2 => t2Stats?.Find(x => x.Steam64 == pl.Steam64),
                _ => null
            };
            ITransportConnection c = pl.Connection;
            FPlayerName names = F.GetPlayerOriginalNames(pl);

            SendToPlayer(c);

            Title.SetText(c, values[0]);
            if (Data.Gamemode is not null)
                Gamemode.SetText(c, Data.Gamemode.DisplayName);
            else
                Gamemode.SetText(c, string.Empty);

            NextGameStartLabel.SetText(c, values[1]);
            NextGameSeconds.SetText(c, values[2]);
            NextGameSecondsCircle.SetText(c, values[3]);
            TeamStatsHeader.SetText(c, values[4]);

            TeamDurationLabel.SetText(c, values[5]);
            TeamT1CasualtiesLabel.SetText(c, values[6]);
            TeamT2CasualtiesLabel.SetText(c, values[7]);
            TeamFlagCapturesLabel.SetText(c, values[8]);
            TeamT1AveragePlayersLabel.SetText(c, values[9]);
            TeamT2AveragePlayersLabel.SetText(c, values[10]);
            TeamT1FOBsPlacedLabel.SetText(c, values[11]);
            TeamT2FOBsPlacedLabel.SetText(c, values[12]);
            TeamT1FOBsDestroyedLabel.SetText(c, values[13]);
            TeamT2FOBsDestroyedLabel.SetText(c, values[14]);
            TeamTeamkillsLabel.SetText(c, values[15]);
            TeamLongestShotLabel.SetText(c, values[16]);

            PersonalStats0Label.SetText(c, values[17]);
            PersonalStats1Label.SetText(c, values[18]);
            PersonalStats2Label.SetText(c, values[19]);
            PersonalStats3Label.SetText(c, values[20]);
            PersonalStats4Label.SetText(c, values[21]);
            PersonalStats5Label.SetText(c, values[22]);
            PersonalStats6Label.SetText(c, values[23]);
            PersonalStats7Label.SetText(c, values[24]);
            PersonalStats8Label.SetText(c, values[25]);
            PersonalStats9Label.SetText(c, values[26]);
            PersonalStats10Label.SetText(c, values[27]);
            PersonalStats11Label.SetText(c, values[28]);

            if (stats is not null)
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, tracker is not null ? tracker.GetPresence(stats) : 0f));
                PersonalStats0.SetText(c, stats.Kills.ToString(Data.Locale));
                PersonalStats1.SetText(c, stats.Deaths.ToString(Data.Locale));
                PersonalStats2.SetText(c, stats.DamageDone.ToString(STAT_FLOAT_FORMAT, Data.Locale));
                PersonalStats3.SetText(c, stats.KillsOnPoint.ToString(Data.Locale));
                PersonalStats4.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(STAT_TIME_FORMAT, Data.Locale));
                PersonalStats5.SetText(c, stats.XPGained.ToString(Data.Locale));
                PersonalStats6.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(STAT_TIME_FORMAT, Data.Locale));
                PersonalStats7.SetText(c, stats.Captures.ToString(Data.Locale));
                PersonalStats8.SetText(c, TimeSpan.FromSeconds(stats.timeonpoint).ToString(STAT_TIME_FORMAT, Data.Locale));
                PersonalStats9.SetText(c, stats.Teamkills.ToString(Data.Locale));
                PersonalStats10.SetText(c, stats.FOBsDestroyed.ToString(Data.Locale));
                PersonalStats11.SetText(c, stats.Credits.ToString(Data.Locale));
            }
            else
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, 0f));
                PersonalStats0.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats1.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats2.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats3.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats4.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats5.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats6.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats7.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats8.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats9.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats10.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
                PersonalStats11.SetText(c, LeaderboardEx.NO_PLAYER_VALUE_PLACEHOLDER);
            }

            TeamDuration.SetText(c, values[29]);
            TeamT1Casualties.SetText(c, values[30]);
            TeamT2Casualties.SetText(c, values[31]);
            TeamFlagCaptures.SetText(c, values[32]);
            TeamT1AveragePlayers.SetText(c, values[33]);
            TeamT2AveragePlayers.SetText(c, values[34]);
            TeamT1FOBsPlaced.SetText(c, values[35]);
            TeamT2FOBsPlaced.SetText(c, values[36]);
            TeamT1FOBsDestroyed.SetText(c, values[37]);
            TeamT2FOBsDestroyed.SetText(c, values[38]);
            TeamTeamkills.SetText(c, values[39]);
            TeamLongestShot.SetText(c, values[40]);


            Team1Header0.SetText(c, values[41]);
            Team2Header0.SetText(c, values[41]);
            Team1Header1.SetText(c, values[42]);
            Team2Header1.SetText(c, values[42]);
            Team1Header2.SetText(c, values[43]);
            Team2Header2.SetText(c, values[43]);
            Team1Header3.SetText(c, values[44]);
            Team2Header3.SetText(c, values[44]);
            Team1Header4.SetText(c, values[45]);
            Team2Header4.SetText(c, values[45]);
            Team1Header5.SetText(c, values[46]);
            Team2Header5.SetText(c, values[46]);

            index = 46;
            if (t1Stats is not null && t1Stats.Count > 0)
            {
                int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team1Name.SetText(c, values[++index]);
                        Team1Kills.SetText(c, values[++index]);
                        Team1Deaths.SetText(c, values[++index]);
                        Team1XP.SetText(c, values[++index]);
                        Team1Credits.SetText(c, values[++index]);
                        Team1Captures.SetText(c, values[++index]);
                        Team1Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t1Stats[i].Steam64 == pl.Steam64)
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index]);
                            Team1PlayerKills[i3].SetText(c, values[++index]);
                            Team1PlayerDeaths[i3].SetText(c, values[++index]);
                            Team1PlayerXP[i3].SetText(c, values[++index]);
                            Team1PlayerCredits[i3].SetText(c, values[++index]);
                            Team1PlayerCaptures[i3].SetText(c, values[++index]);
                            Team1PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team1PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
            if (t2Stats is not null && t2Stats.Count > 0)
            {
                int num = Math.Min(t2Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team2Name.SetText(c, values[++index]);
                        Team2Kills.SetText(c, values[++index]);
                        Team2Deaths.SetText(c, values[++index]);
                        Team2XP.SetText(c, values[++index]);
                        Team2Credits.SetText(c, values[++index]);
                        Team2Captures.SetText(c, values[++index]);
                        Team2Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t2Stats[i].Steam64 == pl.Steam64)
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index]);
                            Team2PlayerKills[i3].SetText(c, values[++index]);
                            Team2PlayerDeaths[i3].SetText(c, values[++index]);
                            Team2PlayerXP[i3].SetText(c, values[++index]);
                            Team2PlayerCredits[i3].SetText(c, values[++index]);
                            Team2PlayerCaptures[i3].SetText(c, values[++index]);
                            Team2PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team2PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
        }
    }
    public void UpdateTime(LanguageSet set, int secondsLeft)
    {
        int time = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);
        string l1 = TimeSpan.FromSeconds(secondsLeft).ToString("m\\:ss");
        string l2 = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[CTFUI.FromMax(Mathf.RoundToInt(time - secondsLeft), time)], 1);
        while (set.MoveNext())
        {
            NextGameSeconds.SetText(set.Next.Connection, l1);
            NextGameSecondsCircle.SetText(set.Next.Connection, l2);
        }
    }
}
