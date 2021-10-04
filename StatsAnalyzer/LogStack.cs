using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Uncreated.Networking;
using Windows.UI.Xaml.Input;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.ApplicationModel.Core;

namespace StatsAnalyzer
{
    public sealed class LogStack : StackPanel
    {
        internal static readonly NetCall<string> SendCommand = new NetCall<string>(1032);
        internal static readonly NetCall<bool> SetConsoleLogging = new NetCall<bool>(1028);
        internal static readonly NetCall RequestFullLog = new NetCall(1029);
        internal static readonly NetCallRaw<Log, byte> SendLogMessage =
            new NetCallRaw<Log, byte>(1030, Log.Read, R => R.ReadUInt8(), Log.Write, (W, B) => W.Write(B));
        [NetCall(ENetCall.FROM_SERVER, 1030)]
        internal static void ReceiveLog(in IConnection conneciton, Log log, byte server)
        {
            Debug.WriteLine(log.Message);
            if (!FilterLog(log.Message)) return;
            Debug.WriteLine(log.Message);
            if (I == null) return;
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                I.Logs.Insert(0, log);
                int lheight = I.LogItems[0] == null || I.LogItems[0].ActualHeight == 0 || double.IsNaN(I.LogItems[0].ActualHeight) ? 40 : (int)I.LogItems[0].ActualHeight;
                StatsPage.I.ScrollBar.Maximum = I.Logs.Count * lheight;
                I.UpdateLogs();
            }).AsTask().ConfigureAwait(false);
        }
        internal static readonly NetCallRaw<Log[], byte> SendFullLog =
            new NetCallRaw<Log[], byte>(1031, Log.ReadMany, R => R.ReadUInt8(), Log.WriteMany, (W, B) => W.Write(B));
        [NetCall(ENetCall.FROM_SERVER, 1031)]
        internal static void ReceiveFullLog(in IConnection conneciton, Log[] logs, byte server)
        {
            if (I == null) return;
            I.Logs = logs.Where(x => FilterLog(x.Message)).ToList();
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                int lheight = I.LogItems[0] == null || I.LogItems[0].ActualHeight == 0 || double.IsNaN(I.LogItems[0].ActualHeight) ? 40 : (int)I.LogItems[0].ActualHeight;
                StatsPage.I.ScrollBar.Maximum = I.Logs.Count * lheight;
                I.UpdateLogs();
            }).AsTask().ConfigureAwait(false);
        }
        internal static readonly Dictionary<ConsoleColor, SolidColorBrush> COLOR_CONVERT = new Dictionary<ConsoleColor, SolidColorBrush>()
        {
            { ConsoleColor.Black, new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) },
            { ConsoleColor.DarkBlue, new SolidColorBrush(Color.FromArgb(255, 0, 0, 204)) },
            { ConsoleColor.DarkGreen, new SolidColorBrush(Color.FromArgb(255, 0, 102, 0)) },
            { ConsoleColor.DarkCyan, new SolidColorBrush(Color.FromArgb(255, 0, 153, 204)) },
            { ConsoleColor.DarkRed, new SolidColorBrush(Color.FromArgb(255, 153, 0, 0)) },
            { ConsoleColor.DarkMagenta, new SolidColorBrush(Color.FromArgb(255, 102, 0, 102)) },
            { ConsoleColor.DarkYellow, new SolidColorBrush(Color.FromArgb(255, 255, 153, 0)) },
            { ConsoleColor.Gray, new SolidColorBrush(Color.FromArgb(255, 89, 89, 89)) },
            { ConsoleColor.DarkGray, new SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) },
            { ConsoleColor.Blue, new SolidColorBrush(Color.FromArgb(255, 0, 102, 255)) },
            { ConsoleColor.Green, new SolidColorBrush(Color.FromArgb(255, 0, 204, 0)) },
            { ConsoleColor.Cyan, new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)) },
            { ConsoleColor.Red, new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) },
            { ConsoleColor.Magenta, new SolidColorBrush(Color.FromArgb(255, 255, 0, 255)) },
            { ConsoleColor.Yellow, new SolidColorBrush(Color.FromArgb(255, 255, 204, 0)) },
            { ConsoleColor.White, new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) }
        };
        private const double SCROLL_SPEED = 4.0d;
        public List<Log> Logs = new List<Log>();
        public LogItem[] LogItems = new LogItem[35];
        public LogStack() : base()
        {
            I = this;
            PointerWheelChanged += LogStack_PointerWheelChanged;
        }
        public void Init()
        {
            for (int i = LogItems.Length - 1; i >= 0; i--)
            {
                LogItems[i] = new LogItem()
                {
                    Visibility = Visibility.Collapsed,
                    MaxHeight = 40,
                    i = i
                };
                StatsPage.I.AddLog(LogItems[i]);
            }/*
            for (int i = 0; i < 100; i++)
            {
                Logs.Add(new Log("test " + i.ToString(), (ConsoleColor)(i % 16), DateTime.Now.AddHours(i / 4).AddMinutes(i)));
            }*/
            int lheight = LogItems[0] == null || LogItems[0].ActualHeight == 0 || double.IsNaN(LogItems[0].ActualHeight) ? 40 : (int)LogItems[0].ActualHeight;
            StatsPage.I.ScrollBar.Maximum = Logs.Count * lheight;
            StatsPage.I.ScrollBar.ViewportSize = StatsPage.I.ActualHeight - StatsPage.I.ConsoleInputHeight;
            StatsPage.I.ScrollBar.Value = StatsPage.I.ScrollBar.Maximum;
            CalculateLogCount();
        }
        private Log[] _current;
        private int offset = 0;
        private void LogStack_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            ArtificialScroll(e.GetCurrentPoint(this).Properties.MouseWheelDelta);
        }
        public static bool ignoreScroll = false;
        public void ArtificialScroll(int delta)
        {
            double amt = delta / 120d * SCROLL_SPEED;
            if (amt + offset < 0) amt = -offset;
            if (amt < 0 && offset <= 0) return;
            else
            {
                offset += (int)Math.Floor(amt);
                UpdateLogs();
            }
            double maxoffset = Logs.Count - lastamt;
            ignoreScroll = true;
            StatsPage.I.ScrollBar.Value = 4000 * (1d - offset / maxoffset);
            ignoreScroll = false;
        }
        public void ArtificialScroll(double amt01)
        {
            int maxoffset = Logs.Count - lastamt;
            double result = amt01 * maxoffset;
            offset = (int)result;
            UpdateLogs();
        }
        private static LogStack I;
        private int lastamt = 0;
        internal void CalculateLogCount()
        {
            int height = (int)(StatsPage.I.ActualHeight - StatsPage.I.ConsoleInputHeight);
            int lheight = LogItems[0] == null || LogItems[0].ActualHeight == 0 || double.IsNaN(LogItems[0].ActualHeight) ? 40 : (int)LogItems[0].ActualHeight;
            int amt = height / lheight;
            if (amt == lastamt) return;
            lastamt = amt;
            for (int i = 0; i < LogItems.Length; i++)
            {
                if (i < amt)
                {
                    if (LogItems[i].Visibility != Visibility.Visible)
                        LogItems[i].Visibility = Visibility.Visible;
                }
                else if (LogItems[i].Visibility != Visibility.Collapsed)
                {
                    LogItems[i].Visibility = Visibility.Collapsed;
                }
            }
            StatsPage.I.ScrollBar.Maximum = Logs.Count * lheight;
            StatsPage.I.ScrollBar.ViewportSize = StatsPage.I.ActualHeight - StatsPage.I.ConsoleInputHeight;
            UpdateLogs();
        }

        internal void UpdateLogs()
        {
            if (offset + lastamt > Logs.Count)
            {
                offset = Logs.Count - lastamt > 0 ? Logs.Count - lastamt : 0;
            }
            _current = Logs.GetRange(offset, offset == 0 ? Math.Min(Logs.Count, lastamt) : lastamt).ToArray();
            for (int i = 0; i < LogItems.Length; i++)
            {
                if (_current.Length <= i) break;
                LogItems[i].SetLogText(_current[i]);
            }
        }
        private static bool FilterLog(string log)
        {
            for (int i = 0; i < UnusedEquals.Length; i++)
            {
                if (log == UnusedEquals[i]) return false;
            }
            for (int i = 0; i < UnusedStartsWith.Length; i++)
            {
                if (log.StartsWith(UnusedStartsWith[i])) return false;
            }
            for (int i = 0; i < UnusedContains.Length; i++)
            {
                if (log.Contains(UnusedContains[i])) return false;
            }
            return true;
        }

        // filters:
        private static readonly string[] UnusedEquals = new string[]
        {
            "Constructor on type 'SDG.Unturned.DialogueAsset' not found."
        };
        private static readonly string[] UnusedContains = new string[]
        {
            "SDG.Unturned.Assets.loadFile",
            "System.Activator.CreateInstance",
            "System.RuntimeType.CreateInstanceImpl"
        };
        private static readonly string[] UnusedStartsWith = new string[]
        {
            "Tree with no asset"
        };
    }
}
