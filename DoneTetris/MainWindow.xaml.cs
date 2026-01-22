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
        private const int Cols = 10;
        private const int Rows = 20;
        
        private readonly DoneRepository _doneRepo = new();
        private readonly MoveRepository _moveRepo = new();
        private readonly MetaRepository _metaRepo = new();

        private bool _nextIsVertical = false;

        // 盤面を表現
        private bool[,] _board = new bool[Rows, Cols];  

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            NormalizeStreakOnStartUp();
            LoadDoneAndMino();
        }

        private void LoadDoneAndMino()
        {
            var dones = _doneRepo.GetAllDonesOrdered();
            var moves = _moveRepo.GetAllMovesOrdered();

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            int todayCount = dones.Count(d => d.DoneDate == today);
            int totalCount = dones.Count();
            int score = moves.Sum(m => m.ClearedLines);
            int streak = _metaRepo.GetInt("CurrentStreak", 0);

            _board = new bool[Rows, Cols];
            int computedScore = 0;

            foreach (var m in moves)
            {
                if (m.LengthN < 1 || m.LengthN > 5) continue;
                if (m.Column < 0 || m.Column > Cols) continue;

                int n = m.LengthN;
                bool isV = m.IsVertical;
                // ミノを配置する際、左端のミノの位置を計算するための式。
                // 左端がはみ出ないとき、右端の確認をした方が効率的だと考えたから。
                int left = isV ? m.Column : (m.Column - (n - 1) / 2);

                if (!CanPlace(_board, m.StartRow, left, n, isV)) continue;

                FillCells(_board, m.StartRow, left, n, isV);
                int cleared = ClearLines(_board);

                // スコアはDBを使う方が一貫性があると判断したためClearedLanesを使う。
                // 再構築の都合でclearedを使うことも問題ない。
                computedScore += m.ClearedLines;
            }

            TodayCountText.Text = todayCount.ToString();
            TotalCountText.Text = totalCount.ToString();
            StreakText.Text = streak.ToString();
            ScoreText.Text = computedScore.ToString();

            if (string.IsNullOrWhiteSpace(StreakText.Text))
                StreakText.Text = "0";

            var nextDone = _doneRepo.GetOldestUnplacedDone();
            NextMinoText.Text = nextDone is null ? "-" : nextDone.GrantedLengthN.ToString();
            NextMinoOrientationText.Text = _nextIsVertical ? "縦" : "横";

            var todayDones = _doneRepo.GetDonesByDate(today);
            TodayDoneListView.ItemsSource = todayDones
                .Select(d => new DoneItemViewModel(d.Id, d.DoneText))
                .ToList();

            DrawBoard();
            ShowInputWarning(false);

        }

        private void NormalizeStreakOnStartUp()
        {
            // CurrentStreakに補正を書けることでstreakを正確にするためのメソッド
            var last = _metaRepo.Get("LastActivateDate") ?? "";
            if (string.IsNullOrWhiteSpace(last))
            {
                _metaRepo.SetInt("CurrentStreak", 0);
                return;
            }

            var today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            var yesterday = DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd");

            if (last != today && last != yesterday)
            {
                _metaRepo.SetInt("CurrentStreak", 0);
            }
        }

        private void TodayDoneCount_Click(object sender, RoutedEventArgs e)
        {
            // 表示を分かりやすくするための.Focus()
            TodayDoneListView.Focus();
        }

        private void TetrisArea_RightClick(object sender, MouseButtonEventArgs e)
        {
            _nextIsVertical = !_nextIsVertical;
            NextMinoOrientationText.Text = _nextIsVertical ? "縦" : "横";
        }

        private void TetrisArea_Click(object sender, MouseButtonEventArgs e)
        {
            var nextDone = _doneRepo.GetOldestUnplacedDone();
            if (nextDone is null)
            {
                BlinkBorder(TetrisBorder, Colors.IndianRed);
                return;
            }

            var p = e.GetPosition(TetrisCanvas);
            int col = GetColumnFromX(p.X);

            int n = nextDone.GrantedLengthN;
            bool isVertical = _nextIsVertical;

            if (!TryFindDropPlacement(_board, col, n, isVertical, out int startRow, out int leftCol))
            {
                BlinkBorder(TetrisBorder, Colors.IndianRed);
                return;
            }

            var boardCopy = (bool[,])_board.Clone();
            FillCells(boardCopy, startRow, leftCol, n, isVertical);
            int cleared = ClearLines(boardCopy);

            try
            {
                _moveRepo.AddMove(new Move
                {
                    DoneId = nextDone.Id,
                    PlacedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Column = col,
                    StartRow = startRow,
                    LengthN = n,
                    IsVertical = isVertical,
                    ClearedLines = cleared
                });

                BlinkBorder(TetrisBorder, Colors.LightGreen);
                LoadDoneAndMino();
            }
            catch
            {
                BlinkBorder(TetrisBorder, Colors.IndianRed);
            }
        }

        private int GetColumnFromX(double x)
        {
            double w = Math.Max(1, TetrisCanvas.ActualWidth);
            double cellW = w / Cols;
            int col = (int)(x / cellW);
            if (col < 0) col = 0; // 左右からはみ出したときのために補正をかけています。
            if (col >= Cols) col = Cols - 1;
            return col;
        }

        private static bool TryFindDropPlacement(bool[,] board, int centerCol, int n, bool isVertical,
                                                out int startRow, out int leftCol)
        {
            startRow = -1;
            leftCol = -1;

            if (n < 1 || n > 5) return false;

            if (!isVertical)
            {
                int left = centerCol - (n - 1) / 2;
                int right = left + n - 1;
                if (left < 0 || right >= Cols) return false;

                for (int r = Rows - 1; r >= 0; r--)
                {
                    bool ok = true;
                    for (int c = left; c <= right; c++)
                    {
                        if (board[r, c]) { ok = false; break; }
                    }
                    if (ok)
                    {
                        startRow = r;
                        leftCol = left;
                        return true;
                    }
                }
                return false;
            }
            else
            {
                int c = centerCol;
                int maxStart = Rows - n;
                if (maxStart < 0) return false;

                for (int r = maxStart; r >= 0; r--)
                {
                    bool ok = true;
                    for (int k = 0; k < n; k++)
                    {
                        if (board[r + k, c]) { ok = false; break; }
                    }
                    if (ok)
                    {
                        startRow = r;
                        leftCol = c;
                        return true;
                    }
                }
                return false;
            }
        }

        private static void FillCells(bool[,] board, int startRow, int leftCol,int n, bool isVertical)
        {
            // メモリ上のミノ配列データを書き換える。
            if (!isVertical)
            {
                for (int i = 0; i < n; i++)
                    board[startRow, leftCol + i] = true;
            }
            else
            {
                for (int i = 0; i < n; i++)
                    board[startRow + i, leftCol] = true;
            }
        }

        private static int ClearLines(bool[,] board)
        {
            int cleared = 0;

            for (int r = Rows - 1; r >= 0; r--)
            {
                bool full = true;
                for (int c = 0; c < Cols; c++)
                {
                    if (!board[r, c]) { full = false; break; }
                }

                if (!full) continue;

                for (int rr = r; rr >= 1; rr--)
                {
                    for (int c = 0; c < Cols; c++)
                        board[rr, c] = board[rr - 1, c];
                }
                
                for (int c = 0; c < Cols; c++)
                    board[0, c] = false;

                cleared++;
                r++;
            }
            return cleared;
        }

        private void DoneInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

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

            string today = DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                int newStreak = ComputeAndUpdateSteak(today);
                int maxN = GetMaxByStreak(newStreak);

                var rnd = new Random();
                int GrantN() => rnd.Next(1, maxN + 1);

                _doneRepo.AddDones(batchId, today, parts, GrantN);

                DoneInputBox.Clear();
                BlinkBorder(DoneInputBox, Colors.LightGreen);
                LoadDoneAndMino();
            }
            catch
            {
                ShowInputWarning(true, "保存に失敗しました。もう一度試してください!");
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

        private static bool CanPlace(bool[,] board, int startRow, int leftCol, int n, bool isVertical)
        {
            if (!isVertical)
            {
                int right = leftCol + n - 1;
                if (startRow < 0 || startRow >= Rows) return false;
                if (leftCol < 0 || right >= Cols) return false;

                for (int c = leftCol; c <= right; c++)
                    if (board[startRow, c]) return false;

                return true;
            }
            else
            {
                int bottom = startRow + n - 1;
                if (leftCol < 0 || leftCol >= Cols) return false;
                if (startRow < 0 || bottom >= Rows) return false;

                for (int r = startRow; r <= bottom; r++)
                    if (board[r, leftCol]) return false;

                return true;
            }
        }

        private void DrawBoard()
        {
            if (TetrisCanvas == null) return;

            TetrisCanvas.Children.Clear();

            double w = Math.Max(1, TetrisCanvas.ActualWidth);
            double h = Math.Max(1, TetrisCanvas.ActualHeight);

            double cellW = w / Cols;
            double cellH = h / Rows;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (!_board[r, c]) continue;

                    var rect = new Rectangle
                    {
                        Width = Math.Max(1, cellW - 1),
                        Height = Math.Max(1, cellH - 1),
                        Fill = Brushes.MediumPurple,
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(rect, c * cellW);
                    Canvas.SetTop(rect, r * cellH);
                    TetrisCanvas.Children.Add(rect);
                }
            }
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

        private async void BlinkBorder(FrameworkElement elem, Color color)
        {
            // 変更の保存・キャンセルが見ることで理解できるように枠色を一瞬変えて戻す機能を付ける。
            // 将来的に点滅させることを考えているため後ほどDispatchTimerを加える可能性がある。
            if (elem is not System.Windows.Controls.Border border) return;

            Brush original = border.BorderBrush;
            border.BorderBrush = new SolidColorBrush(color);
            await System.Threading.Tasks.Task.Delay(160);
            border.BorderBrush = original;

        }

        private int ComputeAndUpdateSteak(string today)
        {
            var last = _metaRepo.Get("LastActivateDate") ?? "";
            int streak = _metaRepo.GetInt("CurrentStreak", 0);

            if (last == today) return streak;

            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            int newStreak = (last == yesterday) ? (streak + 1) : 1;

            _metaRepo.Set("LastActiveDate", today);
            _metaRepo.SetInt("CurrentStreak", newStreak);

            return newStreak;

        }





        
    }
}