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
    public sealed partial class WeaponData : UserControl
    {
        public ushort Weapon
        {
            get => unchecked((ushort)GetValue(WeaponProperty));
            set => SetValue(WeaponProperty, value);
        }
        public static readonly DependencyProperty WeaponProperty =
            DependencyProperty.Register("Weapon", typeof(ushort), typeof(WeaponData), new PropertyMetadata(0));
        public string KitID
        {
            get => (string)GetValue(KitIDProperty);
            set => SetValue(KitIDProperty, value);
        }
        public static readonly DependencyProperty KitIDProperty =
            DependencyProperty.Register("KitID", typeof(string), typeof(WeaponData), new PropertyMetadata(string.Empty));

        public WeaponData()
        {
            this.InitializeComponent();
        }
        public void Load(WarfareWeapon weapon, string name, string kitname)
        {
            Weapon = weapon.ID;
            KitID = weapon.KitID;
            lblKitName.Text = $"{weapon.KitID}{(kitname == weapon.KitID ? string.Empty : $" - {kitname}")}";
            lblWeaponName.Text = name.Length > 0 ? $"{name} ({weapon.ID})" : weapon.ID.ToString(StatsPage.Locale);
            lblTotalKills.Text = weapon.Kills.ToString(StatsPage.Locale);
            lblTotalDeaths.Text = weapon.Deaths.ToString(StatsPage.Locale);
            lblDowns.Text = weapon.Downs.ToString(StatsPage.Locale);
            lblAverageGunKilDistance.Text = weapon.AverageKillDistance.ToString("N2", StatsPage.Locale);
            lblSkullKills.Text = weapon.SkullKills.ToString(StatsPage.Locale);
            lblBodyKills.Text = weapon.BodyKills.ToString(StatsPage.Locale);
            lblArmKills.Text = weapon.ArmKills.ToString(StatsPage.Locale);
            lblLegKills.Text = weapon.LegKills.ToString(StatsPage.Locale);
        }
    }
}
