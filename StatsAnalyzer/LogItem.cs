using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace StatsAnalyzer
{
    public sealed class LogItem : ListViewItem
    {
        public int i;
        public Log Log
        {
            get => (Log)GetValue(LogProperty);
            set => SetValue(LogProperty, value);
        }
        public static readonly DependencyProperty LogProperty =
            DependencyProperty.Register("Log", typeof(Log), typeof(LogItem), new PropertyMetadata(
                new Log() { color = ConsoleColor.White, Message = string.Empty, timestamp = DateTime.Now }));

        public LogItem() : base() { }

        internal static void LogItem_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage
            { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText((sender as LogItem).Log.Message);
            Clipboard.SetContent(dataPackage);
        }

        public void SetLogText(Log log)
        {
            Log = log;
            this.Content = log.Message;
            if (LogStack.COLOR_CONVERT.TryGetValue(log.color, out SolidColorBrush brush))
                this.Foreground = brush;
        }
        public override string ToString() => $"Vis: {Visibility}, \"{Content}\"";
    }
}
