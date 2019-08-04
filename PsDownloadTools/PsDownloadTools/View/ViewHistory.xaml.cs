using PsDownloadTools.Presenter;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Input;

namespace PsDownloadTools.View
{
    public partial class ViewHistory : Window
    {
        private readonly PresenterHistory _presenter;

        public ViewHistory()
        {
            InitializeComponent();
            _presenter = new PresenterHistory(this);
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

        public void LvSetItemSource(IEnumerable list)
        {
            Lv.ItemsSource = list;
        }
    }
}
