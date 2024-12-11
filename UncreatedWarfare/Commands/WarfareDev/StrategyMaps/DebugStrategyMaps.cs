using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("strategymaps", "strt"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugStrategyMaps : ICommand;