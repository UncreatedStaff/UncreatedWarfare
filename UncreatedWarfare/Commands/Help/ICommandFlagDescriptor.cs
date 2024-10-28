using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Commands;

/// <summary>
/// Describes a flag parameter (like '-e') for a command.
/// </summary>
public interface ICommandFlagDescriptor
{
    /// <summary>
    /// Flag name without the dash.
    /// </summary>
    /// <remarks>Example: -e would be "e".</remarks>
    string Name { get; }

    /// <summary>
    /// Translatable description of this flag's purpose.
    /// </summary>
    TranslationList? Description { get; }

    /// <summary>
    /// Permission required to use this flag.
    /// </summary>
    PermissionLeaf Permission { get; }
}
