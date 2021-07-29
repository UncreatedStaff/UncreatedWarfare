using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Teams;
using static Uncreated.Warfare.UCWarfare;

namespace Uncreated.Warfare.Kits
{

    public delegate void KitChangedHandler(UCPlayer player, Kit kit, string oldKit);

    public class KitManager : JSONSaver<Kit>, IDisposable
    {
        public static event KitChangedHandler OnKitChanged;

        public KitManager() : base(Data.KitsStorage + "kits.json")
        {
            PlayerLife.OnPreDeath += PlayerLife_OnPreDeath;
        }
        private void PlayerLife_OnPreDeath(PlayerLife life)
        {
            if (HasKit(life.player.channel.owner.playerID.steamID, out var kit))
            {
                for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                {
                    if (page == PlayerInventory.AREA)
                        continue;

                    for (byte index = 0; index < life.player.inventory.getItemCount(page); index++)
                    {
                        ItemJar jar = life.player.inventory.getItem(page, index);

                        var asset = Rocket.Unturned.Items.UnturnedItems.GetItemAssetById(jar.item.id);
                        float percentage = (float)jar.item.amount / asset.amount;

                        bool notInKit = !kit.HasItemOfID(jar.item.id);
                        if (notInKit || (percentage < 0.3 && asset.type != EItemType.GUN))
                        {
                            if (notInKit)
                            {
                                ItemManager.dropItem(jar.item, life.player.transform.position, false, true, true);
                            }

                            life.player.inventory.removeItem(page, index);
                            index--;
                        }
                    }
                }
            }
        }

        protected override string LoadDefaults() 
        {
            if (JSONMethods.DefaultKits != default)
                return JsonConvert.SerializeObject(JSONMethods.DefaultKits, Formatting.Indented);
            else return "[]";
        }
        public static void CreateKit(string kitName, List<KitItem> items, List<KitClothing> clothes) => AddObjectToSave(new Kit(kitName, items, clothes));
        public static void DeleteKit(string kitName) => RemoveWhere(k => k.Name.ToLower() == kitName.ToLower());
        public static void DeleteAllKits() => RemoveAllObjectsFromSave();
        public static IEnumerable<Kit> GetKitsWhere(Func<Kit, bool> predicate) => GetObjectsWhere(predicate);
        public static bool KitExists(string kitName, out Kit kit) => ObjectExists(i => i != default && kitName != default && i.Name.ToLower() == kitName.ToLower(), out kit);
        public static bool OverwriteKitItems(string kitName, List<KitItem> newItems, List<KitClothing> newClothes, bool save = true)
        {
            if(KitExists(kitName, out Kit kit))
            {
                kit.Items = newItems ?? kit.Items;
                kit.Clothes = newClothes ?? kit.Clothes;
                if(save) Save();
                return true;
            }
            return false;
        }
        public static List<KitItem> ItemsFromInventory(UnturnedPlayer player)
        {
            var items = new List<KitItem>();

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                for (byte i = 0; i < player.Inventory.getItemCount(page); i++)
                {
                    ItemJar jar = player.Inventory.getItem(page, i);

                    items.Add(new KitItem(
                        jar.item.id,
                        jar.x,
                        jar.y,
                        jar.rot,
                        jar.item.quality,
                        Convert.ToBase64String(jar.item.metadata),
                        jar.item.amount,
                        page
                    ));
                }
            }

            return items;
        }
        public static List<KitClothing> ClothesFromInventory(UnturnedPlayer player)
        {
            PlayerClothing playerClothes = player.Player.clothing;

            var clothes = new List<KitClothing>();

            clothes.Add(new KitClothing(playerClothes.shirt, playerClothes.shirtQuality, Convert.ToBase64String(playerClothes.shirtState), KitClothing.EClothingType.SHIRT));
            clothes.Add(new KitClothing(playerClothes.pants, playerClothes.pantsQuality, Convert.ToBase64String(playerClothes.pantsState), KitClothing.EClothingType.PANTS));
            clothes.Add(new KitClothing(playerClothes.vest, playerClothes.vestQuality, Convert.ToBase64String(playerClothes.vestState), KitClothing.EClothingType.VEST));
            clothes.Add(new KitClothing(playerClothes.hat, playerClothes.hatQuality, Convert.ToBase64String(playerClothes.hatState), KitClothing.EClothingType.HAT));
            clothes.Add(new KitClothing(playerClothes.mask, playerClothes.maskQuality, Convert.ToBase64String(playerClothes.maskState), KitClothing.EClothingType.MASK));
            clothes.Add(new KitClothing(playerClothes.backpack, playerClothes.backpackQuality, Convert.ToBase64String(playerClothes.backpackState), KitClothing.EClothingType.BACKPACK));
            clothes.Add(new KitClothing(playerClothes.glasses, playerClothes.glassesQuality, Convert.ToBase64String(playerClothes.glassesState), KitClothing.EClothingType.GLASSES));

            return clothes;
        }
        public static async Task GiveKit(UCPlayer player, Kit kit)
        {
            if (kit == null)
                return;

            if (kit.ShouldClearInventory)
            {
                UCInventoryManager.ClearInventory(player);
            }
            foreach (var clothing in kit.Clothes)
            {
                if (clothing.type == KitClothing.EClothingType.SHIRT)
                    player.Player.clothing.askWearShirt(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == KitClothing.EClothingType.PANTS)
                    player.Player.clothing.askWearPants(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == KitClothing.EClothingType.VEST)
                    player.Player.clothing.askWearVest(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == KitClothing.EClothingType.HAT)
                    player.Player.clothing.askWearHat(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == KitClothing.EClothingType.MASK)
                    player.Player.clothing.askWearMask(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == KitClothing.EClothingType.BACKPACK)
                    player.Player.clothing.askWearBackpack(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == KitClothing.EClothingType.GLASSES)
                    player.Player.clothing.askWearGlasses(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
            }

            foreach (KitItem k in kit.Items)
            {
                Item item = new Item(k.ID, k.amount, k.quality) 
                { metadata = Convert.FromBase64String(k.metadata) };

                if (!player.Player.inventory.tryAddItem(item, k.x, k.y, k.page, k.rotation))
                    if (player.Player.inventory.tryAddItem(item, true))
                        ItemManager.dropItem(item, player.Position, true, true, true);
            }
            string oldkit = player.KitName;

            if (player.KitClass == Kit.EClass.MEDIC && kit.Class != Kit.EClass.MEDIC)
            {
                Data.ReviveManager.DeregisterMedic(player);
            }
            else if (kit.Class == Kit.EClass.MEDIC)
            {
                Data.ReviveManager.RegisterMedic(player);
            }
            player.KitName = kit.Name;
            player.KitClass = kit.Class;
            
            PlayerManager.UpdateObjectsWhere(x => x.Steam64 == player.Steam64, x => { x.KitName = kit.Name; });

            if (kit.IsPremium && kit.Cooldown > 0)
            {
                CooldownManager.StartCooldown(player, ECooldownType.PREMIUM_KIT, kit.Cooldown, kit.Name);
            }

            OnKitChanged?.Invoke(player, kit, oldkit);
            if (oldkit != null && oldkit != string.Empty)
                await RequestSigns.InvokeLangUpdateForSignsOfKit(oldkit);
            await RequestSigns.InvokeLangUpdateForSignsOfKit(kit.Name);

        }
        public static void ResupplyKit(UCPlayer player, Kit kit)
        {
            List<ItemJar> nonKitItems = new List<ItemJar>();

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (page == PlayerInventory.AREA)
                    continue;

                byte count = player.Player.inventory.getItemCount(page);

                for (byte index = 0; index < count; index++)
                {
                    ItemJar jar = player.Player.inventory.getItem(page, 0);

                    if (!kit.HasItemOfID(jar.item.id))
                    {
                        nonKitItems.Add(jar);
                    }
                    player.Player.inventory.removeItem(page, 0);
                }
            }

            foreach (var i in kit.Items)
            {
                var item = new Item(i.ID, i.amount, i.quality);
                item.metadata = System.Convert.FromBase64String(i.metadata);

                if (!player.Player.inventory.tryAddItem(item, i.x, i.y, i.page, i.rotation))
                    player.Player.inventory.tryAddItem(item, true);
            }

            foreach (var jar in nonKitItems)
            {
                player.Player.inventory.tryAddItem(jar.item, true);
            }
        }
        public static async Task<bool> TryGiveUnarmedKit(UCPlayer player)
        {
            string unarmedKit = "";
            if (player.IsTeam1())
                unarmedKit = TeamManager.Team1UnarmedKit;
            if (player.IsTeam2())
                unarmedKit = TeamManager.Team2UnarmedKit;

            if (KitManager.KitExists(unarmedKit, out var kit))
            {
                await KitManager.GiveKit(player, kit);
                return true;
            }
            return false;
        }
        public static bool HasKit(ulong steamID, out Kit kit)
        {
            kit = null;
            var player = UCPlayer.FromID(steamID);

            if (player is null)
            {
                kit = GetObject(k => k.Name == PlayerManager.GetSave(steamID).KitName);
                return kit != null;
            }
            else
            {
                kit = GetObject(k => k.Name == player.KitName);
                return kit != null;
            }
        }
        public static bool HasKit(UnturnedPlayer player, out Kit kit) => HasKit(player.Player.channel.owner.playerID.steamID.m_SteamID, out kit);
        public static bool HasKit(SteamPlayer player, out Kit kit) => HasKit(player.playerID.steamID.m_SteamID, out kit);
        public static bool HasKit(Player player, out Kit kit) => HasKit(player.channel.owner.playerID.steamID.m_SteamID, out kit);
        public static bool HasKit(CSteamID player, out Kit kit) => HasKit(player.m_SteamID, out kit);
        public static bool HasAccess(ulong playerID, string kitName)
        {
            if (UCWarfare.Config.OverrideKitRequirements) return true;
            if (KitExists(kitName, out Kit kit))
                return kit.AllowedUsers.Contains(playerID);
            else return false;
        }
        public static async Task<bool> UpdateText(string kitname, string SignName, string language = JSONMethods.DefaultLanguage)
        {
            if (KitExists(kitname, out Kit kit))
            {
                List<Kit> matches = GetObjectsWhere(k => k.Name == kit.Name);
                for (int i = 0; i < matches.Count; i++)
                {
                    matches[i].SignName = SignName;
                    matches[i].SignTexts.Remove(language);
                    matches[i].SignTexts.Add(language, SignName);
                    await RequestSigns.InvokeLangUpdateForSignsOfKit(matches[i].Name);
                }
                return true;
            }
            else return false;
        }
        public static IEnumerable<Kit> GetAccessibleKits(ulong playerID) => GetObjectsWhere(k => k.AllowedUsers.Contains(playerID));
        public static async Task GiveAccess(ulong playerID, string kitName)
        {
            if (KitExists(kitName, out Kit kit))
            {
                if (!kit.AllowedUsers.Contains(playerID))
                {
                    kit.AllowedUsers.Add(playerID);
                    Save();
                    if (RequestSigns.SignExists(kit.Name, out RequestSign sign))
                        await sign.InvokeUpdate();
                }
            }
        }
        public static async Task RemoveAccess(ulong playerID, string kitName)
        {
            if (KitExists(kitName, out Kit kit))
            {
                kit.AllowedUsers.RemoveAll(i => i == playerID);
                Save();
                if (RequestSigns.SignExists(kit.Name, out RequestSign sign))
                    await sign.InvokeUpdate();
            }
        }

        public void Dispose()
        {
            PlayerLife.OnPreDeath -= PlayerLife_OnPreDeath;
        }
    }

    public class Kit
    {
        public string Name;
        public string DisplayName
        {
            get
            {
                switch (this.Class)
                {
                    case EClass.UNARMED:
                        return "Unarmed";
                    case EClass.SQUADLEADER:
                        return "Squad Leader";
                    case EClass.RIFLEMAN:
                        return "Rifleman";
                    case EClass.MEDIC:
                        return "Medic";
                    case EClass.BREACHER:
                        return "Breacher";
                    case EClass.AUTOMATIC_RIFLEMAN:
                        return "Automatic Rifleman";
                    case EClass.GRENADIER:
                        return "Grenadier";
                    case EClass.MACHINE_GUNNER:
                        return "Machine Gunner";
                    case EClass.LAT:
                        return "Light Anti-Tank";
                    case EClass.HAT:
                        return "Heavy Anti-Tank";
                    case EClass.MARKSMAN:
                        return "Designated Marksman";
                    case EClass.SNIPER:
                        return "Sniper";
                    case EClass.AP_RIFLEMAN:
                        return "Anti-Personnel Rifleman";
                    case EClass.COMBAT_ENGINEER:
                        return "Combat Engineer";
                    case EClass.CREWMAN:
                        return "Crewman";
                    case EClass.PILOT:
                        return "Pilot";

                }
                return Name;
            }
        }
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
        public float TeamLimit;
        [JsonSettable]
        public float Cooldown;
        [JsonSettable]
        public bool ShouldClearInventory;
        public List<KitItem> Items;
        public List<KitClothing> Clothes;
        public List<ulong> AllowedUsers { get; protected set; }
        /// <summary>
        /// {0} = Kit name color (team color), {1} = price, {2} = price color
        /// </summary>
        public Dictionary<string, string> SignTexts;
        public Kit(string name, List<KitItem> items, List<KitClothing> clothes)
        {
            Name = name;
            Items = items;
            Clothes = clothes;
            Class = EClass.NONE;
            Branch = EBranch.DEFAULT;
            Team = 0;
            Cost = 0;
            RequiredLevel = 0;
            TicketCost = 1;
            IsPremium = false;
            PremiumCost = 0;
            TeamLimit = 1;
            Cooldown = 0;
            SignName = DisplayName;
            ShouldClearInventory = true;
            AllowedUsers = new List<ulong>();
            SignTexts = new Dictionary<string, string> { { JSONMethods.DefaultLanguage, $"<color=#{{0}}>{SignName}</color>\n<color=#{{2}}>{{1}}</color>" } };
        }
        public Kit()
        {
            Name = "default";
            Items = new List<KitItem>();
            Clothes = new List<KitClothing>();
            Class = EClass.NONE;
            Branch = EBranch.DEFAULT;
            Team = 0;
            Cost = 0;
            RequiredLevel = 0;
            TicketCost = 1;
            IsPremium = false;
            PremiumCost = 0;
            TeamLimit = 1;
            Cooldown = 0;
            SignName = "Default";
            ShouldClearInventory = true;
            AllowedUsers = new List<ulong>();
            SignTexts = new Dictionary<string, string> { { JSONMethods.DefaultLanguage, $"<color=#{{0}}>{SignName}</color>\n<color=#{{2}}>{{1}}</color>" } };
        }
        public bool HasItemOfID(ushort ID) => this.Items.Exists(i => i.ID == ID);
        public bool IsLimited(out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
        {
            ulong Team = team == 1 || team == 2 ? team : this.Team;
            currentPlayers = 0;
            allowedPlayers = 24;
            if (!requireCounts && (IsPremium || TeamLimit >= 1f))
                return false;
            IEnumerable<UCPlayer> friendlyPlayers = Team == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == Team);
            allowedPlayers = (int)Math.Ceiling(TeamLimit * friendlyPlayers.Count());
            currentPlayers = friendlyPlayers.Count(k => k.KitName == Name);
            if (IsPremium || TeamLimit >= 1f)
                return false;
            return currentPlayers + 1 > allowedPlayers;
        }
        public enum EClothingType : byte
        {
            SHIRT,
            PANTS,
            VEST,
            HAT,
            MASK,
            BACKPACK,
            GLASSES
        }
        public enum EClass : byte
        {
            NONE, //0 
            UNARMED, //1
            SQUADLEADER, //2
            RIFLEMAN, //3
            MEDIC, //4
            BREACHER, //5
            AUTOMATIC_RIFLEMAN, //6
            GRENADIER, //7
            MACHINE_GUNNER, //8
            LAT, //9
            HAT, //10
            MARKSMAN, //11
            SNIPER, //12
            AP_RIFLEMAN, //13
            COMBAT_ENGINEER, //14
            CREWMAN, //15
            PILOT, //16
            SPEC_OPS // 17
        }
    }
    public class KitItem
    {
        public ushort ID;
        public byte x;
        public byte y;
        public byte rotation;
        public byte quality;
        public string metadata;
        public byte amount;
        public byte page;

        public KitItem(ushort ID, byte x, byte y, byte rotation, byte quality, string metadata, byte amount, byte page)
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
    }
    public class KitClothing
    {
        public ushort ID;
        public byte quality;
        public string state;
        public EClothingType type;

        public KitClothing(ushort ID, byte quality, string state, EClothingType type)
        {
            this.ID = ID;
            this.quality = quality;
            this.state = state;
            this.type = type;
        }
        public enum EClothingType
        {
            SHIRT,
            PANTS,
            VEST,
            HAT,
            MASK,
            BACKPACK,
            GLASSES
        }
    }

    public enum EBranch
    {
        DEFAULT,
        INFANTRY,
        ARMOR,
        AIRFORCE,
        SPECOPS,
    }
}
