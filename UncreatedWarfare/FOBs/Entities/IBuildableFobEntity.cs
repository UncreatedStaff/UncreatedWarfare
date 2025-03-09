using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Entities;
public interface IBuildableFobEntity : IFobEntity
{
    public IBuildable Buildable { get; }
    Vector3 ITransformObject.Position { get => Buildable.Position; set => Buildable.Position = value; }
    Quaternion ITransformObject.Rotation { get => Buildable.Rotation; set => Buildable.Rotation = value; }
    Vector3 ITransformObject.Scale { get => Buildable.Scale; set => Buildable.Scale = value; }
    void ITransformObject.SetPositionAndRotation(Vector3 position, Quaternion rotation) => Buildable.SetPositionAndRotation(position, rotation);
    bool ITransformObject.Alive => Buildable.Alive;
    bool WipeStorageOnDestroy { get; }
}