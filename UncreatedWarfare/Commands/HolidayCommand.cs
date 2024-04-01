using SDG.Unturned;
using System.Reflection;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class HolidayCommand : Command
{
    public HolidayCommand() : base("holiday", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = "Prints the current holiday.",
            Parameters =
            [
                new CommandParameter("holiday", typeof(ENPCHoliday))
                {
                    Description = "Set the current holiday manually.",
                    Permission = EAdminType.VANILLA_ADMIN,
                    IsOptional = true
                }
            ]
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        if (ctx.ArgumentCount == 0)
            throw ctx.ReplyString("Current holiday: \"" + HolidayUtil.getActiveHoliday() + "\".");

        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        if (!ctx.TryGet(0, out ENPCHoliday holiday))
            throw ctx.ReplyString("Bad holiday.");

        FieldInfo? field = typeof(Provider).GetField("authorityHoliday",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        if (field == null)
            throw ctx.SendUnknownError();

        field.SetValue(null, holiday);
    }
}