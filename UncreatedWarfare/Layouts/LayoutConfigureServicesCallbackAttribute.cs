using System;

namespace Uncreated.Warfare.Layouts;

/// <summary>
/// Configures a method to invoke when configuring services.
/// </summary>
/// <remarks>The method must be static and have the following signature: <c>void(ContainerBuilder, LayoutInfo)</c>.</remarks>
[BaseTypeRequired(typeof(Layout))]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class LayoutConfigureServicesCallbackAttribute(string methodName) : Attribute
{
    /// <summary>
    /// Name of the static method in this class to invoke.
    /// </summary>
    public string MethodName { get; } = methodName;
}