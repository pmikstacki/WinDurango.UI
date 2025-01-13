using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage.Streams;
using WinDurango.UI.Dialogs;
using WinDurango.UI.Settings;
using WinDurango.UI.Utils;

namespace WinDurango.UI.Controls
{
    public sealed partial class AppTile
    {
        private Package _package;
        private readonly string _familyName;
        private string _Name;
        private string _Publisher;
        private string _Version;
        private Uri _Logo;
        private ProgressDialog currentDialog = null;
        // "Just Works"
        // this needs to be fixed.
        private bool shouldShowDone = true;

        private async void HandleUnregister(object sender, SplitButtonClickEventArgs e)
        {
            if ((bool)unregisterCheckbox.IsChecked)
            {
                var confirmation =
                    new Confirmation(Localization.Locale.GetLocalizedText("Packages.UninstallConfirmation", _Name),
                        "Uninstall?");
                Dialog.BtnClicked answer = await confirmation.Show();

                if (answer != Dialog.BtnClicked.Yes)
                    return;
            }

            if ((bool)unpatchCheckbox.IsChecked && await WinDurangoPatcher.UnpatchPackage(_package, null))
                UnpatchPackage(_package, null);
                
            if ((bool)unregisterCheckbox.IsChecked)
                await Packages.RemovePackage(_package);
                
            App.InstalledPackages.RemovePackage(_package);
            App.MainWindow.AppsListPage.InitAppList();
        }

        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Logger.WriteDebug($"Opening app installation folder {_package.InstalledPath}");
            _ = Process.Start(new ProcessStartInfo(_package.InstalledPath) { UseShellExecute = true });
        }

        private async Task StatusUpdateAsync(string status, int progress)
        {
            Logger.WriteInformation(status);
            
            if (currentDialog == null)
            {
                currentDialog = new ProgressDialog("Working", "Patcher", false);
                // shitty way of doing it
                if (new ProgressDialog("Working", "Patcher", false) != null)
                {
                    await App.MainWindow.DispatcherQueue.EnqueueAsync(async () =>
                    {
                        await currentDialog.ShowAsync();
                    });
                } else
                {
                    Logger.WriteDebug("???");
                }
            } else
            {
                currentDialog.Text = status;
                currentDialog.Progress = progress;
                if (progress == 100)
                {
                    Logger.WriteDebug("100");
                    currentDialog.Hide();
                    currentDialog = null;
                    if (shouldShowDone)
                        await new NoticeDialog("Done!", "Patcher").Show();
                }
            }
        }

        private async void RepatchPackage(object sender, RoutedEventArgs args)
        {
            shouldShowDone = false;
            await WinDurangoPatcher.UnpatchPackage(_package, StatusUpdateAsync);
            shouldShowDone = true;
            await WinDurangoPatcher.PatchPackage(_package, true, StatusUpdateAsync);
            App.MainWindow.ReloadAppList();
        }

        private async void UnpatchPackage(object sender, RoutedEventArgs args)
        {
            await WinDurangoPatcher.UnpatchPackage(_package, StatusUpdateAsync);
            App.MainWindow.ReloadAppList();
        }

        private async void PatchPackage(object sender, RoutedEventArgs args)
        {
            await WinDurangoPatcher.PatchPackage(_package, false, StatusUpdateAsync);
            App.MainWindow.ReloadAppList();
        }

        public AppTile(string familyName)
        {
            _familyName = familyName;
            this.InitializeComponent();

            _package = Packages.GetPackageByFamilyName(_familyName);
            try
            {
                _Name = _package.DisplayName ?? _package.Id.Name;
            }
            catch
            {
                _Name = _package.Id.Name;
            }
            _Publisher = _package.PublisherDisplayName ?? _package.Id.PublisherId;
            _Version = $"{_package.Id.Version.Major.ToString() ?? "U"}.{_package.Id.Version.Minor.ToString() ?? "U"}.{_package.Id.Version.Build.ToString() ?? "U"}.{_package.Id.Version.Revision.ToString() ?? "U"}";
            _Logo = _package.Logo;

            string ss = Packages.GetSplashScreenPath(_package);
            IReadOnlyList<AppListEntry> appListEntries = null;
            try
            {
                appListEntries = _package.GetAppListEntries();
            } catch
            {
                Logger.WriteWarning($"Could not get the applist entries of \"{_Name}\"");
            }
            AppListEntry firstAppListEntry = appListEntries?.FirstOrDefault() ?? null;

            if (firstAppListEntry == null)
                Logger.WriteWarning($"Could not get the applist entry of \"{_Name}\"");

            if (ss == null || !File.Exists(ss))
            {
                try
                {
                    if (firstAppListEntry != null)
                    {
                        RandomAccessStreamReference logoStream = firstAppListEntry.DisplayInfo.GetLogo(new Size(320, 180));
                        BitmapImage logoImage = new();
                        using IRandomAccessStream stream = logoStream.OpenReadAsync().GetAwaiter().GetResult();
                        logoImage.SetSource(stream);
                        appLogo.Source = logoImage;
                    }
                    else
                    {
                        BitmapImage logoImage = new(_Logo);
                        appLogo.Source = logoImage;
                    }
                }
                catch (Exception)
                {
                    BitmapImage logoImage = new(_Logo);
                    appLogo.Source = logoImage;
                }
            }
            else
            {
                appLogo.Source = new BitmapImage(new Uri(ss));
            }
            infoExpander.Header = _Name;

            MenuFlyout rcFlyout = new();

            bool isPatched = false;
            
            installedPackage instPackage = App.InstalledPackages.GetPackage(_package.Id.FamilyName);
            if (instPackage != null)
                isPatched = instPackage.IsPatched;

            MenuFlyoutItem patchButton = new MenuFlyoutItem
            {
                Text = isPatched ? "Repatch" : "Patch",
                Name = "patchButton"
            };

            if (isPatched)
            {
                patchButton.Click += RepatchPackage;
                MenuFlyoutItem unpatchButton = new MenuFlyoutItem
                {
                    Text = "Unpatch",
                    Name = "unpatchButton"
                };
                unpatchButton.Click += UnpatchPackage;
                rcFlyout.Items.Add(unpatchButton);
            } else
            {
                patchButton.Click += PatchPackage;
            }

            rcFlyout.Items.Add(patchButton);

            expanderVersion.Text = $"Publisher: {_Publisher}\nVersion {_Version}";

            RightTapped += (sender, e) =>
            {
                rcFlyout.ShowAt(sender as FrameworkElement);
            };

            startButton.Tapped += async (s, e) =>
            {
                if (_package.Status.LicenseIssue)
                {
                    Logger.WriteError($"Could not launch {_Name} due to licensing issue.");
                    _ = new NoticeDialog($"There is a licensing issue... Do you own this package?", $"Could not launch {_Name}").Show();
                    return;
                }

                if (firstAppListEntry == null)
                {
                    _ = new NoticeDialog($"Could not get the applist entry of \"{_Name}\"", $"Could not launch {_Name}").Show();
                    return;
                }
                Logger.WriteInformation($"Launching {_Name}");
                if (await firstAppListEntry.LaunchAsync() == false)
                    _ = new NoticeDialog($"Failed to launch \"{_Name}\"!", $"Could not launch {_Name}").Show();
            };
        }
    }
}
