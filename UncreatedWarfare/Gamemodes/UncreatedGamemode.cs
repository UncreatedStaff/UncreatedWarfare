using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes
{
    public delegate Task TeamWinDelegate(ulong team);
    public abstract class Gamemode : IDisposable
    {
        public readonly string Name;
        private int EventLoopSpeed;
        private bool useEventLoop;
        public ConfiguredTaskAwaitable EventLoopTask;
        protected CancellationTokenSource Token { get; private set; }
        public event TeamWinDelegate OnTeamWin;
        public EState State;
        protected string shutdownMessage = string.Empty;
        protected bool shutdownAfterGame = false;
        protected ulong shutdownPlayer = 0;
        public Gamemode(string Name, float EventLoopSpeed)
        {
            this.Name = Name;
            this.Token = new CancellationTokenSource();
            this.EventLoopSpeed = Mathf.RoundToInt(EventLoopSpeed * 1000f);
            this.useEventLoop = EventLoopSpeed > 0;
            this.State = EState.LOADING;
        }
        protected void SetTiming(float NewSpeed)
        {
            this.EventLoopSpeed = Mathf.RoundToInt(NewSpeed * 1000f);
            this.useEventLoop = NewSpeed > 0;
        }
        public void Cancel()
        {
            this.Token.Cancel();
        }
        public virtual Task Init()
        {
            this.Token = new CancellationTokenSource();
            return Task.CompletedTask;
        }
        protected async Task InvokeOnTeamWin(ulong winner)
        {
            if (OnTeamWin != null)
                await OnTeamWin.Invoke(winner);
        }
        protected abstract Task EventLoopAction();
        private async Task EventLoop(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                await Task.Delay(this.EventLoopSpeed);
                DateTime start = DateTime.Now;
                try
                {
                    await EventLoopAction();
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
        public abstract Task DeclareWin(ulong winner);
        public abstract Task StartNextGame(bool onLoad = false);
        public virtual void Dispose()
        {
            Cancel();
        }
        public virtual async Task OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            await Task.Yield();
        }
        public virtual async Task OnPlayerJoined(SteamPlayer player)
        {
            await Task.Yield();
        }
        public virtual async Task OnPlayerLeft(ulong player)
        {
            await Task.Yield();
        }
        public virtual async Task OnPlayerDeath(UCWarfare.DeathEventArgs args)
        {
            await Task.Yield();
        }
        public virtual async Task OnLevelLoaded()
        {
            if (useEventLoop)
                EventLoopTask = EventLoop(Token.Token).ConfigureAwait(false);
            await Task.Yield();
        }
        public static Gamemode FindGamemode(string name, Dictionary<string, Type> modes)
        {
            try
            {
                if (modes.TryGetValue(name, out Type type))
                {
                    if (type == default) return null;
                    if (!type.IsSubclassOf(typeof(Gamemode))) return null;
                    Gamemode gamemode = (Gamemode)Activator.CreateInstance(type);
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
