using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;
using System.Text.Json;
using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Database;
public class EFMigrationDesignTimeFactory : IDesignTimeDbContextFactory<WarfareDbContext>
{
    public WarfareDbContext CreateDbContext(string[] args)
    {
        const string configFile = @"C:\SteamCMD\steamapps\common\U3DS\Uncreated\Warfare\sys_config.json";
        if (!File.Exists(configFile))
            throw new ArgumentException($"There should be a config file at \"{configFile}\" with SQL data. If you need this to work on your Mac add a check in EFMigrationDesignTimeFactory.");

        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        SystemConfigData sysConfig = JsonSerializer.Deserialize<SystemConfigData>(File.ReadAllText(configFile)) ?? throw new Exception("Failed to read config, value is null.");
        
        WarfareDbContext.ConnStringOverride = sysConfig.SqlConnectionString ?? (sysConfig.RemoteSQL ?? sysConfig.SQL).GetConnectionString("UCWarfare", true, true);

        return new WarfareDbContext();
    }
}
