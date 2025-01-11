using NUnit.Framework;
using System;
using System.Text.Json;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tests;

public class JsonUtilityTests
{
    [Test]
    public void TestSkipToPropertyStartingBeforeObjectStart()
    {
        ReadOnlySpan<byte> str = """
                                 {
                                     "Property": true
                                 }
                                 """u8;

        Utf8JsonReader reader = new Utf8JsonReader(str);

        Assert.That(JsonUtility.SkipToProperty(ref reader, "Property"), Is.True);

        Assert.That(reader.TokenType, Is.EqualTo(JsonTokenType.True));
    }

    [Test]
    public void TestSkipToPropertyStartingAtObjectStart()
    {
        ReadOnlySpan<byte> str = """
                                 {
                                     "Property": true
                                 }
                                 """u8;

        Utf8JsonReader reader = new Utf8JsonReader(str);

        reader.Read();

        Assert.That(JsonUtility.SkipToProperty(ref reader, "Property"), Is.True);

        Assert.That(reader.TokenType, Is.EqualTo(JsonTokenType.True));
    }

    [Test]
    public void TestDontOverReadStartingBeforeObjectStart()
    {
        ReadOnlySpan<byte> str = """
                                 [
                                     {
                                         "NotProperty": true
                                     },
                                     {
                                         "Property": true
                                     }
                                 ]
                                 """u8;

        Utf8JsonReader reader = new Utf8JsonReader(str);
        reader.Read();

        Assert.That(JsonUtility.SkipToProperty(ref reader, "Property"), Is.False);
    }

    [Test]
    public void TestDontOverReadStartingAtObjectStart()
    {
        ReadOnlySpan<byte> str = """
                                 [
                                     {
                                         "NotProperty": true
                                     },
                                     {
                                         "Property": true
                                     }
                                 ]
                                 """u8;

        Utf8JsonReader reader = new Utf8JsonReader(str);

        reader.Read();
        reader.Read();

        Assert.That(JsonUtility.SkipToProperty(ref reader, "Property"), Is.False);
    }
}
