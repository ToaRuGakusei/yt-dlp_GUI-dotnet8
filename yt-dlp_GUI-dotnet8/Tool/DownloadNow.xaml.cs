using MaterialDesignThemes.Wpf;
using System.Windows;
using yt_dlp_GUI_dotnet8.Tool;
namespace yt_dlp_GUI_dotnet8
{
    /// <summary>
    /// DownloadNow.xaml の相互作用ロジック
    /// </summary>
    public partial class DownloadNow : Window
    {
        public DownloadNow()
        {
            InitializeComponent();
            ChangeTheme();
            Bar.Maximum = 100;
            Bar.Minimum = 0;
        }
        private static void ChangeTheme()
        {
            PaletteHelper palette = new PaletteHelper();
            ITheme theme = palette.GetTheme();
            theme.SetBaseTheme(Theme.Dark);
            palette.SetTheme(theme);
        }
        /// <summary>
        /// プログレスバー関連
        /// </summary>
        /// <returns></returns>
        private async Task LoopTask() => await Task.Run(() =>
                                            {
                                                bool end = false;
                                                string name = String.Empty;
                                                string DownloadNow = String.Empty;
                                                while (true)
                                                {
                                                    this.Dispatcher.Invoke((Action)(() =>
                                                    {
                                                        DownloadNow = FileDownloader.WhatName + "をダウンロード中";
                                                        Title = DownloadNow;
                                                        downloadRead.Content = DownloadNow;
                                                        downloadBytes.Content = (FileDownloader.TotalBytes) + "b";
                                                    }));
                                                    this.Dispatcher.Invoke((Action)(() =>
                                                    {
                                                        Bar.Value = FileDownloader.now;
                                                        downloadLabel.Content = $"{Bar.Value:F0}%";
                                                    }));
                                                    this.Dispatcher.Invoke((Action)(() =>
                                                    {
                                                        name = FileDownloader.WhatName;
                                                        end = FileDownloader.IsEnd;
                                                    }));
                                                    if (name.Contains("ffmpeg") && end == true)
                                                    {
                                                        break;
                                                    }

                                                }
                                            });

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoopTask();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
