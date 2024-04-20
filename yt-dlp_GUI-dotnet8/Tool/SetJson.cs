using Newtonsoft.Json;
using System.IO;

namespace yt_dlp_GUI_dotnet8.Tool
{
    [JsonObject("Settings")]
    public sealed class DownloadSetting
    {
        [JsonProperty("HighQualityVideoIsEnable")]
        public bool HighQualityVideoIsEnable { get; set; }
        [JsonProperty("Cookies")]
        public string Cookies { get; set; }
        [JsonProperty("CookiesIsEnable")]

        public bool CookiesIsEnable { get; set; }
        [JsonProperty("Pixel")]

        public int Pixel { get; set; }
        [JsonProperty("PixelIsEnable")]

        public bool PixelIsEnable { get; set; }
        [JsonProperty("Codec")]
        public int Codec { get; set; }
        [JsonProperty("CodecIsEnable")]

        public bool CodecIsEnable { get; set; }
        [JsonProperty("AudioCodec")]

        public int AudioCodec { get; set; }
        [JsonProperty("AudioCodecIsEnable")]

        public bool AudioCodecIsEnable { get; set; }
        [JsonProperty("Extension")]

        public int Extension { get; set; }
        [JsonProperty("ExtensionIsEnable")]

        public bool ExtensionIsEnable { get; set; }
        [JsonProperty("AudioOnly")]

        public int AudioOnly { get; set; }
        [JsonProperty("AudioOnlyIsEnable")]

        public bool AudioOnlyIsEnable { get; set; }
    }
    public class SetJson
    {

        private string defaultPath = @".\Settings.json";
        public void saveJson(DownloadSetting list)
        {
            var jsonWriteData = JsonConvert.SerializeObject(list);

            //ここでjson形式で保存する
            using (var sw = new StreamWriter(defaultPath, false, System.Text.Encoding.UTF8))
            {
                sw.Write(jsonWriteData);
            }
        }
        public DownloadSetting readJson()
        {
            DownloadSetting downloadSetting = new DownloadSetting();
            using (var sr = new StreamReader(defaultPath, System.Text.Encoding.UTF8))
            {
                var jsonReadData = sr.ReadToEnd();
                downloadSetting = JsonConvert.DeserializeObject<DownloadSetting>(jsonReadData);
            }
            return downloadSetting;
        }
    }
}
