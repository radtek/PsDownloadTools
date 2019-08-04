using PsDownloadTools.View;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PsDownloadTools.Helper
{
    class ExceptionHelper
    {
        public static void InitExceptionHelper(App app)
        {
            //UI线程未捕获异常处理事件
            app.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            //非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private static void App_DispatcherUnhandledException(Object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true;
                MessageBox.Show(Application.Current.TryFindResource("StrUncaughtError") as String + e.Exception.Message);
            }
            catch (Exception ex)
            {
                FileHelper.WriteErrorFile($"{ex.Message}\r\n\r\n{ex.StackTrace}");
                MessageBox.Show(Application.Current.TryFindResource("StrFatalError") as String);
            }
        }

        private static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            String error;
            if (e.ExceptionObject is Exception)
            {
                error = $"{((Exception)e.ExceptionObject).Message}\r\n\r\n{((Exception)e.ExceptionObject).StackTrace}";
            }
            else
            {
                error = e.ExceptionObject.ToString();
            }
            if (!e.IsTerminating)
            {
                MessageBox.Show(Application.Current.TryFindResource("StrUncaughtError") as String + error);
            }
            else
            {
                FileHelper.WriteErrorFile(error);
                MessageBox.Show(Application.Current.TryFindResource("StrFatalError") as String);
            }
        }

        private static void TaskScheduler_UnobservedTaskException(Object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            MessageBox.Show($"{Application.Current.TryFindResource("StrUncaughtError") as String}{e.Exception.Message}\r\n\r\n{e.Exception.StackTrace}");
        }

        public static void ShowErrorMsg(String title, Exception e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                new DialogMessage(
                $"\r\n--------------------------------------\r\n" +
                $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms")}    {title}" +
                $"\r\n--------------------------------------\r\n" +
                $"{e.Message.Substring(0, Math.Min(500, e.Message.Length))}\r\n\r\n{e.StackTrace}" +
                $"{(e.InnerException == null ? String.Empty : $"{e.InnerException.Message.Substring(0, Math.Min(500, e.InnerException.Message.Length))}\r\n\r\n{e.InnerException.StackTrace}")}")
                { Topmost = true }.ShowDialog();
            });
        }

        public static void ShowErrorMsg(String title)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                new DialogMessage(title) { Topmost = true }.ShowDialog();
            });
        }
    }
}
