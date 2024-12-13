using System;
using Uncreated.Warfare.Players.PendingTasks;

namespace Uncreated.Warfare.Players.Management;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IPlayerComponent))]
public sealed class PlayerComponentAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IPlayerPendingTask))]
public sealed class PlayerTaskAttribute : Attribute;