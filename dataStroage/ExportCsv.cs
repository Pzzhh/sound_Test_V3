using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using sound_test.dataStroage;

namespace sound_test.dataStroage
{
    public class ExportCsv
    {
        public ExportCsv(string filePath, MyDatabase.Person person)
        {
            var CsvContext = HeadCreate(person);
            CsvContext += ReportInsert(person);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine(CsvContext);  // 写入一行文本
            }
        }

        private string HeadCreate(MyDatabase.Person person)
        {
            Mystring str = new Mystring("");
            str.AddColume("报告生成时间", DateTime.Now.ToString("f"));
            //报告生成时间
            str.AddColume(
                "姓名", "身份证", "性别", "出生");
            // ↑ [标题] 姓名 身份证 性别 生日
            str.AddColume(
                person.name, person.ID, person.gender, person.birthday);
            // ↑ 姓名         身份证     性别           生日
            str.AddColume("结果总表");

            return str.context;
        }
        private string ReportInsert(MyDatabase.Person person)
        {
            Mystring str = new Mystring("");
            int count = 1;
            foreach (var report in person.Report)
            {
                str.AddColume("");
                str.AddColume($"编号:{count}", $"测试时间:{report.TimeTag.ToString("f")}");
                str.AddColume("频率","dbhl","升五降十","测试模式(S:单 U:多)", "结果", "反应时间");
                foreach (var st in report.ST)
                {
                    str.AddColume(st.Freq,st.dbhl,st.dbhl_Adv,st.testMode, st.Isack, st.RepTime);
                }
                count++;
            }
            // ↑ 测试结果
            return str.context;
        }
        class Mystring
        {
            public string context;
            public Mystring(string str) { context = str; }

            public void AddColume(params string[] str)
            {
                foreach (string item in str)
                {
                    context += (item + ",");
                }
                context += "\n";
            }
        }
    }
}
