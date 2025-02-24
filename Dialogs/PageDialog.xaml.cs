using Microsoft.UI.Xaml.Controls;
using System;

namespace WinDurango.UI.Dialogs
{
    public sealed partial class PageDialog : ContentDialog
    {
        public PageDialog(Type pg, object p, string title = "PageDialog")
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.Title = title;
            //this.MaxWidth = 1200;
            this.frame.Navigate(pg, p);
        }
    }
}
