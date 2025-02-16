using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("removeaccess", "removea", "ra"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitRemoveAccessCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitAccessService _kitAccessService;
    private readonly IKitDataStore _kitDataStore;
    private readonly ChatService _chatService;
    private readonly IUserDataService _userDataService;
    private readonly IPlayerService _playerService;

    public required CommandContext Context { get; init; }

    public KitRemoveAccessCommand(IServiceProvider serviceProvider)
    {
        _kitAccessService = serviceProvider.GetRequiredService<IKitAccessService>();
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _userDataService = serviceProvider.GetRequiredService<IUserDataService>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        (CSteamID? steam64, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(1).ConfigureAwait(false);

        if (!steam64.HasValue || !Context.TryGet(1, out string? kitName))
        {
            if (Context.HasArgs(2))
                throw Context.SendPlayerNotFound();

            throw Context.SendHelp();
        }

        Kit? kit = await _kitDataStore.QueryKitAsync(kitName, KitInclude.Default, token);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        bool hasAccess = await _kitAccessService.HasAccessAsync(steam64.Value, kit.Key, token).ConfigureAwait(false);

        IPlayer player = await _playerService.GetOfflinePlayer(steam64.Value, _userDataService, token).ConfigureAwait(false);

        if (!hasAccess)
        {
            throw Context.Reply(_translations.KitAlreadyMissingAccess, player, kit);
        }

        if (!await _kitAccessService.UpdateAccessAsync(steam64.Value, kit.Key, null, Context.CallerId, token).ConfigureAwait(false))
        {
            throw Context.Reply(_translations.KitAlreadyMissingAccess, player, kit);
        }

        await UniTask.SwitchToMainThread(token);
        
        Context.Reply(_translations.KitAccessRevoked, player, player, kit);

        if (onlinePlayer != null)
        {
            _chatService.Send(onlinePlayer, _translations.KitAccessRevokedDm, kit);
        }
    }
}