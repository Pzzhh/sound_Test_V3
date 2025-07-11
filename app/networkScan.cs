using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace sound_test.app
{
    internal class networkScan
    {
        private static readonly object lockObject = new object();
        public Action<int> TcpScanFinish;
        List<string> HostList = new List<string>();
        bool isfinish = false;
        int Finishcount;
        public const int ScanCount = 255;

        public async Task<List<string>> Scan(string target, int port)
        {
            var num = target.Split(".");
            if (num.Length == 4)
            {
                List<Task> tasks = new List<Task>();  // 用来保存所有任务
                for (int i = 0; i < ScanCount; i++)
                {
                    string Scanaddr = $"{num[0]}.{num[1]}.{num[2]}.{i.ToString()}";
                    tasks.Add(CheckTcpServer(Scanaddr, port));
                }
                await Task.WhenAll(tasks);
            }
            isfinish = true;
            return HostList;
        }


        private async Task CheckTcpServer(string target, int port)
        {
            bool isOpen = await IsPortOpenAsync(target, port);
            if (isOpen)
            {
                lock (lockObject)
                {
                    HostList.Add(target);
                    Debug.WriteLine($"端口 {port} 在主机 {target} 上是开放的!");
                }
            }
            else
            {
               
                //  Debug.WriteLine($"端口 {port} 在主机 {target}!");

            }
           
        }

        // 使用 TcpClient 来异步检查端口是否开放
        private async Task<bool> IsPortOpenAsync(string host, int port)
        {
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    var connectTask = tcpClient.ConnectAsync(host, port);
                    var completedTask = await Task.WhenAny(connectTask, Task.Delay(500));  // 超时 1 秒

                    if (completedTask == connectTask)
                    {
                        bool state = false;
                        byte[] buffer = new byte[50];
                        var _networkStream = tcpClient.GetStream();
                        var DataRecvTask = _networkStream.ReadAsync(buffer, 0, buffer.Length);
                        var DataRecvTaskRes = await Task.WhenAny(DataRecvTask, Task.Delay(200));
                        if (DataRecvTaskRes == DataRecvTask)
                        {
                            // 如果 DataRecvTask 完成，表示成功读取了数据
                            int bytesRead = DataRecvTask.Result;  // 获取读取的字节数
                            if (bytesRead > 0)
                            {
                                Debug.WriteLine($"Connected:{host}, {Encoding.UTF8.GetString(buffer)}");
                                state = true;
                            }
                        }
                        tcpClient.Close();
                        tcpClient.Dispose();
                        return state;
                    }
                    tcpClient.Close();
                    tcpClient.Dispose();
                    return false;  // 如果超时，则认为端口未开放
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
