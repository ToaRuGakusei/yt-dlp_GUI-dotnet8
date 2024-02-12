using System.Windows;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class GetInfomation
    {
        public async Task<VideoData> Infomation(string url)
        {
#pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
            return await Task.Run(async () =>
            {
                var ytdl = new YoutubeDL();
                ytdl.YoutubeDLPath = @".\yt-dlp.exe";
                ytdl.FFmpegPath = @".\ffmpeg.exe";
                VideoData videoData = null;
                try
                {
                    var res = await ytdl.RunVideoDataFetch(url);
                    videoData = res.Data;
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
