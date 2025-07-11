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
using System.Windows.Shapes;
using wpfCamTest;
using System.Drawing; // 引入 System.Drawing.Common 库



namespace sound_test.app.OCR
{
    /// <summary>
    /// ocrTest.xaml 的交互逻辑
    /// </summary>
    public partial class ocrTest : Window
    {
        private FilterInfoCollection videoDevices;  // 存储摄像头设备信息
        private VideoCaptureDevice videoSource;     // 视频捕捉设备
        private Bitmap currentFrame;                // 当前捕获的画面
        public IDcardOCR idcardOCR;                 // 获取识别
        public string ImgSavePath = @"\img";

        public ocrTest()
        {
            InitializeComponent();
            InitializeCamera();
            idcardOCR = new IDcardOCR();
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
                string runpath = Environment.CurrentDirectory;
                string imgrelativePath = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                currentFrame.Save(imgrelativePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                var dinfo = idcardOCR.OCR(imgrelativePath);

                var orignalpath = System.IO.Path.Combine(runpath, imgrelativePath);
                var targetPath = System.IO.Path.Combine(runpath + ImgSavePath, imgrelativePath);

                File.Move(orignalpath, targetPath);
                MessageBox.Show(
                    IDcardOCR.PrintClassProperties(dinfo));

                //  MessageBox.Show($"Image saved as {fileName}");
            }
        }

        // 关闭时停止摄像头流
        protected override void OnClosed(EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            base.OnClosed(e);
        }
    }
}


/* <!-- 用于显示摄像头画面 -->
        <Image Name="CameraImage" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"/>
        <!-- 按钮用于拍照 -->
        <Button Name="CaptureButton" Content="Capture" HorizontalAlignment="Right" VerticalAlignment="Bottom" Width="100" Height="50" Click="CaptureButton_Click"/>*/