using System;
using System.Globalization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("set", "s"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureSetCommand : IExecutableCommand
{
    private readonly BuildableSaver _saver;
    private readonly ITeamManager<Team> _teamManager;
    private readonly StructureTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureSetCommand(BuildableSaver saver, ITeamManager<Team> teamManager, TranslationInjection<StructureTranslations> translations)
    {
        _saver = saver;
        _teamManager = teamManager;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.HasArgs(2))
        {
            throw Context.SendCorrectUsage("/structure <set|s> <group|owner> <value>");
        }

        bool isSettingGroup = Context.MatchParameter(0, "group");
        if (!isSettingGroup && !Context.MatchParameter(0, "owner"))
            throw Context.SendCorrectUsage("/structure <set|s> <group|owner> <value>");

        BarricadeDrop? barricade = null;
        ItemAsset asset;
        if (Context.TryGetStructureTarget(out StructureDrop? structure))
        {
            asset = structure.asset;
        }
        else if (Context.TryGetBarricadeTarget(out barricade))
        {
            asset = barricade.asset;
        }
        else throw Context.Reply(_translations.StructureNoTarget);

        await UniTask.SwitchToMainThread(token);
        if (!Context.TryGet(1, out CSteamID ownerOrGroupId) || ownerOrGroupId != CSteamID.Nil && !isSettingGroup && ownerOrGroupId.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
        {
            if (!Context.MatchParameter(1, "me"))
                throw Context.SendHelp();

            // self
            ownerOrGroupId = isSettingGroup ? Context.Player.UnturnedPlayer.quests.groupID : Context.CallerId;
        }

        string ownerOrGroupDisplay = ownerOrGroupId.m_SteamID.ToString(CultureInfo.InvariantCulture);

        CSteamID? group = isSettingGroup ? ownerOrGroupId : null;
        CSteamID? owner = isSettingGroup ? null : ownerOrGroupId;
        uint instanceId;
        if (structure != null)
        {
            instanceId = structure.instanceID;
            StructureUtility.SetOwnerOrGroup(structure, owner, group);
        }
        else if (barricade != null)
        {
            instanceId = barricade.instanceID;
            BarricadeUtility.SetOwnerOrGroup(barricade, Context.ServiceProvider, owner, group);
        }
        else
            throw Context.Reply(_translations.StructureNoTarget);

        bool isSaved = await _saver.IsBuildableSavedAsync(instanceId, structure != null, token);
        if (isSaved)
        {
            if (structure != null)
                await _saver.SaveStructureAsync(structure, token);
            else
                await _saver.SaveBarricadeAsync(barricade!, token);

            await UniTask.SwitchToMainThread(token);
            Context.LogAction(ActionLogType.SetSavedStructureProperty, $"{asset?.itemName ?? "null"} / {asset?.id ?? 0} / {asset?.GUID ?? Guid.Empty:N} - " +
                                                                       $"SET {(isSettingGroup ? "GROUP" : "OWNER")} >> {ownerOrGroupDisplay}.");
        }

        if (isSettingGroup)
        {
            FactionInfo? info = _teamManager.GetTeam(ownerOrGroupId).Faction;
            if (info != null)
            {
                ownerOrGroupDisplay = TranslationFormattingUtility.Colorize(info.GetName(Context.Language), info.Color, Context.IMGUI);
            }
        }

        Context.Reply(_translations.StructureSaveSetProperty!, isSettingGroup ? "Group" : "Owner", asset, ownerOrGroupDisplay);
    }
}