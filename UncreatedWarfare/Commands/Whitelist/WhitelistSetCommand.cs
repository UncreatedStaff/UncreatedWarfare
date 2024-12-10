using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("set"), SubCommandOf(typeof(WhitelistCommand))]
internal sealed class WhitelistSetCommand : ICommand;