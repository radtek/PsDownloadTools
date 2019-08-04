using PsDownloadTools.Bean;
using PsDownloadTools.Helper;
using PsDownloadTools.Model;
using PsDownloadTools.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace PsDownloadTools.Presenter
{
    class PresenterMain
    {
        private readonly ViewMain _view;
        private readonly ObservableCollection<ObjectRequest> _list = new ObservableCollection<ObjectRequest>();
        private static HttpListener _listener;

        public PresenterMain(ViewMain view)
        {
            _view = view;
            Init();
        }

        private async void Init()
        {
            SettingsHelper.InitSettings();
            HistoryReocrds.InitHistoryRecords();

            _view.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            _view.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            _view.SetLbTitleVersion($" - {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            SetIpPort();
            _view.LvSetItemSource(_list);

            await Aria2Manager.GetInstance().Init(b =>
            {
                if (b)
                {
                    _view.SetControlsEnable();
                    _view.SetBadge(Aria2Manager.GetInstance().GetDownloadItems().Count);
                }
                else
                {
                    ExceptionHelper.ShowErrorMsg(_view.FindResource("StrDownloadManagerConnectError") as String);
                }
            }, b =>
            {
                if (b)
                {
                    _view.SetBadge(Aria2Manager.GetInstance().GetDownloadItems().Count);
                }
            }, (psn, local) =>
            {
                if (!String.IsNullOrEmpty(psn) && !String.IsNullOrEmpty(local))
                {
                    _view.SetBadge(Aria2Manager.GetInstance().GetDownloadItems().Count);
                    HistoryReocrds.Add(psn, local);
                }
            });
        }

        public void OnClosing()
        {
            Aria2Manager.GetInstance().ClearInitHandlers();
        }

        public void OpenAbout()
        {
            ViewAbout viewAbout = new ViewAbout { Owner = _view };
            viewAbout.ShowDialog();
        }

        public void OpenPage()
        {
            Process.Start("https://github.com/VoidStudioCode/PsDownloadTools");
        }

        public void SetIpPort()
        {
            List<String> ipList = NetworkHelper.GetIps();
            _view.CbIpSetItemSource(ipList);
            if (ipList.Count > 0)
            {
                if (ipList.Contains(SettingsHelper.Ip))
                {
                    _view.CbIpSetSelection(ipList.IndexOf(SettingsHelper.Ip));
                }
                else
                {
                    _view.CbIpSetSelection(0);
                }
            }
            _view.TbPortSetText(SettingsHelper.Port);
        }

        public void StartServer(String ip, String port)
        {
            try
            {
                if (_listener == null)
                {
                    if (String.IsNullOrWhiteSpace(ip))
                    {
                        ExceptionHelper.ShowErrorMsg(_view.TryFindResource("StrIpEmpty") as String);
                    }
                    Boolean isValid = NetworkHelper.IsValidIp(ip, out System.Net.IPAddress ipAddress);
                    if (!isValid)
                    {
                        ExceptionHelper.ShowErrorMsg(_view.TryFindResource("StrIpWrong") as String);
                    }

                    if (String.IsNullOrWhiteSpace(port))
                    {
                        ExceptionHelper.ShowErrorMsg(_view.TryFindResource("StrPortEmpty") as String);
                    }
                    isValid = Int32.TryParse(port, out Int32 portInt);
                    if (!isValid)
                    {
                        ExceptionHelper.ShowErrorMsg(_view.TryFindResource("StrPortWrong") as String);
                    }
                    else if (portInt < 0 || portInt > 65535)
                    {
                        ExceptionHelper.ShowErrorMsg(_view.TryFindResource("StrPortWrongRange") as String);
                    }

                    SettingsHelper.Ip = ip;
                    SettingsHelper.Port = port;

                    _listener = new HttpListener(AddToList);
                    _listener.Start(ipAddress, portInt);
                    _view.SetState(true);
                }
                else
                {
                    _listener.Dispose();
                    _listener = null;
                    _view.SetState(false);
                }

            }
            catch (Exception e)
            {
                if (_listener != null)
                {
                    _listener.Dispose();
                    _listener = null;
                }

                ExceptionHelper.ShowErrorMsg(_view.TryFindResource("StrStartFail") as String, e);
            }
        }

        public void OpenSettings()
        {
            ViewSettings viewSettings = new ViewSettings { Owner = _view };
            viewSettings.ShowDialog();
        }

        public void OpenDownloadManager()
        {
            ViewDownload viewDownload = new ViewDownload { Owner = _view };
            viewDownload.ShowDialog();
        }

        public void ShowHistory()
        {
            ViewHistory viewHistory = new ViewHistory { Owner = _view };
            viewHistory.ShowDialog();
        }

        public void ClearHistory()
        {
            if (new DialogMessage(_view.TryFindResource("StrClearHistory") as String, String.Empty, DialogMessage.Buttons.YesNo) { Owner = _view }.ShowDialog() == true)
            {
                HistoryReocrds.Clear();
            }
        }

        private void AddToList(String psnPath, String localPath, Boolean isDownloading)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                if (_list.Count >= 50)
                {
                    for (Int32 i = 0; i < 20; i++)
                        _list.RemoveAt(0);
                }
                _list.Add(new ObjectRequest(psnPath, localPath, isDownloading, DateTime.Now));
                _view.LvScrollToBottom();
            });
        }

        public async void AddDownload(String psnPath)
        {
            await Aria2Manager.GetInstance().AddDownload(psnPath);
        }

        public async void CopyPsnPath(String psnPath)
        {
            if (new DialogMessage(_view.TryFindResource("StrCopyRelated") as String, String.Empty, DialogMessage.Buttons.YesNo) { Owner = _view }.ShowDialog() == true)
            {
                psnPath = await NetworkHelper.GetFellowUrls(psnPath, _view.SetProgressStart, _view.SetProgress, _view.SetProgressFinish);
            }
            Clipboard.SetDataObject(psnPath);
            new DialogMessage(_view.TryFindResource("StrCopyFinished") as String) { Owner = _view }.Show();
        }

        public void SelectFile(ObjectRequest objectRequest)
        {
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    String filePath = openFileDialog.FileName;
                    String fileName = Path.GetFileName(filePath);
                    String fileParent = Path.GetDirectoryName(filePath);
                    String pattern = Regex.Replace(fileName, "_[0-1]?[0-9].pkg", "_[0-1]?[0-9].pkg");

                    if (!objectRequest.PsnName.ToUpper().Equals(fileName.ToUpper()))
                    {
                        if (new DialogMessage(_view.TryFindResource("StrOpenInconsistent") as String, String.Empty, DialogMessage.Buttons.YesNo) { Owner = _view }.ShowDialog() == true)
                        {
                            HistoryReocrds.Add(objectRequest.PsnPath, filePath);
                            objectRequest.LocalPath = filePath;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else if (Regex.IsMatch(filePath, "_[0-1]?[0-9].pkg", RegexOptions.IgnoreCase) && new DialogMessage(_view.TryFindResource("StrOpenRelated") as String, String.Empty, DialogMessage.Buttons.YesNo) { Owner = _view }.ShowDialog() == true)
                    {
                        Dictionary<String, String> matches = Directory.GetFiles(fileParent)
                            .Where(path => Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase))
                            .ToDictionary(localFilePath => objectRequest.PsnPath.Replace(fileName, Path.GetFileName(localFilePath)), localFilePath => localFilePath);
                        HistoryReocrds.AddAll(matches);
                    }
                    else
                    {
                        HistoryReocrds.Add(objectRequest.PsnPath, filePath);
                    }
                    objectRequest.LocalPath = filePath;
                }
                openFileDialog.Dispose();
            }
        }
    }
}
