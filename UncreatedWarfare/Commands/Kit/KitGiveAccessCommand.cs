using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("giveaccess", "givea", "ga"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitGiveAccessCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitAccessService _kitAccessService;
    private readonly IKitDataStore _kitDataStore;
    private readonly ChatService _chatService;
    private readonly IUserDataService _userDataService;

    public required CommandContext Context { get; init; }

    public KitGiveAccessCommand(IServiceProvider serviceProvider)
    {
        _kitAccessService = serviceProvider.GetRequiredService<IKitAccessService>();
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _userDataService = serviceProvider.GetRequiredService<IUserDataService>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        (CSteamID? steam64, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0).ConfigureAwait(false);

        if (!steam64.HasValue || !Context.TryGet(1, out string? kitName))
        {
            if (Context.HasArgs(2))
                throw Context.SendPlayerNotFound();

            throw Context.SendHelp();
        }

        if (!Context.TryGet(2, out KitAccessType accessType) || accessType == KitAccessType.Unknown)
        {
            accessType = KitAccessType.Purchase;
        }

        Kit? kit = await _kitDataStore.QueryKitAsync(kitName, KitInclude.Base, token).ConfigureAwait(false);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        bool hasAccess = await _kitAccessService.HasAccessAsync(steam64.Value, kit.Key, token).ConfigureAwait(false);

        PlayerNames playerName = onlinePlayer?.Names ?? await _userDataService.GetUsernamesAsync(steam64.Value.m_SteamID, token).ConfigureAwait(false);
        IPlayer player = (IPlayer?)onlinePlayer ?? playerName;

        if (hasAccess)
        {
            throw Context.Reply(_translations.KitAlreadyHasAccess, player, kit);
        }

        if (!await _kitAccessService.UpdateAccessAsync(steam64.Value, kit.Key, accessType, token).ConfigureAwait(false))
        {
            throw Context.Reply(_translations.KitAlreadyHasAccess, player, kit);
        }

        await UniTask.SwitchToMainThread(token);

        Context.LogAction(ActionLogType.ChangeKitAccess, steam64.Value.m_SteamID.ToString(CultureInfo.InvariantCulture) + " GIVEN ACCESS TO " + kitName + ", REASON: " + accessType);

        Context.Reply(_translations.KitAccessGiven, player, player, kit);

        if (onlinePlayer != null)
        {
            _chatService.Send(onlinePlayer, _translations.KitAccessGivenDm, kit);
        }
    }
}