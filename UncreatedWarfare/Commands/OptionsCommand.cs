using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

[Command("options", "settings", "option", "config")]
[MetadataFile(nameof(GetHelpMetadata))]
public sealed class OptionsCommand : IExecutableCommand
{
    private const string Syntax = "/options <imgui> [value]";
    private const string Help = "Configure player-specific settings.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Configure player-specific settings.",
            Parameters =
            [
                new CommandParameter("IMGUI")
                {
                    Aliases = [ "legacyui", "oldui" ],
                    Description = "Enables chat support for the <nobr><b>-Glazier IMGUI</b></nobr> launch option. Allows support for some special characters.",
                    Parameters =
                    [
                        new CommandParameter("Enabled", typeof(bool))
                    ]
                },
                new CommandParameter("TrackQuests")
                {
                    Aliases = [ "dailyquests", "quests" ],
                    Description = "Keeps the quest UI from showing up by default.",
                    Parameters =
                    [
                        new CommandParameter("Enabled", typeof(bool))
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
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
    }
}