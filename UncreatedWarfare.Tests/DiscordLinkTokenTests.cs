using NUnit.Framework;
using System;
using Uncreated.Warfare.Moderation.Discord;

namespace Uncreated.Warfare.Tests;

internal class DiscordLinkTokenTests
{
    // tests for converting token input to a normalized abcd-efgh token.

    [Test]
    public void TestGenerateRandomToken()
    {
        string token = AccountLinkingService.GenerateRandomToken();

        Console.WriteLine(token);

        Assert.That(token.Length, Is.EqualTo(9));
        Assert.That(token[4], Is.EqualTo('-'));
        for (int i = 0; i < 4; ++i)
            Assert.That(char.IsLetterOrDigit(token[i]) && token[i] <= byte.MaxValue);
        for (int i = 5; i < 8; ++i)
            Assert.That(char.IsLetterOrDigit(token[i]) && token[i] <= byte.MaxValue);
    }

    [Test]
    [TestCase("aBcD-EfGh")]
    public void TestAlreadyNormalizedSameReference(string value)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(value, Is.EqualTo(normalized));

        Assert.That(ReferenceEquals(normalized, value), Is.True, "Normalized reference is the same as input.");
    }

    [Test]
    [TestCase("aBcDEfGh", "aBcD-EfGh")]
    public void TestNoDash(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }

    [Test]
    [TestCase("  aBcDEfGh  ", "aBcD-EfGh")]
    public void TestTrimmed(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }

    [Test]
    [TestCase("  aBcD-EfGh  ", "aBcD-EfGh")]
    public void TestTrimmedWithDash(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }

    [Test]
    [TestCase("  aBcD -  EfGh  ", "aBcD-EfGh")]
    public void TestTrimmedAtDash(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }

    [Test]
    [TestCase("  aBcD  EfGh  ", "aBcD-EfGh")]
    public void TestSpaceSeparated(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }

    [Test]
    [TestCase("aBcDEf-Gh", "aBcD-EfGh")]
    [TestCase("aB-cDEfGh", "aBcD-EfGh")]
    public void TestMisplacedDash(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }

    [Test]
    [TestCase("  aB  cDEfGh  ", "aBcD-EfGh")]
    [TestCase("  aBcDEf  Gh  ", "aBcD-EfGh")]
    public void TestSpaceMisplacedSeparated(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }

    [Test]
    [TestCase("  aBcDEf -  Gh  ", "aBcD-EfGh")]
    [TestCase("  aB -  cDEfGh  ", "aBcD-EfGh")]
    public void TestTrimmedAtMisplacedDash(string value, string expected)
    {
        string normalized = AccountLinkingService.NormalizeToken(value);

        Assert.That(expected, Is.EqualTo(normalized));
    }
}
