using System;
using System.Collections.Generic;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Networking;
public sealed class MySqlAddressFilter : IIPAddressFilter
{
    private readonly Func<IManualMySqlProvider> _database;
    public MySqlAddressFilter(Func<IManualMySqlProvider> database)
    {
        _database = database;
    }
    public async ValueTask<bool> IsFiltered(uint packedIp, CSteamID player, CancellationToken token)
    {
        bool matched = false;
        await _database().QueryAsync($"SELECT `{DatabaseInterface.ColumnIPWhitelistsIPRange}` FROM `{DatabaseInterface.TableIPWhitelists}` WHERE `{DatabaseInterface.ColumnIPWhitelistsSteam64}` = @0;",
            [ player.m_SteamID ], token, reader =>
            {
                if (reader.IsDBNull(0))
                    return true;

                string str = reader.GetString(0);

                if (str.Equals("*", StringComparison.Ordinal) || str.Equals("%", StringComparison.Ordinal))
                {
                    matched = true;
                }
                else if (IPv4Range.TryParse(str, out IPv4Range range) || IPv4Range.TryParseIPv4(str, out range))
                {
                    matched = range.InRange(packedIp);
                }

                return !matched;
            }).ConfigureAwait(false);

        return matched;
    }
    public async ValueTask RemoveFilteredIPs<T>(IList<T> ips, Func<T, uint> selector, CSteamID player, CancellationToken token)
    {
        await _database().QueryAsync($"SELECT `{DatabaseInterface.ColumnIPWhitelistsIPRange}` FROM `{DatabaseInterface.TableIPWhitelists}` WHERE `{DatabaseInterface.ColumnIPWhitelistsSteam64}` = @0;",
            [ player.m_SteamID ], token, reader =>
            {
                if (reader.IsDBNull(0))
                    return true;

                string str = reader.GetString(0);
                if (str.Equals("*", StringComparison.Ordinal) || str.Equals("%", StringComparison.Ordinal))
                {
                    ips.Clear();
                }
                else if (IPv4Range.TryParse(str, out IPv4Range range) || IPv4Range.TryParseIPv4(str, out range))
                {
                    for (int i = ips.Count - 1; i >= 0; --i)
                    {
                        if (range.InRange(selector(ips[i])))
                            ips.RemoveAt(i);
                    }
                }

                return ips.Count != 0;
            }).ConfigureAwait(false);
    }
}