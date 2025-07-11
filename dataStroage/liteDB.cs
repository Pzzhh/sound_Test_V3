using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiteDB;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows;
using static sound_test.dataStroage.MyDatabase.Person;
using System.Xml.Linq;
using static sound_test.dataStroage.MyDatabase;
using System.ComponentModel;
using Org.BouncyCastle.Utilities.Collections;

namespace sound_test.dataStroage
{
    public class MyDatabase
    {
        const string DataAddr = @"Data.db";
        public MyDatabase()
        {

        }

        public static void SettingInsert<T>(T item)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var setting = db.GetCollection<T>("FreqList");

            }
        }

        public static void SettingADDFreqList(FrepVolumeList f)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var FreqList = db.GetCollection<FrepVolumeList>("FreqList");
                //var existingPerson = FreqList.Find(x => x._frep == testReport._frep).FirstOrDefault();
                if (FreqList != null)
                {
                    //foreach (var item in FreqList.FindAll())
                    //{
                    //    if (f._enduring_ms == item._enduring_ms &&
                    //        f._dbhl == item._dbhl &&
                    //        f._frep == item._frep)
                    //    {
                    //        return;
                    //    }
                    //}
                    FreqList.Insert(f);
                }
            }
        }

        public static void SettingClearFreqList()
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var FreqList = db.GetCollection<FrepVolumeList>("FreqList");
                //var existingPerson = FreqList.Find(x => x._frep == testReport._frep).FirstOrDefault();
                if (FreqList != null)
                {
                    FreqList.DeleteAll();
                }
            }
        }

        public static string SettingGetSettingCellWithClass<T>(string name)
        {
            name = $"{typeof(T).Name}.{name}";
            return SettingGetSettingCell(name);
        }

        public static string SettingGetSettingCell(string name)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var setting = db.GetCollection<SettingCell>("setting");
                var settingcell = setting.Find(x => x.name == name).FirstOrDefault();
                if (settingcell != null)
                {
                    return settingcell.value;
                }
            }
            return "";
        }

        public static bool SettingSaveSettingCellWithClass<T>(string name, string value)
        {
            name = $"{typeof(T).Name}.{name}";
            return SettingSaveSettingCell(name, value);
        }

        public static bool SettingSaveSettingCell(string name, string value)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var setting = db.GetCollection<SettingCell>("setting");
                var settingcell = setting.Find(x => x.name == name).FirstOrDefault();
                if (settingcell != null)
                {
                    settingcell.value = value;
                    return true;
                }
                else
                {
                    setting.Insert(new SettingCell()
                    {
                        name = name,
                        value = value,
                    });
                }
                return false;
            }
        }

        public static List<FrepVolumeList> SettingGetFreqList()
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var FreqList = db.GetCollection<FrepVolumeList>("FreqList");
                if (FreqList != null)
                {
                    return FreqList.FindAll().ToList();
                }
            }
            return new List<FrepVolumeList>();
        }

        public static void HistoryInsert<T>(T item)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var History = db.GetCollection<T>("History");

            }
        }

        public static void CheckPersonExist(string ID, MyDatabase.Person PersonInfo)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var History = db.GetCollection<Person>("History");
                //try
                //{
                var existingPerson = History.Find(x => x.ID == ID).FirstOrDefault();

                //}

                if (existingPerson == null)
                {
                    PersonInfo.ID = ID;
                    History.Insert(PersonInfo);
                }
            }
        }



        public static void AddReport(string ID, TestReport testReport)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var History = db.GetCollection<Person>("History");
                var existingPerson = History.Find(x => x.ID == ID).FirstOrDefault();
                if (existingPerson != null)
                {
                    // 如果该电话号码已存在，更新记录，添加新的电话时间
                    existingPerson.Report.Add(testReport);
                    History.Update(existingPerson);  // 更新数据库中的记录
                }
            }
        }

        public static void RemoveReport(string ID)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var History = db.GetCollection<Person>("History");
                if (History != null)
                {
                    History.Delete(ID);
                }
            }
        }

        public static bool ReadReport(string ID, out List<TestReport> testReport)
        {
            using (var db = new LiteDatabase(DataAddr))
            {
                var History = db.GetCollection<Person>("History");
                var existingPerson = History.Find(x => x.ID == ID).FirstOrDefault();
                if (existingPerson != null)
                {
                    testReport = existingPerson.Report;
                    return true;
                }
            }
            testReport = null;
            return false;
        }

        public static Person[] ReaAllPerson()
        {
            List<Person> list = new List<Person>();
            using (var db = new LiteDatabase(DataAddr))
            {
                var History = db.GetCollection<Person>("History");
                var existingPerson = History.FindAll();
                foreach (var person in existingPerson)
                {
                    list.Add(person);
                }
            }

            return list.ToArray();
        }

        public static void ExportExamResult(string ID)
        {
            List<Person> list = new List<Person>();
            using (var db = new LiteDatabase(DataAddr))
            {
                var History = db.GetCollection<Person>("History");
                var existingPerson = History.Find(x => x.ID == ID).FirstOrDefault();
                if (existingPerson != null)
                {

                }
            }
        }



        public class singalTest
        {
            public string Freq { get; set; }
            public string dbhl { get; set; }

            public string dbhl_Adv { get; set; }
            public string testMode { get; set; }
            public string Isack { get; set; }
            public string RepTime { get; set; }
            public singalTest()
            {
                Freq = "unknowed";
                dbhl = "unknowed";
                dbhl_Adv = "unknowed";
                testMode = "unknowed";
                Isack = "unknowed";
                RepTime = "unknowed";
            }
        }

        public class TestReport
        {
            public DateTime TimeTag { get; set; }
            public List<singalTest> ST { get; set; }
            public TestReport()
            {
                ST = new List<singalTest>();
                TimeTag = DateTime.Now;

            }

        }

        public class SettingCell
        {
            public string name { get; set; }
            public string value { get; set; }
            public SettingCell()
            {
                name = "";
                value = "";
            }
        }
        //public class info
        //{
        //    public string name { get; set; }
        //    public string gender { get; set; }
        //    public string birthday { get; set; }
        //}
        public class Personinfo
        {
            public string name { get; set; }
            public string gender { get; set; }
            public string birthday { get; set; }
            public Personinfo()
            {
                name = "unknowed";
                gender = "unknowed";
                birthday = "未知";
            }
        }

        public class Person
        {
            public string ID { get; set; }
            public string name { get; set; }
            public string gender { get; set; }
            public string birthday { get; set; }

            public List<TestReport> Report { get; set; }
            public Person()
            {
                Report = new List<TestReport>();
                ID = "unknowed";
                name = "unknowed";
                gender = "unknowed";
                birthday = "未知";

            }

        }

        public class FrepVolumeList
        {
            public float _frep { get; set; }
            public float _dbspl { get => _dbhl; }

            public float _dbhl { get; set; }
            public int _enduring_ms { get; set; }
            public float _enduring_Sec { get => _enduring_ms / 1000; set { _enduring_ms = (int)value * 1000; } }

            public int _id { get; set; }
            public override string ToString()
            {
                return $"F:{_frep} V:{_dbspl} T:{_enduring_ms}";
            }
            //private static int UUID = 0;
            public FrepVolumeList()
            {
                //UUID++;
            }

        }

    }


}
