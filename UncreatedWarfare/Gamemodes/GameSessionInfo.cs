using Microsoft.Extensions.Configuration;
using System;

namespace Uncreated.Warfare.Gamemodes;

/// <summary>
/// Stores information from config about a game session.
/// </summary>
public class GameSessionInfo : IDisposable
{
    /// <summary>
    /// Type of <see cref="GameSession"/> to create.
    /// </summary>
    public Type GameSessionType { get; set; }

    /// <summary>
    /// Configuration info about the layout of the game.
    /// </summary>
    public IConfigurationRoot Layout { get; set; }

    /// <summary>
    /// Weight of this session being picked. Defaults to 1.
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Display name of the layout. Defaults to the file name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Layout is IDisposable disposable)
            disposable.Dispose();
    }
}