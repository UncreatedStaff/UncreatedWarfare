using System;

namespace Uncreated.Warfare.Database.Automation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
public sealed class IncludedEnumAttribute : Attribute
{
    public object? Value { get; }
    public IncludedEnumAttribute(object value)
    {
        Value = value;
    }
}
