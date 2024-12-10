using Uncreated.Warfare.Components;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("examine", "exam", "wtf"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureExamineCommand : IExecutableCommand
{
    private readonly ITeamManager<Team> _teamManager;
    private readonly IUserDataService _userDataService;
    private readonly StructureTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureExamineCommand(TranslationInjection<StructureTranslations> translations, ITeamManager<Team> teamManager, IUserDataService userDataService)
    {
        _teamManager = teamManager;
        _userDataService = userDataService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            await ExamineVehicle(vehicle, Context.Player, true, token).ConfigureAwait(false);
        }
        else if (Context.TryGetStructureTarget(out StructureDrop? structure))
        {
            await ExamineStructure(structure, Context.Player, true, token).ConfigureAwait(false);
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            await ExamineBarricade(barricade, Context.Player, true, token).ConfigureAwait(false);
        }
        else throw Context.Reply(_translations.StructureExamineNotExaminable);

        Context.Defer();
    }

    private async Task ExamineVehicle(InteractableVehicle vehicle, WarfarePlayer player, bool sendurl, CancellationToken token = default)
    {
        GameThread.AssertCurrent();
        if (vehicle.lockedOwner.m_SteamID == 0)
        {
            Context.Reply(_translations.StructureExamineNotLocked);
        }
        else
        {
            Team team = _teamManager.GetTeam(vehicle.lockedGroup);
            ulong prevOwner = vehicle.transform.TryGetComponent(out VehicleComponent vcomp) ? vcomp.PreviousOwner : 0ul;
            IPlayer names = await _userDataService.GetUsernamesAsync(vehicle.lockedOwner.m_SteamID, token).ConfigureAwait(false);
            string prevOwnerName;
            if (prevOwner != 0ul)
            {
                PlayerNames pl = await _userDataService.GetUsernamesAsync(prevOwner, token).ConfigureAwait(false);
                prevOwnerName = pl.GetDisplayNameOrPlayerName();
            }
            else prevOwnerName = "None";
            await UniTask.SwitchToMainThread(token);
            if (sendurl)
            {
                Context.ReplySteamProfileUrl(_translations.VehicleExamineLastOwnerPrompt
                        .Translate(vehicle.asset, names, team.Faction, prevOwnerName, prevOwner, player, canUseIMGUI: true), vehicle.lockedOwner);
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(vehicle.lockedOwner);
                await pl.CacheUsernames(_userDataService, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(_translations.VehicleExamineLastOwnerChat, vehicle.asset, names, pl, team.Faction, prevOwnerName, prevOwner);
            }
        }
    }

    private async Task ExamineBarricade(BarricadeDrop bdrop, WarfarePlayer player, bool sendurl, CancellationToken token = default)
    {
        GameThread.AssertCurrent();
        if (bdrop != null)
        {
            BarricadeData data = bdrop.GetServersideData();
            if (data.owner == 0)
            {
                Context.Reply(_translations.StructureExamineNotExaminable);
                return;
            }

            Team team = _teamManager.GetTeam(new CSteamID(data.group));
            IPlayer names = await _userDataService.GetUsernamesAsync(data.owner, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (sendurl)
            {
                Context.ReplySteamProfileUrl(_translations.StructureExamineLastOwnerPrompt.Translate(data.barricade.asset, names, team.Faction, player, canUseIMGUI: true),
                    new CSteamID(data.owner));
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(new CSteamID(data.owner));
                await pl.CacheUsernames(_userDataService, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(_translations.StructureExamineLastOwnerChat, data.barricade.asset, names, pl, team.Faction);
            }
        }
        else
        {
            Context.Reply(_translations.StructureExamineNotExaminable);
        }
    }

    private async Task ExamineStructure(StructureDrop sdrop, WarfarePlayer player, bool sendurl, CancellationToken token = default)
    {
        GameThread.AssertCurrent();
        if (sdrop != null)
        {
            StructureData data = sdrop.GetServersideData();
            if (data.owner == default)
            {
                Context.Reply(_translations.StructureExamineNotExaminable);
                return;
            }
            Team team = _teamManager.GetTeam(new CSteamID(data.group));
            IPlayer names = await _userDataService.GetUsernamesAsync(data.owner, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (sendurl)
            {
                Context.ReplySteamProfileUrl(_translations.StructureExamineLastOwnerPrompt.Translate(data.structure.asset, names, team.Faction, player, canUseIMGUI: true), new CSteamID(data.owner));
            }
            else
            {
                OfflinePlayer pl = new OfflinePlayer(new CSteamID(data.owner));
                await pl.CacheUsernames(_userDataService, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(_translations.StructureExamineLastOwnerChat, data.structure.asset, names, pl, team.Faction);
            }
        }
        else
        {
            Context.Reply(_translations.StructureExamineNotExaminable);
        }
    }
}