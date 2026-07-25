using NUnit.Framework;
using Steamworks;
using Stripe;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Tests.Utility;
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
    [TestCase("https://steamcommunity.com/id/blazingflamegames")]
    [TestCase("http://steamcommunity.com/id/blazingflamegames")]
    [TestCase("steamcommunity.com/id/blazingflamegames")]
    [TestCase("https://www.steamcommunity.com/id/blazingflamegames")]
    [TestCase("http://www.steamcommunity.com/id/blazingflamegames")]
    [TestCase("https://steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("http://steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("https://www.steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("http://www.steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    [TestCase("www.steamcommunity.com/id/blazingflamegames/random/path?query=none")]
    public async Task TestParseCSteamIDFromUrl(string basicUrl)
    {
        // no Steam API service
        CSteamID? steamId = await SteamIdHelper.TryParseSteamIdOrUrl(basicUrl, null);

        Assert.That(steamId, Is.Not.Null);
        Assert.That(steamId.Value.m_SteamID, Is.EqualTo(76561198267927009ul));

        const string configPath = @"C:\SteamCMD\steamapps\common\U3DS\Servers\UncreatedSeason4\Warfare\System Config.yml";
        if (System.IO.File.Exists(configPath))
        {
            // with Steam API service
            string fileContents = System.IO.File.ReadAllText(configPath);
            Match match = Regex.Match(fileContents, @"steam_api_key\: ""([^""]*)""", RegexOptions.CultureInvariant);
            string steamApiKey = match.Groups[1].Value;

            ISteamApiService service = new TestSteamApiService(steamApiKey);

            steamId = await SteamIdHelper.TryParseSteamIdOrUrl(basicUrl, service);

            Assert.That(steamId, Is.Not.Null);
            Assert.That(steamId.Value.m_SteamID, Is.EqualTo(76561198267927009ul));
        }
        else
        {
            Assert.Pass("Steam API key not found.");
        }
    }
}