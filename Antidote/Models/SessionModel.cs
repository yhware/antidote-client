using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Antidote.models
{
    public sealed class SessionModel : INotifyPropertyChanged
    {
        public string Username { get; set; }
        public string RealName { get; set; }
        public int Gisu { get; set; }
        public string Platoon { get; set; }
        public string CompanyName { get; set; }
        public string CompanyCode { get; set; }
        public string ComputerCode { get; set; }
        public string ComputerName { get; set; }
        public string ComputerLocation { get; set; }
        public string SessionCode { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public string IpAddr { get; set; }
        public string Notice { get; set; }

        private TimeSpan remTime;
        private string remTimeStr;
        private TimeSpan usedTime;
        private string usedTimeStr;

        public string RemTimeStr {
            get
            {
                return remTime.ToString(@"hh\:mm");
            }
        } // 남은 시간

        public TimeSpan RemTime
        {
            get
            {
                return remTime;
            }
            set
            {
                if (value != this.remTime)
                {
                    this.remTime = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("RemTimeStr");
                    NotifyPropertyChanged("WindowTitle");
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                return "남은 시간: " + RemTimeStr;
            }
        }

        
        public string UsedTimeStr
        {
            get
            {
                return usedTime.ToString(@"hh\:mm");
            }
        } // 남은 시간

        public TimeSpan UsedTime
        {
            get
            {
                return usedTime;
            }
            set
            {
                if (value != this.usedTime)
                {
                    this.usedTime = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("UsedTimeStr");
                }
            }
        }


        private static SessionModel instance = null;
        private static readonly object padlock = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        SessionModel() { }

        public static SessionModel Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new SessionModel();
                    }
                    return instance;
                }
            }
        }

        public void Initialize(dynamic apiRes)
        {
            dynamic Data = apiRes.Data;
            Username = Data.Username;
            RealName = Data.RealName;
            Gisu = Data.Gisu;
            Platoon = Data.Platoon;
            SessionCode = Data.SessionCode;
            IpAddr = Data.IpAddr;
            CompanyName = Data.CompanyName;
            CompanyCode = Data.CompanyCode;
            ComputerName = Data.ComputerName;
            ComputerLocation = Data.ComputerLocation;
            Notice = Data.Notice;

            //String startDtStr = Data.StartDt;
            //String endDtStr = Data.EndDt;

            //StartDt = DateTime.ParseExact(startDtStr, "yyyy-MM-dd HH:mm:ss",
            //                           System.Globalization.CultureInfo.InvariantCulture);
            //EndDt = DateTime.ParseExact(endDtStr, "yyyy-MM-dd HH:mm:ss",
            //                           System.Globalization.CultureInfo.InvariantCulture);
        }

        public void Clear()
        {
            Username = String.Empty;
            RealName = String.Empty;
            Gisu = -1;
            Platoon = String.Empty;
            SessionCode = String.Empty;
            IpAddr = String.Empty;
            CompanyName = String.Empty;
            CompanyCode = String.Empty;
        }
    }
}
