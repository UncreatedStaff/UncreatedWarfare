using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

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
            ReloadTranslations();
            ctx.Reply(T.ReloadedTranslations);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TRANSLATIONS");
        }
        else if (module.Equals("flags", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is<IFlagRotation>())
            {
                ReloadFlags();
                ctx.Reply(T.ReloadedFlags);
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "FLAGS");
            }
            else ctx.Reply(T.ReloadFlagsInvalidGamemode);
        }
        else if (module.Equals("permissions", StringComparison.OrdinalIgnoreCase))
        {
            ReloadPermissions();
            ctx.Reply(T.ReloadedPermissions);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "PERMISSIONS");
        }
        else if (module.Equals("colors", StringComparison.OrdinalIgnoreCase))
        {
            ReloadColors();
            ctx.Reply(T.ReloadedGeneric, "colors");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "COLORS");
        }
        else if (module.Equals("tcp", StringComparison.OrdinalIgnoreCase))
        {
            ReloadTCPServer();
            ctx.Reply(T.ReloadedTCP);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TCP SERVER");
        }
        else if (module.Equals("sql", StringComparison.OrdinalIgnoreCase))
        {
            ReloadSQLServer(ctx);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "MYSQL CONNECTION");
        }
        else if (module.Equals("teams", StringComparison.OrdinalIgnoreCase) || module.Equals("factions", StringComparison.OrdinalIgnoreCase))
        {
            await TeamManager.ReloadFactions(token).ConfigureAwait(false);
            TeamManager.SetupConfig();
            ctx.Reply(T.ReloadedGeneric, "teams and factions");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TEAMS & FACTIONS");
        }
        else if (module.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            ReloadTranslations();
            ReloadFlags();
            ReloadTCPServer();
            ReloadSQLServer(null);
            ReloadKits();
            foreach (KeyValuePair<string, IConfiguration> config in ReloadableConfigs)
                config.Value.Reload();

            ctx.Reply(T.ReloadedAll);
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "ALL");
        }
        else
        {
            module = module.ToLowerInvariant();
            if (ReloadableConfigs.TryGetValue(module, out IConfiguration config))
            {
                config.Reload();
                ctx.Reply(T.ReloadedGeneric, module.ToProperCase());
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, module.ToUpperInvariant());
            }
            else
            {
                ctx.Defer();
                Task.Run(async () =>
                {
                    IReloadableSingleton? reloadable = await Data.Singletons.ReloadSingletonAsync(module);
                    await UCWarfare.ToUpdate();
                    if (reloadable is null)
                        ctx.SendCorrectUsage(Syntax);
                    else
                    {
                        ctx.Reply(T.ReloadedGeneric, module.ToProperCase());
                        ctx.LogAction(EActionLogType.RELOAD_COMPONENT, module.ToUpperInvariant());
                    }
                });
            }
        }
    }

    private void ReloadColors()
    {
        Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
        Translation.OnColorsReloaded();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            UCWarfare.I.UpdateLangs(PlayerManager.OnlinePlayers[i]);
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
    internal static void ReloadTranslations()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            Data.LanguageAliases = JSONMethods.LoadLangAliases();
            Data.Languages = JSONMethods.LoadLanguagePreferences();
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            Translation.ReadTranslations();
            Deaths.Localization.Reload();
            Localization.ReadEnumTranslations(Data.TranslatableEnumTypes);
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
                Data.ZoneProvider.Reload();
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            TeamManager.OnReloadFlags();
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
            // todo FIX: Invocations
            //Invocations.Warfare.SendRankInfo.NetInvoke(XPManager.config.Data.Ranks, OfficerManager.config.Data.OfficerRanks, OfficerManager.config.Data.FirstStarPoints, OfficerManager.config.Data.PointsIncreasePerStar);
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
    internal static void ReloadSQLServer(CommandInteraction? ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Task.Run(async () =>
        {
            L.Log("Reloading SQL...");
            List<UCPlayer> players = PlayerManager.OnlinePlayers.ToList();
            try
            {
                List<Task> tasks = new List<Task>(players.Count);
                for (int i = 0; i < players.Count; ++i)
                    tasks.Add(players[i].PurchaseSync.WaitAsync());
                await Task.WhenAll(tasks).ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                // not async intentionally, we want it to do all this within a frame
                Data.DatabaseManager.Close();
                Data.DatabaseManager.Dispose();
                Data.DatabaseManager = new WarfareSQL(UCWarfare.Config.SQL);
                if (Data.DatabaseManager.Open())
                    L.Log("Local database reopened");
                else
                    L.LogError("Local database failed to reopen.");
                if (Data.RemoteSQL != null)
                {
                    await UCWarfare.SkipFrame();
                    Data.RemoteSQL.Close();
                    Data.RemoteSQL.Dispose();
                    if (UCWarfare.Config.RemoteSQL != null)
                    {
                        Data.RemoteSQL = new WarfareSQL(UCWarfare.Config.RemoteSQL);
                        if (Data.RemoteSQL.Open())
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
                    players[i].PurchaseSync.Release();
                L.Log("Reload operation complete.");
            }
        });
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