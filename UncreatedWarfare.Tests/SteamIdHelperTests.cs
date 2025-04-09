using NUnit.Framework;
using Steamworks;
using System.Threading.Tasks;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tests;

/// <summary>Unit tests for <see cref="SteamIdHelper"/>.</summary>
public class SteamIdHelperTests
{
    [Test]
    [TestCase("STEAM_0:1:153830640")]
    [TestCase("[U:1:307661281]")]
    [TestCase("76561198267927009")]
    [TestCase("1100001125689e1")]
    [TestCase("307661281")]
    public void TestParseCSteamID(string steamIdInput)
    {
        if (!SteamIdHelper.TryParseSteamId(steamIdInput, out CSteamID steamId))
            Assert.Fail();

        Assert.That(steamId.m_SteamID, Is.EqualTo(76561198267927009ul));
    }

    [Test]
    [TestCase("https://steamcommunity.com/profiles/76561198267927009")]
    [TestCase("http://steamcommunity.com/profiles/76561198267927009")]
    [TestCase("steamcommunity.com/profiles/76561198267927009")]
    [TestCase("https://www.steamcommunity.com/profiles/76561198267927009")]
    [TestCase("http://www.steamcommunity.com/profiles/76561198267927009")]
    [TestCase("www.steamcommunity.com/profiles/76561198267927009/random/path?query=none")]
    [TestCase("https://steamcommunity.com/profiles/76561198267927009/random/path?query=none")]
    [TestCase("http://steamcommunity.com/profiles/76561198267927009/random/path?query=none")]
    [TestCase("steamcommunity.com/profiles/76561198267927009/random/path?query=none")]
    [TestCase("https://www.steamcommunity.com/profiles/76561198267927009/random/path?query=none")]
    [TestCase("http://www.steamcommunity.com/profiles/76561198267927009/random/path?query=none")]
    [TestCase("www.steamcommunity.com/profiles/76561198267927009/random/path?query=none")]
    public async Task TestParseCSteamIDFromBasicUrl(string basicUrl)
    {
        CSteamID? steamId = await SteamIdHelper.TryParseSteamIdOrUrl(basicUrl);

        Assert.That(steamId, Is.Not.Null);
        Assert.That(steamId.Value.m_SteamID, Is.EqualTo(76561198267927009ul));
    }

    [Test]
    [TestCase("https://steamcommunity.com/id/blazingflamegames")]
    [TestCase("http://steamcommunity.com/id/blazingflamegames")]
    [TestCase("steamcommunity.com/id/blazingflamegames")]
    [TestCase("https://www.steamcommunity.com/id/blazingflamegames")]
    [TestCase("http://www.steamcommunity.com/id/blazingflamegames")]
    [TestCase("www.steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("https://steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("http://steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("https://www.steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("http://www.steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("www.steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    public async Task TestParseCSteamIDFromCustomUrl(string customUrl)
    {
        CSteamID? steamId = await SteamIdHelper.TryParseSteamIdOrUrl(customUrl);

        Assert.That(steamId, Is.Not.Null);
        Assert.That(steamId.Value.m_SteamID, Is.EqualTo(76561198267927009ul));
    }
}