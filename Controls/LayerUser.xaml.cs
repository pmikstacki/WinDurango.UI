using System;
using WinDurango.UI.Settings;

namespace WinDurango.UI.Controls
{
    public sealed partial class LayerUser
    {
        public CoreConfigData.User user;

        public LayerUser(CoreConfigData.User user)
        {
            this.InitializeComponent();
            this.user = user;
            this.Info2.Text = $"User {user.Id.ToString()}";
            this.Username.Text = user.Name;
            this.UserId.Text = user.Id.ToString();
        }

        public void Save()
        {
            int index = App.CoreSettings.Settings.Users.IndexOf(this.user);
            user.Name = this.Username.Text;
            ulong id;
            if (!ulong.TryParse(this.UserId.Text, out id))
                throw new Exception($"Cannot parse id field of user {this.user.Id}");

            user.Id = id;

            App.CoreSettings.Settings.Users[index] = user;
            App.CoreSettings.Save();
        }
    }
}
