using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Uncreated.Warfare.Services;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Steam;

/// <summary>
/// Maps territory/region codes to time zones.
/// </summary>
public class TimeZoneRegionalDatabase : IHostedService
{
    public const string SourceUrl = "https://raw.githubusercontent.com/unicode-org/cldr/refs/heads/main/common/supplemental/windowsZones.xml";

    private readonly ILogger<TimeZoneRegionalDatabase> _logger;
    private readonly string? _fallbackSaveFile;

    private readonly Dictionary<string, TimeZoneInfo> _timeZones;
    private readonly Dictionary<string, IReadOnlyList<TimeZoneInfo>> _regionTimeZones;

    /// <summary>
    /// Dictionary of two-character territory codes to default time zones.
    /// </summary>
    /// <remarks>Note that territories can have more than one time zone.</remarks>
    public IReadOnlyDictionary<string, TimeZoneInfo> DefaultTimeZones { get; }

    /// <summary>
    /// Dictionary of two-character territory codes to all time zones the region overlaps with.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<TimeZoneInfo>> RegionTimeZones { get; }

    private TimeZoneRegionalDatabase(ILogger<TimeZoneRegionalDatabase> logger)
    {
        _logger = logger;
        _timeZones = new Dictionary<string, TimeZoneInfo>(256, StringComparer.OrdinalIgnoreCase);
        _regionTimeZones = new Dictionary<string, IReadOnlyList<TimeZoneInfo>>(256, StringComparer.OrdinalIgnoreCase);
        RegionTimeZones = new ReadOnlyDictionary<string, IReadOnlyList<TimeZoneInfo>>(_regionTimeZones);
        DefaultTimeZones = new ReadOnlyDictionary<string, TimeZoneInfo>(_timeZones);
    }

    /// <summary>
    /// Load the time zone database from a the URL to the windowsZones.xml file. Only works in Unity.
    /// </summary>
    public TimeZoneRegionalDatabase(ILogger<TimeZoneRegionalDatabase> logger, WarfareModule module)
        : this(logger)
    {
        _fallbackSaveFile = Path.Combine(module.HomeDirectory, "Cache", "windowsZones.xml");
    }

    /// <summary>
    /// Load the time zone database from a byte array containing the windowsZones.xml file.
    /// </summary>
    public TimeZoneRegionalDatabase(ILogger<TimeZoneRegionalDatabase> logger, string? saveFile, byte[] xmlDoc)
        : this(logger)
    {
        _fallbackSaveFile = saveFile;
        try
        {
            ParseData(xmlDoc);
            if (saveFile != null)
            {
                if (Path.GetDirectoryName(saveFile) is { } dirName)
                    Directory.CreateDirectory(dirName);

                File.WriteAllBytes(saveFile, xmlDoc);
            }

            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing xmlDoc Time Zone data.");
        }

        if (saveFile != null && File.Exists(saveFile))
        {
            try
            {
                ParseData(File.ReadAllBytes(saveFile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing saved Time Zone data.");
            }
        }
        else
        {
            logger.LogError("Cached timezone data doesn't exist.");
        }
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        string? filePath = _fallbackSaveFile;
        if (filePath != null && Path.GetDirectoryName(filePath) is { } dirName)
            Directory.CreateDirectory(dirName);

        try
        {
            using UnityWebRequest req = new UnityWebRequest(SourceUrl, "GET", new DownloadHandlerBuffer(), null);

            await req.SendWebRequest();

            byte[] data = req.downloadHandler.data;

            if (data.Length > 0)
            {
                try
                {
                    ParseData(data);
                    if (filePath != null)
                        File.WriteAllBytes(filePath, data);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing downloaded Time Zone data.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying timezone data.");
        }

        if (filePath != null && File.Exists(filePath))
        {
            try
            {
                ParseData(File.ReadAllBytes(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing saved Time Zone data.");
            }
        }
        else
        {
            _logger.LogError("Cached timezone data doesn't exist.");
        }
    }

    private void ParseData(byte[] data)
    {
        _timeZones.Clear();
        _regionTimeZones.Clear();

        int startIndex = 0;
        if (data is [0xEF, 0xBB, 0xBF, ..])
        {
            startIndex = 3;
        }

        Stream memStream = new MemoryStream(data, startIndex, data.Length - startIndex, false);

        XmlDocument reader = new XmlDocument();
        reader.Load(memStream);

        XmlElement? supplementalData = reader["supplementalData"];
        if (supplementalData == null)
            throw new FormatException("Missing 'supplementalData' tag.");

        XmlElement? windowsZones = supplementalData["windowsZones"];
        if (windowsZones == null)
            throw new FormatException("Missing 'windowsZones' tag.");

        XmlElement? mapTimezones = windowsZones["mapTimezones"];
        if (mapTimezones == null)
            throw new FormatException("Missing 'mapTimezones' tag.");

        KeyValuePair<string, string>[] defaults =
        [
            new KeyValuePair<string, string>("US", "America/Chicago"),
            new KeyValuePair<string, string>("PF", "Pacific/Tahiti"),
            new KeyValuePair<string, string>("CA", "America/Toronto"),
            new KeyValuePair<string, string>("MX", "America/Mexico_City"),
            new KeyValuePair<string, string>("EC", "America/Guayaquil"),
            new KeyValuePair<string, string>("BR", "America/Sao_Paulo"),
            new KeyValuePair<string, string>("CL", "America/Santiago"),
            new KeyValuePair<string, string>("GL", "America/Godthab"),
            new KeyValuePair<string, string>("PT", "Europe/Lisbon"),
            new KeyValuePair<string, string>("ES", "Europe/Madrid"),
            new KeyValuePair<string, string>("CD", "Africa/Kinshasa"),
            new KeyValuePair<string, string>("RU", "Europe/Moscow"),
            new KeyValuePair<string, string>("UA", "Europe/Kiev"),
            new KeyValuePair<string, string>("AQ", "Antarctica/Rothera"),
            new KeyValuePair<string, string>("KZ", "Asia/Oral"),
            new KeyValuePair<string, string>("CN", "Asia/Shanghai"),
            new KeyValuePair<string, string>("ID", "Asia/Jakarta"),
            new KeyValuePair<string, string>("MN", "Asia/Ulaanbaatar"),
            new KeyValuePair<string, string>("AU", "Australia/Sydney"),
            new KeyValuePair<string, string>("PG", "Pacific/Port_Moresby"),
            new KeyValuePair<string, string>("FM", "Pacific/Truk"),
            new KeyValuePair<string, string>("UM", "Pacific/Midway"),
            new KeyValuePair<string, string>("NZ", "Pacific/Auckland"),
            new KeyValuePair<string, string>("KI", "Pacific/Tarawa"),
        ];

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        string? last001Fallback = null, last001FallbackWindowsName = null;

        Dictionary<string, List<TimeZoneInfo>> regionTimeZonesTemp = new Dictionary<string, List<TimeZoneInfo>>(256, StringComparer.OrdinalIgnoreCase);

        foreach (XmlElement element in mapTimezones.GetElementsByTagName("mapZone"))
        {
            string windowsName = element.GetAttribute("other");
            string[] ianaNames = element.GetAttribute("type").Split(' ', int.MaxValue, StringSplitOptions.RemoveEmptyEntries);

            string id = isWindows ? windowsName : ianaNames[0].Trim();

            string territory = element.GetAttribute("territory");

            if (!regionTimeZonesTemp.TryGetValue(territory, out List<TimeZoneInfo> tzList))
            {
                tzList = new List<TimeZoneInfo>(4);
                regionTimeZonesTemp.Add(territory, tzList);
            }

            if (!tzList.Exists(x => x.Id.Equals(id, StringComparison.Ordinal)))
            {
                try
                {
                    TimeZoneInfo tzListInfo = TimeZoneInfo.FindSystemTimeZoneById(id);
                    if (!tzList.Exists(x => x.Id.Equals(tzListInfo.Id, StringComparison.Ordinal)))
                        tzList.Add(tzListInfo);
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning($"Time zone not found for region {territory} time zone list: {id}.");
                }
            }

            int index = Array.FindIndex(defaults, x => x.Key.Equals(territory, StringComparison.Ordinal));
            if (index >= 0)
            {
                KeyValuePair<string, string> defaultPair = defaults[index];
                if (!Array.Exists(ianaNames, y => y.Trim().Equals(defaultPair.Value, StringComparison.Ordinal)))
                    continue;

                if (_timeZones.ContainsKey(territory))
                    continue;

                if (!isWindows)
                    id = defaultPair.Value;
            }

            if (string.IsNullOrWhiteSpace(territory)
                || string.IsNullOrWhiteSpace(id)
                || territory.Equals("001", StringComparison.Ordinal)
                || territory.Equals("ZZ", StringComparison.Ordinal))
            {
                last001FallbackWindowsName = windowsName;
                last001Fallback = id;
                continue;
            }

            TimeZoneInfo? tz = null;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning($"Time zone not found for region {territory}: {id}, checking alternatives.");
                if (!isWindows)
                {
                    for (int i = 0; i < ianaNames.Length; ++i)
                    {
                        if (ianaNames[i].Equals(id, StringComparison.Ordinal))
                            continue;

                        try
                        {
                            string ianaName = ianaNames[i].Trim();
                            tz = TimeZoneInfo.FindSystemTimeZoneById(ianaName);
                            id = ianaName;
                        }
                        catch (TimeZoneNotFoundException)
                        {
                            _logger.LogWarning($" + Alternative time zone not found for region {territory}: {id} while checking for alternatives.");
                            continue;
                        }

                        break;
                    }

                    if (tz == null && last001Fallback != null && string.Equals(last001FallbackWindowsName, windowsName))
                    {
                        try
                        {
                            tz = TimeZoneInfo.FindSystemTimeZoneById(last001Fallback);
                            id = last001Fallback;
                        }
                        catch (TimeZoneNotFoundException)
                        {
                            _logger.LogWarning($" + Fallback alternative time zone not found for region {territory}: {id}.");
                            continue;
                        }
                    }
                }

                if (tz == null)
                {
                    _logger.LogWarning($"Time zone not found for region {territory}: {id} after checking alternatives.");
                    continue;
                }

                _logger.LogInformation($"Found alternative time zone for region {territory}: {id} after checking alternatives.");
            }

            if (_timeZones.TryGetValue(territory, out TimeZoneInfo? existingTimezone))
            {
                if (!Array.Exists(defaults, x => x.Key.Equals(territory, StringComparison.Ordinal)))
                    Console.WriteLine($"Conflicting territory |{territory}| tz: \"{existingTimezone.Id}\" with \"{tz.Id}\".");
            }
            else
            {
                _timeZones[territory] = tz;
            }
        }

        // Curacao renamed from AN to CW
        if (!_timeZones.ContainsKey("AN") && _timeZones.TryGetValue("CW", out TimeZoneInfo? curacao))
            _timeZones.Add("AN", curacao);

        // Bouvet Island
        if (!_timeZones.ContainsKey("BV"))
        {
            string bouvetIsland = isWindows ? "W. Europe Standard Time" : "Europe/Oslo";
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(bouvetIsland);
                _timeZones.TryAdd("BV", tz);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogInformation($"Time zone not found for region BV: {bouvetIsland}.");
            }
        }

        foreach (KeyValuePair<string, List<TimeZoneInfo>> regions in regionTimeZonesTemp)
        {
            regions.Value.Sort((a, b) =>
            {
                int cmp = a.BaseUtcOffset.CompareTo(b.BaseUtcOffset);
                return cmp != 0
                    ? cmp
                    : string.Compare(a.Id, b.Id, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);
            });

            _regionTimeZones.Add(regions.Key, regions.Value.ToArray());
        }

        _logger.LogInformation($"Downloaded region info for {_timeZones.Count} regions' default time zones.");
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}