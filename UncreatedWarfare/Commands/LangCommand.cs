using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands
{
    public class PlayerChangedLanguageEventArgs : EventArgs { public UnturnedPlayer player; public LanguageAliasSet OldLanguage; public LanguageAliasSet NewLanguage; }
    public class LangCommand : IRocketCommand
    {
        public static event EventHandler<PlayerChangedLanguageEventArgs> OnPlayerChangedLanguage;
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
                for(int i = 0; i < Data.LanguageAliases.Keys.Count; i++)
                {
                    string langInput = Data.LanguageAliases.Keys.ElementAt(i);
                    if (!Data.Localization.ContainsKey(langInput)) continue; // only show languages with translations
                    if (i != 0) sb.Append(", ");
                    sb.Append(langInput);
                    LanguageAliasSet aliases;
                    if (Data.LanguageAliases.ContainsKey(langInput))
                        aliases = Data.LanguageAliases[langInput];
                    else
                        aliases = Data.LanguageAliases.Values.FirstOrDefault(x => x.values.Contains(langInput));
                    if (!aliases.Equals(default(LanguageAliasSet))) sb.Append(" : ").Append(aliases.display_name);
                }
                player.SendChat("language_list", UCWarfare.GetColor("language_list"), sb.ToString(), UCWarfare.GetColorHex("language_list_list"));
            } else if (command.Length == 1)
            {
                if(command[0].ToLower() == "current")
                {
                    string OldLanguage = JSONMethods.DefaultLanguage;
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                        OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                    LanguageAliasSet oldSet;
                    if (Data.LanguageAliases.ContainsKey(OldLanguage))
                        oldSet = Data.LanguageAliases[OldLanguage];
                    else
                        oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new List<string>());

                    player.SendChat("language_current", UCWarfare.GetColor("language_current"), $"{oldSet.display_name} : {oldSet.key}", UCWarfare.GetColorHex("language_current_language"));
                } else if(command[0].ToLower() == "reset")
                {
                    string fullname = JSONMethods.DefaultLanguage;
                    LanguageAliasSet alias;
                    if (Data.LanguageAliases.ContainsKey(JSONMethods.DefaultLanguage))
                    {
                        alias = Data.LanguageAliases[JSONMethods.DefaultLanguage];
                        fullname = alias.display_name;
                    } else
                        alias = new LanguageAliasSet(fullname, fullname, new List<string>());
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                    {
                        string OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                        LanguageAliasSet oldSet;
                        if (Data.LanguageAliases.ContainsKey(OldLanguage))
                            oldSet = Data.LanguageAliases[OldLanguage];
                        else
                            oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new List<string>());
                        if (OldLanguage == JSONMethods.DefaultLanguage)
                            player.SendChat("reset_language_not_needed", UCWarfare.GetColor("reset_language_not_needed"), fullname, UCWarfare.GetColorHex("reset_language_not_needed_language"));
                        else
                        {
                            JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, JSONMethods.DefaultLanguage);
                            OnPlayerChangedLanguage?.Invoke(this, new PlayerChangedLanguageEventArgs { player = player, NewLanguage = alias, OldLanguage = oldSet });
                            player.SendChat("reset_language", UCWarfare.GetColor("reset_language"), fullname, UCWarfare.GetColorHex("reset_language_language"));
                        }
                    } else
                        player.SendChat("reset_language_not_needed", UCWarfare.GetColor("reset_language_not_needed"), fullname, UCWarfare.GetColorHex("reset_language_not_needed_language"));
                } else
                {
                    string OldLanguage = JSONMethods.DefaultLanguage;
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                        OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                    LanguageAliasSet oldSet;
                    if (Data.LanguageAliases.ContainsKey(OldLanguage))
                        oldSet = Data.LanguageAliases[OldLanguage];
                    else
                        oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new List<string>());
                    string langInput = command[0].ToLower().Trim();
                    LanguageAliasSet aliases;
                    if (Data.LanguageAliases.ContainsKey(langInput))
                        aliases = Data.LanguageAliases[langInput];
                    else
                        aliases = Data.LanguageAliases.Values.FirstOrDefault(x => x.values.Contains(langInput));
                    if (!aliases.Equals(default))
                    {
                        if (OldLanguage == aliases.key)
                            player.SendChat("change_language_not_needed", UCWarfare.GetColor("change_language_not_needed"), aliases.display_name, UCWarfare.GetColorHex("change_language_not_needed_language"));
                        else
                        {
                            JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, aliases.key);
                            OnPlayerChangedLanguage?.Invoke(this, new PlayerChangedLanguageEventArgs { player = player, NewLanguage = aliases, OldLanguage = oldSet });
                            player.SendChat("changed_language", UCWarfare.GetColor("changed_language"), aliases.display_name, UCWarfare.GetColorHex("changed_language_language"));
                        }
                    }
                    else
                    {
                        player.SendChat("dont_have_language", UCWarfare.GetColor("dont_have_language"), langInput, UCWarfare.GetColorHex("dont_have_language_language"));
                    }
                }
            } else
            {
                player.SendChat("reset_language_how", UCWarfare.GetColor("reset_language_how"), UCWarfare.GetColorHex("reset_language_how_command"));
            }
        }
    }
}