using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Networking.Purchasing;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits")]
public class Kit : ITranslationArgument, ICloneable, IListItem
{
    private int _listItemArrayVersion = -1;
    private int _listUnlockRequirementsArrayVersion = -1;
    private int _listSimplifiedListVersion = -1;
    private IKitItem[]? _items;
    private UnlockRequirement[]? _unlockRequirements;
    private List<SimplifiedItemListEntry>? _simplifiedItemList;

    [NotMapped]
    PrimaryKey IListItem.PrimaryKey
    {
        get => new PrimaryKey(PrimaryKey);
        set => PrimaryKey = value.Key;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint PrimaryKey { get; set; }

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
    public Class Class { get; set; }
    
    [CommandSettable]
    [Required]
    public Branch Branch { get; set; }
    
    [CommandSettable]
    [Required]
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
        set
        {
            SetItemArray(value);
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
                UpdateUnlockRequirementArray();
                _listUnlockRequirementsArrayVersion = v;
            }

            return _unlockRequirements!;
        }
        set
        {
            SetUnlockRequirementArray(value);
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

    [JsonIgnore]
    [NotMapped]
    internal List<SimplifiedItemListEntry> SimplifiedItemList
    {
        get
        {
            int v = ItemModels.GetListVersion();
            if (_listSimplifiedListVersion != v || _simplifiedItemList == null)
            {
                _simplifiedItemList = SimplifiedItemListEntry.GetSimplifiedItemList(this);
                _listSimplifiedListVersion = v;
            }

            return _simplifiedItemList;
        }
    }

    [JsonIgnore]
    [NotMapped]
    public StripeEliteKit EliteKitInfo { get; set; }

    [JsonIgnore]
    [NotMapped]
    public bool NeedsUpgrade => Type == KitType.Loadout && Season < UCWarfare.Season;

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
    public bool Requestable => !Disabled && (Type is not KitType.Loadout || Season >= UCWarfare.Season || Season < 1) &&
                             IsCurrentMapAllowed() &&
                             (IsFactionAllowed(TeamManager.Team1Faction) || IsFactionAllowed(TeamManager.Team2Faction));
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist.</summary>
    public bool IsRequestable(ulong team) => team is not 1ul and not 2ul ? Requestable : (!Disabled && (Type is not KitType.Loadout || Season >= UCWarfare.Season || Season < 1) &&
                             IsCurrentMapAllowed() &&
                             IsFactionAllowed(TeamManager.GetFaction(team)));
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist.</summary>
    public bool IsRequestable(FactionInfo? faction) => faction is null ? Requestable : (!Disabled && (Type is not KitType.Loadout || Season >= UCWarfare.Season || Season < 1) &&
                                                                               IsCurrentMapAllowed() &&
                                                                               IsFactionAllowed(faction));

    public List<KitFilteredFaction> FactionFilter { get; set; } = new List<KitFilteredFaction>(0);
    public List<KitFilteredMap> MapFilter { get; set; } = new List<KitFilteredMap>(0);
    public List<KitSkillset> Skillsets { get; set; } = new List<KitSkillset>(0);
    public List<KitTranslation> Translations { get; set; } = new List<KitTranslation>(0);
    public List<KitItemModel> ItemModels { get; set; } = new List<KitItemModel>(0);
    public List<KitUnlockRequirement> UnlockRequirementsModels { get; set; } = new List<KitUnlockRequirement>(0);
    public IReadOnlyCollection<KitEliteBundle> Bundles { get; set; } = new List<KitEliteBundle>(0);

    public Kit(string internalName, Class @class, Branch branch, KitType type, SquadLevel squadLevel, FactionInfo? faction)
    {
        Faction = faction?.CreateModel();
        FactionId = faction?.PrimaryKey;
        InternalName = internalName;
        Class = @class;
        Branch = branch;
        Type = type;
        SquadLevel = squadLevel;
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        Season = UCWarfare.Season;
        TeamLimit = KitManager.GetDefaultTeamLimit(@class);
        RequestCooldown = KitManager.GetDefaultRequestCooldown(@class);
        CreatedTimestamp = LastEditedTimestamp = DateTime.UtcNow;
    }
    public Kit(string internalName, Kit copy)
    {
        Faction = copy.Faction;
        FactionId = copy.FactionId;
        InternalName = internalName;
        Class = copy.Class;
        Branch = copy.Branch;
        Type = copy.Type;
        SquadLevel = copy.SquadLevel;
        Skillsets = new List<KitSkillset>(F.CloneList(copy.Skillsets));
        FactionFilter = new List<KitFilteredFaction>(F.CloneList(copy.FactionFilter));
        MapFilter = new List<KitFilteredMap>(F.CloneList(copy.MapFilter));
        ItemModels = new List<KitItemModel>(F.CloneList(copy.ItemModels));
        Translations = new List<KitTranslation>(F.CloneList(copy.Translations));
        UnlockRequirementsModels = new List<KitUnlockRequirement>(F.CloneList(copy.UnlockRequirementsModels));

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
    }
    /// <summary>For loadout.</summary>
    public Kit(string loadout, Class @class, string? displayName, FactionInfo? faction)
    {
        Faction = faction?.CreateModel();
        FactionId = faction?.PrimaryKey;
        InternalName = loadout;
        Class = @class;
        Branch = KitManager.GetDefaultBranch(@class);
        Type = KitType.Loadout;
        SquadLevel = SquadLevel.Member;
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        Season = UCWarfare.Season;
        TeamLimit = KitManager.GetDefaultTeamLimit(@class);
        RequestCooldown = KitManager.GetDefaultRequestCooldown(@class);
        PremiumCost = UCWarfare.Config.LoadoutCost;
        CreatedTimestamp = LastEditedTimestamp = DateTime.UtcNow;
        if (displayName != null)
        {
            LanguageInfo defaultLanguage = Warfare.Localization.GetDefaultLanguage();
            Translations.Add(new KitTranslation
            {
                Kit = this,
                KitId = PrimaryKey,
                Value = displayName,
                Language = defaultLanguage,
                LanguageId = defaultLanguage.Key
            });
        }
    }
    public Kit()
    {
        UnlockRequirements = Array.Empty<UnlockRequirement>();
    }
    public void MarkRemoteItemsDirty()
    {
        _items = null;
        _simplifiedItemList = null;
    }
    public void MarkRemoteUnlockRequirementsDirty()
    {
        _unlockRequirements = null;
    }
    public void MarkLocalItemsDirty()
    {
        SetItemArray(Items);
    }
    public void MarkLocalUnlockRequirementsDirty()
    {
        SetUnlockRequirementArray(UnlockRequirements);
    }

    public bool IsFactionAllowed(FactionInfo? faction)
    {
        if (Type == KitType.Public)
            return faction != TeamManager.Team1Faction && faction != TeamManager.Team2Faction || string.Equals(faction?.FactionId, Faction?.InternalName, StringComparison.Ordinal);

        if (faction == TeamManager.Team1Faction && string.Equals(Faction?.InternalName, TeamManager.Team2Faction?.FactionId, StringComparison.Ordinal) ||
            faction == TeamManager.Team2Faction && string.Equals(Faction?.InternalName, TeamManager.Team1Faction?.FactionId, StringComparison.Ordinal))
            return false;

        if (FactionFilter.NullOrEmpty() || faction is null || !faction.PrimaryKey.IsValid)
            return true;
        
        for (int i = 0; i < FactionFilter.Count; ++i)
            if (faction.PrimaryKey.Key == FactionFilter[i].FactionId)
                return FactionFilterIsWhitelist;

        return !FactionFilterIsWhitelist;
    }
    public bool IsCurrentMapAllowed()
    {
        if (MapFilter.NullOrEmpty())
            return true;
        int map = MapScheduler.Current;
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
    public bool MeetsUnlockRequirements(UCPlayer player)
    {
        if (UnlockRequirements is not { Length: > 0 }) return false;
        for (int i = 0; i < UnlockRequirements.Length; ++i)
        {
            if (!UnlockRequirements[i].CanAccess(player))
                return false;
        }

        return true;
    }
    public string GetDisplayName(LanguageInfo? language = null, bool removeNewLine = true)
    {
        if (Translations is not { Count: > 0 }) return InternalName;
        string rtn;
        language ??= Warfare.Localization.GetDefaultLanguage();
        KitTranslation? translation = Translations.FirstOrDefault(x => x.LanguageId == language.Key);
        if (translation != null)
            rtn = translation.Value ?? InternalName;
        else if (!language.IsDefault)
        {
            language = Warfare.Localization.GetDefaultLanguage();
            translation = Translations.FirstOrDefault(x => x.LanguageId == language.Key);
            if (translation != null)
                rtn = translation.Value ?? InternalName;
            else
                rtn = Translations.FirstOrDefault()?.Value ?? InternalName;
        }
        else
            rtn = Translations.FirstOrDefault()?.Value ?? InternalName;
        if (removeNewLine)
            rtn = rtn.Replace('\n', ' ').Replace("\r", string.Empty);
        return rtn;
    }

    [FormatDisplay("Kit Id")]
    public const string IdFormat = "i";
    [FormatDisplay("Display Name")]
    public const string DisplayNameFormat = "d";
    [FormatDisplay("Class (" + nameof(Warfare.Kits.Class) + ")")]
    public const string ClassFormat = "c";
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(IdFormat, StringComparison.Ordinal))
                return InternalName;
            if (format.Equals(ClassFormat, StringComparison.Ordinal))
                return Warfare.Localization.TranslateEnum(Class, language);
        }

        return GetDisplayName(language);
    }

    public object Clone() => new Kit(InternalName, this);
    private void SetItemArray(IKitItem[] items)
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
                WarfareDatabases.Kits.Update(model);
                workingArray[i] = true;
            }
        }

        for (int i = models.Count - 1; i >= 0; --i)
        {
            KitItemModel model = models[i];
            bool found = false;
            for (int j = 0; j < items.Length; ++j)
            {
                if (items[j].PrimaryKey == model.Id)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                models.RemoveAt(i);
            WarfareDatabases.Kits.Remove(model);
        }

        for (int i = 0; i < items.Length; ++i)
        {
            if (workingArray[i])
                continue;

            IKitItem item = items[i];
            item.PrimaryKey = 0;
            KitItemModel model = item.CreateModel(this);
            models.Add(model);
            WarfareDatabases.Kits.Add(model);
        }

        _listItemArrayVersion = models.GetListVersion();
        _items = items;
    }
    private void UpdateItemArray()
    {
        List<KitItemModel> models = ItemModels;
        if (models is not { Count: > 0 })
        {
            _items = Array.Empty<IKitItem>();
            return;
        }

        bool pooled = UCWarfare.IsLoaded && UCWarfare.IsMainThread;
        List<IKitItem> tempList = pooled ? ListPool<IKitItem>.claim() : new List<IKitItem>(models.Count);
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
                    L.LogWarning($"Skipped item {model.Id} in kit {model.Kit.InternalName}:");
                    L.LogWarning(ex.Message);
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
    private void SetUnlockRequirementArray(UnlockRequirement[] items)
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
                WarfareDatabases.Kits.Update(model);
                workingArray[i] = true;
            }
        }

        for (int i = models.Count - 1; i >= 0; --i)
        {
            KitUnlockRequirement model = models[i];
            bool found = false;
            for (int j = 0; j < items.Length; ++j)
            {
                if (items[j].PrimaryKey == model.Id)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                models.RemoveAt(i);
            WarfareDatabases.Kits.Remove(model);
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
            WarfareDatabases.Kits.Add(model);
            models.Add(model);
        }

        _listUnlockRequirementsArrayVersion = models.GetListVersion();
        _unlockRequirements = items;
    }
    private void UpdateUnlockRequirementArray()
    {
        List<KitUnlockRequirement> models = UnlockRequirementsModels;
        if (models is not { Count: > 0 })
        {
            _unlockRequirements = Array.Empty<UnlockRequirement>();
            return;
        }

        bool pooled = UCWarfare.IsLoaded && UCWarfare.IsMainThread;
        List<UnlockRequirement> tempList = pooled ? ListPool<UnlockRequirement>.claim() : new List<UnlockRequirement>(models.Count);
        try
        {
            foreach (KitUnlockRequirement model in models)
            {
                try
                {
                    UnlockRequirement? requirement = model.CreateRuntimeRequirement();
                    if (requirement != null)
                        tempList.Add(requirement);
                }
                catch (Exception ex)
                {
                    L.LogWarning($"Skipped unlock requirement {model.Id} in kit {model.Kit.InternalName}:");
                    L.LogWarning(ex.GetType().Name + " - " + ex.Message);
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
}