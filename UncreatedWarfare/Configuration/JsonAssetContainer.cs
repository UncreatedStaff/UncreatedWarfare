using SDG.Unturned;
using System;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Configuration;
public class JsonAssetContainer<TAsset> : IAssetContainer where TAsset : Asset
{
    private readonly JsonAssetReference<TAsset> _ref;
    public Guid Guid => _ref.GetGuid();
    public ushort Id => _ref.GetId();
    public Asset? Asset => _ref.Asset;
    public JsonAssetContainer(JsonAssetReference<TAsset> assetReference)
    {
        _ref = assetReference;
    }
    public JsonAssetContainer(TAsset? asset)
    {
        _ref = new JsonAssetReference<TAsset>(asset!);
    }
}
