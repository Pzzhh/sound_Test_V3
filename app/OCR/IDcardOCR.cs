using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows;
using PaddleOCRSharp;
using System.Diagnostics;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Drawing;

namespace wpfCamTest
{
    public class IDcardOCR
    {
        private PaddleOCREngine engine;
        public IDcardOCR()
        {
            //使用默认中英文V4模型
            PaddleOCRSharp.OCRModelConfig config = null;
            //使用默认参数
            PaddleOCRSharp.OCRParameter oCRParameter = new PaddleOCRSharp.OCRParameter();
            oCRParameter.max_side_len = 2000;
            //建议程序全局初始化一次即可，不必每次识别都初始化，容易报错。     
            engine = new PaddleOCRSharp.PaddleOCREngine(config, oCRParameter);

        }

        public class IDinfo
        {
            public string name;
            public string gender;
            public string racial;
            public string birthday;
            public string address;
            public string IDnumber;
        }
        //姓名苏海峰性别男民族汉出生1988年9月15日住址安徽省天长市千秋新村三巷10号公民身份号码341181198809150011
        IDinfo DecodeString(List<TextBlock> textBlock)
        {
            string text = string.Empty;
            IDinfo info = new IDinfo();
            foreach (TextBlock block in textBlock)
            {
                if (block.Score > 0.80)
                {
                    text += block.Text;
                }
            }
            string split = @"姓名|性别|民族|出生|住址|公民身份号码";

            // 使用正则表达式分隔符 "|" 和 ";"
            string[] fruits = Regex.Split(text, split);
            if (fruits.Length == 7)
            {
                info.name = fruits[1];
                info.gender = fruits[2];
                info.racial = fruits[3];
                info.birthday = fruits[4];
                info.address = fruits[5];
                info.IDnumber = fruits[6];
                return info;
            }
            return null;
        }

        public IDinfo OCR(string filename)
        {
            try
            {
                var ocrResult = engine.DetectText(filename);
                var info = DecodeString(ocrResult.TextBlocks);
                return info;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"ocr error {e.Message}");
            }
            return null;
        }
        public IDinfo OCR(Image image)
        {
            try
            {
                var ocrResult = engine.DetectText(image);
                var info = DecodeString(ocrResult.TextBlocks);
                return info;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"ocr error {e.Message}");
            }
            return null;
        }
        public static string PrintClassProperties(object obj)
        {
            string objStr = "";
            if (obj == null)
                return "识别识别";
            // 获取对象的类型信息
            Type objType = obj.GetType();

            // 获取对象的所有公共字段
            FieldInfo[] fields = objType.GetFields();
            foreach (var field in fields)
            {
                // 获取字段的名称和值
                object value = field.GetValue(obj);
                objStr += $"{field.Name}: {value}\n";
            }
            return objStr;
        }

    }
}
