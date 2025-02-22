using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinDurango.UI.Controls;
using WinDurango.UI.Settings;
using static WinUI3Localizer.LanguageDictionary;


namespace WinDurango.UI.Pages.Settings
{
    public partial class WdSettingsPage : Page
    {
        public List<LayerUser> users = new();
        public WdSettingsPage()
        {
            this.InitializeComponent();
            LoadUsers();
        }

        public void LoadUsers()
        {
            SelectUser.Items.Clear();
            users.Clear();
            foreach (CoreConfigData.User settingsUser in App.CoreSettings.Settings.Users)
            {
                users.Add(new LayerUser(settingsUser));

                ComboBoxItem item = new ComboBoxItem
                {
                    Content = $"User {settingsUser.Id} ({settingsUser.Name})",
                    Tag = settingsUser.Id.ToString()
                };
                SelectUser.Items.Add(item);
            }
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
        
        private void SaveUsers(object sender, RoutedEventArgs e)
        {
            foreach (var userElement in users)
            {
                if (userElement.GetType() != typeof(LayerUser))
                    continue;

                userElement.Save();
            }
        }
        
        private void OpenAppData(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(App.CoreDataDir) { UseShellExecute = true });
        }

        private void OnUserSelect(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox box)
                return;

            ComboBoxItem item = (ComboBoxItem)box.SelectedItem;

            if (item.Tag is not string tag || !ulong.TryParse(tag, out ulong id)) return;

            LayerUser.Children.Clear();
            var user = users.Find(user => user.user.Id.ToString() == item.Tag.ToString());
            LayerUser.Children.Add(user);
            
        }
    }
}
