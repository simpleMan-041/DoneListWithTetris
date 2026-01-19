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
using System.Collections.Generic;
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

        private bool _nextIsVertical = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDoneAndMino();
        }

        private void TodayDoneCount_Click(object sender, RoutedEventArgs e)
        {
            // 表示を分かりやすくするための.Focus()
            TodayDoneListView.Focus();
        }

        private void DoneInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) return;

            e.Handled = true;

            var raw = DoneInputBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(raw))
            {
                ShowInputWarning(true);
                BlinkBorder(DoneInputBox, Colors.IndianRed);
                return;
            }

            var parts = raw.Split("。")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (parts.Count == 0)
            {
                ShowInputWarning(true);
                BlinkBorder(DoneInputBox, Colors.IndianRed);
                return;
            }

            ShowInputWarning(false);

            int batchId = _doneRepo.GetNextBatchId();

            int streak = SafeParseInt(StreakText.Text);
            int maxN = GetMaxByStreak(streak);

            var rnd = new Random();
            int GrantN() => rnd.Next(1,maxN + 1);

            string today = DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                _doneRepo.AddDones(batchId, today, parts, GrantN);

                DoneInputBox.Clear();
                BlinkBorder(DoneInputBox, Colors.LightGreen);
                LoadDoneAndMino();
            }
            catch
            {
                ShowInputWarning(true, "保存に失敗しました。もう一度試してください！");
                BlinkBorder(DoneInputBox, Colors.IndianRed);
            }
        }

        private void DeleteSelectedButton_Click(object sender, EventArgs e)
        {
            var items = TodayDoneListView.ItemsSource as List<DoneItemViewModel>;
            if (items == null) return;

            var selectedIds = items.Where(x => x.IsSelected).Select(x => x.Id).ToList();
            if (selectedIds.Count == 0)
            {
                ShowInputWarning(true, "削除するDoneにチェックを入れましょう！");
                BlinkBorder(DoneInputBox, Colors.IndianRed);
                return;
            }

            var result = MessageBox.Show("選択したDoneを削除します。よろしいですか？",
                                         "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            _doneRepo.DeleteDoneByIds(selectedIds);
            LoadDoneAndMino() ;

        }

        private void TetrisArea_Click(object sender, EventArgs e)
        {

        }

        private void TetrisArea_RightClick(object sender, EventArgs e)
        {
            _nextIsVertical = !_nextIsVertical;
            NextMinoOrientationText.Text = _nextIsVertical ? "縦" : "横";
        }

        private void LoadDoneAndMino()
        {
            var dones = _doneRepo.GetAllDonesOrdered();
            var moves = _moveRepo.GetAllMovesOrdered();

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            int todayCount = dones.Count(d => d .DoneDate == today);
            int totalCount = dones.Count();
            int score = moves.Sum(m => m.ClearedLines);

            TodayCountText.Text = todayCount.ToString();
            TotalCountText.Text = totalCount.ToString();
            ScoreText.Text = score.ToString();

            if (string.IsNullOrWhiteSpace(StreakText.Text))
                StreakText.Text = "0";

            var nextDone = _doneRepo.GetOldestUnplacedDone();
            if (nextDone is null)
            {
                NextMinoText.Text = "-";
            }
            else
            {
                NextMinoText.Text = nextDone.GrantedLengthN.ToString();
            }

            var todayDones = _doneRepo.GetDonesByDate(today);
            TodayDoneListView.ItemsSource = todayDones
                .Select(d => new DoneItemViewModel(d.Id, d.DoneText))
                .ToList();

            ShowInputWarning(false);

        }

        private static int GetMaxByStreak(int streak)
        {
            if (streak >= 14) return 5;
            if (streak >= 7) return 4;
            if (streak >= 5) return 3;
            if (streak >= 3) return 2;
            return 1;
        }

        private static int SafeParseInt(string? s)
            => int.TryParse(s, out var v) ? v : 0;

        private void ShowInputWarning(bool show, string? message = null)
        {
            InputWarningText.Text = message ?? "空です。Doneを入力しましょう！";
            InputWarningText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void BlinkBorder(System.Windows.Controls.Control ctrl, Color color)
        {
            // 変更の保存・キャンセルが見ることで理解できるように枠色を一瞬変えて戻す機能を付ける。
            // 将来的に点滅させることを考えているため後ほどDispatchTimerを加える可能性がある。
            var original = ctrl.BorderBrush;
            ctrl.BorderBrush = new SolidColorBrush(color);

            await System.Threading.Tasks.Task.Delay(180);

            ctrl.BorderBrush = original;
        }







        
    }
}