using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Teams;
public class FactionInfo : ICloneable, ITranslationArgument
{
    public const string UnknownTeamImgURL = "https://i.imgur.com/z0HE5P3.png";
    public const int FactionIDMaxCharLimit = 16;
    public const int FactionNameMaxCharLimit = 32;
    public const int FactionShortNameMaxCharLimit = 24;
    public const int FactionAbbreviationMaxCharLimit = 6;
    public const int FactionImageLinkMaxCharLimit = 128;

    public const string Admins = "admins";
    public const string USA = "usa";
    public const string Russia = "russia";
    public const string MEC = "mec";
    public const string Germany = "germany";
    public const string China = "china";
    public const string USMC = "usmc";
    public const string Soviet = "soviet";
    public const string Poland = "poland";
    public const string Militia = "militia";
    public const string Israel = "israel";
    public const string France = "france";
    public const string Canada = "canada";
    public const string SouthAfrica = "southafrica";
    public const string Mozambique = "mozambique";

    [JsonIgnore]
    public bool IsDefaultFaction { get; internal set; }

    [Obsolete("Africa was split into individual countries.")]
    public const string LegacyAfrica = "africa";

    private string _factionId;

    [JsonPropertyName("displayName")]
    public string Name { get; set; }

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("nameLocalization")]
    public TranslationList NameTranslations { get; set; }

    [JsonPropertyName("shortNameLocalization")]
    public TranslationList ShortNameTranslations { get; set; }

    [JsonPropertyName("abbreviationLocalization")]
    public TranslationList AbbreviationTranslations { get; set; }

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; }

    [JsonPropertyName("kitPrefix")]
    public string KitPrefix { get; set; }

    [JsonPropertyName("color")]
    public Color Color { get; set; }

    [JsonPropertyName("unarmed")]
    public uint? UnarmedKit { get; set; }

    [JsonPropertyName("flagImg")]
    public string FlagImageURL { get; set; }

    [JsonPropertyName("ammoSupplies")]
    public IAssetLink<ItemAsset>? Ammo { get; set; }

    [JsonPropertyName("buildingSupplies")]
    public IAssetLink<ItemAsset>? Build { get; set; }

    [JsonPropertyName("rallyPoint")]
    public IAssetLink<ItemBarricadeAsset>? RallyPoint { get; set; }

    [JsonPropertyName("radio")]
    public IAssetLink<ItemBarricadeAsset>? FOBRadio { get; set; }

    [JsonPropertyName("backpacks")]
    public AssetVariantDictionary<ItemBackpackAsset> Backpacks { get; set; }

    [JsonPropertyName("shirts")]
    public AssetVariantDictionary<ItemShirtAsset> Shirts { get; set; }

    [JsonPropertyName("pants")]
    public AssetVariantDictionary<ItemPantsAsset> Pants { get; set; }

    [JsonPropertyName("vests")]
    public AssetVariantDictionary<ItemVestAsset> Vests { get; set; }

    [JsonPropertyName("hats")]
    public AssetVariantDictionary<ItemHatAsset> Hats { get; set; }

    [JsonPropertyName("glasses")]
    public AssetVariantDictionary<ItemGlassesAsset> Glasses { get; set; }

    [JsonPropertyName("masks")]
    public AssetVariantDictionary<ItemMaskAsset> Masks { get; set; }

    [JsonPropertyName("tmProSpriteIndex")]
    public int? TMProSpriteIndex { get; set; }

    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }

    [JsonIgnore]
    public uint PrimaryKey { get; set; }

    [JsonIgnore]
    public string Sprite => "<sprite index=" + (TMProSpriteIndex.HasValue ? TMProSpriteIndex.Value.ToString(CultureInfo.InvariantCulture) : "0") + ">";

    [JsonPropertyName("factionId")]
    public string FactionId
    {
        get => _factionId;
        set
        {
            if (value.Length > FactionIDMaxCharLimit)
                throw new ArgumentException("Faction ID must be less than " + FactionIDMaxCharLimit + " characters.", "factionId");
            _factionId = value;
        }
    }

    public FactionInfo()
    {
        Backpacks = new AssetVariantDictionary<ItemBackpackAsset>(1);
        Shirts = new AssetVariantDictionary<ItemShirtAsset>(1);
        Pants = new AssetVariantDictionary<ItemPantsAsset>(1);
        Vests = new AssetVariantDictionary<ItemVestAsset>(1);
        Hats = new AssetVariantDictionary<ItemHatAsset>(1);
        Glasses = new AssetVariantDictionary<ItemGlassesAsset>(0);
        Masks = new AssetVariantDictionary<ItemMaskAsset>(0);

        NameTranslations = new TranslationList(4);
        ShortNameTranslations = new TranslationList(4);
        AbbreviationTranslations = new TranslationList(4);
    }

    public FactionInfo(string factionId, string name, string abbreviation, string? shortName, Color color, uint? unarmedKit, string kitPrefix, string flagImage = UnknownTeamImgURL)
        : this()
    {
        FactionId = factionId;
        Name = name;
        Abbreviation = abbreviation;
        ShortName = shortName;
        Color = color;
        UnarmedKit = unarmedKit;
        FlagImageURL = flagImage;
        KitPrefix = kitPrefix;
    }

    public FactionInfo(Faction model)
    {
        PrimaryKey = model.Key;
        FactionId = model.InternalName;
        Name = model.Name;
        Abbreviation = model.Abbreviation;
        ShortName = model.ShortName;
        Color = HexStringHelper.TryParseColor(model.HexColor, CultureInfo.InvariantCulture, out Color color) ? color : Color.white;
        UnarmedKit = model.UnarmedKitId;
        FlagImageURL = model.FlagImageUrl;
        Emoji = model.Emoji;
        KitPrefix = model.KitPrefix;
        TMProSpriteIndex = model.SpriteIndex is < 0 ? null : model.SpriteIndex;
        Backpacks = new AssetVariantDictionary<ItemBackpackAsset>(1);
        Shirts = new AssetVariantDictionary<ItemShirtAsset>(1);
        Pants = new AssetVariantDictionary<ItemPantsAsset>(1);
        Vests = new AssetVariantDictionary<ItemVestAsset>(1);
        Hats = new AssetVariantDictionary<ItemHatAsset>(1);
        Glasses = new AssetVariantDictionary<ItemGlassesAsset>(1);
        Masks = new AssetVariantDictionary<ItemMaskAsset>(1);

        NameTranslations = new TranslationList(4);
        ShortNameTranslations = new TranslationList(4);
        AbbreviationTranslations = new TranslationList(4);

        ApplyAssets(model);
        ApplyTranslations(model);
    }

    public void CloneFrom(FactionInfo model)
    {
        Name = model.Name;
        PrimaryKey = model.PrimaryKey;
        Abbreviation = model.Abbreviation;
        ShortName = model.ShortName;
        Color = model.Color;
        UnarmedKit = model.UnarmedKit;
        FlagImageURL = model.FlagImageURL;
        Emoji = model.Emoji;
        KitPrefix = model.KitPrefix;
        TMProSpriteIndex = model.TMProSpriteIndex;
        Backpacks = model.Backpacks.Clone();
        Shirts = model.Shirts.Clone();
        Pants = model.Pants.Clone();
        Vests = model.Vests.Clone();
        Hats = model.Hats.Clone();
        Glasses = model.Glasses.Clone();
        Masks = model.Masks.Clone();
        Ammo = model.Ammo;
        Build = model.Build;
        RallyPoint = model.RallyPoint;
        FOBRadio = model.FOBRadio;
    }

    internal Faction CreateModel(ICachableLanguageDataStore languageDataStore)
    {
        Faction faction = new Faction
        {
            Key = PrimaryKey,
            InternalName = FactionId,
            Name = Name,
            ShortName = ShortName,
            Abbreviation = Abbreviation,
            HexColor = HexStringHelper.FormatHexColor(Color),
            UnarmedKitId = UnarmedKit,
            FlagImageUrl = FlagImageURL,
            Emoji = Emoji,
            KitPrefix = KitPrefix,
            SpriteIndex = TMProSpriteIndex,
            Translations = new List<FactionLocalization>(Math.Max(NameTranslations?.Count ?? 0, Math.Max(ShortNameTranslations?.Count ?? 0, AbbreviationTranslations?.Count ?? 0))),
            Assets = new List<FactionAsset>(8)
        };

        if (NameTranslations != null)
        {
            foreach (KeyValuePair<string, string> kvp in NameTranslations)
            {
                FactionLocalization? loc = faction.Translations.FirstOrDefault(x => x.Language.Code.Equals(kvp.Key, StringComparison.Ordinal));
                if (languageDataStore.GetInfoCached(kvp.Key) is not { Key: not 0 } lang)
                    continue;
                if (loc == null)
                {
                    loc = new FactionLocalization
                    {
                        Name = kvp.Value,
                        Faction = faction,
                        FactionId = faction.Key,
                        LanguageId = lang.Key,
                        Language = lang
                    };
                    faction.Translations.Add(loc);
                }
                else loc.Name = kvp.Value;
            }
        }
        if (ShortNameTranslations != null)
        {
            foreach (KeyValuePair<string, string> kvp in ShortNameTranslations)
            {
                FactionLocalization? loc = faction.Translations.FirstOrDefault(x => x.Language.Code.Equals(kvp.Key, StringComparison.Ordinal));
                if (languageDataStore.GetInfoCached(kvp.Key) is not { Key: not 0 } lang)
                    continue;
                if (loc == null)
                {
                    loc = new FactionLocalization
                    {
                        ShortName = kvp.Value,
                        Faction = faction,
                        FactionId = faction.Key,
                        LanguageId = lang.Key,
                        Language = lang
                    };
                    faction.Translations.Add(loc);
                }
                else loc.ShortName = kvp.Value;
            }
        }
        if (AbbreviationTranslations != null)
        {
            foreach (KeyValuePair<string, string> kvp in AbbreviationTranslations)
            {
                FactionLocalization? loc = faction.Translations.FirstOrDefault(x => x.Language.Code.Equals(kvp.Key, StringComparison.Ordinal));
                if (languageDataStore.GetInfoCached(kvp.Key) is not { Key: not 0 } lang)
                    continue;
                if (loc == null)
                {
                    loc = new FactionLocalization
                    {
                        Abbreviation = kvp.Value,
                        Faction = faction,
                        FactionId = faction.Key,
                        LanguageId = lang.Key,
                        Language = lang
                    };
                    faction.Translations.Add(loc);
                }
                else loc.Abbreviation = kvp.Value;
            }
        }

        foreach (FactionLocalization loc in faction.Translations)
            loc.Language = null!;

        if (Ammo is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromAssetLink(Ammo),
                Faction = faction,
                FactionId = faction.Key,
                Redirect = RedirectType.AmmoSupply
            });
        }
        if (Build is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromAssetLink(Build),
                Faction = faction,
                FactionId = faction.Key,
                Redirect = RedirectType.BuildSupply
            });
        }
        if (RallyPoint is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromAssetLink(RallyPoint),
                Faction = faction,
                FactionId = faction.Key,
                Redirect = RedirectType.RallyPoint
            });
        }
        if (FOBRadio is not null)
        {
            faction.Assets.Add(new FactionAsset
            {
                Asset = UnturnedAssetReference.FromAssetLink(FOBRadio),
                Faction = faction,
                FactionId = faction.Key,
                Redirect = RedirectType.Radio
            });
        }
        if (Backpacks != null)
        {
            foreach (KeyValuePair<string, IAssetLink<ItemBackpackAsset>> backpack in Backpacks)
            {
                if (faction.Assets.Any(x => x.Redirect == RedirectType.Backpack && string.Equals(backpack.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                faction.Assets.Add(new FactionAsset
                {
                    Asset = UnturnedAssetReference.FromAssetLink(backpack.Value),
                    Faction = faction,
                    FactionId = faction.Key,
                    Redirect = RedirectType.Backpack,
                    VariantKey = backpack.Key.Length == 0 ? null : backpack.Key
                });
            }
        }
        if (Shirts != null)
        {
            foreach (KeyValuePair<string, IAssetLink<ItemShirtAsset>> shirt in Shirts)
            {
                if (faction.Assets.Any(x => x.Redirect == RedirectType.Shirt && string.Equals(shirt.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                faction.Assets.Add(new FactionAsset
                {
                    Asset = UnturnedAssetReference.FromAssetLink(shirt.Value),
                    Faction = faction,
                    FactionId = faction.Key,
                    Redirect = RedirectType.Shirt,
                    VariantKey = shirt.Key.Length == 0 ? null : shirt.Key
                });
            }
        }
        if (Pants != null)
        {
            foreach (KeyValuePair<string, IAssetLink<ItemPantsAsset>> pants in Pants)
            {
                if (faction.Assets.Any(x => x.Redirect == RedirectType.Pants && string.Equals(pants.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                faction.Assets.Add(new FactionAsset
                {
                    Asset = UnturnedAssetReference.FromAssetLink(pants.Value),
                    Faction = faction,
                    FactionId = faction.Key,
                    Redirect = RedirectType.Pants,
                    VariantKey = pants.Key.Length == 0 ? null : pants.Key
                });
            }
        }
        if (Vests != null)
        {
            foreach (KeyValuePair<string, IAssetLink<ItemVestAsset>> vest in Vests)
            {
                if (faction.Assets.Any(x => x.Redirect == RedirectType.Vest && string.Equals(vest.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                faction.Assets.Add(new FactionAsset
                {
                    Asset = UnturnedAssetReference.FromAssetLink(vest.Value),
                    Faction = faction,
                    FactionId = faction.Key,
                    Redirect = RedirectType.Vest,
                    VariantKey = vest.Key.Length == 0 ? null : vest.Key
                });
            }
        }
        if (Hats != null)
        {
            foreach (KeyValuePair<string, IAssetLink<ItemHatAsset>> hat in Hats)
            {
                if (faction.Assets.Any(x => x.Redirect == RedirectType.Hat && string.Equals(hat.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                faction.Assets.Add(new FactionAsset
                {
                    Asset = UnturnedAssetReference.FromAssetLink(hat.Value),
                    Faction = faction,
                    FactionId = faction.Key,
                    Redirect = RedirectType.Hat,
                    VariantKey = hat.Key.Length == 0 ? null : hat.Key
                });
            }
        }
        if (Glasses != null)
        {
            foreach (KeyValuePair<string, IAssetLink<ItemGlassesAsset>> glasses in Glasses)
            {
                if (faction.Assets.Any(x => x.Redirect == RedirectType.Glasses && string.Equals(glasses.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                faction.Assets.Add(new FactionAsset
                {
                    Asset = UnturnedAssetReference.FromAssetLink(glasses.Value),
                    Faction = faction,
                    FactionId = faction.Key,
                    Redirect = RedirectType.Glasses,
                    VariantKey = glasses.Key.Length == 0 ? null : glasses.Key
                });
            }
        }
        if (Masks != null)
        {
            foreach (KeyValuePair<string, IAssetLink<ItemMaskAsset>> mask in Masks)
            {
                if (faction.Assets.Any(x => x.Redirect == RedirectType.Mask && string.Equals(mask.Key, x.VariantKey, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                faction.Assets.Add(new FactionAsset
                {
                    Asset = UnturnedAssetReference.FromAssetLink(mask.Value),
                    Faction = faction,
                    FactionId = faction.Key,
                    Redirect = RedirectType.Mask,
                    VariantKey = mask.Key.Length == 0 ? null : mask.Key
                });
            }
        }

        return faction;
    }

    public void ApplyTranslations(Faction model)
    {
        NameTranslations.Clear();
        ShortNameTranslations.Clear();
        AbbreviationTranslations.Clear();

        if (model.Translations == null)
            return;

        foreach (FactionLocalization language in model.Translations)
        {
            if (!string.IsNullOrEmpty(language.Name))
                NameTranslations[language.Language.Code] = language.Name;

            if (!string.IsNullOrEmpty(language.ShortName))
                ShortNameTranslations[language.Language.Code] = language.ShortName;

            if (!string.IsNullOrEmpty(language.Abbreviation))
                AbbreviationTranslations[language.Language.Code] = language.Abbreviation;
        }
    }

    public void ApplyAssets(Faction model)
    {
        Backpacks.Clear();
        Shirts.Clear();
        Pants.Clear();
        Vests.Clear();
        Hats.Clear();
        Glasses.Clear();
        Masks.Clear();

        if (model.Assets == null)
        {
            Ammo = null;
            Build = null;
            RallyPoint = null;
            FOBRadio = null;
            return;
        }

        Ammo       = FindAsset(model, RedirectType.AmmoSupply)  ?.Asset.GetAssetLink<ItemAsset>();
        Build      = FindAsset(model, RedirectType.BuildSupply) ?.Asset.GetAssetLink<ItemAsset>();
        RallyPoint = FindAsset(model, RedirectType.RallyPoint)  ?.Asset.GetAssetLink<ItemBarricadeAsset>();
        FOBRadio   = FindAsset(model, RedirectType.Radio)       ?.Asset.GetAssetLink<ItemBarricadeAsset>();

        foreach (FactionAsset asset in model.Assets)
        {
            string key = asset.VariantKey ?? string.Empty;
            switch (asset.Redirect)
            {
                case RedirectType.Backpack:
                    Backpacks[key] = asset.Asset.GetAssetLink<ItemBackpackAsset>();
                    break;

                case RedirectType.Shirt:
                    Shirts[key] = asset.Asset.GetAssetLink<ItemShirtAsset>();
                    break;

                case RedirectType.Pants:
                    Pants[key] = asset.Asset.GetAssetLink<ItemPantsAsset>();
                    break;

                case RedirectType.Vest:
                    Vests[key] = asset.Asset.GetAssetLink<ItemVestAsset>();
                    break;

                case RedirectType.Hat:
                    Hats[key] = asset.Asset.GetAssetLink<ItemHatAsset>();
                    break;

                case RedirectType.Glasses:
                    Glasses[key] = asset.Asset.GetAssetLink<ItemGlassesAsset>();
                    break;

                case RedirectType.Mask:
                    Masks[key] = asset.Asset.GetAssetLink<ItemMaskAsset>();
                    break;
            }
        }
    }

    private static FactionAsset? FindAsset(Faction factionInfo, RedirectType redirect)
    {
        return factionInfo.Assets?.FirstOrDefault(x => x.Redirect == redirect);
    }

    
    public static readonly SpecialFormat FormatShortName = new SpecialFormat("Short Name", "s");
    
    public static readonly SpecialFormat FormatDisplayName = new SpecialFormat("Display Name", "d");
    
    public static readonly SpecialFormat FormatAbbreviation = new SpecialFormat("Abbreviation", "a");
    
    public static readonly SpecialFormat FormatColorShortName = new SpecialFormat("Colored Short Name", "sc");
    
    public static readonly SpecialFormat FormatColorDisplayName = new SpecialFormat("Colored Display Name", "dc");
    
    public static readonly SpecialFormat FormatColorAbbreviation = new SpecialFormat("Colored Abbreviation", "ac");

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (FormatColorDisplayName.Match(in parameters))
            return formatter.Colorize(GetName(parameters.Language), Color, parameters.Options);
        
        if (FormatShortName.Match(in parameters))
            return GetShortName(parameters.Language);
        
        if (FormatColorShortName.Match(in parameters))
            return formatter.Colorize(GetShortName(parameters.Language), Color, parameters.Options);
        
        if (FormatAbbreviation.Match(in parameters))
            return GetAbbreviation(parameters.Language);
        
        if (FormatColorAbbreviation.Match(in parameters))
            return formatter.Colorize(GetAbbreviation(parameters.Language), Color, parameters.Options);

        return GetName(parameters.Language);
    }

    public string GetName(LanguageInfo? language)
    {
        if (language is null || language.IsDefault || NameTranslations is null || !NameTranslations.TryGetValue(language.Code, out string? val))
            return Name;
        return val ?? Name;
    }

    public string GetShortName(LanguageInfo? language)
    {
        if (language is null || language.IsDefault)
            return ShortName ?? Name;
        if (ShortNameTranslations is null || !ShortNameTranslations.TryGetValue(language.Code, out string? val))
        {
            if (NameTranslations is null || !NameTranslations.TryGetValue(language.Code, out val))
                return ShortName ?? Name;
        }
        return val ?? Name;
    }

    public string GetAbbreviation(LanguageInfo? language)
    {
        if (language is null || language.IsDefault || AbbreviationTranslations is null || !AbbreviationTranslations.TryGetValue(language.Code, out string val))
            return Abbreviation;
        return val;
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new FactionInfo(FactionId, Name, Abbreviation, ShortName, Color, UnarmedKit, KitPrefix, FlagImageURL)
        {
            PrimaryKey = PrimaryKey,
            Ammo = Ammo?.Clone() as IAssetLink<ItemAsset>,
            Build = Build?.Clone() as IAssetLink<ItemAsset>,
            RallyPoint = RallyPoint?.Clone() as IAssetLink<ItemBarricadeAsset>,
            FOBRadio = FOBRadio?.Clone() as IAssetLink<ItemBarricadeAsset>,
            Backpacks = Backpacks.Clone(),
            Shirts = Shirts.Clone(),
            Pants = Pants.Clone(),
            Vests = Vests.Clone(),
            Glasses = Glasses.Clone(),
            Masks = Masks.Clone(),
            Hats = Hats.Clone()
        };
    }

    public FactionInfo? NullIfDefault()
    {
        return IsDefaultFaction ? null : this;
    }

    public override string ToString()
    {
        return $"{FactionId} #{PrimaryKey} [{Name}]";
    }
}