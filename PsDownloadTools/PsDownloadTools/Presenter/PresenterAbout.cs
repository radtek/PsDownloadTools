using System;
using System.Reflection;

namespace PsDownloadTools.Presenter
{
    class PresenterAbout
    {
        private readonly ViewAbout _view;

        public PresenterAbout(ViewAbout view)
        {
            _view = view;
        }

        public void Init()
        {
            _view.SetTbName(Assembly.GetExecutingAssembly().GetName().Name.ToString());
            _view.SetTbVersion(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            _view.SetTbDeveloper("Developed by VOID STUDIO");
            _view.SetTbUpdate(GetUpdate());
        }

        private String GetUpdate()
        {
            String text = _view.TryFindResource("StrUpdates") as String;
            text = text.Replace("        ", "");
            text = text.Substring(1);
            return text;
        }

        public String GetDisclaimer()
        {
            String text = _view.TryFindResource("StrDisclaimer") as String;
            text = text.Replace("        ", "");
            text = text.Substring(1);
            return text;
        }
    }
}
