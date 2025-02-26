using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using WinDurango.UI.Controls;
using WinDurango.UI.Utils;

namespace WinDurango.UI.Pages.Dialog
{
    public sealed partial class ModManPage : Page
    {
        private string _modsPath;
        private string _packagePath;

        public ModManPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if ((e.Parameter).GetType() != typeof(string))
                return;

            string path = (string)e.Parameter;
            Init(path);
        }

        private void Init(string path)
        {
            // Layer makes it uppercase. This may cause issues once multiplatform support happens
            _modsPath = Path.Combine(path, "mods");
            _packagePath = path;

            if (!Directory.Exists(_modsPath))
            {
                noModsFolder.Visibility = Visibility.Visible;
                openModsFolder.Visibility = Visibility.Collapsed;
                createModsFolder.Visibility = Visibility.Visible;
                return;
            };

            ReadOnlySpan<string> mods = Directory.GetFiles(_modsPath);

            if (mods.IsEmpty)
            {
                noMods.Visibility = Visibility.Visible;
                return;
            }

            modList.Children.Clear();

            foreach (var mod in mods)
            {
                if (Path.GetExtension(mod) != ".dll" && Path.GetExtension(mod) != ".disabled")
                    continue;

                ModInfo info = new ModInfo(mod);
                modList.Children.Add(info);
            }
        }

        public void RemoveElement(ModInfo element)
        {
            modList.Children.Remove(element);
            if (cModList.Children.ToList().Capacity == 0)
            {
                noMods.Visibility = Visibility.Visible;
            }
        }


        // LATER: make this common
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

        private void CreateModsFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_modsPath))
                    Directory.CreateDirectory(_modsPath);

                this.modList.Children.Clear();

                openModsFolder.Visibility = Visibility.Visible;
                createModsFolder.Visibility = Visibility.Collapsed;
                noMods.Visibility = Visibility.Collapsed;
                noModsFolder.Visibility = Visibility.Collapsed;

                Init(_packagePath);
            }
            catch (Exception ex)
            {
                ShowInfo("Couldn't create mod folder", $"{ex.Message}", InfoBarSeverity.Error);
                Logger.WriteError($"Couldn't create mod folder {_modsPath}");
                Logger.WriteException(ex);
            }
        }

        private void OpenModsFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_modsPath))
                    Directory.CreateDirectory(_modsPath);

                _ = Process.Start(new ProcessStartInfo(_modsPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowInfo($"Couldn't open mod folder", $"{ex.Message}", InfoBarSeverity.Error);
                Logger.WriteError($"Couldn't open mod folder path {_modsPath}");
                Logger.WriteException(ex);
            }
        }

    }
}
