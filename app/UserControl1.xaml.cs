using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiteDB;
using sound_test.app;
using sound_test.app.OCR;
using sound_test.dataStroage;
using sound_test.Setting;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.LinkLabel;
using static sound_test.dataStroage.MyDatabase;
using static sound_test.UserControl1;
using static wpfCamTest.IDcardOCR;

namespace sound_test
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControl1 : UserControl, INotifyPropertyChanged
    {
        private string _ControlName;
        //tcpSever CtrlClient = new tcpSever();
        SlaveHandle slavehandle;
        string targetIp;
        string targetPort;
        string SN_ID;
        /*ui value*/
        string _PersonIdinfoMsg;
        string _VolumeIndicator;

        //public string PersonIdinfoMsg
        //{
        //    get { return _PersonIdinfoMsg; }
        //    set
        //    {
        //        if (_PersonIdinfoMsg != value)
        //        {
        //            _PersonIdinfoMsg = value;
        //            OnPropertyChanged(nameof(PersonIdinfoMsg));
        //        }
        //    }
        //}
        //string _PersonName;
        public string Gender
        {
            get { return indivdualInfo.gender; }
            set
            {
                if (value != indivdualInfo.gender)
                {
                    indivdualInfo.gender = value;
                    OnPropertyChanged(nameof(Gender));
                }
            }
        }
        public string PersonName
        {
            get { return indivdualInfo.name; }
            set
            {
                if (indivdualInfo.name != value)
                {
                    indivdualInfo.name = value;
                    OnPropertyChanged(nameof(PersonName));
                }
            }
        }
        public string PersonID
        {
            get { return indivdualInfo.IDnumber; }
            set
            {
                if (indivdualInfo.IDnumber != value)
                {
                    indivdualInfo.IDnumber = value;
                    OnPropertyChanged(nameof(PersonID));
                }
            }
        }

        bool _canSaveResult;
        public bool AllowSaveResult
        {
            get { return _canSaveResult; }
            set
            {
                if (_canSaveResult != value)
                {
                    _canSaveResult = value;
                    OnPropertyChanged(nameof(AllowSaveResult));
                }
            }
        }

        bool _CanChangeVolume;
        public bool AllowChangeVolume
        {
            get { return _CanChangeVolume; }
            set
            {
                if (_CanChangeVolume != value)
                {
                    _CanChangeVolume = value;
                    OnPropertyChanged(nameof(AllowChangeVolume));
                    OnPropertyChanged(nameof(AllowStop));

                }
            }
        }

        public bool AllowStop
        {
            get { return !_CanChangeVolume; }
        }

        public string VolumeIndicator
        {
            get { return _VolumeIndicator; }
            set
            {
                if (_VolumeIndicator != value)
                {
                    _VolumeIndicator = value;
                    OnPropertyChanged(nameof(VolumeIndicator));
                }
            }
        }
        int MaxDb = (int)GlobalSettings.Instance.Settings.Examination.MaxDb;
        int MinDb = (int)GlobalSettings.Instance.Settings.Examination.MinDb;
        public int SetDB
        {
            get { return volume; }
            set
            {
                if (volume != value)
                {
                    volume = (value > MaxDb) ? MaxDb : value;
                    volume = (volume < MinDb) ? MinDb : volume;
                    VolumeIndicator = $"{volume}DB";
                    foreach (var item in examDetail)
                    {
                        item.dbhl = volume.ToString();
                    }
                    var view = CollectionViewSource.GetDefaultView(examDetail);
                    view.Refresh();
                    OnPropertyChanged(nameof(volume));
                }
            }
        }
        string _BlueToothMsg;
        public string BlueToothMsg
        {
            get { return _BlueToothMsg; }
            set
            {
                if (_BlueToothMsg != value)
                {
                    _BlueToothMsg = value;
                    OnPropertyChanged(nameof(BlueToothMsg));
                }
            }
        }

        string _delayTick;
        public string delayTick
        {
            get { return _delayTick; }
            set
            {
                if (_delayTick != value)
                {
                    _delayTick = value;
                    OnPropertyChanged(nameof(delayTick));
                }
            }
        }



        public struct TestDataPoint
        {
            public int F { get; set; }
            public int V { get; set; }
            public float T { get; set; }
            public int DisplaySec { get { return (int)T / 1000; } }

            public override string ToString()
            {
                return $"频率:{F} 赫兹 音量:{V} 分贝 时间:{DisplaySec} 秒";
            }
        }

        /*TestParameter*/
        int volume;

        /*persion info*/
        IDinfo indivdualInfo = new IDinfo() { address = "", gender = "", birthday = "", IDnumber = "", name = "", racial = "" };
        public ObservableCollection<TaskItem> examDetail { get; set; }


        public UserControl1(string targetIP, string port)
        {
            InitializeComponent();
            examDetail = new ObservableCollection<TaskItem>();
            TaskListView.ItemsSource = examDetail;
            this.DataContext = this;
            targetIp = targetIP;
            targetPort = port;
            SlaveHandle_init(targetIP, port);       //从机通信初始化
            // 初始化任务列表
            //PersonIdinfoMsg = "未输入";
            SetDB = 40;
            AllowChangeVolume = true;
            AllowSaveResult = false;
            ConnectLabel.Foreground = Brushes.Red;
            System.Timers.Timer timer = new System.Timers.Timer();
            UpdataAllFrepList();
            gen_Combox.Items.Add("男");
            gen_Combox.Items.Add("女");
            gen_Combox.SelectedIndex = 0;
#if false
            timer.Elapsed += Button_ClickLoop;
            timer.Interval = 1000;
            timer.Start();
#endif
        }



        void UpdataAllFrepList()
        {
            var freqlist = GlobalSettings.Instance.Settings.Examination.FrepVolumeLists;
            examDetail.Clear();
            foreach (var item in freqlist)
            {
                TaskItem taskitem = new TaskItem();
                taskitem.frepList = item;
                taskitem.Time = " ";
                taskitem.StatusBrush = Brushes.Gray;
                examDetail.Add(taskitem);
            }
        }

        void SlaveHandle_init(string targetIP, string port)
        {
            if (slavehandle != null)
            {
                //slavehandle.
            }
            slavehandle = new SlaveHandle(targetIP, port);      //建立
            //slavehandle.PlayListUpdata += PlayListUpdataEvent;
            slavehandle.PlayInfoUpdata += PlayInfoUpdataEvent;
            slavehandle.ConnectChange += ConnectChangeEvent;
            slavehandle.TestRunStateChange += TestRunStateChangeEvent;
            slavehandle.DeviceBluetoothEvent += (e) =>
            {
                string backstr = (e.isconnect == 1) ? $"设备电量{e.battery}%" : "耳机未连接";
                BlueToothMsg = backstr;
            };
            slavehandle.DeviceInfoEvent += DeviceInfoEvent;
        }

        void DeviceInfoEvent(string title, string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (title)
                {
                    case "SN":
                        SN_ID = message;
                        LabelControlName.Content = $"序列号：{SN_ID}  IP：{targetIp}";
                        break;
                }
            });
        }
        int count = 0, next_Delay = 5, total_Elaspcd, iserror = 0;
        void Button_ClickLoop(object sender, ElapsedEventArgs e)
        {
            //this.Dispatcher.Invoke(() =>
            //{
            //    TimeSpan time = TimeSpan.FromSeconds(total_Elaspcd);
            //    var elasp = string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
            //    delayTick = $"{next_Delay - count} {elasp}";
            //    string tag = ConnectLabel.Content.ToString();
            //    if (tag?.Contains("已连接") == true)
            //    {
            //        total_Elaspcd++;
            //    }
            //});
            //if (count > next_Delay)
            //{
            //    if (AllowChangeVolume)
            //    {
            //        Random rand = new Random();
            //        next_Delay = rand.Next(5, 30);
            //        slavehandle.beginSingalTest(SetVolume);
            //    }
            //    count = 0;
            //}
            //else
            //    count++;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<MyDatabase.FrepVolumeList> fl = new List<FrepVolumeList>();
            foreach (var item in examDetail)
            {
                fl.Add(item.frepList);
            }
            int setvolume = GlobalSettings.Instance.Settings.Examination.DefaultVolumeLevel;
            slavehandle.beginSingalTest(setvolume, fl);
        }

        private void Button_U5D10_Click(object sender, RoutedEventArgs e)
        {
            List<MyDatabase.FrepVolumeList> fl = new List<FrepVolumeList>();
            foreach (var item in examDetail)
            {
                fl.Add(item.frepList);
            }
            int setvolume = GlobalSettings.Instance.Settings.Examination.DefaultVolumeLevel;
            slavehandle.begin_U5D10_Test(setvolume, fl);
        }

        private void Button_STOP_Click(object sender, RoutedEventArgs e)
        {
            slavehandle.Terminal();
        }

        void ConnectChangeEvent(string msg)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (msg)
                {
                    case "connect":
                        ConnectLabel.Content = "已连接"; ConnectLabel.Foreground = Brushes.Green;
                        break;
                    case "disconnect":
                        ConnectLabel.Content = "连接断开"; ConnectLabel.Foreground = Brushes.Red;
                        break;
                    case "ReconnectFail":
                        ConnectLabel.Content = "重连接失败"; ConnectLabel.Foreground = Brushes.Red;
                        break;
                }
            });
        }

        void TestRunStateChangeEvent(string msg)
        {
            if (msg == "begin test")
            {
                AllowChangeVolume = false;
                AllowSaveResult = false;
            }
            else if (msg == "error")
            {
                //MessageBox.Show($"测试失败，设备号{}")
                //iserror = 1;
                AllowChangeVolume = true;
                AllowSaveResult = false;

            }
            else if (msg == "success")
            {
                AllowChangeVolume = true;
                AllowSaveResult = true;
            }
        }





        void PlayInfoUpdataEvent(string[] list)
        {
            this.Dispatcher.Invoke(() =>
            {
                int id = 0;
                if (list[0] == "begintest")
                {
                    if (int.TryParse(list[1], out id) == false)
                        throw new ArgumentException("xxxx");

                    string add_str = list[2];
                    var cell = examDetail.FirstOrDefault(t => t.ID == id);
                    if (cell != null)
                    {
                        var index = examDetail.IndexOf(cell);
                        TaskItem Newcell = examDetail[index];
                        Newcell.Status = "测试中...";
                        Newcell.StatusBrush = Brushes.DarkGreen;
                        if (add_str.Length > 0)
                        {
                            //Newcell.dbhl_Adj = add_str;
                            Newcell.LEarStatus = add_str;
                        }
                        else
                        {
                            Newcell.LEarStatus = "";
                            //Newcell.dbhl_Adj = "";
                        }
                        examDetail[index] = Newcell;
                    }
                }
                if (list[0] == "prepareTest" && list.Length == 2)
                {
                    if (int.TryParse(list[1], out id) == false)
                        throw new ArgumentException("xxxx");
                    if (id == -1)
                    {
                        foreach (var cell in examDetail)
                        {
                            cell.Status = "";
                            cell.Clean_dbhl_Adj = "";
                            //cell.dbhl_Adj = "";
                            cell.Time = "";
                        }
                    }
                    else
                    {
                        var cell = examDetail.FirstOrDefault(t => t.ID == id);
                        if (cell != null)
                        {
                            cell.Status = "";
                            cell.Clean_dbhl_Adj = "";
                            //cell.dbhl_Adj = "";
                            cell.Time = "";
                        }
                    }
                }
                if (list[0] == "result")
                {
                    if (int.TryParse(list[1], out id) == false)
                        throw new ArgumentException("xxxx");
                    string isack = list[2];
                    string ackSec = list[3];
                    string iserror = list[4];

                    var cell = examDetail.FirstOrDefault(t => t.ID == id);
                    if (cell != null)
                    {
                        var index = examDetail.IndexOf(cell);
                        TaskItem Newcell = examDetail[index];

                        Newcell.Time = "";
                        if (iserror.Length > 0)
                        {
                            Newcell.Status = iserror;
                            Newcell.StatusBrush = Brushes.OrangeRed;
                        }
                        else if (isack == "False")
                        {
                            Newcell.Status = "未通过";
                            Newcell.StatusBrush = Brushes.Red;
                        }
                        else
                        {
                            Newcell.Status = "通过";
                            Newcell.Time = ackSec;
                            Newcell.StatusBrush = Brushes.Green;
                        }
                        examDetail[index] = Newcell;
                    }
                }
                if (list[0] == "RunAfter")
                {
                    if (int.TryParse(list[1], out id) == false)
                        throw new ArgumentException("xxxx");
                    string Delay = list[2];
                    var cell = examDetail.FirstOrDefault(t => t.ID == id);
                    if (cell != null)
                    {
                        var index = examDetail.IndexOf(cell);
                        TaskItem Newcell = examDetail[index];
                        Newcell.Status = $"{Delay}秒后开始";
                        Newcell.StatusBrush = Brushes.DarkGreen;
                        examDetail[index] = Newcell;

                    }
                }
                if (list[0] == "Disconnect")
                {
                    int count = examDetail.Count;
                    for (int index = 0; index < count; index++)
                    {
                        TaskItem Newcell = examDetail[index];
                        Newcell.Status = "";
                        //结果重载
                        examDetail[index] = Newcell;
                    }
                }
                var view = CollectionViewSource.GetDefaultView(examDetail);
                view.Refresh();
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var IdinfoDialog = new IDcardInfo();
            IDinfo dinfo = new IDinfo();
            IdinfoDialog.GetIdinfo += ((e) =>
            {
                dinfo = e;
                Debug.WriteLine("已经获取");
            });
            IdinfoDialog.ShowDialog();
            if (dinfo.IDnumber != null || dinfo.name != null)
            {
                indivdualInfo = dinfo;
                //PersonIdinfoMsg = $"{dinfo.name},{dinfo.gender},{dinfo.IDnumber}";
                //PersonIdInfo.Text = $"{dinfo.name},{dinfo.gender},{dinfo.IDnumber}";
            }
            else
            {
                indivdualInfo = null;
                //PersonIdinfoMsg = "未输入姓名";
                //PersonIdInfo.Text = "未输入姓名";
            }
        }

        void Button_Click2(object sender, RoutedEventArgs e)
        {
            if (indivdualInfo == null || examDetail.Count == 0 || indivdualInfo.name.Length < 1)
            {
                MessageBox.Show("保存失败", "确认", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var person = new Person()
            {
                birthday = indivdualInfo.birthday,
                gender = indivdualInfo.gender,
                ID = indivdualInfo.IDnumber,
                name = indivdualInfo.name,
            };
            MyDatabase.CheckPersonExist(indivdualInfo.IDnumber, person);

            MyDatabase.TestReport examReport = new MyDatabase.TestReport();

            foreach (var item in examDetail)
            {
                var rp = new singalTest()
                {
                    Freq = item.Freq,
                    dbhl = item.dbhl,
                    dbhl_Adv = item.dbhl_Adj,
                    testMode = "U",
                    Isack = item.Status,
                    RepTime = item.Time
                };
                if (item.dbhl_Adj == "")
                {
                    rp.testMode = "S";
                }
                examReport.ST.Add(rp); ;

            }
            MyDatabase.AddReport(indivdualInfo.IDnumber, examReport);
            MessageBox.Show("保存成功", "确认", MessageBoxButton.OK, MessageBoxImage.Information);
            AllowSaveResult = false;
            return;
        }

        void Button_Click3(object sender, RoutedEventArgs e)
        {
            slavehandle.Reconnect();
        }

        private void MenuItem_S1_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is TaskItem item)
            {
                int setvolume = GlobalSettings.Instance.Settings.Examination.DefaultVolumeLevel;
                slavehandle.beginSpecifyTest(setvolume, item.frepList);
            }
        }

        private void MenuItem_S2_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is TaskItem item)
            {
                int setvolume = GlobalSettings.Instance.Settings.Examination.DefaultVolumeLevel;
                slavehandle.begin_U5D10_Singal_Test(setvolume, item.frepList);
            }
        }

        private void MenuItem_S3_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is TaskItem item)
            {
                item.Status = "";
                //item.dbhl_Adj = "";
                item.Clean_dbhl_Adj = "";
                item.Time = "";
                var view = CollectionViewSource.GetDefaultView(examDetail);
                view.Refresh();
            }
        }

        private void MenuItem_S4_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            UpdataAllFrepList();
        }

        private void MenuItem_S5_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is TaskItem item)
            {
                TaskItem taskitem = new TaskItem();
                taskitem.frepList = item.frepList;
                //examDetail.
                var index = examDetail.IndexOf(item);
                examDetail.Insert(index, taskitem);
            }
        }

        private void MenuItem_S6_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is TaskItem item)
            {
                var index = examDetail.IndexOf(item);
                examDetail.RemoveAt(index);
            }
        }

        private void MenuItem_S7_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in examDetail)
            {
                item.Status = "";
                //item.dbhl_Adj = "";
                item.Clean_dbhl_Adj = "";
                item.Time = "";
            }
            var view = CollectionViewSource.GetDefaultView(examDetail);
            view.Refresh();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 限制只输入数字（来自你之前的问题）
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void PersonID_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string number = DateTime.Now.ToString("yyyyMMddHHmmss");
            PersonID = number;
        }

        public string ControlName
        {
            get => _ControlName;
            set
            {
                if (_ControlName != value)
                {
                    _ControlName = value;
                    LabelControlName.Content = _ControlName;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public class TaskItem
    {
        private MyDatabase.FrepVolumeList FrepVolumePara = new FrepVolumeList() { _frep = 99, _dbhl = 11, _enduring_ms = 22 };
        public MyDatabase.FrepVolumeList frepList
        {
            get => FrepVolumePara;
            set
            {
                FrepVolumePara._frep = value._frep;
                FrepVolumePara._dbhl = value._dbhl;
                FrepVolumePara._enduring_ms = value._enduring_ms;
            }
        }
        //public string Taskdetail { get; set; }
        //public string TaskNameOrginal { get; set; }
        //public string TaskName
        //{
        //    get
        //    {
        //        string add = "";
        //        if (Taskdetail != null)
        //            add = (Taskdetail.Length > 0) ? $"[{Taskdetail}]" : "";
        //        return $"{TaskNameOrginal} {add}";
        //    }
        //}
        float CheckValue(float input, float min, float max)
        {
            float hp = (input >= min) ? input : min;
            float lp = (hp <= max) ? hp : max;
            return lp;
        }
        public string Freq  //[I] 暂时没有限制输入
        {
            get =>
                frepList._frep.ToString();
            set
            {
                float vfrep = 0;
                if (float.TryParse(value, out vfrep) == false)
                    return;
                frepList._frep = CheckValue(vfrep, 100, 8000);
            }
        }
        public string dbhl
        {
            get =>
                frepList._dbhl.ToString();
            set
            {
                float vdbhl = 0;
                if (float.TryParse(value, out vdbhl) == false)
                    return;
                frepList._dbhl = CheckValue(vdbhl, 0, 80);
            }
        }
        public string enduring
        {
            get =>
                frepList._enduring_Sec.ToString();
            set
            {
                float vf = 0;
                float.TryParse(value, out vf);
                frepList._enduring_Sec = vf;
            }
        }
        public string dbhl_Adj
        {
            get
            {
                string str = "";
                if (!string.IsNullOrEmpty(LEarStatus))
                    str += $" {LEarStatus}";
                if (!string.IsNullOrEmpty(REarStatus))
                    str += $" {REarStatus}";
                return str;
            }
        }
        public string Status { get; set; }

        public string Clean_dbhl_Adj
        {
            set
            {
                REarStatus = "";
                LEarStatus = "";
            }
        }

        public string REarStatus { get; set; }
        public string LEarStatus { get; set; }

        public string Time { get; set; }
        public Brush StatusBrush { get; set; }

        public int ID { get => frepList._id; }

        public TaskItem()
        {
            FrepVolumePara._id = GetUUid();
        }

        static int uuid = 0;
        int GetUUid()
        {
            uuid++;
            return uuid;
        }
    }
}


