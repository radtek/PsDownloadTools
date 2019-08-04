using PsDownloadTools.Presenter;
using PsDownloadTools.View;
using System;
using System.Windows;
using System.Windows.Input;

namespace PsDownloadTools
{
    public partial class ViewAbout : Window
    {
        private readonly PresenterAbout _presenter;

        public ViewAbout()
        {
            InitializeComponent();
            _presenter = new PresenterAbout(this);
            _presenter.Init();
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

        public void SetTbName(String name)
        {
            Tb_Name.Text = name;
        }

        public void SetTbVersion(String version)
        {
            Tb_Version.Text = version;
        }

        public void SetTbDeveloper(String developer)
        {
            Tb_Developer.Text = developer;
        }

        public void SetTbUpdate(String update)
        {
            Tb_Update.Text = update;
        }

        private void Button_Disclaimer_Click(Object sender, RoutedEventArgs e)
        {
            new DialogMessage(_presenter.GetDisclaimer()) { Owner = this }.Show();
        }
    }
}
