using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("points"), MetadataFile]
internal sealed class PointsCommand : ICommand;

[Command("experience", "xp"), SubCommandOf(typeof(PointsCommand))]
internal sealed class PointsExperienceCommand : ICommand;

[Command("credits", "creds"), SubCommandOf(typeof(PointsCommand))]
internal sealed class PointsCreditsCommand : ICommand;

[Command("reputation", "rep"), SubCommandOf(typeof(PointsCommand))]
internal sealed class PointsReputationCommand : ICommand;