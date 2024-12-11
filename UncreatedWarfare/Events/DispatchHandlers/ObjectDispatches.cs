using Uncreated.Warfare.Events.Models.Objects;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;

partial class EventDispatcher
{
    /// <summary>
    /// Invoked by <see cref="ObjectManager.OnQuestObjectUsed"/> when a player interacts with a quest object.
    /// </summary>
    private void ObjectManagerOnQuestObjectUsed(Player player, InteractableObject obj)
    {
        ObjectInfo foundObject = LevelObjectUtility.FindObject(obj.transform);

        if (!foundObject.HasValue)
            return;

        QuestObjectInteracted args = new QuestObjectInteracted
        {
            Player = _playerService.GetOnlinePlayer(player),
            Interactable = obj,
            Object = foundObject.Object,
            ObjectIndex = foundObject.Index,
            RegionPosition = foundObject.Coord,
            Transform = obj.transform
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }
}