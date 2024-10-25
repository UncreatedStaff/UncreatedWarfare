using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("giveaccess", "givea", "ga"), SubCommandOf(typeof(KitCommand))]
internal class KitGiveAccessCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly ChatService _chatService;
    private readonly IUserDataService _userDataService;
    public CommandContext Context { get; set; }

    public KitGiveAccessCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _userDataService = serviceProvider.GetRequiredService<IUserDataService>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out CSteamID steam64, out WarfarePlayer? onlinePlayer) || !Context.TryGet(1, out string? kitName))
        {
            if (Context.HasArgs(2))
                throw Context.SendPlayerNotFound();

            throw Context.SendHelp();
        }

        if (!Context.TryGet(2, out KitAccessType accessType) || accessType == KitAccessType.Unknown)
        {
            accessType = KitAccessType.Purchase;
        }

        Kit? kit = await _kitManager.FindKit(kitName, token, true);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        bool hasAccess = await _kitManager.HasAccess(kit, steam64, token).ConfigureAwait(false);

        PlayerNames playerName = onlinePlayer?.Names ?? await _userDataService.GetUsernamesAsync(steam64.m_SteamID, token).ConfigureAwait(false);
        IPlayer player = (IPlayer?)onlinePlayer ?? playerName;

        if (hasAccess)
        {
            throw Context.Reply(_translations.KitAlreadyHasAccess, player, kit);
        }

        if (!await _kitManager.GiveAccess(kit, steam64, accessType, token).ConfigureAwait(false))
        {
            throw Context.Reply(_translations.KitAlreadyHasAccess, player, kit);
        }

        // todo KitSync.OnAccessChanged(steam64.m_SteamID);

        await UniTask.SwitchToMainThread(token);

        Context.LogAction(ActionLogType.ChangeKitAccess, steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) + " GIVEN ACCESS TO " + kitName + ", REASON: " + accessType);

        Context.Reply(_translations.KitAccessGiven, player, player, kit);

        if (onlinePlayer != null)
        {
            _chatService.Send(onlinePlayer, _translations.KitAccessGivenDm, kit);
            _kitManager.Signs.UpdateSigns(kit, onlinePlayer);
        }
    }
}