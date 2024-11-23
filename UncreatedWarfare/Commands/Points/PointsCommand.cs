using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("points"), MetadataFile]
public sealed class PointsCommand : ICommand;

[Command("experience", "xp"), SubCommandOf(typeof(PointsCommand))]
public sealed class PointsExperienceCommand : ICommand;

[Command("credits", "creds"), SubCommandOf(typeof(PointsCommand))]
public sealed class PointsCreditsCommand : ICommand;