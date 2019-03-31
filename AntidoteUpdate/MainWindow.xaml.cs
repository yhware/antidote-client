using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceProcess;
using System.Timers;
using System.Windows;
using Microsoft.Win32;
using RestSharp;
using SharpRaven;
using SharpRaven.Data;

namespace AntidoteUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static RestClient restClient;
        private static string API_HOST;
        private dynamic SAVE_PATH;
        private string installer_hash_from_server;
        private string new_version_code;
        private RegistryKey AntidoteKey;
        private BackgroundWorker worker;
        private bool workerBusy;
        private int lastDownloadProgress = 0;
        private RavenClient ravenClient;

        public MainWindow()
        {
            ravenClient = new RavenClient("");
            var devMode = Environment.GetEnvironmentVariable("ANTIDOTE_DEV_MODE");
            if(devMode == "YES")
            {
                API_HOST = "http://localhost:5000";
            }
            else
            {
                API_HOST = "https://api.antidote-mgr.com";
            }
            restClient = new RestClient(API_HOST);
            InitializeComponent();
            workerBusy = false;
            worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            Timer timer = new Timer(60000);
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!workerBusy)
            {
                Console.WriteLine("Worker is not busy hence running oper!");
                worker.RunWorkerAsync();
            }
            else
            {
                Console.WriteLine("Worker is BUSY hence NOT running oper!");
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //whatever You want the background thread to do...
            workerBusy = true;
            CheckForUpdate();
        }

        public void SetTaskManager(bool enable)
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (enable && objRegistryKey.GetValue("DisableTaskMgr") != null)
                objRegistryKey.DeleteValue("DisableTaskMgr");
            else
                objRegistryKey.SetValue("DisableTaskMgr", "1");
            objRegistryKey.Close();
        }

        public void CheckForUpdate()
        {
            Console.WriteLine("Checking for newer version...");
            AntidoteKey = Registry.LocalMachine.CreateSubKey(@"Software\Antidote");
            string Version = AntidoteKey.GetValue("Version").ToString();
            RestRequest req = new RestRequest("update", Method.GET);
            req.AddParameter("client_version", Version, ParameterType.QueryString);
            restClient.ExecuteAsync(req, res =>
            {
                Console.WriteLine("API RES RECEIVED!");
                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    dynamic resDic = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(res.Content);
                    String status = resDic.Status;
                    if ("success".Equals(status))
                    {
                        string ActCode = resDic.ActCode;

                        if ("UPDATE_APP".Equals(ActCode))
                        {
                            showProgress("업데이트 시작");
                            this.Dispatcher.Invoke(() =>
                            {
                                this.WindowState = WindowState.Maximized;
                                this.Topmost = true;
                                this.ShowInTaskbar = true;
                                this.Visibility = Visibility.Visible;
                                DownloadProgressBar.IsIndeterminate = false;
                                ProgressTxtBox.Clear();
                                this.Show();
                            });
                            SetTaskManager(false);

                            string InstallerLocation = resDic.Data.InstallerLocation;
                            installer_hash_from_server = resDic.Data.InstallerHash;
                            new_version_code = resDic.Data.VersionCode;
                            Console.WriteLine("Downloading installer... URL IS: " + InstallerLocation);
                            SAVE_PATH = System.IO.Path.Combine(Environment.GetFolderPath(
                                    Environment.SpecialFolder.ApplicationData), "Antidote", "AntidoteInstaller.exe");

                            showProgress("설치파일 다운로드 시작");
                            using (WebClient wc = new WebClient())
                            {
                                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                                wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                                wc.DownloadFileAsync(new Uri(InstallerLocation), SAVE_PATH);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Update not needed");
                            workerBusy = false;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed to ask server for update");
                }

            });

        }

        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                // In case you don't have a progressBar Log the value instead 
                DownloadProgressBar.Value = e.ProgressPercentage;
                if (e.ProgressPercentage != lastDownloadProgress && e.ProgressPercentage % 10 == 0)
                    showProgress("다운로드... " + e.ProgressPercentage + "%");
            });
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                DownloadProgressBar.Value = 0;
                DownloadProgressBar.IsIndeterminate = true;
            });
            if (e.Cancelled)
            {
                this.Dispatcher.Invoke(() =>
                {
                    new MsgWindow("Download Cancelled").ShowDialog();
                });
                return;
            }

            if (e.Error != null) // We have an error! Retry a few times, then abort.
            {
                this.Dispatcher.Invoke(() =>
                {
                    new MsgWindow("An error ocurred while trying to download file").ShowDialog();
                });
                Console.WriteLine("ERROR!!:   " + e.Error.ToString());
                return;
            }

            showProgress("다운로드 완료");
            string installer_hash = hash_installer();
            showProgress("설치 파일 해시 확인 중...");
            Console.WriteLine("Downloaded Installer Hash: " + installer_hash);
            Console.WriteLine("Server Installer Hash: " + installer_hash_from_server);
            if (installer_hash.Equals(installer_hash_from_server))
            {
                showProgress("설치 파일 해시 인증 완료");
                Console.WriteLine("Hash matches");
                showProgress("관리자 프로그램 닫는중...");
                if (AskAntidoteToClose())
                {
                    System.Threading.Thread.Sleep(2000);
                    Process process = new Process();
                    // TODO: Scan for any apps that are using the files to be patched like Antidote.exe
                    process.StartInfo.FileName = SAVE_PATH;
                    process.StartInfo.Arguments = "/S";
                    process.Start();
                    showProgress("패치 진행중...");
                    process.WaitForExit();
                    showProgress("패치 완료");
                    clean_up();

                    System.Threading.Thread.Sleep(1000);
                    // TODO: Restart Antidote
                    Process antidote = new Process();
                    string AntidoteLocation = GetAntidoteLocation("Antidote.exe");
                    antidote.StartInfo.FileName = AntidoteLocation;

                    Process antidoteShield = new Process();
                    antidoteShield.StartInfo.FileName = GetAntidoteLocation("windote.exe");


                    try
                    {
                        antidote.Start();
                        antidoteShield.Start();
                    }
                    catch (InvalidOperationException ioe)
                    {
                        ravenClient.Capture(new SentryEvent(ioe));
                    }
                    catch (Win32Exception we)
                    {
                        ravenClient.Capture(new SentryEvent(we));
                    }
                }
                else
                {
                    showProgress("관리자 프로그램 닫기 실패... 업데이트 실패");
                }
            }
            else
            {
                showProgress("설치 파일 해시 인증 실패... 업데이트 실패");
            }
            workerBusy = false;
        }

        private string GetAntidoteLocation(string processName)
        {
            RegistryKey AntidoteKey = Registry.LocalMachine.CreateSubKey(
                @"Software\Antidote");
            var InstallDir = AntidoteKey.GetValue("INSTALL_DIR");
            AntidoteKey.Close();
            if (InstallDir != null)
            {
                var AntidoteExe = System.IO.Path.Combine(InstallDir.ToString(), processName);
                return AntidoteExe;
            }
            else
            {
                return "";
            }
        }

        private string hash_installer()
        {
            FileStream filestream;
            SHA256 mySHA256 = SHA256Managed.Create();

            filestream = new FileStream(SAVE_PATH, FileMode.Open);

            filestream.Position = 0;

            byte[] hashValue = mySHA256.ComputeHash(filestream);

            string HashStr = BitConverter.ToString(hashValue).Replace("-", String.Empty);

            filestream.Close();
            return HashStr;
        }


        private void clean_up()
        {
            SetTaskManager(true);
            this.Dispatcher.Invoke(() =>
            {
                this.Hide();
                this.WindowState = WindowState.Minimized;
                this.Topmost = false;
                this.ShowInTaskbar = false;
                this.Visibility = Visibility.Hidden;
            });
        }

        private bool AskAntidoteToClose()
        {
            NamedPipeClientStream pipeClient2 =
                    new NamedPipeClientStream(".", "AntidoteShieldPipe",
                        PipeDirection.InOut, PipeOptions.None,
                        TokenImpersonationLevel.Impersonation);

            Console.WriteLine("Connecting to server...\n");
            try
            {
                pipeClient2.Connect(10000);
                Console.WriteLine("Connected!! to pipe\n");
                StreamString ss = new StreamString(pipeClient2);
                ss.WriteString("STOP_REVIVING_ANTIDOTE");
                pipeClient2.Close();
            }
            catch (System.TimeoutException toe)
            {
                Console.WriteLine("Pipe connect timeout!");
                ravenClient.Capture(new SentryEvent(toe));
                return false;
            }


            NamedPipeClientStream pipeClient =
                    new NamedPipeClientStream(".", "AntidotePipe",
                        PipeDirection.InOut, PipeOptions.None,
                        TokenImpersonationLevel.Impersonation);

            Console.WriteLine("Connecting to server...\n");
            try
            {
                pipeClient.Connect(10000);
                Console.WriteLine("Connected!! to pipe\n");
                StreamString ss = new StreamString(pipeClient);
                ss.WriteString("SHUTDOWN_ANTIDOTE_CLIENT_FOR_UPDATE");
                pipeClient.Close();
                return true;
            }
            catch (System.TimeoutException toe)
            {
                Console.WriteLine("Pipe connect timeout!");
                ravenClient.Capture(new SentryEvent(toe));
                return false;
            }


        }

        private void showProgress(string progressMsg)
        {
            this.Dispatcher.Invoke(() =>
            {
                ProgressTxtBox.AppendText(progressMsg);
                ProgressTxtBox.AppendText(Environment.NewLine);
                ProgressTxtBox.ScrollToEnd();
            });
        }
    }
}
