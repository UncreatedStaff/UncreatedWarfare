using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;

namespace Uncreated.Warfare.FOBs.UI;
public class FOBListUI : UnturnedUI
{
    public readonly FOBListElement[] FOBs = ElementPatterns.CreateArray<FOBListElement>("Canvas/{0}", 0, to: 9);
    public FOBListUI() : base(Gamemodes.Gamemode.Config.UIFOBList.GetId()) { }
    public void Hide(UCPlayer player)
    {
        if (!player.HasFOBUI)
            return;

        player.HasFOBUI = false;
        ClearFromPlayer(player.Connection);
    }
    public void Update(FOBManager manager, ulong team = 0, bool resourcesOnly = false)
    {
        if (team is 0ul or 1ul)
        {
            UpdateFor(LanguageSet.OnTeam(1ul), manager.Team1ListEntries, -1, resourcesOnly);
        }
        if (team is 0ul or 2ul)
        {
            UpdateFor(LanguageSet.OnTeam(2ul), manager.Team2ListEntries, -1, resourcesOnly);
        }
        if (team is not 1ul and not 2ul)
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.Where(x => x.GetTeam() == 3ul && x.HasFOBUI))
            {
                player.HasFOBUI = false;
                ClearFromPlayer(player.Connection);
            }
        }
    }
    public void UpdatePast(FOBManager manager, int startIndex, ulong team = 0, bool resourcesOnly = false)
    {
        if (team is 0ul or 1ul)
        {
            UpdatePastFor(LanguageSet.OnTeam(1ul), manager.Team1ListEntries, startIndex);
        }
        if (team is 0ul or 2ul)
        {
            UpdatePastFor(LanguageSet.OnTeam(2ul), manager.Team2ListEntries, startIndex);
        }
        if (team is not 1ul and not 2ul)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer player = PlayerManager.OnlinePlayers[i];
                if (player.GetTeam() == 3ul && player.HasFOBUI)
                {
                    player.HasFOBUI = false;
                    ClearFromPlayer(player.Connection);
                }
            }
        }
    }
    public void UpdatePastFor(LanguageSet.LanguageSetEnumerator set, IReadOnlyList<IFOB?> listEntries, int startIndex)
    {
        foreach (LanguageSet lang in set)
        {
            UpdateFor(lang, listEntries, -1, false, startIndex);
        }
    }
    public void UpdateFor(LanguageSet.LanguageSetEnumerator set, IReadOnlyList<IFOB?> listEntries, int index = -1, bool resourcesOnly = false)
    {
        foreach (LanguageSet lang in set)
        {
            UpdateFor(lang, listEntries, index, resourcesOnly);
        }
    }
    public void UpdateFor(FOBManager manager, UCPlayer player, int index = -1, bool resourcesOnly = false, int startIndex = 0)
    {
        ulong team = player.GetTeam();
        if (team is 1ul or 2ul && !player.HasUIHidden)
            UpdateFor(new LanguageSet(player), team == 1ul ? manager.Team1ListEntries : manager.Team2ListEntries, index, resourcesOnly, startIndex);
        else if (player.HasFOBUI)
        {
            player.HasFOBUI = false;
            ClearFromPlayer(player.Connection);
        }
    }
    public void UpdateFor(LanguageSet set, IReadOnlyList<IFOB?> listEntries, int index = -1, bool resourcesOnly = false, int startIndex = 0)
    {
        int count = Math.Min(listEntries.Count, FOBs.Length);
        if (index != -1 && index < count)
        {
            FOBListElement element = FOBs[index];
            IFOB? fob = listEntries[index];
            if (fob == null)
            {
                if (resourcesOnly)
                    return;
                while (set.MoveNext())
                {
                    if (set.Next.HasFOBUI)
                        element.Root.SetVisibility(set.Next.Connection, false);
                }
                return;
            }
            UpdateOneFOB(ref set, in element, fob, resourcesOnly);
            return;
        }
        bool isClearing = false;
        for (int i = startIndex; i < count; ++i)
        {
            FOBListElement element = FOBs[i];
            IFOB? fob = listEntries[i];
            if (!isClearing)
                isClearing = fob == null;
            if (isClearing)
            {
                if (resourcesOnly)
                    break;
                while (set.MoveNext())
                {
                    if (set.Next.HasFOBUI)
                        element.Root.SetVisibility(set.Next.Connection, false);
                }
                set.Reset();
                continue;
            }

            UpdateOneFOB(ref set, in element, fob!, resourcesOnly);
        }
    }

    private void UpdateOneFOB(ref LanguageSet set, in FOBListElement element, IFOB fob, bool resourcesOnly = false)
    {
        IResourceFOB? rscFob = fob as IResourceFOB;
        if (resourcesOnly)
        {
            if (rscFob == null)
                return;
            string txt = rscFob.UIResourceString;
            while (set.MoveNext())
            {
                if (!set.Next.HasFOBUI)
                {
                    if (set.Next.HasUIHidden)
                        continue;
                    SendToPlayer(set.Next.Connection);
                    set.Next.HasFOBUI = true;
                    element.Root.SetVisibility(set.Next.Connection, true);
                    element.Name.SetText(set.Next.Connection, GetFOBUIText(in set, fob));
                }
                element.Resources.SetText(set.Next.Connection, txt);
            }
            set.Reset();
            return;
        }

        string name = GetFOBUIText(in set, fob);
        string resources = rscFob?.UIResourceString ?? string.Empty;
        while (set.MoveNext())
        {
            ITransportConnection connection = set.Next.Connection;
            if (!set.Next.HasFOBUI)
            {
                if (set.Next.HasUIHidden)
                    continue;
                SendToPlayer(connection);
                set.Next.HasFOBUI = true;
            }
            element.Root.SetVisibility(connection, true);
            element.Resources.SetText(connection, resources);
            element.Name.SetText(connection, name);
        }
        set.Reset();
    }
    public class FOBListElement
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("N{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("R{0}")]
        public UnturnedLabel Resources { get; set; }
    }
    private static string GetFOBUIText(in LanguageSet set, IFOB fob) => T.FOBUI.Translate(set.Language, fob, fob.GridLocation, fob.ClosestLocation, team: set.Team, target: set.Players.Count == 1 ? set.Players[0] : null);
}