using System;
using System.Collections.Generic;
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

namespace sound_test.app.OCR
{
    /// <summary>
    /// personinfo.xaml 的交互逻辑
    /// </summary>
    public partial class personinfo : UserControl, INotifyPropertyChanged
    {
        public IDCardViewModel cardViewModel;
        public personinfo()
        {
            InitializeComponent();
            cardViewModel = new IDCardViewModel();
            this.DataContext = cardViewModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class IDCardViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _gender;
        private DateTime? _birthDate;
        private string _idNumber;
        private string _address;
        private string _issuingAuthority;
        private DateTime? _validityDate;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Gender
        {
            get { return _gender; }
            set
            {
                if (_gender != value)
                {
                    _gender = value;
                    OnPropertyChanged(nameof(Gender));
                }
            }
        }

        public DateTime? BirthDate
        {
            get { return _birthDate; }
            set
            {
                if (_birthDate != value)
                {
                    _birthDate = value;
                    OnPropertyChanged(nameof(BirthDate));
                }
            }
        }

        public string IDNumber
        {
            get { return _idNumber; }
            set
            {
                if (_idNumber != value)
                {
                    _idNumber = value;
                    OnPropertyChanged(nameof(IDNumber));
                }
            }
        }

        public string Address
        {
            get { return _address; }
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged(nameof(Address));
                }
            }
        }

        public string IssuingAuthority
        {
            get { return _issuingAuthority; }
            set
            {
                if (_issuingAuthority != value)
                {
                    _issuingAuthority = value;
                    OnPropertyChanged(nameof(IssuingAuthority));
                }
            }
        }

        public DateTime? ValidityDate
        {
            get { return _validityDate; }
            set
            {
                if (_validityDate != value)
                {
                    _validityDate = value;
                    OnPropertyChanged(nameof(ValidityDate));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

/*<ComboBox SelectedItem="{Binding Gender}" Width="200" VerticalAlignment="Center" Margin="5,0">
                    <ComboBoxItem Content="男"/>
                    <ComboBoxItem Content="女"/>
                </ComboBox>*/
