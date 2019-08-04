using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PsDownloadTools.Helper
{
    class SettingsHelper : ApplicationSettingsBase
    {
        [DllImport("kernel32")]
        private static extern Int64 WritePrivateProfileString(String section, String key, String value, String filePath);
        [DllImport("kernel32")]
        private static extern Int32 GetPrivateProfileString(String section, String key, String def, StringBuilder retVal, Int32 size, String filePath);
        private static readonly String _path = Application.StartupPath + "\\Setting.ini";

        private static String ReadIni(String section, String key, String defaultValue)
        {
            StringBuilder stringBuilder = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, stringBuilder, 255, _path);
            return stringBuilder.ToString();
        }

        private static void WriteIni(String section, String key, String value)
        {
            WritePrivateProfileString(section, key, value, _path);
        }

        public static void InitSettings()
        {
            if (!File.Exists(_path))
            {
                WriteIni("Main", "Ip", Ip);
                WriteIni("Main", "Port", Port);
                WriteIni("Main", "Exts", Exts);
                WriteIni("Main", "Lang", Lang);
                WriteIni("Main", "DownloadPath", DownloadPath);
            }
        }

        public static String Ip
        {
            get { return ReadIni("Main", "Ip", String.Empty); }
            set { WriteIni("Main", "Ip", value); }
        }

        public static String Port
        {
            get { return ReadIni("Main", "Port", "8080"); }
            set { WriteIni("Main", "Port", value); }
        }

        public static String Exts
        {
            get { return ReadIni("Main", "Exts", ".pkg|.pup|.vpk"); }
            set { WriteIni("Main", "Exts", value); }
        }

        public static String Lang
        {
            get { return ReadIni("Main", "Lang", CultureInfo.CurrentCulture.TwoLetterISOLanguageName); }
            set { WriteIni("Main", "Lang", value); }
        }

        public static String DownloadPath
        {
            get { return ReadIni("Main", "DownloadPath", Application.StartupPath + "\\downloads"); }
            set { WriteIni("Main", "DownloadPath", value); }
        }
    }
}
