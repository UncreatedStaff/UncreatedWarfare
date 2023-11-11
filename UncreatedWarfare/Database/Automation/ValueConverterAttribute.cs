using System;

namespace Uncreated.Warfare.Database.Automation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class ValueConverterAttribute : Attribute
{
    public Type? Type { get; }

    /// <param name="type">Converter type to use for this class or property, or <see langword="null"/> to cancel for a property</param>
    public ValueConverterAttribute(Type? type)
    {
        Type = type;
    }
}
