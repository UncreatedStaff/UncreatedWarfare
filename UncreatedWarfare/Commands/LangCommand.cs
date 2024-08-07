using System.Text;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Commands;

[Command("lang", "language", "foreign")]
[MetadataFile(nameof(GetHelpMetadata))]
public class LangCommand : IExecutableCommand
{
    private const string Syntax = "/lang [current|reset|*language*]";
    private const string Help = "Switch your language to some of our supported languages.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Switch your language to some of our supported languages or see a list.",
            Parameters =
            [
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
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - " + Help);
        
        if (Context.HasArgsExact(0))
        {
            int i = -1;

            await Data.LanguageDataStore.WriteWaitAsync(token);

            StringBuilder sb = new StringBuilder();
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

            Context.Reply(T.LanguageList, sb.ToString());
        }
        else if (Context.MatchParameter(0, "refersh", "reload", "update"))
        {
            UCWarfare.I.UpdateLangs(Context.Player, false);

            Context.Reply(T.LanguageRefreshed);
        }
        else if (Context.MatchParameter(0, "current"))
        {
            Context.AssertRanByPlayer();

            LanguageInfo info = await Localization.GetLanguage(Context.CallerId.m_SteamID, token).ConfigureAwait(false);
            Context.Reply(T.LanguageCurrent, info);
        }
        else if (Context.MatchParameter(0, "reset"))
        {
            Context.AssertRanByPlayer();
            
            if (Context.Player.Locale.IsDefaultLanguage)
                throw Context.Reply(T.LangAlreadySet, Context.Player.Locale.LanguageInfo);

            LanguageInfo defaultInfo = Localization.GetDefaultLanguage();

            await Context.Player.Locale.Update(defaultInfo.Code, Data.LocalLocale, token: token).ConfigureAwait(false);
            Context.Reply(T.ResetLanguage, defaultInfo);
            CheckIMGUIRequirements(defaultInfo);
        }
        else if (Context.TryGetRange(0, out string? input) && !string.IsNullOrWhiteSpace(input))
        {
            Context.AssertRanByPlayer();

            LanguageInfo? newSet = Data.LanguageDataStore.GetInfoCached(input, false);

            if (newSet == null)
                throw Context.Reply(T.LanguageNotFound, input);

            LanguageInfo oldSet = await Localization.GetLanguage(Context.CallerId.m_SteamID, token).ConfigureAwait(false);
            if (newSet == oldSet)
                throw Context.Reply(T.LangAlreadySet, oldSet);

            await Context.Player.Locale.Update(newSet.Code, null, token: token).ConfigureAwait(false);
            CheckIMGUIRequirements(newSet);
            Context.Reply(T.ChangedLanguage, newSet);
        }
        else throw Context.Reply(T.ResetLanguageHow);
    }
    private void CheckIMGUIRequirements(LanguageInfo newSet)
    {
        if (Context.Player.Save.IMGUI && !newSet.RequiresIMGUI)
        {
            Context.Reply(T.NoIMGUITip1, newSet);
            Context.Reply(T.NoIMGUITip2);
        }
        else if (!Context.Player.Save.IMGUI && newSet.RequiresIMGUI)
        {
            Context.Reply(T.IMGUITip1, newSet);
            Context.Reply(T.IMGUITip2);
        }
    }
}