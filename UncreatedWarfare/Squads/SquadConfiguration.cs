using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Squads;
public class SquadConfiguration(IServiceProvider serviceProvider) : BaseAlternateConfigurationFile(serviceProvider, "Squads.yml");
