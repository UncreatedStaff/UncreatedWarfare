using System;

namespace Uncreated.Warfare.Database.Automation;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ValueConverterCallbackAttribute : Attribute
{
    public string MethodName { get; }
    public ValueConverterCallbackAttribute(string methodName)
    {
        MethodName = methodName;
    }
}