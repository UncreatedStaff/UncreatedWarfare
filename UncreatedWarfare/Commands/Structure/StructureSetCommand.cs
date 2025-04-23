using System.Globalization;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("set", "s"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureSetCommand : IExecutableCommand
{
    private readonly ITeamManager<Team> _teamManager;
    private readonly StructureTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureSetCommand(ITeamManager<Team> teamManager, TranslationInjection<StructureTranslations> translations)
    {
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

        CSteamID? ownerOrGroupId = null;

        if (Context.MatchParameter(1, "0"))
        {
            ownerOrGroupId = CSteamID.Nil;
        }
        else if (Context.MatchParameter(1, "me"))
        {
            ownerOrGroupId = isSettingGroup ? Context.Player.UnturnedPlayer.quests.groupID : Context.CallerId;
        }
        else if (isSettingGroup && Context.TryGet(1, out ulong groupId))
        {
            ownerOrGroupId = new CSteamID(groupId);
        }
        else if (!isSettingGroup && !(ownerOrGroupId = await Context.TryGetSteamId(1).ConfigureAwait(false)).HasValue)
        {
            string? faction = Context.Get(1);
            Team? team = _teamManager.FindTeam(faction);
            ownerOrGroupId = team?.GroupId;
        }

        await UniTask.SwitchToMainThread(token);

        if (!ownerOrGroupId.HasValue)
        {
            if (!Context.MatchParameter(1, "me"))
                throw Context.SendHelp();

            // self
            ownerOrGroupId = isSettingGroup ? Context.Player.UnturnedPlayer.quests.groupID : Context.CallerId;
        }

        string ownerOrGroupDisplay = ownerOrGroupId.Value.m_SteamID.ToString(CultureInfo.InvariantCulture);

        CSteamID? group = isSettingGroup ? ownerOrGroupId : null;
        CSteamID? owner = isSettingGroup ? null : ownerOrGroupId;
        if (structure != null)
        {
            StructureUtility.SetOwnerOrGroup(structure, owner, group);
        }
        else if (barricade != null)
        {
            BarricadeUtility.SetOwnerOrGroup(barricade, Context.ServiceProvider, owner, group);
        }
        else
            throw Context.Reply(_translations.StructureNoTarget);

        if (isSettingGroup)
        {
            FactionInfo? info = _teamManager.GetTeam(ownerOrGroupId.Value).Faction;
            if (info != null)
            {
                ownerOrGroupDisplay = TranslationFormattingUtility.Colorize(info.GetName(Context.Language), info.Color, Context.IMGUI);
            }
        }

        Context.Reply(_translations.StructureSaveSetProperty!, isSettingGroup ? "Group" : "Owner", asset, ownerOrGroupDisplay);
    }
}