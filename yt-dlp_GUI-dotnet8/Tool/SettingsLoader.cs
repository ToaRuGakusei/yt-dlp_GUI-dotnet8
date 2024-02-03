using System.Diagnostics;
using System.IO;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class SettingsLoader
    {
        public string SettingGetter(string name)
        {
            if (File.Exists($@".\Settings\{name}.txt"))
            {
                StreamReader sm = new StreamReader($@".\Settings\{name}.txt");
                string result = sm.ReadToEnd();
                sm.Close();
                return result;
            }
            else
            {
                return "-9";
            }

        }
        public string SettingEnabled_Check(string name)
        {
            if (File.Exists($@".\Settings\{name}.txt"))
            {
                StreamReader sm = new StreamReader($@".\Settings\{name}.txt");

                string result = sm.ReadLine();
                sm.Close();
                Debug.WriteLine(result);
                return result;

            }
            else
            {
                return "false";
            }


        }

    }
}
