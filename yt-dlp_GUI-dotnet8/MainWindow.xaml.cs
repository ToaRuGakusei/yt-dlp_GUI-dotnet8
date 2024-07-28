using MaterialDesignThemes.Wpf;
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
        //初期化
        private IProgress<DownloadProgress> progress;
        private string cookieBrowser;
        private string videoFormat;
        private bool Cookies_Enabled = false;
        private bool SetPixel_Enabled = false;
        private bool Codec_Enabled = false;
        private bool Codec_Audio_Enabled = false;
        private bool container_Enabled = false;
        private bool Audio_Only_Enabled = false;
        private Toast Toast = new Toast();

        //設定関連の初期化
        private int Pixel = 0;
        private int Codec = 0;
        private int Codec_Audio = 0;
        private int Video = 0;
        private int video_Value = 0;
        private int Audio_Value = 0;
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
        private string yt_dlp_Download_URL = "https://github.com/yt-dlp/yt-dlp/releases/download/2024.07.25/yt-dlp.exe";
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
        private int[] audio = { 233, 140, 234 };
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
        private LoadSettings loadSettings1;


        private readonly string title = "AllVideoDownloader(仮)";
        public class DLList()
        {
            public string url { get; set; }
            public string name { get; set; }
            public Uri image { get; set; }
            public bool isLive { get; set; }
            public bool YesPlayList { get; set; }
            public int value { get; set; }
            public FormatData[] formatDatas { get; set; }
        }
        public MainWindow()
        {
            InitializeComponent();
            CheckUpdate checkUpdate = new();
            _ = checkUpdate.Check("yt-dlp_GUI-dotnet8", "ToaRuGakusei");
            _ = checkUpdate.Check("yt-dlp", "yt-dlp");
            InitializeAsync();//WebView2関連の設定をする
            QuestionDownloadFirst(); //ここでtoolの有無を確認（なければダウンロードするか聞く）
            progress = new Progress<DownloadProgress>((p) => showProgress(p));//進捗状況を反映するための式
            ChangeTheme();//ThemeChange

            _vm = new ViewModel();
            this.DataContext = _vm;
            (App.Current as App).ViewModel = _vm;
            loadSettings1 = new LoadSettings();
            loadSettings1.Settings_Apply();//設定を反映させる
        }
        ViewModel _vm;
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
        //ここでオプションなどを設定し、DownloadVideoに値を渡す。
        private void DownloadAsync(string url, FormatData[] format)
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
                    DownloadVideo(url, ytdl, format);
                }
                else
                {
                    cancel = true;
                    Go.IsEnabled = true;
                    AddUrlList.IsEnabled = true;
                    Clear.IsEnabled = true;
                    MessageBox.Show("キャンセルされました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                ytdl.OutputFolder = folder;
                DownloadVideo(url, ytdl, format);
            }
        }
        private void DownloadVideo(string url, YoutubeDL ytdl, FormatData[] formatData)
        {

            if ((dLLists[0] as DLList).isLive)
            {
                loading = new Loading(true);
                loading.Show();
            }
            var Resolution = "";
            var AudioCodec = "";
            foreach (var format in formatData)
            {
                switch (_vm.CodecAudio)
                {
                    case 0:
                        if (format.FormatId == "140")
                        {
                            AudioCodec = format.FormatId;
                        }
                        break;
                    case 1:
                        if (format.FormatId == "140")
                        {
                            AudioCodec = format.FormatId;

                        }
                        break;
                    case 2:
                        if (format.FormatId == "140")
                        {
                            AudioCodec = format.FormatId;
                        }
                        break;
                }
                if (AudioCodec != "")
                    break;
                else
                    AudioCodec = "bestaudio[ext=m4a]";

            }
            foreach (var format in formatData)
            {
                switch (_vm.Pixel)
                {
                    case 0:
                        if (format.Resolution == "256x144" && format.VideoCodec.Contains("avc"))
                        {
                            Resolution = format.FormatId;
                        }
                        break;
                    case 1:
                        if (format.Resolution == "426x240" && format.VideoCodec.Contains("avc"))
                        {
                            Resolution = format.FormatId;

                        }
                        break;
                    case 2:
                        if (format.Resolution == "640x360" && format.VideoCodec.Contains("avc"))
                        {
                            Resolution = format.FormatId;

                        }
                        break;
                    case 3:
                        if (format.Resolution == "854x480" && format.VideoCodec.Contains("avc"))
                        {
                            Resolution = format.FormatId;

                        }
                        break;
                    case 4:
                        if (format.Resolution == "1280x720" && format.VideoCodec.Contains("avc"))
                        {
                            Resolution = format.FormatId;

                        }
                        break;
                    case 5:
                        if (format.Resolution == "1920x1080" && format.VideoCodec.Contains("avc"))
                        {
                            Resolution = format.FormatId;

                        }
                        break;
                    case 6:
                        if (format.Resolution == "2560x1440")
                        {
                            Resolution = "401";

                        }
                        break;
                    case 7:
                        if (format.Resolution == "3840x2160")
                        {
                            Resolution = "625";

                        }
                        break;
                    case 8:
                        if (format.Resolution == "7680x4320")
                        {
                            Resolution = format.FormatId;

                        }
                        break;
                }
                if (Resolution == "")
                {
                    Resolution = "bestvideo[ext=mp4]";
                }

                Debug.WriteLine("解像度" + Resolution + "\n" + "オーディオコーデック" + AudioCodec);
            }

            var options = new OptionSet()
            {               
                //RemuxVideo= "aac/mkv",
                FormatSort = $"vcodec:{Codec_List[_vm.Codec]}", //コーディックを指定
                AudioFormat = AudioList[_vm.CodecAudio], //オーディオコーデックを指定
                ExtractAudio = (bool)audio_Only_Toggle.IsChecked, //Audioのみになる。（なぜかきちんとコーデックが反映されている）
                MergeOutputFormat = mergeList[_vm.Extension], //コンテナ？を指定
                Cookies = _vm.myCookies, //クッキーを指定
                NoCookies = !(bool)cookie.IsChecked, //クッキーを無効化
                EmbedMetadata = true, //メタデータを付加
                EmbedThumbnail = true, //サムネイルを付加
                IgnoreErrors = true, //エラー無視
                YesPlaylist = (dLLists[0] as DLList).YesPlayList, //PlayListかどうかを明示する
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
        /// 
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
            CheckUpdate checkUpdate = new();
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
            var ffmpeg = await fld.GetContent(ffmpeg_Download_URL, "");
            try
            {
                ZipFile.ExtractToDirectory(ffmpeg, @".\", true);
            }
            catch (Exception)
            {

            }
            ffmpeg.Close();
            dln.Close();
            Toast.ShowToast("Download Done!", "FFMPEGのダウンロードが終わりました");
        }

        private async Task yt_dlp_Download(FileDownloader fld)
        {
            var ytdlp = await fld.GetContent(yt_dlp_Download_URL, "");
            try
            {
                using (FileStream fs = new FileStream(@".\yt-dlp.exe", FileMode.Create))
                {
                    //ファイルに書き込む
                    ytdlp.WriteTo(fs);
                    ytdlp.Close();
                    Toast.ShowToast("Download Done!", "YT-DLPのダウンロードが終わりました", "https://raw.githubusercontent.com/yt-dlp/yt-dlp/master/.github/banner.svg");
                }
            }
            catch (Exception)
            {

            }

        }

        private int count = 1; //ListViewのカウント
        private bool DownloadIsEnd = false;
        private async void showProgress(DownloadProgress p)
        {
            //プログレスバーなどに値を渡す
            prog.Value = p.Progress;
            int a = (int)(p.Progress * 100);
            Title = $"speed: {p.DownloadSpeed} | left: {p.ETA} | %: {a}%";
            progText.Content = p.State;
            Debug.WriteLine(p.State);

            //成功した場合
            if (p.State.ToString() == "Success")
            {
                if (dLLists.Count != count)
                {
                    Next(loadSettings1);
                }
                else if ((dLLists[0] as DLList).YesPlayList)
                {
                    await Task.Delay(1000);
                    CheckProcess();
                    if (DownloadIsEnd)
                    {
                        DownloadIsEnd = false;
                        EndDownload(loadSettings1, false);
                    }
                }
                else
                {
                    if (loading != null)
                    {
                        loading.Close();
                    }
                    EndDownload(loadSettings1, false);
                }

            }
        }

        private void CheckProcess()
        {
            Process[] processes = Process.GetProcessesByName("yt-dlp");
            if (processes.Length == 0)
            {
                Debug.WriteLine("Not runnning");
                DownloadIsEnd = true;

            }
            else
            {
                Debug.WriteLine("Running");
                DownloadIsEnd = false;
            }
        }

        private void Next(LoadSettings load)
        {
            load.SaveInfo(((DLList)list.Items[count - 1]).url);
            count += 1;
            Debug.WriteLine($"Count::{count - 1}");
            var b = ((DLList)list.Items[count - 1]).url;
            var format = ((DLList)list.Items[count - 1]).formatDatas;
            Debug.WriteLine(b);
            if (loading != null)
            {
                loading.Close();
            }
            DownloadAsync(b, format);
        }

        private void EndDownload(LoadSettings load, bool error)
        {
            //情報がnull以外の時
            if (load != null)
            {
                load.SaveInfo(((DLList)list.Items[count - 1]).url);
                listView_Recent.ItemsSource = _vm.Recent;
            }

            //エラーが発生した場合
            if (error)
            {
                Toast.ShowToast("Error!", "エラーが発生しました");
            }
            else
            {
                Toast.ShowToast("All Done!", "おわったお");
            }

            list.ClearValue(ItemsControl.ItemsSourceProperty); //ListViewをクリア

            //ロード画面が起動している場合、閉じる。
            if (loading != null)
            {
                loading.Close();
            }

            //最終処理
            folder = "none";
            count = 1;
            this.IsEnabled = true;
            progText.Content = "Download States";
            dLLists.Clear();
            Title = title;
            Go.IsEnabled = true;
            Clear.IsEnabled = true;
            AddUrlList.IsEnabled = true;
        }

        //WebView2関連のメソッド
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
                    dLLists.Add(new DLList { url = webview.CoreWebView2.Source, name = Title, image = new Uri(result.Thumbnail), isLive = true, YesPlayList = webview.CoreWebView2.Source.Contains("list"), value = 12 });
                }
                else
                {
                    dLLists.Add(new DLList { url = webview.CoreWebView2.Source, name = Title, image = new Uri(result.Thumbnail), isLive = false, YesPlayList = webview.CoreWebView2.Source.Contains("list"), value = 90 });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("このサイトに対応していない可能性があります\n詳しくはお問い合わせください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            list.ItemsSource = dLLists;
            isEnd = true;
            load.Close();
        }
        //ここで終わり

        private async Task UrlsCheck(string[] urls)
        {
            GetInfomation getInfomation = new GetInfomation();
            loading = new Loading(false);
            string Title = String.Empty;
            int a = 0;
            VideoData result;
            loading.Show();
            Notouch();
            foreach (var url in urls)
            {

                if (IsValidUrl(url))
                {
                    result = await getInfomation.Infomation(url);
                    Title = result.Title;
                    var CodecList = await getInfomation.CodecInfomation(result);

                    if (result.LiveStatus == LiveStatus.IsLive)
                    {
                        dLLists.Add(new DLList { url = url, name = Title, image = new Uri(result.Thumbnail), isLive = true, YesPlayList = url.Contains("list"), formatDatas = CodecList });
                    }
                    else
                    {
                        dLLists.Add(new DLList { url = url, name = Title, image = new Uri(result.Thumbnail), isLive = false, YesPlayList = url.Contains("list"), formatDatas = CodecList });
                    }

                }
                a++;
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
                loadSettings1.Settings_Apply();
                Go.IsEnabled = false;
                AddUrlList.IsEnabled = false;
                Clear.IsEnabled = false;
                //this.IsEnabled = false;
                string ExtractUrl = ((DLList)list.Items[0]).url;
                var formats = ((DLList)list.Items[0]).formatDatas;
                Debug.WriteLine(ExtractUrl);
                DownloadAsync(ExtractUrl, formats);
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
                Toast.ShowToast("Cancel", "キャンセルされました");
                list.ClearValue(ItemsControl.ItemsSourceProperty);
                folder = "none";
                count = 1;
                progText.Content = "Download States";
                Go.IsEnabled = true;
                AddUrlList.IsEnabled = true;
                Clear.IsEnabled = true;


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
            Environment.Exit(0);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _vm.Recent.Clear();
            string DownloadRecent_Path = @".\Recent\DownloadRecent.txt";
            File.Delete(DownloadRecent_Path);
            Toast.ShowToast("成功", "履歴を削除しました");

        }

        private void cookie_Checked(object sender, RoutedEventArgs e)
        {
            //PasswordBox.IsEnabled = true;
            //Cookies.IsEnabled = true;
        }
        private void cookie_Unchecked(object sender, RoutedEventArgs e)
        {
            //PasswordBox.IsEnabled = false;
            //cookie.IsEnabled = false;
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
        private void SetPixel_Checked(object sender, RoutedEventArgs e)
        {
            //combo.IsEnabled = true;
        }

        private void SetPixel_Unchecked(object sender, RoutedEventArgs e)
        {
            //combo.IsEnabled = false;
        }

        private void Search_Clicked(object sender, RoutedEventArgs e)
        {
            webview.CoreWebView2.Navigate(SearchBox.Text);
        }
        private void CodecToggle_Checked(object sender, RoutedEventArgs e)
        {
            //codec.IsEnabled = true;
        }

        private void SetDefault_Checked(object sender, RoutedEventArgs e)
        {

            /*combo.IsEnabled = false;
            codec.IsEnabled = false;
            codec_Audio.IsEnabled = false;
            Only.IsEnabled = false;
            container.IsEnabled = false;
            container_Toggle.IsEnabled = false;
            Codec_Audio_Toggle.IsEnabled = false;
            CodecToggle.IsEnabled = false;
            SetPixel.IsEnabled = false;*/
        }

        private void CodecToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            //codec.IsEnabled = false;
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
            //downloadSetting.AudioOnlyIsEnable = true;
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
        }


        private void Codec_Audio_Toggle_Checked(object sender, RoutedEventArgs e)
        {
            //codec_Audio.IsEnabled = true;
        }

        private void Codec_Audio_Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            //codec_Audio.IsEnabled = false;
        }
        private void container_Toggle_Checked(object sender, RoutedEventArgs e)
        {
            //container.IsEnabled = true;
        }

        private void listView_Recent_Selected(object sender, RoutedEventArgs e)
        {
            if (listView_Recent.SelectedItem != null)
            {
                string _u = (listView_Recent.Items[listView_Recent.SelectedIndex] as ViewModel.VideoInfo).URI;
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
                    Toast.ShowToast("成功", "URLをクリップボードに貼り付けました");

                }
                catch (Exception ex)
                {
                    Toast.ShowToast("失敗", "URLの取得に失敗しました");
                }


            }
            listView_Recent.SelectedItem = null;
        }

        private void DeleteRecent_Click(object sender, RoutedEventArgs e)
        {
            _vm.Recent.Clear();
            string DownloadRecent_Path = @".\Recent\DownloadRecent.txt";
            File.Delete(DownloadRecent_Path);
            Toast.ShowToast("成功", "履歴を削除しました");
        }



        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            loadSettings1.WriteSettings();
        }

        private void Setdefault_Unchecked(object sender, RoutedEventArgs e)
        {
            /*Only.IsEnabled = false;
            combo.IsEnabled = true;
            codec.IsEnabled = true;
            codec_Audio.IsEnabled = true;
            container.IsEnabled = true;
            container_Toggle.IsEnabled = true;
            Codec_Audio_Toggle.IsEnabled = true;
            CodecToggle.IsEnabled = true;
            SetPixel.IsEnabled = true;*/
        }

        private void DetailSetting_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}