using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinDurango.UI.Controls;
using WinDurango.UI.Dialogs;
using WinDurango.UI.Settings;
using WinDurango.UI.Utils;

namespace WinDurango.UI.Pages.Dialog
{
    public sealed partial class UserManPage : Page
    {
        public UserManPage()
        {
            this.InitializeComponent();

            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is PageDialog))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
                ((PageDialog)parent).PrimaryButtonText = "Cancel";

            LoadUsers();
        }

        public void RemoveElement(LayerUser element)
        {
            userList.Children.Remove(element);
        }

        public void LoadUsers()
        {
            userList.Children.Clear();
            foreach (CoreConfigData.User settingsUser in App.CoreSettings.Settings.Users)
            {
                userList.Children.Add(new LayerUser(settingsUser));
            }
        }

        private void AddUser(object sender, RoutedEventArgs e)
        {            
            CoreConfigData.User user = new CoreConfigData.User
            {
                Name = $"durangler{CoreConfigData.User.GetFreeId()}",
                Id = CoreConfigData.User.GetFreeId()
            };

            App.CoreSettings.Settings.Users.Add(user);
            userList.Children.Add(new LayerUser(user));
            App.CoreSettings.Save();

            Logger.WriteInformation($"Added user {user.Name}");
        }
    }
}
