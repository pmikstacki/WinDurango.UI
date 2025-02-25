using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using WinDurango.UI.Pages.Dialog;
using WinDurango.UI.Settings;
using WinDurango.UI.Utils;

namespace WinDurango.UI.Controls
{
    public sealed partial class LayerUser
    {
        public CoreConfigData.User user;

        public LayerUser(CoreConfigData.User user)
        {
            this.InitializeComponent();
            this.user = user;
            this.Username.Text = user.Name;
            this.UserId.Text = user.Id.ToString();
        }

        public void Save(object sender, RoutedEventArgs e) => Save();

        public void Save()
        {
            int index = App.CoreSettings.Settings.Users.IndexOf(this.user);
            bool shouldSave = false;

            ulong id;
            if (!ulong.TryParse(this.UserId.Text, out id))
            {
                Logger.WriteError($"Cannot parse id field ({this.UserId.Text}) of user {this.user.Name}");
            }
            else
            {
                if (user.Id != id)
                {
                    user.Id = id;
                    shouldSave = true;
                }
            }

            if (user.Name != this.Username.Text)
            {
                user.Name = this.Username.Text;
                shouldSave = true;
            }

            if (shouldSave)
            {
                App.CoreSettings.Settings.Users[index] = user;
                App.CoreSettings.Save();
            }
        }

        private void DeleteUser(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is UserManPage))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
                ((UserManPage)parent).RemoveElement(this);

            App.CoreSettings.Settings.Users.Remove(user);
        }

        private void UserId_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            ulong id;
            if (!ulong.TryParse(box.Text, out id))
                box.Foreground = new SolidColorBrush(Colors.IndianRed);
            else
                box.Foreground = new SolidColorBrush(Colors.White);
        }
    }
}
