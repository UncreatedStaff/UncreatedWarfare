using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;

namespace Uncreated.Warfare;
public abstract class LanguageDataStore
{
    private List<LanguageInfo>? _langs;
    private readonly UCSemaphore _semaphore = new UCSemaphore();
    public abstract IWarfareSql Sql { get; }
    public IReadOnlyList<LanguageInfo> Languages { get; private set; }

    public Task<bool> WriteWaitAsync(CancellationToken token = default) => _semaphore.WaitAsync(token);
    public void WriteWait() => _semaphore.Wait();
    public void WriteRelease() => _semaphore.Release();
    
    public LanguageInfo? GetInfoCached(string name)
    {
        _semaphore.Wait();
        try
        {
            return _langs?.Find(x => x.LanguageCode.Equals(name, StringComparison.OrdinalIgnoreCase));
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
    public async Task AddInfo(LanguageInfo info, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            int index = _langs == null ? -1 : _langs.FindIndex(x => x.LanguageCode.Equals(info.LanguageCode, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                LanguageInfo lang = _langs![index];
                lang.Aliases = info.Aliases;
                lang.DefaultCultureCode = info.DefaultCultureCode;
                lang.LanguageCode = info.LanguageCode;
                lang.DisplayName = info.DisplayName;
                lang.AvailableCultureCodes = info.AvailableCultureCodes;
                info.PrimaryKey = lang.PrimaryKey;
                _langs![index] = info;
            }
            else
            {
                (_langs ??= new List<LanguageInfo>(1)).Add(info);
            }

            await Sql.QueryAsync(F.BuildInitialInsertQuery(TableLanguages, ColumnLanguageKey,
                    info.PrimaryKey.IsValid, ColumnLanguageKeyExternal,
                    new string[] { TableLanguagesAliases, TableLanguagesAvailableCultures },
                    ColumnLanguageDisplayName, ColumnLanguageCode, ColumnLanguageDefaultCulture),
                new object[] { info.DisplayName, info.LanguageCode, (object?)info.DefaultCultureCode ?? DBNull.Value },
                reader =>
                {
                    info.PrimaryKey = reader.GetInt32(0);
                }, token).ConfigureAwait(false);
            if (info.Aliases.Length <= 0 && info.AvailableCultureCodes.Length <= 0)
                return;

            StringBuilder sb = new StringBuilder(256);
            object[] objs = new object[info.Aliases.Length + info.AvailableCultureCodes.Length + 1];
            objs[0] = info.PrimaryKey.Key;

            if (info.Aliases.Length > 0)
            {
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TableLanguagesAliases, ColumnLanguageKeyExternal, ColumnAliasesValue));
                for (int i = 0; i < info.Aliases.Length; ++i)
                {
                    objs[i + 1] = info.Aliases[i];
                    F.AppendPropertyList(sb, 1, 1, i);
                }

                sb.Append(';');
            }

            if (info.AvailableCultureCodes.Length > 0)
            {
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TableLanguagesAvailableCultures, ColumnLanguageKeyExternal, ColumnAvailableCulturesValue));
                for (int i = 0; i < info.AvailableCultureCodes.Length; ++i)
                {
                    objs[i + info.Aliases.Length + 1] = info.AvailableCultureCodes[i];
                    F.AppendPropertyList(sb, info.Aliases.Length + 1, 1, i, 1);
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
                    DateTime.UtcNow
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
                        reader.IsDBNull(1) ? PrimaryKey.NotAssigned : reader.GetInt32(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2),
                        new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc)));
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

            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnLanguageKey, ColumnLanguageDisplayName, ColumnLanguageCode, ColumnLanguageDefaultCulture)} FROM `{TableLanguages}` ORDER BY `{ColumnLanguageKey}`;", null,
                reader =>
                {
                    string code = reader.GetString(2);
                    LanguageInfo? existing = newList.Find(x => x.LanguageCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        existing = new LanguageInfo();
                        newList.Add(existing);
                    }

                    existing.PrimaryKey = reader.GetInt32(0);
                    existing.LanguageCode = code;
                    existing.DisplayName = reader.GetString(1);
                    existing.DefaultCultureCode = reader.IsDBNull(3) ? null : reader.GetString(3);
                }, token).ConfigureAwait(false);

            List<KeyValuePair<int, string>> tempList = new List<KeyValuePair<int, string>>(32);

            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnLanguageKeyExternal, ColumnAliasesValue)} FROM `{TableLanguagesAliases}`;", null,
                reader =>
                {
                    tempList.Add(new KeyValuePair<int, string>(reader.GetInt32(0), reader.GetString(1)));
                }, token).ConfigureAwait(false);

            tempList.Sort((a, b) => a.Key.CompareTo(b.Key));
            int last = -1;
            string[] arr;
            for (int i = 0; i < tempList.Count; i++)
            {
                KeyValuePair<int, string> val = tempList[i];
                if (i > 0 && tempList[i - 1].Key != val.Key)
                {
                    last = i - 1;
                    arr = new string[i - last];
                    for (int j = 0; j < arr.Length; ++j)
                        arr[j] = tempList[last + j + 1].Value;
                }
            }
            arr = new string[tempList.Count - 1 - last];
            for (int j = 0; j < arr.Length; ++j)
                arr[j] = tempList[last + j + 1].Value;

            tempList.Clear();

            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(ColumnLanguageKeyExternal, ColumnAvailableCulturesValue)} FROM `{TableLanguagesAvailableCultures}`;", null,
                reader =>
                {
                    tempList.Add(new KeyValuePair<int, string>(reader.GetInt32(0), reader.GetString(1)));
                }, token).ConfigureAwait(false);

            tempList.Sort((a, b) => a.Key.CompareTo(b.Key));
            last = -1;
            for (int i = 0; i < tempList.Count; i++)
            {
                KeyValuePair<int, string> val = tempList[i];
                if (i > 0 && tempList[i - 1].Key != val.Key)
                {
                    last = i - 1;
                    arr = new string[i - last];
                    for (int j = 0; j < arr.Length; ++j)
                        arr[j] = tempList[last + j + 1].Value;
                }
            }
            arr = new string[tempList.Count - 1 - last];
            for (int j = 0; j < arr.Length; ++j)
                arr[j] = tempList[last + j + 1].Value;

            _langs = newList;
            Languages = newList.AsReadOnly();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public const string TableLanguages = "lang_info";
    public const string TableLanguagesAliases = "lang_aliases";
    public const string TableLanguagesAvailableCultures = "lang_cultures";
    public const string TableLanguagePreferences = "lang_preferences";

    public const string ColumnLanguageKeyExternal = "Language";

    public const string ColumnLanguageKey = "pk";
    public const string ColumnLanguageDisplayName = "DisplayName";
    public const string ColumnLanguageCode = "Code";
    public const string ColumnLanguageDefaultCulture = "DefaultCultureCode";

    public const string ColumnAliasesValue = "Alias";
    public const string ColumnAvailableCulturesValue = "CultureCode";

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
            new Schema.Column(ColumnLanguageCode, "char(5)"),
            new Schema.Column(ColumnLanguageDefaultCulture, SqlTypes.String(16))
            {
                Nullable = true
            }
        }, true, typeof(LanguageInfo)),
        F.GetListSchema<string>(TableLanguagesAliases, ColumnLanguageKeyExternal, ColumnAliasesValue, TableLanguages, ColumnLanguageKey, length: 64),
        F.GetListSchema<string>(TableLanguagesAvailableCultures, ColumnLanguageKeyExternal, ColumnAvailableCulturesValue, TableLanguages, ColumnLanguageKey, length: 16),
        new Schema(TableLanguagePreferences, new Schema.Column[]
        {
            new Schema.Column(ColumnPreferencesSteam64, SqlTypes.STEAM_64)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnPreferencesLanguage, SqlTypes.INCREMENT_KEY)
            {
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

public sealed class WarfareLanguageDataStore : LanguageDataStore
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