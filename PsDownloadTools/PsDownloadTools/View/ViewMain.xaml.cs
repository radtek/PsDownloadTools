using PsDownloadTools.Bean;
using PsDownloadTools.Presenter;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PsDownloadTools.View
{
    public partial class ViewMain : Window
    {
        private readonly PresenterMain _presenter;

        public ViewMain()
        {
            InitializeComponent();
            _presenter = new PresenterMain(this);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Point position = e.GetPosition(Lb_Title);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (position.X >= 0 && position.X < Lb_Title.ActualWidth && position.Y >= 0 && position.Y < Lb_Title.ActualHeight)
                {
                    DragMove();
                }
            }
        }

        public void SetLbTitleVersion(String version)
        {
            Lb_Title.Content += version;
        }

        private void Btn_About_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.OpenAbout();
        }

        private void Btn_Min_Click(Object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Chk_Max_Restore_Checked(Object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            Grid_Window.Margin = new Thickness(8, 5, 8, 8);
            Grid_Title.Margin = new Thickness(8, 0, -2, 0);
        }

        private void Chk_Max_Restore_Unchecked(Object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            Grid_Window.Margin = new Thickness(0, 0, 0, 0);
            Grid_Title.Margin = new Thickness(8, 0, 8, 0);
        }

        private void Btn_Close_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.OnClosing();
            Close();
        }

        private void Button_Logo_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.OpenPage();
        }

        public void CbIpSetItemSource(IEnumerable list)
        {
            Cb_Ip.ItemsSource = list;
        }

        public void CbIpSetSelection(Int32 index)
        {
            Cb_Ip.SelectedIndex = index;
        }

        public void TbPortSetText(String port)
        {
            Tb_Port.Text = port;
        }

        public void SetState(Boolean isRunning)
        {
            if (isRunning)
            {
                Cb_Ip.IsEnabled = false;
                Tb_Port.IsEnabled = false;
                Chk_Start.Content = TryFindResource("StrStopService") as String;
            }
            else
            {
                Cb_Ip.IsEnabled = true;
                Tb_Port.IsEnabled = true;
                Chk_Start.Content = TryFindResource("StrStartService") as String;
            }
        }

        private void Chk_Start_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.StartServer(Cb_Ip.Text, Tb_Port.Text);
        }

        private void Btn_Settings_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.OpenSettings();
        }

        public void SetControlsEnable()
        {
            Chk_Start.IsEnabled = true;
            Btn_Download_Manager.IsEnabled = true;
        }

        public void SetBadge(Int32 count)
        {
            if ((Lb_Badge.Content == null || Lb_Badge.Content.Equals("0")) && count != 0)
            {
                Storyboard storyboardBadgeShow = FindResource("BadgeShow") as Storyboard;
                storyboardBadgeShow.Begin(Grid_Control);
            }

            if (Lb_Badge.Content != null && !Lb_Badge.Content.Equals("0") && count == 0)
            {
                Storyboard storyboardBadgeHide = FindResource("BadgeHide") as Storyboard;
                storyboardBadgeHide.Begin(Grid_Control);
            }

            Lb_Badge.Content = count.ToString();
        }

        private void Btn_Download_Manager_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.OpenDownloadManager();
        }

        private void Btn_Show_Matches_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.ShowHistory();
        }

        private void Btn_Clear_Matches_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.ClearHistory();
        }

        public void LvSetItemSource(IEnumerable list)
        {
            Lv_Request.ItemsSource = list;
        }

        public void LvScrollToBottom()
        {
            Lv_Request.ScrollIntoView(Lv_Request.Items[Lv_Request.Items.Count - 1]);
        }

        private void Btn_Download_Click(Object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (ListBoxItem)Lv_Request.ContainerFromElement(sender as Button);
            listBoxItem.IsSelected = true;
            ObjectRequest objectRequst = listBoxItem.Content as ObjectRequest;
            _presenter.AddDownload(objectRequst.PsnPath);
        }

        private void Btn_Copy_Click(Object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (ListBoxItem)Lv_Request.ContainerFromElement(sender as Button);
            listBoxItem.IsSelected = true;
            ObjectRequest objectRequst = listBoxItem.Content as ObjectRequest;
            _presenter.CopyPsnPath(objectRequst.PsnPath);
        }

        private void Btn_Open_Click(Object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (ListBoxItem)Lv_Request.ContainerFromElement(sender as Button);
            listBoxItem.IsSelected = true;
            ObjectRequest objectRequst = listBoxItem.Content as ObjectRequest;
            _presenter.SelectFile(objectRequst);
        }

        private void Tb_GotFocus(Object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (ListBoxItem)Lv_Request.ContainerFromElement(sender as TextBox);
            listBoxItem.IsSelected = true;
        }

        public void SetProgressStart()
        {
            Storyboard storyboardDialogProgressShow = FindResource("DialogProgressShow") as Storyboard;
            storyboardDialogProgressShow.Begin(Grid_Window);
        }

        public void SetProgress(String progress)
        {
            Tb_Progress.Text = progress;
        }

        public void SetProgressFinish()
        {
            Storyboard storyboardDialogProgressHide = FindResource("DialogProgressHide") as Storyboard;
            storyboardDialogProgressHide.Begin(Grid_Window);
        }
    }
}
