using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;
public class KitSigns(KitManager manager, IServiceProvider serviceProvider)
{
    private readonly SignInstancer _signs = serviceProvider.GetRequiredService<SignInstancer>();
    private readonly IPlayerService _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    public KitManager Manager { get; } = manager;

    /// <summary>
    /// Get the kit a sign refers to from a given player's perspective.
    /// </summary>
    public async Task<Kit?> GetKitFromSign(BarricadeDrop drop, CSteamID looker, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        KitSignInstanceProvider? signProvider = _signs.GetSignProvider(drop) as KitSignInstanceProvider;

        if (signProvider == null)
            return null;

        if (signProvider.LoadoutNumber <= 0)
        {
            return signProvider.KitId == null ? null : await Manager.FindKit(signProvider.KitId, token, true, KitManager.Set);
        }

        Kit? kit = await Manager.Loadouts.GetLoadout(looker, signProvider.LoadoutNumber, token).ConfigureAwait(false);
        return kit;
    }

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns()
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(null);
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateSignsIntl(null);
            });
        }
    }

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(player);
        else
        {
            WarfarePlayer p2 = player;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateSignsIntl(p2);
            });
        }
    }

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(Kit kit)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kit, null);
        else
        {
            Kit k2 = kit;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateSignsIntl(k2, null);
            });
        }
    }

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(Kit kit, WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kit, player);
        else
        {
            Kit k2 = kit;
            WarfarePlayer p2 = player;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateSignsIntl(k2, p2);
            });
        }
    }

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(string kitId)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kitId, null);
        else
        {
            string k2 = kitId;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateSignsIntl(k2, null);
            });
        }
    }

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(string kitId, WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kitId, player);
        else
        {
            string k2 = kitId;
            WarfarePlayer p2 = player;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateSignsIntl(k2, p2);
            });
        }
    }

    private void UpdateSignsIntl(WarfarePlayer? player)
    {
        if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>();
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player);
        }
    }

    private void UpdateSignsIntl(Kit kit, WarfarePlayer? player)
    {
        if (kit.Type == KitType.Loadout)
        {
            int loadoutId = LoadoutIdHelper.Parse(kit.InternalName, out CSteamID s64);
            if (loadoutId == -1)
                return;

            if (player == null)
            {
                player = _playerService.GetOnlinePlayerOrNull(s64);
                if (player == null)
                    return;
            }
            else if (player.Steam64.m_SteamID != s64.m_SteamID)
                return;

            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber == loadoutId);
        }
        else
        {
            UpdateSignsIntl(kit.InternalName, player);
        }
    }

    private void UpdateSignsIntl(string kitId, WarfarePlayer? player)
    {
        int loadoutId = LoadoutIdHelper.Parse(kitId, out CSteamID s64);
        if (loadoutId != -1)
        {
            if (player == null)
            {
                player = _playerService.GetOnlinePlayerOrNull(s64);
                if (player == null)
                    return;
            }
            else if (player.Steam64.m_SteamID != s64.m_SteamID)
                return;

            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber == loadoutId);
        }
        else if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => provider.KitId.Equals(kitId, StringComparison.Ordinal));
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.KitId.Equals(kitId, StringComparison.Ordinal));
        }
    }
}
