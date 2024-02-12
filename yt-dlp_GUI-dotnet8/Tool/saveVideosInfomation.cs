using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class saveVideosInfomation : GetInfomation
    {
        public class VideoInfo
        {
            public required string Title { get; set; }
            public required Uri image { get; set; }
            public required string URI { get; set; }

        }
        public static ObservableCollection<VideoInfo> ob = new ObservableCollection<VideoInfo>();
        public string DownloadRecent_Path = @".\Recent\DownloadRecent.txt";
        public string Recent_Path = @".\Recent";
        /// <summary>
        /// ここで履歴を保存する
        /// </summary>
        /// <param name="Url"></param>
        public async void SaveInfo(string Url)
        {
            var result = await Infomation(Url);
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
                ob.Clear();
                using (StreamReader sm = new StreamReader(DownloadRecent_Path))
                {
                    while(sm.Peek() != -1)
                    {
                        string[] getInfo = sm.ReadLine().Split(',');
                        ob.Add(new VideoInfo { Title = getInfo[0], image = new Uri(getInfo[2]), URI = getInfo[1] });
                        Debug.WriteLine($"Title = {getInfo[0]},image = new Uri({getInfo[2]}),URI = {getInfo[1]}");
                    }
                    

                }
            }

        }

    }
}
