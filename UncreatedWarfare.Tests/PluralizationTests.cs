using NUnit.Framework;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Tests.Utility;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Tests;
public class PluralizationTests
{
    private static readonly LanguageInfo Language = new LanguageInfo
    {
        Code = "en-us",
        DefaultCultureCode = "en-US",
        Key = 1,
        HasTranslationSupport = true,
        SupportsPluralization = true,
        SteamLanguageName = "English",
        DisplayName = "English",
        NativeName = "English",
        IsDefault = true,
        RequiresIMGUI = false
    };


    [Test]
    public void OnePluralizationInput1([Values(0, 1, 2)] int formatMode, [Values(true, false)] bool extractColor)
    {
        const string str = "<#fae69c>Now deploying to {0}. You will arrive in <#eee>{1} ${p:1:second}</color>.";
        Translation tl = new Translation(str, new TestTranslationCollection(), new TestTranslationService());
        TranslationValue value = new TranslationValue(Language, str, tl);

        TranslationArguments args = new TranslationArguments(value, formatMode == 1, extractColor, value.Language, null, null, formatMode == 2 ? TranslationOptions.ForTerminal : TranslationOptions.None, CultureInfo.InvariantCulture, TimeZoneInfo.Utc);
        ArgumentSpan[] spans = value.GetPluralizations(in args, out int offset);

        value.GetValueString(args.UseIMGUI, args.UseUncoloredTranslation, (args.Options & TranslationOptions.ForTerminal) != 0);

        Assert.That(spans.Length, Is.EqualTo(1));

        string output = TranslationPluralizations.ApplyPluralizers(in args, spans, offset, 2, index => index switch
        {
            0 => "location",
            1 => 1,
            _ => throw new SwitchExpressionException()
        }).ToString();

        Assert.That(output.Contains("second"), Is.True);
        Assert.That(output.Contains("seconds"), Is.False);
    }

    [Test]
    public void OnePluralizationInput2([Values(0, 1, 2)] int formatMode, [Values(true, false)] bool extractColor)
    {
        const string str = "<#fae69c>Now deploying to {0}. You will arrive in <#eee>{1} ${p:1:second}</color>.";
        Translation tl = new Translation(str, new TestTranslationCollection(), new TestTranslationService());
        TranslationValue value = new TranslationValue(Language, str, tl);

        TranslationArguments args = new TranslationArguments(value, formatMode == 1, extractColor, value.Language, null, null, formatMode == 2 ? TranslationOptions.ForTerminal : TranslationOptions.None, CultureInfo.InvariantCulture, TimeZoneInfo.Utc);
        ArgumentSpan[] spans = value.GetPluralizations(in args, out int offset);

        value.GetValueString(args.UseIMGUI, args.UseUncoloredTranslation, (args.Options & TranslationOptions.ForTerminal) != 0);

        Assert.That(spans.Length, Is.EqualTo(1));


        string output = TranslationPluralizations.ApplyPluralizers(in args, spans, offset, 2, index => index switch
        {
            0 => "location",
            1 => 2,
            _ => throw new SwitchExpressionException()
        }).ToString();

        Assert.That(output.Contains("seconds"), Is.True);
    }

}
