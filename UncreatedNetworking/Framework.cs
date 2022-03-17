using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;

namespace Uncreated.Warfare
{
    public struct Log
    {
        public string Message;
        public DateTime timestamp;
        public ConsoleColor color;

        public Log(string Message, ConsoleColor color, DateTime time)
        {
            this.Message = Message;
            this.timestamp = time;
            this.color = color;
        }
        public Log(string Message, ConsoleColor color) : this(Message, color, DateTime.Now) { }

        public Log(string Message) : this(Message, ConsoleColor.DarkGray, DateTime.Now) { }

        public override string ToString() => Message;

        public static Log Read(ByteReader R) =>
            new Log()
            {
                timestamp = R.ReadDateTime(),
                color = (ConsoleColor)R.ReadUInt8(),
                Message = R.ReadString()
            };
        public static void Write(ByteWriter W, Log L)
        {
            W.Write(L.timestamp);
            W.Write((byte)L.color);
            W.Write(L.Message);
        }
        public static Log[] ReadMany(ByteReader R)
        {
            int lenghth = R.ReadInt32();
            Log[] arr = new Log[lenghth];
            for (int i = 0; i < lenghth; i++)
                arr[i] = Read(R);
            return arr;
        }
        public static void WriteMany(ByteWriter W, Log[] A)
        {
            W.Write(A.Length);
            for (int i = 0; i < A.Length; i++)
                Write(W, A[i]);
        }
    }
    public struct FPlayerList
    {
        public ulong Steam64;
        public string Name;
        public byte Team;
        public bool Duty;
        public FPlayerList(ulong Steam64, string Name, byte Team, bool Duty)
        {
            this.Steam64 = Steam64;
            this.Name = Name;
            this.Team = Team;
            this.Duty = Duty;
        }
        public static void Write(ByteWriter W, FPlayerList L)
        {
            W.Write(L.Steam64);
            W.Write(L.Name);
            W.Write(L.Team);
            W.Write(L.Duty);
        }
        public static FPlayerList Read(ByteReader R)
        {
            return new FPlayerList
            {
                Steam64 = R.ReadUInt64(),
                Name = R.ReadString(),
                Team = R.ReadUInt8(),
                Duty = R.ReadBool()
            };
        }
        public static void WriteArray(ByteWriter W, FPlayerList[] L)
        {
            W.Write((byte)L.Length);
            for (int i = 0; i < L.Length; i++)
            {
                W.Write(L[i].Steam64);
                W.Write(L[i].Name);
                W.Write(L[i].Team);
                W.Write(L[i].Duty);
            }
        }
        public static FPlayerList[] ReadArray(ByteReader R)
        {
            byte playerCount = R.ReadUInt8();
            FPlayerList[] list = new FPlayerList[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                list[i] = new FPlayerList
                {
                    Steam64 = R.ReadUInt64(),
                    Name = R.ReadString(),
                    Team = R.ReadUInt8(),
                    Duty = R.ReadBool()
                };
            }
            return list;
        }
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class JsonSettable : Attribute
    {
        public JsonSettable() { }
    }
    /// <summary>Networking abstraction for <see cref="ItemAsset"/>.</summary>
    public class ItemData
    {
        public static readonly Dictionary<byte, Type> DataTypes = new Dictionary<byte, Type>()
        {
            { 0, typeof(ItemData) },
            { 1, typeof(GunData) },
            { 2, typeof(MagazineData) },
            { 3, typeof(ThrowableData) },
            { 4, typeof(ClothingData) },
            { 5, typeof(StorageClothingData) },
            { 6, typeof(BarricadeData) },
            { 7, typeof(StructureData) },
            { 8, typeof(TrapData) },
            { 9, typeof(AttachmentData) },
            { 10, typeof(SightData) },
            { 11, typeof(GripData) },
            { 12, typeof(TacticalData) },
            { 13, typeof(BarrelData) },
            { 14, typeof(ConsumableData) },
            { 15, typeof(UseableData) },
            { 16, typeof(FuelData) },
            { 17, typeof(ZoomData) },
            { 18, typeof(ChargeData) },
            { 19, typeof(RefillData) },
            { 20, typeof(MapData) },
            { 21, typeof(HandcuffData) },
            { 22, typeof(BeaconData) },
            { 23, typeof(SeedData) },
            { 24, typeof(GeneratorData) },
            { 25, typeof(LibraryData) },
            { 26, typeof(StorageData) },
            { 27, typeof(TankData) },
            { 28, typeof(ItemBoxData) },
            { 29, typeof(GearData) },
            { 30, typeof(CloudData) },
            { 31, typeof(DetonatorData) },
            { 32, typeof(FilterData) },
            { 33, typeof(FishingRodData) },
            { 34, typeof(FertilizerData) },
            { 35, typeof(KeyData) },
            { 36, typeof(SupplyData) },
            { 37, typeof(ToolData) },
            { 38, typeof(VehicleRepairToolData) },
            { 39, typeof(TireToolData) },
            { 40, typeof(MeleeData) },
            { 41, typeof(FoodData) },
            { 42, typeof(HealingItemData) },
            { 43, typeof(WaterData) },
            { 44, typeof(HandcuffKeyData) },
            { 45, typeof(BackpackData) },
            { 46, typeof(PantsData) },
            { 47, typeof(ShirtData) },
            { 48, typeof(VestData) },
            { 49, typeof(GlassesData) },
            { 50, typeof(HatData) },
            { 51, typeof(MaskData) }
        };
        public static readonly Dictionary<byte, Type[]> AssetTypes = new Dictionary<byte, Type[]>()
        {
            { 0, new Type[] { typeof(ItemAsset) } },
            { 1, new Type[] { typeof(ItemGunAsset), typeof(ItemWeaponAsset) } },
            { 2, new Type[] { typeof(ItemMagazineAsset), typeof(ItemCaliberAsset) } },
            { 3, new Type[] { typeof(ItemThrowableAsset), typeof(ItemWeaponAsset) } },
            { 4, new Type[] { typeof(ItemClothingAsset) } },
            { 5, new Type[] { typeof(ItemBagAsset), typeof(ItemClothingAsset) } },
            { 6, new Type[] { typeof(ItemBarricadeAsset) } },
            { 7, new Type[] { typeof(ItemStructureAsset) } },
            { 8, new Type[] { typeof(ItemTrapAsset), typeof(ItemBarricadeAsset) } },
            { 9, new Type[] { typeof(ItemCaliberAsset) } },
            { 10, new Type[] { typeof(ItemSightAsset), typeof(ItemCaliberAsset) } },
            { 11, new Type[] { typeof(ItemGripAsset), typeof(ItemCaliberAsset) } },
            { 12, new Type[] { typeof(ItemTacticalAsset), typeof(ItemCaliberAsset) } },
            { 13, new Type[] { typeof(ItemBarrelAsset), typeof(ItemCaliberAsset) } },
            { 14, new Type[] { typeof(ItemConsumeableAsset), typeof(ItemWeaponAsset) } },
            { 15, new Type[] { typeof(ItemWeaponAsset) } },
            { 16, new Type[] { typeof(ItemFuelAsset) } },
            { 17, new Type[] { typeof(ItemOpticAsset) } },
            { 18, new Type[] { typeof(ItemChargeAsset), typeof(ItemBarricadeAsset) } },
            { 19, new Type[] { typeof(ItemRefillAsset) } },
            { 20, new Type[] { typeof(ItemMapAsset) } },
            { 21, new Type[] { typeof(ItemArrestStartAsset) } },
            { 22, new Type[] { typeof(ItemBeaconAsset), typeof(ItemBarricadeAsset) } },
            { 23, new Type[] { typeof(ItemFarmAsset), typeof(ItemBarricadeAsset) } },
            { 24, new Type[] { typeof(ItemGeneratorAsset), typeof(ItemBarricadeAsset) } },
            { 25, new Type[] { typeof(ItemLibraryAsset), typeof(ItemBarricadeAsset) } },
            { 26, new Type[] { typeof(ItemStorageAsset), typeof(ItemBarricadeAsset) } },
            { 27, new Type[] { typeof(ItemTankAsset), typeof(ItemBarricadeAsset) } },
            { 28, new Type[] { typeof(ItemBoxAsset) } },
            { 29, new Type[] { typeof(ItemGearAsset), typeof(ItemClothingAsset) } },
            { 30, new Type[] { typeof(ItemCloudAsset) } },
            { 31, new Type[] { typeof(ItemDetonatorAsset) } },
            { 32, new Type[] { typeof(ItemFilterAsset) } },
            { 33, new Type[] { typeof(ItemFisherAsset) } },
            { 34, new Type[] { typeof(ItemGrowerAsset) } },
            { 35, new Type[] { typeof(ItemKeyAsset) } },
            { 36, new Type[] { typeof(ItemSupplyAsset) } },
            { 37, new Type[] { typeof(ItemToolAsset) } },
            { 38, new Type[] { typeof(ItemVehicleRepairToolAsset), typeof(ItemToolAsset) } },
            { 39, new Type[] { typeof(ItemTireAsset), typeof(ItemVehicleRepairToolAsset), typeof(ItemToolAsset) } },
            { 40, new Type[] { typeof(ItemMeleeAsset), typeof(ItemWeaponAsset) } },
            { 41, new Type[] { typeof(ItemFoodAsset), typeof(ItemConsumeableAsset), typeof(ItemWeaponAsset) } },
            { 42, new Type[] { typeof(ItemMedicalAsset), typeof(ItemConsumeableAsset), typeof(ItemWeaponAsset) } },
            { 43, new Type[] { typeof(ItemWaterAsset), typeof(ItemConsumeableAsset), typeof(ItemWeaponAsset) } },
            { 44, new Type[] { typeof(ItemArrestEndAsset) } },
            { 45, new Type[] { typeof(ItemBackpackAsset), typeof(ItemBagAsset), typeof(ItemClothingAsset) } },
            { 46, new Type[] { typeof(ItemPantsAsset), typeof(ItemBagAsset), typeof(ItemClothingAsset) } },
            { 47, new Type[] { typeof(ItemShirtAsset), typeof(ItemBagAsset), typeof(ItemClothingAsset) } },
            { 48, new Type[] { typeof(ItemVestAsset), typeof(ItemBagAsset), typeof(ItemClothingAsset) } },
            { 49, new Type[] { typeof(ItemGlassesAsset), typeof(ItemGearAsset), typeof(ItemClothingAsset) } },
            { 50, new Type[] { typeof(ItemHatAsset), typeof(ItemGearAsset), typeof(ItemClothingAsset) } },
            { 51, new Type[] { typeof(ItemMaskAsset), typeof(ItemGearAsset), typeof(ItemClothingAsset) } }
        };
        public virtual byte T { get => 0; }
        public ushort ItemID;
        public Guid ItemGUID;
        public string Name;
        public string LocalizedName;
        public string LocalizedDescription;
        public byte SizeX;
        public byte SizeY;
        public EItemType Type;
        public EItemRarity Rarity;
        public ESlotType SlotType;
        public byte Amount;
        public bool SentryAggressive;
        public string DefaultState;
        public ItemData() { }
        public ItemData(ItemAsset asset)
        {
            this.ItemID = asset.id;
            this.ItemGUID = asset.GUID;
            this.Name = asset.name;
            this.LocalizedName = asset.itemName;
            this.LocalizedDescription = asset.itemDescription;
            this.SizeX = asset.size_x;
            this.SizeY = asset.size_y;
            this.Type = asset.type;
            this.Rarity = asset.rarity;
            this.SlotType = asset.slot;
            this.Amount = asset.amount;
            this.SentryAggressive = asset.shouldFriendlySentryTargetUser;
            byte[] state = asset.getState(EItemOrigin.ADMIN);
            if (state.Length == 0)
                this.DefaultState = string.Empty;
            else
                this.DefaultState = Encoding.ASCII.GetString(state);
        }
        public static ItemData[] ReadMany(ByteReader R)
        {
            ItemData[] rtn = new ItemData[R.ReadInt32()];
            for (int i = 0; i < rtn.Length; i++)
                rtn[i] = Read(R);
            return rtn;
        }
        public static ItemData Read(ByteReader R)
        {
            bool isNull = R.ReadBool();
            if (isNull) return null;
            byte type = R.ReadUInt8();
            if (!DataTypes.TryGetValue(type, out Type itemType))
                itemType = typeof(ItemData);
            if (!(Activator.CreateInstance(itemType) is ItemData data)) throw new Exception("Failed to create item type.");
            data.ReadInst(R);
            return data;
        }
        public virtual void ReadInst(ByteReader R)
        {
            ItemID = R.ReadUInt16();
            ItemGUID = R.ReadGUID();
            Name = R.ReadString();
            LocalizedName = R.ReadString();
            LocalizedDescription = R.ReadString();
            SizeX = R.ReadUInt8();
            SizeY = R.ReadUInt8();
            Type = R.ReadEnum<EItemType>();
            Rarity = R.ReadEnum<EItemRarity>();
            SlotType = R.ReadEnum<ESlotType>();
            Amount = R.ReadUInt8();
            DefaultState = Encoding.ASCII.GetString(R.ReadUInt8Array());
        }
        public static void WriteMany(ByteWriter W, ItemData[] A)
        {
            W.Write(A.Length);
            for (int i = 0; i < A.Length; i++)
                Write(W, A[i]);
        }
        public static void Write(ByteWriter W, ItemData D)
        {
            if (D == null)
            {
                W.Write(true);
                return;
            } else
            {
                W.Write(false);
                D.WriteInst(W);
            }
        }
        public virtual void WriteInst(ByteWriter W)
        {
            W.Write(T);
            W.Write(ItemID);
            W.Write(ItemGUID);
            W.Write(Name ?? ItemID.ToString());
            W.Write(LocalizedName ?? ItemID.ToString());
            W.Write(LocalizedDescription ?? string.Empty);
            W.Write(SizeX);
            W.Write(SizeY);
            W.Write(Type);
            W.Write(Rarity);
            W.Write(SlotType);
            W.Write(Amount);
            if (DefaultState.Length == 0)
                W.Write((ushort)0);
            else
                W.Write(Encoding.ASCII.GetBytes(DefaultState));
        }
        public static ItemData FromAsset(ItemAsset asset)
        {
            Type assetType = asset.GetType();
            byte t = 0;
            bool found = false;
            foreach (KeyValuePair<byte, Type[]> typePair in AssetTypes)
            {
                if (typePair.Value[0] == assetType)
                {
                    t = typePair.Key;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                byte len = 0;
                foreach (KeyValuePair<byte, Type[]> typePair in AssetTypes)
                {
                    if (typePair.Value.Length <= len) continue;
                    for (int i = 0; i < typePair.Value.Length; i++)
                    {
                        if (typePair.Value[i].IsAssignableFrom(assetType))
                        {
                            t = typePair.Key;
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
            }
            if (!DataTypes.TryGetValue(t, out Type dataType))
                dataType = typeof(ItemData);
            if (!(Activator.CreateInstance(dataType, asset) is ItemData data)) throw new Exception("Failed to create item type.");
            return data;
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemMagazineAsset"/>.</summary>
    public class GunData : UseableData
    {
        // relook through this tm
        public override byte T => 1;
        public ushort[] MagazineCalibers;
        public ushort[] AttachmentCalibers;
        public byte Firerate;
        public EAction ShootAction;
        public bool DeleteEmptyMagazines;
        public int Bursts;
        public EFireRate FireRates;
        public bool IsTurret;
        public float SpreadADS;
        public float SpreadHipfire;
        public float SpreadSprint;
        public float SpreadCrouch;
        public float SpreadProne;
        public float RecoilADS;
        public bool UseRecoilADS;
        public float RecoilMinX;
        public float RecoilMinY;
        public float RecoilMaxX;
        public float RecoilMaxY;
        public float RecoverX;
        public float RecoverY;
        public float RecoilSprint;
        public float RecoilCrouch;
        public float RecoilProne;
        public float ShakeMinX;
        public float ShakeMinY;
        public float ShakeMinZ;
        public float ShakeMaxX;
        public float ShakeMaxY;
        public float ShakeMaxZ;
        public byte BallisticSteps;
        public float BallisticTravel;
        public float BallisticDrop;
        public float BallisticForce;
        public float ProjectileLifespan;
        public bool PenetrateBuildables;
        public float ReloadTime;
        public float HammerTime;
        public float AlertRadius;
        public float RangefinderRange;
        public bool HeadshotInstakill;
        public bool InfiniteAmmo;
        public byte AmmoPerShot;
        public int FireDelay;
        public bool CanChangeMagazines;
        public bool SprintAiming;
        public bool CanJam;
        public float MaxQualityToJam;
        public float JamMaxChance;
        public float GunshotRolloffDistance;
        public ushort DefaultSight;
        public ushort DefaultTactical;
        public ushort DefaultGrip;
        public ushort DefaultBarrel;
        public ushort DefaultMagazine;
        public float UnloadStepTime;
        public float ReloadStepTime;
        public bool HasSight;
        public bool HasTactical;
        public bool HasGrip;
        public bool HasBarrel;
        public GunData() : base() { }
        public GunData(ItemGunAsset asset) : base(asset)
        {
            this.MagazineCalibers = asset.magazineCalibers;
            this.AttachmentCalibers = asset.attachmentCalibers;
            this.Firerate = asset.firerate;
            this.ShootAction = asset.action;
            this.DeleteEmptyMagazines = asset.shouldDeleteEmptyMagazines;
            this.Bursts = asset.bursts;
            this.FireRates = EFireRate.NONE;
            if (asset.hasSafety) this.FireRates |= EFireRate.SAFETY;
            if (asset.hasSemi) this.FireRates |= EFireRate.SEMI;
            if (asset.hasBurst) this.FireRates |= EFireRate.BURST;
            if (asset.hasAuto) this.FireRates |= EFireRate.AUTO;
            this.IsTurret = asset.isTurret;
            this.SpreadADS = asset.spreadAim;
            this.SpreadHipfire = asset.spreadHip;
            this.SpreadSprint = asset.spreadSprint;
            this.SpreadCrouch = asset.spreadCrouch;
            this.SpreadProne = asset.spreadProne;
            this.RecoilADS = asset.recoilAim;
            this.UseRecoilADS = asset.useRecoilAim;
            this.RecoilMinX = asset.recoilMin_x;
            this.RecoilMinY = asset.recoilMin_y;
            this.RecoilMaxX = asset.recoilMax_x;
            this.RecoilMaxY = asset.recoilMax_y;
            this.RecoverX = asset.recover_x;
            this.RecoverY = asset.recover_y;
            this.RecoilSprint = asset.recoilSprint;
            this.RecoilCrouch = asset.recoilCrouch;
            this.RecoilProne = asset.recoilProne;
            this.ShakeMinX = asset.shakeMin_x;
            this.ShakeMinY = asset.shakeMin_y;
            this.ShakeMinZ = asset.shakeMin_z;
            this.ShakeMaxX = asset.shakeMax_x;
            this.ShakeMaxY = asset.shakeMax_y;
            this.ShakeMaxZ = asset.shakeMax_z;
            this.BallisticSteps = asset.ballisticSteps;
            this.BallisticTravel = asset.ballisticTravel;
            this.BallisticDrop = asset.ballisticDrop;
            this.BallisticForce = asset.ballisticForce;
            this.ProjectileLifespan = asset.projectileLifespan;
            this.PenetrateBuildables = asset.projectilePenetrateBuildables;
            this.ReloadTime = asset.reloadTime;
            this.HammerTime = asset.hammerTime;
            this.AlertRadius = asset.alertRadius;
            this.RangefinderRange = asset.rangeRangefinder;
            this.HeadshotInstakill = asset.instakillHeadshots;
            this.InfiniteAmmo = asset.infiniteAmmo;
            this.AmmoPerShot = asset.ammoPerShot;
            this.FireDelay = asset.fireDelay;
            this.CanChangeMagazines = asset.allowMagazineChange;
            this.SprintAiming = asset.canAimDuringSprint;
            this.CanJam = asset.canEverJam;
            this.MaxQualityToJam = asset.jamQualityThreshold;
            this.JamMaxChance = asset.jamMaxChance;
            this.GunshotRolloffDistance = asset.gunshotRolloffDistance;
            this.DefaultSight = asset.sightID;
            this.DefaultTactical = asset.tacticalID;
            this.DefaultGrip = asset.gripID;
            this.DefaultBarrel = asset.barrelID;
            this.DefaultMagazine = asset.getMagazineID();
            this.UnloadStepTime = asset.unplace;
            this.ReloadStepTime = asset.replace;
            this.HasSight = asset.hasSight;
            this.HasTactical = asset.hasTactical;
            this.HasGrip = asset.hasGrip;
            this.HasBarrel = asset.hasBarrel;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            AttachmentCalibers = R.ReadUInt16Array();
            MagazineCalibers = R.ReadUInt16Array();
            Firerate = R.ReadUInt8();
            ShootAction = R.ReadEnum<EAction>();
            DeleteEmptyMagazines = R.ReadBool();
            Bursts = R.ReadInt32();
            FireRates = R.ReadEnum<EFireRate>();
            IsTurret = R.ReadBool();
            SpreadADS = R.ReadFloat();
            SpreadHipfire = R.ReadFloat();
            SpreadSprint = R.ReadFloat();
            SpreadCrouch = R.ReadFloat();
            SpreadProne = R.ReadFloat();
            RecoilADS = R.ReadFloat();
            UseRecoilADS = R.ReadBool();
            RecoilMinX = R.ReadFloat();
            RecoilMinY = R.ReadFloat();
            RecoilMaxX = R.ReadFloat();
            RecoilMaxY = R.ReadFloat();
            RecoverX = R.ReadFloat();
            RecoverY = R.ReadFloat();
            RecoilSprint = R.ReadFloat();
            RecoilCrouch = R.ReadFloat();
            RecoilProne = R.ReadFloat();
            ShakeMinX = R.ReadFloat();
            ShakeMinY = R.ReadFloat();
            ShakeMinZ = R.ReadFloat();
            ShakeMaxX = R.ReadFloat();
            ShakeMaxY = R.ReadFloat();
            ShakeMaxZ = R.ReadFloat();
            BallisticSteps = R.ReadUInt8();
            BallisticTravel = R.ReadFloat();
            BallisticDrop = R.ReadFloat();
            BallisticForce = R.ReadFloat();
            ProjectileLifespan = R.ReadFloat();
            PenetrateBuildables = R.ReadBool();
            ReloadTime = R.ReadFloat();
            HammerTime = R.ReadFloat();
            AlertRadius = R.ReadFloat();
            RangefinderRange = R.ReadFloat();
            HeadshotInstakill = R.ReadBool();
            InfiniteAmmo = R.ReadBool();
            AmmoPerShot = R.ReadUInt8();
            FireDelay = R.ReadInt32();
            CanChangeMagazines = R.ReadBool();
            SprintAiming = R.ReadBool();
            CanJam = R.ReadBool();
            MaxQualityToJam = R.ReadFloat();
            JamMaxChance = R.ReadFloat();
            GunshotRolloffDistance = R.ReadFloat();
            DefaultSight = R.ReadUInt16();
            DefaultTactical = R.ReadUInt16();
            DefaultGrip = R.ReadUInt16();
            DefaultBarrel = R.ReadUInt16();
            DefaultMagazine = R.ReadUInt16();
            UnloadStepTime = R.ReadFloat();
            ReloadStepTime = R.ReadFloat();
            HasSight = R.ReadBool();
            HasTactical = R.ReadBool();
            HasGrip = R.ReadBool();
            HasBarrel = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(AttachmentCalibers);
            W.Write(MagazineCalibers);
            W.Write(Firerate);
            W.Write(ShootAction);
            W.Write(DeleteEmptyMagazines);
            W.Write(Bursts);
            W.Write(FireRates);
            W.Write(IsTurret);
            W.Write(SpreadADS);
            W.Write(SpreadHipfire);
            W.Write(SpreadSprint);
            W.Write(SpreadCrouch);
            W.Write(SpreadProne);
            W.Write(RecoilADS);
            W.Write(UseRecoilADS);
            W.Write(RecoilMinX);
            W.Write(RecoilMinY);
            W.Write(RecoilMaxX);
            W.Write(RecoilMaxY);
            W.Write(RecoverX);
            W.Write(RecoverY);
            W.Write(RecoilSprint);
            W.Write(RecoilCrouch);
            W.Write(RecoilProne);
            W.Write(ShakeMinX);
            W.Write(ShakeMinY);
            W.Write(ShakeMinZ);
            W.Write(ShakeMaxX);
            W.Write(ShakeMaxY);
            W.Write(ShakeMaxZ);
            W.Write(BallisticSteps);
            W.Write(BallisticTravel);
            W.Write(BallisticDrop);
            W.Write(BallisticForce);
            W.Write(ProjectileLifespan);
            W.Write(PenetrateBuildables);
            W.Write(ReloadTime);
            W.Write(HammerTime);
            W.Write(AlertRadius);
            W.Write(RangefinderRange);
            W.Write(HeadshotInstakill);
            W.Write(InfiniteAmmo);
            W.Write(AmmoPerShot);
            W.Write(FireDelay);
            W.Write(CanChangeMagazines);
            W.Write(SprintAiming);
            W.Write(CanJam);
            W.Write(MaxQualityToJam);
            W.Write(JamMaxChance);
            W.Write(GunshotRolloffDistance);
            W.Write(DefaultSight);
            W.Write(DefaultTactical);
            W.Write(DefaultGrip);
            W.Write(DefaultBarrel);
            W.Write(DefaultMagazine);
            W.Write(UnloadStepTime);
            W.Write(ReloadStepTime);
            W.Write(HasSight);
            W.Write(HasTactical);
            W.Write(HasGrip);
            W.Write(HasBarrel);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemMagazineAsset"/>.</summary>
    public class MagazineData : AttachmentData
    {
        public override byte T => 2;
        public byte Pellets;
        public byte DurabilityOnHit;
        public float ProjectileDamageMultiplier;
        public float ProjectileBlastRadiusMultiplier;
        public float ProjectileLaunchForceMultiplier;
        public float ExplosiveRange;
        public float PlayerDamage;
        public EnvironmentDamage EnvironmentDamage;
        public float Speed;
        public bool IsExplosive;
        public bool CanBeEmpty;
        public MagazineData() : base() { }
        public MagazineData(ItemMagazineAsset asset) : base(asset)
        {
            this.Pellets = asset.pellets;
            this.DurabilityOnHit = asset.stuck;
            this.ProjectileDamageMultiplier = asset.projectileDamageMultiplier;
            this.ProjectileBlastRadiusMultiplier = asset.projectileBlastRadiusMultiplier;
            this.ProjectileLaunchForceMultiplier = asset.projectileLaunchForceMultiplier;
            this.ExplosiveRange = asset.range;
            this.PlayerDamage = asset.playerDamage;
            this.EnvironmentDamage = new EnvironmentDamage
            {
                BarricadeDamage = asset.barricadeDamage,
                StructureDamage = asset.structureDamage,
                VehicleDamage = asset.vehicleDamage,
                AnimalDamage = asset.animalDamage,
                ZombieDamage = asset.zombieDamage,
                PlayerDamage = asset.playerDamage,
                ResourceDamage = asset.resourceDamage,
                ObjectDamage = asset.objectDamage,
            };
            this.Speed = asset.speed;
            this.IsExplosive = asset.isExplosive;
            this.CanBeEmpty = asset.deleteEmpty;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Pellets = R.ReadUInt8();
            DurabilityOnHit = R.ReadUInt8();
            ProjectileDamageMultiplier = R.ReadFloat();
            ProjectileBlastRadiusMultiplier = R.ReadFloat();
            ProjectileLaunchForceMultiplier = R.ReadFloat();
            ExplosiveRange = R.ReadFloat();
            PlayerDamage = R.ReadFloat();
            EnvironmentDamage = EnvironmentDamage.Read(R);
            Speed = R.ReadFloat();
            IsExplosive = R.ReadBool();
            CanBeEmpty = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Pellets);
            W.Write(DurabilityOnHit);
            W.Write(ProjectileDamageMultiplier);
            W.Write(ProjectileBlastRadiusMultiplier);
            W.Write(ProjectileLaunchForceMultiplier);
            W.Write(ExplosiveRange);
            W.Write(PlayerDamage);
            EnvironmentDamage.Write(W, EnvironmentDamage);
            W.Write(Speed);
            W.Write(IsExplosive);
            W.Write(CanBeEmpty);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemThrowableAsset"/>.</summary>
    public class ThrowableData : UseableData
    {
        public override byte T => 3;
        public bool Explosive;
        public bool Flashes;
        public bool Sticky;
        public bool ImpactExplosive;
        public float FuseLength;
        public float StrongThrowForce;
        public float WeakThrowForce;
        public float BoostThrowForceMultiplier;
        public ThrowableData() : base() { }
        public ThrowableData(ItemThrowableAsset asset) : base(asset)
        {
            this.Explosive = asset.isExplosive;
            this.Flashes = asset.isFlash;
            this.Sticky = asset.isSticky;
            this.ImpactExplosive = asset.explodeOnImpact;
            this.FuseLength = asset.fuseLength;
            this.StrongThrowForce = asset.strongThrowForce;
            this.WeakThrowForce = asset.weakThrowForce;
            this.BoostThrowForceMultiplier = asset.boostForceMultiplier;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Explosive = R.ReadBool();
            Flashes = R.ReadBool();
            Sticky = R.ReadBool();
            ImpactExplosive = R.ReadBool();
            FuseLength = R.ReadFloat();
            StrongThrowForce = R.ReadFloat();
            WeakThrowForce = R.ReadFloat();
            BoostThrowForceMultiplier = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Explosive);
            W.Write(Flashes);
            W.Write(Sticky);
            W.Write(ImpactExplosive);
            W.Write(FuseLength);
            W.Write(StrongThrowForce);
            W.Write(WeakThrowForce);
            W.Write(BoostThrowForceMultiplier);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemClothingAsset"/>.</summary>
    public class ClothingData : ItemData
    {
        public override byte T => 4;
        public float Armor;
        public float ExplosionArmor;
        public bool Waterproof;
        public bool Fireproof;
        public bool Radiationproof;
        public bool ShowHair;
        public bool ShowBeard;
        public ClothingData() : base() { }
        public ClothingData(ItemClothingAsset asset) : base(asset)
        {
            this.Armor = asset.armor;
            this.ExplosionArmor = asset.explosionArmor;
            this.Waterproof = asset.proofWater;
            this.Fireproof = asset.proofFire;
            this.Radiationproof = asset.proofRadiation;
            this.ShowHair = asset.hairVisible;
            this.ShowBeard = asset.beardVisible;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Armor = R.ReadFloat();
            ExplosionArmor = R.ReadFloat();
            Waterproof = R.ReadBool();
            Fireproof = R.ReadBool();
            Radiationproof = R.ReadBool();
            ShowHair = R.ReadBool();
            ShowBeard = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Armor);
            W.Write(ExplosionArmor);
            W.Write(Waterproof);
            W.Write(Fireproof);
            W.Write(Radiationproof);
            W.Write(ShowHair);
            W.Write(ShowBeard);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemBagAsset"/>.</summary>
    public class StorageClothingData : ClothingData
    {
        public override byte T => 5;
        public byte Width;
        public byte Height;
        public StorageClothingData() : base() { }
        public StorageClothingData(ItemBagAsset asset) : base(asset)
        {
            this.Width = asset.width;
            this.Height = asset.height;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Width = R.ReadUInt8();
            Height = R.ReadUInt8();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Width);
            W.Write(Height);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemBarricadeAsset"/>.</summary>
    public class BarricadeData : ItemData
    {
        public override byte T => 6;
        public ushort Health;
        public EArmorTier Tier;
        public BarricadeData() : base() { }
        public BarricadeData(ItemBarricadeAsset asset) : base(asset)
        {
            this.Health = asset.health;
            this.Tier = asset.armorTier;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Health = R.ReadUInt16();
            Tier = R.ReadEnum<EArmorTier>();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Health);
            W.Write(Tier);
        }

    }
    /// <summary>Networking abstraction for <see cref="ItemStructureAsset"/>.</summary>
    public class StructureData : ItemData
    {
        public override byte T => 7;
        public ushort Health;
        public EArmorTier Tier;
        public StructureData() : base() { }
        public StructureData(ItemStructureAsset asset) : base(asset)
        {
            this.Health = asset.health;
            this.Tier = asset.armorTier;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Health = R.ReadUInt16();
            Tier = R.ReadEnum<EArmorTier>();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Health);
            W.Write(Tier);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemTrapAsset"/>.</summary>
    public class TrapData : BarricadeData
    {
        public override byte T => 8;
        public float ExplosionRange;
        public float PlayerDamage;
        public EnvironmentDamage Damage;
        public float SetupDelay;
        public float Cooldown;
        public bool Broken;
        public bool Explosive;
        public bool DamagesTires;
        public TrapData() : base() { }
        public TrapData(ItemTrapAsset asset) : base(asset)
        {
            this.ExplosionRange = asset.range2;
            this.PlayerDamage = asset.playerDamage;
            this.Damage = new EnvironmentDamage
            {
                BarricadeDamage = asset.barricadeDamage,
                StructureDamage = asset.structureDamage,
                VehicleDamage = asset.vehicleDamage,
                AnimalDamage = asset.animalDamage,
                ZombieDamage = asset.zombieDamage,
                PlayerDamage = asset.playerDamage,
                ResourceDamage = asset.resourceDamage,
                ObjectDamage = asset.objectDamage,
            };
            this.SetupDelay = asset.trapSetupDelay;
            this.Cooldown = asset.trapCooldown;
            this.Broken = asset.isBroken;
            this.Explosive = asset.isExplosive;
            this.DamagesTires = asset.damageTires;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            ExplosionRange = R.ReadFloat();
            PlayerDamage = R.ReadFloat();
            Damage = EnvironmentDamage.Read(R);
            SetupDelay = R.ReadFloat();
            Cooldown = R.ReadFloat();
            Broken = R.ReadBool();
            Explosive = R.ReadBool();
            DamagesTires = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(ExplosionRange);
            W.Write(PlayerDamage);
            EnvironmentDamage.Write(W, Damage);
            W.Write(SetupDelay);
            W.Write(Cooldown);
            W.Write(Broken);
            W.Write(Explosive);
            W.Write(DamagesTires);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemWeaponAsset"/>.</summary>
    public class UseableData : ItemData
    {
        public override byte T => 15;
        public byte[] BladeIDs;
        public float Range;
        public GunDamage PlayerDamage;
        public DamagePlayerParameters.Bleeding PlayerDamageBleeding;
        public DamagePlayerParameters.Bones PlayerDamageBones;
        public float PlayerDamageFood;
        public float PlayerDamageWater;
        public float PlayerDamageVirus;
        public float PlayerDamageHallucination;
        public GunDamage ZombieDamage;
        public GunDamage AnimalDamage;
        public EnvironmentDamage Damage;
        public float Durability;
        public byte DurabilityWear;
        public bool Invulnerable;
        public EZombieStunOverride StunZombies;
        public bool BypassDamageBlocking;
        public UseableData() : base() { }
        public UseableData(ItemWeaponAsset asset) : base(asset)
        {
            this.BladeIDs = asset.bladeIDs;
            this.Range = asset.range;
            this.PlayerDamage = new GunDamage
            {
                Overall = asset.playerDamageMultiplier.damage,
                Arm = asset.playerDamageMultiplier.arm,
                Leg = asset.playerDamageMultiplier.leg,
                Spine = asset.playerDamageMultiplier.spine,
                Skull = asset.playerDamageMultiplier.skull
            };
            this.PlayerDamageBleeding = asset.playerDamageBleeding;
            this.PlayerDamageBones = asset.playerDamageBones;
            this.PlayerDamageFood = asset.playerDamageFood;
            this.PlayerDamageWater = asset.playerDamageWater;
            this.PlayerDamageVirus = asset.playerDamageVirus;
            this.PlayerDamageHallucination = asset.playerDamageHallucination;
            this.ZombieDamage = new GunDamage
            {
                Overall = asset.zombieDamageMultiplier.damage,
                Arm = asset.zombieDamageMultiplier.arm,
                Leg = asset.zombieDamageMultiplier.leg,
                Spine = asset.zombieDamageMultiplier.spine,
                Skull = asset.zombieDamageMultiplier.skull
            };
            this.AnimalDamage = new GunDamage
            {
                Overall = asset.animalDamageMultiplier.damage,
                Arm = asset.animalDamageMultiplier.leg,
                Leg = asset.animalDamageMultiplier.leg,
                Spine = asset.animalDamageMultiplier.spine,
                Skull = asset.animalDamageMultiplier.skull
            };
            this.Damage = new EnvironmentDamage
            {
                BarricadeDamage = asset.barricadeDamage,
                StructureDamage = asset.structureDamage,
                VehicleDamage = asset.vehicleDamage,
                AnimalDamage = asset.animalDamageMultiplier.damage,
                ZombieDamage = asset.zombieDamageMultiplier.damage,
                PlayerDamage = asset.playerDamageMultiplier.damage,
                ResourceDamage = asset.resourceDamage,
                ObjectDamage = asset.objectDamage,
            };
            this.Durability = asset.durability;
            this.DurabilityWear = asset.wear;
            this.Invulnerable = asset.isInvulnerable;
            this.StunZombies = asset.zombieStunOverride;
            this.BypassDamageBlocking = asset.bypassAllowedToDamagePlayer;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            BladeIDs = R.ReadBytes();
            PlayerDamage = GunDamage.Read(R);
            PlayerDamageBleeding = R.ReadEnum<DamagePlayerParameters.Bleeding>();
            PlayerDamageBones = R.ReadEnum<DamagePlayerParameters.Bones>();
            PlayerDamageFood = R.ReadFloat();
            PlayerDamageWater = R.ReadFloat();
            PlayerDamageVirus = R.ReadFloat();
            PlayerDamageHallucination = R.ReadFloat();
            ZombieDamage = GunDamage.Read(R);
            AnimalDamage = GunDamage.Read(R);
            Damage = EnvironmentDamage.Read(R);
            Durability = R.ReadFloat();
            DurabilityWear = R.ReadUInt8();
            Invulnerable = R.ReadBool();
            StunZombies = R.ReadEnum<EZombieStunOverride>();
            BypassDamageBlocking = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(BladeIDs);
            GunDamage.Write(W, PlayerDamage);
            W.Write(PlayerDamageBleeding);
            W.Write(PlayerDamageBones);
            W.Write(PlayerDamageFood);
            W.Write(PlayerDamageWater);
            W.Write(PlayerDamageVirus);
            W.Write(PlayerDamageHallucination);
            GunDamage.Write(W, ZombieDamage);
            GunDamage.Write(W, AnimalDamage);
            EnvironmentDamage.Write(W, Damage);
            W.Write(Durability);
            W.Write(DurabilityWear);
            W.Write(Invulnerable);
            W.Write(StunZombies);
            W.Write(BypassDamageBlocking);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemCaliberAsset"/>.</summary>
    public class AttachmentData : ItemData
    {
        public override byte T => 9;
        public ushort[] Calibers;
        public float RecoilX;
        public float RecoilY;
        public float Spread;
        public float Shake;
        public float Damage;
        public byte Firerate;
        public float BallisticDamageMultiplier;
        public AttachmentData() : base() { }
        public AttachmentData(ItemCaliberAsset asset) : base(asset)
        {
            this.Calibers = asset.calibers;
            this.RecoilX = asset.recoil_x;
            this.RecoilY = asset.recoil_y;
            this.Spread = asset.spread;
            this.Shake = asset.shake;
            this.Damage = asset.damage;
            this.Firerate = asset.firerate;
            this.BallisticDamageMultiplier = asset.ballisticDamageMultiplier;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Calibers = R.ReadUInt16Array();
            RecoilX = R.ReadFloat();
            RecoilY = R.ReadFloat();
            Spread = R.ReadFloat();
            Shake = R.ReadFloat();
            Damage = R.ReadFloat();
            Firerate = R.ReadUInt8();
            BallisticDamageMultiplier = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Calibers);
            W.Write(RecoilX);
            W.Write(RecoilY);
            W.Write(Spread);
            W.Write(Shake);
            W.Write(Damage);
            W.Write(Firerate);
            W.Write(BallisticDamageMultiplier);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemSightAsset"/>.</summary>
    public class SightData : AttachmentData
    {
        public override byte T => 10;
        public ELightingVision Vision;
        public float Zoom;
        public bool Holographic;
        public SightData() : base() { }
        public SightData(ItemSightAsset asset) : base(asset)
        {
            this.Vision = asset.vision;
            this.Zoom = asset.zoom;
            this.Holographic = asset.isHolographic;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Vision = R.ReadEnum<ELightingVision>();
            Zoom = R.ReadFloat();
            Holographic = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Vision);
            W.Write(Zoom);
            W.Write(Holographic);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemGripAsset"/>.</summary>
    public class GripData : AttachmentData
    {
        public override byte T => 11;
        public bool Bipod;
        public GripData() : base() { }
        public GripData(ItemGripAsset asset) : base(asset)
        {
            this.Bipod = asset.isBipod;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Bipod = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Bipod);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemTacticalAsset"/>.</summary>
    public class TacticalData : AttachmentData
    {
        public override byte T => 12;
        public bool Laser;
        public bool Light;
        public bool Rangefinder;
        public bool Melee;
        public float SpotlightRange;
        public float SpotlightAngle;
        public float SpotlightIntensity;
        public float SpotlightColorR;
        public float SpotlightColorG;
        public float SpotlightColorB;
        public TacticalData() : base() { }
        public TacticalData(ItemTacticalAsset asset) : base(asset)
        {
            this.Laser = asset.isLaser;
            this.Light = asset.isLight;
            this.Rangefinder = asset.isRangefinder;
            this.Melee = asset.isMelee;
            this.SpotlightRange = asset.lightConfig.range;
            this.SpotlightAngle = asset.lightConfig.angle;
            this.SpotlightIntensity = asset.lightConfig.intensity;
            this.SpotlightColorR = asset.lightConfig.color.r;
            this.SpotlightColorG = asset.lightConfig.color.g;
            this.SpotlightColorB = asset.lightConfig.color.b;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Laser = R.ReadBool();
            Light = R.ReadBool();
            Rangefinder = R.ReadBool();
            Melee = R.ReadBool();
            SpotlightRange = R.ReadFloat();
            SpotlightAngle = R.ReadFloat();
            SpotlightIntensity = R.ReadFloat();
            SpotlightColorR = R.ReadFloat();
            SpotlightColorG = R.ReadFloat();
            SpotlightColorB = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Laser);
            W.Write(Light);
            W.Write(Rangefinder);
            W.Write(Melee);
            W.Write(SpotlightRange);
            W.Write(SpotlightAngle);
            W.Write(SpotlightIntensity);
            W.Write(SpotlightColorR);
            W.Write(SpotlightColorG);
            W.Write(SpotlightColorB);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemBarrelAsset"/>.</summary>
    public class BarrelData : AttachmentData
    {
        public override byte T => 13;
        public bool Braked;
        public bool Silenced;
        public float Volume;
        public byte UsageDurability;
        public float BallisticDrop;
        public float GunshotRolloffDistanceMultiplier;
        public BarrelData() : base() { }
        public BarrelData(ItemBarrelAsset asset) : base(asset)
        {
            this.Braked = asset.isBraked;
            this.Silenced = asset.isSilenced;
            this.Volume = asset.volume;
            this.UsageDurability = asset.durability;
            this.BallisticDrop = asset.ballisticDrop;
            this.GunshotRolloffDistanceMultiplier = asset.gunshotRolloffDistanceMultiplier;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Braked = R.ReadBool();
            Silenced = R.ReadBool();
            Volume = R.ReadFloat();
            UsageDurability = R.ReadUInt8();
            BallisticDrop = R.ReadFloat();
            GunshotRolloffDistanceMultiplier = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Braked);
            W.Write(Silenced);
            W.Write(Volume);
            W.Write(UsageDurability);
            W.Write(BallisticDrop);
            W.Write(GunshotRolloffDistanceMultiplier);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemConsumeableAsset"/>.</summary>
    public class ConsumableData : UseableData
    {
        public override byte T => 14;
        public byte Health;
        public byte Food;
        public byte Water;
        public byte Virus;
        public byte Disinfectant;
        public byte Energy;
        public byte Vision;
        public sbyte Oxygen;
        public uint Warmth;
        public int Experience;
        public ItemConsumeableAsset.Bleeding BleedingModifier;
        public ItemConsumeableAsset.Bones BrokenModifier;
        public bool CanHeal;
        public bool CanGainWaterWhenFull;
        public bool DeleteAfterUse;
        public bool ExplodesOnUse;
        public ConsumableData() : base() { }
        public ConsumableData(ItemConsumeableAsset asset) : base(asset)
        {
            this.Health = asset.health;
            this.Food = asset.food;
            this.Water = asset.water;
            this.Virus = asset.virus;
            this.Disinfectant = asset.disinfectant;
            this.Energy = asset.energy;
            this.Vision = asset.vision;
            this.Oxygen = asset.oxygen;
            this.Warmth = asset.warmth;
            this.Experience = asset.experience;
            this.BleedingModifier = asset.bleedingModifier;
            this.BrokenModifier = asset.bonesModifier;
            this.CanHeal = asset.hasAid;
            this.CanGainWaterWhenFull = asset.foodConstrainsWater;
            this.DeleteAfterUse = asset.shouldDeleteAfterUse;
            this.ExplodesOnUse = asset.explosion != 0;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Health = R.ReadUInt8();
            Food = R.ReadUInt8();
            Water = R.ReadUInt8();
            Virus = R.ReadUInt8();
            Disinfectant = R.ReadUInt8();
            Energy = R.ReadUInt8();
            Vision = R.ReadUInt8();
            Oxygen = R.ReadInt8();
            Warmth = R.ReadUInt32();
            Experience = R.ReadInt32();
            BleedingModifier = R.ReadEnum<ItemConsumeableAsset.Bleeding>();
            BrokenModifier = R.ReadEnum<ItemConsumeableAsset.Bones>();
            CanHeal = R.ReadBool();
            CanGainWaterWhenFull = R.ReadBool();
            DeleteAfterUse = R.ReadBool();
            ExplodesOnUse = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Health);
            W.Write(Food);
            W.Write(Water);
            W.Write(Virus);
            W.Write(Disinfectant);
            W.Write(Energy);
            W.Write(Vision);
            W.Write(Oxygen);
            W.Write(Warmth);
            W.Write(Experience);
            W.Write(BleedingModifier);
            W.Write(BrokenModifier);
            W.Write(CanHeal);
            W.Write(CanGainWaterWhenFull);
            W.Write(DeleteAfterUse);
            W.Write(ExplodesOnUse);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemFuelAsset"/>.</summary>
    public class FuelData : ItemData
    {
        public override byte T => 16;
        public ushort Fuel;
        public FuelData() : base() { }
        public FuelData(ItemFuelAsset asset) : base(asset)
        {
            this.Fuel = asset.fuel;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Fuel = R.ReadUInt16();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Fuel);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemOpticAsset"/>.</summary>
    public class ZoomData : ItemData
    {
        public override byte T => 17;
        public float Zoom;
        public ZoomData() : base() { }
        public ZoomData(ItemOpticAsset asset) : base(asset)
        {
            this.Zoom = asset.zoom;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Zoom = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Zoom);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemChargeAsset"/>.</summary>
    public class ChargeData : BarricadeData
    {
        public override byte T => 18;
        public float ExplosionRange;
        public EnvironmentDamage Damage;
        public ChargeData() : base() { }
        public ChargeData(ItemChargeAsset asset) : base(asset)
        {
            this.ExplosionRange = asset.range2;
            this.Damage = new EnvironmentDamage
            {
                BarricadeDamage = asset.barricadeDamage,
                StructureDamage = asset.structureDamage,
                VehicleDamage = asset.vehicleDamage,
                AnimalDamage = asset.animalDamage,
                ZombieDamage = asset.zombieDamage,
                PlayerDamage = asset.playerDamage,
                ResourceDamage = asset.resourceDamage,
                ObjectDamage = asset.objectDamage
            };
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            ExplosionRange = R.ReadFloat();
            Damage = EnvironmentDamage.Read(R);
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(ExplosionRange);
            EnvironmentDamage.Write(W, Damage);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemRefillAsset"/>.</summary>
    public class RefillData : ItemData
    {
        public override byte T => 19;
        public QualityStats Health;
        public QualityStats Food;
        public QualityStats Water;
        public QualityStats Virus;
        public QualityStats Stamina;
        public QualityStats Oxygen;
        public RefillData() : base() { }
        public RefillData(ItemRefillAsset asset) : base(asset)
        {
            this.Health = new QualityStats(asset.cleanHealth, asset.saltyHealth, asset.dirtyHealth);
            this.Food = new QualityStats(asset.cleanFood, asset.saltyFood, asset.dirtyFood);
            this.Water = new QualityStats(asset.cleanWater, asset.saltyWater, asset.dirtyWater);
            this.Virus = new QualityStats(asset.cleanVirus, asset.saltyVirus, asset.dirtyVirus);
            this.Stamina = new QualityStats(asset.cleanStamina, asset.saltyStamina, asset.dirtyStamina);
            this.Oxygen = new QualityStats(asset.cleanOxygen, asset.saltyOxygen, asset.dirtyOxygen);
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Health = QualityStats.Read(R);
            Food = QualityStats.Read(R);
            Water = QualityStats.Read(R);
            Virus = QualityStats.Read(R);
            Stamina = QualityStats.Read(R);
            Oxygen = QualityStats.Read(R);
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            QualityStats.Write(W, Health);
            QualityStats.Write(W, Food);
            QualityStats.Write(W, Water);
            QualityStats.Write(W, Virus);
            QualityStats.Write(W, Stamina);
            QualityStats.Write(W, Oxygen);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemMapAsset"/>.</summary>
    public class MapData : ItemData
    {
        public override byte T => 20;
        public bool Compass;
        public bool Chart;
        public bool Satellite;
        public MapData() : base() { }
        public MapData(ItemMapAsset asset) : base(asset)
        {
            this.Compass = asset.enablesCompass;
            this.Chart = asset.enablesChart;
            this.Satellite = asset.enablesMap;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Compass = R.ReadBool();
            Chart = R.ReadBool();
            Satellite = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Compass);
            W.Write(Chart);
            W.Write(Satellite);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemArrestStartAsset"/>.</summary>
    public class HandcuffData : ItemData
    {
        public override byte T => 21;
        public ushort Strength;
        public HandcuffData() : base() { }
        public HandcuffData(ItemArrestStartAsset asset) : base(asset)
        {
            this.Strength = asset.strength;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Strength = R.ReadUInt16();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Strength);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemBeaconAsset"/>.</summary>
    public class BeaconData : BarricadeData
    {
        public override byte T => 22;
        public ushort Wave;
        public byte Rewards;
        public BeaconData() : base() { }
        public BeaconData(ItemBeaconAsset asset) : base(asset)
        {
            this.Wave = asset.wave;
            this.Rewards = asset.rewards;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Wave = R.ReadUInt16();
            Rewards = R.ReadUInt8();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Wave);
            W.Write(Rewards);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemFarmAsset"/>.</summary>
    public class SeedData : BarricadeData
    {
        public override byte T => 23;
        public uint Growth;
        public ushort Product;
        public bool IgnoreSoilRestrictions;
        public bool CanFertilize;
        public uint HarvestRewardXP;
        public SeedData() : base() { }
        public SeedData(ItemFarmAsset asset) : base(asset)
        {
            this.Growth = asset.growth;
            this.Product = asset.grow;
            this.IgnoreSoilRestrictions = asset.ignoreSoilRestrictions;
            this.CanFertilize = asset.canFertilize;
            this.HarvestRewardXP = asset.harvestRewardExperience;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Growth = R.ReadUInt32();
            Product = R.ReadUInt16();
            IgnoreSoilRestrictions = R.ReadBool();
            CanFertilize = R.ReadBool();
            HarvestRewardXP = R.ReadUInt32();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Growth);
            W.Write(Product);
            W.Write(IgnoreSoilRestrictions);
            W.Write(CanFertilize);
            W.Write(HarvestRewardXP);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemGeneratorAsset"/>.</summary>
    public class GeneratorData : BarricadeData
    {
        public override byte T => 24;
        public ushort Capacity;
        public float PowerRange;
        public float BurnSpeed;
        public GeneratorData() : base() { }
        public GeneratorData(ItemGeneratorAsset asset) : base(asset)
        {
            this.Capacity = asset.capacity;
            this.PowerRange = asset.wirerange;
            this.BurnSpeed = asset.burn;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Capacity = R.ReadUInt16();
            PowerRange = R.ReadFloat();
            BurnSpeed = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Capacity);
            W.Write(PowerRange);
            W.Write(BurnSpeed);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemLibraryAsset"/>.</summary>
    public class LibraryData : BarricadeData
    {
        public override byte T => 25;
        public uint Capacity;
        public byte Tax;
        public LibraryData() : base() { }
        public LibraryData(ItemLibraryAsset asset) : base(asset)
        {
            this.Capacity = asset.capacity;
            this.Tax = asset.tax;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Capacity = R.ReadUInt32();
            Tax = R.ReadUInt8();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Capacity);
            W.Write(Tax);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemOilPumpAsset"/>.</summary>
    public class OilPumpData : BarricadeData
    {
        public override byte T => 26;
        public ushort Capacity;
        public OilPumpData() : base() { }
        public OilPumpData(ItemOilPumpAsset asset) : base(asset)
        {
            this.Capacity = asset.fuelCapacity;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Capacity = R.ReadUInt16();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Capacity);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemStorageAsset"/>.</summary>
    public class StorageData : BarricadeData
    {
        public override byte T => 26;
        public byte StorageSizeX;
        public byte StorageSizeY;
        public bool Display;
        public bool CloseOutsideRange;
        public StorageData() : base() { }
        public StorageData(ItemStorageAsset asset) : base(asset)
        {
            this.StorageSizeX = asset.storage_x;
            this.StorageSizeY = asset.storage_y;
            this.Display = asset.isDisplay;
            this.CloseOutsideRange = asset.shouldCloseWhenOutsideRange;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            StorageSizeX = R.ReadUInt8();
            StorageSizeY = R.ReadUInt8();
            Display = R.ReadBool();
            CloseOutsideRange = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(StorageSizeX);
            W.Write(StorageSizeY);
            W.Write(Display);
            W.Write(CloseOutsideRange);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemTankAsset"/>.</summary>
    public class TankData : BarricadeData
    {
        public override byte T => 27;
        public ETankSource Source;
        public ushort Capacity;
        public TankData() : base() { }
        public TankData(ItemTankAsset asset) : base(asset)
        {
            this.Source = asset.source;
            this.Capacity = asset.resource;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Source = R.ReadEnum<ETankSource>();
            Capacity = R.ReadUInt16();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Source);
            W.Write(Capacity);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemBoxAsset"/>.</summary>
    public class ItemBoxData : ItemData
    {
        public override byte T => 28;
        public int GenerateID;
        public int DestoryID;
        public int[] Drops;
        public EBoxItemOrigin ItemOrigin;
        public EBoxProbabilityModel ProbabilityModel;
        public bool ContainsBonusItems;
        public ItemBoxData() : base() { }
        public ItemBoxData(ItemBoxAsset asset) : base(asset)
        {
            this.GenerateID = asset.generate;
            this.DestoryID = asset.destroy;
            this.Drops = asset.drops;
            this.ItemOrigin = asset.itemOrigin;
            this.ProbabilityModel = asset.probabilityModel;
            this.ContainsBonusItems = asset.containsBonusItems;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            GenerateID = R.ReadInt32();
            DestoryID = R.ReadInt32();
            Drops = R.ReadInt32Array();
            ItemOrigin = R.ReadEnum<EBoxItemOrigin>();
            ProbabilityModel = R.ReadEnum<EBoxProbabilityModel>();
            ContainsBonusItems = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(GenerateID);
            W.Write(DestoryID);
            W.Write(Drops);
            W.Write(ItemOrigin);
            W.Write(ProbabilityModel);
            W.Write(ContainsBonusItems);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemGearAsset"/>.</summary>
    public class GearData : ClothingData
    {
        public override byte T => 29;
        public string HairOverride;
        public GearData() : base() { }
        public GearData(ItemGearAsset asset) : base(asset)
        {
            this.HairOverride = asset.hairOverride;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            HairOverride = R.ReadString();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(HairOverride ?? string.Empty);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemCloudAsset"/>.</summary>
    public class CloudData : ItemData
    {
        public override byte T => 30;
        public float Gravity;
        public CloudData() : base() { }
        public CloudData(ItemCloudAsset asset) : base(asset)
        {
            this.Gravity = asset.gravity;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Gravity = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Gravity);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemDetonatorAsset"/>.</summary>
    public class DetonatorData : ItemData
    {
        public override byte T => 31;
        public DetonatorData() : base() { }
        public DetonatorData(ItemDetonatorAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemFilterAsset"/>.</summary>
    public class FilterData : ItemData
    {
        public override byte T => 32;
        public FilterData() : base() { }
        public FilterData(ItemFilterAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemFisherAsset"/>.</summary>
    public class FishingRodData : ItemData
    {
        public override byte T => 33;
        public ushort Reward;
        public FishingRodData() : base() { }
        public FishingRodData(ItemFisherAsset asset) : base(asset)
        {
            this.Reward = asset.rewardID;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Reward = R.ReadUInt16();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Reward);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemGrowerAsset"/>.</summary>
    public class FertilizerData : ItemData
    {
        public override byte T => 34;
        public FertilizerData() : base() { }
        public FertilizerData(ItemGrowerAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemKeyAsset"/>.</summary>
    public class KeyData : ItemData
    {
        public override byte T => 35;
        public bool ExchangeWithTargetItem;
        public KeyData() : base() { }
        public KeyData(ItemKeyAsset asset) : base(asset)
        {
            this.ExchangeWithTargetItem = asset.exchangeWithTargetItem;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            ExchangeWithTargetItem = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(ExchangeWithTargetItem);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemSupplyAsset"/>.</summary>
    public class SupplyData : ItemData
    {
        public override byte T => 36;
        public SupplyData() : base() { }
        public SupplyData(ItemSupplyAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemToolAsset"/>.</summary>
    public class ToolData : ItemData
    {
        public override byte T => 37;
        public ToolData() : base() { }
        public ToolData(ItemToolAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemVehicleRepairToolAsset"/>.</summary>
    public class VehicleRepairToolData : ToolData
    {
        public override byte T => 38;
        public VehicleRepairToolData() : base() { }
        public VehicleRepairToolData(ItemVehicleRepairToolAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemTireAsset"/>.</summary>
    public class TireToolData : ToolData
    {
        public override byte T => 39;
        public EUseableTireMode TireMode;
        public TireToolData() : base() { }
        public TireToolData(ItemTireAsset asset) : base(asset)
        {
            this.TireMode = asset.mode;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            TireMode = R.ReadEnum<EUseableTireMode>();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(TireMode);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemMeleeAsset"/>.</summary>
    public class MeleeData : UseableData
    {
        public override byte T => 40;
        public float StrongHitDamageMultiplier;
        public float StrongHitSpeedMultiplier;
        public float WeakHitSpeedMultiplier;
        public byte Stamina;
        public bool Repairs;
        public bool Repeats;
        public bool Light;
        public float SpotlightRange;
        public float SpotlightAngle;
        public float SpotlightIntensity;
        public float SpotlightColorR;
        public float SpotlightColorG;
        public float SpotlightColorB;
        public float AlertRadius;
        public MeleeData() : base() { }
        public MeleeData(ItemMeleeAsset asset) : base(asset)
        {
            this.StrongHitDamageMultiplier = asset.strength;
            this.StrongHitSpeedMultiplier = asset.strong;
            this.WeakHitSpeedMultiplier = asset.weak;
            this.Stamina = asset.stamina;
            this.Repairs = asset.isRepair;
            this.Repeats = asset.isRepeated;
            this.Light = asset.isLight;
            this.SpotlightRange = asset.lightConfig.range;
            this.SpotlightAngle = asset.lightConfig.angle;
            this.SpotlightIntensity = asset.lightConfig.intensity;
            this.SpotlightColorR = asset.lightConfig.color.r;
            this.SpotlightColorG = asset.lightConfig.color.g;
            this.SpotlightColorB = asset.lightConfig.color.b;
            this.AlertRadius = asset.alertRadius;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            StrongHitDamageMultiplier = R.ReadFloat();
            StrongHitSpeedMultiplier = R.ReadFloat();
            WeakHitSpeedMultiplier = R.ReadFloat();
            Stamina = R.ReadUInt8();
            Repairs = R.ReadBool();
            Repeats = R.ReadBool();
            Light = R.ReadBool();
            SpotlightRange = R.ReadFloat();
            SpotlightAngle = R.ReadFloat();
            SpotlightIntensity = R.ReadFloat();
            SpotlightColorR = R.ReadFloat();
            SpotlightColorG = R.ReadFloat();
            SpotlightColorB = R.ReadFloat();
            AlertRadius = R.ReadFloat();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(StrongHitDamageMultiplier);
            W.Write(StrongHitSpeedMultiplier);
            W.Write(WeakHitSpeedMultiplier);
            W.Write(Stamina);
            W.Write(Repairs);
            W.Write(Repeats);
            W.Write(Light);
            W.Write(SpotlightRange);
            W.Write(SpotlightAngle);
            W.Write(SpotlightIntensity);
            W.Write(SpotlightColorR);
            W.Write(SpotlightColorG);
            W.Write(SpotlightColorB);
            W.Write(AlertRadius);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemFoodAsset"/>.</summary>
    public class FoodData : ConsumableData
    {
        public override byte T => 41;
        public FoodData() : base() { }
        public FoodData(ItemFoodAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemMedicalAsset"/>.</summary>
    public class HealingItemData : ConsumableData
    {
        public override byte T => 42;
        public HealingItemData() : base() { }
        public HealingItemData(ItemMedicalAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemWaterAsset"/>.</summary>
    public class WaterData : ConsumableData
    {
        public override byte T => 43;
        public WaterData() : base() { }
        public WaterData(ItemWaterAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemArrestEndAsset"/>.</summary>
    public class HandcuffKeyData : ItemData
    {
        public override byte T => 44;
        public ushort ReturnedItem;
        public HandcuffKeyData() : base() { }
        public HandcuffKeyData(ItemArrestEndAsset asset) : base(asset)
        {
            this.ReturnedItem = asset.recover;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            ReturnedItem = R.ReadUInt16();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(ReturnedItem);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemBackpackAsset"/>.</summary>
    public class BackpackData : StorageClothingData
    {
        public override byte T => 45;
        public BackpackData() : base() { }
        public BackpackData(ItemBackpackAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemPantsAsset"/>.</summary>
    public class PantsData : StorageClothingData
    {
        public override byte T => 46;
        public PantsData() : base() { }
        public PantsData(ItemPantsAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemShirtAsset"/>.</summary>
    public class ShirtData : StorageClothingData
    {
        public override byte T => 47;
        public bool IgnoreHand;
        public ShirtData() : base() { }
        public ShirtData(ItemShirtAsset asset) : base(asset)
        {
            this.IgnoreHand = asset.ignoreHand;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            IgnoreHand = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(IgnoreHand);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemVestAsset"/>.</summary>
    public class VestData : StorageClothingData
    {
        public override byte T => 48;
        public VestData() : base() { }
        public VestData(ItemVestAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemGlassesAsset"/>.</summary>
    public class GlassesData : GearData
    {
        public override byte T => 49;
        public ELightingVision VisionType;
        public bool Blindfold;
        public GlassesData() : base() { }
        public GlassesData(ItemGlassesAsset asset) : base(asset) 
        {
            this.VisionType = asset.vision;
            this.Blindfold = asset.isBlindfold;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            VisionType = R.ReadEnum<ELightingVision>();
            Blindfold = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(VisionType);
            W.Write(Blindfold);
        }
    }
    /// <summary>Networking abstraction for <see cref="ItemHatAsset"/>.</summary>
    public class HatData : GearData
    {
        public override byte T => 50;
        public HatData() : base() { }
        public HatData(ItemHatAsset asset) : base(asset) { }
        public override void ReadInst(ByteReader R) => base.ReadInst(R);
        public override void WriteInst(ByteWriter W) => base.WriteInst(W);
    }
    /// <summary>Networking abstraction for <see cref="ItemMaskAsset"/>.</summary>
    public class MaskData : GearData
    {
        public override byte T => 51;
        public bool Earpiece;
        public MaskData() : base() { }
        public MaskData(ItemMaskAsset asset) : base(asset)
        {
            this.Earpiece = asset.isEarpiece;
        }
        public override void ReadInst(ByteReader R)
        {
            base.ReadInst(R);
            Earpiece = R.ReadBool();
        }
        public override void WriteInst(ByteWriter W)
        {
            base.WriteInst(W);
            W.Write(Earpiece);
        }
    }

    [Flags]
    public enum EFireRate : byte
    {
        NONE = 0,
        SAFETY = 1,
        SEMI = 2,
        BURST = 4,
        AUTO = 8
    }
    public struct GunDamage
    {
        public const int SIZE = sizeof(float) * 5;
        public float Overall;
        public float Leg;
        public float Arm;
        public float Spine;
        public float Skull;
        public static GunDamage Read(ByteReader R) => new GunDamage()
        {
            Overall = R.ReadFloat(),
            Leg = R.ReadFloat(),
            Arm = R.ReadFloat(),
            Spine = R.ReadFloat(),
            Skull = R.ReadFloat()
        };
        public static void Write(ByteWriter W, GunDamage D)
        {
            W.Write(D.Overall);
            W.Write(D.Leg);
            W.Write(D.Arm);
            W.Write(D.Spine);
            W.Write(D.Skull);
        }
    }
    public struct QualityStats
    {
        public const int SIZE = sizeof(float) * 3;
        public float Clean;
        public float Salty;
        public float Dirty;
        public QualityStats(float Clean, float Salty, float Dirty)
        {
            this.Clean = Clean;
            this.Salty = Salty;
            this.Dirty = Dirty;
        }
        public static QualityStats Read(ByteReader R) => new QualityStats()
        {
            Clean = R.ReadFloat(),
            Salty = R.ReadFloat(),
            Dirty = R.ReadFloat(),
        };
        public static void Write(ByteWriter W, QualityStats D)
        {
            W.Write(D.Clean);
            W.Write(D.Salty);
            W.Write(D.Dirty);
        }
    }
    public struct EnvironmentDamage
    {
        public const int SIZE = sizeof(float) * 8;
        public float BarricadeDamage;
        public float StructureDamage;
        public float VehicleDamage;
        public float AnimalDamage;
        public float ZombieDamage;
        public float PlayerDamage;
        public float ResourceDamage;
        public float ObjectDamage;
        public static EnvironmentDamage Read(ByteReader R) => new EnvironmentDamage()
        {
            BarricadeDamage = R.ReadFloat(),
            StructureDamage = R.ReadFloat(),
            VehicleDamage = R.ReadFloat(),
            AnimalDamage = R.ReadFloat(),
            ZombieDamage = R.ReadFloat(),
            PlayerDamage = R.ReadFloat(),
            ResourceDamage = R.ReadFloat(),
            ObjectDamage = R.ReadFloat()
        };
        public static void Write(ByteWriter W, EnvironmentDamage D)
        {
            W.Write(D.BarricadeDamage);
            W.Write(D.StructureDamage);
            W.Write(D.VehicleDamage);
            W.Write(D.AnimalDamage);
            W.Write(D.ZombieDamage);
            W.Write(D.PlayerDamage);
            W.Write(D.ResourceDamage);
            W.Write(D.ObjectDamage);
        }
    }
    public class Rank
    {
        [JsonSettable]
        public readonly int level;
        public readonly string name;
        public readonly Dictionary<string, string> name_translations;
        public readonly Dictionary<string, string> abbreviation_translations;
        [JsonSettable]
        public readonly string abbreviation;
        public readonly int XP;
        [JsonConstructor]
        public Rank(int level, string name, Dictionary<string, string> name_translations, string abbreviation, Dictionary<string, string> abbreviation_translations, int xp)
        {
            this.level = level;
            this.name = name;
            this.name_translations = name_translations;
            this.abbreviation = abbreviation;
            this.abbreviation_translations = abbreviation_translations;
            this.XP = xp;
        }
        public Rank(int level, string name, string abbreviation, int xp)
        {
            this.level = level;
            this.name = name;
            this.name_translations = new Dictionary<string, string>
            {
                { "en-us", name }
            };
            this.abbreviation_translations = new Dictionary<string, string>
            {
                { "en-us", abbreviation }
            };
            this.abbreviation = abbreviation;
            this.XP = xp;
        }
        public static Rank Read(ByteReader R) =>
            new Rank(R.ReadInt32(), R.ReadString(), R.ReadString(), R.ReadInt32());
        public static void Write(ByteWriter W, Rank R)
        {
            W.Write(R.level);
            W.Write(R.name);
            W.Write(R.abbreviation);
            W.Write(R.XP);
        }
        public static Rank[] ReadMany(ByteReader R)
        {
            byte count = R.ReadUInt8();
            Rank[] ranks = new Rank[count];
            for (int i = 0; i < count; i++)
                ranks[i] = Read(R);
            return ranks;
        }
        public static void WriteMany(ByteWriter W, Rank[] R)
        {
            W.Write((byte)R.Length);
            for (int i = 0; i < R.Length; i++)
                Write(W, R[i]);
        }
    }

    public interface IJsonReadWrite
    {
        void WriteJson(Utf8JsonWriter writer);
        void ReadJson(ref Utf8JsonReader reader);
    }

    public class Report : IReadWrite<Report>
    {
        public static readonly Type[] ReportTypes = new Type[]
        {
            typeof(Report),
            typeof(ChatAbuseReport),
            typeof(VoiceChatAbuseReport),
            typeof(SoloingReport),
            typeof(WasteingAssetsReport),
            typeof(IntentionalTeamkillReport),
            typeof(GreifingFOBsReport),
            typeof(CheatingReport)
        };
        public struct VehicleTime
        {
            public string VehicleName;
            public float Time;
            public DateTime Timestamp;
        }
        public struct VehicleTeamkill
        {
            public string VehicleName;
            public string Weapon;
            public string Origin;
            public ulong VehicleOwner;
            public DateTime Timestamp;
        }
        public struct Teamkill
        {
            public DateTime Timestamp;
            public ulong Dead;
            public string Weapon;
            public bool IsVehicle;
            public string DeathType;
        }
        public DateTime Time;
        public ulong Reporter;
        public ulong Violator;
        public string Message;
        public byte[] JpgData;
        public virtual byte Type => 0;
        public virtual int Size => 27;
        public static Report ReadReport(ByteReader R)
        {
            byte type = R.ReadUInt8();
            if (ReportTypes.Length <= type) return null;
            Type newtype = ReportTypes[type];
            if (Activator.CreateInstance(newtype) is not Report report) return null;
            report.Read(R);
            return report;
        }
        public static void WriteReport(ByteWriter W, Report R)
        {
            W.Write(R.Type);
            R.Write(W);
        }
        public virtual void Read(ByteReader R)
        {
            Time = R.ReadDateTime();
            Reporter = R.ReadUInt64();
            Violator = R.ReadUInt64();
            Message = R.ReadString();
            JpgData = R.ReadLongBytes();
        }
        public virtual void Write(ByteWriter W)
        {
            W.Write(Time);
            W.Write(Reporter);
            W.Write(Violator);
            W.Write(Message);
            W.WriteLong(JpgData ?? new byte[0]);
        }
        public override string ToString() => $"Report on {Violator} made by {Reporter} at {Time:s}: \"{Message}\" (" + (JpgData == null || JpgData.Length == 0 ? "No Screenshot Attached)." : "Screenshot Attached).");
    }
    public class CheatingReport : Report
    {
        public override byte Type => 7;
    }
    public class ChatAbuseReport : Report
    {
        public override byte Type => 1;
        public override int Size => 29;

        public string[] ChatRecords;
        public override void Read(ByteReader R)
        {
            base.Read(R);
            ChatRecords = R.ReadStringArray();
        }
        public override void Write(ByteWriter W)
        {
            base.Write(W);
            W.Write(ChatRecords);
        }
    }
    public class VoiceChatAbuseReport : Report
    {
        public override byte Type => 2;
        public override int Size => 31;
        public byte[] VoiceRecords;
        public override void Read(ByteReader R)
        {
            base.Read(R);
            VoiceRecords = R.ReadLongBytes();
        }
        public override void Write(ByteWriter W)
        {
            base.Write(W);
            W.WriteLong(VoiceRecords);
        }
    }
    public class SoloingReport : Report
    {
        public override byte Type => 3;
        public override int Size => 28;
        public VehicleTime[] Seats;
        public override void Read(ByteReader R)
        {
            base.Read(R);
            int len = R.ReadUInt8();
            Seats = new VehicleTime[len];
            for (int i = 0; i < len; i++)
            {
                Seats[i] = new VehicleTime()
                {
                    VehicleName = R.ReadString(),
                    Time = R.ReadFloat(),
                    Timestamp = R.ReadDateTime()
                };
            }
        }
        public override void Write(ByteWriter W)
        {
            base.Write(W);
            W.Write((byte)Seats.Length);
            for (int i = 0; i < Seats.Length; i++)
            {
                W.Write(Seats[i].VehicleName);
                W.Write(Seats[i].Time);
                W.Write(Seats[i].Timestamp);
            }
        }
    }
    public class WasteingAssetsReport : Report
    {
        public override byte Type => 4;
        public VehicleTime[] RecentRequests;
        public VehicleTeamkill[] RecentVehicleTeamkills;
        public override int Size => 29;
        public override void Read(ByteReader R)
        {
            base.Read(R);
            int len = R.ReadUInt8();
            RecentRequests = new VehicleTime[len];
            for (int i = 0; i < len; i++)
            {
                RecentRequests[i] = new VehicleTime()
                {
                    VehicleName = R.ReadString(),
                    Time = R.ReadFloat(),
                    Timestamp = R.ReadDateTime()
                };
            }
            len = R.ReadUInt8();
            RecentVehicleTeamkills = new VehicleTeamkill[len];
            for (int i = 0; i < len; i++)
            {
                RecentVehicleTeamkills[i] = new VehicleTeamkill()
                {
                    VehicleName = R.ReadString(),
                    Origin = R.ReadString(),
                    VehicleOwner = R.ReadUInt64(),
                    Weapon = R.ReadString(),
                    Timestamp = R.ReadDateTime()
                };
            }
        }
        public override void Write(ByteWriter W)
        {
            base.Write(W);
            W.Write((byte)RecentRequests.Length);
            for (int i = 0; i < RecentRequests.Length; i++)
            {
                W.Write(RecentRequests[i].VehicleName);
                W.Write(RecentRequests[i].Time);
                W.Write(RecentRequests[i].Timestamp);
            }
            W.Write((byte)RecentVehicleTeamkills.Length);
            for (int i = 0; i < RecentVehicleTeamkills.Length; i++)
            {
                W.Write(RecentVehicleTeamkills[i].VehicleName);
                W.Write(RecentVehicleTeamkills[i].Origin);
                W.Write(RecentVehicleTeamkills[i].VehicleOwner);
                W.Write(RecentVehicleTeamkills[i].Weapon);
                W.Write(RecentVehicleTeamkills[i].Timestamp);
            }
        }
    }
    public class IntentionalTeamkillReport : Report
    {
        public override byte Type => 5;
        public override int Size => 29;
        public Teamkill[] RecentTeamkills;
        public override void Read(ByteReader R)
        {
            base.Read(R);
            int len = R.ReadUInt16();
            RecentTeamkills = new Teamkill[len];
            for (int i = 0; i < len; i++)
            {
                RecentTeamkills[i] = new Teamkill()
                {
                    Dead = R.ReadUInt64(),
                    DeathType = R.ReadString(),
                    Weapon = R.ReadString(),
                    IsVehicle = R.ReadBool(),
                    Timestamp = R.ReadDateTime()
                };
            }
        }
        public override void Write(ByteWriter W)
        {
            base.Write(W);
            W.Write((ushort)RecentTeamkills.Length);
            for (int i = 0; i < RecentTeamkills.Length; i++)
            {
                W.Write(RecentTeamkills[i].Dead);
                W.Write(RecentTeamkills[i].DeathType);
                W.Write(RecentTeamkills[i].Weapon);
                W.Write(RecentTeamkills[i].IsVehicle);
                W.Write(RecentTeamkills[i].Timestamp);
            }
        }
    }
    public class GreifingFOBsReport : Report
    {
        public override byte Type => 6;
        public override int Size => 29;
        public struct StructureDamage
        {
            public DateTime Timestamp;
            public string Structure;
            public float Damage;
            public string DamageOrigin;
            public string Weapon;
        }
        public StructureDamage[] RecentDamage;
        public override void Read(ByteReader R)
        {
            base.Read(R);
            int len = R.ReadUInt16();
            RecentDamage = new StructureDamage[len];
            for (int i = 0; i < len; i++)
            {
                RecentDamage[i] = new StructureDamage()
                {
                    Structure = R.ReadString(),
                    Damage = R.ReadFloat(),
                    DamageOrigin = R.ReadString(),
                    Weapon = R.ReadString(),
                    Timestamp = R.ReadDateTime()
                };
            }
        }
        public override void Write(ByteWriter W)
        {
            base.Write(W);
            W.Write((ushort)RecentDamage.Length);
            for (int i = 0; i < RecentDamage.Length; i++)
            {
                W.Write(RecentDamage[i].Structure);
                W.Write(RecentDamage[i].Damage);
                W.Write(RecentDamage[i].DamageOrigin);
                W.Write(RecentDamage[i].Weapon);
                W.Write(RecentDamage[i].Timestamp);
            }
        }
    }

    public struct DailyQuest : IReadWrite<DailyQuest>
    {
        public const int DAILY_QUEST_LENGTH = 14;
        public const int DAILY_QUEST_CONDITION_LENGTH = 3;
        public const string WORKSHOP_FILE_NAME = "UC_DailyQuests";
        public const ushort DAILY_QUEST_START_ID = 62000;
        public const ushort DAILY_QUEST_FLAG_START_ID = 62000;
        public Condition[] conditions;
        public Guid guid;
        public static DailyQuest[] ReadMany(ByteReader R)
        {
            DailyQuest[] conditions = new DailyQuest[DAILY_QUEST_LENGTH];
            for (int i = 0; i < DAILY_QUEST_LENGTH; ++i)
            {
                conditions[i] = new DailyQuest();
                ref DailyQuest dq = ref conditions[i];
                dq.Read(R);
            }
            return conditions;
        }
        public static void WriteMany(ByteWriter W, DailyQuest[] conditions)
        {
            for (int i = 0; i < DAILY_QUEST_LENGTH; ++i)
            {
                ref DailyQuest dq = ref conditions[i];
                dq.Write(W);
            }
        }
        public void Read(ByteReader R)
        {
            guid = R.ReadGUID();
            conditions = new Condition[DAILY_QUEST_CONDITION_LENGTH];
            for (int i = 0; i < DAILY_QUEST_CONDITION_LENGTH; ++i)
            {
                conditions[i] = new Condition()
                {
                    Key = R.ReadGUID(),
                    FlagValue = R.ReadInt32(),
                    Translation = R.ReadString()
                };
            }
        }
        public void Write(ByteWriter W)
        {
            W.Write(guid);
            for (int i = 0; i < DAILY_QUEST_CONDITION_LENGTH; ++i)
            {
                ref Condition c = ref conditions[i];
                W.Write(c.Key);
                W.Write(c.FlagValue);
                W.Write(c.Translation);
            }
        }
        public struct Condition
        {
            public Guid Key;
            public int FlagValue;
            public string Translation;
        }
    }
    public struct Folder
    {
        public string name;
        public string[] folders;
        public File[] files;
        public struct File
        {
            public byte[] content;
            public string path;
        }
        public Folder(string directoryPath)
        {
            DirectoryInfo info = new DirectoryInfo(directoryPath);
            if (info.Exists)
            {
                string parentPath = info.FullName;
                name = info.Name;
                DirectoryInfo[] dirs = info.EnumerateDirectories("*", SearchOption.AllDirectories).ToArray();
                FileInfo[] files = info.EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
                folders = new string[dirs.Length];
                for (int i = 0; i < dirs.Length; i++)
                {
                    folders[i] = GetRelativePath(parentPath, dirs[i].FullName);
                }
                this.files = new File[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    this.files[i] = new File()
                    {
                        path = GetRelativePath(parentPath, files[i].FullName),
                        content = System.IO.File.ReadAllBytes(files[i].FullName)
                    };
                }
            }
            else throw new DirectoryNotFoundException();
        }
        public readonly void WriteToDisk(string directory)
        {
            string b = Directory.CreateDirectory(Path.Combine(directory, name)).FullName;
            for (int i = 0; i < folders.Length; ++i)
            {
                Directory.CreateDirectory(Path.Combine(b, folders[i]));
            }
            for (int i = 0; i < files.Length; i++)
            {
                ref File file = ref files[i];
                string path = Path.Combine(b, file.path);
                using FileStream stream = new FileStream(path, System.IO.File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.Write, FileShare.Read);
                stream.Write(file.content, 0, file.content.Length);
            }
        }
        public static Folder Read(ByteReader R)
        {
            Folder folder = new Folder();
            folder.name = R.ReadString();
            folder.folders = R.ReadStringArray();
            int len = R.ReadInt32();
            folder.files = new File[len];
            for (int i = 0; i < len; ++i)
            {
                folder.files[i] = new File()
                {
                    path = R.ReadString(),
                    content = R.ReadLongBytes()
                };
            }
            return folder;
        }
        public static void Write(ByteWriter W, Folder folder)
        {
            W.Write(folder.name);
            W.Write(folder.folders);
            W.Write(folder.files.Length);
            for (int i = 0; i < folder.files.Length; ++i)
            {
                ref File file = ref folder.files[i];
                W.Write(file.path);
                W.WriteLong(file.content);
            }
        }
        // https://stackoverflow.com/questions/51179331/is-it-possible-to-use-path-getrelativepath-net-core2-in-winforms-proj-targeti
        private static string GetRelativePath(string relativeTo, string path)
        {
            Uri uri = new Uri(relativeTo);
            string rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (!rel.Contains(Path.DirectorySeparatorChar.ToString()))
            {
                rel = $"." + Path.DirectorySeparatorChar + rel;
            }
            return rel;
        }
    }
    public enum EReportType : byte
    {
        CUSTOM = 0,
        CHAT_ABUSE = 1,
        VOICE_CHAT_ABUSE = 2,
        SOLOING_VEHICLE = 3,
        WASTEING_ASSETS = 4,
        INTENTIONAL_TEAMKILL = 5,
        GREIFING_FOBS = 6,
        CHEATING = 7
    }
    [Flags]
    public enum EAdminType : byte
    {
        ADMIN_ON_DUTY = 1,
        ADMIN_OFF_DUTY = 2,
        TRIAL_ADMIN_ON_DUTY = 4,
        TRIAL_ADMIN_OFF_DUTY = 8,
        HELPER = 16,

        ADMIN = ADMIN_ON_DUTY | ADMIN_OFF_DUTY,
        TRIAL_ADMIN = TRIAL_ADMIN_ON_DUTY | TRIAL_ADMIN_OFF_DUTY,
        MODERATE_PERMS_ON_DUTY = ADMIN_ON_DUTY | TRIAL_ADMIN_ON_DUTY,
        MODERATE_PERMS = ADMIN | TRIAL_ADMIN,
        STAFF = ADMIN | TRIAL_ADMIN | HELPER
    }
}