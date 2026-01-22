using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DoneTetris
{
    internal class DoneItemViewModel : INotifyPropertyChanged
    {
        // 後に見た目との連動を増やす場合にINotifyPropertyChangedに拡張。
        public event PropertyChangedEventHandler? PropertyChanged;
        public long Id { get; }
        public string DoneText { get; }

        private bool _IsSelected;
        public bool IsSelected
        {
            get => _IsSelected;
            set
            {
                if (_IsSelected != value)
                {
                    _IsSelected = value;
                }
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
                );
        }

        public DoneItemViewModel(long id, string doneText)
        {
            Id = id;
            DoneText = doneText;
        }
    }
}
