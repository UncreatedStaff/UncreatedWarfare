using System;
using System.ComponentModel;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction;

/// <summary>
/// Data about replicated player keys.
/// </summary>
public static class PlayerKeys
{
    /// <summary>
    /// Total number of keys in the key array.
    /// </summary>
    public static readonly int KeyCount = 10 + ControlsSettings.NUM_PLUGIN_KEYS;

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if the key isn't valid or is reserved.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    internal static void AssertValidKey(PlayerKey key)
    {
        if ((int)key is < 0 or 1 or 2 or 8 || (int)key >= KeyCount)
            throw new ArgumentOutOfRangeException(nameof(key), key + " doesn't match a valid key.");
    }

    /// <summary>
    /// Check if a key is held down by a <paramref name="player"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Invalid key.</exception>
    /// <exception cref="GameThreadException"/>
    public static bool IsKeyDown(this WarfarePlayer player, PlayerKey key)
    {
        return player.Component<PlayerKeyComponent>().IsKeyDown(key);
    }

    /// <summary>
    /// Invoked when a <paramref name="key"/> is pressed down.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Invalid key.</exception>
    /// <exception cref="GameThreadException"/>
    public static void AddPressedKeyHandler(PlayerKey key, KeyDown keyDown)
    {
        PlayerKeyComponent.AddKeyDownHandler(key, keyDown);
    }

    /// <summary>
    /// Invoked when a <paramref name="key"/> is released.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Invalid key.</exception>
    /// <exception cref="GameThreadException"/>
    public static void AddReleasedKeyHandler(PlayerKey key, KeyUp keyUp)
    {
        PlayerKeyComponent.AddKeyUpHandler(key, keyUp);
    }

    /// <summary>
    /// Invoked when a <paramref name="key"/> is pressed down.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Invalid key.</exception>
    /// <exception cref="GameThreadException"/>
    public static void RemovePressedKeyHandler(PlayerKey key, KeyDown keyDown)
    {
        PlayerKeyComponent.RemoveKeyDownHandler(key, keyDown);
    }

    /// <summary>
    /// Invoked when a <paramref name="key"/> is released.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Invalid key.</exception>
    /// <exception cref="GameThreadException"/>
    public static void RemoveReleasedKeyHandler(PlayerKey key, KeyUp keyUp)
    {
        PlayerKeyComponent.RemoveKeyUpHandler(key, keyUp);
    }

    /// <summary>
    /// Invoked when 'jump' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedJump
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.Jump, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.Jump, value);
    }

    /// <summary>
    /// Invoked when 'jump' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedJump
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.Jump, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.Jump, value);
    }

    /// <summary>
    /// Invoked when 'crouch' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedCrouch
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.Crouch, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.Crouch, value);
    }

    /// <summary>
    /// Invoked when 'crouch' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedCrouch
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.Crouch, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.Crouch, value);
    }

    /// <summary>
    /// Invoked when 'prone' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedProne
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.Prone, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.Prone, value);
    }

    /// <summary>
    /// Invoked when 'prone' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedProne
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.Prone, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.Prone, value);
    }

    /// <summary>
    /// Invoked when 'sprint' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedSprint
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.Sprint, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.Sprint, value);
    }

    /// <summary>
    /// Invoked when 'sprint' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedSprint
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.Sprint, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.Sprint, value);
    }

    /// <summary>
    /// Invoked when 'lean left' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedLeanLeft
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.LeanLeft, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.LeanLeft, value);
    }

    /// <summary>
    /// Invoked when 'lean left' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedLeanLeft
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.LeanLeft, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.LeanLeft, value);
    }

    /// <summary>
    /// Invoked when 'lean right' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedLeanRight
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.LeanRight, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.LeanRight, value);
    }

    /// <summary>
    /// Invoked when 'lean right' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedLeanRight
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.LeanRight, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.LeanRight, value);
    }

    /// <summary>
    /// Invoked when 'steady aim' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedSteadyAim
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.SteadyAim, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.SteadyAim, value);
    }

    /// <summary>
    /// Invoked when 'steady aim' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedSteadyAim
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.SteadyAim, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.SteadyAim, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 1' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedPluginKey1
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.PluginKey1, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.PluginKey1, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 1' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedPluginKey1
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.PluginKey1, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.PluginKey1, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 2' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedPluginKey2
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.PluginKey2, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.PluginKey2, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 2' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedPluginKey2
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.PluginKey2, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.PluginKey2, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 3' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedPluginKey3
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.PluginKey3, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.PluginKey3, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 3' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedPluginKey3
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.PluginKey3, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.PluginKey3, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 4' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedPluginKey4
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.PluginKey4, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.PluginKey4, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 4' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedPluginKey4
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.PluginKey4, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.PluginKey4, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 5' is pressed down.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyDown PressedPluginKey5
    {
        add => PlayerKeyComponent.AddKeyDownHandler(PlayerKey.PluginKey5, value);
        remove => PlayerKeyComponent.RemoveKeyDownHandler(PlayerKey.PluginKey5, value);
    }

    /// <summary>
    /// Invoked when 'plugin key 5' is released.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static event KeyUp ReleasedPluginKey5
    {
        add => PlayerKeyComponent.AddKeyUpHandler(PlayerKey.PluginKey5, value);
        remove => PlayerKeyComponent.RemoveKeyUpHandler(PlayerKey.PluginKey5, value);
    }
}

/// <summary>
/// Enum corresponding to the values in <see cref="PlayerInput.keys"/>.
/// </summary>
public enum PlayerKey
{
    Jump = 0,
    [Obsolete("Replaced with PlayerInput.pendingPrimaryAttackInput.", true), EditorBrowsable(EditorBrowsableState.Never)]
    Primary = 1,
    [Obsolete("Replaced with PlayerInput.pendingSecondaryAttackInput.", true), EditorBrowsable(EditorBrowsableState.Never)]
    Secondary = 2,
    Crouch = 3,
    Prone = 4,
    Sprint = 5,
    LeanLeft = 6,
    LeanRight = 7,
    [Obsolete("This is not in use right now.", true), EditorBrowsable(EditorBrowsableState.Never)]
    Reserved = 8,
    SteadyAim = 9,
    PluginKey1 = 10,
    PluginKey2 = 11,
    PluginKey3 = 12,
    PluginKey4 = 13,
    PluginKey5 = 14
}

/// <summary>
/// Handles when a key is pressed.
/// </summary>
public delegate void KeyDown(WarfarePlayer player, ref bool handled);

/// <summary>
/// Handles when a key is released.
/// </summary>
public delegate void KeyUp(WarfarePlayer player, TimeSpan timeDown, ref bool handled);