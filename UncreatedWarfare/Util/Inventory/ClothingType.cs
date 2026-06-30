using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Util.Inventory;

[Translatable("Clothing Slot", Description = "A clothing slot in the player's inventory.")]
public enum ClothingType : byte
{
    Shirt,
    Pants,
    Vest,
    Hat,
    Mask,
    Backpack,
    Glasses
}