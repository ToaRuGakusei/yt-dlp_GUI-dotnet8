using Octokit;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class CheckUpdate
    {
        public string ReleaseUrl = "";
        public bool isEnd = false;

        public async Task Check(string repo, string owner)
        {
            var github = new GitHubClient(new ProductHeaderValue("yt-dlp-GUI-dotnet8"));
            var releases = await github.Repository.Release.GetAll(owner, repo);
            if (releases.Any())
            {
                var latestRelease = releases[0];
                var latestVersion = latestRelease.CreatedAt;
                var lastUpdate = "";
                try
                {
                    using (StreamReader sm = new StreamReader(@".\.lastUpdate.txt"))
                    {
                        lastUpdate = sm.ReadLine();
                    }
                }
                catch(Exception ex)
                {
                    lastUpdate = ("2024/07/28 18:00:00");
                }

                Debug.WriteLine(Convert.ToDateTime(lastUpdate));
                Debug.WriteLine(latestVersion.DateTime.AddHours(9));

                //JSTで日付を管理（GitHub側は世界標準時間になっているから、9時間プラスして日本時間に変更）
                if (repo == "yt-dlp_GUI-dotnet8" && latestVersion.DateTime.AddHours(9) > Convert.ToDateTime("2024/07/28 18:10:00"))
                {
                    var update = MessageBox.Show($"新しいバージョン({latestRelease.TagName})が見つかりました。\nアップデートしますか？", "お知らせ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (update != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    else
                    {
                        ReleaseUrl = latestRelease.Assets[0].Url;
                        Debug.WriteLine($"Url: {ReleaseUrl}");
                        FileDownloader fileDownloader = new FileDownloader();
                        DownloadNow dln = new DownloadNow();
                        dln.Show();
                        await latest_Download(fileDownloader, dln, repo);
                        isEnd = true;
                    }
                }
                else if(repo == "yt-dlp" && latestVersion.DateTime.AddHours(9) > Convert.ToDateTime(lastUpdate))
                {
                    var update = MessageBox.Show($"新しいYT-DLPのバージョン({latestRelease.TagName})が見つかりました。\nアップデートしますか？", "お知らせ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (update != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    else
                    {
                        ReleaseUrl = latestRelease.Assets[5].Url;
                        Debug.WriteLine($"Url: {ReleaseUrl}");
                        FileDownloader fileDownloader = new FileDownloader();
                        DownloadNow dln = new DownloadNow();
                        dln.Show();
                        await latest_Download(fileDownloader, dln, repo);
                        isEnd = true;
                    }
                }
            }
            else
            {
                Console.WriteLine("リリースが見つかりません。");
            }

        }
        private async Task latest_Download(FileDownloader fld, DownloadNow dln, string repo)
        {
            var latest = await fld.GetContent(ReleaseUrl, "application/octet-stream");
            DateTime dt = DateTime.Now;
            if (repo == "yt-dlp")
            {
                using (StreamWriter sw = new StreamWriter(@".\yt-dlp.exe", true))
                {
                    sw.Write(latest);
                }
                latest.Close();
                dln.Close();
                Toast.ShowToast("Download Done!", "YT-DLPのアップデートが終わりました。");
                using(StreamWriter sw = new StreamWriter(@".\.lastUpdate.txt",false))
                {
                    sw.WriteLine(dt.ToString("yyyy/MM/dd HH:mm:ss"));
                }
            }
            else
            {
                if (Directory.Exists(@".\update"))
                {
                    Directory.Delete(@".\update", true);
                }

                ZipFile.ExtractToDirectory(latest, @".\update");
                latest.Close();
                dln.Close();
                Toast.ShowToast("Download Done!", "アップデートのダウンロードが終わりました\nアプリを再起動します。");
                ProcessStartInfo pi = new ProcessStartInfo()
                {
                    FileName = @".\UpdateCheck.exe",
                    Arguments = "App",
                    UseShellExecute = true,
                };
                Process.Start(pi);
                Environment.Exit(0);
            }          
        }
    }

}

