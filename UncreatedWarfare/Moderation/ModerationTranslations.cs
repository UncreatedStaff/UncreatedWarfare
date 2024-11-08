using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Moderation;
public class ModerationTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Moderation";

    [TranslationData("Gets sent to a player when their message gets blocked by the chat filter.", "Section of the message that matched the chat filter.")]
    public readonly Translation<string> ChatFilterFeedback = new Translation<string>("<#ff8c69>Our chat filter flagged <#fdfdfd>{0}</color>, so the message wasn't sent.");

    [TranslationData("Gets sent to a player when their message gets blocked by the chat filter.", "Amount of alphanumeric characters in succession.")]
    public readonly Translation<int> NameFilterKickMessage = new Translation<int>("Your name does not contain enough alphanumeric characters in succession ({0}), please change your name and rejoin.");

    [TranslationData("Gets sent to a player if they're banned when they join.", "The reason they're banned", "Duration of time they're banned")]
    public readonly Translation<string, int> RejectBanned = new Translation<string, int>("You are banned for {1}: \"{0}\".", arg1Fmt: TimeAddon.Create(TimeFormatType.Short));

    [TranslationData("Gets sent to a player if they're permanently banned when they join.", "The reason they're banned")]
    public readonly Translation<string> RejectPermanentBanned = new Translation<string>("You are permanently banned: \"{0}\".");

    [TranslationData("Gets sent to a player when their nick name gets blocked by the chat filter.", "Violating text.")]
    public readonly Translation<string> NameProfanityNickNameKickMessage = new Translation<string>("Your nickname is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.");

    [TranslationData("Gets sent to a player when their character name gets blocked by the chat filter.", "Violating text.")]
    public readonly Translation<string> NameProfanityCharacterNameKickMessage = new Translation<string>("Your character name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.");

    [TranslationData("Gets sent to a player when their player name gets blocked by the chat filter.", "Violating text.")]
    public readonly Translation<string> NameProfanityPlayerNameKickMessage = new Translation<string>("Your Steam name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.");
}
