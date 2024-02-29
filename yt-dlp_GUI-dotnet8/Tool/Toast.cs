using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
