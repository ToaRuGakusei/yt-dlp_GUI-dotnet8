using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Policy;
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
        //初期化
        private IProgress<DownloadProgress> progress;
        private string cookieBrowser;
        private string videoFormat;
        private bool UseSettingsFile = false;
        private bool Cookies_Enabled = false;
        private bool SetPixel_Enabled = false;
        private bool Codec_Enabled = false;
        private bool Codec_Audio_Enabled = false;
        private bool container_Enabled = false;
        private bool Audio_Only_Enabled = false;

        //設定関連の初期化
        private string Cookie = "";
        private int Pixel = 0;
        private int Codec = 0;
        private int Codec_Audio = 0;
        private int Video = 0;
        private int video_Value = 0;
        private int Merge = 0;
        private int Audio_Only_Value = 0;

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
        private AudioConversionFormat AudioOnlyConversion;
        private string Cookies_Path = @".\Cookies.txt";
        private string[] Codec_List = { "h264", "h265", "vp9", "av1" };
        private bool Cookies_Found = false;
        private Loading loading;

        //解像度の番号がいまいちわからない。情報を取得して選ばせたい。
        //597(256x144) 160(256x144) 133(426x240) 134(640x360) 135(854x480) 298(1280x720) 299(1920x1080) 400(2560x1440) 401(3840x2160) 571(7680x4320)　全部AVC
        private int[] video = { 160, 133, 134, 135, 298, 299, 400, 401, 571 };
        private DownloadMergeFormat mergeOutputFormat;

        //配列で呼び出す
        private ObservableCollection<DownloadMergeFormat> mergeList = new ObservableCollection<DownloadMergeFormat>
        {DownloadMergeFormat.Mp4,
         DownloadMergeFormat.Mkv,
         DownloadMergeFormat.Flv };

        private ObservableCollection<AudioConversionFormat> AudioList = new ObservableCollection<AudioConversionFormat>
        {
          AudioConversionFormat.Mp3,
          AudioConversionFormat.Aac,
          AudioConversionFormat.Flac
        };


        private readonly string title = "AllVideoDownloader(仮)";


        public class DLList()
        {
            public string url { get; set; }
            public string name { get; set; }
            public Uri image { get; set; }
            public bool isLive { get; set; }
            public bool YesPlayList { get; set; }
        }
        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();//WebView2関連の設定をする
            QuestionDownloadFirst(); //ここでtoolの有無を確認（なければダウンロードするか聞く）
            Settings_Apply();//設定を反映させる

            progress = new Progress<DownloadProgress>((p) => showProgress(p));//進捗状況を反映するための式
            ChangeTheme();//ThemeChange

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
            container_Enabled = settingsLoader.SettingEnabled_Check("container_Enabled") == "true" ? true : false;
            Cookie = settingsLoader.SettingGetter("Cookies").Replace("\r\n", "").Replace("\"", "");
            Audio_Only_Enabled = settingsLoader.SettingEnabled_Check("Audio_Only_Enabled") == "true" ? true : false;
            Pixel = int.Parse(settingsLoader.SettingGetter("resolution"));
            Codec = int.Parse(settingsLoader.SettingGetter("codec"));
            Codec_Audio = int.Parse(settingsLoader.SettingGetter("codec_Audio"));
            Video = int.Parse(settingsLoader.SettingGetter("resolution"));
            Merge = int.Parse(settingsLoader.SettingGetter("container"));
            Audio_Only_Value = int.Parse(settingsLoader.SettingGetter("Audio_Only"));
            
            //設定反映(´・ω・`)
            cookie.IsChecked = Cookies_Enabled;
            SetPixel.IsChecked = SetPixel_Enabled;
            CodecToggle.IsChecked = Codec_Enabled;
            Codec_Audio_Toggle.IsChecked = Codec_Audio_Enabled;
            container_Toggle.IsChecked = container_Enabled;
            audio_Only_Toggle.IsChecked = Audio_Only_Enabled;
            if (Codec_Audio != -9)
                AudioConversion = AudioList[Codec_Audio];

            if (Audio_Only_Value != -9)
                AudioOnlyConversion = AudioList[Audio_Only_Value];

            if (Codec != -9)
                videoFormat = Codec_List[Codec];

            if (Video != -9)
                video_Value = video[Video];

            if (Cookie == "-9")
            {
                Cookie = "";
                Cookies_Found = false;
            }
            else
            {
                Cookies_Found = true;
            }

            if (Merge != -9)
            {
                mergeOutputFormat = mergeList[Merge];
            }

            //-9は取得できなかったということで、何も表示させないために0を代入させる。それ以外の場合はそのまま代入。
            combo.SelectedIndex = Pixel == -9 ? 0 : Pixel;
            codec.SelectedIndex = Codec == -9 ? 0 : Codec;
            codec_Audio.SelectedIndex = Codec_Audio == -9 ? 0 : Codec_Audio;
            container.SelectedIndex = Merge == -9 ? 0 : Merge;
            Only.SelectedIndex = Audio_Only_Value == -9 ? 0 : Audio_Only_Value;
            PasswordBox.Text = Cookie;

            SetRecent(); //履歴セット

            //設定ファイルへのアクセスを解放
            UseSettingsFile = false;
        }

        private void SetRecent()
        {
            saveVideosInfomation.ob.Clear();
            saveVideosInfomation Infomation = new saveVideosInfomation();
            Infomation.loadInfo();
            listView_Recent.ItemsSource = saveVideosInfomation.ob;

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
                    cookieBrowser = sm.ReadToEnd(); //CookiesをStreamReaderで取得
                }
            }
        }

        //Toast通知発火
        public static void ShowToast(string title, string body)
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
            else
            {
                ytdl.OutputFolder = folder;
                DownloadVideo(url, ytdl);
            }
        }
        private void DownloadVideo(string url, YoutubeDL ytdl)
        {

            if ((dLLists[0] as DLList).isLive)
            {
                loading = new Loading(true);
                loading.Show();
            }
            var options = new OptionSet()
            {
                Format = $"{video_Value}+bestaudio/bestvideo+bestaudio", //動画のダウンロード形式を指定
                FormatSort = $"vcodec:{videoFormat}", //コーディックを指定
                AudioFormat = (bool)audio_Only_Toggle.IsChecked ? AudioOnlyConversion : AudioConversion, //オーディオコーデックを指定
                ExtractAudio = (bool)audio_Only_Toggle.IsChecked, //Audioのみになる。（なぜかきちんとコーデックが反映されている）
                MergeOutputFormat = mergeOutputFormat, //コンテナ？を指定
                Cookies = Cookie, //クッキーを指定
                NoCookies = !(bool)cookie.IsChecked, //クッキーを無効化
                EmbedMetadata = true, //メタデータを付加
                EmbedThumbnail = true, //サムネイルを付加
                IgnoreErrors = true, //エラー無視
                YesPlaylist = false, //PlayListを明示する
                //ListFormats = true, //フォーマットリストを表示？
                Retries = 5 //リトライ回数を指定（ここはユーザーに選んでもらう）
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
            ShowToast("Download Done!", "FFMPEGのダウンロードが終わりました");
        }

        private async Task yt_dlp_Download(FileDownloader fld)
        {
            var ytdlp = await fld.GetContent(yt_dlp_Download_URL);
            using (FileStream fs = new FileStream(@".\yt-dlp.exe", FileMode.Create))
            {
                //ファイルに書き込む
                ytdlp.WriteTo(fs);
                ytdlp.Close();
                ShowToast("Download Done!", "YT-DLPのダウンロードが終わりました");
            }
        }

        private int count = 1; //ListViewのカウント
        private void showProgress(DownloadProgress p)
        {
            prog.Value = p.Progress;
            int a = (int)(p.Progress * 100);
            Title = $"speed: {p.DownloadSpeed} | left: {p.ETA} | %: {a}%";
            progText.Content = p.State;

            if (p.State.ToString() == "Success")
            {
                saveVideosInfomation sv = new saveVideosInfomation();
                if (dLLists.Count != count)
                {
                    sv.SaveInfo(((DLList)list.Items[count - 1]).url);
                    count += 1;
                    Debug.WriteLine($"Count::{count - 1}");
                    var b = ((DLList)list.Items[count - 1]).url;
                    Debug.WriteLine(b);
                    if (loading != null)
                    {
                        loading.Close();
                    }
                    DownloadAsync(b);
                }
                else
                {
                    if (loading != null)
                    {
                        loading.Close();
                    }
                    EndDownload(sv, false);
                }

            }
            else if (p.State.ToString() == "Error")
            {
                EndDownload(null, true);
                Debug.WriteLine(p.State.ToString());
            }
        }

        private void EndDownload(saveVideosInfomation sv, bool error)
        {
            if (sv != null)
            {
                sv.SaveInfo(((DLList)list.Items[count - 1]).url);
                listView_Recent.ItemsSource = saveVideosInfomation.ob;
            }
            if (error)
            {
                ShowToast("Error!", "エラーが発生しました");
            }
            else
            {
                ShowToast("All Done!", "おわったお");
            }
            list.ClearValue(ItemsControl.ItemsSourceProperty);
            if (loading != null)
            {
                loading.Close();
            }
            folder = "none";
            count = 1;
            this.IsEnabled = true;
            progText.Content = "Download States";
            dLLists.Clear();
            Title = title;
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

        private void WebView2_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
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
            Loading load = new Loading(false);
            load.Show();
            Notouch();
            var result = await getInfomation.Infomation(webview.CoreWebView2.Source);
            string Title = result == null ? "none" : result.Title;
            try
            {
                if (result.LiveStatus == LiveStatus.IsLive)
                {
                    dLLists.Add(new DLList { url = webview.CoreWebView2.Source, name = Title, image = new Uri(result.Thumbnail), isLive = true, YesPlayList = webview.CoreWebView2.Source.Contains("list") });
                }
                else
                {
                    dLLists.Add(new DLList { url = webview.CoreWebView2.Source, name = Title, image = new Uri(result.Thumbnail), isLive = false, YesPlayList = webview.CoreWebView2.Source.Contains("list") });
                }
            }
            catch(Exception ex) 
            {
                MessageBox.Show("このサイトに対応していない可能性があります\n詳しくはお問い合わせください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
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
            if (urls != null)
            {
                try
                {
                    await UrlsCheck(urls);

                }
                catch (Exception)
                {
                    MessageBox.Show("使用できない文字列が入っているか、値が無効です。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                    EndDownload(null, false);
                }
            }



        }

        private async Task UrlsCheck(string[] urls)
        {
            GetInfomation getInfomation = new GetInfomation();
            loading = new Loading(false);
            string Title = String.Empty;
            VideoData result;
            loading.Show();
            Notouch();
            foreach (var url in urls)
            {

                if (IsValidUrl(url))
                {
                    result = await getInfomation.Infomation(url);
                    Title = result.Title;
                    if (result.LiveStatus == LiveStatus.IsLive)
                    {
                        dLLists.Add(new DLList { url = url, name = Title, image = new Uri(result.Thumbnail), isLive = true, YesPlayList = url.Contains("list") });
                    }
                    else
                    {
                        dLLists.Add(new DLList { url = url, name = Title, image = new Uri(result.Thumbnail), isLive = false, YesPlayList = url.Contains("list") });
                    }

                }
            }
            list.ItemsSource = dLLists;
            isEnd = true;
            loading.Close();
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
                Loading load = new Loading(false);
                GetInfomation get = new GetInfomation();
                try
                {
                    //ロード中を呼び出し
                    Notouch();
                    load.Show();

                    //本処理
                    var video = await get.Infomation(SearchBox_Info.Text);
                    if (video != null)
                    {
                        BitmapImage bti = new BitmapImage(new Uri(video.Thumbnail));
                        thumbPic.Source = bti;
                        VideoTitle.Header = video.Title;
                        VidInfo.Text = video.ToString();
                        load.Close();
                        isEnd = true;
                    }
                    load.Close();
                    isEnd = true;
                }
                catch (Exception)
                {
                    MessageBox.Show("対応していないサイトかエラーが発生しました。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                    load.Close();
                    isEnd = true;
                }

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
                ShowToast("Cancel", "キャンセルされたお");
                list.ClearValue(ItemsControl.ItemsSourceProperty);
                folder = "none";
                count = 1;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            saveVideosInfomation.ob.Clear();
            string DownloadRecent_Path = @".\Recent\DownloadRecent.txt";
            File.Delete(DownloadRecent_Path);
            ShowToast("成功", "履歴を削除しました");

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

        //以下より下は設定のメソッドしかないじゃない。

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
        private void audio_Only_Toggle_Checked(object sender, RoutedEventArgs e)
        {
            Only.IsEnabled = true;
            combo.IsEnabled = false;
            codec.IsEnabled = false;
            codec_Audio.IsEnabled = false;
            container.IsEnabled = false;
            container_Toggle.IsEnabled = false;
            Codec_Audio_Toggle.IsEnabled = false;
            CodecToggle.IsEnabled = false;
            SetPixel.IsEnabled = false;
            WriteSettings("Audio_Only_Enabled", "true");
        }
        private void Audio_Only_DropDownClosed(object sender, EventArgs e)
        {
            WriteSettings("Audio_Only", Convert.ToString(Only.SelectedIndex));
        }
        private void audio_Only_Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Only.IsEnabled = false;
            combo.IsEnabled = true;
            codec_Audio.IsEnabled = true;
            codec.IsEnabled = true;
            container.IsEnabled = true;
            container_Toggle.IsEnabled = true;
            Codec_Audio_Toggle.IsEnabled = true;
            CodecToggle.IsEnabled = true;
            SetPixel.IsEnabled = true;

            WriteSettings("Audio_Only_Enabled", "false");
        }
        private void Cookies_Clicked_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Text != "")
            {
                WriteSettings("Cookies", PasswordBox.Text);
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
        }

        private void codec_Audio_DropDownClosed(object sender, EventArgs e)
        {
            WriteSettings("codec_Audio", Convert.ToString(codec_Audio.SelectedIndex));
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
        private void container_Toggle_Checked(object sender, RoutedEventArgs e)
        {
            container.IsEnabled = true;
            WriteSettings("container_Enabled", "true");
        }
        private void container_Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            container.IsEnabled = false;
            WriteSettings("container_Enabled", "false");
        }
        private void container_DropDownClosed(object sender, EventArgs e)
        {
            WriteSettings("container", Convert.ToString(container.SelectedIndex));
        }

        private void listView_Recent_Selected(object sender, RoutedEventArgs e)
        {
            if (listView_Recent.SelectedItem != null)
            {
                string _u = (listView_Recent.Items[listView_Recent.SelectedIndex] as saveVideosInfomation.VideoInfo).URI;
                Debug.WriteLine(_u);
                try
                {
                    Clipboard.SetData(DataFormats.Text, _u);
                    ProcessStartInfo pi = new ProcessStartInfo()
                    {
                        FileName = _u,
                        UseShellExecute = true,
                    };
                    Process.Start(pi);
                    ShowToast("成功", "URLをクリップボードに貼り付けました");

                }
                catch (Exception ex)
                {
                    ShowToast("失敗", "URLの取得に失敗しました");
                }


            }
            listView_Recent.SelectedItem = null;
        }
    }
}