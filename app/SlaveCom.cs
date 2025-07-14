using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Interop;

namespace sound_test.app
{
    class SlaveCom
    {
        //大部分被动调用 留一个用来突发error传递
        tcpSever client;
        bool IsClientConnect;
        public Action<bool> ConnectedEvent;
        public SlaveCom()
        {

        }

        public bool Init(string Ip, string port)
        {
            if (client != null)
            {
                client.Dispose();
            }
            client = new tcpSever();
            client.ConnectEvent += (e) =>
            {
                ConnectedEvent?.Invoke(e);
                if (e == false)
                {
                    Debug.WriteLine($"tcp {Ip} 断开");
                }
                IsClientConnect = e;
            };
            var clienttask = client.Start(Ip, port);
            Task.WaitAll(clienttask);

            if (clienttask.Result == false)
            {
                return false;
            }
            else
            {
                //连接成功
                Debug.WriteLine($"已经接收 {client.GetMessage()}");
                return true;
            }
        }


        void GetName()
        {

        }

        public struct BlueInfoTyped
        {
            public int isConnect;
            public int Battery;
        };

        public struct TestResult
        {
            public string filename;
            public bool isack;
            public double sec;
            public string isError;
            public BlueInfoTyped batinfo;
        };

        public bool isconnect()
        {
            return IsClientConnect;
        }
        //含阻塞
        public async Task<TestResult> TestCmd(string fileName, int Volume)
        {

            var result = new TestResult() { filename = fileName };

            var task = await Getblueinfo();

            if (task.isConnect == 0)
            {
                result.isError = "耳机未连接";

                return result;
            }
            result.batinfo = task;
            var msg = await client.SendAndRead($"play,{fileName},{Volume};");
            if (msg == null)
            {
                result.isError = "通信故障";
                ConnectedEvent?.Invoke(false);
                return result;      //tcp 断开 
            }
            bool iscontain = msg.Contains("play");
            iscontain = msg.Contains("success");
            if (iscontain == false)
            {
                //ConnectedEvent?.Invoke(false);
                result.isError = "播放故障";
                return result;     //接收数据有误
            }

            Debug.WriteLine("play success");
            //for(int i=0;i<5)
            string RamResult;
            while (true)
            {
                RamResult = await client.GetMessage(1000);
                if (RamResult == null)  //连接断开或者MP3播放失败
                {
                    ConnectedEvent?.Invoke(false);
                    result.isError = "通信故障";
                    return result;
                }
                if (RamResult.Length > 0)
                {
                    var ramresultsplit = RamResult.Split(new char[] { ';', ',' });
                    Debug.WriteLine($"{fileName}:接收到的结果{RamResult}");
                    if (ramresultsplit.Length > 4)
                    {
                        if (ramresultsplit[0] == "result")
                        {
                            result.filename = ramresultsplit[1];
                            result.isack = (Convert.ToInt32(ramresultsplit[2]) != 0) ? true : false;
                            result.sec = Convert.ToDouble(ramresultsplit[3]);
                            result.isError = "";
                        }
                    }
                    return result;
                }
            }
            return result;
        }
        public async Task<BlueInfoTyped> Getblueinfo()
        {
            var RamMsg = await client.SendAndRead($"req,devinfo;");
            if (RamMsg == null)
            {
                ConnectedEvent?.Invoke(false);
                return new BlueInfoTyped();      //tcp 断开 
            }
            var MsgLine = RamMsg.Split(";");
            if (MsgLine.Length > 0)
            {
                var detail = MsgLine[0].Split(",");
                //check head of msg
                if (detail.Length >= 5)
                {
                    if (detail[0] == "info" && detail[1] == "dev" && detail[2] == "blue")
                    {
                        BlueInfoTyped blueInfoTyped = new BlueInfoTyped();
                        blueInfoTyped.isConnect = int.Parse(detail[3]);
                        blueInfoTyped.Battery = (blueInfoTyped.isConnect == 0) ? 0 : int.Parse(detail[4]);

                        return blueInfoTyped;
                    }

                }
            }
            return new BlueInfoTyped() { isConnect = 1, Battery = 50 };
        }

        public async void SetLed()
        {
            var RamMsg = await client.SendAndRead($"req,led;");
            if (RamMsg == null)
            {
                ConnectedEvent?.Invoke(false);
                return;     //tcp 断开 
            }
            var MsgLine = RamMsg.Split(";");
            if (MsgLine.Length > 0)
            {

            }
        }

        public async Task<String> GetDevSn()
        {
            var RamMsg = await client.SendAndRead($"req,devSN;");
            if (RamMsg == null)
            {
                ConnectedEvent?.Invoke(false);
                return "unknown";      //tcp 断开 
            }
            var MsgLine = RamMsg.Split(";");
            if (MsgLine.Length > 0)
            {
                var detail = MsgLine[0].Split(",");
                //check head of msg
                if (detail.Length == 4)
                {
                    if (detail[0] == "info" && detail[1] == "dev" && detail[2] == "SN")
                    {
                        detail[3] = detail[3].Replace('\n', ' ');
                        return detail[3];
                    }

                }
            }
            return "unknown";
        }

        public async Task<bool> ConnectDetect()
        {
            string RamMsg = "";
            try
            {
                if (client != null)
                    RamMsg = await client.SendAndRead($"req,devSN;");
            }
            catch
            {
                ConnectedEvent?.Invoke(false);
                return false;
            }
            if (RamMsg == null)
            {
                ConnectedEvent?.Invoke(false);
                return false;      //tcp 断开 
            }
            return true;
        }



        public async Task<List<string>> GetPlayList()
        {
            var RamMsg = await client.SendAndRead($"req,musiclist;");
            if (RamMsg == null)
                return new List<string>();      //tcp 断开 
            var MsgLine = RamMsg.Split(";");
            if (MsgLine.Length > 0)
            {
                var detail = MsgLine[0].Split(",");
                //check head of msg
                if (detail.Length > 2)
                {
                    if (detail[0] == "req" && detail[1] == "musiclist")
                    {

                        var MusicList = detail.ToList();
                        MusicList.RemoveRange(0, 2);
                        return MusicList;
                    }

                }
            }
            return null;
        }

    }
}
