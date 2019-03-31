using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using RestSharp;

namespace Antidote
{
    /// <summary>
    /// Interaction logic for InitSetup.xaml
    /// </summary>
    public partial class InitSetup : Window
    {
        private InitSetupAdminLoginPage loginPage;
        private InitSetupComputerRegPage computerRegPage;
        public InitSetup()
        {
            InitializeComponent();
            loginPage = new InitSetupAdminLoginPage(this);
            MainFrame.Content = loginPage;
        }

        public void login(string Username, string Password)
        {
            var req = ApiController.getAdminLoginReq(Username, Password);
            this.Dispatcher.Invoke(() =>
            {
                loginPage.setButton(false);
            });

                ApiController.client.ExecuteAsync(req, res =>
            {
                try
                {
                    dynamic resDic = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(res.Content);
                    string status = resDic.Status;

                    this.Dispatcher.Invoke(() =>
                    {
                        loginPage.setButton(true);
                    });
                    if ("success" == status)
                    {
                        AppState.jwt = (string) resDic.Data.jwt;

                        this.Dispatcher.Invoke(() =>
                        {
                            InitSetupComputerRegPage initSetupComputerRegPage = new InitSetupComputerRegPage(this);
                            computerRegPage = initSetupComputerRegPage;
                            MainFrame.Content = initSetupComputerRegPage;
                        });
                    }
                    else
                    {
                        string ErrMsg = resDic.ErrMsg;
                        this.Dispatcher.Invoke(() =>
                        {
                            loginPage.setErrMsg(ErrMsg);
                        });
                    }
                }
                catch (Exception)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        loginPage.setErrMsg("서버와의 통신에 문제가 발생하였습니다");
                    });

                }
            });
                Console.WriteLine("login!!");
        }

        public void regComputer(string ComputerName, string ComputerLocation)
        {

            var req = ApiController.getComputerRegReq(AppState.ComputerCode, ComputerName, ComputerLocation);

            ApiController.client.ExecuteAsync(req, res =>
            {
                try
                {
                    dynamic resDic = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(res.Content);
                    string status = resDic.Status;
                    if ("success" == status)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("성공적으로 컴퓨터 등록절차를 마쳤습니다. 프로그램을 재시작 합니다..");
                            SetInitSetupCompleteReg();
                            this.Close();
                            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        });
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            string ErrMsg = resDic.ErrMsg;
                            computerRegPage.setErrMsg(ErrMsg);
                        });
                    }
                }
                catch (Exception)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        loginPage.setErrMsg("서버와의 통신에 문제가 발생하였습니다");
                    });
                }
            });
        }


        private void SetInitSetupCompleteReg()
        {

            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"Software\Antidote"))
                {
                    if (key != null)
                    {
                        key.SetValue("INITIAL_SETUP_COMPLETE", "YES");
                    }
                }
            }
            catch (Exception ex)
            {
                computerRegPage.setErrMsg("설정에 실패했습니다. 프로그램이 관리자 모드로 실행되었는지 확인해주세요");
            }
        }
    }
}
