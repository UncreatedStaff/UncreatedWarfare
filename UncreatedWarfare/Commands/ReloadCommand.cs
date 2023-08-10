using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

public class ReloadCommand : AsyncCommand
{
    public const string Syntax = "/reload [help|module]";
    public const string Help = "Reload certain parts of UCWarfare.";
    public static event VoidDelegate OnTranslationsReloaded;
    public static event VoidDelegate OnFlagsReloaded;

    public static Dictionary<string, IConfiguration> ReloadableConfigs = new Dictionary<string, IConfiguration>();

    public ReloadCommand() : base("reload", EAdminType.ADMIN, 1)
    {
        Structure = new CommandStructure
        {
            Description = Help,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Translations")
                {
                    Aliases = new string[] { "lang" },
                },
                new CommandParameter("Flags"),
                new CommandParameter("Permissions"),
                new CommandParameter("Colors"),
                new CommandParameter("TCP"),
                new CommandParameter("SQL"),
                new CommandParameter("Teams")
                {
                    Aliases = new string[] { "factions" },
                },
                new CommandParameter("Module", typeof(string))
                {
                    Description = "Reload a module with a reload key."
                }
            }
        };
    }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        if (!ctx.IsConsole && !ctx.Caller.IsAdmin)
            ctx.AssertOnDuty();

        if (!ctx.TryGet(0, out string module))
            throw ctx.SendCorrectUsage(Syntax);

        if (module.Equals("help", StringComparison.OrdinalIgnoreCase))
            throw ctx.SendNotImplemented();

        if (module.Equals("translations", StringComparison.OrdinalIgnoreCase) || module.Equals("lang", StringComparison.OrdinalIgnoreCase))
        {
            await ReloadTranslations(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            ctx.Reply(T.ReloadedTranslations, Localization.TotalDefaultTranslations);
            ctx.LogAction(ActionLogType.ReloadComponent, "TRANSLATIONS");
        }
        else if (module.Equals("flags", StringComparison.OrdinalIgnoreCase))
        {
            ReloadFlags();
            ctx.Reply(T.ReloadedFlags);
            ctx.LogAction(ActionLogType.ReloadComponent, "FLAGS");
        }
        else if (module.Equals("permissions", StringComparison.OrdinalIgnoreCase))
        {
            ReloadPermissions();
            ctx.Reply(T.ReloadedPermissions);
            ctx.LogAction(ActionLogType.ReloadComponent, "PERMISSIONS");
        }
        else if (module.Equals("colors", StringComparison.OrdinalIgnoreCase))
        {
            ReloadColors();
            ctx.Reply(T.ReloadedGeneric, "colors");
            ctx.LogAction(ActionLogType.ReloadComponent, "COLORS");
        }
        else if (module.Equals("tcp", StringComparison.OrdinalIgnoreCase))
        {
            ReloadTCPServer();
            ctx.Reply(T.ReloadedTCP);
            ctx.LogAction(ActionLogType.ReloadComponent, "TCP SERVER");
        }
        else if (module.Equals("sql", StringComparison.OrdinalIgnoreCase))
        {
            await ReloadSQLServer(ctx, token).ConfigureAwait(false);
            ctx.LogAction(ActionLogType.ReloadComponent, "MYSQL CONNECTION");
        }
        else if (module.Equals("teams", StringComparison.OrdinalIgnoreCase) || module.Equals("factions", StringComparison.OrdinalIgnoreCase))
        {
            await TeamManager.ReloadFactions(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            TeamManager.SetupConfig();
            ctx.Reply(T.ReloadedGeneric, "teams and factions");
            ctx.LogAction(ActionLogType.ReloadComponent, "TEAMS & FACTIONS");
        }
        else
        {
            module = module.ToLowerInvariant();
            if (ReloadableConfigs.TryGetValue(module, out IConfiguration config))
            {
                config.Reload();
                ctx.Reply(T.ReloadedGeneric, module.ToProperCase());
                ctx.LogAction(ActionLogType.ReloadComponent, module.ToUpperInvariant());
            }
            else
            {
                IReloadableSingleton? reloadable = await Data.Singletons.ReloadSingletonAsync(module, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);

                if (reloadable is null)
                    throw ctx.SendCorrectUsage(Syntax);

                ctx.Reply(T.ReloadedGeneric, module.ToProperCase());
                ctx.LogAction(ActionLogType.ReloadComponent, module.ToUpperInvariant());
            }
        }
    }

    private void ReloadColors()
    {
        Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
        Translation.OnColorsReloaded();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            UCWarfare.I.UpdateLangs(PlayerManager.OnlinePlayers[i], false);
    }
    public static void ReloadPermissions()
    {
        PermissionSaver.Instance.Read();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            EAdminType old = pl.PermissionLevel;
            pl.ResetPermissionLevel();
            EAdminType @new = pl.PermissionLevel;
            if (old == @new) continue;
            if (@new is EAdminType.MEMBER or EAdminType.ADMIN_OFF_DUTY or EAdminType.TRIAL_ADMIN_OFF_DUTY)
            {
                switch (old)
                {
                    case EAdminType.ADMIN_ON_DUTY:
                        DutyCommand.AdminOnToOff(pl);
                        break;
                    case EAdminType.TRIAL_ADMIN_ON_DUTY:
                        DutyCommand.InternOnToOff(pl);
                        break;
                }
            }
            else if (@new is EAdminType.TRIAL_ADMIN_ON_DUTY)
            {
                DutyCommand.InternOffToOn(pl);
            }
            else if (@new is EAdminType.ADMIN_ON_DUTY)
            {
                DutyCommand.AdminOffToOn(pl);
            }
        }
    }
    internal static async Task ReloadTranslations(CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            await Data.ReloadLanguageDataStore(false, token).ConfigureAwait(false);

            Data.LanguageAliases = JSONMethods.LoadLangAliases();
            Data.Languages = JSONMethods.LoadLanguagePreferences();
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            Deaths.Localization.Reload();
            Localization.ReadEnumTranslations(Data.TranslatableEnumTypes);
            await Translation.ReadTranslations(token);
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.ToArray())
                player.Locale.Preferences = await Data.LanguageDataStore.GetLanguagePreferences(player.Steam64, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            OnTranslationsReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            L.LogError("Execption when reloading translations.");
            L.LogError(ex);
        }
    }
    internal static void ReloadGamemodeConfig()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Gamemode.ConfigObj.Reload();
    }
    internal static void ReloadFlags()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            Gamemode.ConfigObj.Reload();
            if (Data.Gamemode is FlagGamemode flaggm)
                flaggm.LoadAllFlags();
            else
            {
                ZoneList? zl = Data.Singletons.GetSingleton<ZoneList>();
                if (zl != null)
                {
                    UCWarfare.RunTask(async () =>
                    {
                        await zl.DownloadAll();
                        await UCWarfare.ToUpdate();
                        TeamManager.OnReloadFlags();
                    }, ctx: "Reload flags");
                }
            }
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            OnFlagsReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            L.LogError("Execption when reloading flags.");
            L.LogError(ex);
        }
    }
    internal static void ReloadKits()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        KitManager manager = SingletonEx.AssertAndGet<KitManager>();
        UCWarfare.RunTask(async () =>
        {
            await manager.DownloadAll();
            await UCWarfare.ToUpdate();
            Signs.UpdateKitSigns(null, null);
            Signs.UpdateLoadoutSigns(null);
        });
    }
    internal static void ReloadAllConfigFiles()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            _ = Data.Singletons.ReloadSingletonAsync("announcer");
            IEnumerable<FieldInfo> objects = typeof(Data).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.FieldType.IsClass);
            foreach (FieldInfo obj in objects)
            {
                try
                {
                    object o = obj.GetValue(null);
                    IEnumerable<FieldInfo> configfields = o.GetType()
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static).Where(x =>
                            x.FieldType.GetInterfaces().Contains(typeof(IConfiguration)));
                    foreach (FieldInfo config in configfields)
                    {
                        IConfiguration c;
                        if (config.IsStatic)
                        {
                            c = (IConfiguration)config.GetValue(null);
                        }
                        else
                        {
                            c = (IConfiguration)config.GetValue(o);
                        }

                        c.Reload();
                    }
                }
                catch (Exception ex)
                {
                    L.LogError("Error reloading config file.");
                    L.LogError(ex);
                }
            }
        }
        catch (Exception ex)
        {
            L.LogError("Failed to find all objects in type " + nameof(Data));
            L.LogError(ex);
        }
    }
    internal static void ReloadTCPServer()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCWarfare.I.InitNetClient();
    }
    internal static async Task ReloadSQLServer(CommandInteraction? ctx, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Reloading SQL...");
        List<UCSemaphore> players = PlayerManager.GetAllSemaphores();
        try
        {
            List<Task> tasks = new List<Task>(players.Count);
            for (int i = 0; i < players.Count; ++i)
                tasks.Add(players[i].WaitAsync(token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            // not async intentionally, we want it to do all this within a frame
            Data.DatabaseManager.CloseAsync(token).Wait(token);
            Data.DatabaseManager.Dispose();
            Data.DatabaseManager = new WarfareSQL(UCWarfare.Config.SQL);
            if (Data.DatabaseManager.OpenAsync(token).Result)
                L.Log("Local database reopened");
            else
                L.LogError("Local database failed to reopen.");
            if (Data.RemoteSQL != null)
            {
                await UCWarfare.SkipFrame();
                Data.RemoteSQL.CloseAsync(token).Wait(token);
                Data.RemoteSQL.Dispose();
                if (UCWarfare.Config.RemoteSQL != null)
                {
                    Data.RemoteSQL = new WarfareSQL(UCWarfare.Config.RemoteSQL);
                    if (Data.RemoteSQL.OpenAsync(token).Result)
                        L.Log("Remote database reopened");
                    else
                        L.LogError("Remote database failed to reopen.");
                }
            }
            ctx?.Reply(T.ReloadedSQL);
        }
        catch (Exception ex)
        {
            L.LogError("Failed to reload SQL.");
            L.LogError(ex);
            ctx?.Reply(T.UnknownError);
        }
        finally
        {
            for (int i = 0; i < players.Count; ++i)
                players[i].Release();
            L.Log("Reload operation complete.");
        }
    }
    internal static void DeregisterConfigForReload(string reloadKey)
    {
        ReloadableConfigs.Remove(reloadKey);
    }
    internal static bool RegisterConfigForReload<TData>(IConfiguration<TData> config) where TData : JSONConfigData, new()
    {
        if (config is null) return false;
        if (ReloadableConfigs.TryGetValue(config.ReloadKey!, out IConfiguration config2))
            return ReferenceEquals(config, config2);
        ReloadableConfigs.Add(config.ReloadKey!, config);
        return true;
    }
}