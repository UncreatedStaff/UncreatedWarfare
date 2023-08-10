using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands;
public class LangCommand : AsyncCommand
{
    private const string Syntax = "/lang [current|reset|*language*]";
    private const string Help = "Switch your language to some of our supported languages.";

    public LangCommand() : base("lang", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = "Switch your language to some of our supported languages or see a list.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Current")
                {
                    IsOptional = true,
                    Description = "See your current language."
                },
                new CommandParameter("Reset")
                {
                    IsOptional = true,
                    Description = "Changes your language back to default."
                },
                new CommandParameter("Language", typeof(string))
                {
                    IsOptional = true,
                    Description = "Changes your language to your choice of supported language."
                }
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
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
        else if (ctx.MatchParameter(0, "refersh", "reload", "update"))
        {
            UCWarfare.I.UpdateLangs(ctx.Caller, false);

            ctx.Reply(T.LanguageRefreshed);
        }
        else if (ctx.MatchParameter(0, "current"))
        {
            ctx.AssertRanByPlayer();

            LanguageInfo info = await Localization.GetLanguage(ctx.CallerID, token).ConfigureAwait(false);
            ctx.Reply(T.LanguageCurrent, info);
        }
        else if (ctx.MatchParameter(0, "reset"))
        {
            ctx.AssertRanByPlayer();
            
            if (ctx.Caller.Locale.IsDefaultLanguage)
                throw ctx.Reply(T.LangAlreadySet, ctx.Caller.Locale.LanguageInfo);

            LanguageInfo defaultInfo = Localization.GetDefaultLanguage();

            await ctx.Caller.Locale.Update(defaultInfo.LanguageCode, Data.LocalLocale, token: token).ConfigureAwait(false);
            ctx.Reply(T.ResetLanguage, defaultInfo);
            CheckIMGUIRequirements(ctx, defaultInfo);
        }
        else if (ctx.TryGetRange(0, out string input) && !string.IsNullOrWhiteSpace(input))
        {
            ctx.AssertRanByPlayer();

            LanguageInfo? newSet = Data.LanguageDataStore.GetInfoCached(input, false);

            if (newSet == null)
                throw ctx.Reply(T.LanguageNotFound, input);

            LanguageInfo oldSet = await Localization.GetLanguage(ctx.CallerID, token).ConfigureAwait(false);
            if (newSet == oldSet)
                throw ctx.Reply(T.LangAlreadySet, oldSet);

            await ctx.Caller.Locale.Update(newSet.LanguageCode, null, token: token).ConfigureAwait(false);
            CheckIMGUIRequirements(ctx, newSet);
            ctx.Reply(T.ChangedLanguage, newSet);
        }
        else throw ctx.Reply(T.ResetLanguageHow);
    }
    private static void CheckIMGUIRequirements(CommandInteraction ctx, LanguageInfo newSet)
    {
        JSONMethods.SetLanguage(ctx.CallerID, newSet.LanguageCode);
        if (ctx.Caller.Save.IMGUI && !newSet.RequiresIMGUI)
        {
            ctx.Reply(T.NoIMGUITip1, newSet);
            ctx.Reply(T.NoIMGUITip2);
        }
        else if (!ctx.Caller.Save.IMGUI && newSet.RequiresIMGUI)
        {
            ctx.Reply(T.IMGUITip1, newSet);
            ctx.Reply(T.IMGUITip2);
        }
    }
}
public delegate void LanguageChanged(UCPlayer player, LanguageAliasSet newLanguage, LanguageAliasSet oldLanguage);