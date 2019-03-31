using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpRaven;

namespace Antidote
{
    public static class AppState
    {
        public static bool SESSION_ACTIVE = false;
        public static bool HANDSHAKE_COMPLETE = false;
        public static string ComputerCode = String.Empty;
        public static bool INIT_SETUP_COMPLETE = false;
        public static MainWindow openWindow { get; set; }
        public static LoginWindow loginWindow { get; set; }
        public static bool isLoginWindowOpen { get; set; }
        public static string jwt = String.Empty;
        public static RavenClient ravenClient { get; set; }
        public static bool DEV_MODE = false;
        public static bool SHUTDOWN_FOR_UPDATE_MODE = false;
        public static string CURRENT_USER = "NONE";
    }
}
