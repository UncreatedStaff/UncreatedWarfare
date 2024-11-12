using System;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;

public interface IKitItem : ICloneable, IComparable, IEquatable<IKitItem>
{
    public uint PrimaryKey { get; set; }
    ItemAsset? GetItem(Kit? kit, Team targetTeam, out byte amount, out byte[] state, AssetRedirectService assetRedirectService, IFactionDataStore factionDataStore);
    KitItemModel CreateModel(Kit kit);
    void WriteToModel(KitItemModel model);
}
