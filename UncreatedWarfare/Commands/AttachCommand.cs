using System;
using SDG.Unturned;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class AttachCommand : Command
{
    private const string Syntax = "/attach <item...> | (<remove> <sight|tact|grip|barrel|ammo>) | (<setammo> <amt>) | (<firemode> <safety|semi|auto|burst>)";

    private static readonly JsonAssetReference<EffectAsset> FiremodeEffect = new JsonAssetReference<EffectAsset>("bc41e0feaebe4e788a3612811b8722d3");

    public AttachCommand() : base("attach", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = "Modify guns past what vanilla Unturned allows.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Item", typeof(ItemCaliberAsset))
                {
                    Description = "Attach an item to your gun."
                },
                new CommandParameter("Remove")
                {
                    Aliases = new string[] { "delete", "clear" },
                    Description = "Remove an item from your gun.",
                    ChainDisplayCount = 2,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Slot", "Sight", "Tactical", "Grip", "Barrel", "Ammo")
                    }
                },
                new CommandParameter("Ammo")
                {
                    Aliases = new string[] { "ammoct", "setammo" },
                    Description = "Set the amount of ammo in your gun (up to 255).",
                    ChainDisplayCount = 2,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Amount", typeof(int))
                    }
                },
                new CommandParameter("Firemode")
                {
                    Aliases = new string[] { "firerate", "mode" },
                    Description = "Change the firemode of your gun.",
                    ChainDisplayCount = 2,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Mode", "Safety", "Semi", "Auto", "Burst")
                    }
                }
            }
        };
    }

    public override unsafe void Execute(CommandContext ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, Syntax);

        ctx.AssertArgs(1, Syntax);

        if (ctx.Caller.Player.equipment.asset is not ItemGunAsset gunAsset)
            throw ctx.Reply(T.AttachNoGunHeld);

        byte[] state = ctx.Caller.Player.equipment.state;
        if (ctx.MatchParameter(0, "remove", "delete", "clear"))
        {
            ctx.AssertArgs(2, Syntax);

            AttachmentType type;
            if (ctx.MatchParameter(1, "sight", "scope", "reticle"))
                type = AttachmentType.Sight;
            else if (ctx.MatchParameter(1, "tact", "tactical", "laser"))
                type = AttachmentType.Tactical;
            else if (ctx.MatchParameter(1, "grip", "stand"))
                type = AttachmentType.Grip;
            else if (ctx.MatchParameter(1, "barrel", "silencer", "suppressor"))
                type = AttachmentType.Barrel;
            else if (ctx.MatchParameter(1, "ammo", "mag", "magazine"))
                type = AttachmentType.Magazine;
            else
                throw ctx.Reply(T.AttachClearInvalidType, ctx.Get(1)!);
            if (state[(int)type] == 0 && state[(int)type + 1] == 0)
                throw ctx.Reply(T.AttachClearAlreadyGone, gunAsset, type);
            ushort oldItem = BitConverter.ToUInt16(state, (int)type);
            state[(int)type] = 0;
            state[(int)type + 1] = 0;
            state[(int)type / 2 + 13] = 100; // durability
            if (type == AttachmentType.Magazine) state[10] = 0; // ammo count
            ctx.Caller.Player.equipment.sendUpdateState();
            if (FiremodeEffect.ValidReference(out EffectAsset effect))
                F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);
            ctx.LogAction(ActionLogType.Detach, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Type: " + type + (Assets.find(EAssetType.ITEM, oldItem) is ItemAsset iAsset ? (" | Prev: " + ActionLog.AsAsset(iAsset)) : string.Empty));
            throw ctx.Reply(T.AttachClearSuccess, gunAsset, type);
        }
        if (ctx.MatchParameter(0, "ammo", "ammoct", "setammo") && ctx.TryGet(1, out byte amt))
        {
            byte prevAmt = state[10];
            state[10] = amt;
            ctx.Caller.Player.equipment.sendUpdateState();
            if (FiremodeEffect.ValidReference(out EffectAsset effect))
                F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);
            ctx.LogAction(ActionLogType.SetAmmo, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Amt: " + amt + (prevAmt != amt ? " | Prev: " + prevAmt : string.Empty));
            throw ctx.Reply(T.AttachSetAmmoSuccess, gunAsset, amt);
        }
        if (ctx.MatchParameter(0, "firerate", "firemode", "mode") && ctx.TryGet(1, out string firemodeStr) && TryGetFiremode(firemodeStr, out EFiremode mode))
        {
            EFiremode prevMode = (EFiremode)state[11];
            state[11] = (byte)mode;
            ctx.Caller.Player.equipment.sendUpdateState();
            if (FiremodeEffect.ValidReference(out EffectAsset effect))
                F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);
            ctx.LogAction(ActionLogType.SetFiremode, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Mode: " + mode + (prevMode != mode ? " | Prev: " + prevMode : string.Empty));
            throw ctx.Reply(T.AttachSetFiremodeSuccess, gunAsset, mode);
        }
        if (ctx.TryGet(0, out ItemCaliberAsset asset, out _, true, allowMultipleResults: true))
        {
            AttachmentType type = GetAttachmentType(asset);
            if (type != AttachmentType.None)
            {
                ushort oldItem = BitConverter.ToUInt16(state, (int)type);
                fixed (byte* ptr = state)
                {
                    UnsafeBitConverter.GetBytes(ptr, asset.id, (int)type);
                    ptr[(int)type / 2 + 13] = 100; // durability
                    if (type == AttachmentType.Magazine) ptr[10] = asset.amount; // ammo count
                }
                ctx.Caller.Player.equipment.sendUpdateState();
                if (FiremodeEffect.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);
                ctx.LogAction(ActionLogType.Attach, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Type: " + type + " | Item: " + ActionLog.AsAsset(asset) + (Assets.find(EAssetType.ITEM, oldItem) is ItemAsset iAsset ? " | Prev: " + ActionLog.AsAsset(iAsset) : string.Empty));
                throw ctx.Reply(T.AttachSuccess, gunAsset, type, asset);
            }
        }
        throw ctx.Reply(T.AttachCaliberNotFound, ctx.Get(0)!);
    }
    private static bool TryGetFiremode(string str, out EFiremode firemode)
    {
        if (str.IndexOf("semi", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.SEMI;
        else if (str.IndexOf("auto", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.AUTO;
        else if (str.IndexOf("burst", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.BURST;
        else if (str.IndexOf("safe", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.SAFETY;
        else
        {
            firemode = EFiremode.SAFETY;
            return false;
        }

        return true;
    }
    private static AttachmentType GetAttachmentType(ItemCaliberAsset asset) =>
        asset switch
        {
            ItemSightAsset => AttachmentType.Sight,
            ItemTacticalAsset => AttachmentType.Tactical,
            ItemGripAsset => AttachmentType.Grip,
            ItemBarrelAsset => AttachmentType.Barrel,
            ItemMagazineAsset => AttachmentType.Magazine,
            _ => AttachmentType.None
        };
}

public enum AttachmentType : byte
{
    None = 255,
    Sight = 0,
    Tactical = 2,
    Grip = 4,
    Barrel = 6,
    Magazine = 8
}