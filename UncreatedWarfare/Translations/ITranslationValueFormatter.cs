using System;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Translations;

/// <summary>
/// Responsible for colorizing, formatting, and internationalizing values passed to translations.
/// </summary>
public interface ITranslationValueFormatter
{
    /// <summary>
    /// The language service being used with this formatter.
    /// </summary>
    LanguageService LanguageService { get; }

    /// <summary>
    /// The translation service being used with this formatter.
    /// </summary>
    ITranslationService TranslationService { get; }

    /// <summary>
    /// Converts a value into a human-readable format.
    /// </summary>
    /// <typeparam name="T">The type of value to format.</typeparam>
    /// <param name="value">The value to convert into a string.</param>
    /// <param name="parameters">Options to influence how the value is converted.</param>
    /// <returns>A human-readable string, optionally colorized and formatted to the viewer's locale.</returns>
    string Format<T>(T? value, in ValueFormatParameters parameters);

    /// <summary>
    /// Converts a value into a human-readable format.
    /// </summary>
    /// <param name="value">The value to convert into a string.</param>
    /// <param name="parameters">Options to influence how the value is converted.</param>
    /// <param name="formatType"><paramref name="value"/>'s type.</param>
    /// <returns>A human-readable string, optionally colorized and formatted to the viewer's locale.</returns>
    string Format(object? value, in ValueFormatParameters parameters, Type? formatType = null);

    /// <summary>
    /// Translates an enum value into the given language.
    /// </summary>
    /// <typeparam name="TEnum">Type of <see cref="Enum"/> to localize.</typeparam>
    /// <param name="value">The enum to localize.</param>
    /// <param name="language">The language to translate the enum into.</param>
    /// <returns>A localized translation of the given enum value, or <see cref="Enum.ToString"/> as a fallback.</returns>
    string FormatEnum<TEnum>(TEnum value, LanguageInfo? language) where TEnum : unmanaged, Enum;

    /// <summary>
    /// Gets the value formatter for a type.
    /// </summary>
    /// <param name="type">The type to be formatted.</param>
    /// <returns>A value formatter used to format and localize values of the given type.</returns>
    IValueFormatter GetValueFormatter(Type type);

    /// <summary>
    /// Translates an enum value into the given language.
    /// </summary>
    /// <param name="value">The enum to localize.</param>
    /// <param name="language">The language to translate the enum into.</param>
    /// <param name="enumType">Type of <see cref="Enum"/> to localize.</param>
    /// <returns>A localized translation of the given enum value, or <see cref="Enum.ToString"/> as a fallback.</returns>
    string FormatEnum(object value, Type enumType, LanguageInfo? language);

    /// <inheritdoc cref="Colorize(string,Color32,TranslationOptions)"/>
    string Colorize(ReadOnlySpan<char> text, Color32 color, TranslationOptions options);

    /// <summary>
    /// Applies a platform-specific color to the given <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <param name="color">The color to make the text.</param>
    /// <param name="options">Translation options specifying the method of colorizing to use.</param>
    /// <returns>The text wrapped with color tags.</returns>
    string Colorize(string text, Color32 color, TranslationOptions options);
}