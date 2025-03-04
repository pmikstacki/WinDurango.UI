using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Gaming.Input;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinDurango.UI.Controls;
using WinDurango.UI.Dialogs;
using WinDurango.UI.Settings;
using WinDurango.UI.Utils;
using static WinDurango.UI.Localization.Locale;

namespace WinDurango.UI.Pages
{
    public sealed partial class AppsListPage : Page
    {
        private Gamepad gamepad;
        private int currentIndex;
        private Point currentPoint = new(0, 0);
        private bool inputProcessed = true;
        private long lastInput;
        
        public async Task InitAppListAsync()
        {
            appList.Children.Clear();
            SwitchScrollDirection(App.Settings.Settings.AppViewIsHorizontalScrolling);

            List<installedPackage> installedPackages = await Task.Run(() => App.InstalledPackages.GetPackages());
            PackageManager pm = new();

            foreach (installedPackage installedPackage in installedPackages)
            {
                if (this.SearchBox.Text.Length > 0)
                {
                    // Maybe we should at some point save the package Name/DisplayName to installedPackage model too? to skip this step
                    Package pk = Packages.GetPackageByFamilyName(installedPackage.FamilyName);
                    if (pk != null)
                    {
                        string searchMatch = "";
                        try
                        {
                            searchMatch = pk.DisplayName ?? pk.Id.Name;
                        }
                        catch
                        {
                            searchMatch = pk.Id.Name;
                        }
                        if (searchMatch.Contains(this.SearchBox.Text, StringComparison.InvariantCultureIgnoreCase) == false) continue;
                    }
                }

                // TODO: add handling for that annoying invalid logo stuff
                if (pm.FindPackageForUser(WindowsIdentity.GetCurrent().User?.Value, installedPackage.FullName) != null)
                {
                    try
                    {
                        AppTile gameContainer = new(installedPackage.FamilyName);

                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            appList.Children.Add(gameContainer);
                        });

                        Logger.WriteDebug($"Added {installedPackage.FamilyName} to the app list");
                    }
                    catch (Exception ex)
                    {
                        // maybe should have infobar on app list?
                        Logger.WriteError($"Failed to add {installedPackage.FamilyName} to the app list: {ex.Message}");
                        Logger.WriteException(ex);
                    }
                }
                else
                {
                    Logger.WriteError($"Couldn't find package {installedPackage.FullName} in installed UWP packages list");
                }
            }
        }

        private async void ShowAppListView(object sender, RoutedEventArgs e)
        {
            List<Windows.ApplicationModel.Package> uwpApps = Packages.GetInstalledPackages().ToList();
            if (uwpApps.Count <= 0)
            {
                NoticeDialog dialog = new NoticeDialog("No UWP Apps have been found.");
                await dialog.ShowAsync();
                return;
            }
            AppListDialog dl = new(uwpApps, true);
            dl.Title = "Installed UWP apps";
            dl.XamlRoot = this.Content.XamlRoot;
            await dl.ShowAsync();
        }

        private async void ShowInstalledEraApps(object sender, RoutedEventArgs e)
        {
            List<Windows.ApplicationModel.Package> eraApps = XHandler.GetXPackages(Packages.GetInstalledPackages().ToList());
            if (eraApps.Count <= 0)
            {
                NoticeDialog dialog = new NoticeDialog("No Era/XUWP Apps have been found.");
                await dialog.ShowAsync();
                return;
            }
            AppListDialog dl = new(eraApps, true);
            dl.Title = "Installed Era/XUWP apps";
            dl.XamlRoot = this.Content.XamlRoot;
            await dl.ShowAsync();
        }

        public void SwitchScrollDirection(bool horizontal)
        {
            if (horizontal)
            {
                scrollView.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollView.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                appList.Orientation = Orientation.Vertical;
                appList.VerticalAlignment = VerticalAlignment.Center;
            } else
            {
                scrollView.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scrollView.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                appList.Orientation = Orientation.Horizontal;
                appList.VerticalAlignment = VerticalAlignment.Top;
            }
        }

        private void UpdateCheckboxes(object sender, RoutedEventArgs e)
        {
            if (autoSymlinkCheckBox == null || addToAppListCheckBox == null)
                return;

            autoSymlinkCheckBox.IsEnabled = (bool)addToAppListCheckBox.IsChecked;
        }

        public AppsListPage()
        {
            InitializeComponent();
            _ = InitAppListAsync();

            Loaded += OnAppListPage_Loaded;
        }
        
        private void OnAppListPage_Loaded(object sender, RoutedEventArgs e)
        {
            Gamepad.GamepadAdded += OnGamepadAdded;
            Gamepad.GamepadRemoved += OnGamepadRemoved;
        }
        
        private void OnGamepadRemoved(object sender, Gamepad e)
        {
            Logger.WriteInformation("Controller disconnected");
            gamepad = null;
            this.DispatcherQueue.TryEnqueue(() =>
            {
                App.MainWindow.SwitchMode(MainWindow.AppMode.DESKTOP);
            });
        }

        private void OnGamepadAdded(object sender, Gamepad e)
        {
            Logger.WriteInformation("Controller connected");
            gamepad = e;
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (appList.Children.Count > 0)
                {
                    appList.Children[currentIndex].Focus(FocusState.Keyboard);
                }
                App.MainWindow.SwitchMode(MainWindow.AppMode.CONTROLLER);
            });
            ListenGamepadInput();
        }


        // can we make this work everywhere, like in content dialogs?
        private async void ListenGamepadInput()
        {
            while (gamepad != null)
            {
                GamepadReading gamepadInput = gamepad.GetCurrentReading();
                bool moveRight = gamepadInput.LeftThumbstickX > 0.5 || (gamepadInput.Buttons & GamepadButtons.DPadRight) != 0;
                bool moveLeft = gamepadInput.LeftThumbstickX < -0.5 || (gamepadInput.Buttons & GamepadButtons.DPadLeft) != 0;
                bool moveUp = gamepadInput.LeftThumbstickY > 0.5 || (gamepadInput.Buttons & GamepadButtons.DPadUp) != 0;
                bool moveDown = gamepadInput.LeftThumbstickY < -0.5 || (gamepadInput.Buttons & GamepadButtons.DPadDown) != 0;
                bool start = (gamepadInput.Buttons & GamepadButtons.Menu) != 0; // start as in the button, not start package
                bool view = (gamepadInput.Buttons & GamepadButtons.View) != 0; // TODO: on click it should switch between the navigationview, bottom docked bar, and apps list (needs handling for other pages)
                bool actionClicked = (gamepadInput.Buttons & GamepadButtons.A) != 0;

                // feel like we should have like event listeners or whatever
                // actionClicked += whatever
                if (actionClicked && inputProcessed)
                {
                    inputProcessed = false;
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        var appTile = appList.Children[currentIndex] as AppTile;
                        appTile.StartApp();
                        inputProcessed = true;
                    });
                }

                // disabled until controller support works
                // also pressing start twice will crash cuz 2 contentdialogs
                //if (start && inputProcessed)
                //{
                //    inputProcessed = false;
                //    this.DispatcherQueue.TryEnqueue(() =>
                //    {
                //        var appTile = appList.Children[currentIndex] as AppTile;
                //        appTile.ShowControllerInteractDialog();
                //        inputProcessed = true;
                //    });
                //}

                if ((moveRight || moveLeft || moveUp || moveDown) && inputProcessed)
                {
                    inputProcessed = false;
                    if (moveRight) this.DispatcherQueue.TryEnqueue(() => MoveFocus(1, 0));
                    else if (moveLeft) this.DispatcherQueue.TryEnqueue(() => MoveFocus(-1, 0));
                    else if (moveUp) this.DispatcherQueue.TryEnqueue(() => MoveFocus(0, -1));
                    else if (moveDown) this.DispatcherQueue.TryEnqueue(() => MoveFocus(0, 1));
                }

                await Task.Delay(100);
            }
        }

        private void MoveFocus(int xOffset, int yOffset)
        {
            bool firstInput = lastInput == 0;
            if (lastInput > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 10)
            {
                inputProcessed = true;
                return;
            }
            lastInput = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (appList.Children.Count == 0)
            {
                inputProcessed = true;
                return;
            }

            // We only set focus first to the index 0 if ShowDevNotice was shown before as it cancels the initial focus done in Load event
            if (firstInput && App.Settings.Settings.ShowDevNotice)
            {
                appList.Children[0].Focus(FocusState.Keyboard);
                inputProcessed = true;
                return;
            }
            
            int columns = GetColumnCount();
            int rows = appList.Children.Count / columns;

            int newX = (int)(currentPoint.X + xOffset);
            int newY = (int)(currentPoint.Y + yOffset);

            newX = Math.Clamp(newX, 0, columns - 1);
            newY = Math.Clamp(newY, 0, rows - 1);

            if (newX != currentPoint.X || newY != currentPoint.Y)
            {
                currentPoint = new Point(newX, newY);
                currentIndex = newY * columns + newX;

                if (currentIndex < appList.Children.Count)
                {
                    appList.Children[currentIndex].Focus(FocusState.Keyboard);
                }
            }

            inputProcessed = true;
        }

        // We need do this our self as WrapPanel doesn't have internal field or function to get current column amount
        private int GetColumnCount()
        {
            if (appList.Children.Count == 0) return 1;


            FrameworkElement firstItem = appList.Children[0] as FrameworkElement;
            if (firstItem == null) return 1;


            double firstItemTop = firstItem.TransformToVisual(appList).TransformPoint(new Point(0, 0)).Y;
            int columnCount = 1;

            for (int i = 1; i < appList.Children.Count; i++)
            {
                var item = appList.Children[i] as FrameworkElement;
                if (item == null)
                    continue;

                double itemTop = item.TransformToVisual(appList).TransformPoint(new Point(0, 0)).Y;

                if (Math.Abs(itemTop - firstItemTop) < 1)
                {
                    columnCount++;
                }
                else
                {
                    break;
                }
            }

            return columnCount;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            _ = InitAppListAsync();
        }

        // needs to be cleaned
        private async void InstallButton_Tapped(SplitButton sender, SplitButtonClickEventArgs args)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);

            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                string manifest = Path.Combine(folder.Path + "\\AppxManifest.xml");
                string mountFolder = Path.Combine(folder.Path + "\\Mount");

                if (File.Exists(manifest))
                {
                    var dialog = new InstallConfirmationDialog(manifest);
                    dialog.PrimaryButtonClick += async (sender, e) =>
                    {
                        dialog.Hide();
                        var controller = new ProgressDialog("Starting installation...", $"Installing {Packages.GetPropertiesFromManifest(manifest).DisplayName}", isIndeterminate: false).GetController();
                        _ = controller.CreateAsync(async () =>
                        {
                            await Packages.InstallPackageAsync(new Uri(manifest, UriKind.Absolute), controller,
                                (bool)addToAppListCheckBox.IsChecked);
                        });
                    };
                    await dialog.ShowAsync();
                }
                else
                {
                    // AppxManifest does not exist in that folder
                    if (Directory.Exists(mountFolder))
                    {
                        // there IS a mount folder
                        if (File.Exists(Path.Combine(mountFolder + "\\AppxManifest.xml")))
                        {
                            var dialog = new InstallConfirmationDialog(Path.Combine(mountFolder + "\\AppxManifest.xml"));
                            dialog.PrimaryButtonClick += async (sender, e) =>
                            {
                                dialog.Hide();
                                var controller = new ProgressDialog("Starting installation...", "Installing", isIndeterminate: false).GetController();
                                _ = controller.CreateAsync(async () =>
                                {
                                    await Packages.InstallXPackageAsync(folder.Path.ToString(), controller,
                                        (bool)addToAppListCheckBox.IsChecked);
                                });
                            };
                            await dialog.ShowAsync();
                        }
                        else
                        {
                            // there is no AppxManifest inside.
                            Logger.WriteError($"Could not find AppxManifest.xml in {folder.Path} and {mountFolder}");
                            await new NoticeDialog(GetLocalizedText("/Packages/ManifestNotFoundMulti", folder.Path, mountFolder), "Error").ShowAsync();
                        }
                    }
                    else
                    {
                        Logger.WriteError($"Could not find AppxManifest.xml in {folder.Path} and no Mount folder exists");
                        await new NoticeDialog(GetLocalizedText("/Packages/ManifestNotFoundNoMount", folder.Path), "Error").ShowAsync();
                    }

                    return;
                }
            }
        }
    }
}
