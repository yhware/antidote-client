using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Antidote.models;
using System.ComponentModel;

namespace Antidote
{
    public class MainWindowViewModel
    {
        private SessionModel sessionModel { get; set; }

        public string Username { get { return sessionModel.Username; }}
        public string RealName { get { return sessionModel.RealName; }}
        public int Gisu { get { return sessionModel.Gisu; }}
        public string Platoon { get { return sessionModel.Platoon; }}
        public string CompanyName { get { return sessionModel.CompanyName; }}
        public string CompanyCode { get { return sessionModel.CompanyCode; }}
        public string ComputerCode { get { return sessionModel.ComputerCode; }}
        public string ComputerName { get { return sessionModel.ComputerName; }}
        public string SessionCode { get { return sessionModel.SessionCode; } }
        public string IpAddr { get { return sessionModel.IpAddr; } }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
