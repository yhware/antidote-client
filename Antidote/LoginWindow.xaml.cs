using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Antidote.models;
using Newtonsoft.Json.Converters;
using System.Windows.Threading;
using WebSocketSharp;
using Microsoft.CSharp.RuntimeBinder;
using SharpRaven.Data;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO.Pipes;
using System.Security.Principal;
using System.Reflection;

namespace Antidote
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private Boolean allowWindowClose = false; // Window 를 닫게 해줄것인지 결정
        private SessionModel sessionModel = SessionModel.Instance;
        private WebSocket ws = WSClient.Instance.GetWebSocket();
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            int scanCode;
            public int flags;
            int time;
            int dwExtraInfo;
        }

        private delegate int LowLevelKeyboardProcDelegate(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(int hHook, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(IntPtr path);

        private IntPtr hHook;
        LowLevelKeyboardProcDelegate hookProc; // prevent gc
        const int WH_KEYBOARD_LL = 13;

        public LoginWindow()
        {
            InitializeComponent();
            this.Closing += new System.ComponentModel.CancelEventHandler(OnClosing);
            IntPtr hModule = GetModuleHandle(IntPtr.Zero);
            hookProc = new LowLevelKeyboardProcDelegate(LowLevelKeyboardProc);
            hHook = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, hModule, 0);
            if (hHook == IntPtr.Zero)
            {
                Console.WriteLine("Failed to set hook, error = " + Marshal.GetLastWin32Error());
            }
            if (AppState.DEV_MODE)
            {
                UnhookWindowsHookEx(hHook);
            }

            var thisWindow = this;
            // Adapt to display resolution change
            SystemEvents.DisplaySettingsChanged += new EventHandler((sender, eve) => {
                thisWindow.WindowState = WindowState.Normal;
                thisWindow.WindowState = WindowState.Maximized;
                thisWindow.Topmost = false;
                thisWindow.Topmost = true;

            });

            // UnhookWindowsHookEx(hHook); // Disable alt-tab hook DELETE THIS AT PRODUCTION!!!!!
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(UpdateSession);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionNumber.Text = version.ToString();

        }

        private void UpdateSession(object sender, EventArgs e)
        {
            if (AppState.SESSION_ACTIVE)
            {
                SessionModel sessionModel = SessionModel.Instance;
                Dictionary<String, String> reqSessionUpdateMsg = new Dictionary<string, string>();
                reqSessionUpdateMsg.Add("SenderType", "Client");
                reqSessionUpdateMsg.Add("MessageCode", Constants.MSG_UPDATE_SESSION);
                reqSessionUpdateMsg.Add("SessionCode", sessionModel.SessionCode);
                
                ws.Send(JsonConvert.SerializeObject(reqSessionUpdateMsg));
            }
        }

        private static int LowLevelKeyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0)
                switch (wParam)
                {
                    case 256: // WM_KEYDOWN
                    case 257: // WM_KEYUP
                    case 260: // WM_SYSKEYDOWN
                    case 261: // M_SYSKEYUP
                        if (
                            (lParam.vkCode == 0x09 && lParam.flags == 32) || // Alt+Tab
                            (lParam.vkCode == 0x1b && lParam.flags == 32) || // Alt+Esc
                            (lParam.vkCode == 0x73 && lParam.flags == 32) || // Alt+F4
                            (lParam.vkCode == 0x1b && lParam.flags == 0) || // Ctrl+Esc
                            (lParam.vkCode == 0x5b && lParam.flags == 1) || // Left Windows Key 
                            (lParam.vkCode == 0x5c && lParam.flags == 1))    // Right Windows Key 
                        {
                            return 1; //Do not handle key events
                        }
                        break;
                }
            return CallNextHookEx(0, nCode, wParam, ref lParam);
        }

        private void Close_Window(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }
            
        private void CloseWindow()
        {
            allowWindowClose = true;
            UnhookWindowsHookEx(hHook);
            this.Close();
        }


        private void OnLogin(object sender, RoutedEventArgs e)
        {
            String Username = LoginUsernameTextBox.Text;
            String Password = LoginPasswordTextBox.Password;
            Console.WriteLine("EXECUTING LOGIN with username " + Username + " Password " + Password);
            LoginBtn.IsEnabled = false;
            var req = ApiController.getLoginReq(Username, Password);

            // TODO : Delete this code after building admin login + PKI verification
            if (Username=="kill_process" && Password == "02356 01357")
            {
                Application.Current.Shutdown();
                Common.SetTaskManager(true);

                NamedPipeClientStream pipeClient =
                        new NamedPipeClientStream(".", "AntidoteShieldPipe",
                            PipeDirection.InOut, PipeOptions.None,
                            TokenImpersonationLevel.Impersonation);

                Console.WriteLine("Connecting to server...\n");
                try
                {
                    pipeClient.Connect(10000);
                    Console.WriteLine("Connected!! to pipe\n");
                    StreamString ss = new StreamString(pipeClient);
                    ss.WriteString("STOP_REVIVING_ANTIDOTE");
                    pipeClient.Close();
                }
                catch (System.TimeoutException te)
                {
                    te.Data.Add("Username", Username);
                    te.Data.Add("Cause", "Timed out while asking AntidoteShield to close beacuse user entered kill_process directive");
                    AppState.ravenClient.Capture(new SentryEvent(te));
                    Console.WriteLine("Pipe connect timeout!");
                }
            }

            ApiController.client.ExecuteAsync(req, res =>
            {
                try
                {
                    Console.WriteLine("Response received!");
                    dynamic resDic = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(res.Content);
                    
                    String status = resDic.Status;
                    if (status == "success")
                    {
                        UnhookWindowsHookEx(hHook);
                        sessionModel.Initialize(resDic);
                        Dictionary<String, String> msg = new Dictionary<string, string>();
                        msg.Add("MessageCode", Constants.MSG_IDENTIFY_SOCKET);
                        string sessionCode = resDic.Data.SessionCode;
                        Console.WriteLine("Received login res from API, SessionCode is " + sessionCode);
                        msg.Add("SessionCode", sessionCode);
                        string serialized_msg = JsonConvert.SerializeObject(msg);
                        Console.WriteLine("Registering WS Connection with msg: " + serialized_msg);
                        if (!ws.IsAlive)
                        {
                            ws.Connect();
                        }
                        ws.Send(serialized_msg);
                        AppState.SESSION_ACTIVE = true;
                        Common.SetTaskManager(true);
                        
                        this.Dispatcher.Invoke(() =>
                        {
                            MainWindow mainWindow = new MainWindow(sessionModel);
                            AppState.openWindow = mainWindow;
                            CloseWindow();
                            mainWindow.Show();
                        });
                    }
                    else
                    {
                        String ErrMsg = resDic.ErrMsg;
                        String ActionCode = String.Empty;
                        try
                        {
                            ActionCode = resDic.ActionCode;
                        }
                        catch (RuntimeBinderException)
                        {

                        }

                        if ((Constants.REQ_PW_RESET).Equals(ActionCode))
                        {
                            // 비밀번호 리셋이 요구되는 경우
                            this.Dispatcher.Invoke(() =>
                            {
                                LoginBtn.IsEnabled = true;
                                MessageBox.Show(ErrMsg);
                                PwResetWindow pwResetWindow = new PwResetWindow(Username);
                                pwResetWindow.Owner = this;
                                pwResetWindow.ShowDialog();
                            });
                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                LoginBtn.IsEnabled = true;
                                MessageBox.Show(ErrMsg);
                                LoginPasswordTextBox.Clear();
                            });
                        }

                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    exception.Data.Add("Username", Username);
                    AppState.ravenClient.Capture(new SentryEvent(exception));
                    this.Dispatcher.Invoke(() =>
                    {
                        LoginBtn.IsEnabled = true;
                        MessageBox.Show("로그인 서버에 문제가 발생해 로그인에 실패하였습니다.");
                        LoginPasswordTextBox.Clear();
                    });
                }
            });
        }

        void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 특정한 경우에만 해당 Window 를 닫게 해준다
            if (!allowWindowClose)
            {
                e.Cancel = true;
            }
            AppState.isLoginWindowOpen = false;

        }

        private void OnReloadBtnClick(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            CloseWindow();
        }

        private void OnAdminBtnClick(object sender, RoutedEventArgs e)
        {

        }

        private void LoginUsernameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnLogin(this, new RoutedEventArgs());
            }
        }
    }
}
