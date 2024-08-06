using System;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;
public class AttachCommand : IExecutableCommand
{
    private const string Syntax = "/attach <item...> | (<remove> <sight|tact|grip|barrel|ammo>) | (<setammo> <amt>) | (<firemode> <safety|semi|auto|burst>)";

    private readonly AttachTranslations _translations;

    private static readonly Guid FiremodeEffectGuid = new Guid("bc41e0feaebe4e788a3612811b8722d3");
    private static EffectAsset? _firemodeEffect;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public AttachCommand(TranslationInjection<AttachTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Modify guns past what vanilla Unturned allows.",
            Parameters =
            [
                new CommandParameter("Item", typeof(ItemCaliberAsset))
                {
                    Description = "Attach an item to your gun."
                },
                new CommandParameter("Remove")
                {
                    Aliases = [ "delete", "clear" ],
                    Description = "Remove an item from your gun.",
                    ChainDisplayCount = 2,
                    Parameters =
                    [
                        new CommandParameter("Slot", "Sight", "Tactical", "Grip", "Barrel", "Ammo")
                    ]
                },
                new CommandParameter("Ammo")
                {
                    Aliases = [ "ammoct", "setammo" ],
                    Description = "Set the amount of ammo in your gun (up to 255).",
                    ChainDisplayCount = 2,
                    Parameters =
                    [
                        new CommandParameter("Amount", typeof(int))
                    ]
                },
                new CommandParameter("Firemode")
                {
                    Aliases = [ "firerate", "mode" ],
                    Description = "Change the firemode of your gun.",
                    ChainDisplayCount = 2,
                    Parameters =
                    [
                        new CommandParameter("Mode", "Safety", "Semi", "Auto", "Burst")
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, Syntax);

        Context.AssertArgs(1, Syntax);

        if (Context.Player.UnturnedPlayer.equipment.asset is not ItemGunAsset gunAsset)
            throw Context.Reply(_translations.AttachNoGunHeld);

        byte[] state = Context.Player.UnturnedPlayer.equipment.state;
        
        _firemodeEffect ??= Assets.find(FiremodeEffectGuid) as EffectAsset;

        AttachmentType type;
        ushort oldItem;
        Span<byte> stateSpan;

        if (Context.MatchParameter(0, "remove", "delete", "clear"))
        {
            Context.AssertArgs(2, Syntax);

            if (Context.MatchParameter(1, "sight", "scope", "reticle"))
                type = AttachmentType.Sight;
            else if (Context.MatchParameter(1, "tact", "tactical", "laser"))
                type = AttachmentType.Tactical;
            else if (Context.MatchParameter(1, "grip", "stand"))
                type = AttachmentType.Grip;
            else if (Context.MatchParameter(1, "barrel", "silencer", "suppressor"))
                type = AttachmentType.Barrel;
            else if (Context.MatchParameter(1, "ammo", "mag", "magazine"))
                type = AttachmentType.Magazine;
            else
                throw Context.Reply(_translations.AttachClearInvalidType, Context.Get(1)!);

            oldItem = BitConverter.ToUInt16(state, (int)type);
            if (oldItem == 0)
            {
                throw Context.Reply(_translations.AttachClearAlreadyGone, gunAsset, type);
            }

            stateSpan = state.AsSpan();

            // attachment id
            BitConverter.TryWriteBytes(stateSpan[(int)type..], 0);

            // durability
            stateSpan[(int)type / 2 + 13] = 100;
            if (type == AttachmentType.Magazine)
            {
                stateSpan[10] = 0; // ammo count
            }

            Context.Player.UnturnedPlayer.equipment.sendUpdateState();
            if (_firemodeEffect != null)
                F.TriggerEffectReliable(_firemodeEffect, EffectManager.SMALL, Context.Player.Position);
            Context.LogAction(ActionLogType.Detach, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Type: " + type + (Assets.find(EAssetType.ITEM, oldItem) is ItemAsset iAsset ? (" | Prev: " + ActionLog.AsAsset(iAsset)) : string.Empty));
            throw Context.Reply(_translations.AttachClearSuccess, gunAsset, type);
        }

        if (Context.MatchParameter(0, "ammo", "ammoct", "setammo") && Context.TryGet(1, out byte amt))
        {
            byte prevAmt = state[10];
            state[10] = amt;
            Context.Player.UnturnedPlayer.equipment.sendUpdateState();
            if (_firemodeEffect != null)
                F.TriggerEffectReliable(_firemodeEffect, EffectManager.SMALL, Context.Player.Position);
            Context.LogAction(ActionLogType.SetAmmo, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Amt: " + amt + (prevAmt != amt ? " | Prev: " + prevAmt : string.Empty));
            throw Context.Reply(_translations.AttachSetAmmoSuccess, gunAsset, amt);
        }

        if (Context.MatchParameter(0, "firerate", "firemode", "mode") && Context.TryGet(1, out string firemodeStr) && TryGetFiremode(firemodeStr, out EFiremode mode))
        {
            EFiremode prevMode = (EFiremode)state[11];
            state[11] = (byte)mode;
            Context.Player.UnturnedPlayer.equipment.sendUpdateState();
            if (_firemodeEffect != null)
                F.TriggerEffectReliable(_firemodeEffect, EffectManager.SMALL, Context.Player.Position);
            Context.LogAction(ActionLogType.SetFiremode, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Mode: " + mode + (prevMode != mode ? " | Prev: " + prevMode : string.Empty));
            throw Context.Reply(_translations.AttachSetFiremodeSuccess, gunAsset, mode);
        }

        if (!Context.TryGet(0, out ItemCaliberAsset? asset, out _, true, allowMultipleResults: true))
            throw Context.Reply(_translations.AttachCaliberNotFound, Context.Get(0)!);
        
        type = GetAttachmentType(asset);

        if (type == AttachmentType.None)
        {
            throw Context.Reply(_translations.AttachCaliberNotFound, Context.Get(0)!);
        }

        oldItem = BitConverter.ToUInt16(state, (int)type);
        stateSpan = state.AsSpan();

        // attachment id
        BitConverter.TryWriteBytes(stateSpan[(int)type..], asset.id);

        // durability
        stateSpan[(int)type / 2 + 13] = 100;

        // ammo count
        if (type == AttachmentType.Magazine)
        {
            stateSpan[10] = asset.amount;
        }

        Context.Player.UnturnedPlayer.equipment.sendUpdateState();
        if (_firemodeEffect != null)
            F.TriggerEffectReliable(_firemodeEffect, EffectManager.SMALL, Context.Player.Position);
        Context.LogAction(ActionLogType.Attach, "Gun: " + ActionLog.AsAsset(gunAsset) + " | Type: " + type + " | Item: " + ActionLog.AsAsset(asset) + (Assets.find(EAssetType.ITEM, oldItem) is ItemAsset iAsset2 ? " | Prev: " + ActionLog.AsAsset(iAsset2) : string.Empty));
        throw Context.Reply(_translations.AttachSuccess, gunAsset, type, asset);
    }

    private static bool TryGetFiremode(string str, out EFiremode firemode)
    {
        if (str.IndexOf("semi", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.SEMI;
        else if (str.IndexOf("auto", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.AUTO;
        else if (str.IndexOf("burst", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.BURST;
        else if (str.IndexOf("saf", StringComparison.InvariantCultureIgnoreCase) != -1)
            firemode = EFiremode.SAFETY;
        else
        {
            firemode = EFiremode.SAFETY;
            return false;
        }

        return true;
    }

    private static AttachmentType GetAttachmentType(ItemCaliberAsset asset)
    {
        return asset switch
        {
            ItemSightAsset => AttachmentType.Sight,
            ItemTacticalAsset => AttachmentType.Tactical,
            ItemGripAsset => AttachmentType.Grip,
            ItemBarrelAsset => AttachmentType.Barrel,
            ItemMagazineAsset => AttachmentType.Magazine,
            _ => AttachmentType.None
        };
    }
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

public class AttachTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Attach Command";

    [TranslationData("Sent when a player tries to use /attach without holding a gun.", IsPriorityTranslation = false)]
    public readonly Translation AttachNoGunHeld = new Translation("<#ff8c69>You must be holding a gun to attach an attachment.");

    [TranslationData("Sent when a player tries to use /attach remove without providing a valid attachment type.", "Caller's input", IsPriorityTranslation = false)]
    public readonly Translation<string> AttachClearInvalidType = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a valid attachment type. Enter one of the following: <#fff><sight|tact|grip|barrel|ammo></color>.");

    [TranslationData("Sent when a player tries to use /attach remove <type> without that attachment.", "Held gun asset", "Type of attachment", IsPriorityTranslation = false)]
    public readonly Translation<ItemGunAsset, AttachmentType> AttachClearAlreadyGone = new Translation<ItemGunAsset, AttachmentType>("<#ff8c69>There is not a <#cedcde>{1}</color> on your {0}.", FormatRarityColor, FormatUppercase);

    [TranslationData("Sent when a player successfully uses /attach remove <type>.", "Held gun asset", "Type of attachment", IsPriorityTranslation = false)]
    public readonly Translation<ItemGunAsset, AttachmentType> AttachClearSuccess = new Translation<ItemGunAsset, AttachmentType>("<#bfb9ac>You removed the <#cedcde>{1}</color> from your {0}.", FormatRarityColor, FormatUppercase);

    [TranslationData("Sent when a player successfully uses /attach <attachment>.", "Held gun asset", "Type of attachment", "Attachment item asset", IsPriorityTranslation = false)]
    public readonly Translation<ItemGunAsset, AttachmentType, ItemCaliberAsset> AttachSuccess = new Translation<ItemGunAsset, AttachmentType, ItemCaliberAsset>("<#bfb9ac>Added {2} as a <#cedcde>{1}</color> to your {0}.", FormatRarityColor, FormatUppercase, FormatRarityColor);

    [TranslationData("Sent when a player tries to attach an item but either it's not an attachment or can't be found.", "Caller's input", IsPriorityTranslation = false)]
    public readonly Translation<string> AttachCaliberNotFound = new Translation<string>("<#ff8c69>Unable to find an attachment named <#fff>{0}</color>.", FormatPropercase);

    [TranslationData("Sent when a player successfully sets the ammo count of a gun.", "Held gun asset", "Amount of ammo", IsPriorityTranslation = false)]
    public readonly Translation<ItemGunAsset, byte> AttachSetAmmoSuccess = new Translation<ItemGunAsset, byte>("<#bfb9ac>Set the ammo count in your {0} to <#fff>{1}</color>.", FormatRarityColor);

    [TranslationData("Sent when a player successfully sets the ammo count of a gun.", "Held gun asset", "Amount of ammo", IsPriorityTranslation = false)]
    public readonly Translation<ItemGunAsset, EFiremode> AttachSetFiremodeSuccess = new Translation<ItemGunAsset, EFiremode>("<#bfb9ac>Set the fire mode of your {0} to <#cedcde>{1}</color>.", FormatRarityColor, FormatUppercase);
}