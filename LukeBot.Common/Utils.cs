using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LukeBot.Common
{
    class Utils
    {
        public static Process StartBrowser(string url)
        {
            Process result = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                result = Process.Start("xdg-open", url);
            }
            else
            {
                throw new Exception.UnsupportedPlatformException("Platform is not supported");
            }

            return result;
        }
    }
}
