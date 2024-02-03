using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class saveVideosInfomation : GetInfomation
    {
        public class VideoInfo
        {
            public required string Title { get; set; }
        }
        public static ObservableCollection<VideoInfo> ob = new ObservableCollection<VideoInfo>();
        public string DownloadRecent_Path = @".\Recent\DownloadRecent.txt";
        public string Recent_Path = @".\Recent";
        /// <summary>
        /// ここで履歴を保存する
        /// </summary>
        /// <param name="Url"></param>
        public async void saveInfo(string Url)
        {
            var result = await Infomation(Url);
            Directory.CreateDirectory(Recent_Path);
            using (StreamWriter sw = new StreamWriter(DownloadRecent_Path, true))
            {
                sw.WriteLine($"{result.Title.ToString()},{result.Thumbnail}");
            }
            loadInfo();
        }
        /// <summary>
        /// 設定を読み込む
        /// </summary>
        public void loadInfo()
        {
            using (StreamReader sm = new StreamReader(DownloadRecent_Path))
            {
                while (sm.Peek() == -1)
                {
                    ob.Add(new VideoInfo { Title = sm.ReadLine() });
                }
            }
        }
    }
}
