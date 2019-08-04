using System;
using System.Windows;
using System.Windows.Input;

namespace PsDownloadTools.View
{
    public partial class DialogMessage : Window
    {
        public enum Buttons { Ok, YesNo };

        public DialogMessage(String msg, String title = "", Buttons buttons = Buttons.Ok)
        {
            InitializeComponent();
            Init(title, msg, buttons);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            InvalidateMeasure();
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

        private void Init(String title, String msg, Buttons buttons = Buttons.Ok)
        {
            Lb_Title.Content = title;
            Tb_Msg.Text = msg;

            switch (buttons)
            {
                case Buttons.Ok:
                    Btn_Yes.Visibility = Visibility.Collapsed;
                    Btn_No.Visibility = Visibility.Collapsed;
                    break;
                case Buttons.YesNo:
                    Btn_Ok.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Btn_Close_Click(Object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Btn_Ok_Click(Object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Btn_Yes_Click(Object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Btn_No_Click(Object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
