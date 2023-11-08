using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using Uncreated.Warfare.API.Gameplay;

namespace Uncreated.Warfare.Core.Gameplay;

[ServiceImplementation(Lifetime = ServiceLifetime.Transient)]
public class GameplayService : IGameplayService
{
    IGameplayHost? Gameplay { get; }
}
