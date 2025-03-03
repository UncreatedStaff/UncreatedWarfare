using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using UnityEngine.Serialization;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;

public class PlacedAmmoBagComponent : MonoBehaviour, IAmmoStorage
{
    private IBuildable _buildable;
    public CSteamID Owner { get; private set; }
    public float AmmoCount { get; private set; }

    public void Init(WarfarePlayer warfarePlayer, IBuildable buildable, float startingAmmo)
    {
        AmmoCount = startingAmmo;
        _buildable = buildable;
        Owner = warfarePlayer.Steam64;
    }
    public void SubtractAmmo(float ammoCount)
    {
        AmmoCount -= ammoCount;
        
        if (AmmoCount <= 0)
        {
            AmmoCount = 0;
            UniTask.Create(async () =>
            {
                await UniTask.NextFrame(); // destroy the ammo bag next frame
                Destroy(this);
                _buildable.Destroy();
            });
        }
    }
}