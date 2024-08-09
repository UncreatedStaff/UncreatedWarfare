using System.Globalization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Signs;

[SignPrefix("kit_")]
[SignPrefix("loadout_")]
public class KitSignInstanceProvider : ISignInstanceProvider
{
    private readonly KitManager _kitManager;
    bool ISignInstanceProvider.CanBatchTranslate => false;
    public string KitId { get; private set; }
    public int LoadoutNumber { get; private set; }
    public KitSignInstanceProvider(KitManager kitManager)
    {
        _kitManager = kitManager;
        LoadoutNumber = -1;
        KitId = null!;
    }

    public void Initialize(BarricadeDrop barricade, string extraInfo)
    {
        if (((InteractableSign)barricade.interactable).text.StartsWith("loadout_"))
        {
            if (int.TryParse(extraInfo, NumberStyles.Number, CultureInfo.InvariantCulture, out int id))
            {
                LoadoutNumber = id;
            }
            else
            {
                LoadoutNumber = -1;
            }
        }
        else
        {
            KitId = extraInfo;
            LoadoutNumber = -1;
        }
    }

    public string Translate(LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        if (LoadoutNumber != -1)
        {
            return Localization.TranslateLoadoutSign(LoadoutNumber, player!);
        }

        if (!_kitManager.Cache.KitDataById.TryGetValue(KitId, out Kit kit))
        {
            return KitId;
        }

        return Localization.TranslateKitSign(kit, player!);
    }
}