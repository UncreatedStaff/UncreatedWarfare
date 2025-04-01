using System;

namespace Uncreated.Warfare.Translations;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class TranslatableAttribute : Attribute
{
    public TranslatableAttribute()
    {
        Default = null;
        Language = null;
    }
    public TranslatableAttribute(string? @default)
    {
        Default = @default;
        Language = null;
    }
    public TranslatableAttribute(string language, string value)
    {
        Default = value;
        Language = language;
    }
    public string? Language { get; }
    public string? Default { get; }
    public string? Description { get; set; }
    public bool IsPrioritizedTranslation { get; set; } = true;
}