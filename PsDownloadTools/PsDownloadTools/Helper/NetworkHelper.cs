using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PsDownloadTools.Helper
{
    class NetworkHelper
    {
        public delegate void SetProgressStart();
        public delegate void SetProgress(String progress);
        public delegate void SetProgressFinish();

        public static List<String> GetIps()
        {
            List<String> ipList = new List<String>();

            try
            {
                String HostName = Dns.GetHostName();
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (Int32 i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipList.Add(IpEntry.AddressList[i].ToString());
                    }
                }
                return ipList;
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg(Application.Current.TryFindResource("StrAquireIpWrong") as String,e);
                return ipList;
            }
        }

        public static Boolean IsValidIp(String ip, out IPAddress ipAddress)
        {
            return IPAddress.TryParse(ip, out ipAddress);
        }

        public static Boolean IsMatchExt(String psnPath)
        {
            String[] exts = SettingsHelper.Exts.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (exts.Length == 0 || String.IsNullOrWhiteSpace(psnPath))
            {
                return false;
            }
            return exts.Any(ext => Regex.IsMatch(psnPath, ext.Replace(".", "\\."), RegexOptions.IgnoreCase));
        }

        public static async Task<String> GetFellowUrls(String psnPath, SetProgressStart setProgressStart, SetProgress setProgress, SetProgressFinish setProgressFinish)
        {
            if (Regex.IsMatch(psnPath, "_[0-1]?[0-9].pkg", RegexOptions.IgnoreCase))
            {
                setProgressStart();
                psnPath = await Task<String>.Factory.StartNew(() =>
                {
                    StringBuilder sb = new StringBuilder();
                    Int32 i = 0;
                    while (true)
                    {
                        String newPath = Regex.Replace(psnPath, "_[0-1]?[0-9].pkg", $"_{i}.pkg");
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            setProgress(new Uri(newPath).Segments.Last());
                        });

                        if (IsNetworkLastFilet(newPath))
                        {
                            break;
                        }
                        else
                        {
                            sb.AppendLine(newPath);
                            i++;
                        }
                    }

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        setProgressFinish();
                    });
                    return sb.ToString();
                });
            }
            return psnPath;
        }

        public static Boolean IsNetworkLastFilet(String psnPath)
        {
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(psnPath);
                httpWebRequest.Method = "HEAD";
                httpWebRequest.Timeout = 10 * 1000;
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Boolean isLastFile = true;
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    Int64 size = httpWebResponse.ContentLength;
                    isLastFile = size < 4L * 1024L * 1024L * 1024L;
                }
                httpWebResponse.Dispose();
                httpWebResponse.Close();
                return isLastFile;
            }
            catch
            {
                return true;
            }
        }
    }
}
