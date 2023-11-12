using System;
using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;

public interface IKitItem : IListItem, ICloneable, IComparable, IEquatable<IKitItem>
{
    ItemAsset? GetItem(Kit? kit, FactionInfo? targetTeam, out byte amount, out byte[] state);
    KitItemModel CreateModel(Kit kit);
    void WriteToModel(KitItemModel model);
}
