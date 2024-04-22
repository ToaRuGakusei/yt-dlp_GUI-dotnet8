using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace yt_dlp_GUI_dotnet8
{
    /// <summary>
    /// AddUrl.xaml の相互作用ロジック
    /// </summary>
    public partial class AddUrl : Window
    {
        public AddUrl()
        {
            InitializeComponent();
            PaletteHelper palette = new PaletteHelper();

            ITheme theme = palette.GetTheme();

            theme.SetBaseTheme(Theme.Dark);
            palette.SetTheme(theme);

            MouseLeftButtonDown += (sender, e) => { DragMove(); };
        }
        public string[] urls;
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string tmp = Box.Text;
            urls = tmp.Split("\r\n");
            this.Close();
        }
        private void Close_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
