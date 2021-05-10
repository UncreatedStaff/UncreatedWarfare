using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Commands
{
    public class LangCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "lang";
        public string Help => "Switch your language to some of our supported languages.";
        public string Syntax => "/lang";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.lang" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if(command.Length == 0)
            {
                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < UCWarfare.I.LanguageAliases.Keys.Count; i++)
                {
                    if (i != 0) sb.Append(", ");
                    string langInput = UCWarfare.I.Localization.Keys.ElementAt(i);
                    sb.Append(langInput);
                    LanguageAliasSet aliases;
                    if (UCWarfare.I.LanguageAliases.ContainsKey(langInput))
                        aliases = UCWarfare.I.LanguageAliases[langInput];
                    else
                        aliases = UCWarfare.I.LanguageAliases.Values.FirstOrDefault(x => x.values.Contains(langInput));
                    if (!aliases.Equals(default(LanguageAliasSet))) sb.Append(" : ").Append(aliases.display_name);
                }
                player.SendChat("language_list", UCWarfare.I.Colors["language_list"], sb.ToString(), UCWarfare.I.ColorsHex["language_list_list"]);
            } else if (command.Length == 1)
            {
                if(command[0].ToLower() == "reset")
                {
                    string fullname = JSONMethods.DefaultLanguage;
                    if (UCWarfare.I.LanguageAliases.ContainsKey(JSONMethods.DefaultLanguage))
                        fullname = UCWarfare.I.LanguageAliases[JSONMethods.DefaultLanguage].display_name;
                    JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, JSONMethods.DefaultLanguage);
                    player.SendChat("reset_language", UCWarfare.I.Colors["reset_language"], fullname, UCWarfare.I.ColorsHex["reset_language_language"]);
                } else
                {
                    string langInput = command[0].ToLower().Trim();
                    LanguageAliasSet aliases;
                    if (UCWarfare.I.LanguageAliases.ContainsKey(langInput))
                        aliases = UCWarfare.I.LanguageAliases[langInput];
                    else
                        aliases = UCWarfare.I.LanguageAliases.Values.FirstOrDefault(x => x.values.Contains(langInput));
                    if (!aliases.Equals(default(LanguageAliasSet)))
                    {
                        JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, aliases.key);
                        player.SendChat("changed_language", UCWarfare.I.Colors["changed_language"], aliases.display_name, UCWarfare.I.ColorsHex["changed_language_language"]);
                    }
                    else
                    {
                        player.SendChat("dont_have_language", UCWarfare.I.Colors["dont_have_language"], langInput, UCWarfare.I.ColorsHex["dont_have_language_language"]);
                    }
                }
            } else
            {
                player.SendChat("reset_language_how", UCWarfare.I.Colors["reset_language_how"], UCWarfare.I.ColorsHex["reset_language_how_command"]);
            }
        }
    }
}