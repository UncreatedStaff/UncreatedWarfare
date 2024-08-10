using System;
using System.Globalization;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;

// ReSharper disable once CheckNamespace (i want this to be accessible everywhere)
namespace Uncreated.Warfare;

/// <summary>
/// Extensions for translating <see cref="Translation"/> objects.
/// </summary>
public static class TranslationExtensions
{
    #region 0-arg
    private static void AssertNoArgs(Translation translation)
    {
        if (translation.ArgumentCount < 1)
            throw new ArgumentException("Translation must have no arguments.", nameof(translation));
    }

    /// <summary>
    /// Translate a 0-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, bool imgui = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(null);

        return value.GetValueString(imgui, false);
    }

    /// <summary>
    /// Translate a 0-arg translation for a player.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, WarfarePlayer player, bool canUseIMGUI = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        return value.GetValueString(canUseIMGUI && player.Save.IMGUI, false);
    }

    /// <summary>
    /// Translate a 0-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        AssertNoArgs(translation);

        TranslationValue value = translation.GetValueForLanguage(set.Language);
        return value.GetValueString(canUseIMGUI && set.IMGUI, false);
    }

    /// <summary>
    /// Translate a 0-arg translation for a given language.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, LanguageInfo language, bool imgui = false)
    {
        AssertNoArgs(translation);

        TranslationValue value = translation.GetValueForLanguage(language);
        return value.GetValueString(imgui, false);
    }

    /// <summary>
    /// Translate a 0-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, out Color color, bool imgui = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(null);

        color = value.Color;
        return value.GetValueString(imgui, true);
    }

    /// <summary>
    /// Translate a 0-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        color = value.Color;
        return value.GetValueString(canUseIMGUI && player.Save.IMGUI, true);
    }

    /// <summary>
    /// Translate a 0-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, in LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(set.Language);

        color = value.Color;
        return value.GetValueString(canUseIMGUI && set.IMGUI, true);
    }

    /// <summary>
    /// Translate a 0-arg translation for a given language and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, LanguageInfo language, out Color color, bool imgui = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(language);

        color = value.Color;
        return value.GetValueString(imgui, true);
    }
    #endregion

    #region 1-arg
    /// <summary>
    /// Translate a 1-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a player.
    /// </summary>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0);
    }
    #endregion

    #region 2-arg
    /// <summary>
    /// Translate a 2-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1);
    }
    #endregion

    #region 3-arg
    /// <summary>
    /// Translate a 3-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2);
    }
    #endregion

    #region 4-arg
    /// <summary>
    /// Translate a 4-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }
    #endregion

    #region 5-arg
    /// <summary>
    /// Translate a 5-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }
    #endregion

    #region 6-arg
    /// <summary>
    /// Translate a 6-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }
    #endregion

    #region 7-arg
    /// <summary>
    /// Translate a 7-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a set of players.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    #endregion
}