using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Gaming.XboxLive.Storage;

namespace yt_dlp_GUI_dotnet8.Tool
{
    /// <summary>
    /// Loading.xaml の相互作用ロジック
    /// </summary>
    public partial class Loading : Window
    {
        /// <summary>
        /// メニューのハンドル取得
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="bRevert"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        /// <summary>
        /// メニュー項目の削除
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="uPosition"></param>
        /// <param name="uFlags"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        private const int SC_CLOSE = 0xf060;

        /// <summary>
        /// uPositionに設定するのは項目のID
        /// </summary>
        private const int MF_BYCOMMAND = 0x0000;
        public Loading(bool isLive)
        {
            InitializeComponent();
            this.ResizeMode = ResizeMode.NoResize;
            ChangeTheme();
            if(isLive)
            {
                downloadRead.Content = "ライブ配信を録画中\n終了までお待ちください";
            }
            Bar.IsIndeterminate = true;
        }
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper((Window)sender).Handle;
            IntPtr hMenu = GetSystemMenu(hwnd, false);
            RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);
        }
        private static void ChangeTheme()
        {
            PaletteHelper palette = new PaletteHelper();
            ITheme theme = palette.GetTheme();
            theme.SetBaseTheme(Theme.Dark);
            palette.SetTheme(theme);
        }
    }
}
