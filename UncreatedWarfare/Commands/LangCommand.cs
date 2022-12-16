using System;
using System.Text;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class LangCommand : Command
{
    private const string Syntax = "/lang [current|reset|*language*]";
    private const string Help = "Switch your language to some of our supported languages.";
    public static event LanguageChanged? OnPlayerChangedLanguage;
    public LangCommand() : base("lang", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        if (ctx.HasArgsExact(0))
        {
            StringBuilder sb = new StringBuilder();
            int i = -1;
            foreach (LanguageAliasSet setData in Data.LanguageAliases)
            {
                // only show languages with translations
                if (!T.AllLanguages.Exists(x => x.Equals(setData.key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (++i != 0) sb.Append(", ");
                sb.Append(setData.key);
                sb.Append(" : ").Append(setData.display_name);
            }
            ctx.Reply(T.LanguageList, sb.ToString());
        }
        else if (ctx.MatchParameter(0, "current"))
        {
            ctx.AssertRanByPlayer();
            
            ctx.Reply(T.LanguageCurrent, Localization.FindLanguageSet(Localization.GetLang(ctx.CallerID))!);
        }
        else if (ctx.MatchParameter(0, "reset"))
        {
            ctx.AssertRanByPlayer();

            LanguageAliasSet set = Localization.DefaultSet;

            if (Data.Languages.TryGetValue(ctx.CallerID, out string oldLang))
            {
                LanguageAliasSet oldSet = Localization.FindLanguageSet(oldLang) ?? new LanguageAliasSet(oldLang, oldLang, Array.Empty<string>());

                if (oldSet.key.Equals(L.Default, StringComparison.Ordinal))
                    throw ctx.Reply(T.LangAlreadySet, set);

                JSONMethods.SetLanguage(ctx.CallerID, L.Default);
                ctx.LogAction(EActionLogType.CHANGE_LANGUAGE, oldLang + " >> " + L.Default);
                OnPlayerChangedLanguage?.Invoke(ctx.Caller, set, oldSet);
                ctx.Reply(T.ResetLanguage, set);
            }
            else throw ctx.Reply(T.LangAlreadySet, set);
        }
        else if (ctx.TryGetRange(0, out string input) && !string.IsNullOrWhiteSpace(input))
        {
            ctx.AssertRanByPlayer();
            
            string oldLang = Localization.GetLang(ctx.CallerID);
            LanguageAliasSet oldSet = Localization.FindLanguageSet(oldLang) ?? new LanguageAliasSet(oldLang, oldLang, Array.Empty<string>());

            LanguageAliasSet? newSet = Localization.FindLanguageSet(input);

            if (newSet != null)
            {
                if (newSet.key.Equals(oldLang, StringComparison.OrdinalIgnoreCase))
                    throw ctx.Reply(T.LangAlreadySet, oldSet);

                JSONMethods.SetLanguage(ctx.CallerID, newSet.key);
                ctx.LogAction(EActionLogType.CHANGE_LANGUAGE, oldLang + " >> " + newSet.key);
                OnPlayerChangedLanguage?.Invoke(ctx.Caller, newSet, oldSet);
                ctx.Reply(T.ChangedLanguage, newSet);
            }
            else throw ctx.Reply(T.LanguageNotFound, input);
        }
        else throw ctx.Reply(T.ResetLanguageHow);
    }
}
public delegate void LanguageChanged(UCPlayer player, LanguageAliasSet newLanguage, LanguageAliasSet oldLanguage);