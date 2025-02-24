using System;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations;

/// <summary>
/// This class stores the actual values for each language.
/// </summary>
public class TranslationValue
{

    private int _colorStrippedValueStart;
    private int _colorStrippedValueLength;

    private int _imguiColorStrippedValueStart;
    private int _imguiColorStrippedValueLength;

    private string? _colorStrippedValueCache;
    private string? _colorStrippedIMGUIValueCache;

#nullable disable
    private ArgumentSpan[] _pluralizations;
    private ArgumentSpan[] _imguiPluralizations;
    private ArgumentSpan[] _terminalPluralizations;
    private string _terminalValue;
    private string _imguiValue;

    /// <summary>
    /// Translation this value belongs to.
    /// </summary>
    public Translation Translation { get; }

    /// <summary>
    /// Language of translations this value contains.
    /// </summary>
    public LanguageInfo Language { get; }

    /// <summary>
    /// The raw value of the translation without pluralization replacements.
    /// </summary>
    public string RawValue { get; private set; }

    /// <summary>
    /// The value of the translation, with pluralizations replaced with their singular values.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Value converted to IMGUI compatability. IMGUI uses full color tags instead of the TMPro shortcut.
    /// </summary>
    public string IMGUIValue
    {
        get
        {
            if (_imguiPluralizations == null)
                InitIMGUI();

            return _imguiValue;
        }
    }

    /// <summary>
    /// Value converted to virtual terminal sequences.
    /// </summary>
    public string TerminalValue
    {
        get
        {
            if (_terminalPluralizations == null)
                InitTerminal();

            return _terminalValue;
        }
    }

    /// <summary>
    /// The value of the translation, with pluralizations replaced with their singular values and the outer color span removed.
    /// </summary>
    /// <remarks>Use <see cref="ColorStrippedValueSpan"/> if possible.</remarks>
    public string ColorStrippedValue => _colorStrippedValueCache ??= new string(ColorStrippedValueSpan);

    /// <summary>
    /// Value converted to IMGUI compatability with the outer color span removed. IMGUI uses full color tags instead of the TMPro shortcut.
    /// </summary>
    /// <remarks>Use <see cref="ColorStrippedIMGUIValueSpan"/> if possible.</remarks>
    public string ColorStrippedIMGUIValue
    {
        get
        {
            if (_imguiPluralizations == null)
                InitIMGUI();
            
            return _colorStrippedIMGUIValueCache ??= new string(ColorStrippedIMGUIValueSpan);
        }
    }

#nullable restore

    /// <summary>
    /// Background color for the message used for sending chat messages, defaulting to white.
    /// </summary>
    public Color Color { get; private set; }

    /// <summary>
    /// Span of text excluding the background color.
    /// </summary>
    public ReadOnlySpan<char> ColorStrippedValueSpan => Value.AsSpan(_colorStrippedValueStart, _colorStrippedValueLength);

    /// <summary>
    /// Span of text excluding the background color (IMGUI).
    /// </summary>
    public ReadOnlySpan<char> ColorStrippedIMGUIValueSpan
    {
        get
        {
            if (_imguiPluralizations == null)
                InitTerminal();

            return IMGUIValue.AsSpan(_imguiColorStrippedValueStart, _imguiColorStrippedValueLength);
        }
    }

    /// <summary>
    /// Offset into the <see cref="Value"/> string the color-stripped value starts.
    /// </summary>
    public int ColorStrippedValueOffset => _colorStrippedValueStart;

    /// <summary>
    /// Offset into the <see cref="IMGUIValue"/> string the color-stripped IMGUI value starts.
    /// </summary>
    public int ColorStrippedIMGUIValueOffset => _imguiColorStrippedValueStart;

    public TranslationValue(LanguageInfo language, string value, Translation translation)
    {
        Language = language;
        Translation = translation;
        RawValue = value;
        SetValue(value);
    }

    internal ArgumentSpan[] GetPluralizations(in TranslationArguments args, out int argumentOffset)
    {
        if (!Language.SupportsPluralization)
        {
            argumentOffset = 0;
            return Array.Empty<ArgumentSpan>();
        }

        if ((args.Options & TranslationOptions.ForTerminal) != 0)
        {
            if (_terminalPluralizations == null)
                InitTerminal();

            argumentOffset = 0;
            return _terminalPluralizations!;
        }

        if (args.UseIMGUI)
        {
            if (_imguiPluralizations == null)
                InitIMGUI();

            argumentOffset = args.UseUncoloredTranslation ? _imguiColorStrippedValueStart : 0;
            return _imguiPluralizations!;
        }

        argumentOffset = args.UseUncoloredTranslation ? _colorStrippedValueStart : 0;
        return _pluralizations;
    }

    public ReadOnlySpan<char> GetValueSpan(bool useIMGUI, bool useUncoloredTranslation, bool forTerminal)
    {
        if (forTerminal)
        {
            return TerminalValue;
        }
        if (useIMGUI)
        {
            return useUncoloredTranslation ? ColorStrippedIMGUIValueSpan : IMGUIValue;
        }

        return useUncoloredTranslation ? ColorStrippedValueSpan : Value;
    }
    
    public string GetValueString(bool useIMGUI, bool useUncoloredTranslation, bool forTerminal)
    {
        if (forTerminal)
        {
            return TerminalValue;
        }
        if (useIMGUI)
        {
            return useUncoloredTranslation ? ColorStrippedIMGUIValue : IMGUIValue;
        }

        return useUncoloredTranslation ? ColorStrippedValue : Value;
    }

    public void SetValue(string value)
    {
        RawValue = value;
        _pluralizations = TranslationArgumentModifiers.ExtractModifiers(out string? newValue, value, 'p');

        Value = newValue ?? value;

        Color? cNormal = TranslationFormattingUtility.ExtractColor(Value, out _colorStrippedValueStart, out _colorStrippedValueLength);
        Color = cNormal.HasValue ? cNormal.Value with { a = 1f } : Color.white;
        _imguiPluralizations = null;
        _terminalPluralizations = null;
    }

    private void InitIMGUI()
    {
        if (_pluralizations.Length > 0)
        {
            string imguiUnformatted = TranslationFormattingUtility.CreateIMGUIString(RawValue);
            _imguiPluralizations = TranslationArgumentModifiers.ExtractModifiers(out string? newIMGUIString, imguiUnformatted, 'p');
            _imguiValue = newIMGUIString ?? imguiUnformatted;
        }
        else
        {
            _imguiPluralizations = _pluralizations;
            _imguiValue = TranslationFormattingUtility.CreateIMGUIString(Value);
        }

        TranslationFormattingUtility.ExtractColor(_imguiValue, out _imguiColorStrippedValueStart, out _imguiColorStrippedValueLength);
    }

    private void InitTerminal()
    {
        if (_pluralizations.Length > 0)
        {
            string terminalUnformatted = TerminalColorHelper.ConvertRichTextToVirtualTerminalSequences(RawValue, Translation.TranslationService.TerminalColoring);
            _terminalPluralizations = TranslationArgumentModifiers.ExtractModifiers(out string? newTerminalString, terminalUnformatted, 'p');
            _terminalValue = newTerminalString ?? terminalUnformatted;
        }
        else
        {
            _terminalPluralizations = _pluralizations;
            _terminalValue = TerminalColorHelper.ConvertRichTextToVirtualTerminalSequences(Value, Translation.TranslationService.TerminalColoring);
        }
    }
}