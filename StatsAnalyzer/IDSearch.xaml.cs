using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StatsAnalyzer
{
    public sealed partial class IDSearch : ContentDialog
    {
        public bool IsVehicle
        {
            get => (bool)GetValue(IsVehicleProperty);
            set => SetValue(IsVehicleProperty, value);
        }
        public static readonly DependencyProperty IsVehicleProperty =
            DependencyProperty.Register("IsVehicle", typeof(bool), typeof(bool), new PropertyMetadata(false));

        public Action<ushort, IDSearch, ContentDialogButtonClickEventArgs> OnOK;
        public IDSearch()
        {
            this.InitializeComponent();
        }
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (ushort.TryParse(searchBox.Text, System.Globalization.NumberStyles.Any, StatsPage.Locale, out ushort id))
            {
                OnOK?.Invoke(id, this, args);
            }
            else
            {
                args.Cancel = true;
            }
        }
        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = false;
        }
    }
}
