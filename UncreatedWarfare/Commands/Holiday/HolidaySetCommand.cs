using System;
using System.Reflection;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("set"), SubCommandOf(typeof(HolidayCommand))]
internal sealed class HolidaySetCommand : IExecutableCommand
{
    private readonly ITranslationValueFormatter _valueFormatter;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public HolidaySetCommand(ITranslationValueFormatter valueFormatter)
    {
        _valueFormatter = valueFormatter;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out ENPCHoliday holiday))
        {
            throw Context.ReplyString("Invalid holiday. Must be field in ENPCHoliday.");
        }

        FieldInfo? field = typeof(HolidayUtil).GetField("holidayOverride", BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null)
        {
            throw Context.ReplyString("Unable to find 'HolidayUtil.holidayOverride' field.");
        }

        field.SetValue(null, holiday);
        Context.ReplyString("Set active holiday to " + _valueFormatter.FormatEnum(holiday, Context.Language));

        field = typeof(Provider).GetField("authorityHoliday", BindingFlags.Static | BindingFlags.NonPublic);
        if (holiday == ENPCHoliday.NONE)
        {
            MethodInfo? method = typeof(HolidayUtil).GetMethod("BackendGetActiveHoliday", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
                holiday = (ENPCHoliday)method.Invoke(null, Array.Empty<object>());
        }

        field?.SetValue(null, holiday);
        return UniTask.CompletedTask;
    }
}