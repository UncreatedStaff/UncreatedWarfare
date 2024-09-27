using NUnit.Framework;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Tests;

public class Tests
{
    private IPlayerService _playerService;
    private ITeamSelectorBehavior _behavior;

    [SetUp]
    public void Setup()
    {
        _playerService = new NullPlayerService();
        _behavior = new DefaultTeamSelectorBehavior(_playerService);

        Team t1 = new Team
        {
            Faction = new FactionInfo("id1", "name1", "abb1", null, Color.red, null, "id1"),
            Id = 1
        };
        Team t2 = new Team
        {
            Faction = new FactionInfo("id2", "name2", "abb2", null, Color.yellow, null, "id2"),
            Id = 2
        };

        _behavior.Teams = new TeamInfo[2];

        _behavior.Teams[0].Team = t1;
        _behavior.Teams[1].Team = t2;
    }

    [Test]
    public void TestBothEmpty()
    {
        _behavior.Teams[0].PlayerCount = 0;
        _behavior.Teams[1].PlayerCount = 0;

        Assert.That(_behavior.CanJoinTeam(0));
        Assert.That(_behavior.CanJoinTeam(1));
    }

    [Test]
    public void TestLowValues()
    {
        // 3-0 works but 4-0 doesnt
        _behavior.Teams[0].PlayerCount = 3;
        _behavior.Teams[1].PlayerCount = 0;

        Assert.That(_behavior.CanJoinTeam(0));
        Assert.That(_behavior.CanJoinTeam(1));
    }

    [Test]
    public void TestSignificantDifference()
    {
        // 3-0 works but 4-0 doesnt
        _behavior.Teams[0].PlayerCount = 4;
        _behavior.Teams[1].PlayerCount = 0;

        Assert.That(!_behavior.CanJoinTeam(0));
        Assert.That(_behavior.CanJoinTeam(1));
    }

    [Test]
    public void TestSmallDifference()
    {
        // 21-18 works but 22-18 doesnt
        _behavior.Teams[0].PlayerCount = 21;
        _behavior.Teams[1].PlayerCount = 18;

        Assert.That(_behavior.CanJoinTeam(0));
        Assert.That(_behavior.CanJoinTeam(1));
    }

    [Test]
    public void TestBarelyOverDifference()
    {
        // 21-18 works but 22-18 doesnt
        _behavior.Teams[0].PlayerCount = 22;
        _behavior.Teams[1].PlayerCount = 18;

        Assert.That(!_behavior.CanJoinTeam(0));
        Assert.That(_behavior.CanJoinTeam(1));
    }
}