using System.Windows;
using System.Windows.Controls;

namespace Antidote
{
    /// <summary>
    /// Interaction logic for InitSetupComputerRegPage.xaml
    /// </summary>
    public partial class InitSetupComputerRegPage : Page
    {
        private InitSetup parentWindow;
        public InitSetupComputerRegPage()
        {
            InitializeComponent();
        }

        public InitSetupComputerRegPage(InitSetup initSetup)
        {
            InitializeComponent();
            this.parentWindow = initSetup;
        }

        private void OnComputerRegClick(object sender, RoutedEventArgs e)
        {
            if(ComputerName.Text == "" || ComputerLocation.Text == "")
            {
                ErrMsg.Text = "컴퓨터 이름이나 컴퓨터 위치는 빈값일수 없습니다.";
                return;
            }
            parentWindow.regComputer(ComputerName.Text, ComputerLocation.Text);
        }


        public void setErrMsg(string ErrMsg)
        {

            this.Dispatcher.Invoke(() =>
            {
                this.ErrMsg.Text = ErrMsg;
            });
        }

    }
}
