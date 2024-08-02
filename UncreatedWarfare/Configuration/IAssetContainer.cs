using System;

namespace Uncreated.Warfare.Configuration;
public interface IAssetContainer
{
    Guid Guid { get; }
    ushort Id { get; }
    Asset? Asset { get; }
}
