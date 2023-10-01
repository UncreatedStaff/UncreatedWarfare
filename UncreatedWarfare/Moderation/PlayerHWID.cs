using System;
using System.Globalization;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;
public class PlayerHWID : IListItem
{
    public PrimaryKey PrimaryKey { get; set; }
    public HWID HWID { get; set; }
    public int Index { get; set; }
    public int LoginCount { get; set; }
    public ulong Steam64 { get; set; }
    public DateTimeOffset? FirstLogin { get; set; }
    public DateTimeOffset LastLogin { get; set; }
    public PlayerHWID() { }
    public PlayerHWID(PrimaryKey primaryKey, int index, ulong steam64, HWID hwid, int loginCount, DateTimeOffset? firstLogin, DateTimeOffset lastLogin)
    {
        PrimaryKey = primaryKey;
        Index = index;
        Steam64 = steam64;
        HWID = hwid;
        LoginCount = loginCount;
        FirstLogin = firstLogin;
        LastLogin = lastLogin;
    }

    public override string ToString() => "# " + Index.ToString(CultureInfo.InvariantCulture) + ", HWID: " + HWID;
}