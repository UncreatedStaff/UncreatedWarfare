// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

sealed class IsExternalInit;
sealed class RequiredMemberAttribute : Attribute;
sealed class SetsRequiredMembersAttribute : Attribute;
sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
{
    public const string RefStructs = "RefStructs";
    public const string RequiredMembers = "RequiredMembers";
    public string FeatureName { get; } = featureName;
    public bool IsOptional { get; init; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
sealed class InterpolatedStringHandlerAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    public InterpolatedStringHandlerArgumentAttribute(string argument) => Arguments = [ argument ];

    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) => Arguments = arguments;

    public string[] Arguments { get; }
}