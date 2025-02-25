using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private void ShowInfo(string error)
        {
            Visibility nmLastState = noMods.Visibility;
            Visibility nmfLastState = noModsFolder.Visibility;
            noMods.Visibility = Visibility.Collapsed;
            noModsFolder.Visibility = Visibility.Collapsed;

            InfoBar info = new InfoBar();
            TextBlock text = new TextBlock();
            text.Text = error;

            info.IsOpen = true;
            info.Content = text;
            info.MaxWidth = this.ActualWidth;
            text.TextWrapping = TextWrapping.WrapWholeWords;

            info.Closed += (InfoBar sender, InfoBarClosedEventArgs args) =>
            {
                noMods.Visibility = nmLastState;
                noModsFolder.Visibility = nmfLastState;
            };

            cModList.Children.Insert(0, info);
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
                ShowInfo($"Couldn't create mod folder\n{ex.Message}");
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
                ShowInfo($"Couldn't open mod folder\n{ex.Message}");
                Logger.WriteError($"Couldn't open mod folder path {_modsPath}");
                Logger.WriteException(ex);
            }
        }

    }
}
