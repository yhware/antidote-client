using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using System.Timers;
using Microsoft.Win32;
using SharpRaven;
using SharpRaven.Data;

namespace Antidote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private BackgroundWorker worker;

        private const int MINIMUM_SPLASH_TIME = 1500; // Miliseconds  

        protected override void OnStartup(StartupEventArgs e)
        {
            Common.SetTaskManager(false);

            var ravenClient = new RavenClient("");
            AppState.ravenClient = ravenClient;

            var devMode = Environment.GetEnvironmentVariable("ANTIDOTE_DEV_MODE");
            AppState.DEV_MODE = (devMode == "YES");


            SplashScreen splash = new SplashScreen();
            splash.Show();
            // Step 2 - Start a stop watch  
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Step 3 - Load your windows but don't show it yet  
            base.OnStartup(e);
            ReadRegistry();
            if (AppState.INIT_SETUP_COMPLETE)
            {
                LoginWindow main = new LoginWindow();
                main.Show();
                AppState.isLoginWindowOpen = true;
                WSClient ws = WSClient.Instance;
                AppState.loginWindow = main;

            }
            else
            {
                InitSetup initSetup = new InitSetup();
                initSetup.Show();
            }
            timer.Stop();

            int remainingTimeToShowSplash = MINIMUM_SPLASH_TIME - (int)timer.ElapsedMilliseconds;
            if (remainingTimeToShowSplash > 0)
                Thread.Sleep(remainingTimeToShowSplash);

            splash.Close();

            worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            System.Timers.Timer workerTimer = new System.Timers.Timer(5000);
            workerTimer.Elapsed += timer_Elapsed;
            workerTimer.Start();

            Process.Start(Path.Combine(Environment.GetEnvironmentVariable("windir"), "explorer.exe"));

        }

        void ReadRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"Software\Antidote"))
                {
                    if (key != null)
                    {
                        var ComputerCodeRaw = key.GetValue("COMPUTER_CODE");
                        if (ComputerCodeRaw != null)
                        {
                            string ComputerCode = ComputerCodeRaw.ToString();
                            ComputerCode = ComputerCode.Substring(1, ComputerCode.Length - 2);
                            AppState.ComputerCode = ComputerCode;
                        }
                        var InitSetupCompleteRaw = key.GetValue("INITIAL_SETUP_COMPLETE");
                        if (InitSetupCompleteRaw != null)
                        {
                            string InitSetupCompleteStr = InitSetupCompleteRaw.ToString();
                            bool InitSetupComplete = (InitSetupCompleteStr == "YES") ? true : false;
                            AppState.INIT_SETUP_COMPLETE = InitSetupComplete;
                        }
                        var InstallLocationRaw = key.GetValue("INSTALL_DIR");
                        if (InstallLocationRaw != null)
                        {
                            string InstallLocationStr = InstallLocationRaw.ToString();
                            Directory.SetCurrentDirectory(InstallLocationStr);
                        }
                        else
                        {
                            var install_dir = Path.Combine(Environment.SystemDirectory, "antidote");
                            Directory.SetCurrentDirectory(install_dir);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppState.ravenClient.Capture(new SentryEvent(ex));
            }
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Initiating pipe server");
            NamedPipeServerStream pipeServer =
                new NamedPipeServerStream("AntidotePipe", PipeDirection.InOut, 1);

            int threadId = Thread.CurrentThread.ManagedThreadId;

            // Wait for a client to connect
            Console.WriteLine("waiting for pipe client...");
            pipeServer.WaitForConnection();

            Console.WriteLine("Client connected on thread[{0}].", threadId);
            try
            {
                // Read the request from the client. Once the client has
                // written to the pipe its security token will be available.

                StreamString ss = new StreamString(pipeServer);
                string msg = ss.ReadString();
                if (msg == Constants.SHUTDOWN_ANTIDOTE_CLIENT_FOR_UPDATE)
                {
                    AppState.SHUTDOWN_FOR_UPDATE_MODE = true;
                    // Shutdown the entire program
                    // TODO: Add method to unload SSDT Driver
                    // 
                    this.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }
                
            }
            // Catch the IOException that is raised if the pipe is broken
            // or disconnected.
            catch (IOException exception)
            {
                Console.WriteLine("ERROR: {0}", exception.Message);
                AppState.ravenClient.Capture(new SentryEvent(exception));
            }
            pipeServer.Close();
        }




    }
}
