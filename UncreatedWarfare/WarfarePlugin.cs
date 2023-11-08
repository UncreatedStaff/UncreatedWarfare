using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;
using System;
using OpenMod.Core.Plugins;

// For more, visit https://openmod.github.io/openmod-docs/devdoc/guides/getting-started.html

[assembly: PluginMetadata("UncreatedWarfare", DisplayName = "Uncreated Warfare")]

namespace Uncreated.Warfare;

public class WarfarePlugin : OpenModUnturnedPlugin
{
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer _stringLocalizer;
    private readonly ILogger<WarfarePlugin> _logger;

    public WarfarePlugin(
        IConfiguration configuration,
        IStringLocalizer stringLocalizer,
        ILogger<WarfarePlugin> logger,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _configuration = configuration;
        _stringLocalizer = stringLocalizer;
        _logger = logger;
    }

    protected override async UniTask OnLoadAsync()
    {
        // await UniTask.SwitchToMainThread(); uncomment if you have to access Unturned or UnityEngine APIs
        _logger.LogInformation("Hello World!");

        // await UniTask.SwitchToThreadPool(); // you can switch back to a different thread
    }

    protected override async UniTask OnUnloadAsync()
    {
        // await UniTask.SwitchToMainThread(); uncomment if you have to access Unturned or UnityEngine APIs
        _logger.LogInformation(_stringLocalizer["plugin_events:plugin_stop"]);
    }
}