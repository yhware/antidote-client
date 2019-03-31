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

namespace AntidoteUpdate
{
    /// <summary>
    /// Interaction logic for MsgWindow.xaml
    /// </summary>
    public partial class MsgWindow : Window
    {
        public MsgWindow()
        {
            InitializeComponent();
        }

        public MsgWindow(string msg)
        {
            InitializeComponent();
            MsgTxt.Text = msg;
        }
    }
}
