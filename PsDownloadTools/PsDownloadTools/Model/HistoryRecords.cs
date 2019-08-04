using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace PsDownloadTools.Helper
{
    class HistoryReocrds
    {
        private static readonly String _path = $"{Application.StartupPath}\\HistoryRecords.xml";
        private static Dictionary<String, String> _matches;

        public static void InitHistoryRecords()
        {
            if (File.Exists(_path))
            {
                _matches = XElement.Load(_path).Elements().Where(match => File.Exists(match.Element("LocalPath").Value)).ToDictionary(match => match.Element("PsnPath").Value, match => match.Element("LocalPath").Value);
            }
            else
            {
                new XElement("Matches", "").Save(_path);
                _matches = new Dictionary<String, String>();
            }
        }

        public static void Add(String psnPath, String localPath)
        {
            if (_matches.ContainsKey(psnPath))
            {
                _matches[psnPath] = localPath;
            }
            else
            {
                _matches.Add(psnPath, localPath);
            }

            XElement rootNode = new XElement("Matches");
            foreach (KeyValuePair<String, String> match in _matches)
            {
                rootNode.Add(new XElement("Match", new XElement("PsnPath", match.Key), new XElement("LocalPath", match.Value)));
            }
            rootNode.Save(_path);
        }

        public static void AddAll(Dictionary<String, String> matches)
        {
            foreach (KeyValuePair<String, String> match in matches)
            {
                if (_matches.ContainsKey(match.Key))
                {
                    _matches[match.Key] = match.Value;
                }
                else
                {
                    _matches.Add(match.Key, match.Value);
                }

            }

            XElement rootNode = new XElement("Matches");
            foreach (KeyValuePair<String, String> match in _matches)
            {
                rootNode.Add(new XElement("Match", new XElement("PsnPath", match.Key), new XElement("LocalPath", match.Value)));
            }
            rootNode.Save(_path);
        }

        public static void Clear()
        {
            _matches.Clear();

            XElement rootNode = new XElement("Matches");
            rootNode.Save(_path);
        }

        public static String GetLocalPath(String psnPath)
        {
            String psnName = new Uri(psnPath).Segments.Last();
            if (_matches.Any(match => match.Key.Contains(psnName)))
            {
                return _matches.First(match => match.Key.Contains(psnName)).Value;
            }
            else
            {
                return String.Empty;
            }
        }

        public static Dictionary<String, String> GetMatches()
        {
            return _matches;
        }
    }
}
