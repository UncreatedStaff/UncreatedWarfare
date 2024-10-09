//using Uncreated.Warfare.Buildables;
//using Uncreated.Warfare.Util;

//namespace Uncreated.Warfare.Components;

///// <summary>
///// Tracks various properties of barricades and structures.
///// </summary>
//public class BuildableComponent : MonoBehaviour, IManualOnDestroy
//{
//    /// <summary>
//    /// Realtime since startup when this buildable was created.
//    /// </summary>
//    public float CreateTime { get; private set; }

//    /// <summary>
//    /// The buildable this object was added to.
//    /// </summary>
//    public IBuildable Buildable { get; private set; }

//    private void Init(IBuildable buildable)
//    {
//        Buildable = buildable;
//        CreateTime = Time.realtimeSinceStartup;
//    }

//    [UsedImplicitly]
//    private void OnDisable()
//    {
//        Destroy(this);
//    }

//    void IManualOnDestroy.ManualOnDestroy()
//    {
//        Destroy(this);
//    }

//    /// <summary>
//    /// Try to get the component of a buildable if it's attached.
//    /// </summary>
//    /// <exception cref="GameThreadException"/>
//    public static bool TryGet(IBuildable buildable, out BuildableComponent component)
//    {
//        GameThread.AssertCurrent();
//        return buildable.Model.gameObject.TryGetComponent(out component);
//    }

//    /// <summary>
//    /// Try to get the component of a barricade if it's attached.
//    /// </summary>
//    /// <exception cref="GameThreadException"/>
//    public static bool TryGet(BarricadeDrop barricade, out BuildableComponent component)
//    {
//        GameThread.AssertCurrent();
//        return barricade.model.gameObject.TryGetComponent(out component);
//    }

//    /// <summary>
//    /// Try to get the component of a structure if it's attached.
//    /// </summary>
//    /// <exception cref="GameThreadException"/>
//    public static bool TryGet(StructureDrop structure, out BuildableComponent component)
//    {
//        GameThread.AssertCurrent();
//        return structure.model.gameObject.TryGetComponent(out component);
//    }

//    /// <summary>
//    /// Get the component of a buildable, or add it if it's not already attached.
//    /// </summary>
//    /// <exception cref="GameThreadException"/>
//    public static BuildableComponent GetOrAdd(IBuildable buildable)
//    {
//        GameThread.AssertCurrent();

//        GameObject gameObject = buildable.Model.gameObject;
//        if (gameObject.TryGetComponent(out BuildableComponent comp))
//        {
//            if (comp.Buildable.InstanceId != buildable.InstanceId || comp.Buildable.IsStructure != buildable.IsStructure)
//            {
//                comp.Init(buildable);
//            }

//            return comp;
//        }

//        comp = gameObject.AddComponent<BuildableComponent>();
//        comp.Init(buildable);
//        return comp;
//    }

//    /// <summary>
//    /// Get the component of a barricade, or add it if it's not already attached.
//    /// </summary>
//    /// <exception cref="GameThreadException"/>
//    public static BuildableComponent GetOrAdd(BarricadeDrop barricade)
//    {
//        GameThread.AssertCurrent();

//        GameObject gameObject = barricade.model.gameObject;
//        if (gameObject.TryGetComponent(out BuildableComponent comp))
//        {
//            if (comp.Buildable.InstanceId != barricade.instanceID || comp.Buildable.IsStructure)
//            {
//                comp.Init(new BuildableBarricade(barricade));
//            }

//            return comp;
//        }

//        comp = gameObject.AddComponent<BuildableComponent>();
//        comp.Init(new BuildableBarricade(barricade));
//        return comp;
//    }

//    /// <summary>
//    /// Get the component of a structure, or add it if it's not already attached.
//    /// </summary>
//    /// <exception cref="GameThreadException"/>
//    public static BuildableComponent GetOrAdd(StructureDrop structure)
//    {
//        GameThread.AssertCurrent();

//        GameObject gameObject = structure.model.gameObject;
//        if (gameObject.TryGetComponent(out BuildableComponent comp))
//        {
//            if (comp.Buildable.InstanceId != structure.instanceID || !comp.Buildable.IsStructure)
//            {
//                comp.Init(new BuildableStructure(structure));
//            }

//            return comp;
//        }

//        comp = gameObject.AddComponent<BuildableComponent>();
//        comp.Init(new BuildableStructure(structure));
//        return comp;
//    }
//}