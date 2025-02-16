using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Thrown when an asset isn't found from configuration.
/// </summary>
public class AssetNotFoundException : Exception
{
    public AssetNotFoundException() : base("Asset not found.") { }

    public AssetNotFoundException(string propertyName) : base($"Asset not found for property \"{propertyName}\".") { }

    public AssetNotFoundException(IAssetLink<Asset> asset) : base(asset.Guid != Guid.Empty
        ? $"Asset not found: {{{asset.Guid:N}}}."
        : $"Asset not found: ({AssetUtility.GetAssetCategory(AssetLink.GetAssetType(asset))}/{asset.Id:D}).") { }

    public AssetNotFoundException(IAssetContainer asset) : base(asset.Guid != Guid.Empty
        ? $"Asset not found: {{{asset.Guid:N}}}."
        : $"Asset not found: ({asset.Id:D}).") { }

    public AssetNotFoundException(IAssetLink<Asset> asset, string propertyName) : base(asset.Guid != Guid.Empty
        ? $"Asset not found for property \"{propertyName}\": {{{asset.Guid:N}}}."
        : $"Asset not found for property \"{propertyName}\": ({AssetUtility.GetAssetCategory(AssetLink.GetAssetType(asset))}/{asset.Id:D}).") { }

    public AssetNotFoundException(IAssetContainer asset, string propertyName) : base(asset.Guid != Guid.Empty
        ? $"Asset not found for property \"{propertyName}\": {{{asset.Guid:N}}}."
        : $"Asset not found for property \"{propertyName}\": ({asset.Id:D}).") { }
}