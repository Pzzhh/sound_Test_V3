using NAudio.CoreAudioApi;
using Org.BouncyCastle.Ocsp;
using sound_test.dataStroage;
using sound_test.Setting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using frepListType = sound_test.dataStroage.MyDatabase.FrepVolumeList;

namespace sound_test.app
{
    //need main handle sql lite and share it for is server
    class SlaveHandle
    {
        SlaveCom Com;
        string _TargetIp;
        string _port;

        /*running state*/
        bool isRunningTest;
        bool Reconnectsignal = false;
        bool _isconnect;
        int MaxRandDelay = 2, MinRandDelay = 1;
        float MaxDB = 80, MinDB = 0;

        bool isconnect
        {
            get { return _isconnect; }
            set
            {
                if (_isconnect != value)
                {
                    _isconnect = value;
                    if (_isconnect == true)
                        ConnectChange?.Invoke("connect");
                    else
                        ConnectChange?.Invoke("disconnect");
                }
            }
        }


        /*PlayList*/
        private List<MyDatabase.FrepVolumeList> _PlayList;
        /*parameter*/
        public int setVolume;

        public string[] ReadPlayList
        {
            get => _PlayList.Select(x => x.ToString()).ToArray();
        }

        List<MyDatabase.FrepVolumeList> PlayList
        {
            get => _PlayList;
            set
            {
                if (_PlayList != value)
                {
                    _PlayList = value;
                    //if (_PlayList != null)
                    //    PlayListUpdata?.Invoke(ReadPlayList);
                }
            }
        }


        /*UI string bing*/

        /*ui respone action*/
        //public event Action<string[]> PlayListUpdata;
        public event Action<string> ConnectChange;
        public event Action<string[]> PlayInfoUpdata;
        public event Action<string> TestRunStateChange;
        public event Action<bluetoothinfotype> DeviceBluetoothEvent;
        public event Action<string, string> DeviceInfoEvent;

        /*struct*/
        public struct bluetoothinfotype
        {
            public int battery;
            public int isconnect;
        }

        /*All thread*/
        Thread RunTest_thread;
        /*Thread signal*/
        public bool RunningTestExit;

        public string[] beginSingalTest(int reqvolume, List<MyDatabase.FrepVolumeList> e)
        {
            if (isRunningTest == true)
                return new string[] { "error", "test is running" };
            if (e.Count == 0)
                return new string[] { "error", "empty task list" };
            setVolume = reqvolume;
            PlayList = e;
            RunTest_thread = new Thread(TestHandle_singal);
            RunTest_thread.Start();
            return new string[] { "success" };
        }

        public string[] beginSpecifyTest(int reqvolume, MyDatabase.FrepVolumeList e)
        {
            if (isRunningTest == true)
                return new string[] { "error", "test is running" };
            if (e == null)
                return new string[] { "error", "empty task " };
            setVolume = reqvolume;

            RunTest_thread = new Thread(() => TestHandle_specifyOne(e));
            RunTest_thread.Start();
            return new string[] { "success" };
        }

        public string[] begin_U5D10_Test(int reqvolume, List<MyDatabase.FrepVolumeList> e)
        {
            if (isRunningTest == true)
                return new string[] { "error", "test is running" };
            if (e.Count == 0)
                return new string[] { "error", "empty task list" };
            setVolume = reqvolume;
            PlayList = e;
            RunTest_thread = new Thread(TestHandle_U5D10);
            RunTest_thread.Start();
            return new string[] { "success" };
        }

        public string[] begin_U5D10_Singal_Test(int reqvolume, MyDatabase.FrepVolumeList e)
        {
            if (isRunningTest == true)
                return new string[] { "error", "test is running" };
            if (e == null)
                return new string[] { "error", "empty task list" };
            setVolume = reqvolume;
            RunTest_thread = new Thread(() => TestHandle_U5D10_Singal(e));
            RunTest_thread.Start();
            return new string[] { "success" };
        }

        public void Terminal()
        {
            RunningTestExit = true;
        }


        public async Task<SlaveCom.TestResult> PlayMusic(string fileName, int Volume)
        {

            var task = await Com.TestCmd(fileName, Volume);
            return task;
        }
        public List<T> RandomSortList<T>(List<T> ListT)
        {
            Random random = new Random();
            List<T> newList = new List<T>();
            foreach (T item in ListT)
            {
                newList.Insert(random.Next(newList.Count + 1), item);
            }
            return newList;
        }

        bool RandDelayloop(int id)
        {
            Random rand = new Random();
            var delays = rand.Next(MinRandDelay, MaxRandDelay);
            for (int i = 0; i < delays; i++)
            {
                if (RunningTestExit)
                    return false;
                string[] RunAfterstr = new string[]{
                    "RunAfter",$"{id}",$"{delays-i}"};
                PlayInfoUpdata?.Invoke(RunAfterstr);
                Thread.Sleep(1000);
            }
            return true;
        }



        private bool singalTestloop(MyDatabase.FrepVolumeList p, out bool isAck, bool isMod = false, float db = 0, int count = 0)
        {
            int stdVolume = setVolume;
            bool isSuccess = true;
            string[] resultstr;
            string NewfileName = "";
            //db = (p._dbhl > MaxDB) ? MaxDB : db;
            //db = (db < MinDB) ? MinDB : db;
            float dbreq = p._dbhl;
            if (isMod == true)
                dbreq = db;
            if (dbreq > 70 && p._frep <= 2000)
                dbreq = 70;
            NewfileName = GetNewFileName(p, dbreq);

            int id = p._id;//[I]
            string add_str = (isMod) ? $"{db.ToString("F1")}[{count}]" : "";
            string[] playstateUpdata = new string[]{
                    "begintest",$"{id}",$"{add_str}"};
            PlayInfoUpdata?.Invoke(playstateUpdata);


            var playtask = PlayMusic(NewfileName, stdVolume);

            Task.WaitAll(playtask);
            var result = playtask.Result;
            if (result.isError.Length > 0)
            {
                isSuccess = false;
            }
            //DeviceBluetoothEvent?.Invoke(new bluetoothinfotype()
            //{
            //    battery = batinfo.battery,
            //    isconnect = batinfo.isconnect
            //});
            isAck = result.isack;

            resultstr = new string[]{
                    "result",$"{p._id}",$"{result.isack}",$"{result.sec}",$"{result.isError}"};

            PlayInfoUpdata?.Invoke(resultstr);
            return isSuccess;
        }


        private string GetNewFileName(MyDatabase.FrepVolumeList e, float DB)
        {
            FrequencyConverter frequencyConverter = new FrequencyConverter();

            int f = (int)e._frep;
            int v = (int)frequencyConverter.ConvertToDbSPL(f, DB);
            int t = (int)e._enduring_ms;
            string Result = $"F:{f} V:{v} T:{t} LR:{0}";
            return Result;
        }

        private float GetDBformFileName(string filename)
        {
            string pattern = @"F:(\d+)\s*V:(\d+)\s*T:(\d+)";
            Match match = Regex.Match(filename, pattern);
            float result = 0;
            if (match.Success && match.Groups.Count == 4)
            {
                string f = match.Groups[1].Value;
                string v = match.Groups[2].Value;
                string t = match.Groups[3].Value;

                result = float.Parse(v);
                result = (result < 0) ? 0 : result;
                result = (result > 100) ? 100 : result;
            }
            return result;
        }
        //advance test


        public SlaveHandle(string Ip, string port)
        {
            _TargetIp = Ip;
            _port = port;
            isconnect = false;
            Thread thread = new Thread(run);
            thread.Start();

            var settings = GlobalSettings.Instance.Settings.Examination;
            MinRandDelay = settings.MinDelay;
            MaxRandDelay = settings.MaxDelay;
            MaxDB = settings.MaxDb;
            MinDB = settings.MinDb;

        }


        //List<MyDatabase.FrepVolumeList> GetPlayList()
        //{
        //    var settings = GlobalSettings.Instance.Settings.Examination;
        //    return settings.FrepVolumeLists;
        //}


        public void Reconnect()
        {
            if (isconnect == false)
            {
                Reconnectsignal = true;
            }
        }
        public void PrintStackTrace()
        {
            StackTrace stackTrace = new StackTrace(true);
            string stack = string.Join("\n", stackTrace.GetFrames().Select(f => $"{f.GetMethod().DeclaringType}.{f.GetMethod().Name}"));
            Debug.WriteLine(stack); // 输出到调试窗口
        }

        void ClientConnectEvent(bool connected)
        {
            Debug.WriteLine("[I] ClientConnectEvent connected " + connected.ToString());
            if (connected)
            {
                RunningTestExit = false;
                isconnect = true;
            }
            else
            {
                PrintStackTrace();
                Debug.WriteLine($"{_TargetIp}");
                //throw new ArgumentException("错误抓到！");
                isconnect = false;
                RunningTestExit = true;
            }
        }
        //RunTest thread
        private void TestHandle_singal()
        {
            isRunningTest = true;
            TestRunStateChange?.Invoke("begin test");

            var testList = RandomSortList<MyDatabase.FrepVolumeList>(PlayList);
            string[] playstateUpdata = new string[]{
                    "prepareTest",$"-1"};
            PlayInfoUpdata?.Invoke(playstateUpdata);
            bool isSuccess = true;
            //随机
            foreach (var taskitem in testList)
            {
                if (RunningTestExit)
                    break;
                isSuccess = RandDelayloop(taskitem._id);
                if (isSuccess == false)
                    break;
                isSuccess = singalTestloop(taskitem, out bool isack);
                if (isSuccess == false)
                    break;
            }
            string Back = (isSuccess) ? "success" : "error";
            TestRunStateChange?.Invoke(Back);
            //检测结果
            isRunningTest = false;
        }

        private void TestHandle_specifyOne(object parameter)
        {
            MyDatabase.FrepVolumeList e = parameter as MyDatabase.FrepVolumeList;
            if (e == null)
                return;
            isRunningTest = true;
            TestRunStateChange?.Invoke("begin test");

            string[] playstateUpdata = new string[]{
                    "prepareTest",$"{e._id}"};
            PlayInfoUpdata?.Invoke(playstateUpdata);
            bool isSuccess = true;
            //随机
            isSuccess = RandDelayloop(e._id);
            if (isSuccess == false)
                return; ;
            isSuccess = singalTestloop(e, out bool isack);
            if (isSuccess == false)
                return;
            string Back = (isSuccess) ? "success" : "error";
            TestRunStateChange?.Invoke(Back);
            //检测结果
            isRunningTest = false;
        }

        bool U5D10_Implement_Singal(MyDatabase.FrepVolumeList e, float dbreq, out bool result)
        {
            bool isSuccess = true;
            result = false;
            int repeatCount = 0, MaxrepeatCount = 1, ThCount = 1, correctCount = 0;
            //随机
            for (int i = 0; i < MaxrepeatCount; i++)
            {
                isSuccess = RandDelayloop(e._id);
                if (isSuccess == false)
                    return isSuccess;

                isSuccess = singalTestloop(e, out bool isack, true, dbreq, i);
                if (isSuccess == false)
                    return isSuccess;
                if (isack)
                {
                    correctCount++;
                }
            }
            if (correctCount >= ThCount)
            {
                result = true;
            }
            return isSuccess;
        }

        bool U5D10_DBCALI(bool result, ref float dbreq, ref int db_stage, ref int UPcount)
        {
            float[] db_step = new float[] { 20, 10, 5 };

        begin:
            switch (db_stage)
            {
                case 0:
                case 1:
                    {
                        if (result == false)
                        {
                            dbreq += db_step[0];
                        }
                        else
                        {
                            db_stage++;
                            if (dbreq >= 40)
                            {
                                dbreq -= db_step[0];
                            }
                            else
                            {
                                db_stage = 2;
                                goto begin;
                            }
                        }
                    }
                    break;
                case 2:
                    {
                        if (result == true)
                        {
                            dbreq -= db_step[1];
                        }
                        else
                        {
                            db_stage++;
                            goto begin;
                        }
                    }
                    break;
                case 3:
                    {
                        if (result == true)
                        {
                            if (UPcount >= 2)
                            {
                                return true;
                            }
                            dbreq -= db_step[1];
                            UPcount = 0;
                        }
                        else
                        {
                            dbreq += db_step[2];
                            UPcount++;
                        }
                    }
                    break;
            }
            return false;
        }

        bool U5D10_Implement(MyDatabase.FrepVolumeList e)
        {
            int upcount = 0;
            //List<string> testList = RandomSortList<string>(ReadPlayList.ToList());

            float dbreq = e._dbhl;
            bool isack = false, isSuccess = false;
            int db_stage = 0, UPcount = 0;
            for (int i = 0; i < 10; i++)
            {
                if (RunningTestExit)
                    break;
                isSuccess = U5D10_Implement_Singal(e, dbreq, out isack);
                Debug.WriteLine($"u5d10 {dbreq} {isack}");
                if (isSuccess == false)
                    break;
                var result = U5D10_DBCALI(isack, ref dbreq, ref db_stage, ref UPcount);
                if (result == true)
                {
                    break;      //退出升五降十 得到结果
                }
                if (dbreq < 0 || dbreq > 80)
                {
                    break;      //退出升五降十 出现故障
                }

            }
            //检测结果
            return isSuccess;
        }

        void TestHandle_U5D10_Singal(object parameter)
        {
            isRunningTest = true;
            MyDatabase.FrepVolumeList e = parameter as MyDatabase.FrepVolumeList;
            TestRunStateChange?.Invoke("begin test");
            bool isSuccess = true;
            isSuccess = U5D10_Implement(e);
            string Back = (isSuccess) ? "success" : "error";
            TestRunStateChange?.Invoke(Back);
            //检测结果
            isRunningTest = false;
        }


        private void TestHandle_U5D10()
        {
            isRunningTest = true;
            TestRunStateChange?.Invoke("begin test");

            var testList = RandomSortList<MyDatabase.FrepVolumeList>(PlayList);
            string[] playstateUpdata = new string[]{
                    "prepareTest",$"-1"};
            PlayInfoUpdata?.Invoke(playstateUpdata);
            bool isSuccess = true;

            //随机
            foreach (var item in testList)
            {
                isSuccess = U5D10_Implement(item);
                if (isSuccess == false)
                    break;
            }
            string Back = (isSuccess) ? "success" : "error";
            TestRunStateChange?.Invoke(Back);
            //检测结果
            isRunningTest = false;
        }

        void DeviceStateReadTick()
        {

        }

        async void run()
        {

            //tcp init
            bool Lastconnect = false;
            Com = new SlaveCom();
            Com.ConnectedEvent += ClientConnectEvent;
            Com.Init(_TargetIp, _port);
            //connect and get all info

            var result = await Com.Getblueinfo();
            var SNstring = await Com.GetDevSn();
            DeviceInfoEvent?.Invoke("SN", SNstring);

            DeviceBluetoothEvent?.Invoke(new bluetoothinfotype()
            {
                battery = result.Battery,
                isconnect = result.isConnect
            });
            Lastconnect = isconnect;
            int disconnectDetectCount = 0;
            while (true)
            {
                disconnectDetectCount++;
                if (disconnectDetectCount % 20 == 0)
                {
                    //var GetIsConnect = await Com.ConnectDetect();
                }
                if (disconnectDetectCount % 20 == 0)
                {
                    //var BTresult = await Com.Getblueinfo();

                    //DeviceBluetoothEvent?.Invoke(new bluetoothinfotype()
                    //{
                    //    battery = BTresult.Battery,
                    //    isconnect = BTresult.isConnect
                    //});
                }
                //this loop is to handle all key handle
                //1 get run key
                //   1.1 new thread is running for handle the whole test event;
                //2 check host is running 
                //  if no halt all the thread and back to reconnect
                if (isconnect != Lastconnect && isconnect == false)
                {
                    Debug.WriteLine("[I] run ReReconnectsignal ");
                    string[] RunAfterstr = new string[]{
                        "Disconnect"};
                    PlayInfoUpdata?.Invoke(RunAfterstr);
                    Com.Init(_TargetIp, _port);
                }
                Lastconnect = isconnect;

                if (Reconnectsignal)
                {
                    Reconnectsignal = false;
                    Com = new SlaveCom();
                    Com.ConnectedEvent += ClientConnectEvent;
                    if (Com.Init(_TargetIp, _port) == false)
                    {
                        ConnectChange?.Invoke("ReconnectFail");
                        Debug.WriteLine($"重连接失败{_TargetIp}");
                    }
                }
                Thread.Sleep(50);
            }
            Debug.WriteLine($"服务终止{_TargetIp}");
        }

    }
    public class FrequencyConverter
    {
        private readonly Dictionary<double, double> splData;

        public FrequencyConverter()
        {
            // 初始化ANSI S3.6-89标准数据，频率(Hz) -> dBSPL
            splData = new Dictionary<double, double>
        {
            { 125, 45.5 },
            { 250, 24.5 },
            { 500, 11.0 },
            { 1000, 6.5 },
            { 1500, 6.5 },
            { 2000, 8.5 },
            { 3000, 7.5 },
            { 4000, 9.0 },
            { 6000, 8.0 },
            { 8000, 9.5 }
        };

        }

        public double ConvertToDbSPL(double frequency, double dbHL)
        {
            // 频率范围检查
            if (frequency < 100 || frequency > 8000)
            {
                Debug.WriteLine($"dbspl to DBhl error exceed range");
                return dbHL;
            }

            // 边界处理
            if (frequency <= 125) return dbHL + splData[125];
            if (frequency >= 8000) return dbHL + splData[8000];

            // 找到最近的两个频率点
            var lowerFreq = splData.Keys.Where(f => f <= frequency).Max();
            var higherFreq = splData.Keys.Where(f => f >= frequency).Min();

            // 线性插值计算dBSPL参考值
            double lowerSPL = splData[lowerFreq];
            double higherSPL = splData[higherFreq];
            double interpolatedSPL = lowerSPL;
            if (higherFreq != lowerFreq)
            {
                interpolatedSPL = lowerSPL + (higherSPL - lowerSPL) * (frequency - lowerFreq) / (higherFreq - lowerFreq);
            }

            // 计算dBSPL
            double dbSPL = dbHL + interpolatedSPL;
            return dbHL;
        }
    }

}
