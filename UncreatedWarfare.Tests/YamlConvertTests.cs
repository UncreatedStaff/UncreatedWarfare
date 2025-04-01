using NUnit.Framework;
using SDG.Unturned;
using System;
using Uncreated.Warfare.Configuration;
using YamlDotNet.Serialization;

namespace Uncreated.Warfare.Tests;

internal class YamlConvertTests
{
    private ISerializer _serializer;
    private IDeserializer _deserializer;
    [SetUp]
    public void SetUpTest()
    {
        _serializer = new SerializerBuilder()
            .DisableAliases()
            .WithTypeConverter(new AssetLinkYamlConverter()) // add more type converters as we go
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithTypeConverter(new AssetLinkYamlConverter())
            .Build();
    }
    [Test]
    public void TestAssetLink()
    {
        IAssetLink<VehicleAsset> asset = AssetLink.Create<VehicleAsset>(Guid.NewGuid());

        string yaml = _serializer.Serialize(asset);
        var deserialized = _deserializer.Deserialize<IAssetLink<VehicleAsset>>(yaml);

        Assert.AreEqual(asset.GUID, deserialized.GUID);
    }
}