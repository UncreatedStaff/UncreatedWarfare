using NUnit.Framework;
using Steamworks;
using System.ComponentModel.Design;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Tests;

public class KitLimiterTests
{
    private IPlayerService _playerService;
    private Team team1;
    private Team team2;

    [SetUp]
    public void Setup()
    {
        _playerService = new NullPlayerService(new ServiceContainer());
        
        team1 = new Team
        {
            Faction = new FactionInfo("id1", "name1", "abb1", null, Color.red, null, "id1"),
            Id = 1
        };
        team2 = new Team
        {
            Faction = new FactionInfo("id2", "name2", "abb2", null, Color.yellow, null, "id2"),
            Id = 2
        };
    }
    
    public bool IsLimited(int allowedPerXUsers, int currentUsers, int totalPossibleUsers)
    {
        int kitsAllowed = totalPossibleUsers / allowedPerXUsers + 1;
        return currentUsers + 1 > kitsAllowed;
    }
    [Test]
    public void TestNoCurrentUsers()
    {
        Assert.False(IsLimited(3, 0, 1)); 
        Assert.False(IsLimited(3, 0, 2)); 
        Assert.False(IsLimited(3, 0, 3)); 
        Assert.False(IsLimited(3, 0, 4)); 
        Assert.False(IsLimited(3, 0, 6));
        Assert.False(IsLimited(3, 0, 8));
        
    }
    [Test]
    public void TestOneCurrentUser()
    {
        Assert.True(IsLimited(3, 1, 1)); 
        Assert.True(IsLimited(3, 1, 2)); 
        Assert.False(IsLimited(3, 1, 3)); 
        Assert.False(IsLimited(3, 1, 4)); 
        Assert.False(IsLimited(3, 1, 5));
        Assert.False(IsLimited(3, 1, 6));
        Assert.False(IsLimited(3, 1, 8));   
    }
    [Test]
    public void TestTwoCurrentUsers()
    {
        Assert.True(IsLimited(3, 2, 1)); 
        Assert.True(IsLimited(3, 2, 2)); 
        Assert.True(IsLimited(3, 2, 3)); 
        Assert.True(IsLimited(3, 2, 4)); 
        Assert.True(IsLimited(3, 2, 5));
        Assert.False(IsLimited(3, 2, 6));
        Assert.False(IsLimited(3, 2, 7));
        Assert.False(IsLimited(3, 2, 8));     
        Assert.False(IsLimited(3, 2, 9));     
    }
}