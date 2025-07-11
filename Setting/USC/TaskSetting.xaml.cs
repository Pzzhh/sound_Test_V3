using Org.BouncyCastle.Utilities.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using sound_test.dataStroage;

namespace sound_test.Setting.USC
{
    /// <summary>
    /// TaskSetting.xaml 的交互逻辑
    /// </summary>
    public partial class TaskSetting : UserControl
    {

        public class LimitedValue
        {
            public readonly int MaxEnduring_Sec = 30;
            public readonly int MinEnduring_Sec = 3;
            public readonly int MaxDBSPL = 80;
            public readonly int MinDBSPL = 10;
            public readonly int MaxFreq = 20000;
            public readonly int MinFreq = 100;
        };

        public TaskSetting()
        {

            InitializeComponent();
            DataContext = new ItemListViewModel();
        }
        public class ItemListViewModel
        {
            public ObservableCollection<Item> Items { get; set; }
            public ICommand AddItemCommand { get; }
            public ICommand DeleteItemCommand { get; }
            public ICommand SaveItemCommand { get; }

            public ItemListViewModel()
            {
                Items = new ObservableCollection<Item>();
                AddItemCommand = new RelayCommand(AddItem);
                DeleteItemCommand = new RelayCommand(DeleteItem);
                SaveItemCommand = new RelayCommand(SaveItem);
                var list = MyDatabase.SettingGetFreqList();
                Items.Clear();
                foreach (var item in list)
                {
                    //[I] 检查以下 dbspl
                    Items.Add(new Item { Freq = item._frep, DBSPL = (decimal)item._dbhl, Enduring_Sec = (int)item._enduring_Sec });
                }
            }

            private void AddItem(object parameter)
            {

                Items.Add(new Item { Freq = 1000, DBSPL = 60, Enduring_Sec = 10 });

            }

            private void DeleteItem(object parameter)
            {
                if (parameter is Item item)
                {
                    Items.Remove(item);
                }
            }

            private void SaveItem(object parameter)
            {

                MyDatabase.SettingClearFreqList();
                //List<MyDatabase.FrepVolumeList> frepVolumeList = new List<MyDatabase.FrepVolumeList>();
                var list = new List<MyDatabase.FrepVolumeList>();
                foreach (var itemx in Items)
                {
                    MyDatabase.FrepVolumeList f = new MyDatabase.FrepVolumeList();
                    f._dbhl = (float)itemx.DBSPL;
                    f._frep = itemx.Freq;
                    f._enduring_ms = itemx.EnduringMs;
                    //frepVolumeList.Add(f);
                    MyDatabase.SettingADDFreqList(f);
                    list.Add(f);
                }
                GlobalSettings.Instance.Settings.Examination.SetFrepVolumeList(list);
            }
        }


        public class Item : INotifyPropertyChanged, IDataErrorInfo
        {

            LimitedValue limited = new LimitedValue();

            private float _frep;
            private decimal _dbspl;
            private int _enduring_Sec;

            public float Freq
            {
                get => _frep;
                set
                {
                    _frep = value;
                    OnPropertyChanged(nameof(Freq));
                }
            }

            public decimal DBSPL  //注意 其实是dbhl的意思
            {
                get => _dbspl;
                set
                {
                    _dbspl = value;
                    OnPropertyChanged(nameof(DBSPL));
                }
            }

            public int EnduringMs
            {
                get { return _enduring_Sec * 1000; }

            }

            public int Enduring_Sec
            {
                get => _enduring_Sec;
                set
                {
                    _enduring_Sec = value;
                    OnPropertyChanged(nameof(Enduring_Sec));
                }
            }

            public string Error => null;

            public string this[string columnName]
            {
                get
                {
                    if (columnName == nameof(Freq))
                    {
                        if (Freq < limited.MinFreq)
                            return "Freq cannot be negative.";
                        if (Freq > limited.MaxFreq)
                            return $"Freq cannot be exceed:{limited.MaxFreq}.";
                    }
                    if (columnName == nameof(Enduring_Sec))
                    {
                        if (Enduring_Sec > limited.MaxEnduring_Sec || Enduring_Sec < limited.MinEnduring_Sec)
                            return $"Enduring_Sec allow range {limited.MinEnduring_Sec}-{limited.MaxEnduring_Sec}";
                    }
                    if (columnName == nameof(DBSPL))
                    {

                        //if (!decimal.TryParse(_dbspl, out decimal DBSPL))
                        if (DBSPL > limited.MaxDBSPL || DBSPL < limited.MinDBSPL)
                            return $"DBSPL allow range {limited.MinDBSPL}-{limited.MaxDBSPL}";
                    }
                    return null;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter) => _execute(parameter);

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}
