using NUnit.Framework;
using Steamworks;
using System;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Tests;

internal class SingleLeaderContestTests
{
    private static readonly Team Team1 = new Team
    {
        Id = 1,
        Faction = new FactionInfo { FactionId = "caf", Name = "Canada" },
        GroupId = new CSteamID(1)
    };
    private static readonly Team Team2 = new Team
    {
        Id = 1,
        Faction = new FactionInfo { FactionId = "ru", Name = "Russia" },
        GroupId = new CSteamID(2)
    };

    /*
     * Bug reported by Hamza where players capturing flags
     * would gain the Flag Captured XP point every tick while owned by the other team
     */
    [Test]
    public void WinGlitchTest()
    {
        SingleLeaderContest contest = new SingleLeaderContest(64);

        int winCt = 0;

        contest.OnWon += t =>
        {
            ++winCt;
            Console.WriteLine($"{t} won.");
        };
        contest.OnPointsChanged += n =>
        {
            Console.WriteLine($"{n} pts changed.");
        };
        contest.OnRestarted += (t, _) =>
        {
            Console.WriteLine($"{t} restarted.");
        };
        contest.OnCleared += (t, amt) =>
        {
            Console.WriteLine($"{t} recapped {amt} pts.");
        };

        contest.AwardPoints(Team2, 64);

        contest.AwardPoints(Team1, 64);

        contest.AwardPoints(Team1, 64);

        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team1));
        Assert.That(winCt, Is.EqualTo(2));

        contest.AwardPoints(Team2, 50);
        Assert.That(winCt, Is.EqualTo(2));

        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team1));
        Assert.That(winCt, Is.EqualTo(2));


        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(2));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(2));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(2));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(2));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(2));

        Assert.That(contest.Leader, Is.EqualTo(Team1));
    }

    [Test]
    public void WinNormal()
    {
        SingleLeaderContest contest = new SingleLeaderContest(64);

        int winCt = 0, clearCt = 0;

        contest.OnWon += t =>
        {
            ++winCt;
            Console.WriteLine($"{t} won.");
        };
        contest.OnPointsChanged += n =>
        {
            Console.WriteLine($"{n} pts changed.");
        };
        contest.OnRestarted += (t, _) =>
        {
            Console.WriteLine($"{t} restarted.");
        };
        contest.OnCleared += (t, amt) =>
        {
            ++clearCt;
            Console.WriteLine($"{t} recapped {amt} pts.");
        };

        // team 2 capture
        contest.AwardPoints(Team2, 64);
        Assert.That(winCt, Is.EqualTo(1));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team2));

        // team 1 neutralize
        contest.AwardPoints(Team1, 64);
        Assert.That(winCt, Is.EqualTo(1));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.False);
        Assert.That(contest.Leader, Is.EqualTo(Team.NoTeam));

        // team 1 capture
        contest.AwardPoints(Team1, 64);
        Assert.That(winCt, Is.EqualTo(2));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team1));

        // team 2 99% to neutralized
        contest.AwardPoints(Team2, 63);
        Assert.That(winCt, Is.EqualTo(2));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team1));

        // team 2 neturalize
        contest.AwardPoints(Team2, 1);
        Assert.That(winCt, Is.EqualTo(2));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.False);
        Assert.That(contest.Leader, Is.EqualTo(Team.NoTeam));

        // team 1 recapture
        contest.AwardPoints(Team1, 64);
        Assert.That(winCt, Is.EqualTo(3));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team1));
    }

    /// <summary>
    /// Reported originally by Angel, causes recapturing to count as a win, even if the flag was only cleared by one point.
    /// </summary>
    [Test]
    public void DoesntWinOnBackcapUnlessNeutralizedTest()
    {
        SingleLeaderContest contest = new SingleLeaderContest(64);


        int winCt = 0, clearCt = 0;

        contest.OnWon += t =>
        {
            ++winCt;
            Console.WriteLine($"{t} won.");
        };
        contest.OnPointsChanged += n =>
        {
            Console.WriteLine($"{n} pts changed.");
        };
        contest.OnRestarted += (t, _) =>
        {
            Console.WriteLine($"{t} restarted.");
        };
        contest.OnCleared += (t, amt) =>
        {
            ++clearCt;
            Console.WriteLine($"{t} recapped {amt} pts.");
        };

        // team 2 capture
        contest.AwardPoints(Team2, 64);
        Assert.That(winCt, Is.EqualTo(1));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team2));

        // team 1 start neutralizing
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(1));
        Assert.That(clearCt, Is.EqualTo(0));
        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team2));

        // team 2 recapture
        contest.AwardPoints(Team2, 1);
        Assert.That(winCt, Is.EqualTo(1));
        Assert.That(clearCt, Is.EqualTo(1));
        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team2));
    }
}