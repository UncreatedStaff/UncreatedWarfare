using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Networking.Purchasing;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Models.Kits;

// todo add delays to kits
[Table("kits")]
public class Kit : ITranslationArgument, ICloneable, IRequestable<Kit>
{
    private int _listItemArrayVersion = -1;
    private int _listUnlockRequirementsArrayVersion = -1;
    private int _listSimplifiedListVersion = -1;
    private IKitItem[]? _items;
    private UnlockRequirement[]? _unlockRequirements;
    private List<SimplifiedItemListEntry>? _simplifiedItemList;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint PrimaryKey { get; set; }

    [JsonIgnore]
    public Faction? Faction { get; set; }

    [ForeignKey(nameof(Faction))]
    [Column("Faction")]
    public uint? FactionId { get; set; }

    [Required]
    [StringLength(25)]
    [Column("Id")]
    public string InternalName { get; set; }
    
    [CommandSettable]
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Class Class { get; set; }
    
    [CommandSettable]
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Branch Branch { get; set; }
    
    [CommandSettable]
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public KitType Type { get; set; }
    
    [CommandSettable("IsDisabled")]
    public bool Disabled { get; set; }

    [CommandSettable("NitroBooster")]
    public bool RequiresNitro { get; set; }

    [CommandSettable("MapWhitelist")]
    public bool MapFilterIsWhitelist { get; set; }

    [CommandSettable("FactionWhitelist")]
    public bool FactionFilterIsWhitelist { get; set; }

    [CommandSettable]
    public int Season { get; set; }

    [CommandSettable]
    [DefaultValue(0f)]
    public float RequestCooldown { get; set; }

    [CommandSettable]
    public float? TeamLimit { get; set; }

    [CommandSettable]
    [DefaultValue(0)]
    public int CreditCost { get; set; }

    [CommandSettable]
    [DefaultValue(0)]
    public decimal PremiumCost { get; set; }

    [CommandSettable]
    [DefaultValue(SquadLevel.Member)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SquadLevel SquadLevel { get; set; }

    [NotMapped]
    public IKitItem[] Items
    {
        get
        {
            int v = ItemModels.GetListVersion();
            if (_listItemArrayVersion != v || _items == null)
            {
                UpdateItemArray();
                _listItemArrayVersion = v;
            }

            return _items!;
        }
    }

    [NotMapped]
    public UnlockRequirement[] UnlockRequirements
    {
        get
        {
            int v = UnlockRequirementsModels.GetListVersion();
            if (_listUnlockRequirementsArrayVersion != v || _unlockRequirements == null)
            {
                UpdateUnlockRequirementArray(WarfareModule.Singleton.ScopedProvider.Resolve<IServiceProvider>() /* todo */);
                _listUnlockRequirementsArrayVersion = v;
            }

            return _unlockRequirements!;
        }
    }

    [Column("Weapons")]
    [CommandSettable("Weapons")]
    [StringLength(128)]
    public string? WeaponText { get; set; }

    [Column("CreatedAt")]
    public DateTimeOffset CreatedTimestamp { get; set; }
    public ulong Creator { get; set; }

    [Column("LastEditedAt")]
    public DateTimeOffset LastEditedTimestamp { get; set; }
    public ulong LastEditor { get; set; }

    [JsonIgnore]
    [NotMapped]
    internal bool IsLoadDirty { get; set; }

    //[JsonIgnore]
    //[NotMapped]
    //internal List<SimplifiedItemListEntry> SimplifiedItemList
    //{
    //    get
    //    {
    //        int v = ItemModels.GetListVersion();
    //        if (_listSimplifiedListVersion != v || _simplifiedItemList == null)
    //        {
    //            _simplifiedItemList = SimplifiedItemListEntry.GetSimplifiedItemList(this);
    //            _listSimplifiedListVersion = v;
    //        }
    //
    //        return _simplifiedItemList;
    //    }
    //}

    [JsonIgnore]
    [NotMapped]
    public StripeEliteKit EliteKitInfo { get; set; }

    /// <summary>
    /// Loadout is expired.
    /// </summary>
    [JsonIgnore]
    [NotMapped]
    public bool NeedsUpgrade => Type == KitType.Loadout && Season < WarfareModule.Season;

    /// <summary>
    /// Loadout is in the process of being created or updated.
    /// </summary>
    [JsonIgnore]
    [NotMapped]
    public bool NeedsSetup => Type == KitType.Loadout && Disabled;

    /// <summary>Checks that the kit is publicly available and has a vaild class (not None or Unarmed).</summary>
    [JsonIgnore]
    [NotMapped]
    public bool IsPublicKit => Type == KitType.Public && Class > Class.Unarmed;

    /// <summary>Elite kit or loadout.</summary>
    [JsonIgnore]
    [NotMapped]
    public bool IsPaid => Type is KitType.Elite or KitType.Loadout;
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist. Checks both active teams, use <see cref="IsRequestable(ulong)"/> to check for a certain team.</summary>
    [JsonIgnore]
    [NotMapped]
    public bool Requestable => !Disabled && (Type is not KitType.Loadout || Season >= WarfareModule.Season || Season < 1) &&
                             IsCurrentMapAllowed()/* todo &&
                             (IsFactionAllowed(TeamManager.Team1Faction) || IsFactionAllowed(TeamManager.Team2Faction)) */;
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist.</summary>
    public bool IsRequestable(ulong team) => team is not 1ul and not 2ul ? Requestable : (!Disabled && (Type is not KitType.Loadout || Season >= WarfareModule.Season || Season < 1) &&
                             IsCurrentMapAllowed() /* todo &&
                             IsFactionAllowed(TeamManager.GetFaction(team)) */);
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist.</summary>
    public bool IsRequestable(FactionInfo? faction) => faction is null ? Requestable : (!Disabled && (Type is not KitType.Loadout || Season >= WarfareModule.Season || Season < 1) &&
                                                                               IsCurrentMapAllowed() &&
                                                                               IsFactionAllowed(faction));

    public List<KitFilteredFaction> FactionFilter { get; set; } = [];
    public List<KitFilteredMap> MapFilter { get; set; } = [];
    public List<KitSkillset> Skillsets { get; set; } = [];
    public List<KitTranslation> Translations { get; set; } = [];

    [JsonIgnore]
    public List<KitItemModel> ItemModels { get; set; } = [];

    [JsonIgnore]
    public List<KitUnlockRequirement> UnlockRequirementsModels { get; set; } = [];
    public IReadOnlyCollection<KitAccess> Access { get; set; } = new List<KitAccess>(0);
    public IReadOnlyCollection<KitEliteBundle> Bundles { get; set; } = new List<KitEliteBundle>(0);

    public Kit(string internalName, Class @class, Branch branch, KitType type, SquadLevel squadLevel, FactionInfo? faction)
    {
        FactionId = faction?.PrimaryKey;
        InternalName = internalName;
        Class = @class;
        Branch = branch;
        Type = type;
        SquadLevel = squadLevel;
        Season = WarfareModule.Season;
        TeamLimit = KitDefaults.GetDefaultTeamLimit(@class);
        RequestCooldown = KitDefaults.GetDefaultRequestCooldown(@class);
        CreatedTimestamp = LastEditedTimestamp = DateTime.UtcNow;
    }
    public Kit(string internalName, Kit copy)
    {
        CopyFrom(copy, true, true);
        InternalName = internalName;
    }
    public void CopyFrom(Kit copy, bool clone, bool copyCachedLists)
    {
        Faction = copy.Faction;
        FactionId = copy.FactionId;
        InternalName = copy.InternalName;
        Class = copy.Class;
        Branch = copy.Branch;
        Type = copy.Type;
        SquadLevel = copy.SquadLevel;
        if (clone)
        {
            Skillsets = [.. CollectionUtility.CloneList(copy.Skillsets) ];
            ItemModels = [.. CollectionUtility.CloneList(copy.ItemModels) ];
            if (copyCachedLists)
            {
                UnlockRequirementsModels = [.. CollectionUtility.CloneList(copy.UnlockRequirementsModels) ];
                FactionFilter = [.. CollectionUtility.CloneList(copy.FactionFilter) ];
                MapFilter = [.. CollectionUtility.CloneList(copy.MapFilter) ];
                Translations = [.. CollectionUtility.CloneList(copy.Translations) ];
            }
        }
        else
        {
            Skillsets = copy.Skillsets;
            ItemModels = copy.ItemModels;
            if (copyCachedLists)
            {
                UnlockRequirementsModels = copy.UnlockRequirementsModels;
                FactionFilter = copy.FactionFilter;
                MapFilter = copy.MapFilter;
                Translations = copy.Translations;
            }
        }

        Season = copy.Season;
        TeamLimit = copy.TeamLimit;
        RequestCooldown = copy.RequestCooldown;
        FactionFilterIsWhitelist = copy.FactionFilterIsWhitelist;
        MapFilterIsWhitelist = copy.MapFilterIsWhitelist;
        Disabled = copy.Disabled;
        CreditCost = copy.CreditCost;
        PremiumCost = copy.PremiumCost;
        WeaponText = copy.WeaponText;
        CreatedTimestamp = DateTime.UtcNow;
        LastEditedTimestamp = copy.LastEditedTimestamp;
        LastEditor = copy.LastEditor;
        RequiresNitro = copy.RequiresNitro;

        if (clone)
            ReapplyPrimaryKey();
    }
    internal void ReapplyPrimaryKey()
    {
        foreach (KitSkillset skillset in Skillsets)
        {
            skillset.Kit = this;
            skillset.KitId = PrimaryKey;
        }
        foreach (KitFilteredFaction faction in FactionFilter)
        {
            faction.Kit = this;
            faction.KitId = PrimaryKey;
        }
        foreach (KitFilteredMap map in MapFilter)
        {
            map.Kit = this;
            map.KitId = PrimaryKey;
        }
        foreach (KitItemModel item in ItemModels)
        {
            item.Kit = this;
            item.KitId = PrimaryKey;
        }
        foreach (KitTranslation translation in Translations)
        {
            translation.Kit = this;
            translation.KitId = PrimaryKey;
        }
        foreach (KitUnlockRequirement unlockRequirement in UnlockRequirementsModels)
        {
            unlockRequirement.Kit = this;
            unlockRequirement.KitId = PrimaryKey;
        }
    }
    /// <summary>For loadout.</summary>
    public Kit(string loadout, Class @class, string? displayName, LanguageInfo displayNameLanguage)
    {
        Faction = null;
        FactionId = null;
        InternalName = loadout;
        Class = @class;
        Branch = KitDefaults.GetDefaultBranch(@class);
        Type = KitType.Loadout;
        SquadLevel = SquadLevel.Member;
        Season = WarfareModule.Season;
        TeamLimit = KitDefaults.GetDefaultTeamLimit(@class);
        RequestCooldown = KitDefaults.GetDefaultRequestCooldown(@class);
        PremiumCost = 10;// todo UCWarfare.Config.LoadoutCost;
        CreatedTimestamp = LastEditedTimestamp = DateTime.UtcNow;
        if (displayName != null)
        {
            Translations.Add(new KitTranslation
            {
                Kit = this,
                KitId = PrimaryKey,
                Value = displayName,
                LanguageId = displayNameLanguage.Key
            });
        }
    }
    public Kit() { }
    public void MarkRemoteItemsDirty()
    {
        _items = null;
        _simplifiedItemList = null;
    }
    public void MarkRemoteUnlockRequirementsDirty()
    {
        _unlockRequirements = null;
    }
    public void MarkLocalItemsDirty(IKitsDbContext dbContext)
    {
        SetItemArray(Items, dbContext);
    }
    public void MarkLocalUnlockRequirementsDirty(IKitsDbContext dbContext)
    {
        SetUnlockRequirementArray(UnlockRequirements, dbContext);
    }

    public bool IsFactionAllowed(FactionInfo? faction)
    {
        // FactionInfo? factionInfo = FactionInfo;
        // todo if (Type == KitType.Public)
        // todo     return faction != TeamManager.Team1Faction && faction != TeamManager.Team2Faction || string.Equals(faction?.FactionId, factionInfo?.FactionId, StringComparison.Ordinal);
        // todo 
        // todo if (faction == TeamManager.Team1Faction && string.Equals(factionInfo?.FactionId, TeamManager.Team2Faction?.FactionId, StringComparison.Ordinal) ||
        // todo     faction == TeamManager.Team2Faction && string.Equals(factionInfo?.FactionId, TeamManager.Team1Faction?.FactionId, StringComparison.Ordinal))
        // todo     return false;

        if (FactionFilter.IsNullOrEmpty() || faction is null || faction.PrimaryKey == 0)
            return true;
        
        for (int i = 0; i < FactionFilter.Count; ++i)
            if (faction.PrimaryKey == FactionFilter[i].FactionId)
                return FactionFilterIsWhitelist;

        return !FactionFilterIsWhitelist;
    }

    public bool IsCurrentMapAllowed()
    {
        if (MapFilter.IsNullOrEmpty())
            return true;
        int map = MapScheduler.CurrentStatic;
        if (map != -1)
        {
            for (int i = 0; i < MapFilter.Count; ++i)
            {
                if (MapFilter[i].Map == map)
                    return MapFilterIsWhitelist;
            }
        }

        return !MapFilterIsWhitelist;
    }

    public bool MeetsUnlockRequirementsFast(WarfarePlayer player)
    {
        if (UnlockRequirements is not { Length: > 0 }) return false;
        for (int i = 0; i < UnlockRequirements.Length; ++i)
        {
            if (!UnlockRequirements[i].CanAccessFast(player))
                return false;
        }

        return true;
    }

    public async Task<bool> MeetsUnlockRequirementsAsync(WarfarePlayer player, CancellationToken token = default)
    {
        if (UnlockRequirements is not { Length: > 0 }) return false;
        for (int i = 0; i < UnlockRequirements.Length; ++i)
        {
            if (!await UnlockRequirements[i].CanAccessAsync(player, token))
                return false;
        }

        return true;
    }

    public string GetDisplayName(LanguageService languageService, LanguageInfo? language = null, bool removeNewLine = true)
    {
        if (Translations is not { Count: > 0 })
            return InternalName;

        string rtn;
        if (languageService == null && language is null)
        {
            rtn = Translations.FirstOrDefault()?.Value ?? InternalName;
        }
        else
        {
            language ??= languageService!.GetDefaultLanguage();
            KitTranslation? translation = Translations.FirstOrDefault(x => x.LanguageId == language.Key);
            if (translation != null)
                rtn = translation.Value ?? InternalName;
            else if (!language.IsDefault && languageService != null)
            {
                language = languageService.GetDefaultLanguage();
                translation = Translations.FirstOrDefault(x => x.LanguageId == language.Key);
                if (translation != null)
                    rtn = translation.Value ?? InternalName;
                else
                    rtn = Translations.FirstOrDefault()?.Value ?? InternalName;
            }
            else
                rtn = Translations.FirstOrDefault()?.Value ?? InternalName;
        }

        if (removeNewLine)
            rtn = rtn.Replace('\n', ' ').Replace("\r", string.Empty);

        return rtn;
    }

    public static readonly SpecialFormat FormatId = new SpecialFormat("Kit Id", "i");
    public static readonly SpecialFormat FormatDisplayName = new SpecialFormat("Display Name", "d");
    public static readonly SpecialFormat FormatClass = new SpecialFormat("Class", "c");
    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (FormatId.Match(in parameters))
            return InternalName;
        if (FormatClass.Match(in parameters))
            return formatter.FormatEnum(Class, parameters.Language);

        return GetDisplayName(formatter.LanguageService, parameters.Language);
    }

    public object Clone() => new Kit(InternalName, this);
    public void SetItemArray(IKitItem[] items, IKitsDbContext dbContext)
    {
        List<KitItemModel> models = ItemModels;
        BitArray workingArray = new BitArray(items.Length);
        for (int i = 0; i < items.Length; ++i)
        {
            IKitItem item = items[i];
            if (item.PrimaryKey == 0)
                continue;

            for (int j = 0; j < models.Count; ++j)
            {
                KitItemModel model = models[j];
                if (model.Id != item.PrimaryKey)
                    continue;

                item.WriteToModel(model);
                dbContext.Update(model);
                workingArray[i] = true;
            }
        }

        for (int i = models.Count - 1; i >= 0; --i)
        {
            KitItemModel model = models[i];
            bool found = false;
            for (int j = 0; j < items.Length; ++j)
            {
                if (items[j].PrimaryKey != model.Id)
                    continue;

                found = true;
                break;
            }

            if (!found)
                models.RemoveAt(i);
            dbContext.Remove(model);
        }

        for (int i = 0; i < items.Length; ++i)
        {
            if (workingArray[i])
                continue;

            IKitItem item = items[i];
            item.PrimaryKey = 0;
            KitItemModel model = item.CreateModel(this);
            models.Add(model);
            dbContext.Add(model);
        }

        _listItemArrayVersion = models.GetListVersion();
        _items = items;
    }
    private void UpdateItemArray()
    {
        KitItemModel[] models = ItemModels.ToArray();
        if (models is not { Length: > 0 })
        {
            _items = Array.Empty<IKitItem>();
            return;
        }

        bool pooled = Provider.isInitialized && GameThread.IsCurrent;
        List<IKitItem> tempList = pooled ? ListPool<IKitItem>.claim() : new List<IKitItem>(models.Length);
        try
        {
            foreach (KitItemModel model in models)
            {
                try
                {
                    tempList.Add(model.CreateRuntimeItem());
                }
                catch (FormatException ex)
                {
                    WarfareModule.Singleton.GlobalLogger.LogWarning(ex, "Skipped item {0} in kit {1}:", model.Id, model.Kit.InternalName);
                }
            }

            _items = tempList.ToArray();
        }
        finally
        {
            if (pooled)
                ListPool<IKitItem>.release(tempList);
        }
    }
    public void SetUnlockRequirementArray(UnlockRequirement[] items, IKitsDbContext dbContext)
    {
        List<KitUnlockRequirement> models = UnlockRequirementsModels;
        BitArray workingArray = new BitArray(items.Length);
        for (int i = 0; i < items.Length; ++i)
        {
            UnlockRequirement item = items[i];
            if (item.PrimaryKey == 0)
                continue;

            for (int j = 0; j < models.Count; ++j)
            {
                KitUnlockRequirement model = models[j];
                if (model.Id != item.PrimaryKey)
                    continue;

                model.Json = item.ToJson();
                dbContext.Update(model);
                workingArray[i] = true;
            }
        }

        for (int i = models.Count - 1; i >= 0; --i)
        {
            KitUnlockRequirement model = models[i];
            bool found = false;
            for (int j = 0; j < items.Length; ++j)
            {
                if (items[j].PrimaryKey != model.Id)
                    continue;
                
                found = true;
                break;
            }

            if (!found)
                models.RemoveAt(i);
            dbContext.Remove(model);
        }

        for (int i = 0; i < items.Length; ++i)
        {
            if (workingArray[i])
                continue;

            UnlockRequirement item = items[i];
            item.PrimaryKey = 0;
            KitUnlockRequirement model = new KitUnlockRequirement
            {
                KitId = PrimaryKey,
                Kit = this,
                Json = item.ToJson()
            };
            dbContext.Add(model);
            models.Add(model);
        }

        _listUnlockRequirementsArrayVersion = models.GetListVersion();
        _unlockRequirements = items;
    }
    private void UpdateUnlockRequirementArray(IServiceProvider serviceProvider)
    {
        KitUnlockRequirement[] models = UnlockRequirementsModels.ToArray();
        if (models is not { Length: > 0 })
        {
            _unlockRequirements = Array.Empty<UnlockRequirement>();
            return;
        }

        bool pooled = Provider.isInitialized && GameThread.IsCurrent;
        List<UnlockRequirement> tempList = pooled ? ListPool<UnlockRequirement>.claim() : new List<UnlockRequirement>(models.Length);
        ILogger logger = serviceProvider.GetRequiredService<ILogger<Kit>>();
        try
        {
            foreach (KitUnlockRequirement model in models)
            {
                try
                {
                    UnlockRequirement? requirement = model.CreateRuntimeRequirement(logger, serviceProvider);
                    if (requirement != null)
                        tempList.Add(requirement);
                }
                catch (Exception ex)
                {
                    WarfareModule.Singleton.GlobalLogger.LogWarning(ex, "Skipped unlock requirement {0} in kit {1}:", model.Id, model.Kit.InternalName);
                }
            }

            _unlockRequirements = tempList.ToArray();
        }
        finally
        {
            if (pooled)
                ListPool<UnlockRequirement>.release(tempList);
        }
    }
    /// <summary>Will not update signs.</summary>
    public void SetSignText(IKitsDbContext dbContext, CSteamID setter, string? text, LanguageInfo language)
    {
        if (language is null)
            throw new ArgumentNullException(nameof(language));

        int index = Translations.FindIndex(x => x.LanguageId == language.Key);
        if (index != -1)
        {
            KitTranslation translation = Translations[index];
            if (string.IsNullOrEmpty(text))
            {
                dbContext.Remove(translation);
                Translations.RemoveAt(index);
            }
            else
            {
                dbContext.Update(translation);
                translation.Value = text;
            }
        }
        else if (!string.IsNullOrEmpty(text))
        {
            KitTranslation translation = new KitTranslation
            {
                KitId = PrimaryKey,
                Value = text,
                LanguageId = language.Key
            };
            dbContext.Add(translation);
            Translations.Add(translation);
        }

        this.UpdateLastEdited(setter);
        dbContext.Update(this);
    }
}