using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

internal sealed class FtdmTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "FreeTDM";

    [TranslationData("Sent when a player tries to enter enemy spawn during an FTDM match.", "The team that owns the spawn they're entering.")]
    public readonly Translation<FactionInfo> EnteredTeamSpawn = new Translation<FactionInfo>("You are not permitted to enter {0}'s spawn.", arg0Fmt: FactionInfo.FormatColorShortName);

    [TranslationData("Sent when a player tries to reenter their spawn after leaving.")]
    public readonly Translation ReenteredSpawn = new Translation("You can not re-enter your spawn after leaving.");

    [TranslationData("Sent on a SEVERE toast when the player is exiting the play area during an FTDM match.", "Seconds until death")]
    public readonly Translation<string> EnteredEnemyTerritory = new Translation<string>("EXITED PLAY AREA\nRETURN IMMEDIATELY\nDEAD IN <uppercase>{0}</uppercase>", TranslationOptions.UnityUI);

}