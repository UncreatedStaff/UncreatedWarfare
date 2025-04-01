using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Uncreated.Warfare.Services;

internal static class AutofacContainerExtensions
{
    private static MethodInfo? _regGenMtd;
    private static void CheckRegisterMethod()
    {
        MethodInfo? regGenMtd = typeof(Autofac.RegistrationExtensions).GetMethod("RegisterType", BindingFlags.Static | BindingFlags.Public, null, [ typeof(ContainerBuilder) ], null);

        if (regGenMtd != null && regGenMtd.IsGenericMethodDefinition && regGenMtd.GetGenericArguments().Length == 1)
        {
            _regGenMtd = regGenMtd;
            return;
        }

        _regGenMtd = null;
        throw new UnexpectedMemberAccessException(new MethodDefinition("RegisterType")
            .WithNoParameters()
            .WithGenericParameterDefinition("TImplementer")
            .Returning(typeof(IRegistrationBuilder<,,>))
        );
    }

    /// <summary>
    /// Register services using an <see cref="IServiceCollection"/> instead of a <see cref="ContainerBuilder"/>.
    /// </summary>
    public static void RegisterFromCollection(this ContainerBuilder bldr, Action<IServiceCollection> action)
    {
        if (action == null)
            return;

        IServiceCollection container = new ServiceCollection();
        action(container);

        bldr.Populate(container);
    }

    /// <summary>
    /// Register services using their ModularRPC proxy types as <typeparamref name="TImplementedType"/>.
    /// </summary>
    public static IRegistrationBuilder<TImplementedType, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRpcType<TImplementedType>(
        this ContainerBuilder bldr) where TImplementedType : class
    {
        return RegisterRpcType<TImplementedType, TImplementedType>(bldr);
    }

    /// <summary>
    /// Register services using their ModularRPC proxy types as <typeparamref name="TServiceType"/>.
    /// </summary>
    public static IRegistrationBuilder<TImplementedType, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRpcType<TImplementedType, TServiceType>(
        this ContainerBuilder bldr) where TImplementedType : class, TServiceType where TServiceType : notnull
    {
        if (_regGenMtd == null)
            CheckRegisterMethod();

        Type proxyType = ProxyGenerator.Instance.GetProxyType<TImplementedType>();

        MethodInfo definedMtd = _regGenMtd!.MakeGenericMethod(proxyType);

        IRegistrationBuilder<TImplementedType, ConcreteReflectionActivatorData, SingleRegistrationStyle> builder =
            (IRegistrationBuilder<TImplementedType, ConcreteReflectionActivatorData, SingleRegistrationStyle>)
            definedMtd.Invoke(null, [ bldr ])!;

        return builder.As<TServiceType>();
    }
}
