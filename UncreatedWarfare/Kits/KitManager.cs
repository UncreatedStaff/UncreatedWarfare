using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using Item = SDG.Unturned.Item;

namespace Uncreated.Warfare.Kits
{

    public delegate void KitChangedHandler(UCPlayer player, Kit kit, string oldKit);

    public class KitManager : JSONSaver<Kit>, IDisposable
    {
        public static event KitChangedHandler OnKitChanged;

        public KitManager() : base(Data.KitsStorage + "kits.json", KitEx.WriteKitJson, KitEx.ReadKitJson)
        {
            PlayerLife.OnPreDeath += PlayerLife_OnPreDeath;
        }
        private void PlayerLife_OnPreDeath(PlayerLife life)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (HasKit(life.player.channel.owner.playerID.steamID, out Kit kit))
            {
                for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                {
                    if (page == PlayerInventory.AREA)
                        continue;

                    for (byte index = 0; index < life.player.inventory.getItemCount(page); index++)
                    {
                        ItemJar jar = life.player.inventory.getItem(page, index);

                        if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) continue;
                        float percentage = (float)jar.item.amount / asset.amount;

                        bool notInKit = !kit.HasItemOfID(asset.GUID) && Whitelister.IsWhitelisted(asset.GUID, out _);
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
            return "[]";
        }
        public static Kit CreateKit(string kitName, List<KitItem> items, List<KitClothing> clothes) => AddObjectToSave(KitEx.Construct(kitName, items, clothes));
        public static Kit? CreateKit(Kit? kit)
        {
            if (kit != null)
                AddObjectToSave(kit);
            return kit;
        }
        public static void DeleteKit(string kitName) => RemoveWhere(k => k.Name.ToLower() == kitName.ToLower());
        public static void DeleteAllKits() => RemoveAllObjectsFromSave();
        public static IEnumerable<Kit> GetKitsWhere(Func<Kit, bool> predicate) => GetObjectsWhere(predicate);
        public static bool KitExists(string kitName, out Kit kit) => ObjectExists(i => i != default && kitName != default && i.Name.ToLower() == kitName.ToLower(), out kit);
        public static bool OverwriteKitItems(string kitName, List<KitItem> newItems, List<KitClothing> newClothes, bool save = true)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (KitExists(kitName, out Kit kit))
            {
                kit.Items = newItems ?? kit.Items;
                kit.Clothes = newClothes ?? kit.Clothes;
                if (save) Save();
                return true;
            }
            return false;
        }
        public static List<KitItem> ItemsFromInventory(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            List<KitItem> items = new List<KitItem>();

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                for (byte i = 0; i < player.Player.inventory.getItemCount(page); i++)
                {
                    ItemJar jar = player.Player.inventory.getItem(page, i);
                    if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)
                    {
                        items.Add(new KitItem(
                            asset.GUID,
                            jar.x,
                            jar.y,
                            jar.rot,
                            Convert.ToBase64String(jar.item.metadata),
                            jar.item.amount,
                            page
                        ));
                    }
                }
            }

            return items;
        }
        public static List<KitClothing> ClothesFromInventory(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            PlayerClothing playerClothes = player.Player.clothing;
            List<KitClothing> clothes = new List<KitClothing>(7);

            if (playerClothes.shirtAsset != null)
                clothes.Add(new KitClothing(playerClothes.shirtAsset.GUID, Convert.ToBase64String(playerClothes.shirtState), EClothingType.SHIRT));
            if (playerClothes.pantsAsset != null)
                clothes.Add(new KitClothing(playerClothes.pantsAsset.GUID, Convert.ToBase64String(playerClothes.pantsState), EClothingType.PANTS));
            if (playerClothes.vestAsset != null)
                clothes.Add(new KitClothing(playerClothes.vestAsset.GUID, Convert.ToBase64String(playerClothes.vestState), EClothingType.VEST));
            if (playerClothes.hatAsset != null)
                clothes.Add(new KitClothing(playerClothes.hatAsset.GUID, Convert.ToBase64String(playerClothes.hatState), EClothingType.HAT));
            if (playerClothes.maskAsset != null)
                clothes.Add(new KitClothing(playerClothes.maskAsset.GUID, Convert.ToBase64String(playerClothes.maskState), EClothingType.MASK));
            if (playerClothes.backpackAsset != null)
                clothes.Add(new KitClothing(playerClothes.backpackAsset.GUID, Convert.ToBase64String(playerClothes.backpackState), EClothingType.BACKPACK));
            if (playerClothes.glassesAsset != null)
                clothes.Add(new KitClothing(playerClothes.glassesAsset.GUID, Convert.ToBase64String(playerClothes.glassesState), EClothingType.GLASSES));
            
            return clothes;
        }
        public static void OnPlayerJoinedQuestHandling(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (!ActiveObjects[i].IsLoadout && ActiveObjects[i].UnlockRequirements != null)
                {
                    Kit kit = ActiveObjects[i];
                    for (int j = 0; j < kit.UnlockRequirements.Length; j++)
                    {
                        if (kit.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets != null && req.UnlockPresets.Length > 0 && !req.CanAccess(player))
                        {
                            if (Assets.find(req.QuestID) is QuestAsset quest)
                            {
                                player.Player.quests.sendAddQuest(quest.id);
                            }
                            else
                            {
                                L.LogWarning("Unknown quest id " + req.QuestID + " in kit requirement for " + kit.Name);
                            }
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                BaseQuestTracker? tracker = QuestManager.CreateTracker(player, req.UnlockPresets[r]);
                                if (tracker == null)
                                {
                                    L.LogWarning("Failed to create tracker for kit " + kit.Name + ", player " + player.Name.PlayerName);
                                }
                            }
                        }
                    }
                }
            }

            RequestSigns.InvokeLangUpdateForAllSigns(player.Player.channel.owner);
        }
        public static bool OnQuestCompleted(UCPlayer player, Guid presetKey)
        {
            bool affectedKit = false;
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (!ActiveObjects[i].IsLoadout && ActiveObjects[i].UnlockRequirements != null)
                {
                    Kit kit = ActiveObjects[i];
                    for (int j = 0; j < kit.UnlockRequirements.Length; j++)
                    {
                        if (kit.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets != null && req.UnlockPresets.Length > 0 && !req.CanAccess(player))
                        {
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                if (req.UnlockPresets[r] == presetKey)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return affectedKit;
        }

        public static void GiveKit(UCPlayer player, Kit kit)
        {
            if (kit == null)
                return;
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (HasKit(player, out Kit oldKit))
            {
                if (oldKit.Skillsets != null)
                {
                    for (int i = 0; i < oldKit.Skillsets.Length; i++)
                    {
                        ref Skillset skillset = ref kit.Skillsets[i];
                        for (int j = 0; j < Skillset.DEFAULT_SKILLSETS.Length; j++)
                        {
                            ref Skillset skillset2 = ref Skillset.DEFAULT_SKILLSETS[j];
                            if (skillset2.TypeEquals(ref skillset))
                            {
                                for (int k = 0; k < kit.Skillsets.Length; k++)
                                {
                                    ref Skillset skillset3 = ref kit.Skillsets[j];
                                    if (skillset2 == skillset3) goto next;
                                }
                                skillset2.ServerSet(player);
                                goto next;
                            }
                        }
                        player.Player.skills.ServerSetSkillLevel(skillset.SpecialityIndex, skillset.SkillIndex, 0);
                        next:;
                    }
                }
            }
            if (kit.Skillsets != null)
            {
                for (int i = 0; i < kit.Skillsets.Length; i++)
                {
                    ref Skillset skillset = ref kit.Skillsets[i];
                    skillset.ServerSet(player);
                }
            }
            UCInventoryManager.ClearInventory(player);
            foreach (KitClothing clothing in kit.Clothes)
            {
                if (Assets.find(clothing.id) is ItemAsset asset)
                {
                    if (clothing.type == EClothingType.SHIRT)
                        player.Player.clothing.askWearShirt(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == EClothingType.PANTS)
                        player.Player.clothing.askWearPants(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == EClothingType.VEST)
                        player.Player.clothing.askWearVest(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == EClothingType.HAT)
                        player.Player.clothing.askWearHat(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == EClothingType.MASK)
                        player.Player.clothing.askWearMask(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == EClothingType.BACKPACK)
                        player.Player.clothing.askWearBackpack(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == EClothingType.GLASSES)
                        player.Player.clothing.askWearGlasses(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                }
            }

            foreach (KitItem k in kit.Items)
            {
                if (Assets.find(k.id) is ItemAsset asset)
                {
                    Item item = new Item(asset.id, k.amount, 100)
                        { metadata = Convert.FromBase64String(k.metadata) };
                    if (!player.Player.inventory.tryAddItem(item, k.x, k.y, k.page, k.rotation))
                        if (player.Player.inventory.tryAddItem(item, true))
                            ItemManager.dropItem(item, player.Position, true, true, true);
                }
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

            if (oldBranch != player.Branch)
            {
                //Points.OnBranchChanged(player, oldBranch, kit.Branch);
            }

            OnKitChanged?.Invoke(player, kit, oldkit);
            if (oldkit != null && oldkit != string.Empty)
                RequestSigns.InvokeLangUpdateForSignsOfKit(oldkit);
            RequestSigns.InvokeLangUpdateForSignsOfKit(kit.Name);
        }
        public static void ResupplyKit(UCPlayer player, Kit kit, bool ignoreAmmoBags = false)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            List<ItemJar> nonKitItems = new List<ItemJar>();

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (page == PlayerInventory.AREA)
                    continue;

                byte count = player.Player.inventory.getItemCount(page);

                for (byte index = 0; index < count; index++)
                {
                    ItemJar jar = player.Player.inventory.getItem(page, 0);
                    if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) continue;
                    if (!kit.HasItemOfID(asset.GUID) && Whitelister.IsWhitelisted(asset.GUID, out _))
                    {
                        nonKitItems.Add(jar);
                    }
                    player.Player.inventory.removeItem(page, 0);
                }
            }

            for (int i = 0; i < kit.Clothes.Count; i++)
            {
                KitClothing clothing = kit.Clothes[i];
                if (Assets.find(clothing.id) is ItemAsset asset)
                {
                    ushort old = 0;
                    switch (clothing.type)
                    {
                        case EClothingType.GLASSES:
                            if (player.Player.clothing.glasses != asset.id)
                            {
                                old = player.Player.clothing.glasses;
                                player.Player.clothing.askWearGlasses(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                            }
                            break;
                        case EClothingType.HAT:
                            if (player.Player.clothing.hat != asset.id)
                            {
                                old = player.Player.clothing.hat;
                                player.Player.clothing.askWearHat(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                            }
                            break;
                        case EClothingType.BACKPACK:
                            if (player.Player.clothing.backpack != asset.id)
                            {
                                old = player.Player.clothing.backpack;
                                player.Player.clothing.askWearBackpack(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                            }
                            break;
                        case EClothingType.MASK:
                            if (player.Player.clothing.mask != asset.id)
                            {
                                old = player.Player.clothing.mask;
                                player.Player.clothing.askWearMask(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                            }
                            break;
                        case EClothingType.PANTS:
                            if (player.Player.clothing.pants != asset.id)
                            {
                                old = player.Player.clothing.pants;
                                player.Player.clothing.askWearPants(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                            }
                            break;
                        case EClothingType.SHIRT:
                            if (player.Player.clothing.shirt != asset.id)
                            {
                                old = player.Player.clothing.shirt;
                                player.Player.clothing.askWearShirt(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                            }
                            break;
                        case EClothingType.VEST:
                            if (player.Player.clothing.vest != asset.id)
                            {
                                old = player.Player.clothing.vest;
                                player.Player.clothing.askWearVest(asset.id, 100, Convert.FromBase64String(clothing.state), true);
                            }
                            break;
                    }
                    if (old != 0)
                        player.Player.inventory.removeItem(2, 0);
                }
            }
            foreach (KitItem i in kit.Items)
            {
                if (ignoreAmmoBags && Gamemode.Config.Barricades.AmmoBagGUID == i.id)
                    continue;
                if (Assets.find(i.id) is ItemAsset itemasset)
                {
                    Item item = new Item(itemasset.id, i.amount, 100, Convert.FromBase64String(i.metadata));

                    if (!player.Player.inventory.tryAddItem(item, i.x, i.y, i.page, i.rotation))
                        player.Player.inventory.tryAddItem(item, true);
                }
            }

            foreach (ItemJar jar in nonKitItems)
            {
                player.Player.inventory.tryAddItem(jar.item, true);
            }
        }
        public static bool TryGiveUnarmedKit(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Kit rifleman = GetKitsWhere(k =>
                    k.Team == player.GetTeam() &&
                    k.Class == EClass.RIFLEMAN &&
                    !k.IsPremium &&
                    !k.IsLoadout &&
                    k.TeamLimit == 1 &&
                    k.UnlockRequirements.Length == 0
                ).FirstOrDefault();

            if (rifleman != null)
            {
                GiveKit(player, rifleman);
                return true;
            }
            return false;
        }
        public static bool HasKit(ulong steamID, out Kit kit)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromID(steamID);

            if (player == null)
            {
                PlayerSave? save = PlayerManager.GetSave(steamID);
                if (save == null)
                {
                    kit = null!;
                    return false;
                }
                kit = GetObject(k => k.Name == save.KitName);
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        public static bool UpdateText(string kitname, string SignName, string language = JSONMethods.DEFAULT_LANGUAGE)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (KitExists(kitname, out Kit kit))
            {
                IEnumerable<Kit> matches = GetObjectsWhere(k => k.Name == kit.Name);
                foreach (Kit k in matches)
                {
                    k.SignTexts.Remove(language);
                    k.SignTexts.Add(language, SignName);
                    RequestSigns.InvokeLangUpdateForSignsOfKit(k.Name);
                }
                return true;
            }
            else return false;
        }
        public static IEnumerable<Kit> GetAccessibleKits(ulong playerID) => GetObjectsWhere(k => k.AllowedUsers.Contains(playerID));
        public static void GiveAccess(ulong playerID, string kitName)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        public static Kit ReadKitJson(ref Utf8JsonReader reader)
        {
            Kit kit = new Kit(true);
            kit.ReadJson(ref reader);
            return kit;
        }

        public static void WriteKitJson(Kit kit, Utf8JsonWriter writer) => kit.WriteJson(writer);
        public static Kit Construct(string name, List<KitItem> items, List<KitClothing> clothes, Action<Kit>? modifiers = null)
        {
            Kit kit = new Kit(true)
            {
                Name = name,
                Items = items,
                Clothes = clothes,
                Class = EClass.NONE,
                Branch = EBranch.DEFAULT,
                Team = 0,
                UnlockRequirements = new BaseUnlockRequirement[0],
                Skillsets = new Skillset[0],
                TicketCost = 1,
                IsPremium = false,
                PremiumCost = 0,
                IsLoadout = false,
                TeamLimit = 1,
                Cooldown = 0,
                AllowedUsers = new List<ulong>()
            };
            kit.SignTexts = new Dictionary<string, string> { { JSONMethods.DEFAULT_LANGUAGE, kit.DisplayName } };
            if (kit.Items == null || items.Count == 0)
                kit.Weapons = string.Empty;
            else
            {
                KitItem i = items.OrderByDescending(x => x.metadata == null ? 0 : x.metadata.Length).First();
                if (i == null) kit.Weapons = string.Empty;
                else if (Assets.find(i.id) is not ItemAsset asset) kit.Weapons = string.Empty;
                else kit.Weapons = asset.itemName;
            }
            modifiers?.Invoke(kit);
            return kit;
        }
        public static bool HasItemOfID(this Kit kit, Guid ID) => kit.Items.Exists(i => i.id == ID);
        public static bool IsLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
