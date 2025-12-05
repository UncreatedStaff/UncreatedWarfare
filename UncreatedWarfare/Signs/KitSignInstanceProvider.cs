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
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Signs;

[SignPrefix("kit_")]
[SignPrefix("loadout_")]
public class KitSignInstanceProvider : ISignInstanceProvider, IRequestable<Kit>, IKitRequirementVisitor<StringBuilder>
{
    private static readonly StringBuilder KitSignBuffer = new StringBuilder(230);

    private readonly IKitDataStore _kitDataStore;
    private readonly PlayerNitroBoostService _nitroBoostService;
    private readonly IConfiguration _systemConfig;
    private readonly KitSignTranslations _translations;
    private readonly TextMeasurementService _measurementService;
    private readonly KitRequirementManager _kitRequirements;
    private SignMetrics _signMetrics;

    private static readonly Color32 ColorKitFavoritedName = new Color32(255, 255, 153, 255);
    private static readonly Color32 ColorKitUnfavoritedName = new Color32(255, 255, 255, 255);

    /// <inheritdoc />
    bool ISignInstanceProvider.CanBatchTranslate => false;

    /// <inheritdoc />
    string ISignInstanceProvider.FallbackText => KitId ?? ("Loadout " + LoadoutNumber.ToString(CultureInfo.InvariantCulture));
    public string KitId { get; private set; }
    public int LoadoutNumber { get; private set; }
    public KitSignInstanceProvider(
        IKitDataStore kitDataStore,
        TranslationInjection<KitSignTranslations> translations,
        PlayerNitroBoostService nitroBoostService,
        IConfiguration systemConfig,
        TextMeasurementService measurementService,
        KitRequirementManager kitRequirements)
    {
        _kitDataStore = kitDataStore;
        _nitroBoostService = nitroBoostService;
        _systemConfig = systemConfig;
        _translations = translations.Value;
        _measurementService = measurementService;
        _kitRequirements = kitRequirements;
        LoadoutNumber = -1;
        KitId = null!;
    }

    public void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        _signMetrics = _measurementService.GetSignMetrics(barricade.asset.GUID);

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

        if (KitId == null)
        {
            return "<color=#ff9933>Invalid Kit</color>";
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
        string kitName = kit.GetDisplayName(language, true, removeNewLine: true);

        // if the name has a newline we want to skip the empty line so all the text is roughly the same size
        bool isFavorited = player != null && player.Component<KitPlayerComponent>().IsKitFavorited(kit.Key);

        bldr.Append("<b>");
        AppendName(kitName, isFavorited ? ColorKitFavoritedName : ColorKitUnfavoritedName, out bool nameHasNewLine);
        bldr.Append("</b>\n");

        // if the name has a newline we want to skip the empty line so all the text is roughly the same size
        if (!nameHasNewLine)
            bldr.Append('\n');

        bool hasWeaponText = !string.IsNullOrWhiteSpace(kit.WeaponText);

        if (!hasWeaponText)
            bldr.Append('\n');

        AppendCost(bldr, kit, language, culture, player);
        bldr.Append('\n');

        if (hasWeaponText)
            bldr.AppendColorized(kit.WeaponText!.ToUpper(culture), new Color32(255, 255, 255, 255)).Append('\n');

        AppendAvailability(bldr, player, kit, language, culture);
    }

    private void AppendAvailability(StringBuilder bldr, WarfarePlayer? player, Kit kit, LanguageInfo language, CultureInfo culture)
    {
        if (player == null)
            return;

        KitPlayerComponent kitPlayerComponent = player.Component<KitPlayerComponent>();
        if (kitPlayerComponent.ActiveKitId == kit.Id)
        {
            bldr.Append(_translations.KitCurrentlyUsing.Translate(language));
            return;
        }

        KitRequirementResolutionContext<StringBuilder> ctx = new KitRequirementResolutionContext<StringBuilder>(player, player.Team, kit, kitPlayerComponent.CachedKit, kitPlayerComponent, bldr);

        int l = bldr.Length;
        foreach (IKitRequirement requirement in _kitRequirements.Request)
        {
            if (requirement.AcceptCached(this, in ctx) == KitRequirementResult.No && bldr.Length > l)
                return;
        }

        bldr.Append(_translations.KitAvailable.Translate(language));
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

        bool isFavorited = player != null && kitPlayerComponent!.IsKitFavorited(kit.Key);

        bldr.Append("<b>");
        AppendName(kitName, isFavorited ? ColorKitFavoritedName : ColorKitUnfavoritedName, out bool nameHasNewLine);
        bldr.Append("</b>\n");

        bool needsUpgrade = kit.Season < WarfareModule.Season;

        // subtitle: /req upgrade, PENDING SETUP, or WEAPON TEXT
        bool hasSubtitle = needsUpgrade || kit.IsLocked || !string.IsNullOrWhiteSpace(kit.WeaponText);

        if (!hasSubtitle)
            bldr.Append('\n');

        // if the name has a newline we want to skip the empty line so all the text is roughly the same size
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
                bldr.AppendColorized(kit.WeaponText!.ToUpper(culture), new Color32(255, 255, 255, 255)).Append('\n');
        }

        if (!kit.IsLocked && !needsUpgrade)
            AppendAvailability(bldr, player, kit, language, culture);
    }

    private void AppendName(string kitName, Color32 color, out bool hasExtraLine)
    {
        Span<Range> outRanges = stackalloc Range[2]; // max 2 lines
        int nameSplits = _measurementService.SplitLines(kitName, 1.3f, _signMetrics, outRanges);

        KitSignBuffer.AppendColorized(ReadOnlySpan<char>.Empty, color, end: false);
        if (nameSplits > 1)
        {
            ReadOnlySpan<char> nameSpan = kitName.AsSpan();
            KitSignBuffer
                .Append(nameSpan[outRanges[0]])
                .Append('\n')
                .Append(nameSpan[outRanges[1]]);
            hasExtraLine = true;
        }
        else
        {
            KitSignBuffer.Append(kitName);
            hasExtraLine = false;
        }

        KitSignBuffer.Append("</color>");
    }


    void IKitRequirementVisitor<StringBuilder>.AcceptGenericRequirementNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, string message)
    {
        ctx.State.Append(message);
    }

    void IKitRequirementVisitor<StringBuilder>.AcceptRequiresSquadNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, bool needsSquadLead)
    {
        Squad? squad = ctx.Player.GetSquad();
        if (squad == null)
        {
            ctx.State.Append(_translations.KitRequiresJoinSquad.Translate(ctx.Player));
        }
        else if (needsSquadLead)
        {
            ctx.State.Append(_translations.KitRequiresSquadLeader.Translate(ctx.Player));
        }
    }

    void IKitRequirementVisitor<StringBuilder>.AcceptMinRequiredSquadMembersNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, WarfarePlayer? playerTakingKit,
        int squadMemberCount, int minimumSquadMembers)
    {
        ctx.State.Append(playerTakingKit != null
            ? _translations.KitAlreadyTakenBySquadMember.Translate(ctx.Player)
            : _translations.KitNotEnoughPlayersInSquad.Translate(squadMemberCount, minimumSquadMembers, ctx.Player)
        );
    }

    void IKitRequirementVisitor<StringBuilder>.AcceptClassesAllowedPerXTeammatesRequirementNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, int allowedPerXUsers,
        int currentUsers, int teammates, int kitsAllowed)
    {
        ctx.State.Append(_translations.KitTeamClassLimitReached.Translate(currentUsers, kitsAllowed, ctx.Player));
    }

    void IKitRequirementVisitor<StringBuilder>.AcceptPremiumCostNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, decimal cost) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptCreditCostNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, double cost, double current) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptExclusiveKitNotMet(in KitRequirementResolutionContext<StringBuilder> ctx) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptLoadoutLockedNotMet(in KitRequirementResolutionContext<StringBuilder> ctx) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptLoadoutOutOfDateNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, int season) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptDisabledNotMet(in KitRequirementResolutionContext<StringBuilder> ctx) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptNitroBoostRequirementNotMet(in KitRequirementResolutionContext<StringBuilder> ctx) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptMapFilterNotMet(in KitRequirementResolutionContext<StringBuilder> ctx) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptFactionFilterNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, FactionInfo faction) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptGlobalCooldownNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, in Cooldown requestCooldown) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptPremiumCooldownNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, in Cooldown requestCooldown) { }
    void IKitRequirementVisitor<StringBuilder>.AcceptKitSpecificUnlockRequirementNotMet(in KitRequirementResolutionContext<StringBuilder> ctx, UnlockRequirement requirement) { }
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

    [TranslationData("Shown on an unused loadout sign.", "The number of the loadout sign.")]
    public readonly Translation<int> LoadoutNumber = new Translation<int>("<b><#7878ff>LOADOUT #{0}</color></b>", TranslationOptions.TMProSign);
    
    [TranslationData("Shown on a used loadout sign so players can see what loadout letter each kit is.", "The letter of the loadout sign.")]
    public readonly Translation<string> LoadoutLetter = new Translation<string>("<sub><#7878ff>LOADOUT {0}</color></sub>", TranslationOptions.TMProSign, arg0Fmt: UppercaseAddon.Instance);
    
    [TranslationData("Shown on a kit sign when the player is already using this kit.")]
    public readonly Translation KitCurrentlyUsing = new Translation("<#827d6d>Selected</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when the kit can only be requested by a squad leader and the player isn't one.")]
    public readonly Translation KitRequiresSquadLeader = new Translation("<#a69870>Squad Leader only</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when the kit can only be requested by players in a squad and the player isn't one.")]
    public readonly Translation KitRequiresJoinSquad = new Translation("<#a69870>Join a Squad</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when there are already too many people using this kit class in the player's squad.")]
    public readonly Translation KitAlreadyTakenBySquadMember = new Translation("<#c2603e>Taken</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when there needs to be a certain amount of players in their squad to use the kit.")]
    public readonly Translation<int, int> KitNotEnoughPlayersInSquad = new Translation<int, int>("<#c2846e>Squad: {0}/{1}</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when there are too many players in the player's squad using this kit type.")]
    public readonly Translation<int, int> KitTeamClassLimitReached = new Translation<int, int>("<#c2846e>{0}/{1} on team</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a kit sign when a kit can be requested.")]
    public readonly Translation KitAvailable = new Translation("<#96ffb2>Available</color>", TranslationOptions.TMProSign);
    
    [TranslationData("Shown on a kit sign when there is no limit to how many other players can be using the kit.")]
    public readonly Translation KitAvailableUnlimited = new Translation("<#111111>Available</color>", TranslationOptions.TMProSign);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation KitLoadoutUpgrade = new Translation("<#33cc33>/req upgrade</color>", TranslationOptions.TMProSign);

    [TranslationData("Shown on a loadout or kit sign when it's locked by the admins while it's being worked on.")]
    public readonly Translation KitLoadoutSetup = new Translation("<#3399ff>PENDING SETUP</color>", TranslationOptions.TMProSign);
}