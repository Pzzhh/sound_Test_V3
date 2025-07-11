using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using static sound_test.dataStroage.MyDatabase;

namespace sound_test.dataStroage
{
    /// <summary>
    /// liteTest.xaml 的交互逻辑
    /// </summary>
    public partial class liteTest : Window
    {
        ObservableCollection<MyDatabase.singalTest> singalTestResult { get; set; }
        ObservableCollection<MyDatabase.Person> Peoples { get; set; }

        public liteTest()
        {
            InitializeComponent();
            singalTestResult = new ObservableCollection<MyDatabase.singalTest>();
            Peoples = new ObservableCollection<MyDatabase.Person>();
            TaskListViewResult.ItemsSource = singalTestResult;
            TaskListViewName.ItemsSource = Peoples;

            DataContext = this;
        }

        private void TaskListViewName_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedPerson = TaskListViewName.SelectedItem as Person;

            if (selectedPerson != null)
            {
                // 显示选中的项的详细信息
                //MessageBox.Show($"Double-clicked: {selectedPerson.ID}, {selectedPerson.name}");
                ShowResult(selectedPerson.ID);
            }
        }

        void ReflushHistoryList()
        {
            var allperson = MyDatabase.ReaAllPerson();
            if (allperson != null)
            {
                Peoples.Clear();
                foreach (var person in allperson)
                {
                    Peoples.Add(person);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ReflushHistoryList();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //var selectedPerson = TaskListViewName.SelectedItem as Person;

            //if (selectedPerson != null)
            //{
            //    string ID = selectedPerson.ID;
            //    var persons = new MyDatabase.Person() { name = "彭梓豪2" };
            //    MyDatabase.CheckPersonExist(ID, persons);
            //    MyDatabase.TestReport testReport = new MyDatabase.TestReport();

            //    for (int i = 0; i < 10; i++)
            //    {
            //        MyDatabase.singalTest test = new MyDatabase.singalTest() { FileName = "hzs", Isack = "ack" };
            //        var time = DateTime.Now;
            //        test.FileName = time.Millisecond.ToString();
            //        Thread.Sleep(10);
            //        testReport.ST.Add(test);
            //    }

            //    MyDatabase.AddReport(ID, testReport);
            //}
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var selectedPerson = TaskListViewName.SelectedItem as Person;

            if (selectedPerson != null)
            {

            }
        }
        void ShowResult(string ID)
        {
            var isVaild = MyDatabase.ReadReport(ID, out List<TestReport> task);
            if (isVaild)
            {
                if (task != null)
                {
                    singalTestResult.Clear();
                    foreach (var report in task)
                    {
                        if (report != null)
                        {
                            singalTestResult.Add(new singalTest()
                            {
                                Freq = report.TimeTag.ToString("g"),
                                Isack = "",
                                RepTime = ""
                            });//显示时间
                            foreach (var test in report.ST)
                            {
                                singalTestResult.Add(test);
                            }

                        }
                    }

                }
            }
        }

        private void TaskListViewName_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var selectedPerson = TaskListViewName.SelectedItem as Person;

            if (selectedPerson != null)
            {
                ContextMenu contextMenu = new ContextMenu();
                MenuItem menuItem1 = new MenuItem { Header = "导出csv" };
                MenuItem menuItem2 = new MenuItem { Header = "删除" };
                MenuItem menuItem3 = new MenuItem { Header = "全部删除" };
                // 菜单项点击事件
                menuItem1.Click += (sender, e) =>
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "csv文件|*.csv|所有文件|*.*";
                    saveFileDialog.FileName = $"{selectedPerson.name}_{selectedPerson.gender}_{selectedPerson.ID}";
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        // 获取用户选择的文件路径
                        string filePath = saveFileDialog.FileName;
                      
                        try
                        {
                            ExportCsv exportCsv = new ExportCsv(filePath, selectedPerson);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"保存失败 {ex.Message}", "确认",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        MessageBox.Show($"保存成功", "确认",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                };

                menuItem2.Click += (sender, e) =>
                {
                    RemoveReport(selectedPerson.ID);
                    ReflushHistoryList();
                };
                menuItem3.Click += (sender, e) =>
                {
                    foreach (var p in Peoples)
                    {
                        RemoveReport(p.ID);
                    }
                    ReflushHistoryList();
                };
                // 将菜单项添加到上下文菜单
                contextMenu.Items.Add(menuItem1);
                contextMenu.Items.Add(menuItem2);
                contextMenu.Items.Add(menuItem3);
                // 显示上下文菜单
                contextMenu.IsOpen = true;

            }
        }
    }
}
