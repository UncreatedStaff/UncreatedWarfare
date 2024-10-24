#if DEBUG
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands.MigrateCommand;

[Command("migrate"), HideFromHelp]
public class MigrateCommand : ICommand;
#endif