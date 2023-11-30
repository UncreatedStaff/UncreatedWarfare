using System;

namespace Uncreated.Warfare.Database.Automation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
public sealed class ExcludedEnumAttribute : Attribute
{
    public object? Value { get; }
    public ExcludedEnumAttribute(object value)
    {
        Value = value;
    }
}
