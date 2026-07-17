using System;
using System.Reflection;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("setconfigoption"), SubCommandOf(typeof(WarfareDevCommand)), HideFromHelp]
internal sealed class BlankCommand : IExecutableCommand
{
    private readonly PlayerReplicatedConfigManager _manager;

    private static readonly Stack<ChangeInfo> Changes = new Stack<ChangeInfo>();

    public required CommandContext Context { get; init; }

    public BlankCommand(PlayerReplicatedConfigManager manager)
    {
        _manager = manager;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.MatchParameter(0, "undo"))
        {
            if (Changes.Count == 0)
                throw Context.ReplyString("No changes to undo.");

            ChangeInfo info = Changes.Pop();
            info.Disposable.Dispose();
            throw Context.ReplyString($"Undid change: {info.Change}.");
        }

        Context.AssertArgs(3);

        bool global = Context.MatchFlag('g', "global");

        string sectionFieldName = Context.Get(0)!;
        FieldInfo? sectionField = typeof(ModeConfigData).GetField(sectionFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (sectionField == null)
            throw Context.ReplyString($"Section not found: \"{sectionFieldName}\".");

        string valueFieldName = Context.Get(1)!;
        FieldInfo? valueField = sectionField.FieldType.GetField(valueFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (valueField == null)
            throw Context.ReplyString($"Value not found: \"{valueFieldName}\".");

        object section = sectionField.GetValue(Provider.modeConfigData)!;

        string valueInput = Context.GetRange(2)!;

        object? newValue;
        if (string.Equals(valueInput, "reset", StringComparison.InvariantCultureIgnoreCase))
        {
            newValue = valueField.GetValue(section);
        }
        else if (!FormattingUtility.TryParseAny(valueInput, Context.ParseCulture, valueField.FieldType, out newValue))
        {
            throw Context.ReplyString($"Failed to parse {valueField.FieldType} from \"{valueInput}\".");
        }

        IDisposable? change;
        ModifyModeConfig<State> apply = (config, ref state) =>
        {
            object section = sectionField.GetValue(config);
            state.OldValue = valueField.GetValue(section);
            valueField.SetValue(section, newValue);
        };
        ModifyModeConfig<State> undo = (config, ref state) =>
        {
            object section = sectionField.GetValue(config);
            valueField.SetValue(section, state.OldValue);
        };
        if (global)
        {
            change = _manager.ReplicateGlobalConfigChange(apply, undo);
        }
        else
        {
            Context.AssertRanByPlayer();
            change = Context.Player.ReplicateConfigChange(apply, undo);
        }

        if (change != null)
        {
            string changeInfo = $"{sectionField.Name}.{valueField.Name} = \"{newValue}\"";
            Context.ReplyString($"Change added. {changeInfo}. Run /wdev setconfigoption undo to undo.");
            Changes.Push(new ChangeInfo
            {
                Disposable = change,
                Change = changeInfo
            });
        }
        else
        {
            Context.SendUnknownError();
        }

        return UniTask.CompletedTask;
    }

    private struct State
    {
        public object OldValue;
    }

    private struct ChangeInfo
    {
        public IDisposable Disposable;
        public string Change;
    }
}