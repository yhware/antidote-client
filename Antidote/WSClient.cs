using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Antidote.models;
using SharpRaven.Data;
using WebSocketSharp;

namespace Antidote
{
    public sealed class WSClient
    {

        private static WSClient instance = null;
        private static readonly object padlock = new object();
        private static WebSocket ws;
        private static string WS_HOST = AppState.DEV_MODE ?
            Settings.Default.DEV_WS_HOST : Settings.Default.PRD_WS_HOST;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ExitWindowsEx(uint uFlags, uint dwReason);


        WSClient()
        {
            //WS_HOST = Settings.Default.PRD_WS_HOST;
            ws = new WebSocket(WS_HOST);
            try
            {
                ws.Connect();
            }
            catch (Exception e)
            {
                AppState.ravenClient.Capture(new SentryEvent(e));
            }

            ws.OnMessage += (sender, e) =>
            {
                // TODO: UNREGISTERED_SESSION 이 들어오면 IDENTIFY SOCKET 메세지를 보내기
                SessionModel sessionModel = SessionModel.Instance;
                Console.WriteLine("Laputa says: " + e.Data);
                dynamic resDic = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(e.Data);
                String messageCode = resDic.MessageCode;
                String sessionCode = resDic.SessionCode;
                switch (messageCode)
                {
                    case "UPDATE_SESSION":
                        double rem_time = resDic.Data.RemTime;
                        double used_time = resDic.Data.UsedTime;
                        if ((sessionModel.SessionCode.Equals(sessionCode)))
                        {
                            Console.WriteLine("UpdateSession");
                            Console.WriteLine("RemTime " + rem_time);
                            Console.WriteLine("UsedTime" + used_time);
                            sessionModel.RemTime = TimeSpan.FromSeconds(rem_time);
                            sessionModel.UsedTime = TimeSpan.FromSeconds(used_time);
                        }
                        break;
                    case "TERMINATE_SESSION":
                        // 세션을 즉시 종료, 주로 시간이 만료되었을때 실행된다
                        ws.Close();
                        break;
                    default:
                        Console.WriteLine("Unidentified message code");
                        break;
                }
            };

            ws.OnClose += (sender, e) =>
            {
                TerminateSession();
            };

            ws.OnError += (sender, e) => {
                Console.WriteLine("WebSocket error!!");
                SentryEvent wsFailEvent = new SentryEvent("WebSocket Failure");
                wsFailEvent.Message = e.ToString();

                AppState.ravenClient.Capture(wsFailEvent);
            };
        }

        private void TerminateSession()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Terminating session...");
                if (!AppState.isLoginWindowOpen)
                {
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    AppState.isLoginWindowOpen = true;
                    AppState.loginWindow = loginWindow;

                }
                if(AppState.openWindow != null) { 
                    AppState.openWindow.allowWindowClose = true;
                    AppState.openWindow.Close();
                }
                AppState.SESSION_ACTIVE = false;
                AppState.HANDSHAKE_COMPLETE = false;
                AppState.ComputerCode = String.Empty;
                AppState.jwt = String.Empty;

            });
            if (!AppState.DEV_MODE && !AppState.SHUTDOWN_FOR_UPDATE_MODE)
            {
                WindowsLogOff();
            }
            if (ws.IsAlive)
            {
                ws.Close();
            }
        }
        

        public WebSocket GetWebSocket()
        {
            return ws;
        }

        public static WSClient Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new WSClient();
                    }
                    return instance;
                }
            }
        }

        public static bool WindowsLogOff()
        {
            return ExitWindowsEx(0 | 0x00000004, 0);
        }
    }
}
