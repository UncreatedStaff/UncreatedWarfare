using System;

namespace Uncreated.Warfare.Database.Automation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DontAddPackedColumnAttribute : Attribute { }