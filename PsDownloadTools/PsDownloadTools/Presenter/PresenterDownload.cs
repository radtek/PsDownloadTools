using PsDownloadTools.Bean;
using PsDownloadTools.Helper;
using PsDownloadTools.View;
using System;

namespace PsDownloadTools.Presenter
{
    class PresenterDownload
    {
        private readonly ViewDownload _view;

        public PresenterDownload(ViewDownload view)
        {
            _view = view;
            Init();
        }

        private void Init()
        {
            _view.LvSetItemSource(Aria2Manager.GetInstance().GetDownloadItems());
            Aria2Manager.GetInstance().StartRefreshing(s =>
            {
                if (String.IsNullOrEmpty(s))
                {
                    Aria2Manager.GetInstance().StopRefreshing();
                }
                else
                {
                    _view.SetTbDwonloadInfo(s);
                }
            });
        }

        public void OnClosing()
        {
            Aria2Manager.GetInstance().ClearProgressHandlers();
            Aria2Manager.GetInstance().StopRefreshing();
        }

        public async void StartAll()
        {
            await Aria2Manager.GetInstance().StartAll();
        }

        public async void PauseAll()
        {
            await Aria2Manager.GetInstance().PauseAll();
        }

        public async void CancelAll()
        {
            try
            {
                await Aria2Manager.GetInstance().CancelAll(b =>
                {
                    if (b)
                    {
                        foreach (DownloadItem downloadItem in Aria2Manager.GetInstance().GetDownloadItems())
                        {
                            FileHelper.DeleteFile(downloadItem.GetLocalPath);
                            FileHelper.DeleteFile(downloadItem.GetLocalPath + ".aria2");
                        }

                        SetBadge(0);
                    }
                    else
                    {
                        ExceptionHelper.ShowErrorMsg(_view.FindResource("StrDeleteFail") as String);
                    }
                });
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("Presenter.CancelSingle", e);
            }
        }

        public async void StartPauseSingle(DownloadItem downloadItem)
        {
            if (downloadItem.IsStop)
            {
                await Aria2Manager.GetInstance().StartSingle(downloadItem.GetGid);
            }
            else
            {
                await Aria2Manager.GetInstance().PauseSingle(downloadItem.GetGid);
            }
        }

        public async void CancelSingle(DownloadItem downloadItem)
        {
            try
            {
                await Aria2Manager.GetInstance().CancelSingle(downloadItem.GetGid, newDownloadItem =>
                {
                    if (newDownloadItem != null)
                    {
                        FileHelper.DeleteFile(newDownloadItem.GetLocalPath);
                        FileHelper.DeleteFile(newDownloadItem.GetLocalPath + ".aria2");

                        SetBadge(Aria2Manager.GetInstance().GetDownloadItems().Count);
                    }
                    else
                    {
                        ExceptionHelper.ShowErrorMsg(_view.FindResource("StrDeleteFail") as String);
                    }
                });
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("Presenter.CancelSingle", e);
            }
        }

        private void SetBadge(Int32 count)
        {
            (_view.Owner as ViewMain).SetBadge(count);
        }
    }
}
