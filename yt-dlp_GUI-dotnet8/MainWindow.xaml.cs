using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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
            SizeChanged += MainWindow_SizeChanged;

        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            StackPanel_Info.MaxHeight = this.Height;
        }

        //
        private VideoData video;
        FormatData[] formats;
        public string folder ="none";
        public bool cancel = false;
        private void DownloadAsync(string url)
        {
            var ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = @".\yt-dlp.exe";
            ytdl.FFmpegPath = @".\ffmpeg.exe";
            if (folder == "none")
            {
                var dlg = new CommonOpenFileDialog();
                dlg.IsFolderPicker = true;
                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ytdl.OutputFolder = dlg.FileName;
                    folder = dlg.FileName;
                    DownloadVideo(url, ytdl);
                }
                else
                {
                    cancel = true;
                    MessageBox.Show("キャンセルされました。","情報",MessageBoxButton.OK, MessageBoxImage.Information);
                }


            }
            else
            {
                ytdl.OutputFolder = folder;

                DownloadVideo(url, ytdl);
            }

        }

        private void DownloadVideo(string url, YoutubeDL ytdl)
        {
            var run = Task.Run(() =>
            {
                var cts = new CancellationTokenSource();
                this.Dispatcher.Invoke((Action)(() =>
                {
                    ytdl.RunVideoDownload(url,
                               progress: progress, ct: cts.Token);

                }));
                /*foreach( var format in formats )
                {
                    Debug.WriteLine(format);
                }*/

            });
            if (run.Status == TaskStatus.Running)
            {
                run.Wait();
            }
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
        int count = 0;
        private void showProgress(DownloadProgress p)
        {
            txtState.Content = p.State.ToString();
            prog.Value = p.Progress;
            int a = (int)(p.Progress * 100);
            progText.Content = $"speed: {p.DownloadSpeed} | left: {p.ETA} | %: {a}%";
            if (p.State.ToString() == "Success")
            {
                count += 1;
                Debug.WriteLine($"Count::{count}");
            }
            else if(p.State.ToString() == "Error")
            {
                MessageBox.Show("失敗");
            }
            if(count == list.Items.Count)
            {
                var done = MessageBox.Show("All Done!","情報",MessageBoxButton.OK, MessageBoxImage.Information);
                if(done == MessageBoxResult.OK)
                {
                    list.ClearValue(ItemsControl.ItemsSourceProperty); 
                    folder = "none";
                    count = 0;
                    progText.Content = "Download States";
                    txtState.Content = "States";

                }

            }
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
            //DownloadAsync();
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
        private void Add_Url_List(object sender, RoutedEventArgs e)
        {
            AddUrl addURl = new AddUrl();
            addURl.ShowDialog();
            var urls = addURl.urls;
            if (urls != null)
            {
                foreach (var url in urls)
                {
                    dLLists.Add(new DLList { url = url });
                }
                list.ItemsSource = dLLists;
            }
            else
            {
                MessageBox.Show("使用できない文字列が入っているか、値が無効です。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (list.Items.Count == 0)
            {
                MessageBox.Show("URLを追加してください", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                foreach (var r in list.Items)
                {
                    if (cancel == true)
                    {
                        cancel = false;
                        folder = "none";
                        break;

                    }
                    var a = (r as DLList)?.url;
                    Debug.WriteLine(a);
                    DownloadAsync(a);

                }
            }
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
        private async void Info_Search(object sender, RoutedEventArgs e)
        {
            var ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = @".\yt-dlp.exe";
            ytdl.FFmpegPath = @".\ffmpeg.exe";
            if (SearchBox_Info.Text != "" && SearchBox_Info.Text.Contains("https://"))
            {
                try
                {
                    var res = await ytdl.RunVideoDataFetch(SearchBox_Info.Text);
                    // get some video information
                    video = res.Data;
                    BitmapImage bti = new BitmapImage(new Uri(video.Thumbnail));
                    thumbPic.Source = bti;
                    VideoTitle.Header = video.Title;
                    VidInfo.Text = video.ToString();

                }
                catch(Exception ex)
                {
                    MessageBox.Show("何らかのエラーにより、処理ができませんでした。\nURLが正しいかご確認ください。");
                }

            }
            else
            {
                MessageBox.Show("URLを入力してください!", "注意", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void CodecToggle_Checked(object sender, RoutedEventArgs e)
        {
            codec.IsEnabled = true;
        }

        private void CodecToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            codec.IsEnabled = false;
        }
    }
}