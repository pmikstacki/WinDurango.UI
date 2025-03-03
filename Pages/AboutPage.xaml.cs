using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using WinDurango.UI.Controls;


namespace WinDurango.UI.Pages
{
    public sealed partial class AboutPage : Page
    {

        public AboutPage()
        {
            this.InitializeComponent();

            string[] lines = File.ReadAllLines("Assets/contributors.txt");
            foreach (var contributor in lines)
            {
                string[] info = contributor.Split(";");
                string name = info[0].Replace("WD_CONTRIB_SEMICOLON", ";");
                string avatar = info[1].Replace("WD_CONTRIB_SEMICOLON", ";");
                string link = info[2].Replace("WD_CONTRIB_SEMICOLON", ";");
                string contributionCount = info[3];

                contributorList.Children.Add(new ContributorInfo(name, avatar, link));
            }
        }
    }
}
