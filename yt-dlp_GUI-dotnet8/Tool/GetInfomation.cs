using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using static yt_dlp_GUI_dotnet8.Tool.GetInfomation;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class GetInfomation
    {
        public sealed class Formats
        {
            public string VideoFormatID { get; set; }
            public string VideoFormats { get; set; }

            public string AudioFormatID { get; set; }
            public string AudioFormats { get; set; }
        }
        public GetInfomation()
        {
            _vm = (App.Current as App).ViewModel;
        }
        ViewModel _vm;
        public async Task<FormatData[]> CodecInfomation(VideoData data)
        {
#pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
            return await Task.Run(() =>
            {
                ObservableCollection<Formats> VideoFormats = new ObservableCollection<Formats>(); //コーディック情報を格納
                FormatData[] formats = null;
                try
                {
                    var videoData = data;
                    formats = videoData.Formats;
                    foreach (var format in formats)
                    {
                        VideoFormats.Add(new Formats { VideoFormats = format.Format, VideoFormatID = format.FormatId, AudioFormats = format.Format.Contains("Audio") ? format.Format : "none", AudioFormatID = format.AudioCodec });
                        Debug.WriteLine($"フォーマットID＝{format.FormatId}\n" +
                            $"フォーマット詳細＝{format.Format}\n" +
                            $"オーディオフォーマット＝{format.AudioCodec}");
                    }

                }
                catch (Exception e)
                {
                    //MessageBox.Show("何らかのエラーにより、処理ができませんでした。\nURLが正しいかご確認ください。");
                    Debug.WriteLine(e);
                    formats = Array.Empty<FormatData>();
                    return formats;
                }
                return formats;

            });
        }


        public async Task<VideoData> Infomation(string url)
        {
#pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
            return await Task.Run(async () =>
            {
                var ytdl = new YoutubeDL();
                ytdl.YoutubeDLPath = @".\yt-dlp.exe";
                ytdl.FFmpegPath = @".\ffmpeg.exe";
                var options = new OptionSet()
                {
                    Cookies = _vm.myCookies.Replace("\"",""), //クッキーを指定
                };
                VideoData videoData = new VideoData();
                try
                {
                    var res = await ytdl.RunVideoDataFetch(url,CancellationToken.None,true,false,options);
                    videoData = res.Data;
                    var formats = videoData.Formats;

                }
                catch (Exception)
                {
                    MessageBox.Show("何らかのエラーにより、処理ができませんでした。\nURLが正しいかご確認ください。");
                }
                return videoData;

            });
        }
    }
}
