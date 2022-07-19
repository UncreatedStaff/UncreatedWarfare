using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Interfaces;

internal interface IObjective : ITranslationArgument
{
    string Name { get; }
    Vector3 Position { get; }
}