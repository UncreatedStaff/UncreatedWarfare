using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Patches;
using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher
{
    /// <summary>
    /// Invoked by <see cref="DamageTool.damagePlayerRequested"/> when a player starts to get damaged. Can be cancelled.
    /// </summary>
    private void DamageToolOnPlayerDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldallow)
    {
        if (!shouldallow || parameters.times == 0f)
            return;

        WarfarePlayer player = _playerService.GetOnlinePlayer(parameters.player);

        DamagePlayerRequested args = new DamagePlayerRequested(in parameters, _playerService)
        {
            Player = player
        };

        // can't support async event handlers because any code calling damagePlayer
        // may expect the player to take damage or die instantly
        //  ex. hitmarkers are handled by checking which players were damaged immediately after shooting
        shouldallow = DispatchEventAsync(args, _unloadToken, allowAsync: false).GetAwaiter().GetResult();

        if (!shouldallow)
            return;
        
        PlayerLifeDoDamageRequested.Damaging = parameters.player.channel.owner.playerID.steamID.m_SteamID;
        parameters = args.Parameters;
        player.Data["LastDamagePlayerRequested"] = args;
    }

    /// <summary>
    /// Invoked by <see cref="PlayerEquipment.onEquipRequested"/>.
    /// </summary>
    private void OnPlayerEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
    {
        if (!ItemUtility.TryFindJarPage(equipment.player.inventory, jar, out Page page) || asset == null)
        {
            shouldAllow = false;
            return;
        }

        WarfarePlayer player = _playerService.GetOnlinePlayer(equipment);

        EquipUseableRequested args = new EquipUseableRequested
        {
            Player = player,
            Asset = asset,
            Page = page,
            Item = jar
        };

        EventContinuations.Dispatch(args, this, _unloadToken, out shouldAllow, static args =>
        {
            if (!args.Player.IsOnline)
                return;

            if (!args.Player.UnturnedPlayer.inventory.items[(int)args.Page].items.Contains(args.Item))
                return;
            
            args.Player.UnturnedPlayer.equipment.ServerEquip((byte)args.Page, args.Item.x, args.Item.y);
        });
    }

    /// <summary>
    /// Invoked by <see cref="PlayerEquipment.onDequipRequested"/>.
    /// </summary>
    private void OnPlayerDequipRequested(PlayerEquipment equipment, ref bool shouldAllow)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(equipment);

        ItemJar? equipped = player.GetHeldItem(out Page page);

        if (equipped == null || equipment.asset == null)
        {
            return;
        }

        DequipUseableRequested args = new DequipUseableRequested
        {
            Player = player,
            EquippedAsset = equipment.asset,
            EquippedPage = page,
            EquppedItem = equipped
        };

        EventContinuations.Dispatch(args, this, _unloadToken, out shouldAllow, static args =>
        {
            if (!args.Player.IsOnline)
                return;

            args.Player.UnturnedPlayer.equipment.ServerEquip(byte.MaxValue, 0, 0);
        });
    }

    /// <summary>
    /// Invoked by <see cref="PlayerLife.OnPreDeath"/> when a player is just about to die.
    /// </summary>
    private void PlayerLifeOnOnPreDeath(PlayerLife playerLife)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(playerLife.player);
        if (!player.Data.TryRemove("LastDamagePlayerRequested", out object? v) || v is not DamagePlayerRequested reqArgs)
            return;

        PlayerDamaged args = new PlayerDamaged(in reqArgs.Parameters)
        {
            Player = player,
            Instigator = _playerService.GetOnlinePlayerOrNull(reqArgs.Parameters.killer),
            IsDeath = true,
            IsInjure = false
        };

        PlayerDying dyingArgs = new PlayerDying(in reqArgs.Parameters)
        {
            Player = player,
            Instigator = args.Instigator
        };

        player.Data["LastPlayerDying"] = dyingArgs;

        _ = DispatchEventAsync(args, CancellationToken.None);
        _ = DispatchEventAsync(dyingArgs, CancellationToken.None, allowAsync: false);
    }

    /// <summary>
    /// Invoked by <see cref="PlayerLife.onHurt"/> when a player gets damaged.
    /// </summary>
    private void OnPlayerHurt(Player unturnedPlayer, byte damage, Vector3 force, EDeathCause cause, ELimb limb, CSteamID killer)
    {
        if (unturnedPlayer.life.isDead)
            return;

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);
        if (!player.Data.TryRemove("LastDamagePlayerRequested", out object? v) || v is not DamagePlayerRequested reqArgs)
            return;

        PlayerDamaged args = new PlayerDamaged(in reqArgs.Parameters)
        {
            Player = player,
            Instigator = _playerService.GetOnlinePlayerOrNull(reqArgs.Parameters.killer),
            IsDeath = false,
            IsInjure = reqArgs.IsInjure
        };

        _ = DispatchEventAsync(args, CancellationToken.None);
    }

    /// <summary>
    /// Invoked by <see cref="UseableConsumeable.onPerformingAid"/> when a player starts to heal another player. Can be cancelled.
    /// </summary>
    private void UseableConsumeableOnPlayerPerformingAid(Player instigator, Player target, ItemConsumeableAsset asset, ref bool shouldAllow)
    {
        WarfarePlayer medic = _playerService.GetOnlinePlayer(instigator);
        WarfarePlayer player = _playerService.GetOnlinePlayer(target);

        PlayerLife targetLife = player.UnturnedPlayer.life;
        AidPlayerRequested args = new AidPlayerRequested
        {
            Item = AssetLink.Create(asset),
            Player = player,
            Medic = medic,
            IsRevive = false,
            StartingHealth = targetLife.health,
            StartingBleedState = targetLife.isBleeding,
            StartingBrokenBonesState = targetLife.isBroken,
            StartingFood = targetLife.food,
            StartingWater = targetLife.water,
            StartingInfection = targetLife.virus,
            StartingStamina = targetLife.stamina,
            StartingWarmth = targetLife.warmth,
            StartingExperience = player.UnturnedPlayer.skills.experience
        };

        shouldAllow = DispatchEventAsync(args, _unloadToken, allowAsync: false).GetAwaiter().GetResult();

        if (!shouldAllow)
            return;

        if (args.IsRevive)
        {
            player.ComponentOrNull<PlayerInjureComponent>()?.PrepAidRevive(args);
        }

        player.Data["LastAidRequested"] = args;
    }

    /// <summary>
    /// Invoked by <see cref="UseableConsumeable.onPerformedAid"/> when a player heals another player.
    /// </summary>
    private void UseableConsumeableOnPlayerPerformedAid(Player instigator, Player target)
    {
        WarfarePlayer medic = _playerService.GetOnlinePlayer(instigator);
        WarfarePlayer player = _playerService.GetOnlinePlayer(target);

        if (!player.Data.TryRemove("LastAidRequested", out object? v) || v is not AidPlayerRequested reqArgs)
            return;


        PlayerLife targetLife = player.UnturnedPlayer.life;
        PlayerAided args = new PlayerAided
        {
            Player = player,
            Medic = medic,
            IsRevive = reqArgs.IsRevive,
            IsEffectiveRevive = reqArgs.IsEffectiveRevive,
            Item = reqArgs.Item,
            HealthChange = reqArgs.StartingHealth - targetLife.health,
            BleedStateChanged = reqArgs.StartingBleedState != targetLife.isBleeding,
            BrokenBonesStateChanged = reqArgs.StartingBrokenBonesState != targetLife.isBroken,
            FoodChange = reqArgs.StartingFood - targetLife.food,
            WaterChange = reqArgs.StartingWater - targetLife.water,
            InfectionChange = reqArgs.StartingInfection - targetLife.virus,
            StaminaChange = reqArgs.StartingStamina - targetLife.stamina,
            WarmthChange = (int)((long)reqArgs.StartingWarmth - targetLife.warmth),
            ExperienceChange = (int)((long)reqArgs.StartingExperience - player.UnturnedPlayer.skills.experience)
        };

        _ = DispatchEventAsync(args, CancellationToken.None);
    }

    /// <summary>
    /// Invoked by <see cref="PlayerEquipment.OnPunch_Global"/> when a player punches with either hand.
    /// </summary>
    private void PlayerEquipmentOnPlayerPunch(PlayerEquipment player, EPlayerPunch punchType)
    {
        PlayerPunched args = new PlayerPunched
        {
            Player = _playerService.GetOnlinePlayer(player.player),
            PunchType = punchType
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }

    /// <summary>
    /// Invoked by <see cref="PlayerQuests.onGroupChanged"/> when a player's group ID or rank chnages.
    /// </summary>
    private void PlayerQuestsOnGroupChanged(PlayerQuests sender, CSteamID oldGroupId, EPlayerGroupRank oldGroupRank, CSteamID newGroupId, EPlayerGroupRank newGroupRank)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(sender);
        if (player == null)
        {
            // this can get invoked before the player's WarfarePlayer gets created when moving them into their team.
            return;
        }

        ITeamManager<Team>? teamManager = _warfare.IsLayoutActive() ? _warfare.GetActiveLayout().TeamManager : null;

        Team oldTeam = Team.NoTeam,
             newTeam = Team.NoTeam;

        if (teamManager != null)
        {
            oldTeam = teamManager.GetTeam(oldGroupId);
            newTeam = teamManager.GetTeam(newGroupId);

            player.UpdateTeam(newTeam);
        }
        else
        {
            player.UpdateTeam(Team.NoTeam);
        }

        PlayerGroupChanged args = new PlayerGroupChanged
        {
            Player = player,
            OldGroupId = oldGroupId,
            NewGroupId = newGroupId,
            OldRank = oldGroupRank,
            NewRank = newGroupRank,
            OldTeam = oldTeam,
            NewTeam = newTeam
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }

    /// <summary>
    /// Invoked by <see cref="PlayerEquipment.OnUseableChanged_Global"/> when a player equips or dequips an item.
    /// </summary>
    private void PlayerEquipmentUseableChanged(PlayerEquipment equipment)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(equipment);

        Page dequippedPage = default;
        ItemJar? dequipped = null;
        InteractableVehicle? dequippedVehicle = null;
        byte dequippedSeat = 0;

        if (player.Data.TryRemove("LastEquippedItem", out object? jarBox)
            && jarBox is LastEquipData data)
        {
            if (data.Item != null && ItemUtility.TryFindJarPage(equipment.player.inventory, data.Item, out dequippedPage))
            {
                dequipped = data.Item;
            }
            else if (data.Vehicle != null)
            {
                dequippedVehicle = data.Vehicle;
                dequippedSeat = data.Seat;
            }
        }

        PlayerUseableEquipped args = new PlayerUseableEquipped
        {
            Player = player,
            Item = equipment.asset,
            Useable = equipment.useable,
            DequippedItem = dequipped,
            DequippedItemPage = dequippedPage,
            DequippedSeat = dequippedSeat,
            DequippedVehicle = dequippedVehicle
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }
}