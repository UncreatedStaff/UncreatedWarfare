using System;

namespace Uncreated.Warfare.Translations;

[Flags]
public enum TranslationOptions
{
    None = 0,

    /// <summary>
    /// Tells the translator to prioritize using base Unity rich text instead of TMPro rich text.
    /// </summary>
    TranslateWithUnityRichText = 1,

    /// <summary>
    /// Tells the translator to replace &lt;#ffffff&gt; format with &lt;color=#ffffff&gt;.
    /// </summary>
    ReplaceTMProRichText = 1 << 1,

    /// <summary>
    /// Tells the translator to target Unity rich text instead of TMPro rich text and replace &lt;#ffffff&gt; tags with &lt;color=#ffffff&gt; tags.
    /// </summary>
    UseUnityRichText = TranslateWithUnityRichText | ReplaceTMProRichText,

    /// <summary>
    /// Tells the translator to translate the messsage for each player separately when broadcasted.
    /// </summary>
    PerPlayerTranslation = 1 << 2,

    /// <summary>
    /// Tells the translator to translate the messsage for each team separately when broadcasted.
    /// </summary>
    PerTeamTranslation = 1 << 3,

    /// <summary>
    /// The translation is for a custom UI element.
    /// </summary>
    UI = 1 << 4,

    /// <summary>
    /// The translation is for a sign.
    /// </summary>
    Sign = 1 << 5,

    /// <summary>
    /// Use for translations to be used on TMPro UI.
    /// </summary>
    TMProUI = UI,

    /// <summary>
    /// Use for translations to be used on TMPro sign.
    /// </summary>
    TMProSign = Sign,

    /// <summary>
    /// Use for translations to be used on non-TMPro UI. Converts to &lt;color=#ffffff&gt; format.
    /// </summary>
    UnityUI = UI | UseUnityRichText,

    /// <summary>
    /// Use for translations to be used on non-TMPro UI. Convert to &lt;color=#ffffff&gt; format, doesn't replace already existing TMPro tags.
    /// </summary>
    UnityUINoReplace = UI | TranslateWithUnityRichText,

    /// <summary>
    /// Tells <see cref="ITranslationValueFormatter"/>'s to not add rich text to arguments. All rich text will also be removed from the original translation.
    /// </summary>
    NoRichText = 1 << 6,

    /// <summary>
    /// Tells the translator to prioritize using virtual terminal sequences instead of TMPro or Unity rich text.
    /// </summary>
    TranslateWithTerminalRichText = 1 << 7,

    /// <summary>
    /// Tells the translator to replace any rich text codes with the equivalent virtual terminal sequences.
    /// </summary>
    ReplaceRichTextWithTerminalRichText = 1 << 8,

    /// <summary>
    /// Configures the translator to output text that can be easily viewed from the termnial.
    /// </summary>
    ForTerminal = TranslateWithTerminalRichText | ReplaceRichTextWithTerminalRichText
}
