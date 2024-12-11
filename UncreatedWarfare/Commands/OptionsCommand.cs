using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("options", "settings", "option", "config"), MetadataFile]
internal sealed class OptionsCommand : IExecutableCommand
{
    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
#if false
        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, Syntax + " - " + Help);
        if (!Context.HasArgs(2))
        {
            TeamSelector.OpenOptionsMenu(Context.Player);
            Context.Defer();
            return default;
        }

        if (Context.MatchParameter(0, "imgui", "legacyui", "oldui"))
        {
            if (!Context.TryGet(1, out bool value))
            {
                throw Context.Reply(T.OptionsInvalidValue, Context.Get(0)!.ToUpperInvariant(), typeof(bool));
            }

            if (PlayerSave.TryReadSaveFile(Context.CallerId.m_SteamID, out PlayerSave save))
            {
                if (save.IMGUI == value)
                    throw Context.Reply(T.OptionsAlreadySet, "IMGUI", Translation.ToString(value, Context.Player.Locale.LanguageInfo, Context.Player.Locale.CultureInfo, null, Context.Player, TranslationFlags.None));
                save.IMGUI = value;
            }
            else
            {
                save = new PlayerSave(Context.Player)
                {
                    IMGUI = value
                };
            }

            PlayerSave.WriteToSaveFile(save);
            Context.Reply(T.OptionsSet, "IMGUI", Translation.ToString(value, Context.Player.Locale.LanguageInfo, Context.Player.Locale.CultureInfo, null, Context.Player, TranslationFlags.None));
        }
        else if (Context.MatchParameter(0, "quests", "dailyquests", "trackquests"))
        {
            if (!Context.TryGet(1, out bool value))
                throw Context.Reply(T.OptionsInvalidValue, Context.Get(0)!.ToUpperInvariant(), typeof(bool));
            if (PlayerSave.TryReadSaveFile(Context.CallerId.m_SteamID, out PlayerSave save))
            {
                if (save.TrackQuests == value)
                    throw Context.Reply(T.OptionsAlreadySet, "TrackQuests", Translation.ToString(value, Context.Player.Locale.LanguageInfo, Context.Player.Locale.CultureInfo, null, Context.Player, TranslationFlags.None));
                save.TrackQuests = value;
            }
            else
            {
                save = new PlayerSave(Context.Player)
                {
                    TrackQuests = value
                };
            }

            PlayerSave.WriteToSaveFile(save);
            Context.Reply(T.OptionsSet, "TrackQuests", Translation.ToString(value, Context.Player.Locale.LanguageInfo, Context.Player.Locale.CultureInfo, null, Context.Player, TranslationFlags.None));
            DailyQuests.CheckTrackQuestsOption(Context.Player);
        }
        else
            throw Context.SendCorrectUsage(Syntax);

        return default;
#endif
        return UniTask.CompletedTask;
    }
}