using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;

namespace Uncreated.Warfare;
public interface ILanguageDataStore
{
    Task Initialize(CancellationToken token = default);
    void WriteWait();
    void WriteRelease();
    Task<LanguageInfo?> GetInfo(string name, bool exactOnly, bool allowCache, CancellationToken token = default);
    Task<LanguageInfo?> GetInfo(PrimaryKey key, bool allowCache, CancellationToken token = default);
    Task<bool> WriteWaitAsync(CancellationToken token = default);
    Task AddOrUpdateInfo(LanguageInfo info, CancellationToken token = default);
    Task UpdateLanguagePreferences(PlayerLanguagePreferences preferences, CancellationToken token = default);
    Task<PlayerLanguagePreferences> GetLanguagePreferences(ulong steam64, CancellationToken token = default);
    Task GetLanguages(IList<LanguageInfo> outputList, CancellationToken token = default);
}
public interface ICachableLanguageDataStore : ILanguageDataStore
{
    IReadOnlyList<LanguageInfo> Languages { get; }
    LanguageInfo? GetInfoCached(string name, bool exactOnly = true);
    LanguageInfo? GetInfoCached(PrimaryKey key);
    Task ReloadCache(CancellationToken token = default);
}

public abstract class MySqlLanguageDataStore : ICachableLanguageDataStore
{
    private List<LanguageInfo>? _langs;
    private Dictionary<string, LanguageInfo>? _codes;
    private readonly UCSemaphore _semaphore = new UCSemaphore();
    public abstract IWarfareSql Sql { get; }
    public IReadOnlyList<LanguageInfo> Languages { get; private set; }
    public Task Initialize(CancellationToken token = default)
    {
        return VerifyTables(token);
    }
    public Task<bool> WriteWaitAsync(CancellationToken token = default) => _semaphore.WaitAsync(token);
    public void WriteWait() => _semaphore.Wait();
    public void WriteRelease() => _semaphore.Release();
    public LanguageInfo? GetInfoCached(string name, bool exactOnly = true)
    {
        lock (this)
        {
            if (_codes != null && _codes.TryGetValue(name, out LanguageInfo info))
                return info;
        }

        _semaphore.Wait();
        try
        {
            if (_langs == null)
                return null;

            LanguageInfo lang = _langs.Find(x => F.RoughlyEquals(x.LanguageCode, name));
            if (lang != null || exactOnly)
                return lang;

            lang = _langs.Find(x => F.RoughlyEquals(x.DisplayName, name));
            if (lang != null)
                return lang;

            string[] words = name.Split(F.SpaceSplit);
            lang = _langs.Find(x => words.All(l => x.Aliases.Any(x => F.RoughlyEquals(l, x))));
            if (lang != null)
                return lang;

            lang = _langs.Find(x => x.DisplayName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1);
            if (lang != null)
                return lang;

            lang = _langs.Find(x => words.All(l => x.DisplayName.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1));
            if (lang != null)
                return lang;

            lang = _langs.Find(x => x.NativeName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1);
            if (lang != null)
                return lang;

            lang = _langs.Find(x => words.All(l => x.NativeName.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1));
            if (lang != null)
                return lang;

            lang = _langs.Find(x => words.All(l => x.Aliases.Any(x => x.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1)));
            return lang;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public LanguageInfo? GetInfoCached(PrimaryKey key)
    {
        _semaphore.Wait();
        try
        {
            return _langs?.Find(x => x.PrimaryKey.Key == key.Key);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task<LanguageInfo?> GetInfo(string name, bool exactOnly, bool allowCache, CancellationToken token = default)
    {
        if (_langs == null || !allowCache)
            await ReloadCache(token).ConfigureAwait(false);

        return GetInfoCached(name, exactOnly);
    }
    public async Task<LanguageInfo?> GetInfo(PrimaryKey key, bool allowCache, CancellationToken token = default)
    {
        if (_langs == null || !allowCache)
            await ReloadCache(token).ConfigureAwait(false);

        return GetInfoCached(key);
    }
    public async Task AddOrUpdateInfo(LanguageInfo info, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            int index = _langs == null ? -1 : _langs.FindIndex(x => x.LanguageCode.Equals(info.LanguageCode, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                LanguageInfo lang = _langs![index];
                if (!ReferenceEquals(lang, info))
                {
                    lang.Aliases = info.Aliases;
                    lang.DefaultCultureCode = info.DefaultCultureCode;
                    lang.DisplayName = info.DisplayName;
                    lang.AvailableCultureCodes = info.AvailableCultureCodes;
                    lang.RequiresIMGUI = info.RequiresIMGUI;
                    lang.FallbackTranslationLanguageCode = info.FallbackTranslationLanguageCode;
                    lang.SteamLanguageName = info.SteamLanguageName;
                    lang.Credits = info.Credits;
                    lang.NativeName = info.NativeName;
                    _langs![index] = info;
                }
            }
            else
            {
                (_langs ??= new List<LanguageInfo>(1)).Add(info);
            }

            await Sql.QueryAsync(F.BuildInitialInsertQuery(TableLanguages, ColumnLanguageKey,
                    info.PrimaryKey.IsValid, ColumnLanguageKeyExternal,
                    new string[] { TableLanguagesAliases, TableLanguagesAvailableCultures, TableLanguageCredits },
                    ColumnLanguageDisplayName, ColumnLanguageCode, ColumnLanguageDefaultCulture, ColumnLanguageHasTranslationSupport,
                    ColumnLanguageRequiresIMGUI, ColumnLanguageFallbackTranslationLanguageCode, ColumnLanguageSteamLanguageName, ColumnLanguageNativeName),
                new object[]
                {
                    info.DisplayName,
                    info.LanguageCode,
                    (object?)info.DefaultCultureCode ?? DBNull.Value,
                    info.HasTranslationSupport,
                    info.RequiresIMGUI,
                    (object?)info.FallbackTranslationLanguageCode ?? DBNull.Value,
                    (object?)info.SteamLanguageName ?? DBNull.Value,
                    info.NativeName.Equals(info.DisplayName, StringComparison.Ordinal) ? DBNull.Value : info.NativeName,
                    
                    info.PrimaryKey.Key
                },
                reader =>
                {
                    info.PrimaryKey = reader.GetInt32(0);
                }, token).ConfigureAwait(false);
            
            if (info.Aliases.Length <= 0 && info.AvailableCultureCodes.Length <= 0 && info.Credits.Length <= 0)
                return;
            StringBuilder sb = new StringBuilder(256);
            object[] objs = new object[info.Aliases.Length + info.AvailableCultureCodes.Length + info.Credits.Length + 1];
            objs[0] = info.PrimaryKey.Key;

            if (info.Aliases.Length > 0)
            {
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TableLanguagesAliases, ColumnLanguageKeyExternal, ColumnAliasesValue));
                for (int i = 0; i < info.Aliases.Length; ++i)
                {
                    int j = i + 1;
                    objs[j] = info.Aliases[i];
                    F.AppendPropertyList(sb, j, 1, i, 1);
                }

                sb.Append(';');
            }

            if (info.AvailableCultureCodes.Length > 0)
            {
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TableLanguagesAvailableCultures, ColumnLanguageKeyExternal, ColumnAvailableCulturesValue));
                for (int i = 0; i < info.AvailableCultureCodes.Length; ++i)
                {
                    int j = i + info.Aliases.Length + 1;
                    objs[j] = info.AvailableCultureCodes[i];
                    F.AppendPropertyList(sb, j, 1, i, 1);
                }

                sb.Append(';');
            }

            if (info.Credits.Length > 0)
            {
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TableLanguageCredits, ColumnLanguageKeyExternal, ColumnCreditsValue));
                for (int i = 0; i < info.Credits.Length; ++i)
                {
                    int j = i + info.Aliases.Length + info.AvailableCultureCodes.Length + 1;
                    objs[j] = info.Credits[i];
                    F.AppendPropertyList(sb, j, 1, i, 1);
                }

                sb.Append(';');
            }

            if (sb.Length == 0)
                return;

            await Sql.NonQueryAsync(sb.ToString(), objs, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task UpdateLanguagePreferences(PlayerLanguagePreferences preferences, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await Sql.NonQueryAsync(F.BuildOtherInsertQueryUpdate(TableLanguagePreferences, ColumnPreferencesSteam64, ColumnPreferencesLanguage, ColumnPreferencesCulture, ColumnPreferencesLastUpdated),
                new object[]
                {
                    preferences.Steam64,
                    preferences.Language.IsValid ? preferences.Language.Key : DBNull.Value,
                    (object?)preferences.CultureCode ?? DBNull.Value,
                    preferences.LastUpdated.UtcDateTime
                }, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task<PlayerLanguagePreferences> GetLanguagePreferences(ulong steam64, CancellationToken token = default)
    {
        if (_langs == null)
            await ReloadCache(token).ConfigureAwait(false);

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            PlayerLanguagePreferences? preferences = null;
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnPreferencesLanguage, ColumnPreferencesCulture, ColumnPreferencesLastUpdated)} FROM `{TableLanguagePreferences}` WHERE `{ColumnPreferencesSteam64}`=@0;", new object[] { steam64 },
                reader =>
                {
                    preferences = new PlayerLanguagePreferences(
                        steam64,
                        reader.IsDBNull(0) ? PrimaryKey.NotAssigned : reader.GetInt32(0),
                        reader.IsDBNull(1) ? null : reader.GetString(1),
                        new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)));
                }, token).ConfigureAwait(false);

            return preferences ?? new PlayerLanguagePreferences(steam64);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public Task VerifyTables(CancellationToken token = default) => Sql.VerifyTables(Schemas, token);
    public async Task ReloadCache(CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<LanguageInfo> newList = _langs ?? new List<LanguageInfo>();
            await GetLanguages(newList, token).ConfigureAwait(false);
            _langs = newList;
            Languages = newList.AsReadOnly();
            lock (this)
            {
                if (_codes != null)
                    _codes.Clear();
                else _codes = new Dictionary<string, LanguageInfo>(newList.Count, StringComparer.OrdinalIgnoreCase);
                for (int i = _langs.Count - 1; i >= 0; --i)
                    _codes[_langs[i].LanguageCode] = _langs[i];
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task GetLanguages(IList<LanguageInfo> outputList, CancellationToken token = default)
    {
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnLanguageKey, ColumnLanguageDisplayName, ColumnLanguageCode,
            ColumnLanguageDefaultCulture, ColumnLanguageHasTranslationSupport, ColumnLanguageRequiresIMGUI,
            ColumnLanguageFallbackTranslationLanguageCode, ColumnLanguageSteamLanguageName, ColumnLanguageNativeName)} FROM `{TableLanguages}` ORDER BY `{ColumnLanguageKey}`;", null,
            reader =>
            {
                string code = reader.GetString(2);
                LanguageInfo? existing = outputList.FirstOrDefault(x => x.LanguageCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    existing = new LanguageInfo(reader.GetInt32(0), code);
                    outputList.Add(existing);
                }
                
                existing.DisplayName = reader.GetString(1);
                existing.DefaultCultureCode = reader.IsDBNull(3) ? null : reader.GetString(3);
                existing.HasTranslationSupport = reader.GetBoolean(4);
                existing.RequiresIMGUI = reader.GetBoolean(5);
                existing.FallbackTranslationLanguageCode = reader.IsDBNull(6) ? null : reader.GetString(6);
                existing.SteamLanguageName = reader.IsDBNull(7) ? null : reader.GetString(7);
                existing.NativeName = reader.IsDBNull(8) ? existing.DisplayName : reader.GetString(8);
            }, token).ConfigureAwait(false);

        List<PrimaryKeyPair<string>> tempList = new List<PrimaryKeyPair<string>>(32);

        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnLanguageKeyExternal, ColumnAliasesValue)} FROM `{TableLanguagesAliases}` ORDER BY `{ColumnLanguageKeyExternal}`;", null,
            reader =>
            {
                tempList.Add(new PrimaryKeyPair<string>(reader.GetInt32(0), reader.GetString(1)));
            }, token).ConfigureAwait(false);

        F.ApplyQueriedList(tempList, (key, arr) =>
        {
            LanguageInfo? info = outputList.FirstOrDefault(x => x.PrimaryKey.Key == key);
            if (info != null)
                info.Aliases = arr;
        }, false);

        tempList.Clear();

        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnLanguageKeyExternal, ColumnAvailableCulturesValue)} FROM `{TableLanguagesAvailableCultures}` ORDER BY `{ColumnLanguageKeyExternal}`;", null,
            reader =>
            {
                tempList.Add(new PrimaryKeyPair<string>(reader.GetInt32(0), reader.GetString(1)));
            }, token).ConfigureAwait(false);

        F.ApplyQueriedList(tempList, (key, arr) =>
        {
            LanguageInfo? info = outputList.FirstOrDefault(x => x.PrimaryKey.Key == key);
            if (info != null)
                info.AvailableCultureCodes = arr;
        }, false);

        List<PrimaryKeyPair<ulong>> tempList2 = new List<PrimaryKeyPair<ulong>>(32);

        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnLanguageKeyExternal, ColumnCreditsValue)} FROM `{TableLanguageCredits}` ORDER BY `{ColumnLanguageKeyExternal}`;", null,
            reader =>
            {
                tempList2.Add(new PrimaryKeyPair<ulong>(reader.GetInt32(0), reader.GetUInt64(1)));
            }, token).ConfigureAwait(false);

        F.ApplyQueriedList(tempList2, (key, arr) =>
        {
            LanguageInfo? info = outputList.FirstOrDefault(x => x.PrimaryKey.Key == key);
            if (info != null)
                info.Credits = arr;
        }, false);
    }

    public const string TableLanguages = "lang_info";
    public const string TableLanguagesAliases = "lang_aliases";
    public const string TableLanguagesAvailableCultures = "lang_cultures";
    public const string TableLanguageCredits = "lang_credits";
    public const string TableLanguagePreferences = "lang_preferences";

    public const string ColumnLanguageKeyExternal = "Language";

    public const string ColumnLanguageKey = "pk";
    public const string ColumnLanguageDisplayName = "DisplayName";
    public const string ColumnLanguageNativeName = "NativeName";
    public const string ColumnLanguageCode = "Code";
    public const string ColumnLanguageDefaultCulture = "DefaultCultureCode";
    public const string ColumnLanguageHasTranslationSupport = "HasTranslationSupport";
    public const string ColumnLanguageRequiresIMGUI = "RequiresIMGUI";
    public const string ColumnLanguageFallbackTranslationLanguageCode = "FallbackTranslationLanguageCode";
    public const string ColumnLanguageSteamLanguageName = "SteamLanguageName";

    public const string ColumnAliasesValue = "Alias";
    public const string ColumnAvailableCulturesValue = "CultureCode";
    public const string ColumnCreditsValue = "Contributor";

    public const string ColumnPreferencesSteam64 = "Steam64";
    public const string ColumnPreferencesLanguage = "Language";
    public const string ColumnPreferencesCulture = "Culture";
    public const string ColumnPreferencesLastUpdated = "LastUpdated";

    public static readonly Schema[] Schemas =
    {
        new Schema(TableLanguages, new Schema.Column[]
        {
            new Schema.Column(ColumnLanguageKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnLanguageDisplayName, SqlTypes.String(64)),
            new Schema.Column(ColumnLanguageNativeName, SqlTypes.String(64))
            {
                Nullable = true
            },
            new Schema.Column(ColumnLanguageCode, "char(5)"),
            new Schema.Column(ColumnLanguageDefaultCulture, SqlTypes.String(16))
            {
                Nullable = true
            },
            new Schema.Column(ColumnLanguageHasTranslationSupport, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnLanguageRequiresIMGUI, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnLanguageFallbackTranslationLanguageCode, "char(5)")
            {
                Nullable = true
            },
            new Schema.Column(ColumnLanguageSteamLanguageName, SqlTypes.String(32))
            {
                Nullable = true
            }
        }, true, typeof(LanguageInfo)),
        F.GetListSchema<string>(TableLanguagesAliases, ColumnLanguageKeyExternal, ColumnAliasesValue, TableLanguages, ColumnLanguageKey, length: 64),
        F.GetListSchema<string>(TableLanguagesAvailableCultures, ColumnLanguageKeyExternal, ColumnAvailableCulturesValue, TableLanguages, ColumnLanguageKey, length: 16),
        F.GetListSchema<ulong>(TableLanguageCredits, ColumnLanguageKeyExternal, ColumnCreditsValue, TableLanguages, ColumnLanguageKey),
        new Schema(TableLanguagePreferences, new Schema.Column[]
        {
            new Schema.Column(ColumnPreferencesSteam64, SqlTypes.STEAM_64)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnPreferencesLanguage, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TableLanguages,
                ForeignKeyColumn = ColumnLanguageKey,
                Nullable = true,
                ForeignKeyUpdateBehavior = ConstraintBehavior.Cascade,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull
            },
            new Schema.Column(ColumnPreferencesCulture, SqlTypes.String(16))
            {
                Nullable = true
            },
            new Schema.Column(ColumnPreferencesLastUpdated, SqlTypes.DATETIME)
        }, true, typeof(PlayerLanguagePreferences))
    };
}

public sealed class WarfareMySqlLanguageDataStore : MySqlLanguageDataStore
{
    public override IWarfareSql Sql => Data.AdminSql;
    public void Subscribe()
    {
        EventDispatcher.PlayerPendingAsync += OnPlayerPending;
    }
    public void Unsubscribe()
    {
        EventDispatcher.PlayerPendingAsync -= OnPlayerPending;
    }
    private async Task OnPlayerPending(PlayerPending e, CancellationToken token)
    {
        e.AsyncData.LanguagePreferences = await GetLanguagePreferences(e.Steam64, token).ConfigureAwait(false);
    }
}