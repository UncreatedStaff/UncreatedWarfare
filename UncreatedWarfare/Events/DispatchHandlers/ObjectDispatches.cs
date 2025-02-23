using Uncreated.Warfare.Events.Models.Objects;
using Uncreated.Warfare.Players;
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

    /// <summary>
    /// Invoked by <see cref="NPCEventManager.onEvent"/>.
    /// </summary>
    private void NPCEventManagerOnEvent(Player instigatingplayer, string eventId)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(instigatingplayer);

        NpcEventTriggered args = new NpcEventTriggered
        {
            Player = player,
            Id = eventId
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }
}