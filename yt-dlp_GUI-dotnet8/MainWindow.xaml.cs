using MaterialDesignThemes.Wpf;
using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using yt_dlp_GUI_dotnet8.Tool;

namespace yt_dlp_GUI_dotnet8
{
    public partial class MainWindow : Window
    {
        // 初期化関連
        private IProgress<DownloadProgress> _progress;
        private string _cookieBrowser;

        // ダウンロード関連の初期化
        private CancellationTokenSource _cts;
        private string _folder = "none";
        private bool _cancel = false;
        private Task _run;
        private bool _isEnd = false;
        private readonly string _ytDlpPath = @".\yt-dlp.exe";
        private readonly string _ffmpegPath = @".\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe";
        private string _ytDlpDownloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/download/2024.08.06/yt-dlp.exe";
        private string _ffmpegDownloadUrl = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip";
        private string _cookiesPath = @".\Cookies.txt";
        private string[] _codecList = { "avc", "h265", "vp9", "av1" };
        private Loading _loading;
        private ObservableCollection<ViewModel.DLList> _dLLists;


        // 配列で呼び出す
        private ObservableCollection<DownloadMergeFormat> _mergeList = new ObservableCollection<DownloadMergeFormat>
        {
            DownloadMergeFormat.Mp4,
            DownloadMergeFormat.Mkv,
            DownloadMergeFormat.Flv
        };

        private ObservableCollection<AudioConversionFormat> _audioList = new ObservableCollection<AudioConversionFormat>
        {
            AudioConversionFormat.Mp3,
            AudioConversionFormat.Aac,
            AudioConversionFormat.Flac
        };

        private LoadSettings _loadSettings;

        private readonly string _title = "AllVideoDownloader(仮)";

        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync(); // WebView2関連の設定をする
            InitializeRun();
        }

        private void InitializeRun()
        {
            var checkUpdate = new CheckUpdate();
            checkUpdate.Check("yt-dlp_GUI-dotnet8", "ToaRuGakusei");
            checkUpdate.Check("yt-dlp", "yt-dlp");
            QuestionDownloadFirst(); // toolの有無を確認（なければダウンロードするか聞く）
            _progress = new Progress<DownloadProgress>(p => ShowProgress(p)); // 進捗状況を反映するための式
            ChangeTheme(); // テーマを変更

            _vm = new ViewModel();
            DataContext = _vm;
            (App.Current as App).ViewModel = _vm;
             _dLLists = _vm.DownloadList;
            _loadSettings = new LoadSettings();
            _loadSettings.ApplySettings(); // 設定を反映させる
        }

        private ViewModel _vm;

        /// <summary>
        /// テーマの変更
        /// </summary>
        private static void ChangeTheme()
        {
            var palette = new PaletteHelper();
            var theme = palette.GetTheme();
            theme.SetBaseTheme(Theme.Dark);
            palette.SetTheme(theme);
        }

        /// <summary>
        /// WebView関連の設定
        /// </summary>
        private async void InitializeAsync()
        {
            webview.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
            await webview.EnsureCoreWebView2Async(null);
            webview.CoreWebView2.NewWindowRequested += NewWindowRequested;
            webview.CoreWebView2.SourceChanged += CompletedPage;

            if (File.Exists(_cookiesPath))
            {
                using (var sm = new StreamReader(_cookiesPath))
                {
                    _cookieBrowser = sm.ReadToEnd(); // CookiesをStreamReaderで取得
                }
            }
        }

        private void DownloadAsync(string url, FormatData[] format)
        {
            var ytdl = new YoutubeDL
            {
                YoutubeDLPath = _ytDlpPath,
                FFmpegPath = _ffmpegPath
            };

            if (_folder == "none")
            {
                var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ytdl.OutputFolder = dlg.FileName;
                    _folder = dlg.FileName;
                    DownloadVideo(url, ytdl, format);
                }
                else
                {
                    _cancel = true;
                    Go.IsEnabled = true;
                    AddUrlList.IsEnabled = true;
                    Clear.IsEnabled = true;
                    MessageBox.Show("キャンセルされました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                ytdl.OutputFolder = _folder;
                DownloadVideo(url, ytdl, format);
            }
        }

        private void DownloadVideo(string url, YoutubeDL ytdl, FormatData[] formatData)
        {
            if (_dLLists[0].IsLive)
            {
                _loading = new Loading(true);
                _loading.Show();
            }

            var resolution = GetResolution(formatData);
            var audioCodec = GetAudioCodec(formatData);

            var options = new OptionSet
            {
                Format = $"{resolution}+{audioCodec}/bestvideo+bestaudio", //動画のダウンロード形式を指定
                FormatSort = $"vcodec:{_codecList[_vm.Codec]}",
                AudioFormat = _audioList[_vm.CodecAudio],
                ExtractAudio = (bool)audio_Only_Toggle.IsChecked,
                MergeOutputFormat = _mergeList[_vm.Extension],
                Cookies = _vm.myCookies,
                NoCookies = !(bool)cookie.IsChecked,
                EmbedMetadata = true,
                EmbedThumbnail = true,
                IgnoreErrors = true,
                YesPlaylist = _dLLists[0].YesPlayList,
                Retries = 5
            };

            _run = Task.Run(() =>
            {
                _cts = new CancellationTokenSource();
                Dispatcher.Invoke(() =>
                {
                    ytdl.RunVideoDownload(url, progress: _progress, ct: _cts.Token, overrideOptions: options);
                });
            });
        }

        private string GetResolution(FormatData[] formatData)
        {
            foreach (var format in formatData)
            {
                if (format.Resolution == GetResolutionByPixel(_vm.Pixel) && format.VideoCodec.Contains($"{Regex.Replace(_codecList[_vm.Codec], @"\d", "")}"))
                {
                    return format.FormatId;
                }
            }
            return "bestvideo[ext=mp4]";
        }

        private string GetResolutionByPixel(int pixel)
        {
            return pixel switch
            {
                0 => "256x144",
                1 => "426x240",
                2 => "640x360",
                3 => "854x480",
                4 => "1280x720",
                5 => "1920x1080",
                6 => "2560x1440",
                7 => "3840x2160",
                8 => "7680x4320",
                _ => string.Empty,
            };
        }

        private string GetAudioCodec(FormatData[] formatData)
        {
            foreach (var format in formatData)
            {
                if (format.FormatId == "140")
                {
                    return format.FormatId;
                }
            }
            return "bestaudio[ext=m4a]";
        }

        private void Notouch()
        {
            IsEnabled = false; // MainWindowの画面を無効化
            _isEnd = false;
            Task.Run(() =>
            {
                while (true)
                {
                    if (_isEnd)
                    {
                        Dispatcher.Invoke(() => IsEnabled = true);
                        break;
                    }
                }
            });
        }

        private async void QuestionDownloadFirst()
        {
            var checkUpdate = new CheckUpdate();
            if (!File.Exists(_ytDlpPath) || !File.Exists(_ffmpegPath))
            {
                var result = MessageBox.Show("yt-dlpまたはffmpegが見つかりませんでした。\nダウンロードしますか？", "情報", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                {
                    result = MessageBox.Show("ダウンロードしないとこのアプリは使用できません。\nこのアプリを終了しますか？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        Close(); // アプリを終了させる
                    }
                    else
                    {
                        QuestionDownloadFirst(); // もう一度ダイアログを表示
                    }
                }
                else
                {
                    await DownloadTool(); // toolのダウンロード開始
                }
            }
        }

        private async Task DownloadTool()
        {
            try
            {
                var fld = new FileDownloader();
                var dln = new DownloadNow();
                Notouch();
                dln.Show();
                await YtDlpDownload(fld);
                await FfmpegDownload(fld, dln);

                _isEnd = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async Task FfmpegDownload(FileDownloader fld, DownloadNow dln)
        {
            var ffmpeg = await fld.GetContent(_ffmpegDownloadUrl, "");
            try
            {
                ZipFile.ExtractToDirectory(ffmpeg, @".\", true);
            }
            catch (Exception) { }

            ffmpeg.Close();
            dln.Close();
            Toast.ShowToast("Download Done!", "FFMPEGのダウンロードが終わりました");
        }

        private async Task YtDlpDownload(FileDownloader fld)
        {
            var ytdlp = await fld.GetContent(_ytDlpDownloadUrl, "");
            try
            {
                using (var fs = new FileStream(_ytDlpPath, FileMode.Create))
                {
                    ytdlp.WriteTo(fs);
                    ytdlp.Close();
                    Toast.ShowToast("Download Done!", "YT-DLPのダウンロードが終わりました", "https://raw.githubusercontent.com/yt-dlp/yt-dlp/master/.github/banner.svg");
                }
            }
            catch (Exception) { }
        }

        private int _count = 1; // ListViewのカウント
        private bool _downloadIsEnd = false;

        private async void ShowProgress(DownloadProgress p)
        {
            prog.Value = p.Progress;
            int a = (int)(p.Progress * 100);
            Title = $"speed: {p.DownloadSpeed} | left: {p.ETA} | %: {a}%";
            progText.Content = p.State;
            Debug.WriteLine(p.State);

            if (p.State.ToString() == "Success")
            {
                if (_dLLists.Count != _count)
                {
                    Next(_loadSettings);
                }
                else if (_dLLists[0].YesPlayList)
                {
                    await Task.Delay(1000);
                    CheckProcess();
                    if (_downloadIsEnd)
                    {
                        _downloadIsEnd = false;
                        EndDownload(_loadSettings, false);
                    }
                }
                else
                {
                    _loading?.Close();
                    EndDownload(_loadSettings, false);
                }
            }
        }

        private void CheckProcess()
        {
            var processes = Process.GetProcessesByName("yt-dlp");
            if (processes.Length == 0)
            {
                Debug.WriteLine("Not runnning");
                _downloadIsEnd = true;
            }
            else
            {
                Debug.WriteLine("Running");
                _downloadIsEnd = false;
            }
        }

        private void Next(LoadSettings load)
        {
            load.SaveInfo(_dLLists[_count - 1].Url);
            _count += 1;
            Debug.WriteLine($"Count::{_count - 1}");
            var b = _dLLists[_count - 1].Url;
            var format = _dLLists[_count - 1].FormatDatas;
            Debug.WriteLine(b);
            _loading?.Close();
            DownloadAsync(b, format);
        }

        private void EndDownload(LoadSettings load, bool error)
        {
            load?.SaveInfo(_dLLists[_count - 1].Url);
            listView_Recent.ItemsSource = _vm.Recent;

            if (error)
            {
                Toast.ShowToast("Error!", "エラーが発生しました");
            }
            else
            {
                Toast.ShowToast("All Done!", "おわったお");
            }

            list.ClearValue(ItemsControl.ItemsSourceProperty); // ListViewをクリア

            _loading?.Close();

            _folder = "none";
            _count = 1;
            IsEnabled = true;
            progText.Content = "Download States";
            _dLLists.Clear();
            Title = _title;
            Go.IsEnabled = true;
            Clear.IsEnabled = true;
            AddUrlList.IsEnabled = true;
        }

        // WebView2関連のメソッド
        private void NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            webview.CoreWebView2.Navigate(e.Uri);
        }

        private void CompletedPage(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            SearchBox.Text = webview.CoreWebView2.Source;
        }

        private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            Debug.WriteLine("初期化完了");
            webview.CoreWebView2.Navigate("https://google.com");
        }

        private void Right_Click(object sender, RoutedEventArgs e)
        {
            webview.GoForward();
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            webview.Reload();
        }

        private void Left_Click(object sender, RoutedEventArgs e)
        {
            webview.GoBack();
        }

        private async void Download_Click_1(object sender, RoutedEventArgs e)
        {
            var getInfomation = new GetInfomation();
            var load = new Loading(false);
            load.Show();
            Notouch();
            var result = await getInfomation.Infomation(webview.CoreWebView2.Source);
            string title = result?.Title ?? "none";
            try
            {
                _dLLists.Add(new ViewModel.DLList
                {
                    Url = webview.CoreWebView2.Source,
                    Name = title,
                    Image = new Uri(result.Thumbnail),
                    IsLive = result.LiveStatus == LiveStatus.IsLive,
                    YesPlayList = webview.CoreWebView2.Source.Contains("list"),
                    Value = result.LiveStatus == LiveStatus.IsLive ? 12 : 90
                });
            }
            catch (Exception)
            {
                MessageBox.Show("このサイトに対応していない可能性があります\n詳しくはお問い合わせください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            list.ItemsSource = _dLLists;
            _isEnd = true;
            load.Close();
        }

        private async Task UrlsCheck(string[] urls)
        {
            var getInfomation = new GetInfomation();
            _loading = new Loading(false);
            string title = string.Empty;
            int a = 0;
            VideoData result;
            _loading.Show();
            Notouch();
            foreach (var url in urls)
            {
                if (IsValidUrl(url))
                {
                    result = await getInfomation.Infomation(url);
                    title = result.Title;
                    FormatData[] codecList = Array.Empty<FormatData>();
                    bool yesPlayList = false;
                    if(url.Contains("/channel") || url.Contains("@"))
                    {
                        yesPlayList = true;
                    }else if(url.Contains("list"))
                    {
                        yesPlayList = true;
                        codecList = await getInfomation.CodecInfomation(result);
                    }
                    else
                    {
                        codecList = await getInfomation.CodecInfomation(result);
                    }
                    _dLLists.Add(new ViewModel.DLList
                    {
                        Url = url,
                        Name = title,
                        Image = new Uri(result.Thumbnail),
                        IsLive = result.LiveStatus == LiveStatus.IsLive,
                        YesPlayList = yesPlayList,
                        FormatDatas = codecList
                    });
                }
                a++;
            }
            list.ItemsSource = _dLLists;
            _isEnd = true;
            _loading.Close();
        }

        public static bool IsValidUrl(string uri)
        {
            return Uri.IsWellFormedUriString(uri, UriKind.Absolute);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_cookiesPath))
            {
                using (var sm = new StreamReader(_cookiesPath))
                {
                    _cookieBrowser = sm.ReadToEnd();
                }
            }

            if (list.Items.Count == 0)
            {
                MessageBox.Show("URLを追加してください", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (_cancel)
                {
                    _cancel = false;
                    _folder = "none";
                }
                _loadSettings.ApplySettings();
                Go.IsEnabled = false;
                AddUrlList.IsEnabled = false;
                Clear.IsEnabled = false;
                string extractUrl = _dLLists[0].Url;
                var formats = _dLLists[0].FormatDatas;
                Debug.WriteLine(extractUrl);
                DownloadAsync(extractUrl, formats);
            }
        }

        private async void Info_Search(object sender, RoutedEventArgs e)
        {
            if (IsValidUrl(SearchBox_Info.Text))
            {
                var load = new Loading(false);
                var get = new GetInfomation();
                try
                {
                    Notouch();
                    load.Show();

                    var video = await get.Infomation(SearchBox_Info.Text);
                    if (video != null)
                    {
                        var bti = new BitmapImage(new Uri(video.Thumbnail));
                        thumbPic.Source = bti;
                        VideoTitle.Header = video.Title;
                        VidInfo.Text = video.ToString();
                        load.Close();
                        _isEnd = true;
                    }
                    load.Close();
                    _isEnd = true;
                }
                catch (Exception)
                {
                    MessageBox.Show("対応していないサイトかエラーが発生しました。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                    load.Close();
                    _isEnd = true;
                }
            }
            else
            {
                MessageBox.Show("URLを入力してください!", "注意", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _dLLists.Clear();
                Toast.ShowToast("Cancel", "キャンセルされました");
                list.ClearValue(ItemsControl.ItemsSourceProperty);
                _folder = "none";
                _count = 1;
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
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private void DeleteRecent_Click(object sender, RoutedEventArgs e)
        {
            _vm.Recent.Clear();
            string downloadRecentPath = @".\Recent\DownloadRecent.txt";
            File.Delete(downloadRecentPath);
            Toast.ShowToast("成功", "履歴を削除しました");
        }

        private void ListView_Recent_Selected(object sender, RoutedEventArgs e)
        {
            if (listView_Recent.SelectedItem != null)
            {
                string url = (listView_Recent.Items[listView_Recent.SelectedIndex] as ViewModel.VideoInfo).URI;
                Debug.WriteLine(url);
                try
                {
                    Clipboard.SetData(DataFormats.Text, url);
                    /*var pi = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    Process.Start(pi);*/
                    Toast.ShowToast("成功", "URLをクリップボードに貼り付けました");
                }
                catch (Exception)
                {
                    Toast.ShowToast("失敗", "URLの取得に失敗しました");
                }
            }
            listView_Recent.SelectedItem = null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _loadSettings.WriteSettings();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _dLLists.Clear();
        }

        private async void Add_Url_List(object sender, RoutedEventArgs e)
        {
            var addURl = new AddUrl();
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
                    //MessageBox.Show("使用できない文字列が入っているか、値が無効です。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                    //EndDownload(null, false);
                }
            }
        }

        private void Search_Clicked(object sender, RoutedEventArgs e)
        {
            webview.CoreWebView2.Navigate(SearchBox.Text);
        }
        private void SaveButton(object sender, RoutedEventArgs e)
        {
            LoadSettings settings = new LoadSettings();
            settings.WriteSettings();
        }
    }
}
