using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Seeding;

public sealed class SeedingTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Seeding";

    [TranslationData("The title of the vote UI to start the seeding mode.")]
    public readonly Translation SeedingVoteTitle = new Translation("The server is low on players! Switch to seeding mode?", TranslationOptions.TMProUI);

    [TranslationData("The text of the 'yes' vote.", "Number of votes, green if it's the current player's vote.")]
    public readonly Translation<string> SeedingVoteYes = new Translation<string>("Yes [<plugin_3/>] votes: {0}", TranslationOptions.TMProUI);

    [TranslationData("The text of the 'no' vote.", "Number of votes, green if it's the current player's vote.")]
    public readonly Translation<string> SeedingVoteNo = new Translation<string>("Ask again later [<plugin_4/>] votes: {0}", TranslationOptions.TMProUI);

    [TranslationData("Description of whats happening when waiting for players to join.")]
    public readonly Translation SeedingDescriptionWaitingForPlayers = new Translation("Waiting for more players...", TranslationOptions.TMProUI);

    [TranslationData("Description of whats happening when enough players have joined but the cooldown timer is ticking.")]
    public readonly Translation SeedingDescriptionWaitingForCooldown = new Translation("Enough players, starting soon...", TranslationOptions.TMProUI);
}