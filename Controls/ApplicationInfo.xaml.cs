using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinDurango.UI.Utils;

namespace WinDurango.UI.Controls
{
    public sealed partial class ApplicationInfo : UserControl
    {
        public ApplicationInfo()
        {
            this.InitializeComponent();
            this.appPicture.ProfilePicture = new BitmapImage(new System.Uri("https://avatars.githubusercontent.com/WinDurango"));
            this.appInfo.Text = $"Version {App.Version}";
            GetRepoInfo();
        }

        public async void GetRepoInfo()
        {
            using HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHubAPI/1.0)");
                string res = await client.GetStringAsync("https://api.github.com/repos/WinDurango/WinDurango.UI");
                using (JsonDocument doc = JsonDocument.Parse(res))
                {
                    JsonElement root = doc.RootElement;
                    int stars = root.GetProperty("stargazers_count").GetInt32();
                    int forks = root.GetProperty("forks").GetInt32();
                    int watchers = root.GetProperty("subscribers_count").GetInt32();

                    this.stars.Content = stars.ToString();
                    this.forks.Content = forks.ToString();
                    this.watchers.Content = watchers.ToString();
                }
            }
            catch (System.Exception ex)
            {
                Logger.WriteError("Couldn't fetch https://api.github.com/WinDurango/WinDurango.UI");
                Logger.WriteException(ex);
            }
        }
    }
}
