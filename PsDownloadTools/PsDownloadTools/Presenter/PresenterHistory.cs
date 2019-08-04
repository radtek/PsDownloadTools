using PsDownloadTools.Helper;
using PsDownloadTools.View;
using System;
using System.Linq;

namespace PsDownloadTools.Presenter
{
    class PresenterHistory
    {
        private readonly ViewHistory _view;

        public PresenterHistory(ViewHistory view)
        {
            _view = view;
            Init();
        }

        private void Init()
        {
            _view.LvSetItemSource(HistoryReocrds.GetMatches().Select(match => new { Name = new Uri(match.Key).Segments.Last(), Local = match.Value, Psn = match.Key }));
        }
    }
}
