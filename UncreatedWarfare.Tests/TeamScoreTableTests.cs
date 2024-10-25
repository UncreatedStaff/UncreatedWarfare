using NUnit.Framework;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Tests;
public class TeamScoreTableTests
{
    private const double Threshold = 0.0000001d;
    private static IReadOnlyList<Team> CreateTeamList(int n)
    {
        Team[] arr = new Team[n];
        for (int i = 0; i < n; ++i)
        {
            arr[i] = new Team
            {
                Faction = new FactionInfo { Name = "faction" + (i + 1) },
                Id = i + 1,
                GroupId = new CSteamID((ulong)(i + 1)),
                Configuration = ConfigurationHelper.EmptySection
            };
        }

        for (int i = 0; i < n; ++i)
        {
            arr[i].Opponents.Capacity = n - 1;
            for (int j = 0; j < n; ++j)
            {
                if (j == i)
                    continue;

                arr[i].Opponents.Add(arr[j]);
            }
        }

        return arr;
    }

    private void AssertTotalSameAsStartingValue(TeamScoreTable tests)
    {
        double ttl = 0;
        foreach (Team team in tests.Teams)
        {
            ttl += tests[team];
        }

        ttl += tests[null];

        Assert.That(ttl, Is.EqualTo(tests.TotalScore).Within(Threshold));
    }

    [Test]
    public void TestOneTeam()
    {
        TeamScoreTable tests = new TeamScoreTable(CreateTeamList(1), 64);

        // N: 64, try add more when can't
        Assert.That(tests.IncrementPoints(null, 32), Is.EqualTo(0).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(64).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(0).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);

        // N: 32, 0: 32
        Assert.That(tests.IncrementPoints(tests.Teams[0], 32), Is.EqualTo(32).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(32).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(32).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);

        // N: 0, 0: 64, try add too many
        Assert.That(tests.IncrementPoints(tests.Teams[0], 64), Is.EqualTo(32).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(0).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(64).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);
    }

    [Test]
    public void TestTwoTeams()
    {
        TeamScoreTable tests = new TeamScoreTable(CreateTeamList(2), 90);

        // N: 90, try add more when can't
        Assert.That(tests.IncrementPoints(null, 45), Is.EqualTo(0).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(90).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(0).Within(Threshold));
        Assert.That(tests[tests.Teams[1]], Is.EqualTo(0).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);

        // N: 45, 0: 45, 1: 0
        Assert.That(tests.IncrementPoints(tests.Teams[0], 45), Is.EqualTo(45).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(45).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(45).Within(Threshold));
        Assert.That(tests[tests.Teams[1]], Is.EqualTo(0).Within(Threshold));

        // N: 45, 0: 22.5, 1: 22.5
        Assert.That(tests.IncrementPoints(tests.Teams[1], 22.5), Is.EqualTo(22.5).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(45).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(22.5).Within(Threshold));
        Assert.That(tests[tests.Teams[1]], Is.EqualTo(22.5).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);

        // N: 45, 0: 30, 1: 15
        Assert.That(tests.IncrementPoints(tests.Teams[0], 7.5), Is.EqualTo(7.5).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(45).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(30).Within(Threshold));
        Assert.That(tests[tests.Teams[1]], Is.EqualTo(15).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);

        // N: 90, 0: 0, 1: 0, try add too many
        Assert.That(tests.IncrementPoints(null, 90), Is.EqualTo(45).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(90).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(0).Within(Threshold));
        Assert.That(tests[tests.Teams[1]], Is.EqualTo(0).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);
    }

    [Test]
    public void TestManyTeams()
    {
        TeamScoreTable tests = new TeamScoreTable(CreateTeamList(10), 1);
        tests.DistributeUniformly();

        Console.WriteLine(tests.ToGraph(false));

        AssertTotalSameAsStartingValue(tests);

        // N: 90, try add more when can't
        Assert.That(tests.IncrementPoints(tests.Teams[1], 1), Is.EqualTo(0.9).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests[null],           Is.EqualTo(0).Within(Threshold));
        Assert.That(tests[tests.Teams[0]], Is.EqualTo(0).Within(Threshold));
        Assert.That(tests[tests.Teams[1]], Is.EqualTo(1).Within(Threshold));
        for (int i = 2; i < 10; ++i)
            Assert.That(tests[tests.Teams[i]], Is.EqualTo(0).Within(Threshold));

        AssertTotalSameAsStartingValue(tests);
    }

    [Test]
    public void TestRemoveFromManyUniformTeams()
    {
        TeamScoreTable tests = new TeamScoreTable(CreateTeamList(10), 10);
        tests.Distribute(0f, 3d, 3d, 3d, 3d, 3d, 3d, 3d, 3d, 3d, 3d);

        Console.WriteLine(tests.ToGraph(false));

        Assert.That(tests.DecrementPoints(tests.Teams[0], 999, false), Is.EqualTo(1).Within(Threshold));

        for (int i = 1; i < tests.Teams.Count; ++i)
            Assert.That(tests[tests.Teams[i]], Is.EqualTo(1d + (1d / 9d)).Within(Threshold));

        Console.WriteLine(tests.ToGraph(false));

        AssertTotalSameAsStartingValue(tests);
    }

    [Test]
    public void TestRemoveFromManyNonUniformTeams()
    {
        TeamScoreTable tests = new TeamScoreTable(CreateTeamList(10), 33);
        tests.Distribute(0f, 4d, 4d, 4d, 4d, 4d, 2d, 2d, 3d, 3d, 3d);

        Console.WriteLine(tests.ToGraph(true));

        Assert.That(tests.DecrementPoints(tests.Teams[0], 999, false), Is.EqualTo(4).Within(Threshold));

        Console.WriteLine(tests.ToGraph(true));

        AssertTotalSameAsStartingValue(tests);

        Assert.That(tests.DecrementPoints(tests.Teams[1], 999, false), Is.EqualTo(4).Within(Threshold));

        Console.WriteLine(tests.ToGraph(true));

        AssertTotalSameAsStartingValue(tests);

        Assert.That(tests.DecrementPoints(tests.Teams[2], 999, false), Is.EqualTo(4).Within(Threshold));

        Console.WriteLine(tests.ToGraph(true));

        AssertTotalSameAsStartingValue(tests);
    }

    [Test]
    public void TestRemoveToNeutral()
    {
        TeamScoreTable tests = new TeamScoreTable(CreateTeamList(3), 3);
        tests.Distribute(0f, 1d, 1d, 1d);

        Console.WriteLine(tests.ToGraph(true));

        Assert.That(tests.DecrementPoints(tests.Teams[0], 999, true), Is.EqualTo(1).Within(Threshold));

        Console.WriteLine(tests.ToGraph(true));

        AssertTotalSameAsStartingValue(tests);

        Assert.That(tests.DecrementPoints(tests.Teams[1], 999, true), Is.EqualTo(1).Within(Threshold));

        Console.WriteLine(tests.ToGraph(true));

        AssertTotalSameAsStartingValue(tests);

        Assert.That(tests.DecrementPoints(tests.Teams[2], 999, true), Is.EqualTo(1).Within(Threshold));

        Console.WriteLine(tests.ToGraph(true));

        AssertTotalSameAsStartingValue(tests);
    }
}
