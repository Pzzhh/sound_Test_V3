using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Diagnostics;
using sound_test.app;
using sound_test.dataStroage;
using sound_test.app.OCR;
using System.ComponentModel;
using System.Net;
using sound_test.app.cali;
using sound_test.Setting;
namespace sound_test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int _slaveCount = 0;
        private string _LocalIP;
        public string LocalIP
        {
            get { return _LocalIP; }
            set
            {
                if (_LocalIP != value)
                {
                    if (IsValidIpAddress(value))
                    {
                        _LocalIP = value;
                        OnPropertyChanged(LocalIP);
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();


            //var newshow = new liteTest();
            //newshow.Show();
            //var ocrtest = new ocrTest();
            //ocrtest.Show();
            var settings = GlobalSettings.Instance.Settings;
            LocalIP = settings.ScanIP;
            this.DataContext = this;
        }

        private async void AddSlave_Click(object sender, RoutedEventArgs e)
        {
            networkScan NewScan = new networkScan();
            var res = await NewScan.Scan(LocalIP, 1234);
            if (res.Count == 0)
            {
                MessageBox.Show($"在地址\"{LocalIP}\"中无设备");
                return;
            }
            foreach (var item in res)
            {
                var slaveControl = new UserControl1(item, "1234")
                {

                };


                // 添加到主界面
                SlavePanel.Children.Add(slaveControl);
                _slaveCount++;
            }
            // 创建新的从机控件
        }



        private void OpenExamReport(object sender, RoutedEventArgs e)
        {
            var HistoryVieW = new liteTest();
            HistoryVieW.Show();
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow.ShowInstance();
        }

        static bool IsValidIpAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var result = MessageBox.Show("确认退出？", "退出", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {

            }
            else
            {
                e.Cancel = true;
            }
        }
    }


}