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