using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Signs;

[SignPrefix("kit_")]
[SignPrefix("loadout_")]
public class KitSignInstanceProvider : ISignInstanceProvider, IRequestable<Kit>
{
    private readonly KitRequestService _kitRequestService;
    private readonly IKitDataStore _kitDataStore;
    private readonly PlayerNitroBoostService _nitroBoostService;
    private readonly IConfiguration _systemConfig;
    private readonly KitSignTranslations _translations;
    private static readonly StringBuilder KitSignBuffer = new StringBuilder(230);

    private static readonly Color32 ColorKitFavoritedName = new Color32(255, 255, 153, 255);
    private static readonly Color32 ColorKitUnfavoritedName = new Color32(255, 255, 255, 255);

    /// <inheritdoc />
    bool ISignInstanceProvider.CanBatchTranslate => false;

    /// <inheritdoc />
    string ISignInstanceProvider.FallbackText => KitId ?? ("Loadout " + LoadoutNumber.ToString(CultureInfo.InvariantCulture));
    public string KitId { get; private set; }
    public int LoadoutNumber { get; private set; }
    public KitSignInstanceProvider(
        KitRequestService kitRequestService,
        IKitDataStore kitDataStore,
        TranslationInjection<KitSignTranslations> translations,
        PlayerNitroBoostService nitroBoostService,
        IConfiguration systemConfig)
    {
        _kitRequestService = kitRequestService;
        _kitDataStore = kitDataStore;
        _nitroBoostService = nitroBoostService;
        _systemConfig = systemConfig;
        _translations = translations.Value;
        LoadoutNumber = -1;
        KitId = null!;
    }

    public void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        if (((InteractableSign)barricade.interactable).text.StartsWith("loadout_"))
        {
            if (int.TryParse(extraInfo, NumberStyles.Number, CultureInfo.InvariantCulture, out int id) && id >= 0)
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

    public string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        // reuse the same string builder since this'll be called a lot

        if (LoadoutNumber >= 0)
        {
            if (LoadoutNumber == 0)
                return "<color=#ff9933>Invalid Loadout ID</color>\n<color=#66ccff>0</color>";

            try
            {
                TranslateLoadoutSign(KitSignBuffer, LoadoutNumber, language, culture, player);
                return KitSignBuffer.ToString();
            }
            finally
            {
                KitSignBuffer.Clear();
            }
        }

        if (!_kitDataStore.CachedKitsById.TryGetValue(KitId, out Kit kit))
        {
            return $"<color=#ff9933>Invalid Kit</color>\n<color=#66ccff>{KitId}</color>";
        }

        try
        {
            TranslateKitSign(KitSignBuffer, kit, language, culture, player);
            return KitSignBuffer.ToString();
        }
        finally
        {
            KitSignBuffer.Clear();
        }
    }

    /*
     *  - KIT NAME WITH A
     *     MAYBE NEWLINE  or OPEN LINE IF NOT NEEDED
     *  * EMPTY LINE IF NO WEAPON TEXT
     *  - COST
     *  - WEAPON TEXT [optional]
     *  - PLAYER COUNT
     */

    private void TranslateKitSign(StringBuilder bldr, Kit kit, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        string kitName = kit.GetDisplayName(language, true);

        // if the name has a newline we want to skip the empty line so all the text is roughly the same size
        bool nameHasNewLine = kitName.IndexOf('\n', StringComparison.Ordinal) >= 0;

        bool isFavorited = player != null && player.Component<KitPlayerComponent>().IsKitFavorited(kit.Key);

        bldr.Append("<b>")
            .AppendColorized(kitName.ToUpper(culture), isFavorited ? ColorKitFavoritedName : ColorKitUnfavoritedName)
            .Append('\n');

        if (!nameHasNewLine)
            bldr.Append('\n');

        bool hasWeaponText = string.IsNullOrWhiteSpace(kit.WeaponText);

        if (!hasWeaponText)
            bldr.Append('\n');

        AppendCost(bldr, kit, language, culture, player);
        bldr.Append('\n');

        if (hasWeaponText)
            bldr.Append(kit.WeaponText!.ToUpper(culture)).Append('\n');

        AppendPlayerCount(bldr, player, kit, language, culture, useClassLimit: false);
    }

    private void AppendPlayerCount(StringBuilder bldr, WarfarePlayer? player, Kit kit, LanguageInfo language, CultureInfo culture, bool useClassLimit)
    {
        if (player == null || kit.TeamLimit is >= 1f or <= 0f)
        {
            bldr.Append(_translations.KitUnlimited.Translate(language));
        }
        else if (useClassLimit
                     ? _kitRequestService.IsClassLimited(kit, out int currentPlayers, out int allowedPlayers, player.Team)
                     : _kitRequestService.IsLimited(kit, out currentPlayers, out allowedPlayers, player.Team)
                )
        {
            bldr.Append(_translations.KitPlayerCountUnavailable.Translate(currentPlayers, allowedPlayers, language, culture, TimeZoneInfo.Utc));
        }
        else
        {
            bldr.Append(_translations.KitPlayerCountAvailable.Translate(currentPlayers, allowedPlayers, language, culture, TimeZoneInfo.Utc));
        }
    }

    private void AppendCost(StringBuilder bldr, Kit kit, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        string cost;
        if (kit.RequiresServerBoost)
        {
            if (player != null && _nitroBoostService.IsBoostingQuick(player.Steam64) is true)
            {
                cost = _translations.KitNitroBoostOwned.Translate(language);
            }
            else
            {
                bldr.Append(_translations.KitNitroBoostNotOwned.Translate(language));
                return;
            }
        }
        else if (kit.Type != KitType.Public)
        {
            if (player != null && player.Component<KitPlayerComponent>().IsKitAccessible(kit.Key))
            {
                cost = _translations.KitPremiumOwned.Translate(language);
            }
            else if (kit.Type == KitType.Special)
            {
                bldr.Append(_translations.KitExclusive.Translate(language));
                return;
            }
            else
            {
                bldr.Append(_translations.KitPremiumCost.Translate(decimal.Round(kit.PremiumCost, 2), language, culture, TimeZoneInfo.Utc));
                return;
            }
        }
        else
        {
            if (kit.CreditCost <= 0)
            {
                cost = _translations.KitFree.Translate(language);
            }
            else if (player != null && player.Component<KitPlayerComponent>().IsKitAccessible(kit.Key))
            {
                cost = _translations.KitPublicOwned.Translate(language);
            }
            else
            {
                bldr.Append(_translations.KitCreditCost.Translate(kit.CreditCost, language, culture, TimeZoneInfo.Utc));
                return;
            }
        }

        if (kit.UnlockRequirements is { Length: > 0 })
        {
            foreach (UnlockRequirement req in kit.UnlockRequirements)
            {
                if (player != null && req.CanAccessFast(player))
                    continue;

                bldr.Append(req.GetSignText(player, language, culture));
                return;
            }
        }

        bldr.Append(cost);
    }

    private void TranslateLoadoutSign(StringBuilder bldr, int loadoutIndex, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        KitPlayerComponent? kitPlayerComponent = player?.Component<KitPlayerComponent>();

        Kit? kit = kitPlayerComponent?.Loadouts.ElementAtOrDefault(loadoutIndex - 1);

        if (kit == null)
        {
            bldr.Append(_translations.LoadoutNumber.Translate(loadoutIndex, language, culture, TimeZoneInfo.Utc));
            bldr.Append('\n', 4);
            bldr.Append(_translations.KitPremiumCost.Translate(_systemConfig.GetValue<decimal>("kits:loadout_cost_usd")));
            return;
        }

        string kitName = kit.GetDisplayName(language, true, removeNewLine: false);

        // if the name has a newline we want to skip the empty line so all the text is roughly the same size
        bool nameHasNewLine = kitName.IndexOf('\n', StringComparison.Ordinal) >= 0;

        bool isFavorited = player != null && kitPlayerComponent!.IsKitFavorited(kit.Key);

        bldr.Append("<b>")
            .AppendColorized(kitName.ToUpper(culture), isFavorited ? ColorKitFavoritedName : ColorKitUnfavoritedName)
            .Append("</b>")
            .Append('\n');

        bool needsUpgrade = kit.Season < WarfareModule.Season;

        // subtitle: /req upgrade, PENDING SETUP, or WEAPON TEXT
        bool hasSubtitle = needsUpgrade || kit.IsLocked || !string.IsNullOrWhiteSpace(kit.WeaponText);

        if (!hasSubtitle)
            bldr.Append('\n');

        if (!nameHasNewLine)
            bldr.Append('\n');

        string loadoutLetter = LoadoutIdHelper.GetLoadoutLetter(LoadoutIdHelper.ParseNumber(kit.Id));
        bldr.Append(_translations.LoadoutLetter.Translate(loadoutLetter, language, culture, TimeZoneInfo.Utc))
            .Append('\n');

        if (kit.IsLocked || needsUpgrade)
            bldr.Append('\n');

        if (hasSubtitle)
        {
            if (kit.IsLocked)
                bldr.Append(_translations.KitLoadoutSetup.Translate(language)).Append('\n');
            else if (needsUpgrade)
                bldr.Append(_translations.KitLoadoutUpgrade.Translate(language)).Append('\n');
            else
                bldr.Append(kit.WeaponText!.ToUpper(culture)).Append('\n');
        }

        if (!kit.IsLocked && !needsUpgrade)
            AppendPlayerCount(bldr, player, kit, language, culture, useClassLimit: true);
    }
}

public class KitSignTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Kit Signs";

    [TranslationData("Shown on a kit sign when a kit is available without purchase (in-game or monetary).")]
    public readonly Translation KitFree = new Translation("<#66ffcc>FREE</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when a kit is unable to be given access through normal means. Usually this is for event kits.")]
    public readonly Translation KitExclusive = new Translation("<#96ffb2>EXCLUSIVE</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when a kit requires nitro boosting and the person looking at the sign is boosting.")]
    public readonly Translation KitNitroBoostOwned = new Translation("<#f66fe6>BOOSTING</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when a kit requires nitro boosting and the person looking at the sign is not boosting.")]
    public readonly Translation KitNitroBoostNotOwned = new Translation("<#9b59b6>NITRO BOOST</color>", TranslationOptions.TMProSign);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<decimal> KitPremiumCost = new Translation<decimal>("<#7878ff>$ {0}</color>", TranslationOptions.TMProSign, arg0Fmt: "N2");

    [TranslationData("Shown on a kit sign when the player isn't high enough rank to access it.", Parameters = [ "Rank", "Color depending on player's current rank." ])]
    public readonly Translation<WarfareRank, Color> KitRequiredRank = new Translation<WarfareRank, Color>("<#{1}>Rank: {0}</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when the player needs to complete a quest to access it.", Parameters = [ "Quest", "Color depending on whether the player has completed the quest." ])]
    public readonly Translation<QuestAsset, Color> KitRequiredQuest = new Translation<QuestAsset, Color>("<#{1}>Quest: <#fff>{0}</color></color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when the player needs to complete multiple quests to access it.", Parameters = [ "Number of quests needed.", "Color depending on whether the player has completed the quest(s).", "s if {0} != 1" ])]
    public readonly Translation<int, Color> KitRequiredQuestsMultiple = new Translation<int, Color>("<#{1}>Finish <#fff>{0}</color> quests.</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when the player has completed all required quests to unlock the kit.")]
    public readonly Translation KitRequiredQuestsComplete = new Translation("<#ff974d>UNLOCKED</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when the player has purchased the kit (with credits).")]
    public readonly Translation KitPublicOwned = new Translation("<#769fb5>UNLOCKED</color>", TranslationOptions.TMProSign);
    
    [TranslationData("Shown on a kit sign when the player has purchased the kit (with real money).")]
    public readonly Translation KitPremiumOwned = new Translation("<#769fb5>PURCHASED</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when the player has not purchased the kit with credits.", IsPriorityTranslation = false)]
    public readonly Translation<int> KitCreditCost = new Translation<int>("<#b8ffc1>C</color> <#fff>{0}</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when there is no limit to how many other players can be using the kit.")]
    public readonly Translation KitUnlimited = new Translation("<#111111>unlimited</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on an unused loadout sign.", "The number of the loadout sign.")]
    public readonly Translation<int> LoadoutNumber = new Translation<int>("<b><#7878ff>LOADOUT #{0}</color></b>", TranslationOptions.TMProSign);
    
    [TranslationData("Shown on a used loadout sign so players can see what loadout letter each kit is.", "The letter of the loadout sign.")]
    public readonly Translation<string> LoadoutLetter = new Translation<string>("<sub><#7878ff>LOADOUT {0}</color></sub>", TranslationOptions.TMProSign, arg0Fmt: UppercaseAddon.Instance);
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<int, int> KitPlayerCountUnavailable = new Translation<int, int>("<#c2603e>{0}/{1}</color>", TranslationOptions.TMProSign);
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<int, int> KitPlayerCountAvailable = new Translation<int, int>("<#96ffb2>{0}/{1}</color>", TranslationOptions.TMProSign);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation KitLoadoutUpgrade = new Translation("<#33cc33>/req upgrade</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a loadout or kit sign when it's locked by the admins while it's being worked on.")]
    public readonly Translation KitLoadoutSetup = new Translation("<#3399ff>PENDING SETUP</color>", TranslationOptions.TMProSign);
}