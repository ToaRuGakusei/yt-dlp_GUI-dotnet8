using Octokit;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class CheckUpdate
    {
        private const string owner = "ToaRuGakusei"; // GitHub リポジトリの所有者名
        private const string repository = "yt-dlp_GUI-dotnet8"; // GitHub リポジトリ名
        public string ReleaseUrl = "";
        public bool isEnd = false;

        public async Task Check()
        {
            var github = new GitHubClient(new ProductHeaderValue("yt-dlp-GUI-dotnet8"));
            var releases = await github.Repository.Release.GetAll(owner, repository);
            if (releases.Any())
            {
                var latestRelease = releases[0];
                var latestVersion = latestRelease.CreatedAt;
                Console.WriteLine($"最新のリリース: {latestRelease.TagName}");
                if (latestVersion.DateTime > File.GetCreationTime(@".\yt-dlp_GUI-dotnet8.exe"))
                {
                    var update = MessageBox.Show($"新しいバージョン({latestVersion})が見つかりました。\nアップデートしますか？", "お知らせ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (update != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    else
                    {
                        ReleaseUrl = latestRelease.Assets.FirstOrDefault().Url;
                        Debug.WriteLine($"Url: {ReleaseUrl}");
                        FileDownloader fileDownloader = new FileDownloader();
                        DownloadNow dln = new DownloadNow();
                        dln.Show();
                        await latest_Download(fileDownloader, dln);
                        isEnd = true;
                        //通知する
                    }
                }
                // ここで最新のバージョンと比較して、必要に応じて通知およびダウンロードを行うロジックを追加できます
                // 例えば、既存のバージョンと最新のバージョンを比較して、新しいバージョンが利用可能であれば通知するなど
                // または、ダウンロードリンクを提供するためのURLを生成するなどの処理もここで行います
            }
            else
            {
                Console.WriteLine("リリースが見つかりません。");
            }

        }
        private async Task latest_Download(FileDownloader fld, DownloadNow dln)
        {
            var latest = await fld.GetContent(ReleaseUrl, "application/octet-stream");
            ZipFile.ExtractToDirectory(latest, @".\");
            latest.Close();
            dln.Close();
            Toast.ShowToast("Download Done!", "latestのダウンロードが終わりました\nアプリを再起動します。");
            ProcessStartInfo pi = new ProcessStartInfo()
            {
                FileName = @".\UpdateCheck.exe",
                Arguments = "App" ,
                UseShellExecute = true,
            };
            Process.Start(pi);
            Environment.Exit(0);
        }
    }
}

