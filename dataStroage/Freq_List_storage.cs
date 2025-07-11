using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sound_test.dataStroage
{
    //class Freq_List_storage
    //{
    //    List<string> list;
    //    string FileName = "para.txt";
    //    public struct FreqListType
    //    {
    //        public float Enduring_Sec;
    //        public int freq;
    //        public float dbspl;
    //    }
    //    public Freq_List_storage()
    //    {
    //        string filepath = Environment.CurrentDirectory + "\\" + FileName;
    //        try
    //        {
    //            if (File.Exists(filepath))
    //            {
    //                var context = File.ReadAllText(filepath);
    //                var context_array = context.Split("\r\n");
    //                list = context_array.ToList();
    //                int count = 0;
    //                list.RemoveAll(item => item.Length < 1);
    //            }
    //        }
    //        catch (Exception ex)
    //        {

    //        }
    //    }

    //    public List<string> GetList()
    //    {
    //        return list;
    //    }

    //    public List<FreqListType> GetFreqList()
    //    {
    //        int count = 0;
    //        foreach (var item in list)
    //        {
    //            var stritem = item.Split(new[] { '\r', '\n', ',' },
    //                StringSplitOptions.RemoveEmptyEntries);
    //            if (stritem.Length == 3)
    //            {

    //            }
    //        }
    //    }
    //}

    class Freq_cali_List_storage
    {

        string FileName = "para.txt";
        public Freq_cali_List_storage()
        {

        }

        public void fileWrite(string name, string content)
        {
            FileName = $"{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}-{name}.txt";
            string filepath = Environment.CurrentDirectory + "\\" + FileName;
            try
            {
                File.WriteAllText(filepath, content);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
