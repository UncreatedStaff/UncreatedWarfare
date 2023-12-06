using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SDG.Unturned;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_items")]
public class KitItemModel : ICloneable, IEquatable<KitItemModel>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    public Kit Kit { get; set; }

    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    [Required]
    public uint KitId { get; set; }
    public UnturnedAssetReference? Item { get; set; }
    public byte? X { get; set; }
    public byte? Y { get; set; }
    public byte? Rotation { get; set; }
    public Page? Page { get; set; }
    public ClothingType? ClothingSlot { get; set; }
    public RedirectType? Redirect { get; set; }

    [StringLength(36)]
    public string? RedirectVariant { get; set; }
    public byte? Amount { get; set; }

    [MaxLength(18)]
    [Column(TypeName = "varbinary(18)")]
    public byte[]? Metadata { get; set; }

    public KitItemModel() { }
    public KitItemModel(KitItemModel model)
    {
        Kit = model.Kit;
        KitId = model.KitId;
        Item = model.Item;
        X = model.X;
        Y = model.Y;
        Rotation = model.Rotation;
        Page = model.Page;
        ClothingSlot = model.ClothingSlot;
        Redirect = model.Redirect;
        RedirectVariant = model.RedirectVariant;
        Amount = model.Amount;
        Metadata = model.Metadata;
    }

    /// <exception cref="FormatException">Invalid data</exception>
    public IKitItem CreateRuntimeItem()
    {
        ClothingType type;

        bool hasPageData = X.HasValue && Y.HasValue && Page.HasValue;
        bool hasGuid = Item.HasValue;
        bool hasRedirect = Redirect.HasValue;
        bool hasClothingData = ClothingSlot.HasValue;
        if (!hasGuid && !hasRedirect)
            throw new FormatException("Item row must either have a GUID or a redirect type.");
        
        if (!hasPageData && !hasClothingData)
            throw new FormatException("Item row must either have page information or a clothing type.");

        if (hasGuid)
        {
            UnturnedAssetReference reference = Item!.Value;
            if (reference.Guid == Guid.Empty && reference.Id == 0)
                throw new FormatException("Item has an empty GUID and Id.");
            
            if (hasPageData)
            {
                byte x = X!.Value,
                     y = Y!.Value,
                     rot = Rotation.HasValue ? (byte)(Rotation.Value % 4) : (byte)0,
                     amt = Amount ?? 0;

                Page page = Page!.Value;
                if (page > Warfare.Kits.Items.Page.Area)
                    throw new FormatException($"Page out of range: {page}.");

                return new SpecificPageKitItem(Id, reference, x, y, rot, page, amt, Metadata ?? Array.Empty<byte>());
            }

            type = ClothingSlot!.Value;
            if (type > ClothingType.Glasses)
                throw new FormatException($"Clothing type out of range: {type}.");
            
            return new SpecificClothingKitItem(Id, reference, type, Metadata ?? Array.Empty<byte>());
        }

        RedirectType redirectType = Redirect!.Value;
        string? redirectVariant = RedirectVariant;

        if (hasPageData)
        {
            byte x = X!.Value,
                y = Y!.Value,
                rot = Rotation.HasValue ? (byte)(Rotation.Value % 4) : (byte)0;

            Page page = Page!.Value;
            if (page > Warfare.Kits.Items.Page.Area)
                throw new FormatException($"Page out of range: {page}.");

            return new AssetRedirectPageKitItem(Id, x, y, rot, page, redirectType, redirectVariant);
        }

        type = ClothingSlot!.Value;
        if (type > ClothingType.Glasses)
            throw new FormatException($"Clothing type out of range: {type}.");

        return new AssetRedirectClothingKitItem(Id, redirectType, type, redirectVariant);
    }
    public bool TryGetItemSize(out byte sizeX, out byte sizeY)
    {
        (sizeX, sizeY) = (1, 1);

        ItemAsset? asset;
        if (Item.HasValue)
            asset = Item.Value.GetAsset<ItemAsset>();
        else if (Redirect.HasValue)
            asset = TeamManager.GetRedirectInfo(Redirect.Value, RedirectVariant ?? string.Empty, null, null, out _, out _);
        else
            return false;

        if (asset == null)
            return false;

        (sizeX, sizeY) = (asset.size_x, asset.size_y);
        return true;
    }
    public object Clone()
    {
        return new KitItemModel(this);
    }

    public bool Equals(KitItemModel? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Id != other.Id
            || KitId != other.KitId
            || !(Item.HasValue ? other.Item.HasValue && other.Item.Value.Equals(Item.Value) : !other.Item.HasValue)
            || !(X.HasValue ? other.X.HasValue && other.X.Value == X.Value : !other.X.HasValue)
            || !(Y.HasValue ? other.Y.HasValue && other.Y.Value == Y.Value : !other.Y.HasValue)
            || !(Rotation.HasValue ? other.Rotation.HasValue && other.Rotation.Value == Rotation.Value : !other.Rotation.HasValue)
            || !(Page.HasValue ? other.Page.HasValue && other.Page.Value == Page.Value : !other.Page.HasValue)
            || !(ClothingSlot.HasValue ? other.ClothingSlot.HasValue && other.ClothingSlot.Value == ClothingSlot.Value : !other.ClothingSlot.HasValue)
            || !(Redirect.HasValue ? other.Redirect.HasValue && other.Redirect.Value == Redirect.Value : !other.Redirect.HasValue)
            || !string.Equals(RedirectVariant, other.RedirectVariant, StringComparison.OrdinalIgnoreCase)
            || !(Amount.HasValue ? other.Amount.HasValue && other.Amount.Value == Amount.Value : !other.Amount.HasValue))
        {
            return false;
        }

        byte[]? meta = other.Metadata;
        if (meta is not { Length: > 0 })
            return Metadata is not { Length: > 0 };
        if (Metadata is not { Length: > 0 } || meta.Length != Metadata.Length)
            return false;

        for (int i = 0; i < meta.Length; ++i)
        {
            if (meta[i] != Metadata[i])
                return false;
        }

        return true;

    }

    public override bool Equals(object? obj) => Equals(obj as KitItemModel);
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        HashCode hashCode = new HashCode();
        hashCode.Add(Id);
        hashCode.Add(Kit);
        hashCode.Add(KitId);
        hashCode.Add(Item);
        hashCode.Add(X);
        hashCode.Add(Y);
        hashCode.Add(Rotation);
        hashCode.Add(Page);
        hashCode.Add(ClothingSlot);
        hashCode.Add(Redirect);
        hashCode.Add(RedirectVariant);
        hashCode.Add(Amount);
        hashCode.Add(Metadata);
        return hashCode.ToHashCode();
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }

    public static bool operator ==(KitItemModel? left, KitItemModel? right) => Equals(left, right);
    public static bool operator !=(KitItemModel? left, KitItemModel? right) => !Equals(left, right);
}
