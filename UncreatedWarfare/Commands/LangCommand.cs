using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class LangCommand : Command
{
    private const string SYNTAX = "/lang [current|reset|*language*]";
    private const string HELP = "Switch your language to some of our supported languages.";
    public static event LanguageChanged OnPlayerChangedLanguage;
    public LangCommand() : base("lang", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.HasArgsExact(0))
        {
            StringBuilder sb = new StringBuilder();
            int i = -1;
            foreach (KeyValuePair<string, LanguageAliasSet> setData in Data.LanguageAliases)
            {
                if (!Data.Localization.ContainsKey(setData.Key)) continue; // only show languages with translations
                if (++i != 0) sb.Append(", ");
                sb.Append(setData.Key);
                LanguageAliasSet aliases = setData.Value;
                sb.Append(" : ").Append(aliases.display_name);
            }
            ctx.Reply(T.LanguageList, sb.ToString());
        }
        else if (ctx.MatchParameter(0, "current"))
        {
            ctx.AssertRanByPlayer();

            if (!Data.Languages.TryGetValue(ctx.CallerID, out string langCode))
                langCode = L.DEFAULT;
            if (!Data.LanguageAliases.TryGetValue(langCode, out LanguageAliasSet set))
                set = new LanguageAliasSet(langCode, langCode, Array.Empty<string>());

            ctx.Reply(T.LanguageCurrent, set);
        }
        else if (ctx.MatchParameter(0, "reset"))
        {
            ctx.AssertRanByPlayer();

            if (!Data.LanguageAliases.TryGetValue(L.DEFAULT, out LanguageAliasSet set))
                set = new LanguageAliasSet(L.DEFAULT, L.DEFAULT, Array.Empty<string>());

            if (Data.Languages.TryGetValue(ctx.CallerID, out string oldLang))
            {
                if (!Data.LanguageAliases.TryGetValue(oldLang, out LanguageAliasSet oldSet))
                    oldSet = new LanguageAliasSet(oldLang, oldLang, Array.Empty<string>());

                if (oldLang == L.DEFAULT)
                    throw ctx.Reply(T.LangAlreadySet, set);

                JSONMethods.SetLanguage(ctx.CallerID, L.DEFAULT);
                ctx.LogAction(EActionLogType.CHANGE_LANGUAGE, oldLang + " >> " + L.DEFAULT);
                if (OnPlayerChangedLanguage != null)
                    OnPlayerChangedLanguage.Invoke(ctx.Caller, set, oldSet);
                ctx.Reply(T.ResetLanguage, set);
            }
            else throw ctx.Reply(T.LangAlreadySet, set);
        }
        else if (ctx.TryGetRange(0, out string input) && !string.IsNullOrWhiteSpace(input))
        {
            ctx.AssertRanByPlayer();

            if (!Data.Languages.TryGetValue(ctx.CallerID, out string oldLang))
                oldLang = L.DEFAULT;

            if (!Data.LanguageAliases.TryGetValue(oldLang, out LanguageAliasSet oldSet))
                oldSet = new LanguageAliasSet(oldLang, oldLang, Array.Empty<string>());

            bool found = false;
            if (!Data.LanguageAliases.TryGetValue(input, out LanguageAliasSet newSet))
            {
                foreach (KeyValuePair<string, LanguageAliasSet> set in Data.LanguageAliases)
                {
                    if (set.Key.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                        set.Value.display_name.Equals(input, StringComparison.OrdinalIgnoreCase))
                    {
                        newSet = set.Value;
                        found = true;
                        break;
                    }
                    else
                    {
                        for (int i = 0; i < set.Value.values.Length; ++i)
                        {
                            if (set.Value.values[i].Equals(input, StringComparison.OrdinalIgnoreCase))
                            {
                                newSet = set.Value;
                                found = true;
                                goto brk;
                            }
                        }

                        brk:
                        break;
                    }
                }
            }
            else found = true;

            if (found)
            {
                if (newSet.key.Equals(oldLang, StringComparison.OrdinalIgnoreCase))
                    throw ctx.Reply(T.LangAlreadySet, oldSet);

                JSONMethods.SetLanguage(ctx.CallerID, newSet.key);
                ctx.LogAction(EActionLogType.CHANGE_LANGUAGE, oldLang + " >> " + newSet.key);
                if (OnPlayerChangedLanguage != null)
                    OnPlayerChangedLanguage.Invoke(ctx.Caller, newSet, oldSet);
                ctx.Reply(T.ChangedLanguage, newSet);
            }
            else throw ctx.Reply(T.LanguageNotFound, input);
        }
        else throw ctx.Reply(T.ResetLanguageHow);
    }
}
public delegate void LanguageChanged(UCPlayer player, LanguageAliasSet newLanguage, LanguageAliasSet oldLanguage);