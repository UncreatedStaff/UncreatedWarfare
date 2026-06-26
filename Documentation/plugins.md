# Plugins
Plugins are separately built DLLs that go in the `Warfare/Plugins` folder.

## Dependency Injection

Uncreated heavily uses the Dependency Injection pattern in our codebase, and plugins are expected to do the same. Read more about [dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview) to understand how the pattern works, but basically you place all your functionality in **services**, and register them with a **container**. Then when you need your services, put them in your type's constructor to **inject** them.

Sometimes, you abstract your service with an interface, especially when it could make sense that you need to swap it out with something else. For example, I may have an `IKitDataStore` to load kits from, then implement it with a `MySqlKitsDataStore`.

When we have services who's only job is to handle events, we call that a **tweak**. For example, a tweak is responsible for preventing players from using their gun in safezone. These are registered just the same as services.

Uncreated uses a library called [Autofac](https://autofac.org/) for dependency injection, so information you see about ServiceContainers and AddSingleton<>, etc. do not apply. Instead service registration looks like this:

Normal service registration:
```cs
// Register IntelligenceTicketTracker as the implementation of a service
bldr.RegisterType<IntelligenceTicketTracker>()
    // Allow looking the service up as itself (IntelligenceTicketTracker)
    .AsSelf()
    // Allow looking the service up by all of it's interfaces. This is required for events.
    .AsImplementedInterfaces()
    // Make the service a singleton (only one of it will ever exist during the scope)
    // If this is in an ILayoutServiceConfigurer, the scope is throughout the layout
    // Otherwise (in an IServiceConfigurer), the scope is until the server is restarted
    .SingleInstance();
```

Register a transient service as a specific interface only:
```cs
// Register MapCacheLocationDataStore only as ICacheLocationDataStore
bldr.RegisterType<MapCacheLocationDataStore>()
    .As<ICacheLocationDataStore>();
    // By default, the service is transient
    // This means everytime it's injected a new one is created.
    // This means that every time an ICacheLocationDataStore is needed,
    // the caches will be re-read, which is a good thing because
    // it allows changing them while the server is running
```

Register a layout-scoped service from a `IServiceConfigurer`:
```cs
bldr.RegisterType<FobStrategyMapHandler>()
    // Allow the service to be requested by FobStrategyMapHandler or by any of its interfaces
    // These include IStrategyMapProvider as well as some event listeners.
    .AsSelf().AsImplementedInterfaces()
    // If this service is requested, it will use the same instance throughout a layout,
    // but will create a new one if it's requested next layout
    .InstancePerMatchingLifetimeScope(LifetimeScopeTags.Layout);
```

## Creating a Gamemode

Gamemodes are created as a dedicated plugin.

### Project Setup

Download UniTask from [the UncreatedWarfare repo](https://github.com/UncreatedStaff/UncreatedWarfare/blob/master/Libraries/UniTask.dll) and place it in a `Libraries` folder within your solution. You may have to edit the path to it in the csproj file.

Create a new C# **Class Library** (NOT .NET Framework) project. Choose `.NET Standard 2.1` as the Framework.

Set up your csproj file, updating your RootNamespace to include your gamemode's name.
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <!-- Basic properties -->
    <PropertyGroup>

        <TargetFramework>netstandard2.1</TargetFramework>
        <Nullable>enable</Nullable>
        <LangVersion>preview</LangVersion>
        <NoWarn>NU1701;NU1803;NU1903;NU1904;1702;1591;1587;1711;1735;1573;1574;1570;1584;1658;EF1001;CS0419;NU5104</NoWarn>
        
        <!-- Usually this is Uncreated.Warfare.GamemodeName -->
        <RootNamespace>TODO</RootNamespace>

    </PropertyGroup>

    <!-- Installation -->
    <PropertyGroup>

        <U3DSPath>C:/SteamCMD/steamapps/common/U3DS</U3DSPath>
        <ServerId>UncreatedSeason4</ServerId>

    </PropertyGroup>

    <!-- References -->
    <ItemGroup>

        <PackageReference Include="Uncreated.Warfare" Version="4.*" />

        <PackageReference Include="PolySharp" Version="1.15.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
        </PackageReference>

        <PackageReference Include="Unturned.MSBuild" Version="1.0.0-test7">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
        </PackageReference>

        <!-- Third Party -->
        <Reference Include="UniTask">
            <HintPath>../Libraries/UniTask.dll</HintPath>
        </Reference>

        <UnturnedReference Include="Assembly-CSharp" />
        <UnturnedReference Include="UnityEngine" />
        <UnturnedReference Include="UnityEngine.CoreModule" />
        <UnturnedReference Include="com.rlabrecque.steamworks.net" />

    </ItemGroup>

    <!-- Copy to Plugins directory on build -->
    <Target Name="__CopyToOutDir" AfterTargets="Build" Condition="exists('$(U3DSPath)/Servers/$(ServerId)')">
        <Copy SourceFiles="$(OutDir)/$(AssemblyName).dll" DestinationFiles="$(U3DSPath)/Servers/$(ServerId)/Warfare/Plugins/$(AssemblyName).dll" />
    </Target>

</Project>
```

### Creating your services

Most flag gamemodes will include a component inheriting `DualSidedFlagService`, or if not, at least implementing `IFlagRotationService` and `IFlagListUIProvider`.

This component will also likely implement many `IEventListener<>`s or `IAsyncEventListener<>`s to handle different events. For example, Insurgency implements `IEventListener<PlayerDied>` to add intel to the other team when a person is killed. Learn about [events](events.md) for more information.

A gamemode plugin may also contain a `ILayoutServiceConfigurer` which registers services to the container when the game starts. Each layout creates its own scope, so registering a singleton will register a service that lasts throughout the entire layout. Learn more about [layouts](layouts.md) for more information.


#### IServiceConfigurer

All classes that implement this interface will be invoked on startup to register global services. Any global service **can not** inject scoped services. Look through [WarfareModule.ConfigureServices()](https://github.com/UncreatedStaff/UncreatedWarfare/blob/master/UncreatedWarfare/WarfareModule.cs) for a list of all registered services.

Example
```cs
public class ServiceConfigurer : IServiceConfigurer
{
    void IServiceConfigurer.ConfigureServices(ContainerBuilder bldr)
    {
        // TODO: Configure Globally-Scoped Services
    }
}
```

`IServiceConfigurer` implementations can inject the following services:
* `IConfiguration`
* `ILogger<>`
* `WarfarePlugin`
    * This is the configuration for the plugin containing the configurer.
* `ContainerBuilder`
    * The globally-scoped container builder.
* `WarfarePluginLoader`
    * The plugin loader.
* `WarfareModule`
    * The module host.
* `WarfarePluginConfiguration`
    * IConfiguration for `Warfare/Plugins/<Plugin Assembly Name>/Config.yml`

#### ILayoutServiceConfigurer

Classes that implement this interface can be added to the **Services** list in a layout file using their assembly-qualified name.
```yml
Services:
  - "Uncreated.Warfare.AdvanceAndSecure.LayoutServiceConfigurer, Uncreated.Warfare.AdvanceAndSecure"
```

Example
```cs
public class LayoutServiceConfigurer : ILayoutServiceConfigurer
{
    public void ConfigureServices(ContainerBuilder bldr)
    {
        // TODO: Configure Layout-Scoped Services
    }
}
```

`ILayoutServiceConfigurer` implementations can inject any global services.