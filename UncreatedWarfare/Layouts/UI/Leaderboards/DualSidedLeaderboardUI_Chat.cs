using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;

partial class DualSidedLeaderboardUI :
    IEventListener<PlayerChatRequested>,
    IEventListener<PlayerChatSent>
{
    private bool _trackChat;

    public readonly ChatEntry[] ChatEntries = ElementPatterns.CreateArray<ChatEntry>("ChatContainer/Viewport/Content/ChatMessage_{0}", 1, to: 32);

    public readonly StateTextBox ChatTextBox = new StateTextBox("Lb_ChatInput", "./InputFieldState") { UseData = true };

    // separate virtual button to listen for pressing [enter] on the chat box
    public readonly UnturnedButton ChatInputTextBoxSubmitListener = new UnturnedButton("Lb_ChatInput/Lb_ChatInput_CommitListener");

    public readonly StateButton SendChatButton = new StateButton("Lb_ChatInput/Lb_SendChat", "./ButtonState");

    public readonly UnturnedUIElement LogicScrollToBottom = new UnturnedUIElement("~/Logic_ChatScrollToBottom");

    public readonly UnturnedEnumButton<EChatMode> ChatModeToggle = new UnturnedEnumButton<EChatMode>(
        [ EChatMode.GLOBAL, EChatMode.GROUP ],
        EChatMode.GLOBAL,
        "Lb_ChatInput/Lb_ChatMode",
        "./Label",
        "./ButtonState",
        "./Lb_ChatMode_RightClickListener"
    )
    {
        // todo maybe add translations
        TextFormatter = (value, _) => value == EChatMode.GLOBAL ? "[World]" : "[Team]"
    };

    public void ScrollToBottomOfChat(ITransportConnection c)
    {
        LogicScrollToBottom.SetVisibility(c, true);
    }

    private void OpenChat()
    {
        foreach (WarfarePlayer onlinePlayer in _playerService.OnlinePlayers)
        {
            ChatModeToggle.Update(onlinePlayer.UnturnedPlayer);
            ChatTextBox.TextBox.SetText(onlinePlayer.UnturnedPlayer, string.Empty);
        }

        ChatInputTextBoxSubmitListener.OnClicked += ChatSubmitted;
        SendChatButton.OnClicked += ChatSubmitted;

        // effectively clear game chat
        Color color = Color.white;
        for (int i = 0; i < 16; ++i)
        {
            _chatService.Broadcast(string.Empty, color, EChatMode.SAY, null, false);
        }

        UpdateChat();
    }

    private void ChatSubmitted(UnturnedButton textbox, Player player)
    {
        string? text = ChatTextBox.TextBox.GetOrAddData(player).Text;

        if (text == null)
            return;

        ChatTextBox.SetText(player, string.Empty);
        if (!ChatModeToggle.TryGetSelection(player, out EChatMode mode))
            mode = EChatMode.GLOBAL;

        ChatManagerOnChatRequested.SimulateChatRequest(player.channel.owner, text, mode, fromUnityEvent: false);
        ScrollToBottomOfChat(player.channel.owner.transportConnection);
    }

    private void UpdateChat()
    {
        foreach (WarfarePlayer onlinePlayer in _playerService.OnlinePlayers)
        {
            UpdateChat(onlinePlayer);
        }
    }

    private void UpdateChat(WarfarePlayer player)
    {
        DualSidedLeaderboardPlayerData data = GetOrAddData(player.Steam64, _createData);
        int index = 0;
        ITransportConnection c = player.Connection;
        foreach (ChatMessageInfo info in data.VisibleChats)
        {
            ChatEntry ui = ChatEntries[data.VisibleChats.Count - index - 1];

            ++index;

            ui.Root.SetVisibility(c, true);
            if (!string.IsNullOrEmpty(info.Avatar))
            {
                ui.Avatar.SetImage(c, info.Avatar);
                ui.Avatar.SetVisibility(c, true);
            }
            else
            {
                ui.Avatar.SetVisibility(c, false);
            }

            ui.Message.SetText(c, info.Message!);
        }
    }

    private void ChatServiceOnOnSendingChatMessage(WarfarePlayer recipient, string text, Color color, EChatMode mode, string? iconUrl, bool richText, WarfarePlayer? fromPlayer, ref bool shouldReplicate)
    {
        if (!IsActive && !_trackChat || text.Length == 0)
            return;

        DualSidedLeaderboardPlayerData data = GetOrAddData(recipient.Steam64, _createData);
        data.VisibleChats.Add(new ChatMessageInfo(string.IsNullOrWhiteSpace(iconUrl) ? fromPlayer?.SteamSummary.AvatarUrlSmall : iconUrl, color == Color.white ? text : TranslationFormattingUtility.Colorize(text, color, imgui: false)));

        shouldReplicate = false;

        if (!IsActive)
            return;

        UpdateChat(recipient);
    }
    
    [EventListener(Priority = int.MaxValue, RequiresMainThread = true)]
    void IEventListener<PlayerChatSent>.HandleEvent(PlayerChatSent e, IServiceProvider serviceProvider)
    {
        if (!IsActive && !_trackChat)
            return;

        foreach (WarfarePlayer onlinePlayer in e.TargetPlayers(e.Request))
        {
            DualSidedLeaderboardPlayerData data = GetOrAddData(onlinePlayer.Steam64, _createData);
            data.VisibleChats.Add(new ChatMessageInfo(onlinePlayer, e));

            if (IsActive)
                UpdateChat(onlinePlayer);
        }
    }

    [EventListener(Priority = int.MinValue)]
    void IEventListener<PlayerChatRequested>.HandleEvent(PlayerChatRequested e, IServiceProvider serviceProvider)
    {
        if (!IsActive)
            return;

        e.ShouldReplicate = false;
    }

    public class ChatEntry
    {
        [Pattern(Root = true)]
        public required UnturnedUIElement Root { get; set; }

        [Pattern("Content", AdditionalPath = "ScaleWrapper")]
        public required UnturnedLabel Message { get; set; }
        
        [Pattern("Avatar")]
        public required UnturnedImage Avatar { get; set; }
    }

    public struct ChatMessageInfo
    {
        public string? Avatar;
        public string? Message;

        public ChatMessageInfo(string? avatar, string message)
        {
            Avatar = avatar;
            Message = message;
        }

        public ChatMessageInfo(WarfarePlayer player, PlayerChatSent chat)
        {
            string? avatar = chat.IconUrlOverride ?? chat.Player.SteamSummary.AvatarUrlSmall;

            if (!string.IsNullOrEmpty(avatar))
                Avatar = avatar;

            Message = chat.FormatHandler(player, /* imgui */ false);
        }
    }
}