using NAudio.Wave;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
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
using System.Timers;
using Renci.SshNet;
using System.Diagnostics;
using System.Security.AccessControl;
using static sound_test.app.SlaveCom;
using System.Collections;
using NAudio.CoreAudioApi.Interfaces;
using sound_test.dataStroage;
using sound_test.Setting;
using NAudio.Dsp;

namespace sound_test.app.cali
{
    /// <summary>
    /// Window_cali.xaml 的交互逻辑
    /// </summary>
    public partial class Window_cali : Window
    {
        public struct ssh_init_type
        {
            public string IP;
            public string UserName;
            public string Password;
        }

        public Window_cali(ssh_init_type ssh_Init_Type)
        {
            var g_sshInfo = GlobalSettings.Instance.Settings.Calibration.SshLoginPara;
            ssh_info.UserName = g_sshInfo.UserName;
            ssh_info.Password = g_sshInfo.Password;
            ssh_info.IP = ssh_Init_Type.IP;
            InitializeComponent();
            InitializeValue();
            InitializePlot();
            LoadMicrophoneList();
            InitializeTimer();
            phasex = 0;
            frep_text.Text = "1000";
        }
        private ssh_init_type ssh_info;

        private WaveInEvent waveIn;
        private PlotModel plotModel;
        private LineSeries waveformSeries;
        //private double buffer[]=new double[44100*2*1];
        private List<double> samples = new List<double>();
        private List<double> samples_RMS = new List<double>();
        private const int SampleRate = 44100;
        private const int DisplaySeconds = 1;
        private System.Timers.Timer updateTimer;
        private bool is1Stick = false, isRunScan = false;
        private SshClient _sshClient;
        private int Freq_Req = 500, currentStage = 0;
        /** setting para **/
        private int MinFreq = 500, MaxFreq = 2500, MaxVolume = 32000;
        private float target_DB = 65, allowError = 1;
        private int freq_step = 25;
        string LinuxResultStroagePath, LinuxRunTestPath;
        BandPassFilter filter;
        /***/
        private List<string> ssh_cmdlines = new List<string>();
        private bool ssh_terminal = false;

        private string SSH_Connect_State
        {

            get
            {
                return label_SSH_State.Content.ToString();
            }
            set
            {
                Dispatcher.Invoke(() =>
                {
                    label_SSH_State.Content = $"{ssh_info.IP} " + value;
                });
            }
        }
        public struct db_cali
        {
            public double freq;
            public double result;
            public double Loffset;
            public double Roffset;
        }
        private List<db_cali> test_result;
        private double currentDBSPL;

        private void InitializeTimer()
        {
            updateTimer = new System.Timers.Timer(500); // 100ms 间隔
            updateTimer.Elapsed += UpdateTimer_Elapsed;
            updateTimer.AutoReset = true; // 自动重复触发
            updateTimer?.Start();
            test_result = new List<db_cali>();
            ConnectButton_Click();
        }

        private void InitializeValue()
        {
            var settings = GlobalSettings.Instance.Settings;
            var cali = settings.Calibration;
            MaxFreq = (int)cali.MaxFreq;
            MinFreq = (int)cali.MinFreq;

            target_DB = cali.TargetDb;
            freq_step = (int)cali.FreqStep;
            allowError = cali.AllowError;
            LinuxResultStroagePath = cali.LinuxFileStorage;
            LinuxRunTestPath = cali.LinuxRunTestPath;
            //ssh_info.IP = cali.SshLoginPara.IP;
            //ssh_info.UserName = cali.SshLoginPara.UserName;
            //ssh_info.Password = cali.SshLoginPara.Password;
            Freq_Req = MinFreq;
        }

        private async void ConnectButton_Click()
        {

            var readThread = new System.Threading.Thread(() =>
            {
                RunSshWithCMD(new CancellationToken(), null, (shellStream) => //cmd handle
                {
                    //shellStream.WriteLine("/home/pzh/qtPCMtest/bin/qtPCMtest");  // 示例命令，持续输出日志内容
                    //shellStream.WriteLine("volume 5000");  // 示例命令，持续输出日志内容
                    shellStream.Flush();
                });

            });
            readThread.Start();

            var AnalayThread = new System.Threading.Thread(() =>
            {
                UpdateTimer_Elapsed2();
            });
            AnalayThread.Start();
        }

        // 发送命令
        private async void SendCommandButton_Click(object sender, RoutedEventArgs e)
        {
            //string command = CommandTextBox.Text;

            //if (_sshClient == null || !_sshClient.IsConnected)
            //{
            //    OutputTextBox.Text += "Not connected.\n";
            //    return;
            //}

            //try
            //{
            //    SendCommandButton.IsEnabled = false;
            //    OutputTextBox.Text += $"> {command}\n";

            //    // 异步执行命令
            //    string result = await ExecuteCommandAsync(command);
            //    OutputTextBox.Text += $"{result}\n";
            //}
            //catch (Exception ex)
            //{
            //    OutputTextBox.Text += $"Error: {ex.Message}\n";
            //}
            //finally
            //{
            //    SendCommandButton.IsEnabled = true;
            //}
        }

        // 异步执行命令
        private async Task<string> ExecuteCommandAsync(string command)
        {
            return await Task.Run(() =>
            {
                // 使用 SshClient 的 RunCommand 方法执行命令
                using (var cmd = _sshClient.CreateCommand(command))
                {
                    cmd.Execute();
                    Debug.WriteLine(cmd.Result);
                    return cmd.Result;
                }
            });
        }

        // 清理资源

        private int orgianal_spl = 500;
        private int currentSetsplVol = 10000;
        private bool isRight = false;

        private int wait_step = 4;
        private int count_repeat = 0;
        SampleState sampleState = SampleState.idle;
        enum SampleState
        {
            idle,
            ready,
            Sampleing,
            finish

        };
        public float phasex = 0;
        float _phase_step2;
        float dfreq = 100;
        float phase_step2
        {
            get
            {
                return (float)_phase_step2;
            }
            set
            {
                _phase_step2 = value / 44100.0f * 360.0f;
            }
        }
        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<double> doubles = new List<double>();
            //if (is1Stick)
            //{
            //lock (samples)
            //if (Monitor.TryEnter(samples))
            //float dfreq = 0;
            //float.TryParse(frep_text.Text, out dfreq);
            phase_step2 = dfreq;
            lock (samples)
            {
                var dsp_List = doubles;
                {
                    Dispatcher.Invoke(() =>
                {
                    var filted_data = (samples);

                    double rmsValue = CalculateRMS(filted_data);
                    // 转换为 dB
                    double dbValue = 94 + 20 * Math.Log10(rmsValue / 500);
                    label_db.Content = "DB:" + dbValue.ToString("");

                    waveformSeries.Points.Clear();
                    double timeStep = 0.1 / SampleRate;
                    for (int i = 0; i < dsp_List.Count; i++)
                    {
                        waveformSeries.Points.Add(new DataPoint(i * timeStep, filted_data[i]));
                    }
                    plotModel.InvalidatePlot(true);
                    dsp_List.Clear();
                });
                }

            }
            //    is1Stick = false;
            //}
            //if (isRunScan)
            //{
            //    if (currentStage == 0)
            //    {
            //        double result = Math.Abs(currentDBSPL - target_DB);
            //        if (result < allowError)
            //        {
            //            if (count_repeat >= 3)
            //            {
            //                Debug.WriteLine($"{isRight} {result} {Freq_Req}  {currentDBSPL} {currentSetsplVol}");
            //                RecordResult(isRight);
            //                Freq_Req = Freq_Req + freq_step;
            //                currentDBSPL = 0;
            //                count_repeat = 0;
            //            }
            //            else
            //                count_repeat++;
            //            //currentSetsplVol = orgianal_spl;
            //        }

            //        {
            //            double factor = Math.Abs(currentDBSPL - target_DB);
            //            int add = 0;
            //            if (factor > allowError)
            //                add = Math.Abs((int)((currentSetsplVol / 20) * (target_DB - currentDBSPL)));
            //            else
            //                add = Math.Abs((int)((currentSetsplVol / 35) * (target_DB - currentDBSPL)));
            //            add = (add > 2000) ? 2000 : add;
            //            if (currentDBSPL - target_DB < 0)
            //            {
            //                currentSetsplVol = currentSetsplVol + add;
            //            }
            //            else
            //                currentSetsplVol = currentSetsplVol - add;
            //            if (currentSetsplVol > MaxVolume)
            //            {
            //                Freq_Req = MinFreq;
            //                isRunScan = false;
            //            }
            //            if (currentSetsplVol < 50)
            //                currentSetsplVol = 50;
            //        }
            //        Dispatcher.Invoke(() =>
            //        {
            //            label_freq.Content = $"FREQ:{Freq_Req} {currentSetsplVol}";
            //        });
            //        SSH_SendCmd($"freq {Freq_Req}\n");
            //        SSH_SendCmd($"volume {currentSetsplVol}\n");
            //        //ExecuteCommandAsync($"freq {Freq_Req}");

            //    }
            //    if (Freq_Req > MaxFreq)
            //    {
            //        if (isRight == true)
            //        {
            //            SSH_Terminal();
            //            Result_Save(test_result, true);
            //            Dispatcher.Invoke(() =>
            //            {
            //                freq_result_plot freq_Result_Plot = new freq_result_plot(test_result);
            //                freq_Result_Plot.Show();
            //            });
            //            Freq_Req = MinFreq;
            //            isRight = false;
            //            isRunScan = false;
            //            return;
            //        }
            //        else
            //        {
            //            Freq_Req = MinFreq;
            //            isRight = true;
            //            return;
            //        }
            //    }
            //    if (currentStage == wait_step)
            //    {
            //        is1Stick = true;

            //    }
            //    if (currentStage >= wait_step + 1 && is1Stick == false)
            //    {
            //        currentStage = 0;
            //    }
            //    else
            //        currentStage++;

            //}
            //else
            //{
            //    currentStage++;
            //    if (currentStage > 5)
            //    {
            //        is1Stick = true;
            //        currentStage = 0;
            //    }
            //    Dispatcher.Invoke(() =>
            //    {
            //        label_freq.Content = "FREQ:STOP";
            //    });
            //}
        }

        void RMSCali()
        {
            lock (samples_RMS)
            {
                //var filted_data = filter.Process(samples_RMS);
                var filted_data = samples_RMS;
                double rmsValue = CalculateRMS(filted_data);
                // 转换为 dB
                double dbValue = 94 + 20 * Math.Log10(rmsValue / 500);
                //label_db.Content = "DB:" + dbValue.ToString("");
                //Debug.WriteLine($"{samples.Count}");

                currentDBSPL = dbValue;
                waveformSeries.Points.Clear();
                double timeStep = 0.1 / SampleRate;
                //for (int i = 0; i < samples.Count; i++)
                //{
                //    waveformSeries.Points.Add(new DataPoint(i * timeStep, filted_data[i]));
                //}
                //plotModel.InvalidatePlot(true);
                samples_RMS.Clear();
            }
        }

        double DBcali()
        {
            double y = (double)currentDBSPL / 20.0f;
            double x = Math.Pow(10, y) / (float)currentSetsplVol;
            double StdPower = 1 / x * Math.Pow(10, target_DB / 20);
            return StdPower;
        }

        void WaitForSampleFinish(SampleState e)
        {
            while (sampleState < e)
            {
                Thread.Sleep(50);
            }
        }

        //void WaitForSampleFinish(SampleState e)
        //{
        //    while (sampleState = e)
        //    {
        //        Thread.Sleep(50);
        //    }
        //}

        void WaitForisRunScan()
        {
            while (isRunScan != true)
            {
                Thread.Sleep(50);
            }
        }

        void ShowResult()
        {

            Dispatcher.Invoke(() =>
                {
                    freq_result_plot freq_Result_Plot = new freq_result_plot(test_result);
                    freq_Result_Plot.Show();
                    label_freq.Content = "FREQ:STOP";
                });
        }



        private void UpdateTimer_Elapsed2()
        {
            int repeatCount = 0;
        waitPoint:
            isRunScan = false;
            WaitForisRunScan();
            //Result_Save(test_result, false);
            test_result.Clear();
            currentSetsplVol = 30000;
            target_DB = 80;
            int minFreqTh = (MinFreq / 100);
            minFreqTh = (minFreqTh <= 0) ? 100 : minFreqTh * 100;
            //freq_step = 500;
            for (Freq_Req = minFreqTh; Freq_Req <= MaxFreq; Freq_Req += freq_step)
            {
                int volume = 0;
                for (int i = 0; i < 3; i++)
                {
                    Dispatcher.Invoke(() =>
                    {
                        label_freq.Content = $"FREQ:{Freq_Req} {currentSetsplVol}";
                    });
                    //sample clean flag 需要等待一次
                    WaitForSampleFinish(SampleState.idle);
                    SSH_SendCmd($"freq {Freq_Req}\n");
                    SSH_SendCmd($"volume {currentSetsplVol}\n");
                    Thread.Sleep(100);
                    sampleState = SampleState.ready;
                    //sample ready
                    WaitForSampleFinish(SampleState.finish);
                    sampleState = SampleState.idle;
                    RMSCali();
                    var stdpower = DBcali();
                    Debug.WriteLine($"{isRight}  {Freq_Req}  {currentDBSPL} {stdpower}");
                    volume += (int)stdpower;

                }
                //currentDBSPLVol = volume;
                RecordResult(false, volume);
                //Thread.Sleep(1000);
            }
            SSH_Terminal();
            Result_Save(test_result, false);
            ShowResult();
            goto waitPoint;
        }

        private void RecordResult(bool isRight, double splvol)
        {
            var index = test_result.FindIndex(item => item.freq == Freq_Req);

            if (index == -1)
            {

                var res = new db_cali();
                res.freq = Freq_Req;
                test_result.Add(res);
                index = test_result.FindIndex(item => item.freq == Freq_Req);
            }
            var result = test_result[index];
            if (isRight == false)
                result.Loffset = splvol;
            else
                result.Roffset = splvol;

            result.result = currentDBSPL;


            test_result[index] = result;
        }
        private void InitializePlot()
        {
            plotModel = new PlotModel { Title = "Microphone Waveform" };
            waveformSeries = new LineSeries { Title = "Waveform" };
            plotModel.Series.Add(waveformSeries);

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = -1,
                Maximum = 1,
                Title = "Amplitude"
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 0.001,
                Title = "Time (s)"
            });

            WaveformPlot.Model = plotModel;
        }

        private void LoadMicrophoneList()
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var capabilities = WaveIn.GetCapabilities(i);
                MicComboBox.Items.Add($"{capabilities.ProductName} ({i})");
            }

            if (MicComboBox.Items.Count > 0)
            {
                MicComboBox.SelectedIndex = 0;
            }
        }

        private void MicComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StopCapture();

            if (MicComboBox.SelectedIndex >= 0)
            {
                StartCapture(MicComboBox.SelectedIndex);
            }
        }

        private void StartCapture(int deviceNumber)
        {
            try
            {
                waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceNumber,
                    WaveFormat = new WaveFormat(SampleRate, 16, 2),
                    BufferMilliseconds = 20
                };
                filter = new BandPassFilter(200, 3000, 44100);
                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.StartRecording();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public class BandPassFilter
        {
            private BiQuadFilter LPfilter;
            private BiQuadFilter HPfilter;
            private double sampleRate;

            public BandPassFilter(float lowFrequency, float highFrequency, double sampleRate)
            {
                this.sampleRate = sampleRate;
                // NAudio 的 BiQuadFilter 配置为带通滤波器
                float centerFreq = (float)((lowFrequency + highFrequency) / 2.0);
                float bandwidth = (float)((highFrequency - lowFrequency) / centerFreq);
                LPfilter = BiQuadFilter.LowPassFilter((float)sampleRate, highFrequency, 1);
                HPfilter = BiQuadFilter.HighPassFilter((float)sampleRate, lowFrequency, 1);
            }

            public double Process(double input)
            {
                double s = input;
                for (int j = 0; j < 5; j++)
                {
                    s = LPfilter.Transform((float)s);
                    s = HPfilter.Transform((float)s);
                }
                return s;
            }

            public List<double> Process(List<double> input)
            {
                List<double> doubles = new List<double>();
                int count = input.Count;
                for (int i = 0; i < count; i++)
                {
                    double s = input[i];
                    for (int j = 0; j < 20; j++)
                    {
                        s = LPfilter.Transform((float)s);
                        s = HPfilter.Transform((float)s);
                    }
                    doubles.Add(s);
                }
                // NAudio 的 BiQuadFilter 处理输入信号
                return doubles;
            }
        }

        private double CalculateRMS(List<double> samples)
        {
            if (samples.Count == 0) return 0;
            double meanSquare = samples.Average(x => (x * 1000) * (x * 1000));
            return Math.Sqrt(meanSquare);
        }



        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            lock (samples) // 确保线程安全
            {
                int maxSamples = SampleRate * DisplaySeconds / 10;
                for (int i = 0; i < e.BytesRecorded; i += 4)
                {
                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                    samples.Add(sample / 32768.0); // Normalize to [-1, 1]
                    if (sampleState == SampleState.Sampleing)
                        samples_RMS.Add(sample / 32768.0);
                }
                if (sampleState == SampleState.ready)
                {
                    sampleState = SampleState.Sampleing;
                    samples_RMS.Clear();
                }
                if (samples_RMS.Count > maxSamples * 5 &&
                    sampleState == SampleState.Sampleing)
                {
                    sampleState = SampleState.finish;
                }


                while (samples.Count > maxSamples)
                {
                    samples.RemoveAt(0);
                }
                //if (is1Stick)
                //{
                //    Dispatcher.Invoke(() =>
                //    {
                //        var filted_data = filter.Process(samples_RMS);
                //        double rmsValue = CalculateRMS(filted_data);
                //        // 转换为 dB
                //        double dbValue = 94 + 20 * Math.Log10(rmsValue / 275);
                //        label_db.Content = "DB:" + dbValue.ToString("");
                //        //Debug.WriteLine($"{samples.Count}");

                //        currentDBSPL = dbValue;
                //        waveformSeries.Points.Clear();
                //        double timeStep = 0.1 / SampleRate;
                //        for (int i = 0; i < samples.Count; i++)
                //        {
                //            waveformSeries.Points.Add(new DataPoint(i * timeStep, filted_data[i]));
                //        }
                //        plotModel.InvalidatePlot(true);
                //        samples_RMS.Clear();
                //    });
                //    is1Stick = false;
                //}
            }
        }

        private void StopCapture()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
            //updateTimer?.Stop();
            samples.Clear();
        }

        protected override void OnClosed(EventArgs e)
        {
            _sshClient?.Disconnect();
            _sshClient?.Dispose();
            StopCapture();
            base.OnClosed(e);
        }

        private void Btn_Begin_Click(object sender, RoutedEventArgs e)
        {
            isRunScan = true;
            SSH_SendCmd($"{LinuxRunTestPath}");
            test_result.Clear();
            //ExecuteCommandAsync("./home/pzh/qtPCMtest/bin/qtPCMtest");

        }

        private void Result_Save(List<db_cali> db_Calis, bool isLeft)
        {
            string FileName = "Cali";
            string result = $"{target_DB}\\n", resultFile = $"{target_DB}\n";
            string Sendstr;
            foreach (var item in db_Calis)
            {
                resultFile = resultFile + $"{item.freq},{(int)item.Loffset},{(int)item.Loffset}\n";
                result = result + $"{item.freq},{(int)item.Loffset},{(int)item.Loffset}\\n";
            }
            Sendstr = $"echo -e \"{result}\" > {LinuxResultStroagePath}/{FileName}.csv\n";
            Freq_cali_List_storage file = new Freq_cali_List_storage();
            string filename = $"{FileName} {target_DB}";
            file.fileWrite(filename, resultFile);
            SSH_SendCmd(Sendstr);
        }



        private void SSH_SendCmd(string cmd)
        {
            lock (ssh_cmdlines)
            {
                ssh_cmdlines.Add(cmd);

            }
        }

        private void SSH_Terminal()
        {
            ssh_terminal = true;
        }

        public async Task RunSshWithCMD(CancellationToken token, Action<string> RX_handle, Action<ShellStream> Cmd)
        {
            string host = ssh_info.IP;  // 远程主机地址
            string username = ssh_info.UserName;    // 用户名
            string password = ssh_info.Password;    // 密码
            int port = 22;                        // SSH端口（默认是 22）
            SSH_Connect_State = "连接中";
            try
            {
                // 创建 SSH 客户端并连接
                using (var sshClient = new SshClient(host, port, username, password))
                {

                    sshClient.Connect();

                    // 创建一个 Shell 会话
                    var shellStream = sshClient.CreateShellStream("dumb", 80, 24, 800, 600, 1024);

                    // 启动一个新的线程来持续读取数据
                    var readThread = new System.Threading.Thread(async () =>
                    {
                        byte[] buffer = new byte[1024];
                        //shellStream.ReadTimeout = 100;
                        while (sshClient.IsConnected && !token.IsCancellationRequested)
                        {
                            try
                            {
                                // 持续读取数据
                                var ReadStr = shellStream.ReadLine();
                                //var bytesRead = bytesReadWait;//.Result;
                                if (ReadStr.Length > 0)
                                {
                                    //string output = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                    //var output = RemoveAnsiEscapeCodes(ReadStr);
                                    //RX_handle?.Invoke(output);
                                    Debug.WriteLine(ReadStr);
                                }
                                if (ReadStr.Length == 0)
                                {

                                }
                            }
                            catch
                            {
                                Debug.WriteLine("catch error");
                            }
                        }
                        Debug.WriteLine("ssh read end");
                    });

                    readThread.IsBackground = true;  // 设置为后台线程，程序结束时自动关闭
                    readThread.Start();

                    // 向 shell 发送命令（例如，你可以在这里发送一个长时间运行的命令）
                    Cmd?.Invoke(shellStream);
                    SSH_Connect_State = "已连接";
                    while (!token.IsCancellationRequested)
                    {
                        if (ssh_terminal)
                        {
                            shellStream.Write(new byte[1] { 0x03 }, 0, 1);
                            shellStream.Flush();
                            shellStream.WriteLine(" ");
                            shellStream.Flush();
                            ssh_terminal = false;
                        }
                        lock (ssh_cmdlines)
                        {
                            foreach (var cmd in ssh_cmdlines)
                            {
                                shellStream.WriteLine(cmd);
                                shellStream.Flush();

                            }
                            ssh_cmdlines.Clear();
                        }
                        await Task.Delay(50);
                    }
                    shellStream.Close();
                    // 断开连接
                    sshClient.Disconnect();
                    Debug.WriteLine("ssh end");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                MessageBox.Show("连接错误");
                SSH_Connect_State = "连接错误";
                //Dispatcher.Invoke(() =>
                //{
                //    this.Close();
                //});
            }
        }
    }
}
