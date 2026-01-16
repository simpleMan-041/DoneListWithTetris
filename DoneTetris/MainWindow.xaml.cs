using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Linq;
using static DoneTetris.DoneRepository;

namespace DoneTetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DoneRepository _doneRepo = new();
        private readonly MoveRepository _moveRepo = new();

        private void LoadDoneAndMino()
        {
            var dones = _doneRepo.GetAllDonesOrdered();
            var moves = _moveRepo.GetAllMovesOrdered();

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            int todayCount = dones.Count(d => d .DoneDate == today);
            int totalCount = dones.Count();

            int score = moves.Sum(m => m.ClearedLines);

            var nextDone = _doneRepo.GetOldestUnplacedDone();
            string nextMinoText = nextDone is null ? "-" : $"{nextDone.GrantedLengthN}";
        }







        public MainWindow()
        {
            InitializeComponent();
        }
    }
}