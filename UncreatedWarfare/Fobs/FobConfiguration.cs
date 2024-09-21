using System;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Home for storing FOB and buildable data.
/// </summary>
public class FobConfiguration(IServiceProvider serviceProvider) : BaseAlternateConfigurationFile(serviceProvider, "Buildables.yml");