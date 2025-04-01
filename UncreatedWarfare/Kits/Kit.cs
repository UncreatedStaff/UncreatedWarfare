using System;
using System.Collections.Generic;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Vehicles.Spawners.Delays;

namespace Uncreated.Warfare.Kits;

public class Kit : IRequestable<Kit>, ITranslationArgument
{
    private IKitItem[]? _items;
    private UnlockRequirement[]? _unlockRequirements;
    private Skillset[]? _skillsets;
    private FactionInfo[]? _factionFilter;
    private uint[]? _mapFilter;
    private EliteBundle[]? _bundles;
    private ILayoutDelay<LayoutDelayContext>[]? _delays;
    private KitAccessRow[]? _access;
    private CSteamID[]? _favorites;
    private TranslationList? _translations;

    /// <summary>
    /// Primary key of the kit in the database.
    /// </summary>
    public uint Key { get; }
    
    /// <summary>
    /// Internal name of the kit.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// Faction of the kit.
    /// </summary>
    public FactionInfo Faction { get; private set; }

    /// <summary>
    /// The class of the kit, influencing its behavior in-game.
    /// </summary>
    public Class Class { get; private set; }

    /// <summary>
    /// The branch of the military the kit is a part of.
    /// </summary>
    public Branch Branch { get; private set; }

    /// <summary>
    /// The type of this kit, differentiating loadouts, elite kits, public kits, and other kits.
    /// </summary>
    public KitType Type { get; private set; }

    /// <summary>
    /// Squad standing of the users of this kit.
    /// </summary>
    public SquadLevel SquadLevel { get; private set; }

    /// <summary>
    /// If the kit has been disabled.
    /// </summary>
    /// <remarks>This also indicates that a loadout is locked while it's undergoing creation or maintenance.</remarks>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// If the kit requires the requester to be boosting the server with Discord Nitro.
    /// </summary>
    public bool RequiresServerBoost { get; private set; }

    /// <summary>
    /// The season number the kit was created for.
    /// </summary>
    public int Season { get; private set; }

    /// <summary>
    /// If the map filter acts as a whitelist instead of a blacklist.
    /// </summary>
    public bool MapFilterIsWhitelist { get; private set; }

    /// <summary>
    /// If the faction filter acts as a whitelist instead of a blacklist.
    /// </summary>
    public bool FactionFilterIsWhitelist { get; private set; }

    /// <summary>
    /// The individual cooldown between requests for this kit.
    /// </summary>
    public TimeSpan RequestCooldown { get; private set; }
    
    /// <summary>
    /// A player can only use this kit after this many players are in their squad.
    /// </summary>
    public int? MinRequiredSquadMembers { get; private set; }
    
    /// <summary>
    /// Whether a player needs to be in a squad in order to use this kit.
    /// </summary>
    public bool RequiresSquad { get; private set; }

    /// <summary>
    /// Cost to unlock this kit in in-game credits.
    /// </summary>
    public int CreditCost { get; private set; }

    /// <summary>
    /// Cost to unlock this kit in US dollars.
    /// </summary>
    public decimal PremiumCost { get; private set; }

    /// <summary>
    /// Text displayed on signs describing the weapons in the kit.
    /// </summary>
    public string? WeaponText { get; private set; }

    /// <summary>
    /// Time at which the kit was created.
    /// </summary>
    public DateTimeOffset CreatedTimestamp { get; private set; }

    /// <summary>
    /// The player who created the kit.
    /// </summary>
    public CSteamID CreatingPlayer { get; private set; }

    /// <summary>
    /// Time at which the kit was last updated.
    /// </summary>
    public DateTimeOffset LastEditedTimestamp { get; private set; }

    /// <summary>
    /// The player who last edited the kit.
    /// </summary>
    public CSteamID LastEditingPlayer { get; private set; }

    /// <summary>
    /// List of all items in the kit.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public IKitItem[] Items => _items ?? throw new NotIncludedException("KitModel.Items");

    /// <summary>
    /// List of all requirements to unlock the kit.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public UnlockRequirement[] UnlockRequirements => _unlockRequirements ?? throw new NotIncludedException("KitModel.UnlockRequirements");

    /// <summary>
    /// List of all skillsets awarded to the player when they equip the kit.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public Skillset[] Skillsets => _skillsets ?? throw new NotIncludedException("KitModel.Skillsets");

    /// <summary>
    /// List of all factions in the blacklist or whitelist (depending on <see cref="FactionFilterIsWhitelist"/>).
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public FactionInfo[] FactionFilter => _factionFilter ?? throw new NotIncludedException("KitModel.FactionFilter");

    /// <summary>
    /// List of all map IDs in the blacklist or whitelist (depending on <see cref="MapFilterIsWhitelist"/>).
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public uint[] MapFilter => _mapFilter ?? throw new NotIncludedException("KitModel.MapFilter");

    /// <summary>
    /// Translation lookup table for the kit name.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public TranslationList Translations => _translations ?? throw new NotIncludedException("KitModel.MapFilter");

    /// <summary>
    /// List of all elite bundles containing this kit. Non-elite kits will always return an empty array.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public EliteBundle[] Bundles => Type == KitType.Elite ? _bundles ?? throw new NotIncludedException("KitModel.Bundles") : Array.Empty<EliteBundle>();

    /// <summary>
    /// List of all delays applied to the kit.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public ILayoutDelay<LayoutDelayContext>[] Delays => _delays ?? throw new NotIncludedException("KitModel.Delays");

    /// <summary>
    /// List of all players who have specific access to the kit.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public KitAccessRow[] Access => _access ?? throw new NotIncludedException("KitModel.Access");

    /// <summary>
    /// List of all players who have favorited this kit.
    /// </summary>
    /// <exception cref="NotIncludedException"/>
    public CSteamID[] Favorites => _favorites ?? throw new NotIncludedException("KitModel.Favorites");

    public bool IsPaid => Type is KitType.Elite or KitType.Loadout;

 #pragma warning disable CS8618
    internal Kit(KitModel model, IFactionDataStore factionDataStore, ICachableLanguageDataStore languageDataStore)
    {
        Key = model.PrimaryKey;
        UpdateFromModel(model, factionDataStore, languageDataStore);
    }
#pragma warning restore CS8618

    public string GetDisplayName(LanguageInfo? language, bool useIdFallback, bool removeNewLine = true)
    {
        if (_translations == null)
        {
            return useIdFallback ? Id : throw new NotIncludedException("KitModel.Translations");
        }

        string str = _translations.Translate(language, Id);
        if (removeNewLine)
            str = str.Replace('\n', ' ').Replace("\r", string.Empty);
        return str;
    }

    internal void UpdateFromModel(KitModel model, IFactionDataStore factionDataStore, ICachableLanguageDataStore languageDataStore)
    {
        if (Key != 0 && Key != model.PrimaryKey)
            throw new ArgumentException("Key not same as model.", nameof(model));

        Id = model.Id;
        Class = model.Class;
        Branch = model.Branch;
        Type = model.Type;
        SquadLevel = model.SquadLevel;
        IsLocked = model.Disabled;
        FactionFilterIsWhitelist = model.FactionFilterIsWhitelist;
        MapFilterIsWhitelist = model.MapFilterIsWhitelist;
        RequiresServerBoost = model.RequiresNitro;
        Season = model.Season;
        RequestCooldown = TimeSpan.FromSeconds(model.RequestCooldown);
        MinRequiredSquadMembers = model.MinRequiredSquadMembers;
        RequiresSquad = model.RequiresSquad;
        CreditCost = model.CreditCost;
        PremiumCost = model.PremiumCost;
        WeaponText = model.Weapons;

        CreatedTimestamp = model.CreatedAt;
        CSteamID creatingPlayer = new CSteamID(model.Creator);
        if (creatingPlayer.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            CreatingPlayer = creatingPlayer;

        LastEditedTimestamp = model.LastEditedAt;
        CSteamID updatingPlayer = new CSteamID(model.LastEditor);
        if (updatingPlayer.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            LastEditingPlayer = updatingPlayer;

        Faction = factionDataStore.FindFaction(model.FactionId) ?? FactionInfo.NoFaction;

        if (model.Items != null)
            UpdateItemsFromModel(model.Items);

        if (model.UnlockRequirements != null)
            UpdateUnlockRequirementsFromModel(model.UnlockRequirements);

        if (model.Skillsets != null)
            UpdateSkillsetsFromModel(model.Skillsets);

        if (model.FactionFilter != null)
            UpdateFactionFilterFromModel(model.FactionFilter, factionDataStore);

        if (model.MapFilter != null)
            UpdateMapFilterFromModel(model.MapFilter);

        if (model.Translations != null)
            UpdateTranslationsFromModel(model.Translations, languageDataStore);

        if (Type == KitType.Elite && model.Bundles != null)
            UpdateBundlesFromModel(model.Bundles);

        if (model.Delays != null)
            UpdateDelaysFromModel(model.Delays);

        if (model.Access != null)
            UpdateAccessFromModel(model.Access);

        if (model.Favorites != null)
            UpdateFavoritesFromModel(model.Favorites);
    }

    private void UpdateUnlockRequirementsFromModel(List<KitUnlockRequirement> unlockRequirements)
    {
        UnlockRequirement[] array = new UnlockRequirement[unlockRequirements.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            UnlockRequirement? requirement = unlockRequirements[i].CreateUninitializedRequirement();
            array[i] = requirement ?? throw new FormatException($"Invalid unlock requirement: {unlockRequirements[i].Id} (Kit #{unlockRequirements[i].KitId}).");
        }

        _unlockRequirements = array;
    }

    private void UpdateItemsFromModel(List<KitItemModel> items)
    {
        IKitItem[] array = new IKitItem[items.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = items[i].CreateRuntimeItem();
        }

        _items = array;
    }

    private void UpdateSkillsetsFromModel(List<KitSkillset> skillsets)
    {
        Skillset[] array = new Skillset[skillsets.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = skillsets[i].Skillset;
        }

        _skillsets = array;
    }

    private void UpdateFactionFilterFromModel(List<KitFilteredFaction> factionFilter, IFactionDataStore factionDataStore)
    {
        FactionInfo[] array = new FactionInfo[factionFilter.Count];

        int index = -1;
        for (int i = 0; i < array.Length; ++i)
        {
            FactionInfo? faction = factionDataStore.FindFaction(factionFilter[i].FactionId);
            if (faction == null)
                continue;

            array[++index] = faction;
        }

        if (index != array.Length - 1)
            Array.Resize(ref array, index + 1);

        _factionFilter = array;
    }

    private void UpdateMapFilterFromModel(List<KitFilteredMap> mapFilter)
    {
        uint[] array = new uint[mapFilter.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = mapFilter[i].Map;
        }

        _mapFilter = array;
    }

    private void UpdateTranslationsFromModel(List<KitTranslation> translations, ICachableLanguageDataStore languageDataStore)
    {
        TranslationList tList = new TranslationList(translations.Count);
        foreach (KitTranslation translation in translations)
        {
            LanguageInfo? lang = languageDataStore.GetInfoCached(translation.LanguageId);
            if (lang != null)
            {
                tList.Add(lang, translation.Value);
            }
        }

        _translations = tList;
    }

    private void UpdateBundlesFromModel(List<KitEliteBundle> bundles)
    {
        EliteBundle[] array = new EliteBundle[bundles.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = bundles[i].Bundle;
        }

        _bundles = array;
    }

    private void UpdateDelaysFromModel(List<KitDelay> delays)
    {
        ILayoutDelay<LayoutDelayContext>[] array = new ILayoutDelay<LayoutDelayContext>[delays.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            ILayoutDelay<LayoutDelayContext>? delay = delays[i].CreateDelay();
            array[i] = delay ?? throw new FormatException($"Invalid unlock requirement: {delays[i].Id} (Kit #{delays[i].KitId}).");
        }

        _delays = array;
    }

    private void UpdateAccessFromModel(List<KitAccess> accesses)
    {
        KitAccessRow[] array = new KitAccessRow[accesses.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            KitAccess access = accesses[i];
            array[i] = new KitAccessRow(new CSteamID(access.Steam64), access.AccessType, access.Timestamp);
        }

        _access = array;
    }

    private void UpdateFavoritesFromModel(List<KitFavorite> favorites)
    {
        CSteamID[] array = new CSteamID[favorites.Count];

        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = new CSteamID(favorites[i].Steam64);
        }

        _favorites = array;
    }

    public static readonly SpecialFormat FormatId = new SpecialFormat("Kit Id", "i");

    public static readonly SpecialFormat FormatDisplayName = new SpecialFormat("Display Name", "d");

    public static readonly SpecialFormat FormatClass = new SpecialFormat("Class", "c");

    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (FormatId.Match(in parameters))
            return Id;

        if (FormatClass.Match(in parameters))
            return formatter.FormatEnum(Class, parameters.Language);

        return GetDisplayName(parameters.Language, true);
    }
}