using System;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

public class KitSignService
{
    private readonly SignInstancer _signs;
    private readonly IPlayerService _playerService;

    public KitSignService(SignInstancer signs, IPlayerService playerService)
    {
        _signs = signs;
        _playerService = playerService;
    }

    /// <summary>
    /// Update all kit signs for all players.
    /// </summary>
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

    /// <summary>
    /// Update all kit signs for <paramref name="player"/>.
    /// </summary>
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

    /// <summary>
    /// Update only loadout signs for <paramref name="player"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateLoadoutSigns(WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateLoadoutSignsIntl(player);
        else
        {
            WarfarePlayer p2 = player;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateLoadoutSignsIntl(p2);
            });
        }
    }

    /// <summary>
    /// Update only loadout signs for all players.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateLoadoutSigns()
    {
        if (GameThread.IsCurrent)
            UpdateLoadoutSignsIntl(null);
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateLoadoutSignsIntl(null);
            });
        }
    }

    /// <summary>
    /// Update all signs for a specific <paramref name="kit"/>.
    /// </summary>
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

    /// <summary>
    /// Update all signs for a specific <paramref name="kit"/> and <paramref name="player"/>.
    /// </summary>
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

    /// <summary>
    /// Update all signs for a specific kit's ID.
    /// </summary>
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

    /// <summary>
    /// Update all signs for a specific kit's ID and <paramref name="player"/>.
    /// </summary>
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
        // expects game thread
        if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>();
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player);
        }
    }

    private void UpdateLoadoutSignsIntl(WarfarePlayer? player)
    {
        // expects game thread
        if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => provider.LoadoutNumber >= 0);
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber >= 0);
        }
    }

    private void UpdateSignsIntl(Kit kit, WarfarePlayer? player)
    {
        // expects game thread
        if (kit.Type == KitType.Loadout)
        {
            int loadoutId = LoadoutIdHelper.Parse(kit.Id, out CSteamID s64);
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

            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber >= 0);
        }
        else
        {
            string capturedKitId = kit.Id;
            if (player == null)
            {
                _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
            else
            {
                _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
        }
    }

    private void UpdateSignsIntl(string kitId, WarfarePlayer? player)
    {
        // expects game thread
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

            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber >= 0);
        }
        else
        {
            string capturedKitId = kitId;
            if (player == null)
            {
                _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
            else
            {
                _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
        }
    }
}
