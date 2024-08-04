using System.Globalization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Signs;

public class KitSignInstanceProvider : ISignInstanceProvider
{
    private readonly KitManager _kitManager;
    private string _kitId = null!;
    bool ISignInstanceProvider.CanBatchTranslate => false;
    public KitSignInstanceProvider(KitManager kitManager)
    {
        _kitManager = kitManager;
    }

    public void Initialize(BarricadeDrop barricade, string extraInfo)
    {
        _kitId = extraInfo;
    }

    public string Translate(LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        if (!_kitManager.Cache.KitDataById.TryGetValue(_kitId, out Kit kit))
        {
            return _kitId;
        }

        return Localization.TranslateKitSign(kit, player!);
    }
}