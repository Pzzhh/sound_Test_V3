using sound_test.dataStroage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Net;
using sound_test.app.cali;

namespace sound_test.Setting.USC
{
    /// <summary>
    /// otherSetting.xaml 的交互逻辑
    /// </summary>
    public partial class otherSetting : UserControl,INotifyPropertyChanged
    {
        string[] TM_Combox_item = new string[] { "禁用", "升五降十", "二次逼近" };
        string _ScanIP;
        public string ScanIP
        {
            get { return _ScanIP; }
            set
            {
                if (_ScanIP != value)
                {
                    if (IsValidIpAddress(value))
                    {
                        _ScanIP = value;
                        OnPropertyChanged(nameof(ScanIP));
                    }
                }
            }
        }

        public otherSetting()
        {
            InitializeComponent();
            InitializeTmCombox();
            InitialValue();
            this.DataContext = this;
        }

        void InitialValue()
        {
            ScanIP = GlobalSettings.Instance.Settings.ScanIP;
        }
        void InitializeTmCombox()
        {

            TMComboBox.ItemsSource = TM_Combox_item;
            //var option = MyDatabase.SettingGetSettingCell(nameof(TM_Combox_item));
            int index = 0;
            foreach (var item in TM_Combox_item)
            {
                if (item == TM_Combox_item[index])
                {
                    TMComboBox.SelectedIndex = index;
                    break;
                }
                else
                    index++;
            }
        }

        private void TMComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var option = TMComboBox.SelectedItem as string;
            if (option != null)
            {
               GlobalSettings.Instance.Settings.ScanIP = option;

            }

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

        private void caliDisplay_Click(object sender, RoutedEventArgs e)
        {
            SetDeviceIP setDeviceIP = new SetDeviceIP();
            setDeviceIP.Show();
           
        }

    }
}
