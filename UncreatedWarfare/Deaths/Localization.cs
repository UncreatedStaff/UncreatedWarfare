﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Deaths;
internal static class Localization
{
    /*
     * {0}  = Dead player's name
     * {1} ?= Killer's name
     * {2} ?= Limb name
     * {3} ?= Item Name
     * {4} ?= Kill Distance
     * {5} ?= Player 3
     * {6} ?= Item 2
     */
    private static readonly Dictionary<string, DeathCause[]> DeathTranslations = new Dictionary<string, DeathCause[]>(48);

    private static readonly DeathCause[] DefaultValues =
    {
        new DeathCause(EDeathCause.ACID)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was burned by an acid zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being burned by an acid zombie.")
            }
        },
        new DeathCause(EDeathCause.ANIMAL)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was attacked by an animal."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being attacked by an animal.")
            }
        },
        new DeathCause(EDeathCause.ARENA, new DeathTranslation(DeathFlags.None, "{0} stepped outside the arena boundary.")),
        new DeathCause(EDeathCause.BLEEDING)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} bled out."),
                new DeathTranslation(DeathFlags.Killer, "{0} bled out because of {1}."),
                new DeathTranslation(DeathFlags.Item, "{0} bled out from a {3}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} bled out because of {1} from a {3}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} bled out by their own hand."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} bled out by their own hand from a {3}."),
            }
        },
        new DeathCause(EDeathCause.BONES)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} fell to their death."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after breaking their legs.")
            }
        },
        new DeathCause(EDeathCause.BOULDER)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was crushed by a mega zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being crushed by a mega zombie.")
            }
        },
        new DeathCause(EDeathCause.BREATH, new DeathTranslation(DeathFlags.None, "{0} asphyxiated.")),
        new DeathCause(EDeathCause.BURNER)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was burned by a mega zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being burned by a mega zombie.")
            }
        },
        new DeathCause(EDeathCause.BURNING)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} burned to death."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being burned.")
            }
        },
        new DeathCause(EDeathCause.CHARGE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was blown up by a demolition charge."),
                new DeathTranslation(DeathFlags.Item, "{0} was blown up by a {3}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{1} blew up {0} with a {3}."),
                new DeathTranslation(DeathFlags.Killer, "{1} blew up {0} with a demolition charge."),
                new DeathTranslation(DeathFlags.Suicide, "{0} blew themselves up with a demolition charge."),
                new DeathTranslation(DeathFlags.Suicide | DeathFlags.Item, "{0} blew themselves up with a {3}."),
            }
        },
        new DeathCause(EDeathCause.FOOD, new DeathTranslation(DeathFlags.None, "{0} starved to death.")),
        new DeathCause(EDeathCause.FREEZING, new DeathTranslation(DeathFlags.None, "{0} froze to death.")),
        new DeathCause(EDeathCause.GRENADE)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.GUN)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.INFECTION)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} died to an infection."),
                new DeathTranslation(DeathFlags.Item, "{0} died to an infection from {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out from an infection."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after using a {3}."),
                new DeathTranslation(DeathFlags.Killer, "{0} died to an infection caused by {1}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} died to an infection from {3} caused by {1}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out from an infection caused by {1}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out after {1} used a {3} on them.")
            }
        },
        new DeathCause(EDeathCause.KILL)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was killed by an operator."), // tested
                new DeathTranslation(DeathFlags.Killer, "{0} was killed by an admin, {1}."),
                new DeathTranslation(DeathFlags.Suicide, "{0} killed themselves as an admin."), // tested
            }
        },
        new DeathCause(EDeathCause.LANDMINE)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.MELEE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was struck in the {2}."),
                new DeathTranslation(DeathFlags.Item, "{0} was struck by a {3} in the {2}."),
                new DeathTranslation(DeathFlags.Killer, "{0} was struck by {1} in the {2}."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Item, "{0} was struck by {1} with a {3} in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being struck in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out after being struck by a {3} in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer, "{0} bled out after being struck by {1} in the {2}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Killer | DeathFlags.Item, "{0} bled out after being struck by {1} with a {3} in the {2}.")
            }
        },
        new DeathCause(EDeathCause.MISSILE)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.PUNCH)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was pummeled to death."),
                new DeathTranslation(DeathFlags.Killer, "{1} punched {0} to death."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being pummeled."),
                new DeathTranslation(DeathFlags.Killer | DeathFlags.Bleeding, "{0} bled out after being punched by {1}.")
            }
        },
        new DeathCause(EDeathCause.ROADKILL)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.SENTRY)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.SHRED)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.SPARK)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was shocked by a mega zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being shocked by a mega zombie.")
            }
        },
        new DeathCause(EDeathCause.SPIT)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was killed by a spitter zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being spit on by a zombie.")
            }
        },
        new DeathCause(EDeathCause.SPLASH)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.SUICIDE, new DeathTranslation(DeathFlags.None, "{0} commited suicide.")),
        new DeathCause(EDeathCause.VEHICLE)
        {
            Translations = new DeathTranslation[]
            {
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
            }
        },
        new DeathCause(EDeathCause.WATER, new DeathTranslation(DeathFlags.None, "{0} dehydrated.")),
        new DeathCause(EDeathCause.ZOMBIE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} was mauled by a zombie."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out after being mauled by a zombie.")
            }
        },
        new DeathCause
        {
            CustomKey = "maincamp",
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} died trying to main-camp."),
                new DeathTranslation(DeathFlags.Item, "{0} tried to main-camp with a {3}."),
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} tried to main-camp {1} with a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Item | DeathFlags.Killer, "{0} tried to main-camp {1} with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding, "{0} bled out trying to main-camp."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item, "{0} bled out trying to main-camp with a {3}."),
                new DeathTranslation(DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out trying to main-camp {1} with a {3} from {4}m away."),
                new DeathTranslation(DeathFlags.NoDistance | DeathFlags.Bleeding | DeathFlags.Item | DeathFlags.Killer, "{0} bled out trying to main-camp {1} with a {3}."),
            }
        },
        new DeathCause
        {
            CustomKey = "maindeath",
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} died trying to enter their enemy's base.")
            }
        },
        new DeathCause
        {
            CustomKey = "explosive-consumable",
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(DeathFlags.None, "{0} tried to consume dangerous food."),
                new DeathTranslation(DeathFlags.Item, "{0} tried to consume {3}."), // tested
                new DeathTranslation(DeathFlags.Item | DeathFlags.Killer, "{0} suicide bombed {1} with a {3}.") // tested
            }
        },
        new DeathCause // mortar override
        {
            ItemCause = new DynamicAssetValue<ItemAsset>(new Guid("d6424d034309417dbc5f17814af905a8")).GetValue(),
            Translations = new DeathTranslation[]
            {
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
            }
        }
    };
    public static void BroadcastDeath(PlayerDied e, DeathMessageArgs args)
    {
        bool sentInConsole = false;
        // red if its a teamkill, otherwise white
        bool tk = (args.Flags & DeathFlags.Suicide) != DeathFlags.Suicide && args.IsTeamkill;
        Color color = UCWarfare.GetColor(tk ? "death_background_teamkill" : "death_background");
        string? str = null;
        foreach (LanguageSet set in LanguageSet.All())
        {
            string msg = TranslateMessage(set.Language, set.CultureInfo, args);
            if (!sentInConsole && set.Language.IsDefault && set.CultureInfo.Name.Equals(Data.AdminLocale.Name, StringComparison.Ordinal))
            {
                Log(tk, msg, e);
                sentInConsole = true;
                str = msg;
            }
            while (set.MoveNext())
            {
                Chat.SendSingleMessage(msg, color, EChatMode.SAY, null, true, set.Next.Player.channel.owner);
            }
        }

        if (!sentInConsole)
        {
            str = TranslateMessage(Warfare.Localization.GetDefaultLanguage(), Data.AdminLocale, args);
            Log(tk, str, e);
        }

        e.LocalizationArgs = args;
        e.Message = str!;
        EventDispatcher.InvokeOnPlayerDied(e);

        if (e.WasEffectiveKill && e.Killer is { PendingCheaterDeathBan: false } &&
            Util.IsSuspiciousDeath(e.Killer.Player, e.Cause, e.PrimaryAsset, e.SecondaryItem, e.Limb,
                e.KillDistance, e.WasEffectiveKill, e.WasSuicide, e.WasTeamkill))
        {
            e.Killer.PendingCheaterDeathBan = true;
            UCWarfare.I.StartCoroutine(BanInRandomTime(e.Killer.Steam64, e.Killer.SteamPlayer.joined));
        }
    }

    private static IEnumerator<WaitForSecondsRealtime> BanInRandomTime(ulong steam64, float joined)
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        L.Log("Auto ban by anticheat: " + steam64.ToString(Data.AdminLocale) + ".", ConsoleColor.Cyan);
        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(50f, 80f));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Ban ban = new Ban
        {
            Player = steam64,
            Actors = new RelatedActor[]
            {
                new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.AntiCheat)
            },
            RelevantLogsBegin = now.Subtract(TimeSpan.FromSeconds(Time.realtimeSinceStartup - joined)),
            RelevantLogsEnd = now,
            StartedTimestamp = now,
            ResolvedTimestamp = now,
            Message = $"Autoban by anti-cheat. Appeal at discord.gg/{UCWarfare.Config.DiscordInviteCode}.",
            Duration = Timeout.InfiniteTimeSpan,
            PresetLevel = 1,
            PresetType = PresetType.Cheating
        };

        UCWarfare.RunTask(Data.ModerationSql.AddOrUpdate, ban, CancellationToken.None, ctx: "Ban suspected cheater.");
    }
    private static void Log(bool tk, string msg, PlayerDied e)
    {
        string log = Util.RemoveRichText(msg);
        L.Log(log, tk ? ConsoleColor.Cyan : ConsoleColor.DarkCyan);
        if (OffenseManager.IsValidSteam64Id(e.Instigator))
        {
            ActionLog.Add(ActionLogType.Death, log + " | Killer: " + e.Instigator.m_SteamID, e.Player.Steam64);
            ActionLog.Add(ActionLogType.Kill, log + " | Dead: " + e.Player.Steam64, e.Instigator.m_SteamID);
            if (tk)
                ActionLog.Add(ActionLogType.Teamkill, log + " | Dead: " + e.Player.Steam64, e.Instigator.m_SteamID);
        }
        else
            ActionLog.Add(ActionLogType.Death, log, e.Player.Steam64);
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
    public static void Write(string? path, LanguageInfo language, bool writeMissing)
    {
        if (path == null)
        {
            path = Path.Combine(Data.Paths.LangStorage, L.Default);
            F.CheckDir(path, out bool folderIsThere);
            if (!folderIsThere)
                return;
            path = Path.Combine(path, "deaths.json");
        }

        if (!DeathTranslations.TryGetValue(language.Code, out DeathCause[] causes) && (language.IsDefault || !DeathTranslations.TryGetValue(L.Default, out causes)))
            causes = DefaultValues;
        List<DeathCause> causesFull = new List<DeathCause>(causes);
        if (causes != DefaultValues && writeMissing)
        {
            for (int i = 0; i < DefaultValues.Length; ++i)
            {
                DeathCause current = DefaultValues[i];
                int existingIndex = causesFull.FindIndex(x => x.Equals(current));
                if (existingIndex < 0)
                {
                    causesFull.Add(current);
                }
                else
                {
                    DeathCause? existing = causesFull[existingIndex];
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
                        causesFull[existingIndex] = existing = (DeathCause)existing.Clone();
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
        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
        writer.WriteStartArray();
        for (int i = 0; i < causesFull.Count; ++i)
        {
            causesFull[i].WriteJson(writer);
        }
        writer.WriteEndArray();
        writer.Dispose();
    }
    internal static void Reload()
    {
        Warfare.Localization.ClearSection(TranslationSection.Deaths);
        Warfare.Localization.IncrementSection(TranslationSection.Deaths, Mathf.CeilToInt(DefaultValues.SelectMany(x => x.Translations).Count()));
        string[] langDirs = Directory.GetDirectories(Data.Paths.LangStorage, "*", SearchOption.TopDirectoryOnly);

        F.CheckDir(Data.Paths.LangStorage + L.Default, out bool folderIsThere);
        if (folderIsThere)
        {
            string directory = Path.Combine(Data.Paths.LangStorage, L.Default, "deaths.json");
            if (!File.Exists(directory))
            {
                using FileStream stream = File.Create(directory);
                byte[] comment = System.Text.Encoding.UTF8.GetBytes(JsonComment);
                stream.Write(comment, 0, comment.Length);
                Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                writer.WriteStartArray();
                for (int i = 0; i < DefaultValues.Length; ++i)
                {
                    DefaultValues[i].WriteJson(writer);
                }
                writer.WriteEndArray();
                writer.Dispose();
            }
            List<DeathCause> causes = new List<DeathCause>(DefaultValues.Length);
            foreach (string folder in langDirs)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folder);
                string lang = directoryInfo.Name;
                FileInfo[] langFiles = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                LanguageInfo? language = Data.LanguageDataStore.GetInfoCached(lang);
                foreach (FileInfo info in langFiles)
                {
                    if (info.Name.Equals("deaths.json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (DeathTranslations.ContainsKey(lang)) continue;
                        using FileStream stream = info.OpenRead();
                        if (stream.Length > int.MaxValue)
                            continue;
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                DeathCause info2 = new DeathCause();
                                info2.ReadJson(ref reader);
                                causes.Add(info2);
                            }
                        }

                        DeathTranslations.Add(lang, causes.ToArray());
                        language?.IncrementSection(TranslationSection.Deaths, causes.Count);
                        causes.Clear();
                        break;
                    }
                }
            }
        }
    }
    public static string TranslateMessage(LanguageInfo language, CultureInfo culture, DeathMessageArgs args)
    {
        if (string.IsNullOrEmpty(args.ItemName)) args.Flags &= ~DeathFlags.Item;
        if (string.IsNullOrEmpty(args.Item2Name)) args.Flags &= ~DeathFlags.Item2;
        if (string.IsNullOrEmpty(args.KillerName)) args.Flags &= ~DeathFlags.Killer;
        if (string.IsNullOrEmpty(args.Player3Name)) args.Flags &= ~DeathFlags.Player3;
        bool isDefault = false;
        if (DeathTranslations.Count == 0 || (!DeathTranslations.TryGetValue(language.Code, out DeathCause[] causes) && (L.Default.Equals(language) || !DeathTranslations.TryGetValue(L.Default, out causes))))
        {
            isDefault = true;
            causes = DefaultValues;
        }

        if (causes is null)
            return Warfare.Localization.TranslateEnum(args.DeathCause, language) + " Dead: " + args.DeadPlayerName;
        rtn:
        int i = FindDeathCause(language, causes, ref args);

        DeathCause cause = causes[i];
        string? val = Translate(language, culture, cause, args);
        if (val is null)
        {
            if (isDefault)
                return Warfare.Localization.TranslateEnum(args.DeathCause, language) + " Dead: " + args.DeadPlayerName;
            causes = DefaultValues;
            isDefault = true;
            goto rtn;
        }
        return val;
    }

    private static int FindDeathCause(LanguageInfo language, DeathCause[] causes, ref DeathMessageArgs args)
    {
        while (true)
        {
            Guid guid;
            bool item = (guid = args.ItemGuid) != default;
            string? specKey = args.SpecialKey;
            if (specKey is not null)
            {
                for (int i = 0; i < causes.Length; ++i)
                {
                    DeathCause cause = causes[i];
                    if (cause.CustomKey is not null && cause.CustomKey.Equals(specKey, StringComparison.Ordinal)) return i;
                }
            }

            if (item)
            {
                if (args.ItemIsVehicle)
                {
                    for (int i = 0; i < causes.Length; ++i)
                    {
                        DeathCause cause = causes[i];
                        if (cause.VehicleCause.HasValue && cause.VehicleCause.Value.IsMatch(guid)) return i;
                    }
                }
                else
                {
                    for (int i = 0; i < causes.Length; ++i)
                    {
                        DeathCause cause = causes[i];
                        if (cause.ItemCause.HasValue && cause.ItemCause.Value.IsMatch(guid)) return i;
                    }
                }
            }

            EDeathCause cause2 = args.DeathCause;
            for (int i = 0; i < causes.Length; ++i)
            {
                DeathCause cause = causes[i];
                if (cause.Cause.HasValue && cause.Cause == cause2) return i;
            }
            if (!language.IsDefault && DeathTranslations.TryGetValue(L.Default, out causes))
            {
                language = Warfare.Localization.GetDefaultLanguage();
                continue;
            }
            if (causes != DefaultValues)
            {
                causes = DefaultValues;
                continue;
            }

            return -1;
        }
    }

    private static string? Translate(LanguageInfo language, CultureInfo culture, DeathCause cause, DeathMessageArgs args)
    {
        DeathFlags flags = args.Flags;
    redo:
        for (int i = 0; i < cause.Translations.Length; ++i)
        {
            ref DeathTranslation d = ref cause.Translations[i];
            if (d.Flags == flags)
                return args.Translate(d.Value, language, culture);
        }
        L.LogWarning("Exact match not found for " + flags + ".");
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
                    return args.Translate(d.Value, language, culture);
            }
        }
        else if ((flags & DeathFlags.Suicide) == DeathFlags.Suicide)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == DeathFlags.Suicide)
                    return args.Translate(d.Value, language, culture);
            }
        }
        else if ((flags & DeathFlags.Player3) == DeathFlags.Player3)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == DeathFlags.Player3)
                    return args.Translate(d.Value, language, culture);
            }
        }
        for (int i = 0; i < cause.Translations.Length; ++i)
        {
            ref DeathTranslation d = ref cause.Translations[i];
            if (d.Flags == DeathFlags.None)
                return args.Translate(d.Value, language, culture);
        }

        return null;
    }
}
public struct DeathMessageArgs
{
    public EDeathCause DeathCause;
    public string? SpecialKey;
    public string DeadPlayerName;
    public string? KillerName;
    public ulong DeadPlayerTeam;
    public ulong KillerTeam;
    public ELimb Limb;
    public string? ItemName;
    public Guid ItemGuid;
    public Guid Item2Guid;
    public bool ItemIsVehicle;
    public float KillDistance;
    public string? Player3Name;
    public ulong Player3Team;
    public string? Item2Name;
    public DeathFlags Flags;
    public bool IsTeamkill;
    internal readonly string Translate(string template, LanguageInfo language, CultureInfo culture)
    {
        object[] format = new object[7];
        format[0] = DeadPlayerName.Colorize(TeamManager.GetTeamHexColor(DeadPlayerTeam));
        format[1] = KillerName is null ? string.Empty : KillerName.Colorize(TeamManager.GetTeamHexColor(KillerTeam));
        format[2] = Warfare.Localization.TranslateEnum(Limb, language);
        format[3] = ItemName is not null ? ItemName.EndsWith(" Built", StringComparison.Ordinal) ? ItemName[..^6] : ItemName : string.Empty;
        format[4] = KillDistance.ToString("F0", culture);
        format[5] = Player3Name is null ? string.Empty : Player3Name.Colorize(TeamManager.GetTeamHexColor(Player3Team));
        format[6] = Item2Name ?? string.Empty;
        try
        {
            return string.Format(template, format);
        }
        catch (FormatException)
        {
            L.LogWarning("Formatting error for template: \"" + template + "\" (" + Warfare.Localization.TranslateEnum(DeathCause, Warfare.Localization.GetDefaultLanguage()) + ":"
                         + Warfare.Localization.TranslateEnum(Flags, Warfare.Localization.GetDefaultLanguage()) + ":" + language.Code.ToUpper() + ").");
            return template.Replace("{0}", (string)format[0]);
        }
    }
}

public class DeathCause : IJsonReadWrite, IEquatable<DeathCause>, ICloneable
{
    public EDeathCause? Cause;
    public DynamicAssetValue<ItemAsset>.Choice? ItemCause;
    public DynamicAssetValue<VehicleAsset>.Choice? VehicleCause;
    public string? CustomKey;
    public DeathTranslation[] Translations;

    public DeathCause() { }

    private DeathCause(EDeathCause? cause, DynamicAssetValue<ItemAsset>.Choice? itemCause, DynamicAssetValue<VehicleAsset>.Choice? vehicleCause, string? customKey, DeathTranslation[] translations)
    {
        Cause = cause;
        ItemCause = itemCause;
        VehicleCause = vehicleCause;
        CustomKey = customKey;
        Translations = translations;
    }
    public DeathCause(EDeathCause cause)
    {
        Cause = cause;
    }
    public DeathCause(EDeathCause cause, DeathTranslation translation) : this(cause, new DeathTranslation[] { translation }) { }
    public DeathCause(EDeathCause cause, DeathTranslation[] translations) : this(cause)
    {
        Translations = translations;
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject) reader.Read();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop is not null)
                {
                    if (prop.Equals("death-cause", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.TokenType == JsonTokenType.String && (prop = reader.GetString()) is not null)
                        {
                            if (Enum.TryParse(prop, true, out EDeathCause c))
                            {
                                CustomKey = null;
                                Cause = c;
                                ItemCause = null;
                                VehicleCause = null;
                            }
                        }
                    }
                    else if (prop.Equals("item-cause", StringComparison.OrdinalIgnoreCase))
                    {
                        CustomKey = null;
                        Cause = null;
                        ItemCause = DynamicAssetValue<ItemAsset>.ReadChoice(ref reader);
                        VehicleCause = null;
                    }
                    else if (prop.Equals("vehicle-cause", StringComparison.OrdinalIgnoreCase))
                    {
                        CustomKey = null;
                        Cause = null;
                        ItemCause = null;
                        VehicleCause = DynamicAssetValue<VehicleAsset>.ReadChoice(ref reader);
                    }
                    else if (prop.Equals("custom-key", StringComparison.OrdinalIgnoreCase))
                    {
                        if ((prop = reader.GetString()) is not null)
                        {
                            CustomKey = prop;
                            Cause = null;
                            ItemCause = null;
                            VehicleCause = null;
                        }
                    }
                    else if (prop.Equals("translations", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            List<DeathTranslation> translations = new List<DeathTranslation>(5);
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    prop = reader.GetString();
                                    if (reader.Read() && prop is not null && Enum.TryParse(prop, true, out DeathFlags flags)
                                        && reader.TokenType == JsonTokenType.String && (prop = reader.GetString()) is not null)
                                    {
                                        translations.Add(new DeathTranslation(flags, prop));
                                    }
                                }
                            }

                            Translations = translations.ToArray();
                        }
                    }
                }
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
        else if (ItemCause.HasValue)
        {
            writer.WritePropertyName("item-cause");
            ItemCause.Value.Write(writer);
        }
        else if (VehicleCause.HasValue)
        {
            writer.WritePropertyName("vehicle-cause");
            VehicleCause.Value.Write(writer);
        }
        else if (CustomKey is not null)
        {
            writer.WriteString("custom-key", CustomKey);
        }
        else
        {
            writer.WriteProperty("error", true);
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

    public override bool Equals(object obj) => obj is DeathCause c && Equals(c);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Cause, ItemCause, VehicleCause, CustomKey, Translations);
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public static bool operator ==(DeathCause? left, DeathCause? right) => Equals(left, right);
    public static bool operator !=(DeathCause? left, DeathCause? right) => !(left == right);
    public object Clone()
    {
        DeathTranslation[] newTranslations = new DeathTranslation[Translations.Length];
        Array.Copy(Translations, 0, newTranslations, 0, newTranslations.Length);
        return new DeathCause(Cause, ItemCause, VehicleCause, CustomKey, newTranslations);
    }

    public bool Equals(DeathCause other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        if (other.Cause.HasValue != Cause.HasValue)
            return false;
        if (other.Cause.HasValue && other.Cause.Value != Cause!.Value)
            return false;

        if (other.ItemCause.HasValue != ItemCause.HasValue)
            return false;
        if (other.ItemCause.HasValue && !other.ItemCause.Value.Equals(ItemCause!.Value))
            return false;

        if (other.VehicleCause.HasValue != VehicleCause.HasValue)
            return false;
        if (other.VehicleCause.HasValue && !other.VehicleCause.Value.Equals(VehicleCause!.Value))
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
