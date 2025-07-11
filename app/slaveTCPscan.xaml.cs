using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;
using sound_test.app;
//using Timer

namespace sound_test.app
{
    /// <summary>
    /// slaveTCPscan.xaml 的交互逻辑
    /// </summary>
    public partial class slaveTCPscan : Window
    {
        string localIP;
        int timerTick;
        List<string> Devlist;
        public slaveTCPscan(string _localIP)
        {
            InitializeComponent();
            this.localIP = _localIP;
        }
        private async void StartTask(object sender, RoutedEventArgs e)
        {
            // 显示弹出窗口
            ProgressPopup.IsOpen = true;

            DispatcherTimer timer = new DispatcherTimer();
            timerTick = 0;
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += Timer_Tick;  // 绑定定时器事件
            timer.Start();

            Thread thread = new Thread(() =>
            {
                networkScan NewScan = new networkScan();
                var res = NewScan.Scan(localIP, 1234);
                Task.WaitAll(res);
                GetNewlist(res.Result);
                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = 100;
                
                    ProgressPopup.IsOpen = false;

                });
            });
            thread.Start();


        }

        private void GetNewlist(List<string> e)
        {
            Devlist = e;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timerTick <= 90)
            {
                timerTick += 5;
                Debug.WriteLine(timerTick);
                progressBar.Value = timerTick;
            }
            else
            {
                var timer = sender as DispatcherTimer;
                timer.Stop();
                
                Debug.WriteLine("timerStop");
            }
        }
    }
}
