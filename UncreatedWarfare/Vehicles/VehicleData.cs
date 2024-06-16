using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

public class VehicleData : ITranslationArgument, IListItem
{
    public const int VEHICLE_TYPE_MAX_CHAR_LIMIT = 20;
    [CommandSettable]
    public string Name { get; set; }
    [CommandSettable]
    public Guid VehicleID { get; set; }
    [CommandSettable]
    public PrimaryKey Faction { get; set; }
    [CommandSettable]
    public float RespawnTime { get; set; }
    [CommandSettable]
    public int TicketCost { get; set; }
    [CommandSettable]
    public int CreditCost { get; set; }
    [CommandSettable]
    public float Cooldown { get; set; }
    [CommandSettable]
    public Branch Branch { get; set; }
    [CommandSettable]
    public Class RequiredClass { get; set; }
    [CommandSettable]
    public int RearmCost { get; set; }
    [CommandSettable]
    public VehicleType Type { get; set; }
    [CommandSettable]
    public bool RequiresSL { get; set; }
    [CommandSettable]
    public ushort UnlockLevel { get; set; }
    [CommandSettable]
    public bool DisallowAbandons { get; set; }
    [CommandSettable]
    public bool CrewInvincible { get; set; }
    [CommandSettable]
    public bool PassengersInvincible { get; set; }
    [CommandSettable]
    public float AbandonValueLossSpeed { get; set; } = 0.125f;
    [CommandSettable]
    public int Map { get; set; } = -1;
    public UnlockRequirement[] UnlockRequirements { get; set; }
    public Guid[] Items { get; set; }
    public Delay[] Delays { get; set; }

    [JsonConverter(typeof(ByteArrayConverter))]
    public byte[] CrewSeats { get; set; }
    public MetaSave? Metadata { get; set; }
    [JsonIgnore]
    public PrimaryKey PrimaryKey { get; set; }
    // for JSON backwards compatability
    public ulong Team
    {
        get
        {
            if (TeamManager.Team1Faction.PrimaryKey == Faction)
                return 1ul;
            return TeamManager.Team2Faction.PrimaryKey == Faction ? 2ul : 0ul;
        }
        set
        {
            Faction = value switch
            {
                1ul => TeamManager.Team1Faction.PrimaryKey,
                2ul => TeamManager.Team2Faction.PrimaryKey,
                _ => PrimaryKey.NotAssigned
            };
        }
    }
    public VehicleData(Guid vehicleID)
    {
        VehicleID = vehicleID;
        Team = 0;
        RespawnTime = 600;
        TicketCost = 0;
        CreditCost = 0;
        Cooldown = 0;
        if (Assets.find(vehicleID) is VehicleAsset va)
        {
            Name = va.name;
            if (va.engine == EEngine.PLANE || va.engine == EEngine.HELICOPTER || va.engine == EEngine.BLIMP)
                Branch = Branch.Airforce;
            else if (va.engine == EEngine.BOAT)
                Branch = Branch.Navy;
            else
                Branch = Branch.Infantry;
        }
        else Branch = Branch.Default;
        RequiredClass = Class.None;
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        RearmCost = 3;
        Type = VehicleType.None;
        RequiresSL = false;
        UnlockLevel = 0;
        Items = Array.Empty<Guid>();
        CrewSeats = Array.Empty<byte>();
        Delays = Array.Empty<Delay>();
        Metadata = null;
    }
    public VehicleData()
    {
        Name = string.Empty;
        VehicleID = Guid.Empty;
        Team = 0;
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        RespawnTime = 600;
        TicketCost = 0;
        CreditCost = 0;
        Cooldown = 0;
        Branch = Branch.Default;
        RequiredClass = Class.None;
        RearmCost = 3;
        Type = VehicleType.None;
        RequiresSL = false;
        UnlockLevel = 0;
        Items = Array.Empty<Guid>();
        CrewSeats = Array.Empty<byte>();
        Delays = Array.Empty<Delay>();
        Metadata = null;
    }

                                                                                      // intentional is null check
    public static bool CanTransport(VehicleData data, InteractableVehicle vehicle) => vehicle is not null && CanTransport(data, vehicle.passengers.Length);
    public static bool CanTransport(VehicleData data, int passengerCt) => !IsEmplacement(data.Type) && data.CrewSeats.Length < passengerCt;
    public static bool IsGroundVehicle(VehicleType type) => !IsAircraft(type);
    public static bool IsArmor(VehicleType type) => type is VehicleType.APC or VehicleType.IFV or VehicleType.MBT or VehicleType.ScoutCar;
    public static bool IsLogistics(VehicleType type) => type is VehicleType.LogisticsGround or VehicleType.TransportAir;
    public static bool IsAircraft(VehicleType type) => type is VehicleType.TransportAir or VehicleType.AttackHeli or VehicleType.Jet;
    public static bool IsAssaultAircraft(VehicleType type) => type is VehicleType.AttackHeli or VehicleType.Jet;
    public static bool IsEmplacement(VehicleType type) => type is VehicleType.HMG or VehicleType.ATGM or VehicleType.AA or VehicleType.Mortar;
    public static bool IsFlyingEngine(EEngine engine) => engine is EEngine.BLIMP or EEngine.PLANE or EEngine.HELICOPTER;
    public bool HasDelayType(DelayType type) => Delay.HasDelayType(Delays, type);
    public bool IsDelayed(out Delay delay) => Delay.IsDelayed(Delays, out delay, Team);
    public bool IsCrewSeat(byte seat)
    {
        if (CrewSeats != null)
        {
            for (int i = 0; i < CrewSeats.Length; ++i)
            {
                if (CrewSeats[i] == seat)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public void SaveMetaData(InteractableVehicle vehicle)
    {
        List<VBarricade>? barricades = null;
        List<ISpecificPageKitItem>? trunk = null;

        if (vehicle.trunkItems.items.Count > 0)
        {
            trunk = new List<ISpecificPageKitItem>();

            foreach (ItemJar jar in vehicle.trunkItems.items)
            {
                if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)
                {
                    RedirectType redirect = TeamManager.GetRedirectInfo(asset.GUID, out _, out string? variant, false);
                    //if (redirect != RedirectType.None)
                    //{
                    //    trunk.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned,
                    //        jar.x, jar.y, jar.rot, Page.Storage, redirect, variant));
                    //} todo
                    //else
                    //{
                    trunk.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned,
                        new UnturnedAssetReference(asset.GUID), jar.x, jar.y, jar.rot, Page.Storage,
                        jar.item.amount, jar.item.metadata));
                    //}
                }
            }
        }

        VehicleBarricadeRegion vehicleRegion = BarricadeManager.findRegionFromVehicle(vehicle);
        if (vehicleRegion != null)
        {
            barricades = new List<VBarricade>();
            for (int i = 0; i < vehicleRegion.drops.Count; i++)
            {
                BarricadeData bdata = vehicleRegion.drops[i].GetServersideData();
                barricades.Add(new VBarricade(bdata.barricade.asset.GUID, bdata.barricade.asset.health, bdata.point.x, bdata.point.y,
                    bdata.point.z, bdata.angle_x, bdata.angle_y, bdata.angle_z, Convert.ToBase64String(bdata.barricade.state)));
            }
        }

        if (barricades is not null || trunk is not null)
            Metadata = new MetaSave(barricades, trunk);
    }
    public string GetCostLine(UCPlayer ucplayer)
    {
        UCPlayer.TryApplyViewLens(ref ucplayer);
        if (UnlockRequirements == null || UnlockRequirements.Length == 0)
            return string.Empty;
        else
        {
            for (int i = 0; i < UnlockRequirements.Length; i++)
            {
                UnlockRequirement req = UnlockRequirements[i];
                if (req.CanAccess(ucplayer))
                    continue;
                return req.GetSignText(ucplayer);
            }
        }
        return string.Empty;
    }
    [FormatDisplay("Colored Vehicle Name")]
    public const string COLORED_NAME = "cn";
    [FormatDisplay("Vehicle Name")]
    public const string NAME = "n";
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        string name = Assets.find(VehicleID) is VehicleAsset va ? va.vehicleName : VehicleID.ToString("N");
        if (format is not null && format.Equals(COLORED_NAME, StringComparison.Ordinal))
            return Localization.Colorize(TeamManager.GetTeamHexColor(Team), name, flags);
        return name;
    }
}

public class MetaSave
{
    public List<VBarricade>? Barricades { get; set; }
    public List<ISpecificPageKitItem>? TrunkItems { get; set; } // todo convert to IPageKitItem
    public MetaSave() { }
    public MetaSave(List<VBarricade>? barricades, List<ISpecificPageKitItem>? trunkItems)
    {
        Barricades = barricades;
        TrunkItems = trunkItems;
    }
}

public class VBarricade : IListSubItem
{
    public Guid BarricadeID { get; set; }
    public ushort Health { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float AngleX { get; set; }
    public float AngleY { get; set; }
    public float AngleZ { get; set; }
    [JsonIgnore]
    public byte[] Metadata { get; set; }
    // for backwards compatability
    public string State
    {
        get => Convert.ToBase64String(Metadata);
        set => Metadata = Convert.FromBase64String(value);
    }
    public PrimaryKey LinkedKey { get; set; }
    public PrimaryKey PrimaryKey { get; set; }
    public VBarricade() { }
    public VBarricade(Guid barricadeID, ushort health, float posX, float posY, float posZ, float angleX, float angleY, float angleZ, string state)
    {
        BarricadeID = barricadeID;
        Health = health;
        PosX = posX;
        PosY = posY;
        PosZ = posZ;
        AngleX = angleX;
        AngleY = angleY;
        AngleZ = angleZ;
        State = state;
    }
    public VBarricade(Guid barricadeID, ushort health, float posX, float posY, float posZ, float angleX, float angleY, float angleZ, byte[] state)
    {
        BarricadeID = barricadeID;
        Health = health;
        PosX = posX;
        PosY = posY;
        PosZ = posZ;
        AngleX = angleX;
        AngleY = angleY;
        AngleZ = angleZ;
        Metadata = state;
    }

    // ReSharper disable InconsistentNaming

    public const string COLUMN_PK = "pk";
    public const string COLUMN_GUID = "Item";
    public const string COLUMN_HEALTH = "Health";
    public const string COLUMN_POS_X = "Pos_X";
    public const string COLUMN_POS_Y = "Pos_Y";
    public const string COLUMN_POS_Z = "Pos_Z";
    public const string COLUMN_ROT_X = "Rot_X";
    public const string COLUMN_ROT_Y = "Rot_Y";
    public const string COLUMN_ROT_Z = "Rot_Z";
    public const string COLUMN_METADATA = "Metadata";
    public const string COLUMN_ITEM_PK = "pk";
    public const string COLUMN_ITEM_BARRICADE_PK = "Barricade";
    public const string COLUMN_ITEM_GUID = "Guid";
    public const string COLUMN_ITEM_POS_X = "Pos_X";
    public const string COLUMN_ITEM_POS_Y = "Pos_Y";
    public const string COLUMN_ITEM_ROT = "Rotation";
    public const string COLUMN_ITEM_AMOUNT = "Amount";
    public const string COLUMN_ITEM_QUALITY = "Quality";
    public const string COLUMN_ITEM_METADATA = "Metadata";
    public const string COLUMN_DISPLAY_SKIN = "Skin";
    public const string COLUMN_DISPLAY_MYTHIC = "Mythic";
    public const string COLUMN_DISPLAY_TAGS = "Tags";
    public const string COLUMN_DISPLAY_DYNAMIC_PROPS = "DynamicProps";
    public const string COLUMN_DISPLAY_ROT = "Rotation";

    // ReSharper restore InconsistentNaming
    public static Schema[] GetDefaultSchemas(string tableName, string tableItemsName, string tableDisplayDataName, string fkColumn, string mainTable, string mainPkColumn, bool includeHealth = true, bool oneToOne = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 9;
        if (!oneToOne)
            ++ct;
        if (includeHealth)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_GUID, SqlTypes.GUID_STRING);
        if (includeHealth)
        {
            columns[++index] = new Schema.Column(COLUMN_HEALTH, SqlTypes.USHORT)
            {
                Default = "'" + ushort.MaxValue.ToString(Data.AdminLocale) + "'"
            };
        }
        columns[++index] = new Schema.Column(COLUMN_POS_X, SqlTypes.FLOAT);
        columns[++index] = new Schema.Column(COLUMN_POS_Y, SqlTypes.FLOAT);
        columns[++index] = new Schema.Column(COLUMN_POS_Z, SqlTypes.FLOAT);
        columns[++index] = new Schema.Column(COLUMN_ROT_X, SqlTypes.FLOAT);
        columns[++index] = new Schema.Column(COLUMN_ROT_Y, SqlTypes.FLOAT);
        columns[++index] = new Schema.Column(COLUMN_ROT_Z, SqlTypes.FLOAT);
        columns[++index] = new Schema.Column(COLUMN_METADATA, SqlTypes.BYTES_255);
        Schema[] schemas = new Schema[3];
        schemas[0] = new Schema(tableName, columns, false, typeof(VBarricade));
        schemas[1] = new Schema(tableDisplayDataName, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                AutoIncrement = true,
                PrimaryKey = true,
                ForeignKey = true,
                ForeignKeyColumn = oneToOne ? fkColumn : COLUMN_PK,
                ForeignKeyTable = tableName
            },
            new Schema.Column(COLUMN_DISPLAY_SKIN, SqlTypes.USHORT),
            new Schema.Column(COLUMN_DISPLAY_MYTHIC, SqlTypes.USHORT),
            new Schema.Column(COLUMN_DISPLAY_ROT, SqlTypes.BYTE),
            new Schema.Column(COLUMN_DISPLAY_TAGS, SqlTypes.STRING_255)
            {
                Nullable = true
            },
            new Schema.Column(COLUMN_DISPLAY_DYNAMIC_PROPS, SqlTypes.STRING_255)
            {
                Nullable = true
            }
        }, false, typeof(Structures.ItemDisplayData));
        schemas[2] = new Schema(tableItemsName, new Schema.Column[]
        {
            new Schema.Column(COLUMN_ITEM_PK, SqlTypes.INCREMENT_KEY)
            {
                AutoIncrement = true,
                PrimaryKey = true,
            },
            new Schema.Column(COLUMN_ITEM_BARRICADE_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = oneToOne ? fkColumn : COLUMN_PK,
                ForeignKeyTable = tableName
            },
            new Schema.Column(COLUMN_ITEM_GUID, SqlTypes.GUID_STRING),
            new Schema.Column(COLUMN_ITEM_AMOUNT, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_QUALITY, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_POS_X, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_POS_Y, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_ROT, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_METADATA, SqlTypes.BYTES_255),
        }, false, typeof(Structures.ItemJarData));
        return schemas;
    }
}

[Translatable("Vehicle Type", Description = "Variant of vehicle, influences death messages, XP toasts, and spotting icons.")]
public enum VehicleType
{
    [Translatable("Unknown", IsPrioritizedTranslation = false)]
    None,
    [Translatable(Languages.Russian, "Хамви")]
    [Translatable(Languages.Spanish, "Humvee")]
    [Translatable(Languages.Romanian, "Humvee")]
    [Translatable(Languages.PortugueseBrazil, "Humvee")]
    [Translatable(Languages.Polish, "Humvee")]
    [Translatable(Languages.ChineseSimplified, "悍马")]
    Humvee,
    [Translatable(Languages.Russian, "Транспорт")]
    [Translatable(Languages.Spanish, "Transporte")]
    [Translatable(Languages.Romanian, "Transport")]
    [Translatable(Languages.PortugueseBrazil, "Transporte")]
    [Translatable(Languages.Polish, "Humvee")]
    [Translatable(Languages.ChineseSimplified, "运输卡车")]
    [Translatable("Transport Truck")]
    TransportGround,
    [Translatable(Languages.ChineseSimplified, "侦查车")]
    ScoutCar,
    [Translatable(Languages.Russian, "Логистический")]
    [Translatable(Languages.Spanish, "Logistico")]
    [Translatable(Languages.Romanian, "Camion")]
    [Translatable(Languages.PortugueseBrazil, "Logística")]
    [Translatable(Languages.Polish, "Transport Logistyczny")]
    [Translatable(Languages.ChineseSimplified, "补给卡车")]
    [Translatable("Logistics Truck")]
    LogisticsGround,
    [Translatable(Languages.Russian, "БТР")]
    [Translatable(Languages.Spanish, "APC")]
    [Translatable(Languages.Romanian, "TAB")]
    [Translatable(Languages.Polish, "APC")]
    [Translatable(Languages.ChineseSimplified, "轮式步战车")]
    [Translatable("APC")]
    APC,
    [Translatable(Languages.Russian, "БМП")]
    [Translatable(Languages.Spanish, "IFV")]
    [Translatable(Languages.Romanian, "MLI")]
    [Translatable(Languages.Polish, "BWP")]
    [Translatable(Languages.ChineseSimplified, "步战车")]
    [Translatable("IFV")]
    IFV,
    [Translatable(Languages.Russian, "ТАНК")]
    [Translatable(Languages.Spanish, "Tanque")]
    [Translatable(Languages.Romanian, "Tanc")]
    [Translatable(Languages.PortugueseBrazil, "Tanque")]
    [Translatable(Languages.Polish, "Czołg")]
    [Translatable(Languages.ChineseSimplified, "坦克")]
    [Translatable("Tank")]
    MBT,
    [Translatable(Languages.Russian, "Верталёт")]
    [Translatable(Languages.Spanish, "Helicoptero")]
    [Translatable(Languages.Romanian, "Elicopter")]
    [Translatable(Languages.PortugueseBrazil, "Helicóptero")]
    [Translatable(Languages.Polish, "Helikopter")]
    [Translatable(Languages.ChineseSimplified, "运输直升机")]
    [Translatable("Transport Heli")]
    TransportAir,
    [Translatable(Languages.Russian, "Верталёт")]
    [Translatable(Languages.Spanish, "Helicoptero")]
    [Translatable(Languages.Romanian, "Elicopter")]
    [Translatable(Languages.PortugueseBrazil, "Helicóptero")]
    [Translatable(Languages.Polish, "Helikopter")]
    [Translatable(Languages.ChineseSimplified, "武装直升机")]
    [Translatable("Attack Heli")]
    AttackHeli,
    [Translatable(Languages.Russian, "реактивный")]
    [Translatable(Languages.ChineseSimplified, "战斗机")]
    [Translatable("Jet")]
    Jet,
    [Translatable(Languages.Russian, "Размещение")]
    [Translatable(Languages.Spanish, "Emplazamiento")]
    [Translatable(Languages.Romanian, "Amplasament")]
    [Translatable(Languages.PortugueseBrazil, "Emplacamento")]
    [Translatable(Languages.Polish, "Fortyfikacja")]
    [Translatable("Emplacement", IsPrioritizedTranslation = false)]
    [Obsolete("Use the individual emplacement types instead.", true)]
    Emplacement,
    [Translatable(Languages.Russian, "зенитный")]
    [Translatable(Languages.ChineseSimplified, "防空")]
    [Translatable("Anti-Aircraft")]
    AA,
    [Translatable(Languages.Russian, "Тяжелый пулемет")]
    [Translatable(Languages.ChineseSimplified, "重机枪")]
    [Translatable("Heavy Machine Gun")]
    HMG,
    [Translatable(Languages.Russian, "противотанковая ракета")]
    [Translatable(Languages.ChineseSimplified, "反坦克导弹发射器")]
    [Translatable("ATGM")]
    ATGM,
    [Translatable(Languages.Russian, "Миномет")]
    [Translatable(Languages.Spanish, "Mortero")]
    [Translatable(Languages.ChineseSimplified, "迫击炮")]
    [Translatable("Mortar")]
    Mortar
}

/// <summary>Max field character limit: <see cref="Delay.DELAY_TYPE_MAX_CHAR_LIMIT"/>.</summary>
public enum DelayType
{
    None = 0,
    Time = 1,
    /// <summary><see cref="VehicleData.Team"/> must be set.</summary>
    Flag = 2,
    /// <summary><see cref="VehicleData.Team"/> must be set.</summary>
    FlagPercentage = 3,
    OutOfStaging = 4,
    Teammates = 5
}
[JsonConverter(typeof(DelayConverter))]
public struct Delay : IJsonReadWrite
{
    public const int DELAY_TYPE_MAX_CHAR_LIMIT = 16;
    public static readonly Delay Nil = new Delay(DelayType.None, float.NaN, null);
    [JsonIgnore]
    public bool IsNil => float.IsNaN(Value);
    public DelayType Type;
    public string? Gamemode;
    public float Value;
    public Delay(DelayType type, float value, string? gamemode = null)
    {
        Type = type;
        Value = value;
        Gamemode = gamemode;
    }
    public override string ToString() =>
        $"{Type} Delay, {(string.IsNullOrEmpty(Gamemode) ? "any" : Gamemode)} " +
        $"gamemode{(!UsesValue(Type) ? string.Empty : $" Value: {Value}")}";

    public static bool UsesValue(DelayType type) => type is not DelayType.None and not DelayType.OutOfStaging;
    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteNumber(nameof(Type), (int)Type);
        writer.WriteString(nameof(Gamemode), Gamemode);
        writer.WriteNumber(nameof(Value), Value);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.TokenType == JsonTokenType.PropertyName || (reader.Read() && reader.TokenType == JsonTokenType.PropertyName))
        {
            string? prop = reader.GetString();
            if (reader.Read() && prop != null)
            {
                if (prop.Equals(nameof(Type), StringComparison.OrdinalIgnoreCase))
                {
                    if (reader.TryGetInt32(out int i))
                        Type = (DelayType)i;
                }
                else if (prop.Equals(nameof(Gamemode), StringComparison.OrdinalIgnoreCase))
                {
                    if (reader.TokenType == JsonTokenType.Null) Gamemode = null;
                    else Gamemode = reader.GetString();
                }
                else if (prop.Equals(nameof(Value), StringComparison.OrdinalIgnoreCase))
                {
                    reader.TryGetSingle(out Value);
                }
            }
        }
    }
    public static void AddDelay(ref Delay[] delays, DelayType type, float value, string? gamemode = null)
    {
        int index = -1;
        for (int i = 0; i < delays.Length; i++)
        {
            ref Delay del = ref delays[i];
            if (del.Type == type && Math.Abs(del.Value - value) < float.Epsilon && (del.Gamemode == gamemode || (string.IsNullOrEmpty(del.Gamemode) && string.IsNullOrEmpty(gamemode))))
            {
                index = i;
                break;
            }
        }
        if (index == -1)
        {
            Delay del = new Delay(type, value, gamemode);
            Delay[] old = delays;
            delays = new Delay[old.Length + 1];
            if (old.Length > 0)
            {
                Array.Copy(old, 0, delays, 0, old.Length);
                delays[delays.Length - 1] = del;
            }
            else
            {
                delays[0] = del;
            }
        }
    }
    public static bool RemoveDelay(ref Delay[] delays, DelayType type, float value, string? gamemode = null)
    {
        if (delays.Length == 0) return false;
        int index = -1;
        for (int i = 0; i < delays.Length; i++)
        {
            ref Delay del = ref delays[i];
            if (del.Type == type && del.Value == value && (del.Gamemode == gamemode || (string.IsNullOrEmpty(del.Gamemode) && string.IsNullOrEmpty(gamemode))))
            {
                index = i;
                break;
            }
        }
        if (index == -1) return false;
        Delay[] old = delays;
        delays = new Delay[old.Length - 1];
        if (old.Length == 1) return true;
        if (index != 0)
            Array.Copy(old, 0, delays, 0, index);
        Array.Copy(old, index + 1, delays, index, old.Length - index - 1);
        return true;
    }
    public static bool HasDelayType(Delay[] delays, DelayType type)
    {
        string gm = Data.Gamemode.Name;
        for (int i = 0; i < delays.Length; i++)
        {
            ref Delay del = ref delays[i];
            if (!string.IsNullOrEmpty(del.Gamemode) && !gm.Equals(del.Gamemode, StringComparison.OrdinalIgnoreCase)) continue;
            if (del.Type == type) return true;
        }
        return false;
    }
    public static bool IsDelayedType(Delay[] delays, DelayType type, ulong team)
    {
        string gm = Data.Gamemode.Name;
        for (int i = 0; i < delays.Length; i++)
        {
            ref Delay del = ref delays[i];
            if (!string.IsNullOrEmpty(del.Gamemode))
            {
                string gamemode = del.Gamemode!;
                bool blacklist = false;
                if (gamemode[0] == '!')
                {
                    blacklist = true;
                    gamemode = gamemode.Substring(1);
                }

                if (gm.Equals(gamemode, StringComparison.OrdinalIgnoreCase))
                {
                    if (blacklist) continue;
                }
                else if (!blacklist) continue;
            }
            if (del.Type == type)
            {
                switch (type)
                {
                    case DelayType.None:
                        return false;
                    case DelayType.Time:
                        if (TimeDelayed(ref del))
                            return true;
                        break;
                    case DelayType.Flag:
                        if (FlagDelayed(ref del, team))
                            return true;
                        break;
                    case DelayType.FlagPercentage:
                        if (FlagPercentDelayed(ref del, team))
                            return true;
                        break;
                    case DelayType.OutOfStaging:
                        if (StagingDelayed(ref del))
                            return true;
                        break;
                    case DelayType.Teammates:
                        if (TeammatesDelayed(ref del))
                            return true;
                        break;
                }
            }
        }
        return false;
    }
    // TODO: gamemode blacklist not working
    public static bool IsDelayed(Delay[] delays, out Delay delay, ulong team)
    {
        delay = Nil;
        string? gm = Data.Gamemode?.Name;
        if (delays == null || delays.Length == 0) return false;
        bool anyVal = false;
        bool isNoneYet = false;
        for (int i = delays.Length - 1; i >= 0; i--)
        {
            ref Delay del = ref delays[i];
            bool universal = string.IsNullOrEmpty(del.Gamemode);
            if (!universal)
            {
                string gamemode = del.Gamemode!; // !TeamCTF
                bool blacklist = false;
                if (gamemode[0] == '!') // true
                {
                    blacklist = true;
                    gamemode = gamemode.Substring(1); // TeamCTF
                }

                if (gm is not null && gm.Equals(gamemode, StringComparison.OrdinalIgnoreCase)) // false
                {
                    if (blacklist) continue;
                }
                else if (!blacklist) continue; // false
                universal = true;
            }
            if (universal && anyVal) continue;
            switch (del.Type)
            {
                case DelayType.None:
                    if (!universal)
                    {
                        delay = del;
                        isNoneYet = true;
                    }
                    break;
                case DelayType.Time:
                    if ((!universal || !isNoneYet) && TimeDelayed(ref del))
                    {
                        delay = del;
                        if (!universal) return true;
                        anyVal = true;
                    }
                    break;
                case DelayType.Flag:
                    if ((!universal || !isNoneYet) && FlagDelayed(ref del, team))
                    {
                        delay = del;
                        if (!universal) return true;
                        anyVal = true;
                    }
                    break;
                case DelayType.FlagPercentage:
                    if ((!universal || !isNoneYet) && FlagPercentDelayed(ref del, team))
                    {
                        delay = del;
                        if (!universal) return true;
                        anyVal = true;
                    }
                    break;
                case DelayType.OutOfStaging:
                    if ((!universal || !isNoneYet) && StagingDelayed(ref del))
                    {
                        delay = del;
                        if (!universal) return true;
                        anyVal = true;
                    }
                    break;
                case DelayType.Teammates:
                    if ((!universal || !isNoneYet) && TeammatesDelayed(ref del))
                    {
                        delay = del;
                        if (!universal) return true;
                        anyVal = true;
                    }
                    break;
            }
        }
        return anyVal;
    }
    private static bool TimeDelayed(ref Delay delay) => Data.Gamemode != null && delay.Value > Data.Gamemode.SecondsSinceStart;
    private static bool TeammatesDelayed(ref Delay delay) => Data.Gamemode != null &&
        delay.Value > 0 && 
        Mathf.Min(
            PlayerManager.OnlinePlayers.Where(p => p.IsTeam1).Count(), 
            PlayerManager.OnlinePlayers.Where(p => p.IsTeam2).Count()
            ) < Mathf.FloorToInt(delay.Value);
    private static bool FlagDelayed(ref Delay delay, ulong team) => FlagDelayed(ref delay, false, team);
    private static bool FlagPercentDelayed(ref Delay delay, ulong team) => FlagDelayed(ref delay, true, team);
    private static bool FlagDelayed(ref Delay delay, bool percent, ulong team)
    {
        if (Data.Is(out Invasion inv))
        {
            int ct = percent ? Mathf.RoundToInt(inv.Rotation.Count * delay.Value / 100f) : Mathf.RoundToInt(delay.Value);
            if (team == 1)
            {
                if (inv.AttackingTeam == 1)
                    return inv.ObjectiveT1Index < ct;
                else
                    return inv.Rotation.Count - inv.ObjectiveT2Index - 1 < ct;
            }
            else if (team == 2)
            {
                if (inv.AttackingTeam == 2)
                    return inv.Rotation.Count - inv.ObjectiveT2Index - 1 < ct;
                else
                    return inv.ObjectiveT1Index < ct;
            }
            return false;
        }
        else if (Data.Is(out IFlagTeamObjectiveGamemode fr))
        {
            int ct = percent ? Mathf.RoundToInt(fr.Rotation.Count * delay.Value / 100f) : Mathf.RoundToInt(delay.Value);
            int i2 = GetHighestObjectiveIndex(team, fr);
            return (team == 1 && i2 < ct) ||
                   (team == 2 && fr.Rotation.Count - i2 - 1 < ct);
        }
        else if (Data.Is(out Insurgency ins))
        {
            int ct = percent ? Mathf.RoundToInt(ins.Caches.Count * delay.Value / 100f) : Mathf.RoundToInt(delay.Value);
            return ins.Caches != null && ins.CachesDestroyed < ct;
        }
        return false;
    }
    private static bool StagingDelayed(ref Delay delay) => Data.Is(out IStagingPhase sp) && sp.State == State.Staging;
    private static int GetHighestObjectiveIndex(ulong team, IFlagTeamObjectiveGamemode gm)
    {
        if (team == 1)
        {
            for (int i = 0; i < gm.Rotation.Count; i++)
            {
                if (!gm.Rotation[i].HasBeenCapturedT1)
                    return i;
            }
            return 0;
        }
        else if (team == 2)
        {
            for (int i = gm.Rotation.Count - 1; i >= 0; i--)
            {
                if (!gm.Rotation[i].HasBeenCapturedT2)
                    return i;
            }
            return gm.Rotation.Count - 1;
        }
        return -1;
    }

    public const string COLUMN_PK = "pk";
    public const string COLUMN_TYPE = "Type";
    public const string COLUMN_GAMEMODE = "Gamemode";
    public const string COLUMN_VALUE = "Value";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 4;
        if (!oneToOne && hasPk)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_TYPE, SqlTypes.Enum(DelayType.None));
        columns[++index] = new Schema.Column(COLUMN_VALUE, SqlTypes.FLOAT)
        {
            Nullable = true
        };
        columns[++index] = new Schema.Column(COLUMN_GAMEMODE, "varchar(32)")
        {
            Nullable = true
        };
        return new Schema(tableName, columns, false, typeof(Delay));
    }
}
internal sealed class ByteArrayConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null!;
            case JsonTokenType.String:
                string str = reader.GetString()!;
                try
                {
                    return Convert.FromBase64String(str);
                }
                catch (FormatException ex)
                {
                    throw new JsonException("Unexpected token: Non-Base64 String.", ex);
                }
            case JsonTokenType.Number:
                if (reader.TryGetByte(out byte b))
                    return new byte[] { b };
                throw new JsonException("Unexpected token: Number > 255 or < 0.");
            case JsonTokenType.False:
                return new byte[1];
            case JsonTokenType.True:
                return new byte[] { 1 };
            case JsonTokenType.StartArray:
                List<byte> bytes = new List<byte>(8);
                while (reader.Read() && reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetByte(out byte val))
                        bytes.Add(val);
                    else throw new JsonException("Invalid number in array[" + bytes.Count + "]");
                }
                return bytes.ToArray();
            default:
                throw new JsonException("Unexpected token: " + reader.TokenType + " when reading byte array.");
        }
    }
    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        JsonIndent indent = value.Length < 12 ? writer.StopIndenting() : default;
        for (int i = 0; i < value.Length; ++i)
        {
            writer.WriteNumberValue(value[i]);
        }
        indent.Dispose();
        writer.WriteEndArray();
    }
}
public sealed class DelayConverter : JsonConverter<Delay>
{
    public override Delay Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Delay delay = new Delay();
        delay.ReadJson(ref reader);
        return delay;
    }
    public override void Write(Utf8JsonWriter writer, Delay value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        value.WriteJson(writer);
        writer.WriteEndObject();
    }
}