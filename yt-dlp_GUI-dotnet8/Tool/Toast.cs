using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace yt_dlp_GUI_dotnet8.Tool
{
    public class Toast
    {
        public static void ShowToast(string title, string body)
        {
            new ToastContentBuilder()
                        .AddText(title)
                        .AddText(body)
                        .SetToastDuration(ToastDuration.Short)
                        .SetToastScenario(ToastScenario.Default)
                        .Show();
        }
        public static void ShowToast(string title, string body, string uri)
        {
            new ToastContentBuilder()
                        .AddText(title)
                        .AddText(body)
                        .AddHeroImage(new Uri(uri))
                        .SetToastDuration(ToastDuration.Short)
                        .SetToastScenario(ToastScenario.Default)
                        .Show();
        }
    }
}
