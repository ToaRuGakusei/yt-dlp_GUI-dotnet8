using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class SetJson
    {
        public sealed class DownloadSetting
        {
            public bool HighQualityVideoIsEnable {  get; set; }
            public string Cookies {  get; set; }
            public bool CookiesIsEnable { get; set; }
            public string Pixel {  get; set; }
            public bool PixelIsEnable { get; set; }
            public string Codec { get; set; }
            public string CodexIsEnable {  get; set; }
            public string AudioCodec {  get; set; }
            public bool AudioCodecIsEnable {  get; set; }
            public string Extension {  get; set; }
            public bool ExtensionIsEnable { get; set; } 
            public string AudioOnly {  get; set; }
            public bool AudioOnlyIsEnable {  get; set; }
        }

        public void saveJson(DownloadSetting list)
        {

        }
    }
}
