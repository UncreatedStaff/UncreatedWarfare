using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("util", "u", "tools"), SubCommandOf(typeof(ZoneCommand))]
public class ZoneUtilityCommand : ICommand;