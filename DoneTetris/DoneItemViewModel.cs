using System;
using System.Collections.Generic;
using System.Text;

namespace DoneTetris
{
    internal class DoneItemViewModel
    {
        // 後に見た目との連動を増やす場合にINotifyPropertyChangedに拡張。
        public long Id { get; }
        public string DoneText { get; }
        public bool IsSelected { get; }

        public DoneItemViewModel(long id, string doneText)
        {
            Id = id;
            DoneText = doneText;
        }
    }
}
