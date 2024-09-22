﻿using System;
using System.Reflection;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("holiday")]
public class HolidayCommand : IExecutableCommand
{
    private readonly ITranslationValueFormatter _valueFormatter;
    private static readonly PermissionLeaf PermissionSetHoliday = new PermissionLeaf("commands.holiday.set", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public HolidayCommand(ITranslationValueFormatter valueFormatter)
    {
        _valueFormatter = valueFormatter;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "View or set the current holiday.",
            Parameters =
            [
                new CommandParameter("holiday", typeof(ENPCHoliday))
                {
                    Description = "Set the current holiday manually.",
                    Permission = PermissionSetHoliday,
                    IsOptional = true
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.ArgumentCount == 0)
        {
            throw Context.ReplyString($"Current holiday: \"{HolidayUtil.getActiveHoliday()}\".");
        }

        await Context.AssertPermissions(PermissionSetHoliday, token);

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
    }
}