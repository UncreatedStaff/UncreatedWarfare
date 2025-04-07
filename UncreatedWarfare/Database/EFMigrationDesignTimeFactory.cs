#if DEBUG && NETCOREAPP
using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Security;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Database;

/// <summary>
/// This class is created by visual studio when creating migrations.
/// </summary>
public class EFMigrationDesignTimeFactory : IDesignTimeDbContextFactory<WarfareDbContext>
{
    public WarfareDbContext CreateDbContext(string[] args)
    {
        string[] possiblePaths =
        [
            @"C:\SteamCMD\steamapps\common\U3DS\Servers\UncreatedSeason4\Warfare\System Config.yml"
        ];

        string? configFile = possiblePaths.FirstOrDefault(File.Exists);
        if (configFile == null)
        {
            throw new ArgumentException($"There should be a config file at one of the provided paths with SQL data. " +
                                        $"Add the path to \"possiblePaths\" in \"EFMigrationDesignTimeFactory.cs\".");
        }

        WarfareDbContext.IsInDesignTimeFactory = true;

        try
        {
            ThreadUtil.setupGameThread();
        }
        catch (SecurityException) { }

        GameThread.Setup();

        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        ConfigurationBuilder builder = new ConfigurationBuilder();

        IFileProvider fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(configFile));

        ConfigurationHelper.AddJsonOrYamlFile(builder, fileProvider, configFile);

        IConfigurationRoot sysConfig = builder.Build();

        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient(sp => new WarfareDbContext(sp.GetRequiredService<ILogger<WarfareDbContext>>(), WarfareDbContext.GetOptions(sp)));
        serviceCollection.AddSingleton<IConfiguration>(sysConfig);
        serviceCollection.AddLogging(builder => builder.AddProvider(new WarfareLoggerProvider(null)));

        IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = false
        });

        WarfareDbContext dbContext = serviceProvider.GetRequiredService<WarfareDbContext>();

        if (sysConfig is IDisposable disp)
            disp.Dispose();
        
        return dbContext;
    }
}
#endif