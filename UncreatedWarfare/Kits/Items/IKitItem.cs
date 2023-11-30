using SDG.Unturned;
using System;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;

public interface IKitItem : ICloneable, IComparable, IEquatable<IKitItem>
{
    public uint PrimaryKey { get; set; }
    ItemAsset? GetItem(Kit? kit, FactionInfo? targetTeam, out byte amount, out byte[] state);
    KitItemModel CreateModel(Kit kit);
    void WriteToModel(KitItemModel model);
}
