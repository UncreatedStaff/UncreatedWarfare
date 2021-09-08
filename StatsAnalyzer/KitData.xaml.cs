using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uncreated.Warfare.Stats;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace StatsAnalyzer
{
    public sealed partial class KitData : UserControl
    {
        public string KitName;
        public KitData()
        {
            this.InitializeComponent();
        }
        public void Load(WarfareStats.KitData data, ulong owner)
        {
            KitName = data.KitID;
            lblAverageGunKilDistance.Text = data.AverageGunKillDistance.ToString("N2");
            lblDowns.Text = data.Downs.ToString(StatsPage.Locale);
            lblTotalKills.Text = data.Kills.ToString(StatsPage.Locale);
            lblTotalDeaths.Text = data.Deaths.ToString(StatsPage.Locale);
            lblRevives.Text = data.Revives.ToString(StatsPage.Locale);
            string[] ids = data.KitID.Split('_');
            if (ids.Length > 1 && ulong.TryParse(ids[0], System.Globalization.NumberStyles.Any, StatsPage.Locale, out ulong Steam64) && Steam64 == owner)
            {
                lblKitName.Text = "Loadout " + string.Join('_', ids.Skip(1)) + ", Team: " + data.Team;
            } else
            {
                lblKitName.Text = data.KitID + ", Team: " + data.Team;
            }
            lblRequestCount.Text = data.TimesRequested.ToString(StatsPage.Locale);
            lblPlaytime.Text = F.GetTimeFromMinutes(data.PlaytimeMinutes);
        }
    }
}
