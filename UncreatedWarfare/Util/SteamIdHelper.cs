using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Parsing utilities for Steam64 IDs.
/// </summary>
public static class SteamIdHelper
{
    /// <summary>
    /// Check if a Steam64 ID belongs to an individual.
    /// </summary>
    public static bool IsIndividual(this CSteamID steamId)
    {
        return steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;
    }

    /// <summary>
    /// Check if a Steam64 ID belongs to an individual.
    /// </summary>
    public static bool IsIndividualRef(this in CSteamID steamId)
    {
        return Unsafe.AsRef(in steamId).GetEAccountType() == EAccountType.k_EAccountTypeIndividual;
    }

    /// <summary>
    /// Parse a Steam ID in any form.
    /// </summary>
    public static bool TryParseSteamId(string str, out CSteamID steamId)
    {
        if (str.Length > 2 && str[0] is 'N' or 'n' or 'O' or 'o' or 'L' or 'l' or 'z' or 'Z')
        {
            if (str.Equals("Nil", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("zero", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.Nil;
                return true;
            }
            if (str.Equals("OutofDateGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("out-of-date-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("out of date gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("out_of_date_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.OutofDateGS;
                return true;
            }
            if (str.Equals("LanModeGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("lan-mode-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("lan mode gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("lan_mode_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.LanModeGS;
                return true;
            }
            if (str.Equals("NotInitYetGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("not-init-yet-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("not init yet gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("not_init_yet_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.NotInitYetGS;
                return true;
            }
            if (str.Equals("NonSteamGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("non-steam-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("non steam gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("non_steam_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.NonSteamGS;
                return true;
            }
        }

        if (str.Length >= 8 && uint.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out uint acctId1))
        {
            steamId = new CSteamID(new AccountID_t(acctId1), EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeIndividual);
            return true;
        }

        if (str.Length >= 17 && ulong.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong id))
        {
            steamId = new CSteamID(id);

            // try parse as hex instead
            if (steamId.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            {
                if (!ulong.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
                    return true;
                CSteamID steamid2 = new CSteamID(id);
                if (steamid2.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                    steamId = steamid2;
            }
            return true;
        }

        if (str.Length >= 15 && ulong.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong acctId2))
        {
            steamId = new CSteamID(acctId2);
            return true;
        }

        if (str.StartsWith("STEAM_", StringComparison.InvariantCultureIgnoreCase) && str.Length > 10)
        {
            if (str[7] != ':' || str[9] != ':')
                goto fail;
            char uv = str[6];
            if (!char.IsDigit(uv))
                goto fail;
            EUniverse universe = (EUniverse)(uv - 48);
            if (universe == EUniverse.k_EUniverseInvalid)
                universe = EUniverse.k_EUniversePublic;

            bool y;
            if (str[8] == '1')
                y = true;
            else if (str[8] == '0')
                y = false;
            else goto fail;
            if (!uint.TryParse(str.Substring(10), NumberStyles.Number, CultureInfo.InvariantCulture, out uint acctId))
                goto fail;

            steamId = new CSteamID(new AccountID_t((uint)(acctId * 2 + (y ? 1 : 0))), universe, EAccountType.k_EAccountTypeIndividual);
            return true;
        }

        if (str.Length > 8 && str[0] == '[')
        {
            if (str[2] != ':' || str[4] != ':' || str[^1] != ']')
                goto fail;
            EAccountType type;
            char c = str[1];
            if (c is 'I' or 'i')
                type = EAccountType.k_EAccountTypeInvalid;
            else if (c == 'U')
                type = EAccountType.k_EAccountTypeIndividual;
            else if (c == 'M')
                type = EAccountType.k_EAccountTypeMultiseat;
            else if (c == 'G')
                type = EAccountType.k_EAccountTypeGameServer;
            else if (c == 'A')
                type = EAccountType.k_EAccountTypeAnonGameServer;
            else if (c == 'P')
                type = EAccountType.k_EAccountTypePending;
            else if (c == 'C')
                type = EAccountType.k_EAccountTypeContentServer;
            else if (c == 'g')
                type = EAccountType.k_EAccountTypeClan;
            else if (c is 'T' or 'L' or 'c')
                type = EAccountType.k_EAccountTypeChat;
            else if (c == 'a')
                type = EAccountType.k_EAccountTypeAnonUser;
            else goto fail;
            char uv = str[3];
            if (!char.IsDigit(uv))
                goto fail;
            uint acctId;
            if (str[^3] != ':')
            {
                if (!uint.TryParse(str.Substring(5, str.Length - 6), NumberStyles.Number, CultureInfo.InvariantCulture, out acctId))
                    goto fail;
            }
            else
            {
                if (!uint.TryParse(str.Substring(5, str.Length - 8), NumberStyles.Number, CultureInfo.InvariantCulture, out acctId))
                    goto fail;
                acctId *= 2;
                uv = str[^2];
                if (uv == '1')
                    ++acctId;
                else if (uv != '0')
                    goto fail;
            }

            EUniverse universe = (EUniverse)(uv - 48);
            if (universe == EUniverse.k_EUniverseInvalid)
                universe = EUniverse.k_EUniversePublic;

            steamId = new CSteamID(new AccountID_t(acctId), universe, type);
            return true;
        }

        fail:
        steamId = CSteamID.Nil;
        return false;
    }

    /// <summary>
    /// Parse a player Steam ID in any form, or a steam profile URL. Custom URLs are allowed.
    /// </summary>
    public static ValueTask<CSteamID?> TryParseSteamIdOrUrl(string str, CancellationToken token = default)
    {
        if (TryParseSteamId(str, out CSteamID steamId))
        {
            return steamId.IsIndividualRef() ? new ValueTask<CSteamID?>(steamId) : default;
        }

        if (!str.Contains("steamcommunity.com", StringComparison.OrdinalIgnoreCase))
        {
            return default;
        }

        if (!(Uri.TryCreate(str, UriKind.Absolute, out Uri uri)
              || Uri.TryCreate("www." + str, UriKind.Absolute, out uri)
              || Uri.TryCreate("https://" + str, UriKind.Absolute, out uri)
              || Uri.TryCreate("https://www." + str, UriKind.Absolute, out uri))
            || uri.Scheme is not "http" and not "https")
        {
            return default;
        }

        string domain = uri.GetComponents(UriComponents.Host, UriFormat.Unescaped);
        if (!domain.Equals("steamcommunity.com", StringComparison.OrdinalIgnoreCase) && !domain.Equals("www.steamcommunity.com", StringComparison.OrdinalIgnoreCase))
            return default;

        string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        if (path.StartsWith("id/", StringComparison.OrdinalIgnoreCase) && path.Length > 3)
        {
            // custom URL
            ReadOnlySpan<char> customId = path.AsSpan(3);
            int pathEndIndex = customId.IndexOf('/');
            if (pathEndIndex != -1)
                customId = customId.Slice(0, pathEndIndex);

            return new ValueTask<CSteamID?>(GetSteamIdFromCustomUrlId(customId, token));
        }

        if (path.StartsWith("profiles/", StringComparison.OrdinalIgnoreCase) && path.Length > 9)
        {
            // basic URL
            ReadOnlySpan<char> steam64Str = path.AsSpan(9);
            int pathEndIndex = steam64Str.IndexOf('/');
            if (pathEndIndex != -1)
                steam64Str = steam64Str.Slice(0, pathEndIndex);

            if (ulong.TryParse(steam64Str, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64))
                return new ValueTask<CSteamID?>(new CSteamID(steam64));
        }

        return default;
    }

#pragma warning disable CS8500

    /// <summary>
    /// Queries the steam profile endpoint to get the player's Steam ID.
    /// </summary>
    /// <param name="customUrlId">The ID that would be in a custom steam URL.</param>
    public static unsafe Task<CSteamID?> GetSteamIdFromCustomUrlId(ReadOnlySpan<char> customUrlId, CancellationToken token = default)
    {
        CreateUrlState state = default;
        state.CustomId = &customUrlId;

        string newUrl = string.Create(37 + customUrlId.Length, state, (span, state) =>
        {
            ReadOnlySpan<char> begin = "https://steamcommunity.com/id/";
            ReadOnlySpan<char> end = "/?xml=1";

            begin.CopyTo(span);
            state.CustomId->CopyTo(span.Slice(30));
            end.CopyTo(span.Slice(30 + state.CustomId->Length));
        });

        return WarfareModule.IsActive
            ? UnityGetSteamIdFromCustomUrl(newUrl, token)
            : SystemGetSteamIdFromCustomUrl(newUrl, token);
    }

    private unsafe struct CreateUrlState
    {
        public ReadOnlySpan<char>* CustomId;
    }

#pragma warning restore CS8500

    private static async Task<CSteamID?> SystemGetSteamIdFromCustomUrl(string customUrl, CancellationToken token)
    {
        const int tryCount = 2;
        using HttpClient client = new HttpClient();

        for (int tryNum = 0; tryNum < 3; ++tryNum)
        {
            client.Timeout = TimeSpan.FromSeconds(2d + tryNum);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, customUrl);
            string data;
            try
            {
                using HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    // individual not found
                    return null;
                }

                response.EnsureSuccessStatusCode();

                data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                if (tryNum == tryCount - 1)
                    return null;
                
                await Task.Delay(TimeSpan.FromSeconds(1f), token);
                continue;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                string? steamID64 = doc["profile"]?["steamID64"]?.InnerText;

                if (steamID64 == null || !ulong.TryParse(steamID64, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64))
                {
                    return null;
                }

                return new CSteamID(steam64);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static async Task<CSteamID?> UnityGetSteamIdFromCustomUrl(string customUrl, CancellationToken token = default)
    {
        const int tryCount = 2;

        for (int tryNum = 0; tryNum < 3; ++tryNum)
        {
            using UnityWebRequest request = new UnityWebRequest(customUrl, "GET", downloadHandler: new DownloadHandlerBuffer(), uploadHandler: null);
            string data;
            try
            {
                request.timeout = 2 + tryNum;

                await request.SendWebRequest().WithCancellation(token);

                data = request.downloadHandler.text;
            }
            catch (Exception ex)
            {
                if (request.responseCode == 304)
                {
                    // individual not found
                    return null;
                }

                if (tryNum == tryCount - 1)
                {
                    WarfareModule.Singleton.GlobalLogger.LogError(ex, $"UnityGetSteamIdFromCustomUrl - Error executing custom steam URL query: \"{customUrl}\".");
                    return null;
                }

                WarfareModule.Singleton.GlobalLogger.LogError("UnityGetSteamIdFromCustomUrl - Error executing custom steam URL query: \"{0}\". Retrying {1} / {2}.", customUrl, tryNum + 1, tryCount);
                await Task.Delay(TimeSpan.FromSeconds(1f), token);
                continue;
            }

            if (tryNum > 0)
            {
                WarfareModule.Singleton.GlobalLogger.LogInformation("UnityGetSteamIdFromCustomUrl - Executing custom steam URL query: \"{0}\" succeeded after {1} tries.", customUrl, tryNum + 1);
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                string? steamID64 = doc["profile"]?["steamID64"]?.InnerText;

                if (steamID64 == null || !ulong.TryParse(steamID64, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64))
                {
                    if (!string.Equals(doc["response"]?["error"]?.InnerText, "The specified profile could not be found."))
                    {
                        WarfareModule.Singleton.GlobalLogger.LogError($"UnityGetSteamIdFromCustomUrl - Error parsing result from Steam API query: \"{customUrl}\", unable to find 'profile.steamID64' field");
                    }

                    return null;
                }

                return new CSteamID(steam64);
            }
            catch (Exception ex)
            {
                WarfareModule.Singleton.GlobalLogger.LogError(ex, $"UnityGetSteamIdFromCustomUrl - Error parsing result from Steam API query: \"{customUrl}\".");
            }
        }

        return null;
    }
}