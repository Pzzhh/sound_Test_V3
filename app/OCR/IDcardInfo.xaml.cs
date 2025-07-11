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
using static wpfCamTest.IDcardOCR;

namespace sound_test.app.OCR
{
    /// <summary>
    /// IDcardInfo.xaml 的交互逻辑
    /// </summary>
    public partial class IDcardInfo : Window
    {
        public Action<IDinfo> GetIdinfo;
        public IDcardInfo()
        {
            InitializeComponent();
            myusercontrol.ConfirmEvent += usercontrolReqExitWindow;
        }

        private void USC_IDcardOcr_Loaded(object sender, RoutedEventArgs e)
        {

        }

        void usercontrolReqExitWindow(IDinfo dinfo)
        {
            GetIdinfo?.Invoke(dinfo);
            this.Close();
        }
    }
}
