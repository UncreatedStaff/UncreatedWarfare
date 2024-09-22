using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction;
public class PlayerKeyComponent : IPlayerComponent
{
    private static readonly KeyDown?[] KeyDownListeners = new KeyDown?[PlayerKeys.KeyCount];
    private static readonly KeyUp?[] KeyUpListeners = new KeyUp?[PlayerKeys.KeyCount];
    private static ulong _keyEventMask;
    private static bool _initialized;

    private bool _first;

    private bool[] _lastKeys;
    private float[] _keyDownTimes;
    public WarfarePlayer Player { get; private set; }
    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        if (!isOnJoin)
            return;

        _keyDownTimes = new float[PlayerKeys.KeyCount];
        _lastKeys = new bool[PlayerKeys.KeyCount];
        _first = true;
        if (_initialized)
            return;

        _initialized = true;
        IPlayerService playerService = serviceProvider.GetRequiredService<IPlayerService>();
        PlayerInput.onPluginKeyTick += (player, _, key, _) =>
        {
            if (key != 0)
                return;

            playerService.GetOnlinePlayer(player).Component<PlayerKeyComponent>().OnKeyTick();
        };
    }

    /// <remarks>Use <see cref="PlayerKeys"/> events instead.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="GameThreadException"/>
    internal static void AddKeyDownHandler(PlayerKey key, KeyDown handler)
    {
        GameThread.AssertCurrent();
        if (handler == null) return;
        PlayerKeys.AssertValidKey(key);
        ref KeyDown? d = ref KeyDownListeners[(int)key];
        _keyEventMask |= 1u << (int)key;
        d += handler;
    }

    /// <remarks>Use <see cref="PlayerKeys"/> events instead.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="GameThreadException"/>
    internal static void AddKeyUpHandler(PlayerKey key, KeyUp handler)
    {
        GameThread.AssertCurrent();
        if (handler == null) return;
        PlayerKeys.AssertValidKey(key);
        ref KeyUp? d = ref KeyUpListeners[(int)key];
        _keyEventMask |= 1u << (int)key;
        d += handler;
    }

    /// <remarks>Use <see cref="PlayerKeys"/> events instead.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="GameThreadException"/>
    internal static void RemoveKeyDownHandler(PlayerKey key, KeyDown handler)
    {
        GameThread.AssertCurrent();
        if (handler == null) return;
        PlayerKeys.AssertValidKey(key);
        ref KeyDown? d = ref KeyDownListeners[(int)key];
        d -= handler;
        if (d == null && KeyUpListeners[(int)key] == null)
            _keyEventMask &= ~(1u << (int)key);
    }

    /// <remarks>Use <see cref="PlayerKeys"/> events instead.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="GameThreadException"/>
    internal static void RemoveKeyUpHandler(PlayerKey key, KeyUp handler)
    {
        GameThread.AssertCurrent();
        if (handler == null) return;
        PlayerKeys.AssertValidKey(key);
        ref KeyUp? d = ref KeyUpListeners[(int)key];
        d -= handler;
        if (d == null && KeyDownListeners[(int)key] == null)
            _keyEventMask &= ~(1u << (int)key);
    }

    /// <summary>
    /// Check if <paramref name="key"/> is currently held down.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="GameThreadException"/>
    internal bool IsKeyDown(PlayerKey key)
    {
        GameThread.AssertCurrent();
        PlayerKeys.AssertValidKey(key);
        return Player.IsOnline && Player.UnturnedPlayer.input.keys[(int)key];
    }

    private void OnKeyTick()
    {
        if (!Player.IsOnline)
        {
            return;
        }

        int keyCount = PlayerKeys.KeyCount;

        bool[] keys = Player.UnturnedPlayer.input.keys;
        if (_first || _keyEventMask == 0)
        {
            for (int i = 0; i < keyCount; ++i)
                _lastKeys[i] = keys[i];
            _first = false;
        }
        else
        {
            for (int i = 0; i < keyCount; ++i)
            {
                bool state = keys[i];
                if ((_keyEventMask & (1u << i)) != 0)
                {
                    bool lastState = _lastKeys[i];

                    if (state == lastState)
                        continue;

                    if (state)
                    {
                        OnKeyDown((PlayerKey)i);
                        _keyDownTimes[i] = Time.realtimeSinceStartup;
                    }
                    else
                    {
                        OnKeyUp((PlayerKey)i, Time.realtimeSinceStartup - _keyDownTimes[i]);
                    }
                }

                _lastKeys[i] = state;
            }
        }
    }

    private void OnKeyDown(PlayerKey key)
    {
        KeyDown? callback = KeyDownListeners[(int)key];
        if (callback == null)
            return;

        bool handled = false;
        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (KeyDown invocable in callback.GetInvocationList())
        {
            invocable?.Invoke(Player, ref handled);
            if (handled)
                break;
        }
    }

    private void OnKeyUp(PlayerKey key, float timeSpan)
    {
        KeyUp? callback = KeyUpListeners[(int)key];
        if (callback == null)
            return;

        bool handled = false;
        TimeSpan span = TimeSpan.FromSeconds(timeSpan);

        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (KeyUp invocable in callback.GetInvocationList())
        {
            invocable?.Invoke(Player, span, ref handled);
            if (handled)
                break;
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
