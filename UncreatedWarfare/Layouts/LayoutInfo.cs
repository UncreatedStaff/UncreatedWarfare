using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Uncreated.Warfare.Layouts;

/// <summary>
/// Stores information from config about a layout.
/// </summary>
public class LayoutInfo : IDisposable
{
    private int _disposed;
    /// <summary>
    /// Type of <see cref="Layouts.Layout"/> to create.
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

    /// <summary>
    /// If this layout is a special seeding layout, which can be activated and deactivated randomly when player count changes.
    /// </summary>
    public required bool IsSeeding { get; init; }

    /// <summary>
    /// Binded configuration.
    /// </summary>
    public LayoutInfoConfiguration Configuration { get; } = new LayoutInfoConfiguration();

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        if (Layout is IDisposable disposable)
            disposable.Dispose();
    }

    /// <summary>
    /// Resolves a possibly relative path to this layout.
    /// </summary>
    public string ResolveRelativePath(string path)
    {
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(path, Path.GetDirectoryName(FilePath));
    }
}

/// <summary>
/// Binded configuration in <see cref="LayoutInfo"/>.
/// </summary>
public class LayoutInfoConfiguration
{
    /// <summary>
    /// Display name of the gamemode.
    /// </summary>
    public string? GamemodeName { get; set; }

    /// <summary>
    /// Display name of the layout, not including the gamemode.
    /// </summary>
    public string? LayoutName { get; set; }

    /// <summary>
    /// URL to the image for this gamemode.
    /// </summary>
    public string? Image { get; set; }

}