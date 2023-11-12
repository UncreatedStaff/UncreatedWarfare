using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Networking.Purchasing;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits")]

public class Kit : ITranslationArgument, ICloneable, IListItem
{
    private int _listItemArrayVersion = -1;
    private int _listSimplifiedListVersion = -1;
    private IKitItem[]? _items;
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

    [Required]
    public string Id { get; set; }
    
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
    public TranslationList SignText { get; set; }

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
    public UnlockRequirement[] UnlockRequirements { get; set; }

    [Column("Weapons")]
    [CommandSettable("Weapons")]
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

    public Kit(string id, Class @class, Branch branch, KitType type, SquadLevel squadLevel, FactionInfo? faction)
    {
        Faction = faction?.CreateModel();
        Id = id;
        Class = @class;
        Branch = branch;
        Type = type;
        SquadLevel = squadLevel;
        SignText = new TranslationList(id);
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        Season = UCWarfare.Season;
        TeamLimit = KitManager.GetDefaultTeamLimit(@class);
        RequestCooldown = KitManager.GetDefaultRequestCooldown(@class);
        CreatedTimestamp = LastEditedTimestamp = DateTime.UtcNow;
    }
    public Kit(string id, Kit copy)
    {
        Faction = copy.Faction;
        Id = id;
        Class = copy.Class;
        Branch = copy.Branch;
        Type = copy.Type;
        SquadLevel = copy.SquadLevel;
        SignText = copy.SignText.Clone();
        UnlockRequirements = F.CloneArray(copy.UnlockRequirements);
        Skillsets = new List<KitSkillset>(F.CloneList(copy.Skillsets));
        FactionFilter = new List<KitFilteredFaction>(F.CloneList(copy.FactionFilter));
        MapFilter = new List<KitFilteredMap>(F.CloneList(copy.MapFilter));
        ItemModels = new List<KitItemModel>(F.CloneList(copy.ItemModels));

        foreach (KitSkillset skillset in Skillsets)
            skillset.Kit = this;
        foreach (KitFilteredFaction faction in FactionFilter)
            faction.Kit = this;
        foreach (KitFilteredMap map in MapFilter)
            map.Kit = this;
        foreach (KitItemModel item in ItemModels)
            item.Kit = this;

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
        LanguageInfo defaultLanguage = Warfare.Localization.GetDefaultLanguage();
        string? val = copy.SignText.Translate(defaultLanguage);
        if (val != null)
        {
            Translations.Add(new KitTranslation
            {
                Kit = this,
                Id = 0u,
                Value = val,
                Language = defaultLanguage,
                LanguageId = defaultLanguage.Key
            });
        }
    }
    /// <summary>For loadout.</summary>
    public Kit(string loadout, Class @class, string? displayName, FactionInfo? faction)
    {
        Faction = faction?.CreateModel();
        Id = loadout;
        Class = @class;
        Branch = KitManager.GetDefaultBranch(@class);
        Type = KitType.Loadout;
        SquadLevel = SquadLevel.Member;
        SignText = displayName == null ? new TranslationList() : new TranslationList(displayName);
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
                Id = 0u,
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
    public void MarkItemsDirty()
    {
        _items = null;
        _simplifiedItemList = null;
    }

    public bool IsFactionAllowed(FactionInfo? faction)
    {
        if (Type == KitType.Public)
            return faction != TeamManager.Team1Faction && faction != TeamManager.Team2Faction || string.Equals(faction?.FactionId, Faction?.Id, StringComparison.Ordinal);

        if (faction == TeamManager.Team1Faction && string.Equals(Faction?.Id, TeamManager.Team2Faction?.FactionId, StringComparison.Ordinal) ||
            faction == TeamManager.Team2Faction && string.Equals(Faction?.Id, TeamManager.Team1Faction?.FactionId, StringComparison.Ordinal))
            return false;

        if (FactionFilter.NullOrEmpty() || faction is null || !faction.PrimaryKey.IsValid)
            return true;
        
        for (int i = 0; i < FactionFilter.Count; ++i)
            if (string.Equals(FactionFilter[i].Faction?.Id, faction.FactionId, StringComparison.Ordinal))
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
        if (SignText is null) return Id;
        string rtn;
        language ??= Warfare.Localization.GetDefaultLanguage();
        if (SignText.TryGetValue(language.Code, out string val))
            rtn = val ?? Id;
        else if (SignText.Count > 0)
            rtn = SignText.FirstOrDefault().Value ?? Id;
        else rtn = Id;
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
                return Id;
            if (format.Equals(ClassFormat, StringComparison.Ordinal))
                return Warfare.Localization.TranslateEnum(Class, language);
        }

        return GetDisplayName(language);
    }

    public object Clone() => new Kit(Id, this);
    private void SetItemArray(IKitItem[] items)
    {
        List<KitItemModel> models = ItemModels;
        bool[] workingArray = new bool[items.Length];
        for (int i = 0; i < items.Length; ++i)
        {
            IKitItem item = items[i];
            if (!item.PrimaryKey.IsValid)
                continue;

            for (int j = 0; j < models.Count; ++j)
            {
                // todo
            }
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
                    L.LogWarning($"Skipped item {model.Id} in kit {model.Kit.Id}:");
                    L.LogWarning(ex.Message);
                }
            }
        }
        finally
        {
            if (pooled)
                ListPool<IKitItem>.release(tempList);
        }

        _items = tempList.ToArray();
    }
}