using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("buildables", "bl"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DevBuildables : ICommand;
