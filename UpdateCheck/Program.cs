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
                    string latest = @".\update\net8.0-windows10.0.17763.0"; //ディレクトリ
                    if (Directory.Exists(latest))
                    {
                        Moveto(@".\");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("不正な起動を確認\n何かキーを押してください...");
                Console.ReadKey();
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
            DirectoryInfo di = new DirectoryInfo(latest + @"update\net8.0-windows10.0.17763.0");
            FileInfo[] files = di.GetFiles();
            Directory.Delete(@".\runtimes", true);
            Directory.Move(latest + @"update\net8.0-windows10.0.17763.0\runtimes", @".\runtimes");

            foreach (FileInfo file in files)
            {
                if(!file.Name.Contains("UpdateCheck"))
                {
                    File.Copy(file.FullName, $@".\{Path.GetFileName(file.FullName)}", true);
                }
            }
            Directory.Delete(latest + @"update",true);

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
