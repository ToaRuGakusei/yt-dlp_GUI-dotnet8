using System.Windows;

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
            Bar.Maximum = 100;
            Bar.Minimum = 0;
        }
        private async Task LoopTask() => await Task.Run(() =>
                                            {
                                                bool end = false;
                                                string name = "";
                                                while (true)
                                                {
                                                    this.Dispatcher.Invoke((Action)(() =>
                                                    {
                                                        downloadRead.Content = FileDownloader.WhatName + "ダウンロード中";
                                                        downloadBytes.Content = (FileDownloader.TotalBytes) + "b";
                                                    }));
                                                    this.Dispatcher.Invoke((Action)(() =>
                                                    {
                                                        Bar.Value = FileDownloader.now;
                                                        downloadLabel.Content = $"{Bar.Value:F0}%";
                                                        name = FileDownloader.WhatName;
                                                        end = FileDownloader.IsEnd;
                                                    }));
                                                    if (name == "ffmpeg" && end == true)
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
