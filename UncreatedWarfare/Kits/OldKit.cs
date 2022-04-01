using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.Networking.Encoding;
using Uncreated.Warfare;
using Uncreated.Warfare.Kits;

namespace DNF_Sandbox;
public class OldKit
{
    public string DisplayName
    {
        get
        {
            return this.Class switch
            {
                EClass.UNARMED => "Unarmed",
                EClass.SQUADLEADER => "Squad Leader",
                EClass.RIFLEMAN => "Rifleman",
                EClass.MEDIC => "Medic",
                EClass.BREACHER => "Breacher",
                EClass.AUTOMATIC_RIFLEMAN => "Automatic Rifleman",
                EClass.GRENADIER => "Grenadier",
                EClass.MACHINE_GUNNER => "Machine Gunner",
                EClass.LAT => "Light Anti-Tank",
                EClass.HAT => "Heavy Anti-Tank",
                EClass.MARKSMAN => "Designated Marksman",
                EClass.SNIPER => "Sniper",
                EClass.AP_RIFLEMAN => "Anti-Personnel Rifleman",
                EClass.COMBAT_ENGINEER => "Combat Engineer",
                EClass.CREWMAN => "Crewman",
                EClass.PILOT => "Pilot",
                _ => Name,
            };
        }
    }
    public string Name;
    [JsonSettable]
    public EClass Class;
    [JsonSettable]
    public string SignName;
    [JsonSettable]
    public EBranch Branch;
    [JsonSettable]
    public ulong Team;
    [JsonSettable]
    public ushort Cost;
    [JsonSettable]
    public ushort RequiredLevel;
    [JsonSettable]
    public ushort TicketCost;
    [JsonSettable]
    public bool IsPremium;
    [JsonSettable]
    public float PremiumCost;
    [JsonSettable]
    public bool IsLoadout;
    [JsonSettable]
    public float TeamLimit;
    [JsonSettable]
    public float Cooldown;
    [JsonSettable]
    public bool ShouldClearInventory;
    public List<OldKitItem> Items;
    public List<OldKitClothing> Clothes;
    public List<ulong> AllowedUsers;
    public Dictionary<string, string> SignTexts;
    [JsonSettable]
    public string Weapons;
    public int Requests;
    [JsonConstructor]
    public OldKit()
    {
        Name = "default";
        Items = new List<OldKitItem>();
        Clothes = new List<OldKitClothing>();
        Class = EClass.NONE;
        Branch = EBranch.DEFAULT;
        Team = 0;
        Cost = 0;
        RequiredLevel = 0;
        TicketCost = 1;
        IsPremium = false;
        PremiumCost = 0;
        IsLoadout = false;
        TeamLimit = 1;
        Cooldown = 0;
        SignName = "Default";
        ShouldClearInventory = true;
        AllowedUsers = new List<ulong>();
        SignTexts = new Dictionary<string, string> { { "en-us", SignName } };
        Weapons = string.Empty;
        Requests = 0;
    }
    /// <summary>empty constructor</summary>
    public OldKit(bool dummy) { }
    public static OldKit?[] ReadMany(ByteReader R)
    {
        OldKit?[] kits = new OldKit?[R.ReadInt32()];
        for (int i = 0; i < kits.Length; i++)
        {
            kits[i] = Read(R);
        }
        return kits;
    }
    public static OldKit? Read(ByteReader R)
    {
        if (R.ReadUInt8() == 1) return null;
        OldKit kit = new OldKit(true);
        kit.Name = R.ReadString();
        ushort itemCount = R.ReadUInt16();
        ushort clothesCount = R.ReadUInt16();
        ushort allowedUsersCount = R.ReadUInt16();
        List<OldKitItem> items = new List<OldKitItem>(itemCount);
        List<OldKitClothing> clothes = new List<OldKitClothing>(clothesCount);
        List<ulong> allowedUsers = new List<ulong>(allowedUsersCount);
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new OldKitItem()
            {
                ID = R.ReadUInt16(),
                amount = R.ReadUInt8(),
                quality = R.ReadUInt8(),
                page = R.ReadUInt8(),
                x = R.ReadUInt8(),
                y = R.ReadUInt8(),
                rotation = R.ReadUInt8(),
                metadata = Convert.ToBase64String(R.ReadBytes())
            });
        }
        for (int i = 0; i < clothesCount; i++)
        {
            clothes.Add(new OldKitClothing()
            {
                ID = R.ReadUInt16(),
                quality = R.ReadUInt8(),
                type = R.ReadEnum<EClothingType>(),
                state = Convert.ToBase64String(R.ReadBytes())
            });
        }
        for (int i = 0; i < allowedUsersCount; i++)
            allowedUsers.Add(R.ReadUInt64());
        kit.AllowedUsers = allowedUsers;
        kit.Items = items;
        kit.Clothes = clothes;
        kit.Branch = R.ReadEnum<EBranch>();
        kit.Class = R.ReadEnum<EClass>();
        kit.Cooldown = R.ReadFloat();
        kit.Cost = R.ReadUInt16();
        kit.IsPremium = R.ReadBool();
        kit.IsLoadout = R.ReadBool();
        kit.PremiumCost = R.ReadFloat();
        kit.RequiredLevel = R.ReadUInt16();
        kit.ShouldClearInventory = R.ReadBool();
        kit.Team = R.ReadUInt64();
        kit.TeamLimit = R.ReadFloat();
        kit.TicketCost = R.ReadUInt16();
        return kit;
    }
    public static void WriteMany(ByteWriter W, OldKit[] kits)
    {
        W.Write(kits.Length);
        for (int i = 0; i < kits.Length; i++)
            Write(W, kits[i]);
    }
    public static void Write(ByteWriter W, OldKit kit)
    {
        if (kit == null)
        {
            W.Write((byte)1);
            return;
        }
        else W.Write((byte)0);
        W.Write(kit.Name);
        W.Write((ushort)kit.Items.Count);
        W.Write((ushort)kit.Clothes.Count);
        W.Write((ushort)kit.AllowedUsers.Count);
        for (int i = 0; i < kit.Items.Count; i++)
        {
            OldKitItem item = kit.Items[i];
            W.Write(item.ID);
            W.Write(item.amount);
            W.Write(item.quality);
            W.Write(item.page);
            W.Write(item.x);
            W.Write(item.y);
            W.Write(item.rotation);
            W.Write(Convert.FromBase64String(item.metadata));
        }
        for (int i = 0; i < kit.Clothes.Count; i++)
        {
            OldKitClothing clothing = kit.Clothes[i];
            W.Write(clothing.ID);
            W.Write(clothing.quality);
            W.Write(clothing.type);
            W.Write(Convert.FromBase64String(clothing.state));
        }
        for (int i = 0; i < kit.AllowedUsers.Count; i++)
            W.Write(kit.AllowedUsers[i]);
        W.Write(kit.Branch);
        W.Write(kit.Class);
        W.Write(kit.Cooldown);
        W.Write(kit.Cost);
        W.Write(kit.IsPremium);
        W.Write(kit.IsLoadout);
        W.Write(kit.PremiumCost);
        W.Write(kit.RequiredLevel);
        W.Write(kit.ShouldClearInventory);
        W.Write(kit.Team);
        W.Write(kit.TeamLimit);
        W.Write(kit.TicketCost);
    }
}
public class OldKitItem
{
    public ushort ID;
    public byte x;
    public byte y;
    public byte rotation;
    public byte quality;
    public string metadata;
    public byte amount;
    public byte page;
    [JsonConstructor]
    public OldKitItem(ushort ID, byte x, byte y, byte rotation, byte quality, string metadata, byte amount, byte page)
    {
        this.ID = ID;
        this.x = x;
        this.y = y;
        this.rotation = rotation;
        this.quality = quality;
        this.metadata = metadata;
        this.amount = amount;
        this.page = page;
    }
    public OldKitItem() { }
}
public class OldKitClothing
{
    public ushort ID;
    public byte quality;
    public string state;
    public EClothingType type;
    [JsonConstructor]
    public OldKitClothing(ushort ID, byte quality, string state, EClothingType type)
    {
        this.ID = ID;
        this.quality = quality;
        this.state = state;
        this.type = type;
    }
    public OldKitClothing() { }
}
