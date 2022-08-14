using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Traits;

public static class TraitSigns
{
    public const string TRAIT_SIGN_PREFIX = Signs.PREFIX + Signs.TRAIT_PREFIX;
    internal static string TranslateTraitSign(TraitData trait, UCPlayer player)
    {
        string str = Localization.GetLang(player.Steam64);
        str = TranslateTraitSign(trait, str);
        return FormatTraitSign(trait, str, player);
    }
    public static unsafe string FormatTraitSign(TraitData trait, string tr2, UCPlayer player)
    {
        if (trait.UnlockRequirements is null || trait.UnlockRequirements.Length == 0)
            return tr2;
        for (int i = 0; i < trait.UnlockRequirements.Length; ++i)
        {
            BaseUnlockRequirement req = trait.UnlockRequirements[i];
            if (!req.CanAccess(player))
                return Signs.QuickFormat(tr2, req.GetSignText(player));
        }
        return Signs.QuickFormat(tr2, T.TraitSignUnlocked.Translate(player));
    }
    internal static string TranslateTraitSign(TraitData trait, string language)
    {
        bool keepline = false;
        string name = trait.NameTranslations.Translate(language);
        for (int i = 0; i < name.Length; ++i)
        {
            if (name[i] == '\n')
            {
                keepline = true;
                break;
            }
        }
        name = "<b>" + name.ToUpper().ColorizeTMPro(UCWarfare.GetColorHex("kit_public_header"), true) + "</b>";

        string cost = trait.CreditCost > 0 ? T.KitCreditCost.Translate(language, trait.CreditCost) : T.TraitSignFree.Translate(language);

        if (!keepline) cost = "\n" + cost;

        return
            name + "\n" +
            cost + "\n" +
            (trait.UnlockRequirements is null || trait.UnlockRequirements.Length == 0 ? "\n" : "{0}\n") +
            trait.DescriptionTranslations.Translate(language).ColorizeTMPro(UCWarfare.GetColorHex("trait_desc"));
    }
}
