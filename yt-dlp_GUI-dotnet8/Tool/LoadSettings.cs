using System.Diagnostics;
using System.IO;

namespace yt_dlp_GUI_dotnet8.Tool
{
    internal class LoadSettings
    {
        private readonly ViewModel _viewModel;
        private readonly SetJson _setJson = new SetJson();
        private readonly DownloadSetting _downloadSetting = new DownloadSetting();
        private bool _useSettingsFile = false;
        public LoadSettings()
        {
            _viewModel = (App.Current as App)?.ViewModel ?? throw new NullReferenceException("ViewModel is null");
        }

        public void FirstWriteSettings()
        {
            try
            {
                _downloadSetting.AudioCodec = 1;
                _downloadSetting.AudioCodecIsEnable = true;
                _downloadSetting.AudioOnly = 0;
                _downloadSetting.AudioOnlyIsEnable = false;
                _downloadSetting.Codec = 0;
                _downloadSetting.CodecIsEnable = true;
                _downloadSetting.CookiesIsEnable = false;
                _downloadSetting.Cookies = "なし";
                _downloadSetting.Extension = 1;
                _downloadSetting.ExtensionIsEnable = true;
                _downloadSetting.HighQualityVideoIsEnable = true;
                _downloadSetting.Pixel = 5;
                _downloadSetting.PixelIsEnable = true;

                _setJson.saveJson(_downloadSetting);
                ApplySettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FirstWriteSettings: {ex.Message}");
            }
        }

        public void WriteSettings()
        {
            try
            {
                _downloadSetting.AudioCodec = _viewModel.CodecAudio;
                _downloadSetting.AudioCodecIsEnable = _viewModel.CodecAudioIsChecked;
                _downloadSetting.AudioOnly = _viewModel.AudioOnly;
                _downloadSetting.AudioOnlyIsEnable = _viewModel.AudioOnlyIsChecked;
                _downloadSetting.Codec = _viewModel.Codec;
                _downloadSetting.CodecIsEnable = _viewModel.CodecIsChecked;
                _downloadSetting.CookiesIsEnable = _viewModel.CookiesIsChecked;
                _downloadSetting.Cookies = _viewModel.myCookies ?? string.Empty;
                _downloadSetting.Extension = _viewModel.Extension;
                _downloadSetting.ExtensionIsEnable = _viewModel.ExtensionIsChecked;
                _downloadSetting.HighQualityVideoIsEnable = _viewModel.SetHighQuality;
                _downloadSetting.Pixel = _viewModel.Pixel;
                _downloadSetting.PixelIsEnable = _viewModel.PixelIsChecked;

                _setJson.saveJson(_downloadSetting);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in WriteSettings: {ex.Message}");
            }
        }

        public void ApplySettings()
        {
            try
            {
                _useSettingsFile = true;

                if (!File.Exists(@".\Settings.json"))
                {
                    using (StreamWriter sw = new StreamWriter(@".\Settings.json"))
                    {
                        sw.WriteLine(" ");
                    }

                    FirstWriteSettings();
                }
                else
                {
                    SetJson setJson = new SetJson();
                    var loadedSettings = setJson.readJson();

                    _viewModel.CookiesIsChecked = loadedSettings.CookiesIsEnable;
                    _viewModel.PixelIsChecked = loadedSettings.PixelIsEnable;
                    _viewModel.CodecIsChecked = loadedSettings.CodecIsEnable;
                    _viewModel.CodecAudioIsChecked = loadedSettings.AudioCodecIsEnable;
                    _viewModel.ExtensionIsChecked = loadedSettings.ExtensionIsEnable;
                    _viewModel.myCookies = loadedSettings.Cookies?.Replace("\r\n", "").Replace("\"", "");
                    _viewModel.AudioOnlyIsChecked = loadedSettings.AudioOnlyIsEnable;
                    _viewModel.Pixel = loadedSettings.Pixel;
                    _viewModel.Codec = loadedSettings.Codec;
                    _viewModel.CodecAudio = loadedSettings.AudioCodec;
                    _viewModel.Extension = loadedSettings.Extension;
                    _viewModel.AudioOnly = loadedSettings.AudioOnly;

                    SetRecent(); // Load recent history
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplySettings: {ex.Message}");
            }
            finally
            {
                _useSettingsFile = false;
            }
        }

        private void SetRecent()
        {
            _viewModel.Recent.Clear();
            LoadInfo();
        }

        private readonly string _downloadRecentPath = @".\Recent\DownloadRecent.txt";
        private readonly string _recentPath = @".\Recent";

        public async void SaveInfo(string url)
        {
            try
            {
                var getInfomation = new GetInfomation();
                var result = await getInfomation.Infomation(url);
                Directory.CreateDirectory(_recentPath);

                using (StreamWriter sw = new StreamWriter(_downloadRecentPath, true))
                {
                    sw.WriteLine($"{result.Title},{url},{result.Thumbnail}");
                }

                LoadInfo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveInfo: {ex.Message}");
            }
        }

        public void LoadInfo()
        {
            try
            {
                if (File.Exists(_downloadRecentPath))
                {
                    _viewModel.Recent.Clear();

                    using (StreamReader sr = new StreamReader(_downloadRecentPath))
                    {
                        while (sr.Peek() != -1)
                        {
                            var getInfo = sr.ReadLine().Split(',');
                            _viewModel.Recent.Add(new ViewModel.VideoInfo
                            {
                                Title = getInfo[0],
                                image = new Uri(getInfo[2]),
                                URI = getInfo[1]
                            });

                            Debug.WriteLine($"Title = {getInfo[0]}, image = new Uri({getInfo[2]}), URI = {getInfo[1]}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadInfo: {ex.Message}");
            }
        }
    }
}
