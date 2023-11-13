using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_items")]
public class KitItemModel : ICloneable
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

    public object Clone()
    {
        return new KitItemModel(this);
    }
}
