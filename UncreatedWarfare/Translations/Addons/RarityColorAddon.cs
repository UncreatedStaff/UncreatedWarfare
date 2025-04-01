using System;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations.Addons;
public sealed class RarityColorAddon : IArgumentAddon
{
    public static RarityColorAddon Instance { get; } = new RarityColorAddon();

    private static readonly IArgumentAddon[] InstanceArray = [ Instance ];
    public string DisplayName => "Asset Rarity Color";
    private RarityColorAddon() { }
    
    public static string Apply(string text, Asset? asset, ITranslationValueFormatter formatter, in ValueFormatParameters args)
    {
        EItemRarity rarity = asset switch
        {
            ItemAsset item => item.rarity,
            VehicleAsset veh => veh.rarity,
            _ => EItemRarity.COMMON
        };

        return formatter.Colorize(asset?.FriendlyName ?? text, ItemTool.getRarityColorUI(rarity), args.Options);
    }

    public string ApplyAddon(ITranslationValueFormatter formatter, string text, TypedReference value, in ValueFormatParameters args)
    {
        Asset? asset = TypedReference.ToObject(value) as Asset;
        return Apply(text, asset, formatter, in args);
    }

    public static implicit operator ArgumentFormat(RarityColorAddon addon) => ReferenceEquals(addon, Instance) ? new ArgumentFormat(InstanceArray) : new ArgumentFormat(addon);
}