using DanielWillett.ReflectionTools;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Commands;

[Command("request", "req", "r"), SynchronizedCommand, MetadataFile]
public sealed class RequestCommand : ICompoundingCooldownCommand
{
    private readonly SignInstancer _signInstancer;
    private readonly WarfareModule _module;
    private readonly RequestTranslations _translations;
    public float CompoundMultiplier => 2f;
    public float MaxCooldown => 900f; // 15 mins

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public RequestCommand(
        TranslationInjection<RequestTranslations> translations,
        SignInstancer signInstancer,
        WarfareModule module)
    {
        _signInstancer = signInstancer;
        _module = module;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        IRequestable<object>? requestable = GetRequestable();

        if (requestable == null)
        {
            throw Context.Reply(_translations.RequestNoTarget);
        }

        Type requestSourceType = requestable.GetType();

        // get value of IRequestable< ? > for 'requestable'
        Type requestValueType = requestSourceType.GetInterfaces()
            .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestable<>))
            .GetGenericArguments()[0];

        //                           \/ requestSourceType     \/ requestValueType
        // example: IRequestHandler<KitSignInstanceProvider, Kit>
        Type requestType = typeof(IRequestHandler<,>).MakeGenericType(requestSourceType, requestValueType);

        object? reqHandler = _module.ScopedProvider.ResolveOptional(requestType);
        if (reqHandler == null)
        {
            Context.Logger.LogError("Missing service for request handler {0}.", requestType);
            throw Context.SendGamemodeError();
        }

        RequestCommandResultHandler resultHandler = ActivatorUtilities.CreateInstance<RequestCommandResultHandler>(_module.ScopedProvider.Resolve<IServiceProvider>());

        // gets the implemented RequestAsync method for an interface
        MethodInfo method = requestType.GetMethod("RequestAsync", BindingFlags.Public | BindingFlags.Instance)!;
        method = Accessor.GetImplementedMethod(reqHandler.GetType(), method)!;

        // call RequestAsync
        await (Task<bool>)method.Invoke(reqHandler, [ Context.Player, requestable, resultHandler, token ]);
        Context.Defer();
    }

    private IRequestable<object>? GetRequestable()
    {
        if (!Context.TryGetTargetTransform(out Transform? transform))
        {
            return null;
        }

        IRequestable<object>? requestable = ContainerHelper.FindComponent<IRequestable<object>>(transform);
        if (requestable != null)
        {
            return requestable;
        }

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? drop) || drop.interactable is not InteractableSign)
        {
            return null;
        }

        ISignInstanceProvider? provider = _signInstancer.GetSignProvider(drop);
        return provider as IRequestable<object>;
    }
}