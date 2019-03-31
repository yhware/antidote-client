using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Antidote.models;
using Newtonsoft.Json;
using WebSocketSharp;

namespace Antidote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static SessionModel sessionInfo;
        public Boolean allowWindowClose = false; // Window 를 닫게 해줄것인지 결정
        private WebSocket ws = WSClient.Instance.GetWebSocket();

        public MainWindow(SessionModel sessionInfo)
        {
            InitializeComponent();
            this.Closing += new System.ComponentModel.CancelEventHandler(OnClosing);
            sessionInfo = SessionModel.Instance;
            DataContext = sessionInfo;
            this.Left = System.Windows.SystemParameters.WorkArea.Width - this.Width -100;
            this.Top = System.Windows.SystemParameters.WorkArea.Height - this.Height- 50;
        }

        private void Click_Btn(object sender, RoutedEventArgs e)
        {
            LoginWindow lw = new LoginWindow();
            lw.Show();
        }


        void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 특정한 경우에만 해당 Window 를 닫게 해준다
            if (!allowWindowClose)
            {
                e.Cancel = true;
            }
        }

        private void OnPwResetBtnClick(object sender, RoutedEventArgs e)
        {
            sessionInfo = SessionModel.Instance;
            PwResetWindow pwResetWindow = new PwResetWindow(sessionInfo.Username);
            pwResetWindow.Show();
        }


        private void OnSessionEndBtnClick(object sender, RoutedEventArgs e)
        {
            Dictionary<String, String> msg = new Dictionary<string, string>();
            SessionModel sessionModel = SessionModel.Instance;
            msg.Add("MessageCode", Constants.MSG_TERMINATE_SESSION);
            msg.Add("SessionCode", sessionModel.SessionCode);
            string serialized_msg = JsonConvert.SerializeObject(msg);
            ws.Send(serialized_msg);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
