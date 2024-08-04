using System;

namespace Uncreated.Warfare.Signs;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SignPrefixAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}