using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.IO;
using WinDurango.UI.Pages.Dialog;

namespace WinDurango.UI.Controls
{
    public sealed partial class ModInfo : UserControl
    {
        private string _dllPath;
        private readonly FileVersionInfo _info;

        public ModInfo(string dll)
        {
            _dllPath = dll;
            this.InitializeComponent();
            enableSwitch.IsOn = Path.GetExtension(_dllPath) == ".dll";
            enableSwitch.Toggled += ChangeModStatus;

            _info = FileVersionInfo.GetVersionInfo(_dllPath);

            string name = _info.ProductName;
            string description = _info.FileDescription;
            string publisher = _info.CompanyName;

            if (name == "" || name == null)
                name = Path.GetFileNameWithoutExtension(_dllPath);

            if (publisher == "" || publisher == null)
                publisher = "Unknown Author";

            this.name.Text = name;
            // check if desc is invalid OR the name bc C# projs seem to have a bunch of fields set "incorrectly"
            if (description == null || description == "" || description == name)
                this.description.Visibility = Visibility.Collapsed;

            this.version.Text = $"v{_info.ProductVersion}";
            this.description.Text = description;
            this.publisher.Text = publisher;
        }

        private void ChangeModStatus(object sender, RoutedEventArgs e)
        {
            ToggleSwitch s = (ToggleSwitch)sender;

            string newExt = s.IsOn ? ".dll" : ".disabled";

            string newPath = Path.ChangeExtension(_dllPath, newExt);
            File.Move(_dllPath, newPath);
            _dllPath = newPath;

        }

        private void DeleteMod(object sender, RoutedEventArgs e)
        {
            Flyout flyout = new Flyout();
            TextBlock title = new TextBlock { Text = $"Are you sure you want to delete {_info.ProductName}?" };
            TextBlock info = new TextBlock { Text = $"This file will be deleted from the disk." };
            title.Style = (Style)Application.Current.Resources["BaseTextBlockStyle"];
            Button button = new Button();
            button.Content = "Delete";
            button.Margin = new Thickness(0, 10, 0, 0);

            button.Click += (s, e) =>
            {
                flyout.Hide();
                File.Delete(_dllPath);
                var parent = VisualTreeHelper.GetParent(this);
                while (parent != null && !(parent is ModManPage))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                if (parent != null)
                    ((ModManPage)parent).RemoveElement(this);
            };

            flyout.Content = new StackPanel
            {
                Children =
                {
                    title,
                    info,
                    button
                }
            };

            flyout.ShowAt((Button)sender);
        }
    }
}
