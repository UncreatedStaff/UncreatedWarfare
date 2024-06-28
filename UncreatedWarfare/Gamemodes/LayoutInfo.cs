using Microsoft.Extensions.Configuration;
using System;

namespace Uncreated.Warfare.Gamemodes;

/// <summary>
/// Stores information from config about a layout.
/// </summary>
public class LayoutInfo : IDisposable
{
    /// <summary>
    /// Type of <see cref="Gamemodes.Layout"/> to create.
    /// </summary>
    public required Type LayoutType { get; init; }

    /// <summary>
    /// Configuration info about the layout of the game.
    /// </summary>
    public required IConfigurationRoot Layout { get; init; }

    /// <summary>
    /// File path of the configuration file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Weight of this session being picked. Defaults to 1.
    /// </summary>
    public required double Weight { get; init; }

    /// <summary>
    /// Display name of the layout. Defaults to the file name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Layout is IDisposable disposable)
            disposable.Dispose();
    }
}