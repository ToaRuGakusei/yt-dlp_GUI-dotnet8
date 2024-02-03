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
        public async void saveInfo(string Url)
        {
            var result = await Infomation(Url);
            Directory.CreateDirectory(@".\Recent");
            using (StreamWriter sw = new StreamWriter(@".\Recent\DownloadRecent.txt", true))
            {
                sw.WriteLine($"{result.Title.ToString()},{result.Thumbnail}");
            }
            loadInfo();

        }
        public class VideoInfo
        {
            public string Title { get; set; }
        }
        public static ObservableCollection<VideoInfo> ob = new ObservableCollection<VideoInfo>();
        public void loadInfo()
        {
            using (StreamReader sm = new StreamReader(@".\Recent\DownloadRecent.txt"))
            {
                while (sm.Peek() == -1)
                {
                    ob.Add(new VideoInfo { Title = sm.ReadLine() });
                }
            }
        }
    }
}
