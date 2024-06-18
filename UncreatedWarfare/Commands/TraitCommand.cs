using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Traits;
using UnityEngine;

namespace Uncreated.Warfare.Commands;

[Command("trait")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class TraitCommand : IExecutableCommand
{
    private const string Syntax = "/trait <give|take|clear|set>";
    private const string Help = "Manage properties of traits";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Manage properties of traits.",
            Parameters =
            [
                new CommandParameter("Give")
                {
                    Aliases = [ "get" ],
                    Description = "Give the caller a trait.",
                    Parameters =
                    [
                        new CommandParameter("Trait", typeof(TraitData))
                    ]
                },
                new CommandParameter("Take")
                {
                    Aliases = [ "revoke", "remove" ],
                    Description = "Remove a trait from the caller.",
                    Parameters =
                    [
                        new CommandParameter("Trait", typeof(TraitData))
                    ]
                },
                new CommandParameter("Clear")
                {
                    Description = "Clear all traits from the caller."
                },
                new CommandParameter("Set")
                {
                    Description = "Set a config value for a trait.",
                    Parameters =
                    [
                        new CommandParameter("Trait", typeof(TraitData))
                        {
                            Parameters =
                            [
                                new CommandParameter("Property", typeof(string))
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("Value", typeof(object))
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertOnDuty();

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertGamemode<ITraits>();

        if (!TraitManager.Loaded)
            throw Context.SendGamemodeError();

        if (Context.MatchParameter(0, "give", "get"))
        {
            Context.AssertHelpCheck(1, "/trait <give|get> <trait...> - Gives a trait to the caller.");

            Context.AssertRanByPlayer();

            if (!Context.TryGetRange(1, out string? trait))
                throw Context.SendCorrectUsage("/trait <give|get> <trait...>");
            
            TraitData? data = TraitManager.FindTrait(trait);
            if (data is null)
                throw Context.Reply(T.TraitNotFound, trait);

            TraitManager.GiveTrait(Context.Player, data);
            Context.LogAction(ActionLogType.GiveTrait, data.TypeName);
            Context.Defer();
            return default;
        }
        else if (Context.MatchParameter(0, "take", "revoke", "remove"))
        {
            Context.AssertHelpCheck(1, "/trait <take|revoke|remove> <trait> - Removes the given trait from the caller.");
            Context.AssertRanByPlayer();

            if (!Context.TryGetRange(1, out string? trait))
                throw Context.SendCorrectUsage("/trait <give|get> <trait...>");
            
            TraitData? data = TraitManager.FindTrait(trait);
            if (data is null)
                throw Context.Reply(T.TraitNotFound, trait);

            for (int i = 0; i < Context.Player.ActiveTraits.Count; ++i)
            {
                if (Context.Player.ActiveTraits[i].Data.Type != data.Type)
                    continue;

                Trait t = Context.Player.ActiveTraits[i];
                UnityEngine.Object.Destroy(t);
                Context.LogAction(ActionLogType.RevokeTrait, data.TypeName + " - Active for " + Localization.GetTimeFromSeconds(Mathf.CeilToInt(Time.realtimeSinceStartup - t.StartTime)));
                throw Context.Reply(T.TraitRemoved, data);
            }
            throw Context.Reply(T.TraitNotActive, data);
        }
        
        if (Context.MatchParameter(0, "clear"))
        {
            Context.AssertHelpCheck(1, "/trait clear - Removes all traits from the player.");
            Context.AssertRanByPlayer();

            int ct = 0;
            for (int i = 0; i < Context.Player.ActiveTraits.Count; ++i)
            {
                UnityEngine.Object.Destroy(Context.Player.ActiveTraits[i]);
                ++ct;
            }

            if (ct == 0)
                throw Context.Reply(T.NoTraitsToClear);
            Context.Player.ActiveTraits.Clear();
            Context.LogAction(ActionLogType.ClearTraits, ct.ToString(Data.AdminLocale) + " trait(s) cleared.");
            Context.Reply(T.TraitsCleared, ct);
            return default;
        }
        
        if (Context.MatchParameter(0, "set"))
        {
            Context.AssertHelpCheck(1, "/trait <set> <trait> <property> <value...> - Set a property of a trait and update it.");

            if (!Context.TryGet(1, out string trait))
                throw Context.SendCorrectUsage("/trait <set> <trait> <property> <value...>");

            TraitData? data = TraitManager.FindTrait(trait);
            if (data is null)
                throw Context.Reply(T.TraitNotFound, trait);

            if (!Context.TryGetRange(3, out string? value) || !Context.TryGet(2, out string? property))
            {
                throw Context.SendCorrectUsage("/trait <set> <trait> <property> <value...>");
            }

            SetPropertyResult result = Context.SetProperty(data, property, value, out property, out Type propertyType);
            switch (result)
            {
                case SetPropertyResult.Success:
                    Context.LogAction(ActionLogType.SetTraitProperty, $"{data.TypeName} - SET " + property.ToUpper() + " >> " + value.ToUpper());
                    Signs.UpdateTraitSigns(null, data);
                    TraitManager.Singleton.Save();
                    throw Context.Reply(T.TraitSetProperty, data, property, value);

                case SetPropertyResult.PropertyNotFound:
                    throw Context.Reply(T.TraitInvalidProperty, property);

                case SetPropertyResult.PropertyProtected:
                    throw Context.Reply(T.TraitNotJsonSettable, property);

                case SetPropertyResult.TypeNotSettable:
                case SetPropertyResult.ParseFailure:
                    throw Context.Reply(T.TraitInvalidSetValue, value, property, propertyType);

                default:
                    throw Context.SendUnknownError();
            }
        }
        
        throw Context.SendCorrectUsage(Syntax);
    }
}
