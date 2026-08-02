using System;

namespace Uncreated.Warfare.Buildables;

public sealed class FallingEffectGroup : IDisposable
{
    private readonly FallingEffectManager _manager;

    private readonly EffectAsset[] _effects;
    private readonly FallingEffect?[] _activeEffects;

    private bool _isDisposed;

    public ItemAsset Item { get; }

    internal FallingEffectGroup(ItemAsset item, EffectAsset[] effects, FallingEffectManager manager)
    {
        if (effects.Length < 1)
            throw new ArgumentException("Must have at least one effect.", nameof(effects));

        Item = item;
        _effects = effects;
        _manager = manager;
        _activeEffects = new FallingEffect[effects.Length];
    }

    internal int GetNextFreeAssetIndex(out EffectAsset asset)
    {
        for (int i = 0; i < _activeEffects.Length; ++i)
        {
            if (_activeEffects[i] != null)
                continue;

            asset = _effects[i];
            return i;
        }

        asset = _effects[0];
        return -1;
    }

    internal void AddEffect(FallingEffect effect)
    {
        _activeEffects[effect.IndexInGroup] = effect;
    }

    internal void RemoveEffect(FallingEffect effect)
    {
        if (_isDisposed)
            return;

        int index = effect.IndexInGroup;
        if (index >= _activeEffects.Length || index < 0)
            return;

        Interlocked.CompareExchange(ref _activeEffects[index], null, effect);
    }

    public void Dispose()
    {
        _isDisposed = true;
        for (int i = 0; i < _activeEffects.Length; ++i)
        {
            FallingEffect? effect = _activeEffects[i];
            if (effect != null)
                _manager.DestroyFallingEffect(effect);
        }
    }
}
