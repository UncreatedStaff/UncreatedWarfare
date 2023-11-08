using OpenMod.API.Ioc;
using System.Threading.Tasks;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.API.Users;

[Service]
public interface IWarfareUserDataProvider
{
    Task<WarfareUserData?> GetUserDataAsync(ulong steam64);
    Task SaveUserDataAsync(WarfareUserData data);
}