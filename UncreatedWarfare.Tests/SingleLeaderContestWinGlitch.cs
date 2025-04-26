using NUnit.Framework;
using Steamworks;
using System;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Tests;

/*
 * Bug reported by Hamza where players capturing flags
 * would gain the Flag Captured XP point every tick while owned by the other team
 */
internal class SingleLeaderContestWinGlitch
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
}
