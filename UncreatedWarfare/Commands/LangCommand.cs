using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uncreated.Warfare.Commands
{
    public delegate void PlayerChangedLanguageDelegate(UnturnedPlayer player, LanguageAliasSet oldLanguage, LanguageAliasSet newLanguage);
    public class LangCommand : IRocketCommand
    {
        public static event PlayerChangedLanguageDelegate OnPlayerChangedLanguage;
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "lang";
        public string Help => "Switch your language to some of our supported languages.";
        public string Syntax => "/lang";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.lang" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            UnturnedPlayer player = (UnturnedPlayer)caller;
            string op = command.Length > 0 ? command[0].ToLower() : string.Empty;
            if (command.Length == 0)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Data.LanguageAliases.Keys.Count; i++)
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
                player.SendChat("language_list", sb.ToString());
            }
            else if (command.Length == 1)
            {
                if (op == "current")
                {
                    string OldLanguage = JSONMethods.DEFAULT_LANGUAGE;
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                        OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                    LanguageAliasSet oldSet;
                    if (Data.LanguageAliases.ContainsKey(OldLanguage))
                        oldSet = Data.LanguageAliases[OldLanguage];
                    else
                        oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new string[0]);

                    player.SendChat("language_current", $"{oldSet.display_name} : {oldSet.key}");
                }
                else if (op == "reset")
                {
                    string fullname = JSONMethods.DEFAULT_LANGUAGE;
                    LanguageAliasSet alias;
                    if (Data.LanguageAliases.ContainsKey(JSONMethods.DEFAULT_LANGUAGE))
                    {
                        alias = Data.LanguageAliases[JSONMethods.DEFAULT_LANGUAGE];
                        fullname = alias.display_name;
                    }
                    else
                        alias = new LanguageAliasSet(fullname, fullname, new string[0]);
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                    {
                        string OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                        LanguageAliasSet oldSet;
                        if (Data.LanguageAliases.ContainsKey(OldLanguage))
                            oldSet = Data.LanguageAliases[OldLanguage];
                        else
                            oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new string[0]);
                        if (OldLanguage == JSONMethods.DEFAULT_LANGUAGE)
                            player.SendChat("reset_language_not_needed", fullname);
                        else
                        {
                            JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, JSONMethods.DEFAULT_LANGUAGE);
                            if (OnPlayerChangedLanguage != null)
                                OnPlayerChangedLanguage.Invoke(player, oldSet, alias);
                            player.SendChat("reset_language", fullname);
                        }
                    }
                    else
                        player.SendChat("reset_language_not_needed", fullname);
                }
                else
                {
                    string OldLanguage = JSONMethods.DEFAULT_LANGUAGE;
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                        OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                    LanguageAliasSet oldSet;
                    if (Data.LanguageAliases.ContainsKey(OldLanguage))
                        oldSet = Data.LanguageAliases[OldLanguage];
                    else
                        oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new string[0]);
                    string langInput = op.Trim();
                    bool found = false;
                    if (!Data.LanguageAliases.TryGetValue(langInput, out LanguageAliasSet aliases))
                    {
                        IEnumerator<LanguageAliasSet> sets = Data.LanguageAliases.Values.GetEnumerator();
                        found = sets.MoveNext();
                        while (found)
                        {
                            if (sets.Current.key == langInput || sets.Current.values.Contains(langInput))
                            {
                                aliases = sets.Current;
                                break;
                            }
                            found = sets.MoveNext();
                        }
                        sets.Dispose();
                    }
                    else found = true;

                    if (found && aliases.key != null && aliases.values != null && aliases.display_name != null)
                    {
                        if (OldLanguage == aliases.key)
                            player.SendChat("change_language_not_needed", aliases.display_name);
                        else
                        {
                            JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, aliases.key);
                            if (OnPlayerChangedLanguage != null)
                                OnPlayerChangedLanguage.Invoke(player, oldSet, aliases);
                            player.SendChat("changed_language", aliases.display_name);
                        }
                    }
                    else
                    {
                        player.SendChat("dont_have_language", langInput);
                    }
                }
            }
            else
            {
                player.SendChat("reset_language_how");
            }
        }
    }
}