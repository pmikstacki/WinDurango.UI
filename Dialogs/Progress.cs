using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinDurango.UI.Dialogs
{
    public class ProgressDialog : ContentDialog
    {
        private string _text;
        private string _title;
        private double _progress;
        private bool _isIndeterminate;
        private ProgressBar _progressBar;
        private TextBlock _textBlock;
        private readonly ProgressController _controller;

        public ProgressDialog(string content, string title = "Information", bool isIndeterminate = true)
        {
            _controller = new ProgressController(this);
            _text = content;
            _title = title;
            _isIndeterminate = isIndeterminate;

            _progressBar = new ProgressBar
            {
                IsIndeterminate = _isIndeterminate,
                Width = 300,
                Value = _progress
            };

            _textBlock = new TextBlock
            {
                Text = _text,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Content = new Grid
            {
                RowDefinitions =
                {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                        new RowDefinition { Height = GridLength.Auto }
                },
                Children =
                {
                    _textBlock,
                    _progressBar
                }
            };

            Grid.SetRow(_progressBar, 2);
            Title = _title;
            XamlRoot = App.MainWindow.Content.XamlRoot;
        }

        public ProgressController GetController() { return _controller; }

        public string PTitle
        {
            get => _title;
            set
            {
                _title = value;
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _textBlock.Text = value;
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                if (_isIndeterminate)
                    _isIndeterminate = false;
                _progress = value;
                _progressBar.Value = value;
            }
        }
    }
}
