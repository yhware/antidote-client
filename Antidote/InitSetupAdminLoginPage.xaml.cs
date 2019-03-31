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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Antidote
{
    /// <summary>
    /// Interaction logic for InitSetupAdminLoginPage.xaml
    /// </summary>
    public partial class InitSetupAdminLoginPage : Page
    {
        private InitSetup parentWindow;
        public InitSetupAdminLoginPage()
        {
            InitializeComponent();
        }

        public InitSetupAdminLoginPage(InitSetup parentWindow)
        {
            this.parentWindow = parentWindow;
            InitializeComponent();
        }

        private void OnLoginBtnClick(object sender, RoutedEventArgs e)
        {
            parentWindow.login(Username.Text, Password.Password);

        }

        public void setErrMsg(string ErrMsg)
        {

            this.Dispatcher.Invoke(() =>
            {
                this.ErrMsg.Text = ErrMsg;
            });
        }

        public void setButton(bool isEnabled)
        {
            LoginBtn.IsEnabled = isEnabled;
        }
    }
}
