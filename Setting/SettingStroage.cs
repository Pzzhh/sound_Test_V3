using sound_test.dataStroage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sound_test.dataStroage;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.CompilerServices;
using static sound_test.dataStroage.MyDatabase;
using System.Globalization;
using static sound_test.app.SlaveHandle;
using System.Diagnostics;

namespace sound_test.Setting
{
    // 全局设置基类，定义数据结构
    public class SettingStroage
    {
        // 嵌套结构体，用于 SSH 登录参数
        public struct SshInitType
        {
            public string IP { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        // 嵌套类，用于校准参数
        public class Cali
        {
            public float TargetDb { get; set; }
            public float MaxFreq { get; set; }
            public float MinFreq { get; set; }
            public float FreqStep { get; set; }
            public float AllowError { get; set; }
            public string LinuxFileStorage { get; set; }

            public string LinuxRunTestPath { get; set; }
            public SshInitType SshLoginPara { get; set; }

            // 深拷贝方法
            public Cali Clone()
            {
                return new Cali
                {
                    TargetDb = TargetDb,
                    MaxFreq = MaxFreq,
                    MinFreq = MinFreq,
                    FreqStep = FreqStep,
                    AllowError = AllowError,
                    LinuxFileStorage = LinuxFileStorage,
                    LinuxRunTestPath = LinuxRunTestPath,
                    SshLoginPara = SshLoginPara // 结构体是值类型，自动复制
                };
            }

            public void Read()
            {
                // 使用反射保存所有属性，简化代码
                foreach (var property in typeof(Cali).GetProperties())
                {
                    string name = property.Name;
                    object value = property.GetValue(this);
                    MyDatabase.SettingSaveSettingCellWithClass<Cali>(name, value?.ToString() ?? string.Empty);
                }
            }


        }

        // 嵌套类，用于考试参数
        public class ExamPara
        {
            public float MaxDb { get; set; }
            public float MinDb { get; set; }
            public List<MyDatabase.FrepVolumeList> FrepVolumeLists = new List<MyDatabase.FrepVolumeList>();
            public int MinDelay { get; set; }
            public int MaxDelay { get; set; }

            public int DefaultVolumeLevel { get; set; }

            private event Action FrepVolumeListUpdata;

            public void SetFrepVolumeList(List<MyDatabase.FrepVolumeList> e)
            {
                if (e != null)
                {
                    Debug.WriteLine("INST HASH" + FrepVolumeLists.GetHashCode());
                    FrepVolumeLists.Clear();
                    foreach (var item in e)
                    {
                        FrepVolumeLists.Add(item);
                    }
                    //FrepVolumeLists = e;
                    FrepVolumeListUpdata?.Invoke();
                }
            }

            // 深拷贝方法
            public ExamPara Clone()
            {
                return new ExamPara
                {
                    MaxDb = MaxDb,
                    MinDb = MinDb,
                    FrepVolumeLists = new List<MyDatabase.FrepVolumeList>(FrepVolumeLists), // 深拷贝列表
                    MinDelay = MinDelay,
                    MaxDelay = MaxDelay,
                    DefaultVolumeLevel = DefaultVolumeLevel,
                };
            }

            public void Updata()
            {
                MyDatabase.SettingSaveSettingCellWithClass<ExamPara>(nameof(MaxDb), MaxDb.ToString());
                MyDatabase.SettingSaveSettingCellWithClass<ExamPara>(nameof(MinDb), MinDb.ToString());
                MyDatabase.SettingSaveSettingCellWithClass<ExamPara>(nameof(MinDelay), MinDelay.ToString());
                MyDatabase.SettingSaveSettingCellWithClass<ExamPara>(nameof(MaxDelay), MinDelay.ToString());

                FrepVolumeLists = MyDatabase.SettingGetFreqList();
            }
        }

        // 全局设置属性
        public string TestMode { get; set; }
        public string ScanIP { get; set; }
        public Cali Calibration = new Cali();
        public ExamPara Examination = new ExamPara();

        // 深拷贝方法，用于创建局部设置
        public virtual SettingStroage Clone()
        {
            return new SettingStroage
            {
                TestMode = TestMode,
                ScanIP = ScanIP,
                Calibration = Calibration.Clone(),
                Examination = Examination.Clone()
            };
        }

        private static void Save<T>(T item)
        {
            // 使用反射保存所有属性，简化代码
            foreach (var property in typeof(T).GetProperties())
            {
                string name = property.Name;
                if (string.IsNullOrEmpty(name))
                    continue;
                object value = property.GetValue(item);
                MyDatabase.SettingSaveSettingCellWithClass<T>(name, value?.ToString() ?? string.Empty);
            }
        }

        private static void Read<T>(ref T item)
        {
            foreach (var property in typeof(T).GetProperties())
            {
                string name = property.Name;
                string str = MyDatabase.SettingGetSettingCellWithClass<T>(name);

                try
                {
                    if (string.IsNullOrEmpty(str))
                    {
                        continue; // 如果数据库中没有值，跳过
                    }

                    // 根据属性类型转换值
                    if (property.PropertyType == typeof(float))
                    {
                        if (float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue))
                        {
                            property.SetValue(item, floatValue);
                        }
                        else
                        {
                            // 记录或处理转换失败（可以根据需求抛出异常或设置默认值）
                            Console.WriteLine($"无法将 '{str}' 转换为 float，属性：{name}");
                        }
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(item, str);
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue))
                        {
                            property.SetValue(item, intValue);
                        }
                        else
                        {
                            // 记录或处理转换失败（可以根据需求抛出异常或设置默认值）
                            Console.WriteLine($"无法将 '{str}' 转换为 int，属性：{name}");
                        }
                    }

                }
                catch (Exception ex)
                {
                    // 记录错误
                    Console.WriteLine($"保存属性 {name} 时出错：{ex.Message}");
                }
            }
        }



        public void Updata()
        {
            MyDatabase.SettingSaveSettingCellWithClass<SettingStroage>(nameof(TestMode), TestMode);
            MyDatabase.SettingSaveSettingCellWithClass<SettingStroage>(nameof(ScanIP), ScanIP);
            Save<Cali>(Calibration);
            Save<ExamPara>(Examination);

        }

        public static void ReadSaveSettings(ref SettingStroage settingStroage)
        {
            Read<SettingStroage>(ref settingStroage);
            Cali calibration = settingStroage.Calibration;
            Read<Cali>(ref calibration);
            settingStroage.Calibration = calibration;

            // 提取 Examination 到局部变量
            ExamPara examination = settingStroage.Examination;
            Read<ExamPara>(ref examination);
            examination.FrepVolumeLists = MyDatabase.SettingGetFreqList();
            settingStroage.Examination = examination;

        }

    }

    // 单例类，管理全局设置
    public sealed class GlobalSettings
    {
        private static GlobalSettings _instance = new GlobalSettings();
        private SettingStroage _settings;

        private GlobalSettings()
        {

            _settings = new SettingStroage
            {

                TestMode = "Default",
                ScanIP = "192.168.1.1",
                Calibration = new SettingStroage.Cali
                {
                    TargetDb = 65.0f,
                    MaxFreq = 8500.0f,
                    MinFreq = 100.0f,
                    FreqStep = 100.0f,
                    AllowError = 0.1f,
                    LinuxFileStorage = "/home/pzh/sound_capability",
                    LinuxRunTestPath = "/home/pzh/qtPCMtest/bin/qtPCMtest",
                    SshLoginPara = new SettingStroage.SshInitType
                    {
                        IP = "192.168.1.100",
                        UserName = "pzh",
                        Password = "1234"
                    }
                },
                Examination = new SettingStroage.ExamPara
                {
                    MaxDb = 100.0f,
                    MinDb = 0.0f,
                    MinDelay = 2,
                    MaxDelay = 15,
                    DefaultVolumeLevel = 10,
                }
            };
            SettingStroage.ReadSaveSettings(ref _settings);
            _settings.Updata();
            _settings.Examination.MinDelay = 1;
            _settings.Examination.MaxDelay = 3;
            _settings.Examination.MaxDb = 100;
            _settings.Examination.DefaultVolumeLevel = 40;
            _settings.Examination.MaxDb = 100;
            //_settings.ScanIP = "192.168.137.78";
            _settings.Calibration.MaxFreq = 8500.0f;
            _settings.Calibration.MinFreq = 80.0f; 
            _settings.Calibration.FreqStep = 500.0f;
            //_settings.Calibration.st = 500.0f;
            //_settings.ScanIP = "192.168.137.78";
            Debug.WriteLine("INST HASH" + _settings.Examination.FrepVolumeLists.GetHashCode());
            //MyDatabase.SettingSaveSettingCell(nameof(TestMode));
        }

        public static GlobalSettings Instance => _instance;

        public SettingStroage Settings => _settings; // 返回全局设置的深拷贝
    }

    // 局部设置类，继承自 SettingStroage
    public class SettingLocal : SettingStroage
    {
        public SettingLocal()
        {
            // 从全局设置复制初始值
            var globalSettings = GlobalSettings.Instance.Settings;
            TestMode = globalSettings.TestMode;
            ScanIP = globalSettings.ScanIP;
            Calibration = globalSettings.Calibration.Clone();
            Examination = globalSettings.Examination.Clone();
        }
    }
}
