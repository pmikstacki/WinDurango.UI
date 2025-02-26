using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using WinDurango.UI.Utils;
using System.Diagnostics;
using WinDurango.UI.Controls;
using WinDurango.UI.Dialogs;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.Controls;

namespace WinDurango.UI.Pages.Dialog
{
    public sealed partial class SaveManagerPage : Page
    {
        private Package _package;
        private string _dataDir;
        public SaveManagerPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string familyName)
            {
                _package = Packages.GetPackageByFamilyName(familyName);
                this.Loaded += (sender, e) =>
                {
                    Init();
                };
            }
        }

        public void ShowInfo(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            this.infoBar.Children.Clear();
            double width = this.ActualWidth;
            if (width == 0)
            {
                width = this.Frame.ActualWidth;
            }

            InfoBar infoBar = new InfoBar();
            Button copyButton = new Button();
            SymbolIcon symbolIcon = new SymbolIcon(Symbol.Copy);

            copyButton.Content = symbolIcon;
            copyButton.HorizontalAlignment = HorizontalAlignment.Right;
            copyButton.Click += (sender, e) =>
            {
                DataPackage dp = new DataPackage();
                dp.SetText(message);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
            };

            infoBar.Severity = severity;
            infoBar.IsOpen = true;
            infoBar.Message = message;
            infoBar.Title = title;
            infoBar.ActionButton = copyButton;
            infoBar.MaxWidth = this.ActualWidth;

            Logger.WriteInformation(message);

            this.infoBar.Children.Add(infoBar);
        }

        // TODO: slow
        private void Init()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", _package.Id.FamilyName);
            if (!Directory.Exists(path))
            {
                ShowInfo("Error finding package data", $"Could not find path {path}.\nHave you ran this package yet?", InfoBarSeverity.Error);
                return;
            }

            _dataDir = path;
            string userStorage = Path.Combine(path, "LocalState", "WinDurango", "UserStorage");

            if (!Directory.Exists(userStorage))
            {
                string msg = $"Could not find WinDurango UserStorage path.\nHave you ran this package yet?";
                bool exists = Directory.Exists(Path.Combine(path, "LocalState"));
                if (exists)
                {
                    userStorage = Path.Combine(path, "LocalState");
                    msg += "\n\nUsing LocalState instead.";
                }
                ShowInfo("Error finding UserStorage", msg, exists ? InfoBarSeverity.Warning : InfoBarSeverity.Error);
                if (!exists)
                    return;
            }

            containerList.Children.Clear();
            folderList.Children.Clear();
            foreach (string dir in Directory.GetDirectories(userStorage))
            {
                // GetFileName returns the name of the file including the extension, alright.
                // so then we have GetDirectoryName... what does it do? I thought it would return the folder name, but no... it returns the path.
                string dispName = Path.GetFileName(dir);
                string dispNameTxt = Path.Combine(dir, "wd_displayname.txt");
                bool isContainer = File.Exists(dispNameTxt);
                if (isContainer) {
                    dispName = File.ReadAllText(dispNameTxt);
                    containerList.Children.Add(new ContainerInfo(dir, dispName, isContainer));
                } else
                {
                    folderList.Children.Add(new ContainerInfo(dir, dispName, isContainer));
                }
            }

            if (folderList.Children.Count != 0)
            {
                folderHeader.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }

            if (containerList.Children.Count != 0)
            {
                containerHeader.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }

            if (containerList.Children.Count == 0 && folderList.Children.Count == 0)
            {
                noSavesHeader.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }

        }

        public void RemoveElement(ContainerInfo element)
        {
            folderList.Children.Remove(element);
            containerList.Children.Remove(element);
        }

        private void ViewFolder(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            string dir = _dataDir;

            string userStorage = Path.Combine(dir, "LocalState", "WinDurango", "UserStorage");
            if (Directory.Exists(userStorage))
            {
                dir = userStorage;
            }

            FSHelper.OpenFolder(dir);
        }
    }
}
