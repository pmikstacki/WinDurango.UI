using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace WinDurango.UI.Controls
{
    public sealed partial class ContributorInfo : UserControl
    {
        public ContributorInfo(string name, string pfp, string link)
        {
            this.InitializeComponent();
            this.developerName.Content = name;
            this.developerPicture.ProfilePicture = new BitmapImage(new Uri(pfp));
            this.developerName.NavigateUri = new Uri(link);
        }
    }
}
