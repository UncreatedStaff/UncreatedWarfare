using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.SQL;

namespace Uncreated.Warfare.Networking;
public sealed class MySqlAddressFilter : IIPAddressFilter
{
    private readonly Func<IMySqlDatabase> _database;
    public MySqlAddressFilter(Func<IMySqlDatabase> database)
    {
        _database = database;
    }
    public async ValueTask<bool> IsFiltered(IPAddress ip, ulong player, CancellationToken token)
    {
        bool matched = false;
        await _database().QueryAsync($"SELECT `{WarfareSQL.ColumnIPWhitelistsIPRange}` FROM `{WarfareSQL.TableIPWhitelists}` WHERE `{WarfareSQL.ColumnIPWhitelistsSteam64}` = @0;",
            new object[] { player }, reader =>
            {
                if (!reader.IsDBNull(0))
                {
                    string str = reader.GetString(0);
                    if (str.Equals("*", StringComparison.Ordinal) || str.Equals("%", StringComparison.Ordinal))
                        matched = true;
                    else if (IPv4Range.TryParse(str, out IPv4Range range) || IPv4Range.TryParseIPv4(str, out range))
                        matched = range.InRange(ip);

                    if (matched)
                        return false;
                }

                return true;
            }, token).ConfigureAwait(false);

        return matched;
    }
    public async ValueTask RemoveFilteredIPs<T>(IList<T> ips, Func<T, uint> selector, ulong player, CancellationToken token)
    {
        await _database().QueryAsync($"SELECT `{WarfareSQL.ColumnIPWhitelistsIPRange}` FROM `{WarfareSQL.TableIPWhitelists}` WHERE `{WarfareSQL.ColumnIPWhitelistsSteam64}` = @0;",
            new object[] { player }, reader =>
            {
                if (!reader.IsDBNull(0))
                {
                    string str = reader.GetString(0);
                    if (str.Equals("*", StringComparison.Ordinal) || str.Equals("%", StringComparison.Ordinal))
                        ips.Clear();
                    else if (IPv4Range.TryParse(str, out IPv4Range range) || IPv4Range.TryParseIPv4(str, out range))
                    {
                        for (int i = ips.Count - 1; i >= 0; --i)
                        {
                            if (range.InRange(selector(ips[i])))
                                ips.RemoveAt(i);
                        }
                    }

                    if (ips.Count == 0)
                        return false;
                }

                return true;
            }, token).ConfigureAwait(false);
    }
}
