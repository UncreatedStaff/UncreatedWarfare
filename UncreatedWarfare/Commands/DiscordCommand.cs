﻿using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace Uncreated.Warfare.Commands
{
    public class DiscordCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "discord";
        public string Help => "Links you to the Uncreated Discord server.";
        public string Syntax => "/discord";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.discord" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (player == null) return;
            player.Player.channel.owner.SendURL("Join our Discord Server", "https://discord.gg/" + UCWarfare.Config.DiscordInviteCode);
        }
    }
}
