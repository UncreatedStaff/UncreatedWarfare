using System;

namespace Uncreated.Warfare.Players.Management;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PlayerComponentAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PlayerTaskAttribute : Attribute;