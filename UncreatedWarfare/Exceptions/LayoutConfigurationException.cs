﻿using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Gamemodes.Phases;
using Uncreated.Warfare.Gamemodes.Teams;

namespace Uncreated.Warfare.Exceptions;

/// <summary>
/// Thrown when a component in a game session has invalid configuration data.
/// </summary>
public class LayoutConfigurationException : Exception
{
    public object? Component { get; }

    public LayoutConfigurationException(ILayoutPhase phase, string reason) : base($"There was an error reading the layout configuration for layout phase {Accessor.ExceptionFormatter.Format(phase.GetType())}. {reason}")
    {
        Component = phase;
    }

    public LayoutConfigurationException(ITeamManager<Team> teamManager, string reason) : base($"There was an error reading the layout configuration for team manager {Accessor.ExceptionFormatter.Format(teamManager.GetType())}. {reason}")
    {
        Component = teamManager;
    }

    public LayoutConfigurationException(string message) : base(message)
    {

    }
}