using System;
using System.Windows;
using RestSharp;

namespace Antidote
{
    /// <summary>
    /// Interaction logic for PwResetWindow.xaml
    /// </summary>
    public partial class PwResetWindow : Window
    {
        public string Username { get; set; }
        public PwResetWindow()
        {
            InitializeComponent();
        }

        public PwResetWindow(String Username)
        {
            this.Username = Username;
            InitializeComponent();
            UsernameTextBox.Text = Username;
        }

        private void OnPwResetBtnClick(object sender, RoutedEventArgs e)
        {
            string OldPw = OldPwBox.Password;
            string NewPw = NewPwBox.Password;
            string NewPwCheck = NewPwCheckBox.Password;

            if (NewPw.Equals(NewPwCheck))
            {
                var req = ApiController.getPwResetReq(Username, OldPw, NewPw);
                ApiController.client.ExecuteAsync(req, res =>
                {
                    Console.WriteLine(res.Content);
                    dynamic resDic = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(res.Content);
                    String status = resDic.Status;
                    if ("success".Equals(status))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("비밀번호가 성공적으로 변경되었습니다.");
                            this.Close();
                        });
                    }
                    else
                    {
                        String ErrMsg = resDic.ErrMsg;
                        this.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(ErrMsg);
                            OldPwBox.Clear();
                            NewPwBox.Clear();
                            NewPwCheckBox.Clear();
                        });
                    }
                });
            }
            else
            {
                MessageBox.Show("새로설정할 비밀번호 확인이 불일치합니다.");
                NewPwBox.Clear();
                NewPwCheckBox.Clear();
            }

        }
    }
}
