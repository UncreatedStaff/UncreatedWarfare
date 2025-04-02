using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Interaction.Requests;
public static class RequestHelper
{
    /// <summary>
    /// Safely applies costs from a list of costs. If one fails the previous costs are refunded.
    /// </summary>
    public static async Task<bool> TryApplyCosts(IReadOnlyList<UnlockCost>? costs, IRequestable<object> requestable, IRequestResultHandler resultHandler, WarfarePlayer player, Team team, CancellationToken token = default)
    {
        if (costs == null || costs.Count == 0)
            return true;

        foreach (UnlockCost cost in costs)
        {
            if (player.IsOnline && await cost.CanApply(player, team, token))
                continue;

            await UniTask.SwitchToMainThread(token);
            if (player.IsOnline)
                resultHandler.MissingUnlockCost(player, requestable, cost);
            return false;
        }

        int failIndex = -1;

        for (int i = 0; i < costs.Count; ++i)
        {
            UnlockCost cost = costs[i];
            if (!player.IsOnline)
            {
                failIndex = i;
                break;
            }

            bool success = await cost.TryApply(player, team, CancellationToken.None);
            if (success)
                continue;

            failIndex = i;
            break;
        }

        if (failIndex == -1)
        {
            return true;
        }

        for (int i = failIndex - 1; i >= 0; --i)
        {
            await costs[i].Undo(player, team, CancellationToken.None);
        }

        await UniTask.SwitchToMainThread(token);
        if (player.IsOnline)
            resultHandler.MissingUnlockCost(player, requestable, costs[failIndex]);
        return false;
    }

    /// <summary>
    /// Attempts to fufill a request for an object of an unknown type for <paramref name="player"/>.
    /// </summary>
    public static Task<bool> RequestAsync(WarfarePlayer player, IRequestable<object> requestable, ILogger logger, IServiceProvider serviceProvider, Type requestResultHandlerType, CancellationToken token = default)
    {
        if (!typeof(IRequestResultHandler).IsAssignableFrom(requestResultHandlerType))
        {
            throw new ArgumentException($"Request result handler type must implement {Accessor.ExceptionFormatter.Format<IRequestResultHandler>()}.");
        }

        Type requestSourceType = requestable.GetType();

        // get value of IRequestable< ? > for 'requestable'
        Type requestValueType = requestSourceType.GetInterfaces()
            .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestable<>))
            .GetGenericArguments()[0];

        //                           \/ requestSourceType     \/ requestValueType
        // example: IRequestHandler<KitSignInstanceProvider, Kit>
        Type requestType = typeof(IRequestHandler<,>).MakeGenericType(requestSourceType, requestValueType);

        object? reqHandler = serviceProvider.GetService(requestType);
        if (reqHandler == null)
        {
            logger.LogError("Missing service for request handler {0}.", requestType);
            serviceProvider.GetRequiredService<ChatService>().Send(player, serviceProvider.GetRequiredService<TranslationInjection<CommonTranslations>>().Value.GamemodeError);
            return Task.FromResult(false);
        }

        IRequestResultHandler resultHandler = (IRequestResultHandler)ActivatorUtilities.CreateInstance(serviceProvider, requestResultHandlerType);

        // gets the implemented RequestAsync method for an interface
        MethodInfo method = requestType.GetMethod("RequestAsync", BindingFlags.Public | BindingFlags.Instance)!;
        method = Accessor.GetImplementedMethod(reqHandler.GetType(), method)!;

        // call RequestAsync
        return (Task<bool>)method.Invoke(reqHandler, [ player, requestable, resultHandler, token ]);
    }

    public static IRequestable<object>? GetRequestable(Transform transform, SignInstancer signInstancer)
    {
        IRequestable<object>? requestable = ContainerHelper.FindComponent<IRequestable<object>>(transform);
        if (requestable != null || !transform.CompareTag("Barricade") && !transform.CompareTag("Vehicle"))
        {
            return requestable;
        }

        BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
        if (barricade?.interactable is not InteractableSign)
        {
            return null;
        }

        ISignInstanceProvider? provider = signInstancer.GetSignProvider(barricade);
        return provider as IRequestable<object>;
    }
}
