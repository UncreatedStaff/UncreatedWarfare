using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Items;

public sealed class KitWeaponTextService : BaseAlternateConfigurationFile
{
    private readonly List<IAssetLink<ItemGunAsset>> _blacklist;

    public KitWeaponTextService() : base(Path.Combine("Kits", "Ignored Weapons.yml"))
    {
        _blacklist = new List<IAssetLink<ItemGunAsset>>(2);
        HandleChange();
    }

    public bool IsBlacklisted(IAssetLink<Asset> asset)
    {
        lock (_blacklist)
        {
            return _blacklist.ContainsAsset(asset);
        }
    }

    protected override void HandleChange()
    {
        lock (_blacklist)
        {
            _blacklist.Clear();

            foreach (IConfigurationSection section in UnderlyingConfiguration.GetSection("Weapons").GetChildren())
            {
                IAssetLink<ItemGunAsset> gun = section.GetAssetLink<ItemGunAsset>();
                if (gun.Exists)
                    _blacklist.Add(gun);
            }
        }
    }

    public string GetWeaponText(IReadOnlyCollection<IItem> items)
    {
        SortedList<Page, ItemAsset> guns = new SortedList<Page, ItemAsset>(4);
        lock (_blacklist)
        {
            foreach (IItem item in items)
            {
                // don't worry about clothes or redirected items
                if (item is not (IConcreteItem concrete and IPageItem page))
                    continue;

                if (!concrete.Item.TryGetAsset(out ItemAsset? asset) || asset.type != EItemType.GUN)
                    continue;

                if (_blacklist.ContainsAsset(asset) || guns.ContainsValue(asset))
                    continue;

                // only one gun per page
                guns.TryAdd(page.Page, asset);
            }
        }

        string[] itemNames = new string[guns.Count];
        int index = -1;
        foreach (ItemAsset gun in guns.Values)
        {
            itemNames[++index] = gun.itemName ?? gun.name;
        }

        return string.Join(", ", itemNames);
    }
}