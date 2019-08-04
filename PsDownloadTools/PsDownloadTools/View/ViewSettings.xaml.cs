using PsDownloadTools.Presenter;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Input;

namespace PsDownloadTools.View
{
    public partial class ViewSettings : Window
    {
        private readonly PresenterSettings _presenter;

        public ViewSettings()
        {
            InitializeComponent();
            _presenter = new PresenterSettings(this);
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
            Close();
        }

        public String TbExt
        {
            get { return Tb_Ext.Text; }
            set { Tb_Ext.Text = value; }
        }
        public String CbLang
        {
            get { return Cb_Lang.Text; }
        }

        public String BtnDownloadPath
        {
            get { return Btn_Download_Path.Content.ToString(); }
            set { Btn_Download_Path.Content = value; }
        }

        public void SetCbLangSource(IList list, Int32 index)
        {
            Cb_Lang.ItemsSource = list;
            Cb_Lang.SelectedIndex = index;
        }

        private void Cb_Lang_SelectionChanged(Object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_presenter != null)
            {
                _presenter.ChangeLanguage(e.AddedItems[0].ToString());
            }
        }

        private void Btn_Download_Path_Click(object sender, RoutedEventArgs e)
        {
            _presenter.SelectPath();
        }

        private void Btn_Save_Click(Object sender, RoutedEventArgs e)
        {
            _presenter.Save();
            Close();
        }
    }
}
