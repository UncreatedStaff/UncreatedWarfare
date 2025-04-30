using System;

namespace Uncreated.Warfare.Exceptions;

/// <summary>
/// Thrown when configuration data is invalid.
/// </summary>
public class GameConfigurationException : Exception
{
    public GameConfigurationException(string message) : base(message) { }
    public GameConfigurationException(string message, string fileName) : base($"There was an error in configuration file \"{fileName}\": {message}" + (message.EndsWith('.') ? string.Empty : ".")) { }
}