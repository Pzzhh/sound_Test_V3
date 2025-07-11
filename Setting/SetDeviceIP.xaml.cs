using sound_test.app.cali;
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
using System.Net;

namespace sound_test.Setting
{
    /// <summary>
    /// SetDeviceIP.xaml 的交互逻辑
    /// </summary>
    public partial class SetDeviceIP : Window
    {
        static string default_IP;
        public SetDeviceIP()
        {
            InitializeComponent();
            if (default_IP == null)
            {
                InputTextBox.Text = GlobalSettings.Instance.Settings.ScanIP;
            }else
            {
                InputTextBox.Text = default_IP;
            }

        }

        static bool IsValidIpAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Window_cali.ssh_init_type ssh_Init_Type = new Window_cali.ssh_init_type() { };
            var addr = InputTextBox.Text;
            if (IsValidIpAddress(addr) == false)
            {
                MessageBox.Show("IP 输入错误");
                return;
            }
            default_IP=addr;
            ssh_Init_Type.IP = addr;
            ssh_Init_Type.UserName = "pzh";
            ssh_Init_Type.Password = "1234";
            Window_cali cali = new Window_cali(ssh_Init_Type);
            cali.Show();
            this.Close();
        }
    }
}
