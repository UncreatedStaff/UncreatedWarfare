using System;
using System.ComponentModel;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Util.Inventory;

/// <summary>
/// Enumerate through a player's clothing slots. It does not skip empty slots.
/// </summary>
public struct EquippedClothingIterator : IEnumerable<ClothingItem>, IEnumerator<ClothingItem>
{
    private ClothingItem _current;

    /// <inheritdoc />
    public readonly ClothingItem Current => _current;

    public EquippedClothingIterator(Player player)
    {
        _current = new ClothingItem(player.clothing);
    }

    public EquippedClothingIterator(PlayerClothing clothing)
    {
        _current = new ClothingItem(clothing);
    }

    public EquippedClothingIterator(WarfarePlayer player)
    {
        _current = new ClothingItem(player.UnturnedPlayer.clothing);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public EquippedClothingIterator GetEnumerator()
    {
        EquippedClothingIterator copy = this;
        copy._current.Type = (ClothingType)byte.MaxValue;
        return copy;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        unchecked
        {
            ++_current.Type;
            return _current.Type <= ClothingType.Glasses;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _current.Type = (ClothingType)byte.MaxValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Dispose() { }

    /// <inheritdoc />
    object IEnumerator.Current => Current;
    IEnumerator<ClothingItem> IEnumerable<ClothingItem>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Helper that normalizes the different code paths for each clothing type into one structure.
/// </summary>
public struct ClothingItem
{
    /// <summary>
    /// Total number of clothing types in Unturned.
    /// </summary>
    public const int Count = 7;

    private static readonly Page[] StorageTable =
    [
        Page.Shirt,
        Page.Pants,
        Page.Vest,
        (Page)byte.MaxValue,
        (Page)byte.MaxValue,
        Page.Backpack,
        (Page)byte.MaxValue
    ];

    private readonly PlayerClothing? _clothing;

    /// <summary>
    /// The type of clothing this helper is referencing.
    /// </summary>
    public ClothingType Type;

    /// <summary>
    /// A bit-mask including only this clothing slot's bit.
    /// </summary>
    public readonly byte Flag => (byte)(1 << (int)Type);

    /// <summary>
    /// The asset currently being worn by the player.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The player was not configured when this struct was created, or it was given an invalid enum value for the clothing type.
    /// </exception>
    public readonly ItemClothingAsset? Asset =>
        _clothing is null ? throw new InvalidOperationException("Clothing not set") : Type switch
        {
            ClothingType.Shirt => _clothing.shirtAsset,
            ClothingType.Pants => _clothing.pantsAsset,
            ClothingType.Vest => _clothing.vestAsset,
            ClothingType.Hat => _clothing.hatAsset,
            ClothingType.Mask => _clothing.maskAsset,
            ClothingType.Backpack => _clothing.backpackAsset,
            ClothingType.Glasses => _clothing.glassesAsset,
            _ => throw new InvalidOperationException("Type out of range.")
        };

    /// <summary>
    /// The quality of the clothing being worn by the player.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The player was not configured when this struct was created, or it was given an invalid enum value for the clothing type.
    /// </exception>
    public readonly byte Quality =>
        _clothing is null ? throw new InvalidOperationException("Clothing not set") : Type switch
        {
            ClothingType.Shirt => _clothing.shirtQuality,
            ClothingType.Pants => _clothing.pantsQuality,
            ClothingType.Vest => _clothing.vestQuality,
            ClothingType.Hat => _clothing.hatQuality,
            ClothingType.Mask => _clothing.maskQuality,
            ClothingType.Backpack => _clothing.backpackQuality,
            ClothingType.Glasses => _clothing.glassesQuality,
            _ => throw new InvalidOperationException("Type out of range.")
        };

    /// <summary>
    /// The state array of the clothing being worn by the player.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The player was not configured when this struct was created, or it was given an invalid enum value for the clothing type.
    /// </exception>
    public readonly byte[] State =>
        _clothing is null ? throw new InvalidOperationException("Clothing not set") : Type switch
        {
            ClothingType.Shirt => _clothing.shirtState,
            ClothingType.Pants => _clothing.pantsState,
            ClothingType.Vest => _clothing.vestState,
            ClothingType.Hat => _clothing.hatState,
            ClothingType.Mask => _clothing.maskState,
            ClothingType.Backpack => _clothing.backpackState,
            ClothingType.Glasses => _clothing.glassesState,
            _ => throw new InvalidOperationException("Type out of range.")
        };

    /// <summary>
    /// If this clothing type has storage.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">
    /// This struct was given an invalid enum value for the clothing type when it was created.
    /// </exception>
    public readonly bool HasStorage => StorageTable[(int)Type] < Page.Storage;

    /// <summary>
    /// The page this clothing type is stored in.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">
    /// This struct was given an invalid enum value for the clothing type when it was created.
    /// </exception>
    public readonly Page StoragePage => StorageTable[(int)Type];

    /// <summary>
    /// The acceptable base type for assets of this clothing type.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This struct was given an invalid enum value for the clothing type when it was created.
    /// </exception>
    public readonly Type AssetType =>
        Type switch
        {
            ClothingType.Shirt => typeof(ItemShirtAsset),
            ClothingType.Pants => typeof(ItemPantsAsset),
            ClothingType.Vest => typeof(ItemVestAsset),
            ClothingType.Hat => typeof(ItemHatAsset),
            ClothingType.Mask => typeof(ItemMaskAsset),
            ClothingType.Backpack => typeof(ItemBackpackAsset),
            ClothingType.Glasses => typeof(ItemGlassesAsset),
            _ => throw new InvalidOperationException("Type out of range.")
        };

    public ClothingItem(ClothingType type)
    {
        if (type > ClothingType.Glasses)
            throw new ArgumentOutOfRangeException(nameof(type));
        Type = type;
    }

    public ClothingItem(PlayerClothing? clothing, ClothingType type)
    {
        if (type > ClothingType.Glasses)
            throw new ArgumentOutOfRangeException(nameof(type));
        _clothing = clothing;
        Type = type;
    }

    internal ClothingItem(PlayerClothing clothing)
    {
        _clothing = clothing;
        Type = (ClothingType)byte.MaxValue;
    }

    /// <summary>
    /// Determines whether or not an <paramref name="asset"/> is the correct type to be worn in this clothing type.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This struct was given an invalid enum value for the clothing type when it was created.
    /// </exception>
    public readonly bool ValidAsset(Asset? asset)
    {
        return Type switch
        {
            ClothingType.Shirt => asset is ItemShirtAsset,
            ClothingType.Pants => asset is ItemPantsAsset,
            ClothingType.Vest => asset is ItemVestAsset,
            ClothingType.Hat => asset is ItemHatAsset,
            ClothingType.Mask => asset is ItemMaskAsset,
            ClothingType.Backpack => asset is ItemBackpackAsset,
            ClothingType.Glasses => asset is ItemGlassesAsset,
            _ => throw new InvalidOperationException("Type out of range.")
        };
    }

    /// <exception cref="InvalidOperationException">
    /// The player was not configured when this struct was created, or it was given an invalid enum value for the clothing type.
    /// </exception>
    public readonly void AskWear(ItemAsset? asset, byte quality, byte[] state, bool playEffect)
    {
        GameThread.AssertCurrent();

        if (_clothing == null)
            throw new InvalidOperationException("Clothing not set");

        if (asset == null)
        {
            switch (Type)
            {
                case ClothingType.Shirt:
                    _clothing.askWearShirt(null, quality, state, playEffect);
                    return;

                case ClothingType.Pants:
                    _clothing.askWearPants(null, quality, state, playEffect);
                    return;

                case ClothingType.Vest:
                    _clothing.askWearVest(null, quality, state, playEffect);
                    return;

                case ClothingType.Hat:
                    _clothing.askWearHat(null, quality, state, playEffect);
                    return;

                case ClothingType.Mask:
                    _clothing.askWearMask(null, quality, state, playEffect);
                    return;

                case ClothingType.Backpack:
                    _clothing.askWearBackpack(null, quality, state, playEffect);
                    return;

                case ClothingType.Glasses:
                    _clothing.askWearGlasses(null, quality, state, playEffect);
                    return;

                default:
                    throw new InvalidOperationException("Type out of range.");
            }
        }

        switch (Type)
        {
            case ClothingType.Shirt:
                if (asset is not ItemShirtAsset shirt)
                    throw new ArgumentException("Expected ItemShirtAsset.", nameof(asset));

                _clothing.askWearShirt(shirt, quality, state, playEffect);
                break;

            case ClothingType.Pants:
                if (asset is not ItemPantsAsset pants)
                    throw new ArgumentException("Expected ItemPantsAsset.", nameof(asset));

                _clothing.askWearPants(pants, quality, state, playEffect);
                break;

            case ClothingType.Vest:
                if (asset is not ItemVestAsset vest)
                    throw new ArgumentException("Expected ItemVestAsset.", nameof(asset));

                _clothing.askWearVest(vest, quality, state, playEffect);
                break;

            case ClothingType.Hat:
                if (asset is not ItemHatAsset hat)
                    throw new ArgumentException("Expected ItemHatAsset.", nameof(asset));

                _clothing.askWearHat(hat, quality, state, playEffect);
                break;

            case ClothingType.Mask:
                if (asset is not ItemMaskAsset mask)
                    throw new ArgumentException("Expected ItemMaskAsset.", nameof(asset));

                _clothing.askWearMask(mask, quality, state, playEffect);
                break;

            case ClothingType.Backpack:
                if (asset is not ItemBackpackAsset backpack)
                    throw new ArgumentException("Expected ItemBackpackAsset.", nameof(asset));

                _clothing.askWearBackpack(backpack, quality, state, playEffect);
                break;

            case ClothingType.Glasses:
                if (asset is not ItemGlassesAsset glasses)
                    throw new ArgumentException("Expected ItemGlassesAsset.", nameof(asset));

                _clothing.askWearGlasses(glasses, quality, state, playEffect);
                break;

            default:
                throw new InvalidOperationException("Type out of range.");
        }
    }
}