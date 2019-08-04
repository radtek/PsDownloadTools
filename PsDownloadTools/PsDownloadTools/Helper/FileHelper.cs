using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PsDownloadTools.Helper
{
    class FileHelper
    {
        private static readonly String _dirError = $"{Application.StartupPath}\\Error.txt";

        public static void DeleteFile(String path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static void WriteErrorFile(String error)
        {
            File.AppendAllText(_dirError,
                $"\r\n--------------------------------------\r\n" +
                $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms")}" +
                $"\r\n--------------------------------------\r\n" +
                error, Encoding.UTF8);
        }
    }
}
