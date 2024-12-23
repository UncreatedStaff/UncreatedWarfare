using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.StateStorage;
public class BuildableStateSave
{
    required public IAssetLink<ItemPlaceableAsset> BuildableAsset { get; set; }
    required public string InertFriendlyName { get; set; }
    public string? FactionId { get; set; }
    required public string Base64State { get; set; }
}
