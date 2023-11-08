using OpenMod.API.Ioc;

namespace Uncreated.Warfare.API.Gameplay;

[Service]
public interface IGameplayService
{
    IGameplayHost? Host { get; }
}
