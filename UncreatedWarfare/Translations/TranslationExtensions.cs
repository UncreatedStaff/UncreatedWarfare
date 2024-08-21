using System;
using System.Globalization;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

// ReSharper disable once CheckNamespace (i want this to be accessible everywhere)
namespace Uncreated.Warfare;

/// <summary>
/// Extensions for translating <see cref="Translation"/> objects.
/// </summary>
public static class TranslationExtensions
{
    #region ITranslationArgument
    /// <summary>
    /// Shortcut for translating a <see cref="ITranslationArgument"/> manually using the default language and culture settings.
    /// </summary>
    /// <param name="format">Format information for the translation. This can just be a <see cref="string"/>.</param>
    public static string Translate(this ITranslationArgument argument, ITranslationService translationService, ArgumentFormat format = default, bool imgui = false, TranslationOptions options = TranslationOptions.None)
    {
        if (imgui && (options & TranslationOptions.NoRichText) == 0)
            options |= TranslationOptions.TranslateWithUnityRichText;

        ValueFormatParameters parameters = new ValueFormatParameters(-1,
            translationService.LanguageService.GetDefaultCulture(),
            translationService.LanguageService.GetDefaultLanguage(),
            options, in format, null, null, null, 0
        );

        return translationService.ValueFormatter.Format(argument, in parameters);
    }

    /// <summary>
    /// Shortcut for translating a <see cref="ITranslationArgument"/> manually using the given language and culture settings.
    /// </summary>
    /// <param name="format">Format information for the translation. This can just be a <see cref="string"/>.</param>
    public static string Translate(this ITranslationArgument argument, ITranslationService translationService, LanguageInfo? language, CultureInfo? culture, ArgumentFormat format = default, bool imgui = false, TranslationOptions options = TranslationOptions.None)
    {
        if (imgui && (options & TranslationOptions.NoRichText) == 0)
            options |= TranslationOptions.TranslateWithUnityRichText;

        language ??= translationService.LanguageService.GetDefaultLanguage();
        culture ??= translationService.LanguageService.GetDefaultCulture();

        ValueFormatParameters parameters = new ValueFormatParameters(-1, culture, language, options, in format, null, null, null, 0);

        return translationService.ValueFormatter.Format(argument, in parameters);
    }

    /// <summary>
    /// Shortcut for translating a <see cref="ITranslationArgument"/> manually using a player's translation settings.
    /// </summary>
    /// <param name="format">Format information for the translation. This can just be a <see cref="string"/>.</param>
    public static string Translate(this ITranslationArgument argument, ITranslationService translationService, WarfarePlayer player, ArgumentFormat format = default, bool canUseIMGUI = false, TranslationOptions options = TranslationOptions.None)
    {
        if (canUseIMGUI && (options & TranslationOptions.NoRichText) == 0 && player.Save.IMGUI)
            options |= TranslationOptions.TranslateWithUnityRichText;

        ValueFormatParameters parameters = new ValueFormatParameters(-1, player.Locale.CultureInfo, player.Locale.LanguageInfo, options, in format, player.Team, player, null, 0);

        return translationService.ValueFormatter.Format(argument, in parameters);
    }

    /// <summary>
    /// Shortcut for translating a <see cref="ITranslationArgument"/> manually using a user's translation settings.
    /// </summary>
    /// <param name="format">Format information for the translation. This can just be a <see cref="string"/>.</param>
    public static string Translate(this ITranslationArgument argument, ITranslationService translationService, ICommandUser user, ArgumentFormat format = default, bool canUseIMGUI = false, TranslationOptions options = TranslationOptions.None)
    {
        return user is WarfarePlayer player
            ? argument.Translate(translationService, player, format, canUseIMGUI, options)
            : argument.Translate(translationService, format, canUseIMGUI && user.IMGUI, user.IsTerminal ? options | TranslationOptions.ForTerminal : options);
    }

    /// <summary>
    /// Shortcut for translating a <see cref="ITranslationArgument"/> manually using a set of player's settings.
    /// </summary>
    /// <param name="format">Format information for the translation. This can just be a <see cref="string"/>.</param>
    public static string Translate(this ITranslationArgument argument, ITranslationService translationService, in LanguageSet set, ArgumentFormat format = default, bool canUseIMGUI = false, TranslationOptions options = TranslationOptions.None)
    {
        if (set.Players.Count == 1)
        {
            return argument.Translate(translationService, set.Players[0], format, canUseIMGUI, options);
        }

        if (canUseIMGUI && (options & TranslationOptions.NoRichText) == 0 && set.IMGUI)
            options |= TranslationOptions.TranslateWithUnityRichText;

        ValueFormatParameters parameters = new ValueFormatParameters(-1, set.Culture, set.Language, options, in format, set.Team, null, null, 0);

        return translationService.ValueFormatter.Format(argument, in parameters);
    }

    #endregion

    #region unsafe
    /// <summary>
    /// Translate a translation using default language and settings using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        return translation.TranslateUnsafe(in arguments, formatting);
    }

    /// <summary>
    /// Translate a translation for a player using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.TranslateUnsafe(in arguments, formatting);
    }

    /// <summary>
    /// Translate a translation for a user using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.TranslateUnsafe(formatting, player, canUseIMGUI)
            : translation.TranslateUnsafe(formatting, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a translation for a set of players using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.TranslateUnsafe(in arguments, formatting);
    }

    /// <summary>
    /// Translate a translation for a set of players using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.TranslateUnsafe(in arguments, formatting);
    }

    /// <summary>
    /// Translate a translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.TranslateUnsafe(in arguments, formatting);
    }

    /// <summary>
    /// Translate a translation for a player and output the background <paramref name="color"/> of the message using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.TranslateUnsafe(in arguments, formatting);
    }

    /// <summary>
    /// Translate a translation for a user and output the background <paramref name="color"/> of the message using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.TranslateUnsafe(formatting, player, out color, canUseIMGUI)
            : translation.TranslateUnsafe(formatting, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a translation for a set of players and output the background <paramref name="color"/> of the message using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, in LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.TranslateUnsafe(in arguments, formatting);
    }

    /// <summary>
    /// Translate a translation for a set of players and output the background <paramref name="color"/> of the message using an object[] instead of generic arguments.
    /// </summary>
    /// <exception cref="ArgumentException">Arguments in <paramref name="formatting"/> aren't convertible to the type the translation is expecting.</exception>
    public static string TranslateUnsafe(this Translation translation, object[] formatting, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.TranslateUnsafe(in arguments, formatting);
    }
    #endregion

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
    public static string Translate(this Translation translation, bool imgui = false, bool forTerminal = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(null);

        return value.GetValueString(imgui, false, forTerminal);
    }

    /// <summary>
    /// Translate a 0-arg translation for a player.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, WarfarePlayer player, bool canUseIMGUI = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        return value.GetValueString(canUseIMGUI && player.Save.IMGUI, false, false);
    }

    /// <summary>
    /// Translate a 0-arg translation for a user.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(player, canUseIMGUI)
            : translation.Translate(canUseIMGUI && user.IMGUI, user.IsTerminal);
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
        return value.GetValueString(canUseIMGUI && set.IMGUI, false, false);
    }

    /// <summary>
    /// Translate a 0-arg translation for a given language.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, LanguageInfo language, bool imgui = false)
    {
        AssertNoArgs(translation);

        TranslationValue value = translation.GetValueForLanguage(language);
        return value.GetValueString(imgui, false, false);
    }

    /// <summary>
    /// Translate a 0-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, out Color color, bool imgui = false, bool forTerminal = false)
    {
        AssertNoArgs(translation);
        TranslationValue value = translation.GetValueForLanguage(null);

        color = value.Color;
        return value.GetValueString(imgui, true, forTerminal);
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
        return value.GetValueString(canUseIMGUI && player.Save.IMGUI, true, false);
    }

    /// <summary>
    /// Translate a 0-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static string Translate(this Translation translation, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(player, out color, canUseIMGUI)
            : translation.Translate(out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
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
        return value.GetValueString(canUseIMGUI && set.IMGUI, true, false);
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
        return value.GetValueString(imgui, true, false);
    }
    #endregion

    #region 1-arg
    /// <summary>
    /// Translate a 1-arg translation using default language and settings.
    /// </summary>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

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
    /// Translate a 1-arg translation for a user.
    /// </summary>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, player, canUseIMGUI)
            : translation.Translate(arg0, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 1-arg translation for a set of players.
    /// </summary>
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
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0);
    }

    /// <summary>
    /// Translate a 1-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, player, out color, canUseIMGUI)
            : translation.Translate(arg0, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 1-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0>(this Translation<T0> translation, T0 arg0, in LanguageSet set, out Color color, bool canUseIMGUI = false)
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
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

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
    /// Translate a 2-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 2-arg translation for a set of players.
    /// </summary>
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
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1);
    }

    /// <summary>
    /// Translate a 2-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 2-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1>(this Translation<T0, T1> translation, T0 arg0, T1 arg1, in LanguageSet set, out Color color, bool canUseIMGUI = false)
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
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

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
    /// Translate a 3-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 3-arg translation for a set of players.
    /// </summary>
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
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2);
    }

    /// <summary>
    /// Translate a 3-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 3-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2>(this Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, in LanguageSet set, out Color color, bool canUseIMGUI = false)
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
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

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
    /// Translate a 4-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 4-arg translation for a set of players.
    /// </summary>
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
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Translate a 4-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 4-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3>(this Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, in LanguageSet set, out Color color, bool canUseIMGUI = false)
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
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

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
    /// Translate a 5-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 5-arg translation for a set of players.
    /// </summary>
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
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Translate a 5-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 5-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4>(this Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, in LanguageSet set, out Color color, bool canUseIMGUI = false)
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
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());
        
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
    /// Translate a 6-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 6-arg translation for a set of players.
    /// </summary>
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
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Translate a 6-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 6-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5>(this Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, in LanguageSet set, out Color color, bool canUseIMGUI = false)
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
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

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
    /// Translate a 7-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 7-arg translation for a set of players.
    /// </summary>
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
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Translate a 7-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 7-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, in LanguageSet set, out Color color, bool canUseIMGUI = false)
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
    public static string Translate<T0, T1, T2, T3, T4, T5, T6>(this Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    #endregion

    #region 8-arg
    /// <summary>
    /// Translate a 8-arg translation using default language and settings.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Translate a 8-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Translate a 8-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 8-arg translation for a set of players.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Translate a 8-arg translation for a set of players.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Translate a 8-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Translate a 8-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Translate a 8-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 8-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, in LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Translate a 8-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
    #endregion

    #region 9-arg
    /// <summary>
    /// Translate a 9-arg translation using default language and settings.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Translate a 9-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Translate a 9-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 9-arg translation for a set of players.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Translate a 9-arg translation for a set of players.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Translate a 9-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Translate a 9-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Translate a 9-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 9-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, in LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Translate a 9-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
    #endregion

    #region 10-arg
    /// <summary>
    /// Translate a 10-arg translation using default language and settings.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, false, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Translate a 10-arg translation for a player.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, WarfarePlayer player, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, false, player, translation.Options);

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Translate a 10-arg translation for a user.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, ICommandUser user, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 10-arg translation for a set of players.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, in LanguageSet set, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0]);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, false, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Translate a 10-arg translation for a set of players.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, LanguageInfo? language, CultureInfo? culture, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, false, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Translate a 10-arg translation using default language and settings and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, out Color color, bool imgui = false, bool forTerminal = false)
    {
        TranslationValue value = translation.GetValueForLanguage(null);

        TranslationOptions translationOptions = translation.Options;
        if (forTerminal)
            translationOptions |= TranslationOptions.ForTerminal;
        TranslationArguments arguments = new TranslationArguments(value, imgui, true, value.Language, null, null, translationOptions, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Translate a 10-arg translation for a player and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, WarfarePlayer player, out Color color, bool canUseIMGUI = false)
    {
        TranslationValue value = translation.GetValueForLanguage(player.Locale.LanguageInfo);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && player.Save.IMGUI, true, player, translation.Options);

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Translate a 10-arg translation for a user and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, ICommandUser user, out Color color, bool canUseIMGUI = false)
    {
        return user is WarfarePlayer player
            ? translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, out color, canUseIMGUI)
            : translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, out color, canUseIMGUI && user.IMGUI, user.IsTerminal);
    }

    /// <summary>
    /// Translate a 10-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, in LanguageSet set, out Color color, bool canUseIMGUI = false)
    {
        if (set.Players.Count == 1)
        {
            return translation.Translate(set.Players[0], out color);
        }

        TranslationValue value = translation.GetValueForLanguage(set.Language);

        TranslationArguments arguments = new TranslationArguments(value, canUseIMGUI && set.IMGUI, true, set.Language, null, set.Team, translation.Options, translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Translate a 10-arg translation for a set of players and output the background <paramref name="color"/> of the message.
    /// </summary>
    public static string Translate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, LanguageInfo? language, CultureInfo? culture, out Color color, bool imgui = false)
    {
        TranslationValue value = translation.GetValueForLanguage(language);

        TranslationArguments arguments = new TranslationArguments(value, imgui, true, language ?? value.Language, null, null, translation.Options, culture ?? translation.LanguageService.GetDefaultCulture());

        color = value.Color;
        return translation.Translate(in arguments, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
    #endregion
}