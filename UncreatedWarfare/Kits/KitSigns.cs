using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Signs;

namespace Uncreated.Warfare.Kits;
public class KitSigns(KitManager manager, IServiceProvider serviceProvider)
{
    private readonly SignInstancer _signs = serviceProvider.GetRequiredService<SignInstancer>();
    public KitManager Manager { get; } = manager;

    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns()
    {
        if (UCWarfare.IsMainThread)
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
        if (UCWarfare.IsMainThread)
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
        if (UCWarfare.IsMainThread)
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
        if (UCWarfare.IsMainThread)
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
        if (UCWarfare.IsMainThread)
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
        if (UCWarfare.IsMainThread)
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
        // todo add loadouts
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
        UpdateSignsIntl(kit.InternalName, player);
    }

    private void UpdateSignsIntl(string kitId, WarfarePlayer? player)
    {
        // todo add loadouts
        if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => provider.KitId.Equals(kitId, StringComparison.Ordinal));
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.KitId.Equals(kitId, StringComparison.Ordinal));
        }
    }
}
