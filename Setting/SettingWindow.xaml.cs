using sound_test.dataStroage;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace sound_test.Setting
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : System.Windows.Window
    {
        SettingWindow()
        {
            InitializeComponent();
        }
        private static SettingWindow _instance;

        public static void ShowInstance()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new SettingWindow();
                _instance.Closed += (s, e) => _instance = null; // 关闭时置空
                _instance.Show();
            }
            else
            {
                _instance.Activate(); // 激活已有窗口
            }
        }
    }
}
