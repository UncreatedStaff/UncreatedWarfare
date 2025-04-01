using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("holiday"), MetadataFile]
internal sealed class HolidayCommand : IExecutableCommand
{
    private readonly ITranslationValueFormatter _valueFormatter;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public HolidayCommand(ITranslationValueFormatter valueFormatter)
    {
        _valueFormatter = valueFormatter;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.ArgumentCount != 0)
            throw Context.SendHelp();

        Context.ReplyString($"Current holiday: {_valueFormatter.FormatEnum(HolidayUtil.getActiveHoliday(), Context.Language)}.");
        return UniTask.CompletedTask;
    }
}