using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace yt_dlp_GUI_dotnet8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IProgress<DownloadProgress> progress;
        private string cookieBrowser;
        private string videoFormat = "h265";
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
            if (File.Exists(@".\Cookies.txt"))
            {
                using (StreamReader sm = new StreamReader(@".\Cookies.txt"))
                {
                    cookieBrowser = sm.ReadToEnd();
                }
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            StackPanel_Info.MaxHeight = this.Height;
        }

        public static void ShowNotif(string title, string body)
        {
            new ToastContentBuilder()
                        .AddText(title)
                        .AddText(body)
                        .SetToastDuration(ToastDuration.Short)
                        .SetToastScenario(ToastScenario.Default)
                        .Show();
        }

        public string folder = "none";
        public bool cancel = false;
        public Task run;
        private void DownloadAsync(string url)
        {
            var ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = @".\yt-dlp.exe";
            ytdl.FFmpegPath = @".\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe";
            if (folder == "none")
            {
                var dlg = new CommonOpenFileDialog();
                dlg.IsFolderPicker = true;
                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ytdl.OutputFolder = dlg.FileName;
                    folder = dlg.FileName;
                    DownloadVideo(url, ytdl, DownloadMergeFormat.Mkv);
                }
                else
                {
                    cancel = true;
                    MessageBox.Show("キャンセルされました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }


            }
            else
            {
                ytdl.OutputFolder = folder;

                DownloadVideo(url, ytdl, DownloadMergeFormat.Mkv);
            }

        }
        public CancellationTokenSource cts;
        private void DownloadVideo(string url, YoutubeDL ytdl, DownloadMergeFormat format)
        {
            var options = new OptionSet()
            {
                Format = $"bestvideo+251/bestvideo+bestaudio/best",
                FormatSort = $"vcodec:{videoFormat}",
                AudioFormat = AudioConversionFormat.Aac,
                WriteThumbnail = true,
                WriteSubs = true,
                WriteAutoSubs = true,
                WriteInfoJson = true,
                WriteWeblocLink = true,
                MergeOutputFormat = DownloadMergeFormat.Mp4,
                EmbedChapters = true,
                EmbedInfoJson = true,
                EmbedSubs = true,
                EmbedMetadata = true,
                EmbedThumbnail = true/*,
                Cookies = cookieBrowser*/

            };
            //--format bestvideo+251/bestvideo+bestaudio/best --embed-subs --embed-thumbnail --all-subs --merge-output-format {mp4_mkv} --all-subs --embed-subs --embed-thumbnail --xattrs --add-metadata -i -ciw -o \"{saveFolder}\\%(title)s\" {_u} 19
            run = Task.Run(() =>
            {
                cts = new CancellationTokenSource();
                this.Dispatcher.Invoke((Action)(() =>
                {
                    ytdl.RunVideoDownload(url, progress: progress, ct: cts.Token, overrideOptions: options);

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
            if (!File.Exists(@".\yt-dlp.exe") || !File.Exists(@".\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe"))
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
                        //ここでyt-dlpとffmpegダウンロードする
                        FileDownloader fld = new FileDownloader();

                        var ytdlp = await fld.GetContent("https://github.com/yt-dlp/yt-dlp/releases/download/2023.12.30/yt-dlp.exe");
                        using (FileStream fs = new FileStream(@".\yt-dlp.exe", FileMode.Create))
                        {
                            //ファイルに書き込む
                            ytdlp.WriteTo(fs);
                            ytdlp.Close();
                            ShowNotif("Download Done!", "YT-DLPのダウンロードが終わりました");
                        }

                        var ffmpeg = await fld.GetContent("https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip");
                        ZipFile.ExtractToDirectory(ffmpeg, @".\");
                        ffmpeg.Close();
                        ShowNotif("Download Done!", "FFMPEGのダウンロードが終わりました");

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                }
            }
        }
        int count = 1;
        private void showProgress(DownloadProgress p)
        {
            txtState.Content = p.State.ToString();
            prog.Value = p.Progress;
            int a = (int)(p.Progress * 100);
            progText.Content = $"speed: {p.DownloadSpeed} | left: {p.ETA} | %: {a}%";
            if (p.State.ToString() == "Success")
            {
                if (dLLists.Count != count)
                {
                    count += 1;
                    Debug.WriteLine($"Count::{count - 1}");
                    var b = ((DLList)list.Items[count - 1]).url;
                    Debug.WriteLine(b);
                    DownloadAsync(b);
                }else
                {
                    ShowNotif("All Done!", "おわったお");
                    list.ClearValue(ItemsControl.ItemsSourceProperty);
                    folder = "none";
                    count = 0;
                    progText.Content = "Download States";
                    txtState.Content = "States";
                    dLLists.Clear();
                }

            }
            else if (p.State.ToString() == "Error")
            {
                MessageBox.Show("失敗");
                Debug.WriteLine(p.State.ToString());
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //if()
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
            Cookies.IsEnabled = true;
        }

        private void cookie_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.IsEnabled = false;
            Cookies.IsEnabled = false;

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
                try
                {
                    foreach (var url in urls)
                    {

                        if (IsValidUrl(url))
                        {
                            dLLists.Add(new DLList { url = url });
                        }
                    }
                    list.ItemsSource = dLLists;
                }
                catch(Exception ex) 
                {
                    MessageBox.Show("使用できない文字列が入っているか、値が無効です。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                }   
            }

        }
        public static bool IsValidUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(@".\Cookies.txt"))
            {
                using (StreamReader sm = new StreamReader(@".\Cookies.txt"))
                {
                    cookieBrowser = sm.ReadToEnd();
                }
            }
            if (list.Items.Count == 0)
            {
                MessageBox.Show("URLを追加してください", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (cancel == true)
                {
                    cancel = false;
                    folder = "none";

                }
                var a = ((DLList)list.Items[0]).url;
                Debug.WriteLine(a);
                DownloadAsync(a);
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
            if (SearchBox_Info.Text != "" && SearchBox_Info.Text.Contains("https://"))
            {
                GetInfomation get = new GetInfomation();
                var video = await get.Infomation(SearchBox_Info.Text);
                BitmapImage bti = new BitmapImage(new Uri(video.Thumbnail));
                thumbPic.Source = bti;
                VideoTitle.Header = video.Title;
                VidInfo.Text = video.ToString();

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

        private void Cookies_Clicked_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password != "")
            {
                using (StreamWriter sw = new StreamWriter(@".\Cookies.txt", false))
                {
                    sw.WriteLine(PasswordBox.Password);
                }
            }
        }

        private void combo_DropDownClosed(object sender, EventArgs e)
        {
            string sel = ((ComboBox)sender).Text;
            Debug.WriteLine(sel);
        }

        private void codec_DropDownClosed(object sender, EventArgs e)
        {
            string sel = ((ComboBox)sender).Text;
            Debug.WriteLine(sel);
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
                dLLists.Clear();
                ShowNotif("Cancel", "キャンセルされたお");
                list.ClearValue(ItemsControl.ItemsSourceProperty);
                folder = "none";
                count = 0;
                progText.Content = "Download States";
                txtState.Content = "States";
            }
        }

        private void codec_Audio_DropDownClosed(object sender, EventArgs e)
        {
            string sel = ((ComboBox)sender).Text;
            Debug.WriteLine(sel);
        }

        private void Codec_Audio_Toggle_Checked(object sender, RoutedEventArgs e)
        {
            codec_Audio.IsEnabled = true;
        }

        private void Codec_Audio_Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            codec_Audio.IsEnabled = false;
        }
    }
}