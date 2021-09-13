using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes
{
    public delegate Task TeamWinDelegate(ulong team);
    public abstract class Gamemode : MonoBehaviour
    {
        public readonly string Name;
        private float EventLoopSpeed;
        private bool useEventLoop;
        public event TeamWinDelegate OnTeamWin;
        public EState State;
        protected string shutdownMessage = string.Empty;
        protected bool shutdownAfterGame = false;
        protected ulong shutdownPlayer = 0;
        public Coroutine EventLoopCoroutine;
        public bool isPendingCancel;

        public long GameID;
        public Gamemode(string Name, float EventLoopSpeed)
        {
            this.Name = Name;
            this.EventLoopSpeed = EventLoopSpeed;
            this.useEventLoop = EventLoopSpeed > 0;
            this.State = EState.LOADING;
        }
        protected void SetTiming(float NewSpeed)
        {
            this.EventLoopSpeed = NewSpeed;
            this.useEventLoop = NewSpeed > 0;
        }
        public void Cancel()
        {
            isPendingCancel = true;
            if (EventLoopCoroutine == null)
                return;
            StopCoroutine(EventLoopCoroutine);
        }
        public virtual void Init()
        { }
        protected void InvokeOnTeamWin(ulong winner)
        {
            if (OnTeamWin != null)
                OnTeamWin.Invoke(winner);
        }
        protected abstract void EventLoopAction();
        private IEnumerator<WaitForSeconds> EventLoop()
        {
            while (!isPendingCancel)
            {
                yield return new WaitForSeconds(EventLoopSpeed);
                DateTime start = DateTime.Now;
                try
                {
                    EventLoopAction();
                }
                catch (Exception ex)
                {
                    F.LogError("Error in " + Name + " gamemode in the event loop:");
                    F.LogError(ex);
                }
                if (UCWarfare.I.CoroutineTiming)
                    F.Log(Name + " Eventloop: " + (DateTime.Now - start).TotalMilliseconds.ToString(Data.Locale) + "ms.");
            }
        }
        public void ShutdownAfterGame(string reason, ulong player)
        {
            shutdownAfterGame = true;
            shutdownMessage = reason;
            shutdownPlayer = player;
        }
        public void CancelShutdownAfterGame()
        {
            shutdownAfterGame = false;
            shutdownMessage = string.Empty;
            shutdownPlayer = 0;
        }
        public abstract void DeclareWin(ulong winner);
        public virtual void StartNextGame(bool onLoad = false)
        {
            GameID = DateTime.Now.Ticks;
            for (int i = 0; i < Provider.clients.Count; i++)
                if (PlayerManager.HasSave(Provider.clients[i].playerID.steamID.m_SteamID, out PlayerSave save)) save.LastGame = GameID;
            PlayerManager.ApplyToOnline();
        }
        public virtual void Dispose()
        {
            Cancel();
        }
        public virtual void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        { }
        public virtual void OnPlayerJoined(SteamPlayer player)
        { }
        public virtual void OnPlayerLeft(ulong player)
        { }
        public virtual void OnPlayerDeath(UCWarfare.DeathEventArgs args)
        { }
        public virtual void OnLevelLoaded()
        {
            if (useEventLoop)
            {
                EventLoopCoroutine = StartCoroutine(EventLoop());
            }
        }
        public static Gamemode FindGamemode(string name, Dictionary<string, Type> modes)
        {
            try
            {
                if (modes.TryGetValue(name, out Type type))
                {
                    if (type == default) return null;
                    if (!type.IsSubclassOf(typeof(Gamemode))) return null;
                    Gamemode gamemode = UCWarfare.I.gameObject.AddComponent(type) as Gamemode;
                    return gamemode;
                }
                else return null;
            }
            catch (Exception ex)
            {
                F.LogWarning("Exception when finding gamemode: \"" + name + '\"');
                F.LogError(ex, ConsoleColor.Yellow);
                return null;
            }
        }
    }
    public enum EState : byte
    {
        ACTIVE,
        PAUSED,
        FINISHED,
        LOADING
    }
}
