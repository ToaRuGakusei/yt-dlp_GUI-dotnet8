using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace yt_dlp_GUI_dotnet8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IProgress<DownloadProgress> progress;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
            QuestionDownloadFirst();
            progress = new Progress<DownloadProgress>((p) => showProgress(p));
        }
        /// <summary>
        /// ここでWebView関連の設定を行っている
        /// </summary>
        private async void InitializeAsync()
        {
            //初期化完了時のイベント
            webview.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;

            await webview.EnsureCoreWebView2Async(null);
            //新しいタブで開かないようにする。
            webview.CoreWebView2.NewWindowRequested += NewWindowRequested;
            webview.CoreWebView2.SourceChanged += CompletedPage;


        }
        //
        private VideoData video;
        FormatData[] formats;

        private async void DownloadAsync()
        {
            var ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = @".\yt-dlp.exe";
            ytdl.FFmpegPath = @".\ffmpeg.exe";
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ytdl.OutputFolder = dlg.FileName;
            }

            await Task.Run(() =>
            {
                var cts = new CancellationTokenSource();
                this.Dispatcher.Invoke((Action)(() =>
                {
                    ytdl.RunVideoDownload(webview.CoreWebView2.Source,
                               progress: progress, ct: cts.Token);
                    /*var res = await ytdl.RunVideoDataFetch("https://www.youtube.com/watch?v=OSHNSq0rA3s");
                    // get some video information
                    video = res.Data;
                    Debug.WriteLine($"video={video}\ntitle={title}\nuploader={uploader}\nviews={views}\nformats={formats.ToString()}");
                    */
                }));
                /*foreach( var format in formats )
                {
                    Debug.WriteLine(format);
                }*/
                
            });
        }

        private async void QuestionDownloadFirst()
        {
            if (!File.Exists(@".\yt-dlp.exe") || !File.Exists(@".\ffmpeg.exe"))
            {
                var result = MessageBox.Show("yt-dlpまたはffmpegが見つかりませんでした。\nダウンロードしますか？", "情報", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                {
                    result = MessageBox.Show("ダウンロードしないとこのアプリは使用できません。\nこのアプリを終了しますか？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        this.Close();
                    }
                    else
                    {
                        QuestionDownloadFirst();
                    }
                }
                else
                {
                    try
                    {
                        await YoutubeDLSharp.Utils.DownloadYtDlp();
                        await YoutubeDLSharp.Utils.DownloadFFmpeg();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                }
            }


        }
        private void showProgress(DownloadProgress p)
        {
            txtState.Content = p.State.ToString();
            prog.Value = p.Progress;
            progText.Content = $"speed: {p.DownloadSpeed} | left: {p.ETA}";
        }

        private void NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            //新しいウィンドウを開かなくする
            e.Handled = true;

            //元々のWebView2でリンク先を開く
            webview.CoreWebView2.Navigate(e.Uri);
        }
        private void CompletedPage(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            SearchBox.Text = webview.CoreWebView2.Source;

        }

        private void WebView2_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            Debug.WriteLine("初期化完了");
            webview.CoreWebView2.Navigate("https://google.com");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OutlinedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //if()
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void right_Click(object sender, RoutedEventArgs e)
        {
            webview.GoForward();
        }

        private void reload_Click(object sender, RoutedEventArgs e)
        {
            bool a = false;
            if (a == true)
            {
                webview.Stop();
            }
            else
            {
                webview.Reload();
            }
        }

        private void left_Click(object sender, RoutedEventArgs e)
        {
            webview.GoBack();

        }

        private void paste_Click(object sender, RoutedEventArgs e)
        {

        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            DownloadAsync();
        }
        ObservableCollection<DLList> dLLists = new ObservableCollection<DLList>();
        private void download_Click_1(object sender, RoutedEventArgs e)
        {
            dLLists.Add(new DLList { url = webview.CoreWebView2.Source, name = "none" });
            list.ItemsSource = dLLists;
        }

        public class DLList()
        {
            public string url { get; set; }
            public string name { get; set; }

        }

        private void cookie_Checked(object sender, RoutedEventArgs e)
        {
            PasswordBox.IsEnabled = true;
        }

        private void cookie_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.IsEnabled = false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            dLLists.Clear();
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            DownloadAsync();
        }

        private void SetPixel_Checked(object sender, RoutedEventArgs e)
        {
            combo.IsEnabled = true;
        }

        private void SetPixel_Unchecked(object sender, RoutedEventArgs e)
        {
            combo.IsEnabled = false;
        }

        private void Search_Clicked(object sender, RoutedEventArgs e)
        {
            webview.CoreWebView2.Navigate(SearchBox.Text);
        }

        private void prog_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine(prog.Value.ToString());
        }
    }
}