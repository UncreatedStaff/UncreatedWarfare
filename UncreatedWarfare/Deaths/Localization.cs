using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Uncreated.Framework;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Deaths;
internal static class Localization
{
    /*
     * {0} =  Dead player's name
     * {1} ?= Killer's name
     * {2} ?= Limb name
     * {3} ?= Item Name
     * {4} ?= Kill Distance
     * {5} ?= Player 3
     * {6} ?= Item 2
     */
    private static readonly Dictionary<string, DeathCause[]> DeathTranslations = new Dictionary<string, DeathCause[]>(48);

    private static readonly DeathCause[] DefaultValues = new DeathCause[]
    {
        new DeathCause(EDeathCause.ACID, new DeathTranslation(EDeathFlags.NONE, "{0} was burned by an acid zombie.")),
        new DeathCause(EDeathCause.ANIMAL, new DeathTranslation(EDeathFlags.NONE, "{0} was attacked by an animal.")),
        new DeathCause(EDeathCause.ARENA, new DeathTranslation(EDeathFlags.NONE, "{0} stepped outside the arena boundary.")),
        new DeathCause(EDeathCause.BLEEDING)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} bled out."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} bled out because of {1}."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} bled out from a {3}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} bled out because of {1} from a {3}."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} bled out by their own hand."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} bled out by their own hand from a {3}."),
            }
        },
        new DeathCause(EDeathCause.BONES, new DeathTranslation(EDeathFlags.NONE, "{0} fell to their death.")),
        new DeathCause(EDeathCause.BOULDER, new DeathTranslation(EDeathFlags.NONE, "{0} was crushed by a mega zombie.")),
        new DeathCause(EDeathCause.BREATH, new DeathTranslation(EDeathFlags.NONE, "{0} asphyxiated.")),
        new DeathCause(EDeathCause.BURNER, new DeathTranslation(EDeathFlags.NONE, "{0} was burned by a mega zombie.")),
        new DeathCause(EDeathCause.BURNING, new DeathTranslation(EDeathFlags.NONE, "{0} burned to death.")),
        new DeathCause(EDeathCause.CHARGE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was blown up by a demolition charge."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was blown up by a {3}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{1} blew up {0} with a {3}."),
                new DeathTranslation(EDeathFlags.KILLER, "{1} blew up {0} with a demolition charge."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} blew themselves up with a demolition charge."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} blew themselves up with a {3}."),
            }
        },
        new DeathCause(EDeathCause.FOOD, new DeathTranslation(EDeathFlags.NONE, "{0} starved to death.")),
        new DeathCause(EDeathCause.FREEZING, new DeathTranslation(EDeathFlags.NONE, "{0} froze to death.")),
        new DeathCause(EDeathCause.GRENADE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was blown up by a grenade."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was blown up by a {3}."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was blown up by {1} with a grenade."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} was blown up by {1} with a {3}."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} blew themselves up with a grenade."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} blew themselves up with a {3}."),
            }
        },
        new DeathCause(EDeathCause.GUN)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was shot in the {2}."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was shot with a {3} in the {2} from {4}m away."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was shot by {1} in the {2} from {4}m away."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{1} shot {0} with a {3} in the {2} from {4}m away."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} shot themselves in the {2}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} shot themselves in the {2} with a {3}."),
            }
        },
        new DeathCause(EDeathCause.INFECTION, new DeathTranslation(EDeathFlags.NONE, "{0} died to an infection.")),
        new DeathCause(EDeathCause.KILL)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was killed by an operator."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was killed by an admin, {1}."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} killed themselves as an admin."),
            }
        },
        new DeathCause(EDeathCause.KILL)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was killed by an operator."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was killed by an admin, {1}."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} killed themselves as an admin."),
            }
        },
        new DeathCause(EDeathCause.LANDMINE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was blown up by a landmine."),
                new DeathTranslation(EDeathFlags.ITEM2, "{0} was blown up by a landmine triggered by a {6}."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was blown up by a {3}."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.ITEM2, "{0} was blown up by a {3} triggered by a {6}."),
                new DeathTranslation(EDeathFlags.PLAYER3, "{0} was blown up by a landmine triggered by {5}."),
                new DeathTranslation(EDeathFlags.PLAYER3 | EDeathFlags.ITEM2, "{0} was blown up by a landmine triggered by {5} using a {6}."),
                new DeathTranslation(EDeathFlags.PLAYER3 | EDeathFlags.ITEM, "{0} was blown up by a {3} triggered by {5}."),
                new DeathTranslation(EDeathFlags.PLAYER3 | EDeathFlags.ITEM | EDeathFlags.ITEM2, "{0} was blown up by a {3} triggered by {5} using a {6}."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was blown up by {1}'s landmine."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM2, "{0} was blown up by {1}'s landmine triggered with a {6}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} was blown up by {1}'s {3}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM | EDeathFlags.ITEM2, "{0} was blown up by {1}'s {3} triggered with a {6}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.PLAYER3, "{0} was blown up by {1}'s landmine that was triggered by {5}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM2 | EDeathFlags.PLAYER3, "{0} was blown up by {1}'s landmine that was triggered by {5} using a {6}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM | EDeathFlags.PLAYER3, "{0} was blown up by {1}'s {3} that was triggered by {5}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM | EDeathFlags.ITEM2 | EDeathFlags.PLAYER3, "{0} was blown up by {1}'s {3} that was triggered by {5} using a {6}."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} blew themselves up with a landmine."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM2, "{0} blew themselves up with a landmine triggered using a {6}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} blew themselves up with a {3}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM | EDeathFlags.ITEM2, "{0} blew themselves up with a {3} triggered using a {6}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.PLAYER3, "{0} was blown up with their landmine that was triggered by {5}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM2 | EDeathFlags.PLAYER3, "{0} was blown up with their landmine that was triggered by {5} using a {6}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM | EDeathFlags.PLAYER3, "{0} was blown up with their {3} that was triggered by {5}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM | EDeathFlags.ITEM2 | EDeathFlags.PLAYER3, "{0} was blown up with their {3} that was triggered by {5} uing a {6}."),
            }
        },
        new DeathCause(EDeathCause.MELEE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was struck in the {2}."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was struck by a {3} in the {2}."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was struck by {1} in the {2}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} was struck by {1} with a {3} in the {2}."),
            }
        },
        new DeathCause(EDeathCause.MISSILE)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was hit by a missile."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was hit by a {3} from {4}m away."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was hit by {1}'s missile from {4}m away."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} was hit by {1}'s {3} from {4}m away."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} blew themselves up with a missile."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} blew themselves up with a {3}."),
            }
        },
        new DeathCause(EDeathCause.PUNCH)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was pummeled to death."),
                new DeathTranslation(EDeathFlags.KILLER, "{1} punched {0} to death.")
            }
        },
        new DeathCause(EDeathCause.ROADKILL)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was ran over."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was ran over by a {3}."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.KILLER, "{0} was ran over by {1} using a {3} going {4} mph."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} ran themselves over."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} ran themselves over using a {3} going {4} mph."),
            }
        },
        new DeathCause(EDeathCause.SENTRY)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was killed by a sentry."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was killed by a sentry's {3}."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} was killed by {1}'s sentry's {3}."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} was killed by their own sentry."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} was killed by their own sentry's {3}."),
            }
        },
        new DeathCause(EDeathCause.SHRED)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was shredded by wire."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was shredded by {3}."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was shredded by {1}'s wire."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} was shredded by {1}'s {3}."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} was shredded by their own wire."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} was shredded by their own {3}."),
            }
        },
        new DeathCause(EDeathCause.SPARK, new DeathTranslation(EDeathFlags.NONE, "{0} was shocked by a mega zombie.")),
        new DeathCause(EDeathCause.SPIT, new DeathTranslation(EDeathFlags.NONE, "{0} was killed by a spitter zombie.")),
        new DeathCause(EDeathCause.SPLASH)
        {
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was killed by fragmentation."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was killed by {3} fragmentation from {4}m away."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.KILLER, "{0} was killed {1}'s {3} fragmentation from {4}m away."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} killed themselves with fragmentation."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} killed themselves with {3} fragmentation."),
            }
        },
        new DeathCause(EDeathCause.SUICIDE, new DeathTranslation(EDeathFlags.NONE, "{0} commited suicide.")),
        new DeathCause(EDeathCause.VEHICLE)
        {
            Translations = new DeathTranslation[]
            {
                // ITEM {3} = vehicle name, ITEM2 {6} = item name
                new DeathTranslation(EDeathFlags.NONE, "{0} was blown up inside a vehicle."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was blown up inside a {3}."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.ITEM2 | EDeathFlags.KILLER, "{0} was blown up by {1} inside a {3} with a {6} from {4}m away."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.ITEM2 | EDeathFlags.PLAYER3, "{0} was blown up inside {5}'s {3} with a {6} from {4}m away."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.ITEM2 | EDeathFlags.KILLER | EDeathFlags.PLAYER3, "{0} was blown up by {1} inside {5}'s {3} with a {6} from {4}m away."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.ITEM2, "{0} was blown up inside a {3} with a {6}."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.KILLER | EDeathFlags.PLAYER3, "{0} was blown up by {1} inside {5}'s {3} from {4}m away."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.PLAYER3, "{0} was blown up inside {5}'s {3} from {4}m away."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.KILLER, "{0} was blown up by {1} inside a {3} from {4}m away."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} went down with their vehicle."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} went down with their {3}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM | EDeathFlags.ITEM2, "{0} went down with their {3} using a {6}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM | EDeathFlags.ITEM2 | EDeathFlags.PLAYER3, "{0} went down with {5}'s {3} using a {6}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM | EDeathFlags.PLAYER3, "{0} went down with {5}'s {3}."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.PLAYER3, "{0} went down with {5}'s vehicle."),
            }
        },
        new DeathCause(EDeathCause.WATER, new DeathTranslation(EDeathFlags.NONE, "{0} dehydrated.")),
        new DeathCause(EDeathCause.ZOMBIE, new DeathTranslation(EDeathFlags.NONE, "{0} was mauled by a zombie.")),
        new DeathCause()
        {
            CustomKey = "maincamp",
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} died trying to main-camp."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} tried to main-camp with a {3}."),
                new DeathTranslation(EDeathFlags.ITEM | EDeathFlags.KILLER, "{0} tried to main-camp {1} with a {3} from {4}m away."),
            }
        },
        new DeathCause()
        {
            CustomKey = "maindeath",
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} died trying to enter their enemy's base.")
            }
        },
        new DeathCause() // mortar override
        {
            ItemCause = new DynamicAssetValue<ItemAsset>(new Guid("d6424d034309417dbc5f17814af905a8")).GetValue(),
            Translations = new DeathTranslation[]
            {
                new DeathTranslation(EDeathFlags.NONE, "{0} was blown up by a mortar shell."),
                new DeathTranslation(EDeathFlags.ITEM, "{0} was blown up by a mortar shell."),
                new DeathTranslation(EDeathFlags.KILLER, "{0} was blown up by {1}'s mortar from {4}m away."),
                new DeathTranslation(EDeathFlags.KILLER | EDeathFlags.ITEM, "{0} was blown up by {1}'s mortar from {4}m away."),
                new DeathTranslation(EDeathFlags.SUICIDE, "{0} blew themselves up with a mortar shell."),
                new DeathTranslation(EDeathFlags.SUICIDE | EDeathFlags.ITEM, "{0} blew themselves up with a mortar shell."),
            }
        }
    };
    public static void BroadcastDeath(PlayerDied e, DeathMessageArgs args)
    {
        bool sentInConsole = false;
        // red if its a teamkill, otherwise white
        Color color = UCWarfare.GetColor((args.Flags & EDeathFlags.SUICIDE) != EDeathFlags.SUICIDE && args.isTeamkill ? "death_background_teamkill" : "death_background");
        foreach (LanguageSet set in Translation.EnumerateLanguageSets())
        {
            string msg = TranslateMessage(set.Language, args);
            if (!sentInConsole && set.Language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
            {
                L.Log(CommandContext.RemoveRichText(msg), ConsoleColor.DarkCyan);
                sentInConsole = true;
            }
            while (set.MoveNext())
            {
                Chat.SendSingleMessage(msg, color, EChatMode.SAY, null, true, set.Next.Player.channel.owner);
            }
        }

        if (!sentInConsole)
            L.Log(CommandContext.RemoveRichText(TranslateMessage(JSONMethods.DEFAULT_LANGUAGE, args)), ConsoleColor.DarkCyan);

        EventDispatcher.InvokeOnPlayerDied(e);
    }
    internal static void Reload()
    {
        string[] langDirs = Directory.GetDirectories(Data.LangStorage, "*", SearchOption.TopDirectoryOnly);

        F.CheckDir(Data.LangStorage + JSONMethods.DEFAULT_LANGUAGE, out bool folderIsThere);
        if (folderIsThere)
        {
            if (!File.Exists(Data.LangStorage + JSONMethods.DEFAULT_LANGUAGE + @"\deaths.json"))
            {
                using (FileStream stream = File.Create(Data.LangStorage + JSONMethods.DEFAULT_LANGUAGE + @"\deaths.json"))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartArray();
                    for (int i = 0; i < DefaultValues.Length; ++i)
                    {
                        DefaultValues[i].WriteJson(writer);
                    }
                    writer.WriteEndArray();
                    writer.Dispose();
                }
            }
            foreach (string folder in langDirs)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folder);
                string lang = directoryInfo.Name;
                FileInfo[] langFiles = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                foreach (FileInfo info in langFiles)
                {
                    if (info.Name == "deaths.json")
                    {
                        if (DeathTranslations.ContainsKey(lang)) continue;
                        using (FileStream stream = info.OpenRead())
                        {
                            if (stream.Length > int.MaxValue)
                                continue;
                            byte[] bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, bytes.Length);
                            Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                            List<DeathCause> causes = new List<DeathCause>(DefaultValues.Length);
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
                            causes.Clear();
                        }
                    }
                }
            }
        }
    }
    public static string TranslateMessage(string language, DeathMessageArgs args)
    {
        if (string.IsNullOrEmpty(args.ItemName)) args.Flags &= ~EDeathFlags.ITEM;
        if (string.IsNullOrEmpty(args.Item2Name)) args.Flags &= ~EDeathFlags.ITEM2;
        if (string.IsNullOrEmpty(args.KillerName)) args.Flags &= ~EDeathFlags.KILLER;
        if (string.IsNullOrEmpty(args.Player3Name)) args.Flags &= ~EDeathFlags.PLAYER3;
        DeathCause[] causes;
        bool isDefault = false;
        if (DeathTranslations.Count == 0)
        {
            isDefault = true;
            causes = DefaultValues;
        }
        else DeathTranslations.TryGetValue(language, out causes);
        if (causes is null)
            return args.DeathCause.ToString() + " Dead: " + args.DeadPlayerName;
    rtn:
        int i = FindDeathCause(causes, ref args);
        if (i == -1)
        {
            if (language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
            {
                isDefault = true;
                causes = DefaultValues;
            }
            else
                DeathTranslations.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out causes);
            i = FindDeathCause(causes, ref args);
            if (i == -1)
                return args.DeathCause.ToString() + " Dead: " + args.DeadPlayerName;
        }

        DeathCause cause = causes[i];
        string? val = Translate(language, cause, args);
        if (val is null)
        {
            if (isDefault) return args.DeathCause.ToString() + " Dead: " + args.DeadPlayerName;
            causes = DefaultValues;
            isDefault = true;
            goto rtn;
        }
        return val;
    }
    private static int FindDeathCause(DeathCause[] causes, ref DeathMessageArgs args)
    {
        Guid guid;
        bool item = (guid = args.ItemGuid) != default;
        string? specKey = args.SpecialKey;
        if (specKey is not null)
        {
            for (int i = 0; i < causes.Length; ++i)
            {
                DeathCause cause = causes[i];
                if (cause.CustomKey is not null && cause.CustomKey.Equals(specKey, StringComparison.Ordinal))
                    return i;
            }
        }
        if (item)
        {
            if (args.ItemIsVehicle)
            {
                for (int i = 0; i < causes.Length; ++i)
                {
                    DeathCause cause = causes[i];
                    if (cause.VehicleCause.HasValue && cause.VehicleCause.Value.IsMatch(guid))
                        return i;
                }
            }
            else
            {
                for (int i = 0; i < causes.Length; ++i)
                {
                    DeathCause cause = causes[i];
                    if (cause.ItemCause.HasValue && cause.ItemCause.Value.IsMatch(guid))
                        return i;
                }
            }
        }

        EDeathCause cause2 = args.DeathCause;
        for (int i = 0; i < causes.Length; ++i)
        {
            DeathCause cause = causes[i];
            if (cause.Cause.HasValue && cause.Cause == cause2)
                return i;
        }

        return -1;
    }
    private static string? Translate(string language, DeathCause cause, DeathMessageArgs args)
    {
        EDeathFlags flags = args.Flags;
        for (int i = 0; i < cause.Translations.Length; ++i)
        {
            ref DeathTranslation d = ref cause.Translations[i];
            if (d.Flags == flags)
                return args.Translate(d.Value, language);
        }
        if ((flags & EDeathFlags.KILLER) == EDeathFlags.KILLER)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == EDeathFlags.KILLER)
                    return args.Translate(d.Value, language);
            }
        }
        else if ((flags & EDeathFlags.SUICIDE) == EDeathFlags.SUICIDE)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == EDeathFlags.SUICIDE)
                    return args.Translate(d.Value, language);
            }
        }
        else if ((flags & EDeathFlags.PLAYER3) == EDeathFlags.PLAYER3)
        {
            for (int i = 0; i < cause.Translations.Length; ++i)
            {
                ref DeathTranslation d = ref cause.Translations[i];
                if (d.Flags == EDeathFlags.PLAYER3)
                    return args.Translate(d.Value, language);
            }
        }
        for (int i = 0; i < cause.Translations.Length; ++i)
        {
            ref DeathTranslation d = ref cause.Translations[i];
            if (d.Flags == EDeathFlags.NONE)
                return args.Translate(d.Value, language);
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
    public bool ItemIsVehicle;
    public float KillDistance;
    public string? Player3Name;
    public ulong Player3Team;
    public string? Item2Name;
    public EDeathFlags Flags;
    public bool isTeamkill;
    internal string Translate(string template, string language)
    {
        object[] format = new object[7];
        format[0] = DeadPlayerName.Colorize(TeamManager.GetTeamHexColor(DeadPlayerTeam));
        format[1] = KillerName is null ? string.Empty : KillerName.Colorize(TeamManager.GetTeamHexColor(KillerTeam));
        format[2] = Translation.TranslateEnum(Limb, language);
        format[3] = ItemName is null ? string.Empty : ItemName;
        format[4] = KillDistance.ToString("F0", Data.Locale);
        format[5] = Player3Name is null ? string.Empty : Player3Name.Colorize(TeamManager.GetTeamHexColor(Player3Team));
        format[6] = Item2Name is null ? string.Empty : Item2Name;
        try
        {
            return string.Format(template, format);
        }
        catch (FormatException)
        {
            L.LogWarning("Formatting error for template: \"" + template + "\" (" + DeathCause.ToString() + ":" + Flags.ToString() + ":" + language.ToUpper() + ").");
            return template.Replace("{0}", (string)format[0]);
        }
    }
}

public class DeathCause : IJsonReadWrite
{
    public EDeathCause? Cause;
    public DynamicAssetValue<ItemAsset>.Choice? ItemCause;
    public DynamicAssetValue<VehicleAsset>.Choice? VehicleCause;
    public string? CustomKey;
    public DeathTranslation[] Translations;

    public DeathCause() { }

    public DeathCause(EDeathCause cause)
    {
        this.Cause = cause;
    }
    public DeathCause(EDeathCause cause, DeathTranslation translation) : this (cause, new DeathTranslation[] { translation }) { }
    public DeathCause(EDeathCause cause, DeathTranslation[] translations) : this(cause)
    {
        this.Translations = translations;
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        string? prop;
        if (reader.TokenType != JsonTokenType.StartObject) reader.Read();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                prop = reader.GetString();
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
                                    if (reader.Read() && prop is not null && Enum.TryParse(prop, true, out EDeathFlags flags) 
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
}
public readonly struct DeathTranslation
{
    public readonly EDeathFlags Flags;
    public readonly string Value;
    public DeathTranslation(EDeathFlags flags, string value)
    {
        this.Flags = flags;
        this.Value = value;
    }
}

[Flags]
public enum EDeathFlags : byte
{
    NONE = 0,
    ITEM = 1,
    KILLER = 2,
    SUICIDE = 4,
    PLAYER3 = 8,
    ITEM2 = 16,
}
