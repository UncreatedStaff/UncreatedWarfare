using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Models.Localization;

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

    public override async Task Execute(CommandContext ctx, CancellationToken token)
    {
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        
        if (ctx.HasArgsExact(0))
        {
            StringBuilder sb = new StringBuilder();
            int i = -1;
            Data.LanguageDataStore.WriteWait();
            try
            {
                foreach (LanguageInfo info in Data.LanguageDataStore.Languages)
                {
                    if (!info.HasTranslationSupport)
                        continue;

                    if (++i != 0) sb.Append(", ");
                    sb.Append(info.Code);
                    sb.Append(" : ").Append(info.DisplayName);
                }
            }
            finally
            {
                Data.LanguageDataStore.WriteRelease();
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

            await ctx.Caller.Locale.Update(defaultInfo.Code, Data.LocalLocale, token: token).ConfigureAwait(false);
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

            await ctx.Caller.Locale.Update(newSet.Code, null, token: token).ConfigureAwait(false);
            CheckIMGUIRequirements(ctx, newSet);
            ctx.Reply(T.ChangedLanguage, newSet);
        }
        else throw ctx.Reply(T.ResetLanguageHow);
    }
    private static void CheckIMGUIRequirements(CommandContext ctx, LanguageInfo newSet)
    {
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