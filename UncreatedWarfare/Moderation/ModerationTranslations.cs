using System;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Moderation;
public class ModerationTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Moderation";

    [TranslationData("Gets sent to a player when their message gets blocked by the chat filter.", "Section of the message that matched the chat filter.")]
    public readonly Translation<string> ChatFilterFeedback = new Translation<string>("<#ff8c69>Our chat filter flagged <#fdfdfd>{0}</color>, so the message wasn't sent.");

    [TranslationData("Gets sent to a player when their message gets blocked by the chat filter.", "Amount of alphanumeric characters in succession.")]
    public readonly Translation<int> NameFilterKickMessage = new Translation<int>("Your name does not contain enough alphanumeric characters in succession ({0}), please change your name and rejoin.", TranslationOptions.UnityUI | TranslationOptions.NoRichText);

    [TranslationData("Gets sent to a player if they're banned when they join.", "The reason they're banned", "Duration of time they're banned")]
    public readonly Translation<string, TimeSpan> RejectBanned = new Translation<string, TimeSpan>("You are banned for {1}: \"{0}\".", TranslationOptions.UnityUI | TranslationOptions.NoRichText, arg1Fmt: TimeAddon.Create(TimeSpanFormatType.Short));

    [TranslationData("Gets sent to a player if they're permanently banned when they join.", "The reason they're banned")]
    public readonly Translation<string> RejectPermanentBanned = new Translation<string>("You are permanently banned: \"{0}\".", TranslationOptions.UnityUI | TranslationOptions.NoRichText);

    [TranslationData("Gets sent to a player if someone else sharing an IP or HWID is banned when they join.", "The reason they're banned", "Duration of time they're banned", "The banned player's Steam64 ID.", "The banned player's name.")]
    public readonly Translation<string, TimeSpan, IPlayer, IPlayer> RejectLinkedBanned = new Translation<string, TimeSpan, IPlayer, IPlayer>("{3} ({2}) is banned for {1}: \"{0}\".", TranslationOptions.UnityUI | TranslationOptions.NoRichText, arg1Fmt: TimeAddon.Create(TimeSpanFormatType.Short), arg2Fmt: WarfarePlayer.FormatSteam64, arg3Fmt: WarfarePlayer.FormatPlayerName);

    [TranslationData("Gets sent to a player if someone else sharing an IP or HWID is permanently banned when they join.", "The reason they're banned", "The banned player's Steam64 ID.", "The banned player's name.")]
    public readonly Translation<string, IPlayer, IPlayer> RejectPermanentLinkedBanned = new Translation<string, IPlayer, IPlayer>("{2} ({1}) is permanently banned: \"{0}\".", TranslationOptions.UnityUI | TranslationOptions.NoRichText, arg1Fmt: WarfarePlayer.FormatSteam64, arg2Fmt: WarfarePlayer.FormatPlayerName);

    [TranslationData("Gets sent to a player who is globally banned in a known ban database (like UCS).", "Name of the global ban system.", "Ban date.", "Our discord join code.")]
    public readonly Translation<string, DateTime, string, uint> RejectGloballyBanned = new Translation<string, DateTime, string, uint>("You were globally banned in the {0} global ban system {1} (ban ID {3}). Join our Discord (discord.gg/{2}) for help.", arg1Fmt: TimeAddon.Create(DateTimeFormatType.RelativeLong));

    [TranslationData("Gets sent to a player if someone else sharing an IP or HWID is banned in a known ban database (like UCS).", "Name of the global ban system.", "Ban date.", "Our discord join code.", "The banned player's Steam64 ID.", "The banned player's name.")]
    public readonly Translation<string, DateTime, string, ulong, string, uint> RejectGloballyLinkedBanned = new Translation<string, DateTime, string, ulong, string, uint>("{4} ({3}) is globally banned in the {0} global ban system {1} (ban ID {5}). Join our Discord (discord.gg/{2}) for help.", arg1Fmt: TimeAddon.Create(DateTimeFormatType.RelativeLong));

    [TranslationData("Gets sent to a player when they're kicked.", "The reason they're kicked")]
    public readonly Translation<string> RejectKicked = new Translation<string>("You were kicked for \"{0}\".", TranslationOptions.UnityUI | TranslationOptions.NoRichText);

    [TranslationData("Tilte of the warning popup that shows when you get warned.")]
    public readonly Translation WarnPopupTitle = new Translation("<color=#ffff00>Warning</color>", TranslationOptions.TMProUI);

    [TranslationData("Description of the warning popup that shows when you get warned.")]
    public readonly Translation<IPlayer, string> WarnPopupDescription = new Translation<IPlayer, string>("<color=#ffff00>{0} warned you for <color=#ffffff>{1}</color>.</color>", TranslationOptions.TMProUI, arg0Fmt: WarfarePlayer.FormatColoredDisplayOrPlayerName);

    [TranslationData("Description of the warning popup that shows when you get warned when it was done by something other than a player (like the console or an automated system).")]
    public readonly Translation<string> WarnPopupDescriptionNoActor = new Translation<string>("You have been warned for \"{0}\".", TranslationOptions.TMProUI);

    [TranslationData("Gets sent to a player when their nick name gets blocked by the chat filter.", "Violating text.")]
    public readonly Translation<string> NameProfanityNickNameKickMessage = new Translation<string>("Your nickname is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.", TranslationOptions.UnityUI | TranslationOptions.NoRichText);

    [TranslationData("Gets sent to a player when their character name gets blocked by the chat filter.", "Violating text.")]
    public readonly Translation<string> NameProfanityCharacterNameKickMessage = new Translation<string>("Your character name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.", TranslationOptions.UnityUI | TranslationOptions.NoRichText);

    [TranslationData("Gets sent to a player when their player name gets blocked by the chat filter.", "Violating text.")]
    public readonly Translation<string> NameProfanityPlayerNameKickMessage = new Translation<string>("Your Steam name is in violation of our profanity filter: \"{0}\". Please change your name and rejoin.", TranslationOptions.UnityUI | TranslationOptions.NoRichText);



    [TranslationData("Broadcasted when a permanent ban is applied to a player by another player.", "Offender", "Admin")]
    public readonly Translation<IPlayer, IPlayer> BanPermanentSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffcc99><#d8addb>{0}</color> was <b>permanently</b> banned by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredDisplayOrPlayerName);

    [TranslationData("Broadcasted when a permanent ban is applied to a player by something other than a player (like the console or an automated system).", "Offender")]
    public readonly Translation<IPlayer> BanPermanentSuccessBroadcastNoActor = new Translation<IPlayer>("<#ffcc99><#d8addb>{0}</color> was <b>permanently</b> banned.", arg0Fmt: WarfarePlayer.FormatCharacterName);

    [TranslationData("Broadcasted when a non-permanent ban is applied to a player by another player.", "Offender", "Admin", "Duration")]
    public readonly Translation<IPlayer, IPlayer, TimeSpan> BanSuccessBroadcast = new Translation<IPlayer, IPlayer, TimeSpan>("<#ffcc99><#d8addb>{0}</color> was banned for <#9cffb3>{2}</color> by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData("Broadcasted when a non-permanent ban is applied to a player by something other than a player (like the console or an automated system).", "Offender", "Duration")]
    public readonly Translation<IPlayer, TimeSpan> BanSuccessBroadcastNoActor = new Translation<IPlayer, TimeSpan>("<#ffcc99><#d8addb>{0}</color> was banned for <#9cffb3>{1}</color>.", arg0Fmt: WarfarePlayer.FormatCharacterName);


    [TranslationData("Broadcasted when a permanent mute is applied to a player by another player.", "Offender", "Admin", "Type of mute (ex. 'Voice and Text Chat')")]
    public readonly Translation<IPlayer, IPlayer, MuteType> MutePermanentSuccessBroadcast = new Translation<IPlayer, IPlayer, MuteType>("<#ffcc99><#d8addb>{0}</color> was <b>permanently</b> {2} muted by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredDisplayOrPlayerName);

    [TranslationData("Broadcasted when a permanent mute is applied to a player by something other than a player (like the console or an automated system).", "Offender", "Type of mute (ex. 'Voice and Text Chat')")]
    public readonly Translation<IPlayer, MuteType> MutePermanentSuccessBroadcastNoActor = new Translation<IPlayer, MuteType>("<#ffcc99><#d8addb>{0}</color> was <b>permanently</b> {1} muted.", arg0Fmt: WarfarePlayer.FormatCharacterName);

    [TranslationData("Broadcasted when a non-permanent mute is applied to a player by another player.", "Offender", "Admin", "Duration", "Type of mute (ex. 'Voice and Text Chat')")]
    public readonly Translation<IPlayer, IPlayer, TimeSpan, MuteType> MuteSuccessBroadcast = new Translation<IPlayer, IPlayer, TimeSpan, MuteType>("<#ffcc99><#d8addb>{0}</color> was {3} muted for <#9cffb3>{2}</color> by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData("Broadcasted when a non-permanent mute is applied to a player by something other than a player (like the console or an automated system).", "Offender", "Duration", "Type of mute (ex. 'Voice and Text Chat')")]
    public readonly Translation<IPlayer, TimeSpan, MuteType> MuteSuccessBroadcastNoActor = new Translation<IPlayer, TimeSpan, MuteType>("<#ffcc99><#d8addb>{0}</color> was {2} muted for <#9cffb3>{1}</color>.", arg0Fmt: WarfarePlayer.FormatCharacterName);


    [TranslationData("Broadcasted when a player is kicked (removed from the server) by another player.", "Offender", "Admin")]
    public readonly Translation<IPlayer, IPlayer> KickSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffcc99><#d8addb>{0}</color> was kicked by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredPlayerName);
    
    [TranslationData("Broadcasted when a player is kicked (removed from the server) by something other than a player (like the console or an automated system).", "Offender")]
    public readonly Translation<IPlayer> KickSuccessBroadcastNoActor = new Translation<IPlayer>("<#ffcc99><#d8addb>{0}</color> was kicked.", arg0Fmt: WarfarePlayer.FormatCharacterName);


    [TranslationData("Broadcasted when a player is warned (removed from the server) by another player.", "Offender", "Admin")]
    public readonly Translation<IPlayer, IPlayer> WarnSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffff99><#d8addb>{0}</color> was warned by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredPlayerName);
    
    [TranslationData("Broadcasted when a player is warned (removed from the server) by something other than a player (like the console or an automated system).", "Offender")]
    public readonly Translation<IPlayer> WarnSuccessBroadcastNoActor = new Translation<IPlayer>("<#ffff99><#d8addb>{0}</color> was warned.", arg0Fmt: WarfarePlayer.FormatCharacterName);


    [TranslationData("Broadcasted when a player is unbaned by another player.", "Offender", "Admin")]
    public readonly Translation<IPlayer, IPlayer> UnbanSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#9999ff><#d8addb>{0}</color> was unbanned by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData("Broadcasted when a player is unbaned  by something other than a player (like the console or an automated system).", "Offender")]
    public readonly Translation<IPlayer> UnbanSuccessBroadcastNoActor = new Translation<IPlayer>("<#9999ff><#d8addb>{0}</color> was unbanned.", arg0Fmt: WarfarePlayer.FormatCharacterName);


    [TranslationData("Broadcasted when a player is unmuted by another player.", "Offender", "Admin")]
    public readonly Translation<IPlayer, IPlayer> UnmuteSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#9999ff><#d8addb>{0}</color> was unmuted by {1}.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData("Broadcasted when a player is unmuted by something other than a player (like the console or an automated system).", "Offender")]
    public readonly Translation<IPlayer> UnmuteSuccessBroadcastNoActor = new Translation<IPlayer>("<#9999ff><#d8addb>{0}</color> was unmuted.", arg0Fmt: WarfarePlayer.FormatCharacterName);


    [TranslationData("Text on the mute notification UI that shows up next to the voice chat icon.")]
    public readonly Translation MutedUI = new Translation("MUTED", TranslationOptions.TMProUI);
}