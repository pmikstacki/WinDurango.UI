using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls.Primitives;
using WinDurango.UI.Settings;
using WinDurango.UI.Utils;
using WinDurango.UI.Pages.Settings;

namespace WinDurango.UI.Pages
{
    public sealed partial class SettingsPage : Page
    {
        // should probably merge these into one?
        
        private void NavigationInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is not NavigationViewItem item)
            {
                return;
            }

            string tag = item.Tag.ToString();
            Type pageType = tag switch
            {
                "LayerSettings" => typeof(WdSettingsPage),
                "UiSettings" => typeof(UiSettings),
                _ => typeof(WdSettingsPage)
            };

            contentFrame.Navigate(pageType);
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            contentFrame.Navigate(typeof(WdSettingsPage));
        }
    }
}
