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
using WinDurango.UI.Utils;
using System.Diagnostics;
using WinDurango.UI.Pages.Dialog;

namespace WinDurango.UI.Controls
{
    public sealed partial class ContainerInfo : UserControl
    {
        private string _folder;
        private DirectoryInfo _dir;
        private string _displayName;
        private bool _isContainer;

        public ContainerInfo(string folder, string displayName, bool isContainer = false)
        {
            this.InitializeComponent();
            this._isContainer = isContainer;
            this._folder = folder;
            this._dir = new DirectoryInfo(_folder);
            this._displayName = displayName;

            // could allow for folders to be renamed but user can do that (not like they can't change the text in the file but atleast it's slightly more user friendly this way)
            if (!isContainer)
                this.renameButton.Visibility = Visibility.Collapsed;

            this.name.Text = _displayName;

            string folderName = Path.GetFileName(_folder);
            if (folderName == _displayName)
            {
                this.folderName.Visibility = Visibility.Collapsed;
            }
            Logger.WriteDebug(folderName);
            this.folderName.Text = Path.GetFileName(_folder);
            long dirSize = _dir.GetDirectorySize();
            // can't wait for the funny inaccuracy as windows probably uses some weird KiB type or whatever
            this.folderSize.Text = dirSize.GetSizeString();

            ToolTipService.SetToolTip(this.folderName, folderName);
            ToolTipService.SetToolTip(this.name, _displayName);
            ToolTipService.SetToolTip(this.folderSize, $"{dirSize} bytes");
        }

        private void DeleteContainer(object sender, RoutedEventArgs e)
        {
            Flyout flyout = new Flyout();
            TextBlock title = new TextBlock { Text = $"Are you sure you want to delete {_displayName}?" };
            TextBlock info = new TextBlock { Text = $"This save will be deleted from the disk and may not be recoverable." };
            title.Style = (Style)Application.Current.Resources["BaseTextBlockStyle"];
            Button button = new Button();
            button.Content = "Delete";
            button.Margin = new Thickness(0, 10, 0, 0);

            button.Click += (s, e) =>
            {
                flyout.Hide();
                var parent = VisualTreeHelper.GetParent(this);
                while (parent != null && !(parent is SaveManagerPage))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                if (parent != null)
                {
                    SaveManagerPage smParent = (SaveManagerPage)parent;
                    try
                    {
                        Directory.Delete(_folder, true);
                    } catch (Exception ex)
                    {
                        smParent.ShowInfo($"Failed to delete {_displayName}", $"{ex.Message}", InfoBarSeverity.Error);
                        return;
                    }
                    smParent.RemoveElement(this);
                }
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

        private void ViewFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                FSHelper.OpenFolder(_folder);
            }
            catch (Exception ex)
            {
                var parent = VisualTreeHelper.GetParent(this);
                while (parent != null && !(parent is SaveManagerPage))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                if (parent != null)
                    ((SaveManagerPage)parent).ShowInfo($"Couldn't open folder {Path.GetFileName(_folder)}", $"{ex.Message}", InfoBarSeverity.Error);

                Logger.WriteError($"Couldn't open container folder path {_folder}");
                Logger.WriteException(ex);
            }
        }

        private void RenameContainer(string name)
        {
            string path = Path.Combine(_folder, "wd_displayname.txt");
            if (File.Exists(path))
            {
                File.WriteAllText(path, name);
                this._displayName = name;
                this.name.Text = _displayName;
            }
        }

        private void rename(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is SaveManagerPage))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
            {
                SaveManagerPage smParent = (SaveManagerPage)parent;
                try
                {
                    RenameContainer(((TextBox)sender).Text);
                }
                catch (Exception ex)
                {
                    smParent.ShowInfo($"Failed to rename {_displayName}", $"{ex.Message}", InfoBarSeverity.Error);
                    return;
                }
            }
        }

        private void renameButton_Click(object sender, RoutedEventArgs e)
        {
            Flyout flyout = new Flyout();
            TextBlock title = new TextBlock { Text = $"Rename {_displayName}" };
            TextBox box = new TextBox { Text = _displayName };
            Button button = new Button();
            box.KeyDown += (sender, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter) 
                {
                    flyout.Hide();
                    rename(sender, e);  
                }
            };

            title.Style = (Style)Application.Current.Resources["BaseTextBlockStyle"];

            button.Content = "Rename";
            button.Margin = new Thickness(0, 10, 0, 0);
            button.Click += (sender, e) =>
            {
                flyout.Hide();
                rename(button, new RoutedEventArgs());
            };

            flyout.Content = new StackPanel
            {
                Children =
                {
                    title,
                    new StackPanel
                    {
                        Children = {
                            box,
                            button
                        }
                    }
                }
            };

            flyout.ShowAt((Button)sender);
        }
    }
}
