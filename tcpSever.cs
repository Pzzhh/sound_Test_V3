using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace sound_test
{
    public class tcpSever : INotifyPropertyChanged
    {
        private TcpClient client;
        private NetworkStream _networkStream;
        public List<string> _RecvString;


        private bool _isconnect;
         ManualResetEvent signal = new ManualResetEvent(false);
        private object SendLock;

        private bool _isQuit;
        private bool _usereadHandle;

        public event Action<string> DataReceived; // 数据到达事件
        public event Action<bool> ConnectEvent; // 数据到达事件
        public async Task<bool> Start(string ip, string port, bool usereadHandle = false)
        {
            int _port = int.Parse(port);
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ip, _port);
                _networkStream = client.GetStream();
                _usereadHandle = usereadHandle;
                _RecvString = new List<string>();
                //if (usereadHandle)
                _ = HandleClientAsync(_networkStream, client);
            }
            catch (Exception ex)
            {
                _isconnect = false;
                ConnectEvent?.Invoke(_isconnect);
                MessageBox.Show($"Error: {ex.Message},{port}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return _isconnect;
        }

        public void Stop()
        {

        }

        private async Task HandleClientAsync(NetworkStream stream, TcpClient client)
        {
            using (client)
            {
                var buffer = new byte[1024];
                //var stream = client.GetStream();
                _isconnect = true;
                ConnectEvent?.Invoke(_isconnect);
                while (_isconnect)
                {
                    try
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            //lock (_RecvString)
                            //{
                            _RecvString.Add(message);
                            signal.Set();
                            //}
                        }
                        if (bytesRead == 0)
                        {
                            Debug.WriteLine($"Error reading from client: disconnnect");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error reading from client: {ex.Message}");
                        break;
                    }
                }

                ConnectEvent?.Invoke(false);
                _isconnect = false;
            }
        }

        public async Task Send(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _networkStream.WriteAsync(data, 0, data.Length);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<string> SendAndRead(string message)
        {
            if (_usereadHandle)
            {
                throw new InvalidOperationException();
            }
            try
            {
                //lock (SendLock)
                //{
                byte[] data = Encoding.UTF8.GetBytes(message);
                var task = _networkStream.WriteAsync(data, 0, data.Length);
                if (signal.WaitOne(10000) == true)
                {
                   
                    signal.Reset();
                    string rxmessage = _RecvString[0];
                    _RecvString.RemoveAt(0);
                    return rxmessage;
                    //}
                }
               
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            if (_isconnect)
            {
                return null;
            }
            return "";
        }

        public async Task<string> GetMessage(int timeout = 200)
        {
            try
            {
                if (_isconnect == false)
                    return null;
                if (signal.WaitOne(1000) == true)
                {
                    signal.Reset();
                    if (_RecvString.Count != 0)
                    {
                        string rxmessage = _RecvString[0];
                        _RecvString.RemoveAt(0);
                        return rxmessage;
                    }
                    //}
                }

                //var buffer = new byte[1024];
                //var DataRecvTask = _networkStream.ReadAsync(buffer, 0, buffer.Length);
                //var DataRecvTaskRes = await Task.WhenAny(DataRecvTask, Task.Delay(timeout));
                //Task.WaitAll(DataRecvTaskRes);
                //// if (DataRecvTask == DataRecvTaskRes)
                //{
                //    int bytesRead = DataRecvTask.Result;  // 获取读取的字节数
                //    if (bytesRead > 0)
                //    {
                //        string rxmessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                //        return rxmessage;
                //    }
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            return "";
        }

        public void Dispose()
        {
            _networkStream.Dispose();
            client.Dispose();
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
