using System.Diagnostics;
using System.IO;

namespace yt_dlp_GUI_dotnet8.Tool
{
    internal class LoadSettings
    {
        public LoadSettings()
        {
            _viewModel = (App.Current as App).ViewModel;
            (App.Current as App).ViewModel = _viewModel;
        }
        ViewModel _viewModel;

        /// <summary>
        /// ここで設定をロード
        /// 2024/02/03作成
        /// </summary>
        /// 

        //以下より下は設定のメソッドしかないじゃない。
        private SetJson setJson = new SetJson();
        private DownloadSetting downloadSetting = new DownloadSetting();

        public void FirstWriteSettings()
        {
            try
            {
                downloadSetting.AudioCodec = 0;
                downloadSetting.AudioCodecIsEnable = false;
                downloadSetting.AudioOnly = 0;
                downloadSetting.AudioOnlyIsEnable = false;
                downloadSetting.Codec = 0;
                downloadSetting.CodecIsEnable = false;
                downloadSetting.CookiesIsEnable = false;
                downloadSetting.Cookies = "なし";
                downloadSetting.Extension = 0;
                downloadSetting.ExtensionIsEnable = false;
                downloadSetting.HighQualityVideoIsEnable = true;
                downloadSetting.Pixel = 5;
                downloadSetting.PixelIsEnable = false;
            }
            catch (Exception ex)
            {

            }


            setJson.saveJson(downloadSetting);
        }


        public void WriteSettings()
        {
            //なんかスマートじゃないじゃない
            try
            {
                downloadSetting.AudioCodec = _viewModel.CodecAudio;
                downloadSetting.AudioCodecIsEnable = _viewModel.CodecAudioIsChecked;
                downloadSetting.AudioOnly = _viewModel.AudioOnly;
                downloadSetting.AudioOnlyIsEnable = _viewModel.AudioOnlyIsChecked;
                downloadSetting.Codec = _viewModel.Codec;
                downloadSetting.CodecIsEnable = _viewModel.CodecIsChecked;
                downloadSetting.CookiesIsEnable = _viewModel.CookiesIsChecked;
                if (_viewModel.myCookies != null)
                {
                    downloadSetting.Cookies = _viewModel.myCookies;
                }
                else
                {
                    downloadSetting.Cookies = "なし";
                }
                downloadSetting.Extension = _viewModel.Extension;
                downloadSetting.ExtensionIsEnable = _viewModel.ExtensionIsChecked;
                downloadSetting.HighQualityVideoIsEnable = _viewModel.SetHighQuality;
                downloadSetting.Pixel = _viewModel.Pixel;
                downloadSetting.PixelIsEnable = _viewModel.PixelIsChecked;
            }
            catch (Exception ex)
            {

            }


            setJson.saveJson(downloadSetting);
        }

        private bool UseSettingsFile = false;

        public void Settings_Apply()
        {
            //設定ファイルへのアクセスを制限
            UseSettingsFile = true;
            if (!File.Exists(@".\Settings.json"))
            {
                StreamWriter sw = new StreamWriter(@".\Settings.json");
                sw.WriteLine(" ");
                sw.Close();
                FirstWriteSettings();
            }
            else
            {
                //設定ロード＾＾
                //ごり押ししすぎ
                SetJson setJson = new SetJson();

                var Load = setJson.readJson();
                _viewModel.CookiesIsChecked = Load.CookiesIsEnable;
                _viewModel.PixelIsChecked = Load.PixelIsEnable;
                _viewModel.CodecIsChecked = Load.CodecIsEnable;
                _viewModel.CodecAudioIsChecked = Load.AudioCodecIsEnable;
                _viewModel.ExtensionIsChecked = Load.ExtensionIsEnable;
                if (Load.Cookies != null)
                {
                    _viewModel.myCookies = Load.Cookies.Replace("\r\n", "").Replace("\"", "");
                }
                _viewModel.AudioOnlyIsChecked = Load.AudioOnlyIsEnable;
                _viewModel.Pixel = Load.Pixel;
                _viewModel.Codec = Load.Codec;
                _viewModel.CodecAudio = Load.AudioCodec;
                _viewModel.Extension = Load.Extension;
                _viewModel.AudioOnly = Load.AudioOnly;

                //設定反映(´・ω・)
                /*cookie.IsChecked = Cookies_Enabled;
                SetPixel.IsChecked = SetPixel_Enabled;
                CodecToggle.IsChecked = Codec_Enabled;
                Codec_Audio_Toggle.IsChecked = Codec_Audio_Enabled;
                container_Toggle.IsChecked = container_Enabled;
                audio_Only_Toggle.IsChecked = Audio_Only_Enabled;
                if (Codec_Audio != -1)
                    AudioConversion = AudioList[Codec_Audio];
                else
                    AudioConversion = AudioList[0];

                if (Audio_Only_Value != -1)
                    AudioOnlyConversion = AudioList[Audio_Only_Value];
                else
                    AudioOnlyConversion = AudioList[0];

                if (Codec != -1)
                    videoFormat = Codec_List[Codec];
                else
                    videoFormat = Codec_List[0];

                if (Merge != -1)
                {
                    mergeOutputFormat = mergeList[Merge];
                }
                else
                {
                    mergeOutputFormat = mergeList[0];
                }

                if (!(Pixel == -1))
                {
                    video_Value = video[Pixel];
                    combo.SelectedIndex = Pixel;
                }
                else
                {
                    video_Value = video[5];
                    combo.SelectedIndex = 0;
                }

                if (!(Codec == -1))
                {
                    codec.SelectedIndex = Codec;
                }
                else
                {
                    codec.SelectedIndex = 0;
                }

                if (!(Codec_Audio == -1))
                {
                    codec_Audio.SelectedIndex = Codec_Audio;
                    Audio_Value = audio[Codec_Audio];
                }
                else
                {
                    codec_Audio.SelectedIndex = 0;
                    Audio_Value = audio[1];
                }

                if (!(Merge == -1))
                {
                    container.SelectedIndex = Merge;
                }
                else
                {
                    container.SelectedIndex = 0;
                }

                if (!(Audio_Only_Value == -1))
                {
                    Only.SelectedIndex = Audio_Only_Value;
                }
                else
                {
                    Only.SelectedIndex = 0;
                }
                PasswordBox.Text = Cookie;
            }*/

                SetRecent(); //履歴セット

                //設定ファイルへのアクセスを解放
                UseSettingsFile = false;
            }




        }

        private void SetRecent()
        {
            _viewModel.Recent.Clear();
            loadInfo();
        }
        
        public string DownloadRecent_Path = @".\Recent\DownloadRecent.txt";
        public string Recent_Path = @".\Recent";
        /// <summary>
        /// ここで履歴を保存する
        /// </summary>
        /// <param name="Url"></param>
        public async void SaveInfo(string Url)
        {
            GetInfomation getInfomation = new GetInfomation();
            var result = await getInfomation.Infomation(Url);
            Directory.CreateDirectory(Recent_Path);
            using (StreamWriter sw = new StreamWriter(DownloadRecent_Path, true))
            {
                sw.WriteLine($"{result.Title.ToString()},{Url},{result.Thumbnail}");
            }
            loadInfo();
        }
        /// <summary>
        /// 設定を読み込む
        /// </summary>
        public void loadInfo()
        {
            if (File.Exists(DownloadRecent_Path))
            {
                _viewModel.Recent.Clear();
                using (StreamReader sm = new StreamReader(DownloadRecent_Path))
                {
                    while (sm.Peek() != -1)
                    {
                        string[] getInfo = sm.ReadLine().Split(',');
                        _viewModel.Recent.Add(new ViewModel.VideoInfo { Title = getInfo[0], image = new Uri(getInfo[2]), URI = getInfo[1] });
                        Debug.WriteLine($"Title = {getInfo[0]},image = new Uri({getInfo[2]}),URI = {getInfo[1]}");
                    }


                }
            }

        }
    }
}
