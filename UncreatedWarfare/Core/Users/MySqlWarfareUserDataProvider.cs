using OpenMod.API.Ioc;
using System.Threading.Tasks;
using Uncreated.Warfare.API.Users;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Core.Users;

[ServiceImplementation]
internal class MySqlWarfareUserDataProvider : IWarfareUserDataProvider
{
    public Task<WarfareUserData?> GetUserDataAsync(ulong steam64)
    {
        throw new System.NotImplementedException();
    }

    public Task SaveUserDataAsync(WarfareUserData data)
    {
        throw new System.NotImplementedException();
    }
}
