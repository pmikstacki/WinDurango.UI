using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using WinDurango.UI.Utils;
using Image = Microsoft.UI.Xaml.Controls.Image;

namespace WinDurango.UI.Dialogs
{
    public sealed partial class AppListDialog
    {
        public List<Package> Packages { get; set; }

        public AppListDialog(List<Package> packages, bool multiSelect = false)
        {
            this.Packages = packages;

            this.DataContext = this;

            this.InitializeComponent();

            if (multiSelect)
                appListView.SelectionMode = ListViewSelectionMode.Multiple;
            appListView.MaxHeight = App.MainWindow.Bounds.Height * 0.65;

        }

        private void AppListView_Loaded(object sender, RoutedEventArgs e)
        {
            appListView.MinWidth = Math.Max(App.MainWindow.Bounds.Width / 2, 500);
            appListView.MaxWidth = Math.Max(App.MainWindow.Bounds.Width / 2, 500);

            var listView = (ListView)sender;

            foreach (Package pkg in Packages)
            {
                // if we already have the package "installed" we will skip it (not show it in the AppListView)
                // maybe this behavior should change?
                if (App.InstalledPackages.GetPackages().Find(p => p.FamilyName == pkg.Id.FamilyName) != null) continue;
                
                ListViewItem item = new() { MinWidth = 200 };
                StackPanel stackPanel = new() { Orientation = Orientation.Horizontal };

                // NOTE: DO NOT TOUCH THIS MAGICAL SHIT 
                // it throws massive error if the image is invalid somehow or whatever...
                Uri pkLogo = null;
                try
                {
                    pkLogo = pkg.Logo;
                }
                catch (Exception ex)
                {
                    Logger.WriteError($"pkg.Logo threw {ex.GetType()} for {pkg.Id.FamilyName}");
                    Logger.WriteException(ex);
                }

                // Some app logos are "empty" but pkg.Logo isn't null on them, why microsoft??
                Image packageLogo = new()
                {
                    Width = 64,
                    Height = 64,
                    Margin = new Thickness(5),
                    Source = new BitmapImage(pkLogo ?? new Uri("ms-appx:///Assets/no_img64.png"))
                };

                StackPanel packageInfo = new()
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(5)
                };

                string displayName;
                try
                {
                    displayName = pkg.DisplayName;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    displayName = pkg.Id.Name;
                }

                TextBlock packageName = new()
                {
                    Text = displayName ?? "Unknown",
                    FontWeight = FontWeights.Bold
                };

                TextBlock publisherName = new() { Text = pkg.PublisherDisplayName ?? "Unknown" };

                packageInfo.Children.Add(packageName);
                packageInfo.Children.Add(publisherName);

                stackPanel.Children.Add(packageLogo);
                stackPanel.Children.Add(packageInfo);

                item.Content = stackPanel;

                item.Tag = pkg;

                listView.Items.Add(item);
            }
        }
        
        private void AddToAppList(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            foreach (ListViewItem listViewItem in appListView.SelectedItems)
            {
                var package = listViewItem.Tag as Package;
                if (package != null && package?.Id?.FamilyName != null)
                {
                    if (App.InstalledPackages.GetPackage(package.Id.FamilyName) == null)
                        App.InstalledPackages.AddPackage(package);
                }
            }

            _ = App.MainWindow.AppsListPage.InitAppListAsync();
        }

        private void HideDialog(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide();
        }
    }
}
