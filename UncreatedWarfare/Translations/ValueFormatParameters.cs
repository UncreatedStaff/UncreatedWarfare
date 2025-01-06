using System;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations;
public readonly ref struct ValueFormatParameters
{
    public readonly int Argument;
    public readonly CultureInfo Culture;
    public readonly LanguageInfo Language;
    public readonly Team? Team;
    public readonly WarfarePlayer? Player;
    public readonly TranslationOptions Options;
    public readonly ArgumentFormat Format;
    public readonly Func<int, object?>? ArgumentAccessor;
    public readonly int ArgumentCount;

    /// <summary>
    /// If unity rich text should be used over TMPro rich text.
    /// </summary>
    public bool IMGUI => (Options & TranslationOptions.TranslateWithUnityRichText) != 0;
    public ValueFormatParameters(in ValueFormatParameters parameters, TranslationOptions flags)
        : this (parameters.Argument, parameters.Culture, parameters.Language, flags, in parameters.Format, parameters.Team, parameters.Player, parameters.ArgumentAccessor, parameters.ArgumentCount) { }
    public ValueFormatParameters(int argument, in TranslationArguments args, in ArgumentFormat format, Func<int, object?>? argumentAccessor, int argumentCount)
        : this (argument, args.Culture, args.Language, args.Options, in format, args.Team, args.Player, argumentAccessor, argumentCount) { }
    public ValueFormatParameters(int argument, CultureInfo culture, LanguageInfo language, TranslationOptions options, in ArgumentFormat format, Team? team, WarfarePlayer? player, Func<int, object?>? argumentAccessor, int argumentCount)
    {
        Argument = argument;
        Culture = culture;
        Language = language;
        Options = options;
        Format = format;
        ArgumentAccessor = argumentAccessor;
        ArgumentCount = argumentCount;
        Team = team;
        Player = player;
    }
    public ValueFormatParameters(CultureInfo culture, LanguageInfo language, TranslationOptions options, ArgumentFormat format)
    {
        Culture = culture;
        Language = language;
        Options = options;
        Format = format;
    }
    public ValueFormatParameters(CultureInfo culture, LanguageInfo language, TranslationOptions options, ArgumentFormat format, Team team)
    {
        Culture = culture;
        Language = language;
        Options = options;
        Format = format;
        Team = team;
    }
    public ValueFormatParameters(CultureInfo culture, LanguageInfo language, TranslationOptions options, ArgumentFormat format, WarfarePlayer player)
    {
        Culture = culture;
        Language = language;
        Options = options;
        Format = format;
        Player = player;
        Team = player.Team;
    }
}
