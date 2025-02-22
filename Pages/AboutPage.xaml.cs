using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using WinDurango.UI.Utils;


namespace WinDurango.UI.Pages
{
    public sealed partial class AboutPage : Page
    {
        private static string GetGitHubPfp(string username)
        {
            return $"https://github.com/{username}.png";
        }

        public AboutPage()
        {
            this.InitializeComponent();
            dexrn_Info.developerPicture.ProfilePicture = new BitmapImage(new Uri(GetGitHubPfp("DexrnZacAttack")));
            dexrn_Info.developerName.Content = "DexrnZacAttack";
            dexrn_Info.developerName.NavigateUri = new Uri("https://github.com/DexrnZacAttack");
            dexrn_Info.developerInfo.Text = "UI design, functionality, learning C#";
            danilwhale_Info.developerPicture.ProfilePicture = new BitmapImage(new Uri(GetGitHubPfp("danilwhale")));
            danilwhale_Info.developerName.Content = "danilwhale";
            danilwhale_Info.developerName.NavigateUri = new Uri("https://github.com/danilwhale");
            danilwhale_Info.developerInfo.Text = "Refactoring, teaching, bug fixing, etc";

            windurango_Info.developerInfo.Text = $"Version {App.Version}";
            windurango_Info.developerName.Content = "WinDurango.UI";
            windurango_Info.developerName.NavigateUri = new Uri("https://github.com/WinDurango/WinDurango.UI");
            windurango_Info.developerPicture.ProfilePicture = new BitmapImage(new Uri(GetGitHubPfp("WinDurango")));
        }
    }
}
