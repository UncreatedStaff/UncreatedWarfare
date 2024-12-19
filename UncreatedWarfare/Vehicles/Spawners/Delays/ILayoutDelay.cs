using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Stripe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Configuration.TypeConverters;
using Uncreated.Warfare.Layouts;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Uncreated.Warfare.Vehicles.Spawners.Delays;
public interface ILayoutDelay<TContext> where TContext : LayoutDelayContext
{
    TimeSpan GetTimeLeft(TContext context);
}