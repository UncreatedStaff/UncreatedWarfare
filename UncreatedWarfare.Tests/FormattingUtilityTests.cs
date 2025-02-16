using NUnit.Framework;
using System.Globalization;
using Uncreated.Warfare.Util;
using UnityEngine;

namespace Uncreated.Warfare.Tests;
/// <summary>Unit tests for <see cref="FormattingUtility"/>.</summary>

public class FormattingUtilityTests
{
    private const float Tolerance = 0.001f;

    [Test]
    [TestCase("#0066ff99")]
    [TestCase("0066ff99")]
    [TestCase("rgb(0, 102, 255, 153)")]
    [TestCase("(0, 102, 255, 153)")]
    [TestCase("hsv(216, 100, 100, 153)")]
    public void TestParseColor(string colorInput)
    {
        Color value = new Color32(0, 102, 255, 153);
        if (!HexStringHelper.TryParseColor(colorInput, CultureInfo.InvariantCulture, out Color color))
            Assert.Fail();

        Assert.That(color.r, Is.InRange(value.r - Tolerance, value.r + Tolerance));
        Assert.That(color.g, Is.InRange(value.g - Tolerance, value.g + Tolerance));
        Assert.That(color.b, Is.InRange(value.b - Tolerance, value.b + Tolerance));
        Assert.That(color.a, Is.InRange(value.a - Tolerance, value.a + Tolerance));
    }
    [Test]
    [TestCase("#0066ff")]
    [TestCase("0066ff")]
    [TestCase("rgb(0, 102, 255)")]
    [TestCase("(0, 102, 255)")]
    [TestCase("hsv(216, 100, 100)")]
    public void TestParseColorNoAlpha(string colorInput)
    {
        Color value = new Color32(0, 102, 255, 255);
        if (!HexStringHelper.TryParseColor(colorInput, CultureInfo.InvariantCulture, out Color color))
            Assert.Fail();


        Assert.That(color.r, Is.InRange(value.r - Tolerance, value.r + Tolerance));
        Assert.That(color.g, Is.InRange(value.g - Tolerance, value.g + Tolerance));
        Assert.That(color.b, Is.InRange(value.b - Tolerance, value.b + Tolerance));
        Assert.That(color.a, Is.InRange(value.a - Tolerance, value.a + Tolerance));
    }
    [Test]
    [TestCase("#0066ff99")]
    [TestCase("0066ff99")]
    [TestCase("rgb(0, 102, 255, 153)")]
    [TestCase("(0, 102, 255, 153)")]
    [TestCase("hsv(216, 100, 100, 153)")]
    public void TestParseColor32(string colorInput)
    {
        Color32 value = new Color32(0, 102, 255, 153);
        if (!HexStringHelper.TryParseColor32(colorInput, CultureInfo.InvariantCulture, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
    [Test]
    [TestCase("#0066ff")]
    [TestCase("0066ff")]
    [TestCase("rgb(0, 102, 255)")]
    [TestCase("(0, 102, 255)")]
    [TestCase("hsv(216, 100, 100)")]
    public void TestParseColor32NoAlpha(string colorInput)
    {
        Color32 value = new Color32(0, 102, 255, 255);
        if (!HexStringHelper.TryParseColor32(colorInput, CultureInfo.InvariantCulture, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
    [Test]
    [TestCase("#0066ff99")]
    [TestCase("0066ff99")]
    public void TestParse8HexColor32(string colorInput)
    {
        Color32 value = new Color32(0, 102, 255, 153);
        if (!HexStringHelper.TryParseHexColor32(colorInput, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
    [Test]
    [TestCase("#0066ff")]
    [TestCase("0066ff")]
    public void TestParse6HexColor32(string colorInput)
    {
        Color32 value = new Color32(0, 102, 255, 255);
        if (!HexStringHelper.TryParseHexColor32(colorInput, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
    [Test]
    [TestCase("#ac49")]
    [TestCase("ac49")]
    public void TestParse4HexColor32(string colorInput)
    {
        Color32 value = new Color32(170, 204, 68, 153);
        if (!HexStringHelper.TryParseHexColor32(colorInput, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
    [Test]
    [TestCase("#ac4")]
    [TestCase("ac4")]
    public void TestParse3HexColor32(string colorInput)
    {
        Color32 value = new Color32(170, 204, 68, 255);
        if (!HexStringHelper.TryParseHexColor32(colorInput, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
    [Test]
    [TestCase("#a9")]
    [TestCase("a9")]
    public void TestParse2HexColor32(string colorInput)
    {
        Color32 value = new Color32(170, 170, 170, 153);
        if (!HexStringHelper.TryParseHexColor32(colorInput, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
    [Test]
    [TestCase("#a")]
    [TestCase("a")]
    public void TestParse1HexColor32(string colorInput)
    {
        Color32 value = new Color32(170, 170, 170, 255);
        if (!HexStringHelper.TryParseHexColor32(colorInput, out Color32 color))
            Assert.Fail();

        Assert.AreEqual(value, color);
    }
}
