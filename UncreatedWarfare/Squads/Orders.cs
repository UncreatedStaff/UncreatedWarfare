using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Point;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Squads;

public static class Orders
{
    public static List<Order> orders = new List<Order>(16);

    public static Order GiveOrder(Squad squad, UCPlayer commander, EOrder type, Vector3 marker, Translation message, object[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Order order = squad.Leader.Player.gameObject.AddComponent<Order>();
        order.Initialize(squad, commander, type, marker, message, formatting: formatting);
        orders.Add(order);

        commander.SendChat(T.OrderSent, squad, order);
        foreach (LanguageSet set in LanguageSet.InSquad(squad))
        {
            string msg = T.OrderReceived.Translate(set.Language, commander, order, team: squad.Team);
            while (set.MoveNext())
            {
                order.SendUI(set.Next);
                ToastMessage.QueueMessage(set.Next, new ToastMessage(msg, EToastMessageSeverity.MEDIUM));
            }
        }

        commander.Player.quests.sendSetMarker(false, marker);

        return order;
    }
    public static bool HasOrder(Squad? squad, out Order order)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        order = null!;
        if (squad == null) return false;
        return squad.Leader.Player.TryGetComponent(out order);
    }
    public static bool CancelOrder(Order order)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool success = orders.Remove(order);
        order.Cancel();
        return success;
    }
    public static void OnFOBBunkerBuilt(FOB fob, BuildableComponent buildable)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (KeyValuePair<ulong, float> pair in buildable.PlayerHits)
        {
            UCPlayer? player = UCPlayer.FromID(pair.Key);
            if (player != null &&
                pair.Value / buildable.Buildable.RequiredHits >= 0.1f &&
                HasOrder(player.Squad, out Order order) &&
                order.Type == EOrder.BUILDFOB &&
                (fob.Position - order.Marker).sqrMagnitude <= Math.Pow(80, 2)
            )
            {
                order.Fulfill();
            }
        }
    }
}

public class Order : MonoBehaviour, ITranslationArgument
{
    public UCPlayer Commander { get; private set; }
    public Squad Squad { get; private set; }
    public EOrder Type { get; private set; }
    public Vector3 Marker { get; private set; }
    public Translation Message { get; private set; }
    public object[]? Formatting { get; private set; }
    public int TimeLeft { get; private set; }
    public string MinutesLeft => Mathf.CeilToInt(TimeLeft / 60f).ToString(Data.LocalLocale);
    public string RewardLevel { get; private set; }
    public int RewardXP { get; private set; }
    public int RewardTW { get; private set; }
    public bool IsActive { get; private set; }
    public Flag? Flag { get; private set; }

    private OrderCondition _condition;

    private Coroutine _loop;

    public void Initialize(Squad squad, UCPlayer commander, EOrder type, Vector3 marker, Translation message, Flag? flag = null, object[]? formatting = null)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Squad = squad;
        Commander = commander;
        Type = type;
        Marker = marker;
        Message = message;
        Formatting = formatting;
        Flag = flag;

        switch (Type)
        {
            case EOrder.ATTACK:
                TimeLeft = 300;
                RewardXP = 0;
                RewardTW = 0;
                break;
            case EOrder.DEFEND:
                TimeLeft = 420;
                RewardXP = 0;
                RewardTW = 0;
                break;
            case EOrder.BUILDFOB:
                TimeLeft = 420;
                RewardXP = 150;
                RewardTW = 100;
                break;
            case EOrder.MOVE:
                TimeLeft = 240;
                RewardXP = 150;
                RewardTW = 100;

                Vector3 avgMemberPoint = Vector3.zero;
                foreach (UCPlayer player in Squad.Members)
                    avgMemberPoint += player.Position;

                avgMemberPoint /= squad.Members.Count;
                float distanceToMarker = (avgMemberPoint - Marker).magnitude;

                L.Log("distance to marker: " + distanceToMarker);

                if (distanceToMarker < 100) { RewardXP = 0; RewardTW = 0; }
                if (distanceToMarker >= 100 && distanceToMarker < 200) { RewardXP = 15; RewardTW = 15; }
                if (distanceToMarker >= 200 && distanceToMarker < 400) { RewardXP = 50; RewardTW = 50; }
                if (distanceToMarker >= 600 && distanceToMarker < 1000) { RewardXP = 70; RewardTW = 70; }
                if (distanceToMarker >= 1000) { RewardXP = 90; RewardTW = 90; }

                break;
        }

        RewardLevel = RewardTW switch
        {
            < 50 => "Low".Colorize("999999"),
            >= 50 and < 90 => "Medium".Colorize("e0b4a2"),
            >= 90 and < 120 => "High".Colorize("f5dfa6"),
            >= 120 => "Very High".Colorize("ffe4b3")
        };

        _condition = new OrderCondition(type, squad, marker);
        IsActive = true;

        _loop = StartCoroutine(Tick());

    }
    public void Fulfill()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!IsActive) return;

        switch (Type)
        {
            case EOrder.ATTACK:
                break;
            case EOrder.DEFEND:
                break;
            case EOrder.BUILDFOB:
                foreach (UCPlayer player in Squad.Members)
                {
                    ActionLogger.Add(EActionLogType.FUFILLED_ORDER, "BUILD FOB AT " + Marker.ToString("N2"), player);
                    GiveReward(player);
                    HideUI(player);
                }
                break;
            case EOrder.MOVE:
                foreach (UCPlayer player in _condition.FullfilledPlayers)
                {
                    if (player.IsOnline)
                    {
                        ActionLogger.Add(EActionLogType.FUFILLED_ORDER, "MOVE TO " + Marker.ToString("N2"), player);
                        GiveReward(player);
                        HideUI(player);
                    }
                }

                break;
        }

        if (Commander.IsOnline)
        {
            GiveReward(Commander);
        }

        IsActive = false;
        StartCoroutine(Delete());
    }
    private void GiveReward(UCPlayer player)
    {
        // TODO: colorize toast message
        Points.AwardXP(player, RewardXP, "ORDER FULFILLED".Colorize("a6f5b8"));
    }

    public void Cancel()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!IsActive) return;

        ToastMessage toast = new ToastMessage("ORDER CANCELLED".Colorize("c7b3a5"), EToastMessageSeverity.MINI);

        foreach (UCPlayer player in Squad.Members)
        {
            HideUI(player);
            ToastMessage.QueueMessage(player, toast);
        }

        ToastMessage.QueueMessage(Commander, toast);

        IsActive = false;
        Destroy(this);
    }
    public void TimeOut()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!IsActive) return;

        ToastMessage toast = new ToastMessage("ORDER TIMED OUT".Colorize("c7b3a5"), EToastMessageSeverity.MINI);

        foreach (UCPlayer player in Squad.Members)
        {
            HideUI(player);
            ToastMessage.QueueMessage(player, toast);
        }

        ToastMessage.QueueMessage(Commander, toast);

        IsActive = false;
        Destroy(this);
    }
    public void SendUI(UCPlayer player)
    {
        SquadManager.OrderUI.SendToPlayer(player.Connection);
        UpdateUI(player);
    }
    public void UpdateUI(UCPlayer player)
    {
        SquadManager.OrderUI.SetOrder(player, this);
    }
    public void HideUI(UCPlayer player)
    {
        SquadManager.OrderUI.ClearFromPlayer(player.Connection);
    }

    public IEnumerator<WaitForSeconds> Tick()
    {
        int counter = 0;
        float tickFrequency = 1;

        while (true)
        {
#if DEBUG
            IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            // every 1 second

            TimeLeft--;

            if (counter % (5 / tickFrequency) == 0) // every 5 seconds
            {
                if (Type == EOrder.MOVE)
                {
                    _condition.UpdateData();
                    if (_condition.Check())
                        Fulfill();
                    yield break;
                }
            }
            if (counter % (60 / tickFrequency) == 0) // every 60 seconds
            {
                foreach (UCPlayer player in Squad.Members)
                    UpdateUI(player);
            }


            if (TimeLeft <= 0)
            {
                TimeOut();
            }

            counter++;
            if (counter >= 60 / tickFrequency)
                counter = 0;
#if DEBUG
            profiler.Dispose();
#endif
            yield return new WaitForSeconds(tickFrequency);
        }
    }
    public IEnumerator<WaitForSeconds> Delete()
    {
        yield return new WaitForSeconds(20);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        // TODO: Clear UI
        StopCoroutine(_loop);
        Destroy(this);
    }

    [FormatDisplay("Message")]
    public const string FormatMessage = "m";
    [FormatDisplay("Type (" + nameof(EOrder) + ")")]
    public const string FormatType = "t";
    public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is null || format.Equals(FormatMessage, StringComparison.Ordinal))
            goto end;
        if (format.Equals(FormatType, StringComparison.Ordinal))
            return Localization.TranslateEnum(Type, language);
        end:
        if (Formatting is null || Formatting.Length == 0)
            return Message.Translate(language);
        return Message.TranslateUnsafe(language, Formatting, target, targetTeam: Squad.Team);
    }
}


public struct OrderCondition
{
    public readonly EOrder Type;
    public readonly Squad Squad;
    public readonly Vector3 Marker;
    public List<UCPlayer> FullfilledPlayers;


    public OrderCondition(EOrder type, Squad squad, Vector3 marker)
    {
        Type = type;
        Squad = squad;
        Marker = marker;
        FullfilledPlayers = new List<UCPlayer>(12);
    }
    public bool Check()
    {
        if (Type == EOrder.MOVE)
        {
            return FullfilledPlayers.Count >= 0.75F * Squad.Members.Count;
        }
        return false;
    }
    public void UpdateData()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Type == EOrder.MOVE)
        {
            foreach (UCPlayer player in Squad.Members)
            {
                if ((player.Position - Marker).sqrMagnitude <= Math.Pow(40, 2))
                {
                    if (!FullfilledPlayers.Contains(player))
                        FullfilledPlayers.Add(player);
                }
            }
        }
    }
}
[Translatable("Order Type")]
public enum EOrder
{
    ATTACK,
    DEFEND,
    [Translatable("Build FOB")]
    BUILDFOB,
    MOVE
}
