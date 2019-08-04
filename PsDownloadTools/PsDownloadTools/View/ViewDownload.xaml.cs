using PsDownloadTools.Bean;
using PsDownloadTools.Presenter;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PsDownloadTools.View
{
    public partial class ViewDownload : Window
    {
        private readonly PresenterDownload _presenter;

        public ViewDownload()
        {
            InitializeComponent();
            _presenter = new PresenterDownload(this);
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

        private void Btn_Close_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.OnClosing();
            Close();
        }

        private void Btn_Start_All_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.StartAll();
        }

        private void Btn_Pause_All_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.PauseAll();
        }

        private void Btn_Cancel_All_Click(Object sender, RoutedEventArgs e)
        {
            if (new DialogMessage(TryFindResource("StrConfirmToCancelAll") as String, String.Empty, DialogMessage.Buttons.YesNo) { Owner = this }.ShowDialog() == true)
            {
                _presenter.CancelAll();
            }
        }

        private void Chk_Download_Start_Pause_Click(Object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (ListBoxItem)Lv.ContainerFromElement(sender as CheckBox);
            listBoxItem.IsSelected = true;
            DownloadItem downloadItem = listBoxItem.Content as DownloadItem;
            _presenter.StartPauseSingle(downloadItem);
        }

        private void Btn_Download_Cancel_Click(Object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (ListBoxItem)Lv.ContainerFromElement(sender as Button);
            listBoxItem.IsSelected = true;
            DownloadItem downloadItem = listBoxItem.Content as DownloadItem;

            if (new DialogMessage(TryFindResource("StrConfirmToCancelSingle") as String + $" {downloadItem.GetName}?", String.Empty, DialogMessage.Buttons.YesNo) { Owner = this }.ShowDialog() == true)
            {
                _presenter.CancelSingle(downloadItem);
            }
        }

        public void LvSetItemSource(IEnumerable list)
        {
            Lv.ItemsSource = list;
        }

        public void SetTbDwonloadInfo(String info)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Tb_Download_Info.Text = info;
            });
        }
    }
}
