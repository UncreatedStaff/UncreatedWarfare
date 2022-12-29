using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Traits;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class TraitCommand : Command
{
    private const string Syntax = "/trait <give|take|clear|set>";
    private const string Help = "Manage properties of traits";

    public TraitCommand() : base("trait", EAdminType.ADMIN_ON_DUTY) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertOnDuty();

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        ctx.AssertGamemode<ITraits>();

        if (!TraitManager.Loaded)
            throw ctx.SendGamemodeError();

        if (ctx.MatchParameter(0, "give", "get"))
        {
            ctx.AssertHelpCheck(1, "/trait <give|get> <trait...> - Gives a trait to the caller.");

            ctx.AssertRanByPlayer();

            if (ctx.TryGetRange(1, out string trait))
            {
                TraitData? data = TraitManager.FindTrait(trait);
                if (data is null)
                    throw ctx.Reply(T.TraitNotFound, trait);

                TraitManager.GiveTrait(ctx.Caller, data);
                ctx.LogAction(ActionLogType.GiveTrait, data.TypeName);
                ctx.Defer();
            }
            else throw ctx.SendCorrectUsage("/trait <give|get> <trait...>");
        }
        else if (ctx.MatchParameter(0, "take", "revoke", "remove"))
        {
            ctx.AssertHelpCheck(1, "/trait <take|revoke|remove> <trait> - Removes the given trait from the caller.");
            ctx.AssertRanByPlayer();

            if (ctx.TryGetRange(1, out string trait))
            {
                TraitData? data = TraitManager.FindTrait(trait);
                if (data is null)
                    throw ctx.Reply(T.TraitNotFound, trait);

                for (int i = 0; i < ctx.Caller.ActiveTraits.Count; ++i)
                {
                    if (ctx.Caller.ActiveTraits[i].Data.Type == data.Type)
                    {
                        Trait t = ctx.Caller.ActiveTraits[i];
                        Object.Destroy(t);
                        ctx.LogAction(ActionLogType.RevokeTrait, data.TypeName + " - Active for " + Mathf.CeilToInt(Time.realtimeSinceStartup - t.StartTime).GetTimeFromSeconds(L.Default));
                        throw ctx.Reply(T.TraitRemoved, data);
                    }
                }
                throw ctx.Reply(T.TraitNotActive, data);
            }
            throw ctx.SendCorrectUsage("/trait <give|get> <trait...>");
        }
        else if (ctx.MatchParameter(0, "clear"))
        {
            ctx.AssertHelpCheck(1, "/trait clear - Removes all traits from the player.");
            ctx.AssertRanByPlayer();

            int ct = 0;
            for (int i = 0; i < ctx.Caller.ActiveTraits.Count; ++i)
            {
                Object.Destroy(ctx.Caller.ActiveTraits[i]);
                ++ct;
            }

            if (ct == 0)
                throw ctx.Reply(T.NoTraitsToClear);
            ctx.Caller.ActiveTraits.Clear();
            ctx.LogAction(ActionLogType.ClearTraits, ct.ToString(Data.AdminLocale) + " trait(s) cleared.");
            ctx.Reply(T.TraitsCleared, ct);
        }
        else if (ctx.MatchParameter(0, "set"))
        {
            ctx.AssertHelpCheck(1, "/trait <set> <trait> <property> <value...> - Set a property of a trait and update it.");

            if (ctx.TryGet(1, out string trait))
            {
                TraitData? data = TraitManager.FindTrait(trait);
                if (data is null)
                    throw ctx.Reply(T.TraitNotFound, trait);

                if (ctx.TryGetRange(3, out string value) && ctx.TryGet(2, out string property))
                {
                    ESetFieldResult result = TraitManager.Singleton.SetProperty(data, ref property, value);
                    switch (result)
                    {
                        case ESetFieldResult.SUCCESS:
                            ctx.LogAction(ActionLogType.SetTraitProperty, $"{data.TypeName} - SET " + property.ToUpper() + " >> " + value.ToUpper());
                            ctx.Reply(T.TraitSetProperty, data, property, value);
                            Signs.UpdateTraitSigns(null, data);
                            TraitManager.Singleton.Save();
                            return;
                        default:
                        case ESetFieldResult.OBJECT_NOT_FOUND:
                            throw ctx.Reply(T.TraitNotFound, trait);
                        case ESetFieldResult.FIELD_NOT_FOUND:
                            throw ctx.Reply(T.TraitInvalidProperty, property);
                        case ESetFieldResult.FIELD_NOT_SERIALIZABLE:
                        case ESetFieldResult.INVALID_INPUT:
                            throw ctx.Reply(T.TraitInvalidSetValue, value, property);
                        case ESetFieldResult.FIELD_PROTECTED:
                            throw ctx.Reply(T.TraitNotJsonSettable, property);
                    }
                }
            }
            throw ctx.SendCorrectUsage("/trait <set> <trait> <property> <value...>");
        }
        else throw ctx.SendCorrectUsage(Syntax);
    }
}
