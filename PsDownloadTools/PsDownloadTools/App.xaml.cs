using PsDownloadTools.Helper;
using PsDownloadTools.Model;
using PsDownloadTools.View;
using System;
using System.Threading;
using System.Windows;

namespace PsDownloadTools
{
    public partial class App : Application
    {
        public static Mutex _mutex;

        private void Application_Startup(Object sender, StartupEventArgs e)
        {
            DllHelper.RegistDLL();

            ResourceDictionary resource = new ResourceDictionary { Source = new Uri(@"Resource\Languages\langs.xaml", UriKind.Relative) };
            if (resource.Contains(SettingsHelper.Lang))
            {
                String path = resource[resource[SettingsHelper.Lang].ToString()].ToString();
                resource = new ResourceDictionary { Source = new Uri(path, UriKind.Relative) };
            }
            Application.Current.Resources.MergedDictionaries[0] = resource;

            if (e.Args.Length == 1 && e.Args[0] == "-c")
            {
                CompanionManager.StartCompanionThread();
            }
            else
            {
                _mutex = new Mutex(true, "PsDownloadTools", out Boolean ret);
                if (!ret)
                {
                    new DialogMessage(TryFindResource("StrProgramRunning") as String).ShowDialog();
                    Environment.Exit(0);
                }
                else
                {
                    ExceptionHelper.InitExceptionHelper(this);
                    CompanionManager.CallCompanionThread();
                    Current.StartupUri = new Uri("View/ViewMain.xaml", UriKind.Relative);
                }
            }
        }
    }
}
