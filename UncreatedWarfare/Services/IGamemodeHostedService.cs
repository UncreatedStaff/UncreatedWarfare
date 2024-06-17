using Cysharp.Threading.Tasks;

namespace Uncreated.Warfare.Services;
public interface IGamemodeHostedService
{
    UniTask StartAsync();
    UniTask StopAsync();
}
