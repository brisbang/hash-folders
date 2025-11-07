using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HashFolders
{
    public class RowItem : INotifyPropertyChanged
    {
        private string _action = string.Empty;
        public string Action
        {
            get => _action;
            set
            {
                if (_action != value)
                {
                    _action = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _target = string.Empty;
        public string Target
        {
            get => _target;
            set
            {
                if (_target != value)
                {
                    _target = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class WorkerStatusViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RowItem> Rows { get; } = new ObservableCollection<RowItem>();

        private RowItem _selectedRow;
        public RowItem SelectedRow { get => _selectedRow; set { _selectedRow = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
