using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using WinDurango.UI.Controls;
using WinDurango.UI.Dialogs;
using WinDurango.UI.Pages.Dialog;
using WinDurango.UI.Settings;


namespace WinDurango.UI.Pages.Settings
{
    public partial class WdSettingsPage : Page
    {
        public WdSettingsPage()
        {
            this.InitializeComponent();
        }

        public CoreConfigData.User GetUser(ulong id)
        {
            return App.CoreSettings.Settings.Users.Find(u => u.Id == id);
        }

        public void OnToggleSetting(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch && toggleSwitch.Tag is string settingName)
            {
                App.CoreSettings.Set(settingName, toggleSwitch.IsOn);
            }
        }

        private async void ManageUsers(object sender, RoutedEventArgs e)
        {
            PageDialog pgd = new PageDialog(typeof(UserManPage), null, $"Users");
            pgd.XamlRoot = App.MainWindow.Content.XamlRoot;
            await pgd.ShowAsync();
        }

        private void OpenAppData(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(App.CoreDataDir) { UseShellExecute = true });
        }
    }
}
