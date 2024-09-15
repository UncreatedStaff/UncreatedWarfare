using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Kits.Items;
public interface ISpecificKitItem : IKitItem
{
    UnturnedAssetReference Item { get; }

    [JsonConverter(typeof(ByteArrayJsonConverter))]
    byte[] State { get; }
}