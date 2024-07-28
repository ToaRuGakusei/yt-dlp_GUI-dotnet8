using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Windows.Media.Capture.Frames;
using yt_dlp_GUI_dotnet8.Tool;

namespace yt_dlp_GUI_dotnet8
{
    public class ViewModel:Prism.Mvvm.BindableBase
    {
        public ViewModel() { }
        private string _MyCookies = "";
        public string myCookies 
        { 
            get=> _MyCookies; 
            set => SetProperty(ref _MyCookies, value,nameof(myCookies));
        }
        private int _Pixel;
        public int Pixel
        {
            get => _Pixel;
            set=> SetProperty(ref _Pixel, value,nameof(Pixel));
        }
        private bool _PixelIsChecked =false;
        public bool PixelIsChecked
        {
            get => _PixelIsChecked;
            set => SetProperty(ref _PixelIsChecked, value,nameof(PixelIsChecked));
        }
        private bool _SetHighQuality = true;
        public bool SetHighQuality
        {
            get => _SetHighQuality;
            set => SetProperty(ref _SetHighQuality, value,nameof(SetHighQuality));
        }
        private int _Extension;
        public int Extension
        {
            get => _Extension;
            set => SetProperty(ref _Extension, value,nameof(Extension));
        }
        private bool _ExtensionIsChecked = false;
        public bool ExtensionIsChecked
        {
            get => _ExtensionIsChecked;
            set => SetProperty(ref _ExtensionIsChecked, value,nameof(ExtensionIsChecked));
        }
        private bool _CookiesIsChecked = false;
        public bool CookiesIsChecked
        {
            get => _CookiesIsChecked;
            set=> SetProperty(ref _CookiesIsChecked, value,nameof(CookiesIsChecked));
        }
        private int _Codec;
        public int Codec
        {
            get => _Codec;
            set => SetProperty(ref _Codec,value,nameof(Codec));
        }
        private bool _CodecIsChecked = false;
        public bool CodecIsChecked
        {
            get => _CodecIsChecked;
            set => SetProperty(ref _CodecIsChecked,value,nameof(CodecIsChecked));
        }
        private bool _AudioOnlyIsChecked = false;
        public bool AudioOnlyIsChecked
        {
            get=> _AudioOnlyIsChecked;
            set => SetProperty(ref _AudioOnlyIsChecked,value,nameof(AudioOnlyIsChecked));
        }
        private int _AudioOnly;
        public int AudioOnly
        {
            get => _AudioOnly;
            set => SetProperty(ref _AudioOnly,value,nameof(AudioOnly)); 
        }
        private int _CodecAudio;
        public int CodecAudio
        {
            get=> _CodecAudio;
            set=> SetProperty(ref _CodecAudio,value,nameof(CodecAudio));
        }
        private bool _CodecAudioIsChecked = false;
        public bool CodecAudioIsChecked
        {
            get => _CodecAudioIsChecked;
            set => SetProperty(ref _CodecAudioIsChecked,value,nameof(CodecAudioIsChecked));
        }
        private ObservableCollection<VideoInfo> _Recent = new ObservableCollection<VideoInfo>();
        public ObservableCollection<VideoInfo> Recent
        {
            get => _Recent;
            set => SetProperty(ref _Recent,value,nameof(Recent));
        }
        public class VideoInfo
        {
            public required string Title { get; set; }
            public required Uri image { get; set; }
            public required string URI { get; set; }

        }
    }
}
