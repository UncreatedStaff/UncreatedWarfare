using System;

namespace Uncreated.Warfare.Layouts;

/// <summary>
/// Define special settings for session-hosted services.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
sealed class SessionHostedServiceAttribute : Attribute
{
    /// <summary>
    /// If this service should be loaded by default. Otherwise it'll have to be loaded explicitly in the configuration file.
    /// </summary>
    // todo actually add this
    public bool EnabledByDefault { get; set; } = true;
}