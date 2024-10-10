using DanielWillett.ModularRpcs.Annotations;
using System.Globalization;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Kits;

partial class KitManager
{
    [RpcReceive]
    public async Task<bool> ReceiveKitAccess(CSteamID player, CSteamID adminInstigator, uint kitPrimaryKey, bool state, KitAccessType accessType, CancellationToken token)
    {
        Kit? kit = await GetKit(kitPrimaryKey, token).ConfigureAwait(false);

        if (kit == null)
            return false;

        bool success = state ? await GiveAccess(kit, player, accessType, token)
                             : await RemoveAccess(kit, player, token);

        if (!success)
        {
            return false;
        }

        if (state)
        {
            ActionLog.Add(
                ActionLogType.ChangeKitAccess,
                player.m_SteamID.ToString(CultureInfo.InvariantCulture) + " GIVEN ACCESS TO " + kit.InternalName + ", REASON: " + accessType + ".",
                adminInstigator
            );
        }
        else
        {
            ActionLog.Add(
                ActionLogType.ChangeKitAccess,
                player.m_SteamID.ToString(CultureInfo.InvariantCulture) + " DENIED ACCESS TO " + kit.InternalName + ".",
                adminInstigator
            );
        }

        return true;
    }
}
