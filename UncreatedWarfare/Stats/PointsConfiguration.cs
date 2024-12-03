﻿using System;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Stats;

/// <summary>
/// Home for storing point presets.
/// </summary>
public class PointsConfiguration(IServiceProvider serviceProvider) : BaseAlternateConfigurationFile(serviceProvider, "Points.yml");