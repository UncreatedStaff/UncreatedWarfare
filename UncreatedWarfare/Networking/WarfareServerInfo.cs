using Uncreated.Encoding;

namespace Uncreated.Homebase.Unturned.Warfare;
public class WarfareServerConfig : ServerConfig
{
    public int FactionTeam1;
    public int FactionTeam2;
    public int MapId;

    protected override void Read(ByteReader reader)
    {
        FactionTeam1 = reader.ReadInt32();
        FactionTeam2 = reader.ReadInt32();
        MapId = reader.ReadInt32();
    }

    protected override void Write(ByteWriter writer)
    {
        writer.Write(FactionTeam1);
        writer.Write(FactionTeam2);
        writer.Write(MapId);
    }
}
