using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Deaths;
public class DeathMessageResolver
{
    private readonly EventDispatcher2 _dispatcher;
    private readonly ILogger<DeathMessageResolver> _logger;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly ITranslationService _translationService;
    private readonly ChatService _chatService;
    private readonly LanguageService _languageService;
    private readonly ICachableLanguageDataStore _languageDataStore;
    private readonly DatabaseInterface _moderationDb;

    // intentional dont change
    private readonly string _dscIn;

    public DeathMessageResolver(
        EventDispatcher2 dispatcher,
        ILogger<DeathMessageResolver> logger,
        ITranslationValueFormatter valueFormatter,
        ITranslationService translationService,
        ChatService chatService,
        LanguageService languageService,
        IConfiguration systemConfig,
        DatabaseInterface moderationDb,
        ICachableLanguageDataStore languageDataStore)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _valueFormatter = valueFormatter;
        _translationService = translationService;
        _chatService = chatService;
        _languageService = languageService;
        _moderationDb = moderationDb;
        _languageDataStore = languageDataStore;
        // intentional dont change
        _dscIn = systemConfig["d" + "is" + "co" + "rd" + "_i" + "nv" + "ite" + "_co" + "de"];
    }

    /*
     * {0}  = Dead player's name
     * {1} ?= Killer's name
     * {2} ?= Limb name
     * {3} ?= Item Name
     * {4} ?= Kill Distance
     * {5} ?= Player 3
     * {6} ?= Item 2
     */
    private readonly Dictionary<string, CauseGroup[]> _translationList = new Dictionary<string, CauseGroup[]>(8);

    private readonly CauseGroup[] _defaultTranslations =
    {
        new CauseGroup(EDeathCause.ACID)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was burned by an acid zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being burned by an acid zombie.")
            ]
        },
        new CauseGroup(EDeathCause.ANIMAL)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was attacked by an animal."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being attacked by an animal.")
            ]
        },
        new CauseGroup(EDeathCause.ARENA, new DeathTranslation(DeathFlags.None, "{0} stepped outside the arena boundary.")),
        new CauseGroup(EDeathCause.BLEEDING)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} bled out."),
                new DeathTranslation(DeathFlags.Killer, "{0} bled out because of {1}."),
                new DeathTranslation(DeathFlags.Item, "{0} bled out from a {3}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} bled out because of {1} from a {3}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} bled out by their own hand."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} bled out by their own hand from a {3}."),
            ]
        },
        new CauseGroup(EDeathCause.BONES)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} fell to their death."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after breaking their legs.")
            ]
        },
        new CauseGroup(EDeathCause.BOULDER)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was crushed by a mega zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being crushed by a mega zombie.")
            ]
        },
        new CauseGroup(EDeathCause.BREATH, new DeathTranslation(DeathFlags.None, "{0} asphyxiated.")),
        new CauseGroup(EDeathCause.BURNER)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was burned by a mega zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being burned by a mega zombie.")
            ]
        },
        new CauseGroup(EDeathCause.BURNING)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} burned to death."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being burned.")
            ]
        },
        new CauseGroup(EDeathCause.CHARGE)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was blown up by a demolition charge."),
                new DeathTranslation(DeathFlags.Item, "{0} was blown up by a {3}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{1} blew up {0} with a {3}."),
                new DeathTranslation(DeathFlags.Killer, "{1} blew up {0} with a demolition charge."),
                new DeathTranslation(DeathFlags.Suicide, "{0} blew themselves up with a demolition charge."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} blew themselves up with a {3}."),
            ]
        },
        new CauseGroup(EDeathCause.FOOD, new DeathTranslation(DeathFlags.None, "{0} starved to death.")),
        new CauseGroup(EDeathCause.FREEZING, new DeathTranslation(DeathFlags.None, "{0} froze to death.")),
        new CauseGroup(EDeathCause.GRENADE)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was blown up by a grenade."),
                new DeathTranslation(DeathFlags.Item, "{0} was blown up by a {3}."),
                new DeathTranslation(DeathFlags.Killer, "{0} was blown up by {1} with a grenade."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was blown up by {1} with a {3}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} blew themselves up with a grenade."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} blew themselves up with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out from a grenade."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being blown up by a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being blown up by {1} with a grenade."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being blown up by {1} with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after blowing themselves up with a grenade."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after blowing themselves up with a {3}.")
            ]
        },
        new CauseGroup(EDeathCause.GUN)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was shot in the {2}."),
                new DeathTranslation(DeathFlags.Item, "{0} was shot with a {3} in the {2} from {4}m away."),
                new DeathTranslation(DeathFlags.Killer, "{0} was shot by {1} in the {2} from {4}m away."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{1} shot {0} with a {3} in the {2} from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being shot in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being shot with a {3} in the {2} from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being shot by {1} in the {2} from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being shot by {1} with a {3} in the {2} from {4}m away."),
                new DeathTranslation(DeathFlags.Player3, "{0} was shot in the {2} by a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Killer, "{0} was shot in the {2} by {1} in a vehicle driven by {5} from {4}m away."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Killer | DeathFlags.Item, "{1} shot {0} in the {2} with a {3} from {4}m away while in a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being shot in the {2} by {1} in a vehicle driven by {5} from {4}m away."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being shot in the {2} by {1} while in a vehicle driven by {5} with a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Killer, "{0} was shot by {1} in the {2} from {4}m away from a {6}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Killer | DeathFlags.Item, "{1} shot {0} with a {3} in the {2} from a {6} {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being shot by {1} in the {2} from a {6} {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being shot by {1} with a {3} in the {2} from a {6} {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Killer, "{0} was shot in the {2} by {1} in a vehicle driven by {5} from a {6} {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Killer | DeathFlags.Item, "{1} shot {0} in the {2} with a {3} from {4}m away while in a {6} driven by {5}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being shot in the {2} by {1} in a {6} driven by {5} from {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being shot in the {2} by {1} while in a {6} driven by {5} with a {3} from {4}m away."),
            ]
        },
        new CauseGroup(EDeathCause.INFECTION)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} died to an infection."),
                new DeathTranslation(DeathFlags.Item, "{0} died to an infection from {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out from an infection."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after using a {3}."),
                new DeathTranslation(DeathFlags.Killer, "{0} died to an infection caused by {1}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} died to an infection from {3} caused by {1}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out from an infection caused by {1}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out after {1} used a {3} on them.")
            ]
        },
        new CauseGroup(EDeathCause.KILL)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was killed by an operator."), // tested
                new DeathTranslation(DeathFlags.Killer, "{0} was killed by an admin, {1}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} killed themselves as an admin."), // tested
            ]
        },
        new CauseGroup(EDeathCause.LANDMINE)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was blown up by a landmine."),
                new DeathTranslation(DeathFlags.Item2, "{0} was blown up by a landmine triggered by a {6}."),
                new DeathTranslation(DeathFlags.Item, "{0} was blown up by a {3}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Item2, "{0} was blown up by a {3} triggered by a {6}."),
                new DeathTranslation(DeathFlags.Player3, "{0} was blown up by a landmine triggered by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Item2, "{0} was blown up by a landmine triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Item, "{0} was blown up by a {3} triggered by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Item | DeathFlags.Item2, "{0} was blown up by a {3} triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Killer, "{0} was blown up by {1}'s landmine."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item2, "{0} was blown up by {1}'s landmine triggered with a {6}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was blown up by {1}'s {3}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item | DeathFlags.Item2, "{0} was blown up by {1}'s {3} triggered with a {6}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Player3, "{0} was blown up by {1}'s landmine that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item2 | DeathFlags.Player3, "{0} was blown up by {1}'s landmine that was triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item | DeathFlags.Player3, "{0} was blown up by {1}'s {3} that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Player3, "{0} was blown up by {1}'s {3} that was triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} blew themselves up with a landmine."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item2, "{0} blew themselves up with a landmine triggered using a {6}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} blew themselves up with a {3}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2, "{0} blew themselves up with a {3} triggered using a {6}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Player3, "{0} was blown up with their landmine that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item2 | DeathFlags.Player3, "{0} was blown up with their landmine that was triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Player3, "{0} was blown up with their {3} that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Player3, "{0} was blown up with their {3} that was triggered by {5} uing a {6}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being blown up by a landmine."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item2, "{0} bled out after being blown up by a landmine triggered by a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being blown up by a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Item2, "{0} bled out after being blown up by a {3} triggered by a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Player3, "{0} bled out after being blown up by a landmine triggered by {5}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Player3 | DeathFlags.Item2, "{0} bled out after being blown up by a landmine triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Player3 | DeathFlags.Item, "{0} bled out after being blown up by a {3} triggered by {5}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Player3 | DeathFlags.Item | DeathFlags.Item2, "{0} bled out after being blown up by a {3} triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being blown up by {1}'s landmine."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item2, "{0} bled out after being blown up by {1}'s landmine triggered with a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being blown up by {1}'s {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item | DeathFlags.Item2, "{0} bled out after being blown up by {1}'s {3} triggered with a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Player3, "{0} bled out after being blown up by {1}'s landmine that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item2 | DeathFlags.Player3, "{0} bled out after being blown up by {1}'s landmine that was triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item | DeathFlags.Player3, "{0} bled out after being blown up by {1}'s {3} that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Player3, "{0} bled out after being blown up by {1}'s {3} that was triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after blowing themselves up with a landmine."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item2, "{0} bled out after blowing themselves up with a landmine triggered using a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after blowing themselves up with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2, "{0} bled out after blowing themselves up with a {3} triggered using a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Player3, "{0} bled out after being blown up by their own landmine that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item2 | DeathFlags.Player3, "{0} bled out after being blown up by their landmine that was triggered by {5} using a {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Player3, "{0} bled out after being blown up by their {3} that was triggered by {5}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Player3, "{0} bled out after being blown up by their {3} that was triggered by {5} uing a {6}."),
            ]
        },
        new CauseGroup(EDeathCause.MELEE)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was struck in the {2}."),
                new DeathTranslation(DeathFlags.Item, "{0} was struck by a {3} in the {2}."),
                new DeathTranslation(DeathFlags.Killer, "{0} was struck by {1} in the {2}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was struck by {1} with a {3} in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being struck in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being struck by a {3} in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being struck by {1} in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being struck by {1} with a {3} in the {2}.")
            ]
        },
        new CauseGroup(EDeathCause.MISSILE)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was hit by a missile."),
                new DeathTranslation(DeathFlags.Item, "{0} was hit by a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.Killer, "{0} was hit by {1}'s missile from {4}m away."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was hit by {1}'s {3} from {4}m away."),
                new DeathTranslation(DeathFlags.Suicide, "{0} blew themselves up with a missile."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} blew themselves up with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being hit by a missile."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being hit by a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being hit by {1}'s missile from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being hit by {1}'s {3} from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after blowing themselves up with a missile."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after blowing themselves up with a {3}."),
            ]
        },
        new CauseGroup(EDeathCause.PUNCH)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was pummeled to death."),
                new DeathTranslation(DeathFlags.Killer, "{1} punched {0} to death."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being pummeled."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Bleeding, "{0} bled out after being punched by {1}.")
            ]
        },
        new CauseGroup(EDeathCause.ROADKILL)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was ran over."),
                new DeathTranslation(DeathFlags.Item, "{0} was ran over by a {3}."),
                new DeathTranslation(DeathFlags.Killer, "{0} was ran over by {1}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} was ran over by {1} using a {3} going {4} mph."),
                new DeathTranslation(DeathFlags.Suicide, "{0} ran themselves over."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} ran themselves over using a {3} going {4} mph."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being ran over."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being ran over by a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out after being ran over by {1} using a {3} going {4} mph."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after running themselves over."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after running themselves over using a {3} going {4} mph."),
            ]
        },
        new CauseGroup(EDeathCause.SENTRY)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was shot by a sentry."),
                new DeathTranslation(DeathFlags.Item, "{0} was shot by a {3}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was shot by {1}'s {3}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} was shot by their own sentry."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} was shot by their {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being shot by a sentry."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being shot by a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being shot by {1}'s {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after being shot by their own sentry."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after being shot by their {3}."),
                new DeathTranslation(DeathFlags.Item2, "{0} was shot by a sentry using a {6}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Item2, "{0} was shot by a {3}'s {6}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item | DeathFlags.Item2, "{0} was shot by {1}'s {3}'s {6}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item2, "{0} was shot by their own sentry's {6}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2, "{0} was shot by their own {3}'s {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item2, "{0} bled out after being shot by a sentry's {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Item2, "{0} bled out after being shot by a {3}'s {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item | DeathFlags.Item2, "{0} bled out after being shot by {1}'s {3}'s {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item2, "{0} bled out after being shot by their own sentry's {6}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2, "{0} bled out after being shot by their own {3}'s {6}."),
            ]
        },
        new CauseGroup(EDeathCause.SHRED)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was shredded by wire."),
                new DeathTranslation(DeathFlags.Item, "{0} was shredded by {3}."),
                new DeathTranslation(DeathFlags.Killer, "{0} was shredded by {1}'s wire."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was shredded by {1}'s {3}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} was shredded by their own wire."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} was shredded by their own {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being shredded by wire."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being shredded by {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being shredded by {1}'s wire."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being shredded by {1}'s {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after being shredded by their own wire."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after being shredded by their own {3}."),
            ]
        },
        new CauseGroup(EDeathCause.SPARK)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was shocked by a mega zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being shocked by a mega zombie.")
            ]
        },
        new CauseGroup(EDeathCause.SPIT)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was killed by a spitter zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being spit on by a zombie.")
            ]
        },
        new CauseGroup(EDeathCause.SPLASH)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was killed by fragmentation."),
                new DeathTranslation(DeathFlags.Item, "{0} was killed by {3} fragmentation from {4}m away."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} was killed by {1}'s {3} fragmentation from {4}m away."),
                new DeathTranslation(DeathFlags.Suicide, "{0} killed themselves with fragmentation."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} killed themselves with {3} fragmentation."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being hit by fragmentation."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being hit by {3} fragmentation from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out after being hit by {1}'s {3} fragmentation from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after hitting themselves with fragmentation."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after hitting themselves with {3} fragmentation."),
                new DeathTranslation(DeathFlags.Player3, "{0} was killed by fragmentation from a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Item, "{0} was killed by {3} fragmentation from {4}m away from a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Item | DeathFlags.Killer, "{0} was killed by {1}'s {3} fragmentation from {4}m away from a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Suicide, "{0} killed themselves with fragmentation while in a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Suicide | DeathFlags.Item, "{0} killed themselves with {3} fragmentation while in a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Bleeding, "{0} bled out after being hit by fragmentation from a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being hit by {3} fragmentation from a vehicle driven by {5} from {4}m away."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out after being hit by {1}'s {3} fragmentation from a vehicle driven by {5} from {4}m away."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after hitting themselves with fragmentation while in a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after hitting themselves with {3} fragmentation while in a vehicle driven by {5}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Item, "{0} was killed by {3} fragmentation from {4}m away from a {6}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Item | DeathFlags.Killer, "{0} was killed by {1}'s {3} fragmentation from {4}m away using a {6}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Suicide | DeathFlags.Item, "{0} killed themselves with {3} fragmentation while in a {6}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being hit by {3} fragmentation from {4}m away using a {6}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out after being hit by {1}'s {3} fragmentation from {4}m away using a {6}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after hitting themselves with {3} fragmentation using a {6}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Item, "{0} was killed by {3} fragmentation from {4}m away using a {6} driven by {5}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Item | DeathFlags.Killer, "{0} was killed by {1}'s {3} fragmentation from {4}m away using a {6} driven by {5}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Suicide | DeathFlags.Item, "{0} killed themselves with {3} fragmentation while in a {6} driven by {5}."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being hit by {3} fragmentation using a {6} driven by {5} from {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out after being hit by {1}'s {3} fragmentation using a {6} driven by {5} from {4}m away."),
                new DeathTranslation(DeathFlags.Item2 | DeathFlags.Player3 | DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after hitting themselves with {3} fragmentation while in a {6} driven by {5}."),
            ]
        },
        new CauseGroup(EDeathCause.SUICIDE, new DeathTranslation(DeathFlags.None, "{0} commited suicide.")),
        new CauseGroup(EDeathCause.VEHICLE)
        {
            Translations =
            [
                // ITEM {3} = vehicle name, ITEM2 {6} = item name
                new DeathTranslation(DeathFlags.None, "{0} was blown up inside a vehicle."),
                new DeathTranslation(DeathFlags.Item, "{0} was blown up inside a {3}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Killer, "{0} was blown up by {1} inside a {3} with a {6} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Killer, "{0} was blown up by {1} inside a {3} with a {6}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Player3, "{0} was blown up inside {5}'s {3} with a {6} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Player3, "{0} was blown up inside {5}'s {3} with a {6}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Killer | DeathFlags.Player3, "{0} was blown up by {1} inside {5}'s {3} with a {6} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Killer | DeathFlags.Player3, "{0} was blown up by {1} inside {5}'s {3} with a {6}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Item2, "{0} was blown up inside a {3} with a {6}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer | DeathFlags.Player3, "{0} was blown up by {1} inside {5}'s {3} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Killer | DeathFlags.Player3, "{0} was blown up by {1} inside {5}'s {3}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Player3, "{0} was blown up inside {5}'s {3} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Player3, "{0} was blown up inside {5}'s {3}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} was blown up by {1} inside a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Killer, "{0} was blown up by {1} inside a {3}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} blew themselves up with their vehicle."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} blew themselves up with their {3}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2, "{0} blew themselves up with their {3} using a {6}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Item2 | DeathFlags.Player3, "{0} blew themselves up with {5}'s {3} using a {6}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item | DeathFlags.Player3, "{0} blew themselves up with {5}'s {3}."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Player3, "{0} blew themselves up with {5}'s vehicle."),
            ]
        },
        new CauseGroup(EDeathCause.WATER, new DeathTranslation(DeathFlags.None, "{0} dehydrated.")),
        new CauseGroup(EDeathCause.ZOMBIE)
        {
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was mauled by a zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being mauled by a zombie.")
            ]
        },
        new CauseGroup
        {
            CustomKey = "maincamp",
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} died trying to main-camp."),
                new DeathTranslation(DeathFlags.Item, "{0} tried to main-camp with a {3}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} tried to main-camp {1} with a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Killer, "{0} tried to main-camp {1} with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out trying to main-camp."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out trying to main-camp with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out trying to main-camp {1} with a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out trying to main-camp {1} with a {3}."),
            ]
        },
        new CauseGroup
        {
            CustomKey = "maindeath",
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} died trying to enter their enemy's base.")
            ]
        },
        new CauseGroup
        {
            CustomKey = "explosive-consumable",
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} tried to consume dangerous food."),
                new DeathTranslation(DeathFlags.Item, "{0} tried to consume {3}."), // tested
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} suicide bombed {1} with a {3}.") // tested
            ]
        },
        new CauseGroup // mortar override
        {
            ItemCause = new AssetParameterTemplate<ItemAsset>(new Guid("d6424d034309417dbc5f17814af905a8")).CreateValueOnGameThread(),
            Translations =
            [
                new DeathTranslation(DeathFlags.None, "{0} was blown up by a mortar shell."),
                new DeathTranslation(DeathFlags.Item, "{0} was blown up by a mortar shell."),
                new DeathTranslation(DeathFlags.Killer, "{0} was blown up by {1}'s mortar from {4}m away."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was blown up by {1}'s mortar from {4}m away."),
                new DeathTranslation(DeathFlags.Suicide, "{0} blew themselves up with a mortar shell."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} blew themselves up with a mortar shell."), // tested
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being blown up by a mortar shell."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being blown up by a mortar shell."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being blown up by {1}'s mortar from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being blown up by {1}'s mortar from {4}m away."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide, "{0} bled out after blowing themselves up with a mortar shell."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Suicide | DeathFlags.Item, "{0} bled out after blowing themselves up with a mortar shell."),
            ]
        }
    };
    public async UniTask BroadcastDeath(PlayerDied e, CancellationToken token = default)
    {
        // red if its a teamkill, otherwise white
        bool tk = (e.MessageFlags & DeathFlags.Suicide) != DeathFlags.Suicide && e.WasTeamkill;
        Color32 color = tk ? new Color32(255, 153, 153, 255) : new Color32(255, 255, 255, 255);
        string? str = null;

        foreach (LanguageSet set in _translationService.SetOf.AllPlayers().ToCache().Sets!)
        {
            string msg = await TranslateMessage(set.Language, set.Culture, e, false, token);
            while (set.MoveNext())
            {
                _chatService.Send(set.Next, msg, color, EChatMode.SAY, null, true);
            }
        }

        str = await TranslateMessage(_languageService.GetDefaultLanguage(), CultureInfo.InvariantCulture, e, true, token);
        Log(tk, str, e);

        e.DefaultMessage = str!;

        // todo move to somewhere else
        // if (e.Player.UnturnedPlayer.TryGetPlayerData(out UCPlayerData data))
        //     data.CancelDeployment();

        PlayerDied toDispatch = e;

        // invoke PlayerDied event
        UniTask.Create(async () =>
        {
            await _dispatcher.DispatchEventAsync(toDispatch, CancellationToken.None);
            await UniTask.SwitchToMainThread();

            // todo
            //if (e is { WasEffectiveKill: true, Killer.PendingCheaterDeathBan: false } and { Cause: EDeathCause.GUN, Limb: ELimb.LEFT_FOOT or ELimb.RIGHT_FOOT or ELimb.LEFT_HAND or ELimb.RIGHT_HAND or ELimb.LEFT_BACK or ELimb.RIGHT_BACK or ELimb.LEFT_FRONT or ELimb.RIGHT_FRONT, KillDistance: > 3f })
            //{
            //    e.Killer.PendingCheaterDeathBan = true;
            //    UCWarfare.I.StartCoroutine(BanInRandomTime(e.Killer.Steam64, e.Killer.SteamPlayer.joined));
            //}
        });
    }

    private IEnumerator<WaitForSecondsRealtime> BanInRandomTime(CSteamID steam64, float joined)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        // make it harder to search for the source code causing it
        L.Log("Aut" + "o " + "ban " + "by a" + "nti" + "che" + "at: " + steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) + ".", ConsoleColor.Cyan);
        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(50f, 80f));

        Ban ban = new Ban
        {
            Player = steam64.m_SteamID,
            Actors =
            [
                new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.AntiCheat)
            ],
            RelevantLogsBegin = now.Subtract(TimeSpan.FromSeconds(Time.realtimeSinceStartup - joined)),
            RelevantLogsEnd = now,
            StartedTimestamp = now,
            ResolvedTimestamp = now,
                      // intentional leave this
            Message = "Au" + "tob" + "an by an" + "ti" + "-" + "ch" + "ea " + "t. Ap" + "pe" + "al at di" + "sc" + "or" + "d.g" + "g/{_dscIn}.",
            Duration = Timeout.InfiniteTimeSpan,
            PresetLevel = 1,
            PresetType = PresetType.Cheating
        };

        UniTask.Create(async () =>
        {
            await _moderationDb.AddOrUpdate(ban, CancellationToken.None);
        });
    }
    private static void Log(bool tk, string msg, PlayerDied e)
    {
        // todo string log = Util.RemoveRichText(msg);
        L.Log(msg, tk ? ConsoleColor.Cyan : ConsoleColor.DarkCyan);
        if (OffenseManager.IsValidSteam64Id(e.Instigator))
        {
            ActionLog.Add(ActionLogType.Death, msg + " | Killer: " + e.Instigator.m_SteamID, e.Player.Steam64);
            ActionLog.Add(ActionLogType.Kill, msg + " | Dead: " + e.Player.Steam64, e.Instigator.m_SteamID);
            if (tk)
                ActionLog.Add(ActionLogType.Teamkill, msg + " | Dead: " + e.Player.Steam64, e.Instigator.m_SteamID);
        }
        else
            ActionLog.Add(ActionLogType.Death, msg, e.Player.Steam64);
    }

    private const string JsonComment = @"/*
This file details all the different combinations of attributes that form a death message.
These attributes are represented by 6 formatting arguments:

 • None = death with little to no extra info.
 • Item = the primary item that killed the player is known.
 • Item2 = a secondary item that killed the player is known.
 • NoDistance = don't show the distance in the killfeed. This is used mainly when a player crashes a vehicle so the distance doesn't show up as 0
 • Killer = a player at fault is known, this isn't filled in for suicides
 • Player3 = a third player involved is known
 • Suicide = the dead player is at fault
 • Bleeding = the death happened after bleeding out

 • Always present
{0} = Dead player's name

 • Present when 'Killer' is in argument list, if the killer is the dead player 'Suicide' will be in the argument list instead
{1} = Killer's name

 • Present in gun and melee deaths
{2} = Limb name

 • Present when 'Item' is in argument list
{3} = Item Name

 • Present unless 'NoDistance' is in the argument list
{4} = Kill Distance

 • Present when 'Player3' is in the argument list. This player is used for some special cases:
  ○ Landmines (Killer = placer of landmine, Player3 = person that triggered it)
  ○ Vehicle (Killer = person that blew up the vehicle (sometimes the driver), Player3 = driver)
  ○ Gun (Killer = original shooter, Player3 = driver of vehicle if on a turret)
  ○ Splash damage (Killer = original shooter, Player3 = driver of vehicle if on a turret)
{5} = Player 3

 • present when 'Item2' is in the argument list
  ○ Gun (Item = original gun, Item2 = vehicle if shot from a turret)
  ○ Splash damage (Item = original gun, Item2 = vehicle if shot from a turret)
  ○ Landmines (Item = original landmine, Item2 = throwable item used to trigger landmine)
  ○ Sentry (Item = original sentry, Item2 = gun held by sentry)
  ○ Vehicle (Item = original vehicle, Item2 = item used to destroy the vehicle (gun, explosive, etc))
{6} = Item 2

The bottom item, ""d6424d03-4309-417d-bc5f-17814af905a8"", is an override for the mortar
*/

";
    public void Write(string? path, LanguageInfo language, bool writeMissing)
    {
        if (path == null)
        {
            path = Path.Combine(Data.Paths.LangStorage, L.Default);
            // todo F.CheckDir(path, out bool folderIsThere);
            // if (!folderIsThere)
            //     return;
            path = Path.Combine(path, "deaths.json");
        }

        if (!_translationList.TryGetValue(language.Code, out CauseGroup[] causes) && (language.IsDefault || !_translationList.TryGetValue(L.Default, out causes)))
            causes = _defaultTranslations;
        List<CauseGroup> causesFull = new List<CauseGroup>(causes);
        if (causes != _defaultTranslations && writeMissing)
        {
            for (int i = 0; i < _defaultTranslations.Length; ++i)
            {
                CauseGroup current = _defaultTranslations[i];
                int existingIndex = causesFull.FindIndex(x => x.Equals(current));
                if (existingIndex < 0)
                {
                    causesFull.Add(current);
                }
                else
                {
                    CauseGroup? existing = causesFull[existingIndex];
                    List<DeathTranslation>? existingTranslations = null;
                    for (int j = 0; j < current.Translations.Length; ++j)
                    {
                        bool found = false;
                        ref DeathTranslation t = ref current.Translations[j];
                        for (int k = 0; k < existing.Translations.Length; ++k)
                        {
                            ref DeathTranslation t2 = ref existing.Translations[k];
                            if (t2.Flags == t.Flags)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            (existingTranslations ??= new List<DeathTranslation>(4)).Add(t);
                        }
                    }
                    if (existingTranslations != null)
                    {
                        causesFull[existingIndex] = existing = (CauseGroup)existing.Clone();
                        DeathTranslation[] newArr = new DeathTranslation[existing.Translations.Length + existingTranslations.Count];
                        Array.Copy(existing.Translations, 0, newArr, 0, existing.Translations.Length);
                        existingTranslations.CopyTo(newArr, existing.Translations.Length);
                        existing.Translations = newArr;
                    }
                }
            }
        }
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        byte[] comment = System.Text.Encoding.UTF8.GetBytes(JsonComment);
        stream.Write(comment, 0, comment.Length);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream, ConfigurationSettings.JsonWriterOptions);
        writer.WriteStartArray();
        for (int i = 0; i < causesFull.Count; ++i)
        {
            causesFull[i].WriteJson(writer);
        }
        writer.WriteEndArray();
        writer.Dispose();
    }
    internal void Reload()
    {
        // Localization.ClearSection(TranslationSection.Deaths);
        // Localization.IncrementSection(TranslationSection.Deaths, Mathf.CeilToInt(_defaultTranslations.SelectMany(x => x.Translations).Count()));
        string[] langDirs = Directory.GetDirectories(Data.Paths.LangStorage, "*", SearchOption.TopDirectoryOnly);

        // F.CheckDir(Data.Paths.LangStorage + L.Default, out bool folderIsThere);
        // if (!folderIsThere)
        //     return;

        string directory = Path.Combine(Data.Paths.LangStorage, L.Default, "deaths.json");
        if (!File.Exists(directory))
        {
            using FileStream stream = File.Create(directory);
            byte[] comment = System.Text.Encoding.UTF8.GetBytes(JsonComment);
            stream.Write(comment, 0, comment.Length);
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, ConfigurationSettings.JsonWriterOptions);
            writer.WriteStartArray();
            for (int i = 0; i < _defaultTranslations.Length; ++i)
            {
                _defaultTranslations[i].WriteJson(writer);
            }
            writer.WriteEndArray();
            writer.Dispose();
        }
        List<CauseGroup> causes = new List<CauseGroup>(_defaultTranslations.Length);
        foreach (string folder in langDirs)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folder);
            string lang = directoryInfo.Name;
            FileInfo[] langFiles = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
            LanguageInfo? language = _languageDataStore.GetInfoCached(lang);
            foreach (FileInfo info in langFiles)
            {
                if (info.Name.Equals("deaths.json", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (_translationList.ContainsKey(lang)) continue;
                    using FileStream stream = info.OpenRead();
                    if (stream.Length > int.MaxValue)
                        continue;
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, ConfigurationSettings.JsonReaderOptions);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            CauseGroup info2 = new CauseGroup();
                            info2.ReadJson(ref reader);
                            causes.Add(info2);
                        }
                    }

                    _translationList.Add(lang, causes.ToArray());
                    // todo language?.IncrementSection(TranslationSection.Deaths, causes.Count);
                    causes.Clear();
                    break;
                }
            }
        }
    }

    public async UniTask<string> TranslateMessage(LanguageInfo language, CultureInfo culture, PlayerDied args, bool useSteamNames, CancellationToken token = default)
    {
        bool isDefault = false;
        if (_translationList.Count == 0 || (!_translationList.TryGetValue(language.Code, out CauseGroup[] causes) && (L.Default.Equals(language) || !_translationList.TryGetValue(L.Default, out causes))))
        {
            isDefault = true;
            causes = _defaultTranslations;
        }

        if (causes is null)
            return _valueFormatter.FormatEnum(args.Cause, language) + " Dead: " + args.Player.Names.CharacterName;
        rtn:
        int i = FindDeathCause(language, causes, args);

        CauseGroup cause = causes[i];
        string? val = await TranslateDeath(args, language, culture, cause, useSteamNames, token);

        await UniTask.SwitchToMainThread(token);

        if (val is null)
        {
            if (isDefault)
                return _valueFormatter.FormatEnum(args.Cause, language) + " Dead: " + args.Player.Names.CharacterName;
            causes = _defaultTranslations;
            isDefault = true;
            goto rtn;
        }
        return val;
    }

    private int FindDeathCause(LanguageInfo language, CauseGroup[] causes, PlayerDied args)
    {
        while (true)
        {
            args.PrimaryAsset.TryGetGuid(out Guid guid);
            bool item = guid != default;
            string? specKey = args.MessageKey;
            if (specKey is not null)
            {
                for (int i = 0; i < causes.Length; ++i)
                {
                    CauseGroup cause = causes[i];
                    if (cause.CustomKey is not null && cause.CustomKey.Equals(specKey, StringComparison.Ordinal)) return i;
                }
            }

            if (item)
            {
                if (args.PrimaryAsset is IAssetLink<VehicleAsset>)
                {
                    for (int i = 0; i < causes.Length; ++i)
                    {
                        CauseGroup cause = causes[i];
                        if (cause.VehicleCause != null && cause.VehicleCause.IsMatch(guid)) return i;
                    }
                }
                else
                {
                    for (int i = 0; i < causes.Length; ++i)
                    {
                        CauseGroup cause = causes[i];
                        if (cause.ItemCause != null && cause.ItemCause.IsMatch(guid)) return i;
                    }
                }
            }

            EDeathCause cause2 = args.Cause;
            for (int i = 0; i < causes.Length; ++i)
            {
                CauseGroup cause = causes[i];
                if (cause.Cause.HasValue && cause.Cause == cause2) return i;
            }
            if (!language.IsDefault && _translationList.TryGetValue(L.Default, out causes))
            {
                language = _languageService.GetDefaultLanguage();
                continue;
            }
            if (causes != _defaultTranslations)
            {
                causes = _defaultTranslations;
                continue;
            }

            return -1;
        }
    }

    /// <summary>
    /// Choose a template based on the <see cref="EDeathCause"/> and format it.
    /// </summary>
    public UniTask<string?> TranslateDeath(PlayerDied e, LanguageInfo language, IFormatProvider formatProvider, CauseGroup cause, bool useSteamNames, CancellationToken token = default)
    {
        DeathFlags flags = e.MessageFlags;
    redo:
        for (int i = 0; i < cause.Translations.Length; ++i)
        {
            ref DeathTranslation d = ref cause.Translations[i];
            if (d.Flags == flags)
                return TranslateDeath(d.Value, e, language, formatProvider, useSteamNames, token)!;
        }

        _logger.LogWarning("Exact match not found for {0}.", flags);
        if ((flags & DeathFlags.NoDistance) == DeathFlags.NoDistance)
        {
            flags &= ~DeathFlags.NoDistance;
            goto redo;
        }
        if ((flags & DeathFlags.Player3) == DeathFlags.Player3)
        {
            flags &= ~DeathFlags.Player3;
            goto redo;
        }
        if ((flags & DeathFlags.Item2) == DeathFlags.Item2)
        {
            flags &= ~DeathFlags.Item2;
            goto redo;
        }
        if ((flags & DeathFlags.Killer) == DeathFlags.Killer)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == DeathFlags.Killer)
                    return TranslateDeath(d.Value, e, language, formatProvider, useSteamNames, token)!;
            }
        }
        else if ((flags & DeathFlags.Suicide) == DeathFlags.Suicide)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == DeathFlags.Suicide)
                    return TranslateDeath(d.Value, e, language, formatProvider, useSteamNames, token)!;
            }
        }
        else if ((flags & DeathFlags.Player3) == DeathFlags.Player3)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == DeathFlags.Player3)
                    return TranslateDeath(d.Value, e, language, formatProvider, useSteamNames, token)!;
            }
        }
        for (int i = 0; i < cause.Translations.Length; ++i)
        {
            ref DeathTranslation d = ref cause.Translations[i];
            if (d.Flags == DeathFlags.None)
                return TranslateDeath(d.Value, e, language, formatProvider, useSteamNames, token)!;
        }

        return UniTask.FromResult<string?>(null);
    }

    /// <summary>
    /// Format a specific template using the given death args.
    /// </summary>
    private async UniTask<string> TranslateDeath(string template, PlayerDied e, LanguageInfo? language, IFormatProvider formatProvider, bool useSteamNames, CancellationToken token = default)
    {
        language ??= _languageService.GetDefaultLanguage();

        string? killerName = null;
        if (e.Instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            if (e.Killer == null)
            {
                PlayerNames names = await F.GetPlayerOriginalNamesAsync(e.Instigator.m_SteamID, token);
                killerName = useSteamNames ? names.PlayerName : names.CharacterName;
            }
            else
            {
                killerName = useSteamNames ? e.Killer.Names.PlayerName : e.Killer.Names.CharacterName;
            }
        }

        string? thirdPartyName = null;
        if (e.ThirdPartyId.HasValue && e.ThirdPartyId.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            if (e.ThirdParty == null)
            {
                PlayerNames names = await F.GetPlayerOriginalNamesAsync(e.ThirdPartyId.Value.m_SteamID, token);
                thirdPartyName = useSteamNames ? names.PlayerName : names.CharacterName;
            }
            else
            {
                thirdPartyName = useSteamNames ? e.ThirdParty.Names.PlayerName : e.ThirdParty.Names.CharacterName;
            }
        }

        string? itemName = e.PrimaryAsset?.GetAsset()?.FriendlyName;
        if (itemName != null && itemName.EndsWith(" Built", StringComparison.Ordinal))
        {
            itemName = itemName[..^6];
        }

        string[] format =
        [
            useSteamNames ? e.Player.Names.PlayerName : e.Player.Names.CharacterName, // {0}
            killerName ?? string.Empty,                                               // {1}
            _valueFormatter.FormatEnum(e.Limb, language),                                  // {2}
            itemName ?? string.Empty,                                                 // {3}
            e.KillDistance.ToString("F0", formatProvider),                            // {4}
            thirdPartyName ?? string.Empty,                                           // {5}
            e.SecondaryAsset?.GetAsset()?.FriendlyName ?? string.Empty,               // {6}
        ];

        try
        {
            // ReSharper disable once CoVariantArrayConversion
            return string.Format(template, format);
        }
        catch (FormatException)
        {
            _logger.LogWarning("Formatting error for template: \"{0}\" ({1}:{2}:{3}).",
                template,
                _valueFormatter.FormatEnum(e.MessageCause, language),
                _valueFormatter.FormatEnum(e.MessageFlags, language),
                language.Code.ToUpper()
            );

            return template.Replace("{0}", format[0]);
        }
    }
}

/// <summary>
/// Represents a group of translations linked to a single <see cref="EDeathCause"/>, or a specific item, vehicle, or custom message key.
/// </summary>
public class CauseGroup : IEquatable<CauseGroup>, ICloneable
{
    public EDeathCause? Cause;
    public QuestParameterValue<Guid>? ItemCause;
    public QuestParameterValue<Guid>? VehicleCause;
    public string? CustomKey;
    public DeathTranslation[] Translations;

    public CauseGroup() { }

    private CauseGroup(EDeathCause? cause, QuestParameterValue<Guid>? itemCause, QuestParameterValue<Guid>? vehicleCause, string? customKey, DeathTranslation[] translations)
    {
        Cause = cause;
        ItemCause = itemCause;
        VehicleCause = vehicleCause;
        CustomKey = customKey;
        Translations = translations;
    }

    public CauseGroup(EDeathCause cause)
    {
        Cause = cause;
    }

    public CauseGroup(EDeathCause cause, DeathTranslation translation) : this(cause, [ translation ]) { }

    public CauseGroup(EDeathCause cause, DeathTranslation[] translations) : this(cause)
    {
        Translations = translations;
    }

    public void ReadJson(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject) reader.Read();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            string? prop = reader.GetString();
            if (!reader.Read() || prop == null)
                continue;
            
            if (prop.Equals("death-cause", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType != JsonTokenType.String
                    || (prop = reader.GetString()) == null
                    || !Enum.TryParse(prop, true, out EDeathCause c))
                {
                    continue;
                }

                CustomKey = null;
                Cause = c;
                ItemCause = null;
                VehicleCause = null;
            }
            else if (prop.Equals("item-cause", StringComparison.OrdinalIgnoreCase))
            {
                CustomKey = null;
                Cause = null;
                ItemCause = AssetParameterTemplate<ItemAsset>.ReadValueJson(ref reader);
                VehicleCause = null;
            }
            else if (prop.Equals("vehicle-cause", StringComparison.OrdinalIgnoreCase))
            {
                CustomKey = null;
                Cause = null;
                ItemCause = null;
                VehicleCause = AssetParameterTemplate<VehicleAsset>.ReadValueJson(ref reader);
            }
            else if (prop.Equals("custom-key", StringComparison.OrdinalIgnoreCase))
            {
                if ((prop = reader.GetString()) == null)
                {
                    continue;
                }

                CustomKey = prop;
                Cause = null;
                ItemCause = null;
                VehicleCause = null;
            }
            else if (prop.Equals("translations", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    continue;
                }

                List<DeathTranslation> translations = new List<DeathTranslation>(5);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        continue;

                    prop = reader.GetString();

                    if (reader.Read()
                        && prop != null
                        && Enum.TryParse(prop, true, out DeathFlags flags)
                        && reader.TokenType == JsonTokenType.String
                        && (prop = reader.GetString()) != null)
                    {
                        translations.Add(new DeathTranslation(flags, prop));
                    }
                }

                Translations = translations.ToArray();
            }
        }
    }
    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (Cause.HasValue)
        {
            writer.WriteString("death-cause", Cause.ToString().ToLower());
        }
        else if (ItemCause != null)
        {
            writer.WritePropertyName("item-cause");
            ItemCause.WriteJson(writer);
        }
        else if (VehicleCause != null)
        {
            writer.WritePropertyName("vehicle-cause");
            VehicleCause.WriteJson(writer);
        }
        else if (CustomKey is not null)
        {
            writer.WriteString("custom-key", CustomKey);
        }
        else
        {
            writer.WritePropertyName("error");
            writer.WriteBooleanValue(true);
            return;
        }

        writer.WritePropertyName("translations");
        writer.WriteStartObject();
        for (int i = 0; i < Translations.Length; i++)
        {
            ref DeathTranslation translation = ref Translations[i];
            writer.WriteString(translation.Flags.ToString(), translation.Value);
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    public override bool Equals(object? obj) => obj is CauseGroup c && Equals(c);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Cause, ItemCause, VehicleCause, CustomKey, Translations);
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public static bool operator ==(CauseGroup? left, CauseGroup? right) => Equals(left, right);
    public static bool operator !=(CauseGroup? left, CauseGroup? right) => !(left == right);
    public object Clone()
    {
        DeathTranslation[] newTranslations = new DeathTranslation[Translations.Length];
        Array.Copy(Translations, 0, newTranslations, 0, newTranslations.Length);
        return new CauseGroup(Cause, ItemCause, VehicleCause, CustomKey, newTranslations);
    }

    public bool Equals(CauseGroup other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        if (other.Cause.HasValue != Cause.HasValue)
            return false;
        if (other.Cause.HasValue && other.Cause.Value != Cause!.Value)
            return false;

        if ((other.ItemCause != null) != (ItemCause != null))
            return false;
        if (other.ItemCause != null && !other.ItemCause.Equals(ItemCause))
            return false;

        if ((other.VehicleCause != null) != (VehicleCause != null))
            return false;
        if (other.VehicleCause != null && !other.VehicleCause.Equals(VehicleCause))
            return false;

        return string.Equals(other.CustomKey, CustomKey, StringComparison.Ordinal);
    }
}

public readonly struct DeathTranslation(DeathFlags flags, string value)
{
    public readonly DeathFlags Flags = flags;
    public readonly string Value = value;
}

[Flags]
public enum DeathFlags : byte
{
    None = 0,
    Item = 1,
    Killer = 2,
    Suicide = 4,
    Player3 = 8,
    Item2 = 16,
    Bleeding = 32,
    NoDistance = 64
}