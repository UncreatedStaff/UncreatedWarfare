using System;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Lobby;

/// <summary>
/// Home for storing information about the lobby.
/// </summary>
public class LobbyConfiguration(IServiceProvider serviceProvider) : BaseAlternateConfigurationFile(serviceProvider, "Lobby.yml", mapSpecific: true);