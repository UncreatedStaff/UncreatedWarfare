using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;
using Uncreated.Warfare.Stats;
using Windows.ApplicationModel.Core;
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
    public sealed partial class WeaponList : UserControl
    {
        public const int COLUMN_COUNT = 6;
        public WeaponList()
        {
            this.InitializeComponent();
        }

        internal static readonly NetCallRaw<WarfareWeapon[], string, string[]> SendWeapons =
            new NetCallRaw<WarfareWeapon[], string, string[]>(2019, ReadWeaponArray, R => R.ReadString(), ReadStringArray, WriteWeaponArray, (W, S) => W.Write(S), WriteStringArray);

        [NetCall(ENetCall.FROM_SERVER, 2019)]
        internal static void ReceiveWeapons(in IConnection connection, WarfareWeapon[] weapons, string weaponname, string[] kitnames)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                await StatsPage.I.UpdateWeaponList(weapons, weaponname, kitnames);
            }).AsTask().ConfigureAwait(false);
        }
        public void Load(WarfareWeapon[] weapons, string weaponname, string[] kitnames)
        {
            lblTitle.Text = weaponname;
            if (weapons.Length != kitnames.Length)
            {
                Debug.WriteLine($"Desync between weapons ({weapons.Length}) and kitnames ({kitnames.Length}).");
                return;
            }
            pnlGrid.Children.Clear();
            int columns = Math.Min(weapons.Length, COLUMN_COUNT);
            int rows = (weapons.Length / COLUMN_COUNT) + 1;
            pnlGrid.ColumnDefinitions.Clear();
            pnlGrid.RowDefinitions.Clear();
            for (int i = 0; i < columns; i++)
                pnlGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < rows; i++)
                pnlGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponData dataCtrl = new WeaponData()
                {
                    //Margin = new Thickness(4, 0, 4, 0),
                    Padding = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                dataCtrl.PointerReleased += KitPointerReleased;
                dataCtrl.PointerEntered += KitPointerEntered;
                dataCtrl.PointerExited += KitPointerExited;
                dataCtrl.Load(weapons[i], weaponname, kitnames[i]);
                pnlGrid.Children.Add(dataCtrl);
                Grid.SetRow(dataCtrl, i / COLUMN_COUNT);
                Grid.SetColumn(dataCtrl, i % COLUMN_COUNT);
            }
        }
        public static string[] ReadStringArray(ByteReader R)
        {
            int length = R.ReadInt32();
            string[] rtn = new string[length];
            for (int i = 0; i < length; i++)
                rtn[i] = R.ReadString();
            return rtn;
        }
        public static void WriteStringArray(ByteWriter W, string[] A)
        {
            W.Write(A.Length);
            for (int i = 0; i < A.Length; i++)
                W.Write(A[i]);
        }
        public static WarfareWeapon[] ReadWeaponArray(ByteReader R)
        {
            int length = R.ReadInt32();
            WarfareWeapon[] weapons = new WarfareWeapon[length];
            for (int i = 0; i < length; i++)
            {
                weapons[i] = WarfareWeapon.Read(R);
            }
            return weapons;
        }
        public static void WriteWeaponArray(ByteWriter W, WarfareWeapon[] A)
        {
            W.Write(A.Length);
            for (int i = 0; i < A.Length; i++)
            {
                WarfareWeapon.Write(W, A[i]);
            }
        }

        private void KitPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UserControl data)
                data.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 255, 255));
        }
        private void KitPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UserControl data)
                data.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(25, 255, 255, 255));
        }
        private void KitPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (sender is WeaponData data && data.KitID != null)
                StatsPage.RequestWeaponData.Invoke(StatsPage.I.NetClient.connection, data.Weapon, data.KitID, !StatsPage.I.IsCached(data.Weapon, false));
        }
    }
}
