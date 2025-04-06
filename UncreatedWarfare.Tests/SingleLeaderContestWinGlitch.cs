using NUnit.Framework;
using Steamworks;
using System;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tests;

/*
 * Bug reported by Hamza where players capturing flags
 * would infintely gain the Flag Captured XP point
 */
internal class SingleLeaderContestWinGlitch
{
    private static readonly Team Team1 = new Team
    {
        Id = 1,
        Faction = new FactionInfo { FactionId = "caf", Name = "Canada" },
    };
    private static readonly Team Team2 = new Team
    {
        Id = 1,
        Faction = new FactionInfo { FactionId = "ru", Name = "Russia" },
    };

    [Test]
    public void SingleLeaderContestWinGlitchTest()
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
        contest.OnRestarted += t =>
        {
            Console.WriteLine($"{t} restarted.");
        };

        contest.AwardPoints(Team1, 64);

        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team1));
        Assert.That(winCt, Is.EqualTo(1));
        contest.AwardPoints(Team1, 64);
        Assert.That(winCt, Is.EqualTo(1));

        contest.AwardPoints(Team2, 5);
        Assert.That(winCt, Is.EqualTo(1));

        Assert.That(contest.IsWon, Is.True);
        Assert.That(contest.Leader, Is.EqualTo(Team1));
        Assert.That(winCt, Is.EqualTo(1));


        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(1));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(1));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(1));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(1));
        contest.AwardPoints(Team1, 1);
        Assert.That(winCt, Is.EqualTo(1));

        Assert.That(contest.Leader, Is.EqualTo(Team1));
    }
}
