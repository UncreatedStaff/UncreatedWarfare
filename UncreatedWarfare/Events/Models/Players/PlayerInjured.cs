using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Events.Players;
public class PlayerInjured : PlayerEvent
{
    private readonly unsafe void* _parameters;
    public unsafe ref DamagePlayerParameters Parameters => ref Unsafe.AsRef<DamagePlayerParameters>(_parameters);
    public unsafe PlayerInjured(UCPlayer player, in DamagePlayerParameters parameters) : base(player)
    {
        _parameters = Unsafe.AsPointer(ref Unsafe.AsRef(in parameters));
    }
}
public class PlayerInjuring : CancellablePlayerEvent
{
    private readonly unsafe void* _parameters;
    public unsafe ref DamagePlayerParameters Parameters => ref Unsafe.AsRef<DamagePlayerParameters>(_parameters);
    public unsafe PlayerInjuring(UCPlayer player, in DamagePlayerParameters parameters) : base(player)
    {
        _parameters = Unsafe.AsPointer(ref Unsafe.AsRef(in parameters));
    }
}
