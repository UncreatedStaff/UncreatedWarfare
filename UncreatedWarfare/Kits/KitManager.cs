using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Teams;
using Item = SDG.Unturned.Item;

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
            if (HasKit(life.player.channel.owner.playerID.steamID, out Kit kit))
            {
                for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                {
                    if (page == PlayerInventory.AREA)
                        continue;

                    for (byte index = 0; index < life.player.inventory.getItemCount(page); index++)
                    {
                        ItemJar jar = life.player.inventory.getItem(page, index);

                        if (!(Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)) continue;
                        float percentage = (float)jar.item.amount / asset.amount;

                        bool notInKit = !kit.HasItemOfID(jar.item.id) && Whitelister.IsWhitelisted(asset.GUID, out _);
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
        public static Kit CreateKit(string kitName, List<KitItem> items, List<KitClothing> clothes) => AddObjectToSave(KitEx.Construct(kitName, items, clothes));
        public static Kit CreateKit(Kit kit) => AddObjectToSave(kit);
        public static void DeleteKit(string kitName) => RemoveWhere(k => k.Name.ToLower() == kitName.ToLower());
        public static void DeleteAllKits() => RemoveAllObjectsFromSave();
        public static IEnumerable<Kit> GetKitsWhere(Func<Kit, bool> predicate) => GetObjectsWhere(predicate);
        public static bool KitExists(string kitName, out Kit kit) => ObjectExists(i => i != default && kitName != default && i.Name.ToLower() == kitName.ToLower(), out kit);
        public static bool OverwriteKitItems(string kitName, List<KitItem> newItems, List<KitClothing> newClothes, bool save = true)
        {
            if (KitExists(kitName, out Kit kit))
            {
                kit.Items = newItems ?? kit.Items;
                kit.Clothes = newClothes ?? kit.Clothes;
                if (save) Save();
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

            List<KitClothing> clothes = new List<KitClothing>
            {
                new KitClothing(playerClothes.shirt, playerClothes.shirtQuality, Convert.ToBase64String(playerClothes.shirtState), EClothingType.SHIRT),
                new KitClothing(playerClothes.pants, playerClothes.pantsQuality, Convert.ToBase64String(playerClothes.pantsState), EClothingType.PANTS),
                new KitClothing(playerClothes.vest, playerClothes.vestQuality, Convert.ToBase64String(playerClothes.vestState), EClothingType.VEST),
                new KitClothing(playerClothes.hat, playerClothes.hatQuality, Convert.ToBase64String(playerClothes.hatState), EClothingType.HAT),
                new KitClothing(playerClothes.mask, playerClothes.maskQuality, Convert.ToBase64String(playerClothes.maskState), EClothingType.MASK),
                new KitClothing(playerClothes.backpack, playerClothes.backpackQuality, Convert.ToBase64String(playerClothes.backpackState), EClothingType.BACKPACK),
                new KitClothing(playerClothes.glasses, playerClothes.glassesQuality, Convert.ToBase64String(playerClothes.glassesState), EClothingType.GLASSES)
            };
            return clothes;
        }
        public static void GiveKit(UCPlayer player, Kit kit)
        {
            //DateTime start = DateTime.Now;

            if (kit == null)
                return;

            if (kit.ShouldClearInventory)
            {
                UCInventoryManager.ClearInventory(player);
            }
            foreach (KitClothing clothing in kit.Clothes)
            {
                if (clothing.type == EClothingType.SHIRT)
                    player.Player.clothing.askWearShirt(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == EClothingType.PANTS)
                    player.Player.clothing.askWearPants(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == EClothingType.VEST)
                    player.Player.clothing.askWearVest(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == EClothingType.HAT)
                    player.Player.clothing.askWearHat(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == EClothingType.MASK)
                    player.Player.clothing.askWearMask(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == EClothingType.BACKPACK)
                    player.Player.clothing.askWearBackpack(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                if (clothing.type == EClothingType.GLASSES)
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

            if (Data.Is(out IRevives g))
            {
                if (player.KitClass == EClass.MEDIC && kit.Class != EClass.MEDIC)
                {
                    g.ReviveManager.DeregisterMedic(player);
                }
                else if (kit.Class == EClass.MEDIC)
                {
                    g.ReviveManager.RegisterMedic(player);
                }
            }
            player.KitName = kit.Name;
            player.KitClass = kit.Class;

            EBranch oldBranch = player.Branch;

            player.Branch = kit.Branch;

            if (oldBranch != kit.Branch && kit.Branch != EBranch.DEFAULT)
            {
                Points.OnBranchChanged(player, oldBranch, kit.Branch);
            }

            
            


            OnKitChanged?.Invoke(player, kit, oldkit);
            if (oldkit != null && oldkit != string.Empty)
                RequestSigns.InvokeLangUpdateForSignsOfKit(oldkit);
            RequestSigns.InvokeLangUpdateForSignsOfKit(kit.Name);

        }
        public static void ResupplyKit(UCPlayer player, Kit kit, bool ignoreAmmoBags = false)
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
                    if (!(Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)) continue;
                    if (!kit.HasItemOfID(jar.item.id) && Whitelister.IsWhitelisted(asset.GUID, out _))
                    {
                        nonKitItems.Add(jar);
                    }
                    player.Player.inventory.removeItem(page, 0);
                }
            }

            foreach (KitItem i in kit.Items)
            {
                if (ignoreAmmoBags && Assets.find(Gamemode.Config.Barricades.AmmoBagGUID) is ItemAsset asset && asset.id == i.ID)
                    continue;
                Item item = new Item(i.ID, i.amount, i.quality);
                item.metadata = Convert.FromBase64String(i.metadata);

                if (!player.Player.inventory.tryAddItem(item, i.x, i.y, i.page, i.rotation))
                    player.Player.inventory.tryAddItem(item, true);
            }

            foreach (ItemJar jar in nonKitItems)
            {
                player.Player.inventory.tryAddItem(jar.item, true);
            }
        }
        public static bool TryGiveUnarmedKit(UCPlayer player)
        {
            string unarmedKit = "";
            if (player.IsTeam1())
                unarmedKit = TeamManager.Team1UnarmedKit;
            if (player.IsTeam2())
                unarmedKit = TeamManager.Team2UnarmedKit;

            if (KitExists(unarmedKit, out Kit kit))
            {
                GiveKit(player, kit);
                return true;
            }
            return false;
        }
        public static bool TryGiveRiflemanKit(UCPlayer player)
        {
            Kit rifleman = GetKitsWhere(k =>
                    k.Team == player.GetTeam() &&
                    k.Class == EClass.RIFLEMAN &&
                    !k.IsPremium &&
                    !k.IsLoadout &&
                    k.TeamLimit == 1 &&
                    k.UnlockLevel == 0
                ).FirstOrDefault();

            if (rifleman != null)
            {
                GiveKit(player, rifleman);
                return true;
            }
            return false;
        }
        public static void AddRequest(Kit kit)
        {
            kit.Requests++;
            Save();
        }
        public static bool HasKit(ulong steamID, out Kit kit)
        {
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
        public static bool HasKit(UCPlayer player, out Kit kit)
        {
            kit = GetObject(k => k.Name == player.KitName);
            return kit != null;
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
        public static bool UpdateText(string kitname, string SignName, string language = JSONMethods.DefaultLanguage)
        {
            if (KitExists(kitname, out Kit kit))
            {
                List<Kit> matches = GetObjectsWhere(k => k.Name == kit.Name);
                for (int i = 0; i < matches.Count; i++)
                {
                    matches[i].SignName = SignName;
                    matches[i].SignTexts.Remove(language);
                    matches[i].SignTexts.Add(language, SignName);
                    RequestSigns.InvokeLangUpdateForSignsOfKit(matches[i].Name);
                }
                return true;
            }
            else return false;
        }
        public static IEnumerable<Kit> GetAccessibleKits(ulong playerID) => GetObjectsWhere(k => k.AllowedUsers.Contains(playerID));
        public static void GiveAccess(ulong playerID, string kitName)
        {
            if (KitExists(kitName, out Kit kit))
            {
                if (!kit.AllowedUsers.Contains(playerID))
                {
                    kit.AllowedUsers.Add(playerID);
                    Save();
                    if (RequestSigns.SignExists(kit.Name, out RequestSign sign))
                        sign.InvokeUpdate();
                }
            }
        }
        public static void RemoveAccess(ulong playerID, string kitName)
        {
            if (KitExists(kitName, out Kit kit))
            {
                kit.AllowedUsers.RemoveAll(i => i == playerID);
                Save();
                if (RequestSigns.SignExists(kit.Name, out RequestSign sign))
                    sign.InvokeUpdate();
            }
        }

        public void Dispose()
        {
            PlayerLife.OnPreDeath -= PlayerLife_OnPreDeath;
        }
    }
    public static class KitEx
    {
        public static Kit Construct(string name, List<KitItem> items, List<KitClothing> clothes, Action<Kit> modifiers = null)
        {
            Kit kit = new Kit(true)
            {
                Name = name,
                Items = items,
                Clothes = clothes,
                Class = EClass.NONE,
                Branch = EBranch.DEFAULT,
                Team = 0,
                Cost = 0,
                UnlockLevel = 0,
                TicketCost = 1,
                IsPremium = false,
                PremiumCost = 0,
                IsLoadout = false,
                TeamLimit = 1,
                Cooldown = 0,
                ShouldClearInventory = true,
                AllowedUsers = new List<ulong>(),
                Requests = 0
            };
            kit.SignName = kit.DisplayName;
            kit.SignTexts = new Dictionary<string, string> { { JSONMethods.DefaultLanguage, kit.SignName } };
            if (kit.Items == null || items.Count == 0)
                kit.Weapons = string.Empty;
            else
            {
                KitItem i = items.OrderByDescending(x => x.metadata == null ? 0 : x.metadata.Length).First();
                if (i == null) kit.Weapons = string.Empty;
                else if (!(Assets.find(EAssetType.ITEM, i.ID) is ItemAsset asset)) kit.Weapons = string.Empty;
                else kit.Weapons = asset.itemName;
            }
            if (modifiers != null) modifiers(kit);
            return kit;
        }
        public static bool HasItemOfID(this Kit kit, ushort ID) => kit.Items.Exists(i => i.ID == ID);
        public static bool IsLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
        {
            ulong Team = team == 1 || team == 2 ? team : kit.Team;
            currentPlayers = 0;
            allowedPlayers = 24;
            if (!requireCounts && (kit.IsPremium || kit.TeamLimit >= 1f))
                return false;
            IEnumerable<UCPlayer> friendlyPlayers = Team == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == Team);
            allowedPlayers = (int)Math.Ceiling(kit.TeamLimit * friendlyPlayers.Count());
            currentPlayers = friendlyPlayers.Count(k => k.KitName == kit.Name);
            if (kit.IsPremium || kit.TeamLimit >= 1f)
                return false;
            return currentPlayers + 1 > allowedPlayers;
        }

        public static bool IsClassLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
        {
            ulong Team = team == 1 || team == 2 ? team : kit.Team;
            currentPlayers = 0;
            allowedPlayers = 24;
            if (!requireCounts && (kit.IsPremium || kit.TeamLimit >= 1f))
                return false;
            IEnumerable<UCPlayer> friendlyPlayers = Team == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == Team);
            allowedPlayers = (int)Math.Ceiling(kit.TeamLimit * friendlyPlayers.Count());
            currentPlayers = friendlyPlayers.Count(k => k.KitClass == kit.Class);
            if (kit.IsPremium || kit.TeamLimit >= 1f)
                return false;
            return currentPlayers + 1 > allowedPlayers;
        }
    }
}
