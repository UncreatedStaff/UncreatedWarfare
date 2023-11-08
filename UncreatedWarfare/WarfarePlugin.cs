using Cysharp.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;
using System;
using Uncreated.Warfare.Database;

// For more, visit https://openmod.github.io/openmod-docs/devdoc/guides/getting-started.html

[assembly: PluginMetadata("UncreatedWarfare", DisplayName = "Uncreated Warfare")]

namespace Uncreated.Warfare;

public class WarfarePlugin : OpenModUnturnedPlugin
{
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer _stringLocalizer;
    private readonly ILogger<WarfarePlugin> _logger;

    private readonly WarfareDbContext _dbContext;

    public WarfarePlugin(
        IConfiguration configuration,
        IStringLocalizer stringLocalizer,
        ILogger<WarfarePlugin> logger,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _configuration = configuration;
        _stringLocalizer = stringLocalizer;
        _logger = logger;

        _dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();
    }

    protected override async UniTask OnLoadAsync()
    {
        _logger.LogDebug("Migrating database...");
        await _dbContext.Database.MigrateAsync();
        _logger.LogDebug("Done.");

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