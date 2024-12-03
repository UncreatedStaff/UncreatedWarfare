﻿using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Event listener args which fires after a <see cref="Squad"/> is disbanded.
/// </summary>
[EventModel(SynchronizedModelTags = [ "squads" ])]
public class SquadDisbanded : SquadUpdated;