using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public sealed class OptionsCommand : Command
{
    private const string Syntax = "/options <imgui> [value]";
    private const string Help = "Configure player-specific settings.";

    public OptionsCommand() : base("options", EAdminType.MEMBER)
    {
        AddAlias("settings");
        AddAlias("option");
        AddAlias("config");
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        if (!ctx.HasArgs(2))
        {
            TeamSelector.OpenOptionsMenu(ctx.Caller);
            ctx.Defer();
            return;
        }
        if (ctx.MatchParameter(0, "imgui", "legacyui", "oldui"))
        {
            if (Util.TryParse(ctx.GetRange(1)!, out bool value))
            {
                if (PlayerSave.TryReadSaveFile(ctx.CallerID, out PlayerSave save))
                {
                    if (save.IMGUI == value)
                        throw ctx.Reply(T.OptionsAlreadySet, "IMGUI",
                            Translation.ToString(value, ctx.Caller.Language, null, ctx.Caller, TranslationFlags.None));
                    save.IMGUI = value;
                }
                else
                {
                    save = new PlayerSave(ctx.Caller)
                    {
                        IMGUI = value
                    };
                }
                PlayerSave.WriteToSaveFile(save);
                ctx.Reply(T.OptionsSet, "IMGUI",
                    Translation.ToString(value, ctx.Caller.Language, null, ctx.Caller, TranslationFlags.None));
            }
            else throw ctx.Reply(T.OptionsInvalidValue, ctx.Get(0)!.ToUpperInvariant(), typeof(bool));
        }
        else
            throw ctx.SendCorrectUsage(Syntax);
    }
}