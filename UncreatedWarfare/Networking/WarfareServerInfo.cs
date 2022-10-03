using System;
using Uncreated.Encoding;

namespace Uncreated.Homebase.Unturned.Warfare;
public class WarfareServerInfo : ServerInfo
{
    public WarfareServerInfo() : base(EServer.WARFARE) { }
    internal static WarfareServerInfo Read(ByteReader reader)
    {
        throw new NotImplementedException();
    }
    internal static void Write(ByteWriter writer, WarfareServerInfo info)
    {
        throw new NotImplementedException();
    }
}
