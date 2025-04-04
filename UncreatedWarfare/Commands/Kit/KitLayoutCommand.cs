﻿using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("layout", "loadout", "items", "order", "customize"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitLayoutCommand : ICommand;