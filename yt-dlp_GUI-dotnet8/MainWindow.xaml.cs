using MaterialDesignThemes.Wpf;
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
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using yt_dlp_GUI_dotnet8.Tool;

namespace yt_dlp_GUI_dotnet8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //初期化（設定など）
        private IProgress<DownloadProgress> progress;
        private string cookieBrowser;
        private string videoFormat;
        private bool UseSettingsFile = false;
        private bool Cookies_Enabled = false;
        private bool SetPixel_Enabled = false;
        private bool Codec_Enabled = false;
        private bool Codec_Audio_Enabled = false;
        private int Cookie = 0;
        private int Pixel = 0;
        private int Codec = 0;
        private int Codec_Audio = 0;

        //初期化（ダウンロード関連）
        private CancellationTokenSource cts;
        private string folder = "none";
        private bool cancel = false;
        private Task run;
        private bool isEnd = false;
        private ObservableCollection<DLList> dLLists = new ObservableCollection<DLList>();
        private readonly string yt_dlp_Path = @".\yt-dlp.exe";
        private readonly string ffmpeg_Path = @".\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe";
        private string yt_dlp_Download_URL = "https://github.com/yt-dlp/yt-dlp/releases/download/2023.12.30/yt-dlp.exe";
        private string ffmpeg_Download_URL = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip";
        private AudioConversionFormat AudioConversion;
        private DownloadMergeFormat DownloadMerge;
        private string Cookies_Path = @".\Cookies.txt";
        public class DLList()
        {
            public string url { get; set; }
            public string name { get; set; }
        }
        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();//WebView2関連の設定をする
            QuestionDownloadFirst(); //ここでtoolの有無を確認（なければダウンロードするか聞く）
            Settings_Apply();//設定を反映させる

            progress = new Progress<DownloadProgress>((p) => showProgress(p));//進捗状況を反映するための式
            ChangeTheme();//

        }

        /// <summary>
        /// テーマの変更をここでする。
        /// テーマの仕組みがよくわからない。
        /// 2024/02/03作成
        /// </summary>
        private static void ChangeTheme()
        {
            PaletteHelper palette = new PaletteHelper();
            ITheme theme = palette.GetTheme();
            theme.SetBaseTheme(Theme.Dark);
            palette.SetTheme(theme);
        }

        /// <summary>
        /// ここで設定をロード
        /// もっときれいにしたい。xmlで設定を保存する方法勉強中
        /// 2024/02/03作成
        /// </summary>
        private void Settings_Apply()
        {
            //設定ファイルへのアクセスを制限
            UseSettingsFile = true;

            //設定ロード＾＾
            //ごり押ししすぎ
            SettingsLoader settingsLoader = new SettingsLoader();
            Cookies_Enabled = settingsLoader.SettingEnabled_Check("Cookies_Enabled") == "true" ? true : false;
            SetPixel_Enabled = settingsLoader.SettingEnabled_Check("resolution_Enabled") == "true" ? true : false;
            Codec_Enabled = settingsLoader.SettingEnabled_Check("codec_Enabled") == "true" ? true : false;
            Codec_Audio_Enabled = settingsLoader.SettingEnabled_Check("codec_Audio_Enabled") == "true" ? true : false;
            Cookie = int.Parse(settingsLoader.SettingGetter("Cookies"));
            Pixel = int.Parse(settingsLoader.SettingGetter("resolution"));
            Codec = int.Parse(settingsLoader.SettingGetter("codec"));
            Codec_Audio = int.Parse(settingsLoader.SettingGetter("codec_Audio"));

            //設定反映(´・ω・`)
            cookie.IsChecked = Cookies_Enabled;
            SetPixel.IsChecked = SetPixel_Enabled;
            CodecToggle.IsChecked = Codec_Enabled;
            Codec_Audio_Toggle.IsChecked = Codec_Audio_Enabled;

            switch (Codec_Audio)
            {
                case 0:
                    AudioConversion = AudioConversionFormat.Mp3;
                    break;
                case 1:
                    AudioConversion = AudioConversionFormat.Aac;
                    break;
                case 2:
                    AudioConversion = AudioConversionFormat.Flac;
                    break;
            }
            switch (Codec)
            {
                case 0:
                    videoFormat = "h264";
                    break;
                case 1:
                    videoFormat = "h265";
                    break;
                case 2:
                    videoFormat = "vp9";
                    break;
                case 3:
                    videoFormat = "av1";
                    break;
            }

            //-9は取得できなかったということで、何も表示させないために-1を代入させる。それ以外の場合はそのまま代入。
            combo.SelectedIndex = Pixel == -9 ? -1 : Pixel;
            codec.SelectedIndex = Codec == -9 ? -1 : Codec;
            codec_Audio.SelectedIndex = -9 == 114514 ? -1 : Codec_Audio;

            //設定ファイルへのアクセスを解放
            UseSettingsFile = false;
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

            if (System.IO.File.Exists(Cookies_Path))
            {
                using (StreamReader sm = new StreamReader(Cookies_Path))
                {
                    cookieBrowser = sm.ReadToEnd();
                }
            }
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
        //ここでオプションなどを設定し、DownloadVideoに値を渡す。
        private void DownloadAsync(string url)
        {
            var ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = yt_dlp_Path;
            ytdl.FFmpegPath = ffmpeg_Path;
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
                    MessageBox.Show("キャンセルされました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        private void DownloadVideo(string url, YoutubeDL ytdl)
        {
            var options = new OptionSet()
            {
                Format = $"bestvideo+251/bestvideo+bestaudio/best",
                FormatSort = $"vcodec:{videoFormat}",
                AudioFormat = AudioConversion,
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
                EmbedThumbnail = true
            };
            run = Task.Run(() =>
            {
                cts = new CancellationTokenSource();
                this.Dispatcher.Invoke((Action)(() =>
                {
                    ytdl.RunVideoDownload(url, progress: progress, ct: cts.Token, overrideOptions: options);

                }));
            });
        }

        /// <summary>
        /// MainWindowを無効化する
        /// </summary>
        private void Notouch()
        {
            this.IsEnabled = false; //MainWindowの画面を無効化
            isEnd = false;
            Task.Run(() =>
            {
                while (true)
                {
                    if (isEnd)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            this.IsEnabled = true;
                        }));
                        break;
                    }
                }
            });
        }
        /// <summary>
        /// toolの有無を確認
        /// </summary>
        private async void QuestionDownloadFirst()
        {
            if (!System.IO.File.Exists(@".\yt-dlp.exe") || !System.IO.File.Exists(@".\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe"))
            {
                var result = MessageBox.Show("yt-dlpまたはffmpegが見つかりませんでした。\nダウンロードしますか？", "情報", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                {
                    result = MessageBox.Show("ダウンロードしないとこのアプリは使用できません。\nこのアプリを終了しますか？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        this.Close(); //アプリを終了させる
                    }
                    else
                    {
                        QuestionDownloadFirst();//もう一度ダイアログを表示
                    }
                }
                else
                {
                    await DownloadTool();//toolのダウンロード開始
                }
            }
        }
        /// <summary>
        /// ここで必要なツールをダウンロード
        /// </summary>
        /// <returns></returns>
        private async Task DownloadTool()
        {
            try
            {
                //ここでyt-dlpとffmpegダウンロードする
                FileDownloader fld = new FileDownloader();
                DownloadNow dln = new DownloadNow();
                Notouch();
                dln.Show();
                await yt_dlp_Download(fld);
                await ffmpeg_Download(fld, dln);

                isEnd = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async Task ffmpeg_Download(FileDownloader fld, DownloadNow dln)
        {
            var ffmpeg = await fld.GetContent(ffmpeg_Download_URL);
            ZipFile.ExtractToDirectory(ffmpeg, @".\");
            ffmpeg.Close();
            dln.Close();
            ShowNotif("Download Done!", "FFMPEGのダウンロードが終わりました");
        }

        private async Task yt_dlp_Download(FileDownloader fld)
        {
            var ytdlp = await fld.GetContent(yt_dlp_Download_URL);
            using (FileStream fs = new FileStream(@".\yt-dlp.exe", FileMode.Create))
            {
                //ファイルに書き込む
                ytdlp.WriteTo(fs);
                ytdlp.Close();
                ShowNotif("Download Done!", "YT-DLPのダウンロードが終わりました");
            }
        }

        private int count = 1; //ListViewのカウント
        private void showProgress(DownloadProgress p)
        {
            prog.Value = p.Progress;
            Title = p.State.ToString();
            int a = (int)(p.Progress * 100);
            progText.Content = $"speed: {p.DownloadSpeed} | left: {p.ETA} | %: {a}%";
            if (p.State.ToString() == "Success")
            {
                saveVideosInfomation sv = new saveVideosInfomation();
                if (dLLists.Count != count)
                {
                    sv.saveInfo(((DLList)list.Items[count - 1]).url);
                    count += 1;
                    Debug.WriteLine($"Count::{count - 1}");
                    var b = ((DLList)list.Items[count - 1]).url;
                    Debug.WriteLine(b);
                    DownloadAsync(b);
                }
                else
                {
                    EndDownload(sv);
                }

            }
            else if (p.State.ToString() == "Error")
            {
                MessageBox.Show("失敗");
                Debug.WriteLine(p.State.ToString());
            }
        }

        private void EndDownload(saveVideosInfomation sv)
        {
            sv.saveInfo(((DLList)list.Items[count - 1]).url);
            listView_Recent.ItemsSource = saveVideosInfomation.ob;
            ShowNotif("All Done!", "おわったお");
            list.ClearValue(ItemsControl.ItemsSourceProperty);
            folder = "none";
            count = 1;
            progText.Content = "Download States";
            dLLists.Clear();
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
        private async void download_Click_1(object sender, RoutedEventArgs e)
        {
            GetInfomation getInfomation = new GetInfomation();
            Loading load = new Loading();
            load.Show();
            Notouch();
            var result = await getInfomation.Infomation(webview.CoreWebView2.Source);
            string Title = result == null ? "none" : result.Title;
            dLLists.Add(new DLList { url = webview.CoreWebView2.Source, name = Title });
            list.ItemsSource = dLLists;
            isEnd = true;
            load.Close();
        }
        private void cookie_Checked(object sender, RoutedEventArgs e)
        {
            PasswordBox.IsEnabled = true;
            Cookies.IsEnabled = true;
            WriteSettings("Cookies_Enabled", "true");
        }
        private void cookie_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.IsEnabled = false;
            Cookies.IsEnabled = false;
            WriteSettings("Cookies_Enabled", "false");
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            dLLists.Clear();
        }
        private async void Add_Url_List(object sender, RoutedEventArgs e)
        {
            AddUrl addURl = new();
            addURl.ShowDialog();
            var urls = addURl.urls;

            try
            {
                await UrlsCheck(urls);

            }
            catch (Exception ex)
            {
                MessageBox.Show("使用できない文字列が入っているか、値が無効です。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private async Task UrlsCheck(string[] urls)
        {
            GetInfomation getInfomation = new GetInfomation();
            Loading load = new Loading();
            string Title = String.Empty;
            VideoData result;
            load.Show();
            Notouch();
            foreach (var url in urls)
            {

                if (IsValidUrl(url))
                {
                    result = await getInfomation.Infomation(url);
                    Title = result.Title;
                    dLLists.Add(new DLList { url = url, name = Title });
                }
            }
            list.ItemsSource = dLLists;
            isEnd = true;
            load.Close();
        }
        /// <summary>
        /// きちんとしたURLのフォーマットになっているかチェック
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsValidUrl(string uri)
        {
            return Uri.IsWellFormedUriString(uri, UriKind.Absolute);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(Cookies_Path))
            {
                using (StreamReader sm = new StreamReader(Cookies_Path))
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

                Settings_Apply();
                string ExtractUrl = ((DLList)list.Items[0]).url;
                Debug.WriteLine(ExtractUrl);
                DownloadAsync(ExtractUrl);
            }
        }
        private async void Info_Search(object sender, RoutedEventArgs e)
        {
            if (IsValidUrl(SearchBox_Info.Text))
            {
                //初期化
                Loading load = new Loading();
                GetInfomation get = new GetInfomation();

                //ロード中を呼び出し
                Notouch();
                load.Show();

                //本処理
                var video = await get.Infomation(SearchBox_Info.Text);
                BitmapImage bti = new BitmapImage(new Uri(video.Thumbnail));
                thumbPic.Source = bti;
                VideoTitle.Header = video.Title;
                VidInfo.Text = video.ToString();
                load.Close();
                isEnd = true;
            }
            else
            {
                MessageBox.Show("URLを入力してください!", "注意", MessageBoxButton.OK, MessageBoxImage.Error);
            }

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
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show("アプリを終了してもよろしいでしょうか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        //以下より下は設定のメソッドしかない。

        private void SetPixel_Checked(object sender, RoutedEventArgs e)
        {
            combo.IsEnabled = true;
            WriteSettings("resolution_Enabled", "true");
        }

        private void SetPixel_Unchecked(object sender, RoutedEventArgs e)
        {
            combo.IsEnabled = false;
            WriteSettings("resolution_Enabled", "false");
        }

        private void Search_Clicked(object sender, RoutedEventArgs e)
        {
            webview.CoreWebView2.Navigate(SearchBox.Text);
        }

        private void prog_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine(prog.Value.ToString());
        }

        private void CodecToggle_Checked(object sender, RoutedEventArgs e)
        {
            codec.IsEnabled = true;
            WriteSettings("codec_Enabled", "true");
        }

        private void CodecToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            codec.IsEnabled = false;
            WriteSettings("codec_Enabled", "false");
        }

        private void Cookies_Clicked_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password != "")
            {
                WriteSettings("Cookies", PasswordBox.Password);
            }
        }

        private void combo_DropDownClosed(object sender, EventArgs e)
        {
            WriteSettings("resolution", Convert.ToString(combo.SelectedIndex));
        }

        private void codec_DropDownClosed(object sender, EventArgs e)
        {
            string sel = ((ComboBox)sender).Text;
            WriteSettings("codec", Convert.ToString(codec.SelectedIndex));
            Debug.WriteLine(sel);
        }

        private void codec_Audio_DropDownClosed(object sender, EventArgs e)
        {
            WriteSettings("codec_Audio", Convert.ToString(codec_Audio.SelectedIndex));
        }
        private void WriteSettings(string Title, string value)
        {
            if (!UseSettingsFile)
            {
                Directory.CreateDirectory(@".\Settings");
                using (StreamWriter sw = new StreamWriter($@".\Settings\{Title}.txt", false))
                {
                    sw.WriteLine(value);
                }
            }

        }
        private void ReadSettings(string Title, string value)
        {
            Directory.CreateDirectory(@".\Settings");
            using (StreamWriter sw = new StreamWriter($@".\Settings\{Title}.txt", false))
            {
                sw.WriteLine(value);
            }
        }
        private void Codec_Audio_Toggle_Checked(object sender, RoutedEventArgs e)
        {
            codec_Audio.IsEnabled = true;
            WriteSettings("codec_Audio_Enabled", "true");
        }

        private void Codec_Audio_Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            codec_Audio.IsEnabled = false;
            WriteSettings("codec_Audio_Enabled", "false");
        }

    }
}