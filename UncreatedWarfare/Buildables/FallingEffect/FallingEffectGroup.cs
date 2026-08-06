using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uncreated.Warfare.Buildables;

public sealed class FallingEffectGroup : IDisposable
{
    private readonly FallingEffectManager _manager;

    private readonly FallingEffect?[] _activeEffects;

    private bool _isDisposed;

    public ItemAsset Item { get; }

    public ImmutableArray<EffectAsset> Effects { get; }

    internal FallingEffectGroup(ItemAsset item, EffectAsset[] effects, FallingEffectManager manager)
    {
        if (effects.Length < 1)
            throw new ArgumentException("Must have at least one effect.", nameof(effects));

        int nullEffects = effects.Count(x => x == null);
        if (nullEffects > 0)
            throw new ArgumentException($"{nullEffects} / {effects.Length} effect assets are null.", nameof(effects));

        Item = item;
        Effects = ImmutableArray.Create(effects);
        _manager = manager;
        _activeEffects = new FallingEffect[effects.Length];
    }

    internal int GetNextFreeAssetIndex(out EffectAsset asset)
    {
        ImmutableArray<EffectAsset> effects = Effects;
        for (int i = 0; i < _activeEffects.Length; ++i)
        {
            if (_activeEffects[i] != null)
                continue;

            asset = effects[i];
            return i;
        }

        asset = effects[0];
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
