using System.Globalization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Signs;

[SignPrefix("loadout_")]
public class LoadoutSignInstanceProvider : ISignInstanceProvider
{
    private readonly KitManager _kitManager;
    private int _loadoutId = -1!;
    bool ISignInstanceProvider.CanBatchTranslate => false;
    public LoadoutSignInstanceProvider(KitManager kitManager)
    {
        _kitManager = kitManager;
    }

    public void Initialize(BarricadeDrop barricade, string extraInfo)
    {
        if (!int.TryParse(extraInfo, out _loadoutId))
            _loadoutId = -1;
    }

    public string Translate(LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        if (_loadoutId is < 0 or > byte.MaxValue)
        {
            return "Loadout";
        }

        return Localization.TranslateLoadoutSign((byte)_loadoutId, player!);
    }
}