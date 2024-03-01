using System.Diagnostics;

namespace UpdateCheck
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                if (args[0] == "App") //アプリ経由の起動ではない場合、起動させない。
                {
                    string latest = @".\net8.0-windows10.0.17763.0"; //ディレクトリ
                    if (Directory.Exists(latest))
                    {
                        Delete(@".\");
                    }
                }
            }
            catch (Exception)
            {

            }


        }
        private static void Delete(string latest)
        {
            DirectoryInfo di = new DirectoryInfo(latest);
            FileInfo[] files = di.GetFiles();
            foreach (FileInfo file in files)
            {
                if (!file.Name.Contains("UpdateCheck")) //UpdateCheckerは消さないように
                {
                    file.Delete(); //ファイル削除
                }
            }
            Moveto(latest);
        }
        private static void Moveto(string latest)
        {
            DirectoryInfo di = new DirectoryInfo(latest + @"net8.0-windows10.0.17763.0");
            FileInfo[] files = di.GetFiles();
            Directory.Delete(@".\runtimes", true);
            Directory.Move(latest + @"net8.0-windows10.0.17763.0\runtimes", @".\runtimes");

            foreach (FileInfo file in files)
            {
                if(!file.Name.Contains("UpdateCheck"))
                {
                    file.MoveTo($@".\{Path.GetFileName(file.FullName)}");
                }
            }
            Directory.Delete(latest + @"net8.0-windows10.0.17763.0");

            //完了後
            Console.WriteLine("更新が完了しました。\nアプリの再起動を開始します。");
            Thread.Sleep(1000);
            ProcessStartInfo pi = new ProcessStartInfo()
            {
                FileName = @".\yt-dlp_GUI-dotnet8.exe",
            };
            Process.Start(pi);
            Environment.Exit(0);
        }
    }
}
