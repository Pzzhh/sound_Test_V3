using AForge.Video.DirectShow;
using AForge.Video;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using wpfCamTest;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;
using static wpfCamTest.IDcardOCR;
using System.Reflection;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static sound_test.dataStroage.MyDatabase;
//using System.

namespace sound_test.app.OCR
{
    /// <summary>
    /// USC_IDcardOcr.xaml 的交互逻辑
    /// </summary>
    public partial class USC_IDcardOcr : UserControl
    {
        private FilterInfoCollection videoDevices;  // 存储摄像头设备信息
        private VideoCaptureDevice videoSource;     // 视频捕捉设备
        private Bitmap currentFrame;                // 当前捕获的画面
        public IDcardOCR idcardOCR;                 // 获取识别
        public string ImgSavePath = @"\img";
        public Action<IDinfo> ConfirmEvent;
        //private IDinfo personInfo;

        public USC_IDcardOcr()
        {
            InitializeComponent();
            InitializeCamera();
            idcardOCR = new IDcardOCR();
            // personInfo = null;
        }

        // 初始化摄像头
        private void InitializeCamera()
        {
            // 获取系统中的所有视频设备
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("No video devices found!");
                return;
            }

            // 选择第一个摄像头
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);

            // 设置画面更新事件
            videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);

            // 开始视频流
            videoSource.Start();
        }

        // 每当新的一帧画面被捕获时调用
        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // 将捕获的帧转换为 Bitmap 格式
            currentFrame = (Bitmap)eventArgs.Frame.Clone();

            // 在 UI 线程上更新图像显示
            Dispatcher.Invoke(() =>
            {
                CameraImage.Source = BitmapToImageSource(currentFrame);
            });
        }

        // 将 Bitmap 转换为 ImageSource（适用于 WPF）
        private System.Windows.Media.Imaging.BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                // 将 Bitmap 保存到内存流
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Seek(0, SeekOrigin.Begin);

                // 使用 BitmapImage 来创建 ImageSource
                System.Windows.Media.Imaging.BitmapImage imageSource = new System.Windows.Media.Imaging.BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = memory;
                imageSource.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                imageSource.EndInit();

                return imageSource;
            }
        }

        // 点击按钮时，保存当前画面为图片
        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentFrame != null)
            {
                //string runpath = Environment.CurrentDirectory;
                ////string imgrelativePath = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                ////currentFrame.Save(imgrelativePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                //var fileaddr = System.IO.Path.Combine(runpath, imgrelativePath);
                var dinfo = idcardOCR.OCR(currentFrame);
                MessageBox.Show(
                   IDcardOCR.PrintClassProperties(dinfo));
                if (dinfo != null)
                {
                    Dinfo2IDViewModel(ref personinfoBox.cardViewModel, dinfo);
                }
            }
        }


        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 关闭时停止摄像头流
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }
        }

        private void test_btn1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.*|*.bmp;*.jpg;*.jpeg;*.tiff;*.tiff;*.png";
            if (ofd.ShowDialog() != true) return;
            var dinfo = idcardOCR.OCR(ofd.FileName);
            string showstr = string.Empty;
            MessageBox.Show(
                   IDcardOCR.PrintClassProperties(dinfo));
            if (dinfo != null)
            {
                Dinfo2IDViewModel(ref personinfoBox.cardViewModel, dinfo);
            }

        }

        private void ConFirm_Click(object sender, RoutedEventArgs e)
        {
            var idviewinfo = personinfoBox.cardViewModel;
            if (idviewinfo.Name == null ||
                idviewinfo.IDNumber == null)
            {
                MessageBoxResult result = MessageBox.Show("未输入身份证号和姓名，确认退出?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            var dinfo = new IDinfo();
            IDViewModel2Dinfo(idviewinfo, ref dinfo);
            ConfirmEvent?.Invoke(dinfo);
        }

        void Dinfo2IDViewModel(ref IDCardViewModel idviewinfo, IDinfo dinfo)
        {
            idviewinfo.Name = dinfo.name;
            idviewinfo.Address = dinfo.address;
            idviewinfo.IDNumber = dinfo.IDnumber;
            idviewinfo.Gender = dinfo.gender;
            idviewinfo.BirthDate = DateTime.ParseExact(dinfo.birthday, "yyyy年M月d日", System.Globalization.CultureInfo.InvariantCulture);
        }

        void IDViewModel2Dinfo(IDCardViewModel idviewinfo, ref IDinfo dinfo)
        {
            dinfo.name = idviewinfo.Name;
            dinfo.address = idviewinfo.Address;
            dinfo.IDnumber = idviewinfo.IDNumber;
            dinfo.gender = idviewinfo.Gender;
            dinfo.birthday = idviewinfo.BirthDate?.ToString("g");
        }
    }
}
