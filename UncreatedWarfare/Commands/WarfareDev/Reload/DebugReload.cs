using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("reload"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugReload : ICommand;
