using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.StrategyMaps;
public class StrategyMapsConfiguration(IServiceProvider serviceProvider) : BaseAlternateConfigurationFile(serviceProvider, "StrategyMaps.yml");


