using System;
using System.Collections.Generic;
using System.Text;

namespace DoneTetris
{
    public sealed class Done
    {
        public long Id { get; set; }
        public int BatchId { get; set; }
        public string DoneDate { get; set; } = "";
        public string DoneText { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public int GrantedLengthN { get; set; }
    }

    public sealed class Move
    {
        public long Id { get; set; }
        public long DoneId { get; set; }
        public string PlacedAt { get; set; } = "";
        public int Column { get; set; }
        public int StartRow { get; set; }
        public int LengthN { get; set; }
        public bool IsVertical { get; set; }
        public int ClearedLines { get; set; }
    }

}
