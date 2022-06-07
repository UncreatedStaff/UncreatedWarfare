using Rocket.API;
using Rocket.Core;
using Rocket.Core.Commands;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

public class ReloadCommand : IRocketCommand
{
    public static event VoidDelegate OnTranslationsReloaded;
    public static event VoidDelegate OnFlagsReloaded;

    public const string RELOAD_ALL_PERMISSION = "uc.reload.all";

    private readonly List<string> _permissions = new List<string>(1) { "uc.reload" };
    public static Dictionary<string, IConfiguration> ReloadableConfigs = new Dictionary<string, IConfiguration>();
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "reload";
    public string Help => "Reload certain parts of UCWarfare.";
    public string Syntax => "/reload [help|module]";
    public List<string> Aliases => _aliases;
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        CommandContext ctx = new CommandContext(caller, command);
        if (!ctx.TryGet(0, out string module))
        {
            ctx.Reply("reload_syntax");
            return;
        }
        if (module.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Reply("todo");
        }
        else if (module.Equals("translations", StringComparison.OrdinalIgnoreCase) || module.Equals("lang", StringComparison.OrdinalIgnoreCase))
        {
            if (!ctx.HasPermissionOrReplyOr(RELOAD_ALL_PERMISSION, "uc.reload.translations")) return;
            ReloadTranslations();
            ctx.Reply("reload_reloaded_translations");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TRANSLATIONS");
        }
        else if (module.Equals("flags", StringComparison.OrdinalIgnoreCase))
        {
            if (!ctx.HasPermissionOrReplyOr(RELOAD_ALL_PERMISSION, "uc.reload.flags")) return;
            if (Data.Is<IFlagRotation>())
            {
                ReloadFlags();
                ctx.Reply("reload_reloaded_flags");
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "FLAGS");
            }
            else ctx.Reply("reload_reloaded_flags_gm");
        }
        else if (module.Equals("rocket", StringComparison.OrdinalIgnoreCase) || module.Equals("ldm", StringComparison.OrdinalIgnoreCase))
        {
            if (!ctx.HasPermissionOrReplyOr(RELOAD_ALL_PERMISSION, "uc.reload.rocket")) return;
            ReloadRocket();
            ctx.Reply("reload_reloaded_rocket");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "ROCKET");
        }
        else if (module.Equals("colors", StringComparison.OrdinalIgnoreCase))
        {
            if (!ctx.HasPermissionOrReplyOr(RELOAD_ALL_PERMISSION, "uc.reload.colors")) return;
            ReloadColors();
            ctx.Reply("reload_reloaded_generic", "colors");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "COLORS");
        }
        else if (module.Equals("tcp", StringComparison.OrdinalIgnoreCase))
        {
            if (!ctx.HasPermissionOrReplyOr(RELOAD_ALL_PERMISSION, "uc.reload.tcp")) return;
            ReloadTCPServer();
            ctx.Reply("reload_reloaded_tcp");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "TCP SERVER");
        }
        else if (module.Equals("sql", StringComparison.OrdinalIgnoreCase))
        {
            if (!ctx.HasPermissionOrReplyOr(RELOAD_ALL_PERMISSION, "uc.reload.sql")) return;
            ReloadSQLServer();
            ctx.Reply("reload_reloaded_sql");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "MYSQL CONNECTION");
        }
        else if (module.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (!ctx.HasPermissionOrReply(RELOAD_ALL_PERMISSION)) return;
            ReloadTranslations();
            ReloadFlags();
            ReloadTCPServer();
            ReloadSQLServer();
            ReloadKits();
            foreach (KeyValuePair<string, IConfiguration> config in ReloadableConfigs)
                config.Value.Reload();

            ctx.Reply("reload_reloaded_all");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "ALL");
        }
        else
        {
            module = module.ToLowerInvariant();
            if (!ctx.HasPermissionOrReplyOr(RELOAD_ALL_PERMISSION, "uc.reload." + module)) return;
            if (ReloadableConfigs.TryGetValue(module, out IConfiguration config))
            {
                config.Reload();
                ctx.Reply("reload_reloaded_generic", module.ToProperCase());
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, module.ToUpperInvariant());
            }
            else
            {
                IReloadableSingleton? reloadable = Data.Singletons.ReloadSingleton(module);
                if (reloadable is null) goto notFound;
                ctx.Reply("reload_reloaded_generic", module.ToProperCase());
                ctx.LogAction(EActionLogType.RELOAD_COMPONENT, module.ToUpperInvariant());
            }
        }
        return;
    notFound:
        ctx.Reply("reload_syntax");
    }

    private void ReloadColors()
    {
        throw new NotImplementedException();
    }
    internal static void ReloadRocket()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCWarfare.Instance.Configuration.Load();
        R.Settings.Load();
        R.Translation.Load();
        R.Permissions.Reload();
        U.Settings.Load();
        U.Translation.Load();
        typeof(RocketCommandManager).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(R.Commands, Array.Empty<object>());
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
            Data.Localization = JSONMethods.LoadTranslations();
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            Deaths.Localization.Reload();
            Translation.ReadEnumTranslations(Data.TranslatableEnumTypes);
            if (OnTranslationsReloaded != null)
                OnTranslationsReloaded.Invoke();
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
            if (OnFlagsReloaded != null)
                OnFlagsReloaded.Invoke();
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
        Task.Run(async () =>
        {
            await manager.ReloadKits();
            await UCWarfare.ToUpdate();
            if (RequestSigns.Loaded)
            {
                RequestSigns.UpdateAllSigns();
            }
            if (!KitManager.KitExists(TeamManager.Team1UnarmedKit, out _))
                L.LogError("Team 1's unarmed kit, \"" + TeamManager.Team1UnarmedKit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            if (!KitManager.KitExists(TeamManager.Team2UnarmedKit, out _))
                L.LogError("Team 2's unarmed kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            if (!KitManager.KitExists(TeamManager.DefaultKit, out _))
                L.LogError("The default kit, \"" + TeamManager.DefaultKit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
        });
    }
    internal static void ReloadAllConfigFiles()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            UCWarfare.I.Announcer.Reload();
            IEnumerable<FieldInfo> objects = typeof(Data).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.FieldType.IsClass);
            foreach (FieldInfo obj in objects)
            {
                try
                {
                    object o = obj.GetValue(null);
                    IEnumerable<FieldInfo> configfields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).
                        Where(x => x.FieldType.GetInterfaces().Contains(typeof(IConfiguration)));
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
                catch (Exception) { }
            }
            // FIX: Invocations
            //Invocations.Warfare.SendRankInfo.NetInvoke(XPManager.config.Data.Ranks, OfficerManager.config.Data.OfficerRanks, OfficerManager.config.Data.FirstStarPoints, OfficerManager.config.Data.PointsIncreasePerStar);
        }
        catch (Exception ex)
        {
            L.LogError("Failed to find all objects in type " + typeof(Data).Name);
            L.LogError(ex);
        }
    }
    internal static void ReloadTCPServer()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.ReloadTCP();
    }
    internal static void ReloadSQLServer()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.DatabaseManager.Close();
        Data.DatabaseManager.Open();
    }
    internal static void DeregisterConfigForReload(string reloadKey)
    {
        ReloadableConfigs.Remove(reloadKey);
    }
    internal static bool RegisterConfigForRelaod<TData>(Config<TData> config) where TData : ConfigData, new()
    {
        if (config is null) return false;
        if (ReloadableConfigs.TryGetValue(config.ReloadKey!, out IConfiguration config2))
            return ReferenceEquals(config, config2);
        ReloadableConfigs.Add(config.ReloadKey!, config);
        return true;
    }
}