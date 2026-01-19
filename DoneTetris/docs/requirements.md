---

# DoneTetris 要件確定：画面イベント一覧＋DB最小設計（確定版）

## 0. 前提（確定）

* **次ミノの選び方**：未配置Done（Moveが存在しないDone）の **先頭（最も古い）1件**
* **横ミノ基準**：クリック列を **中心**として配置（中心揃え）
* **Done削除と盤面**：直近追加を取り消すと **盤面も巻き戻す（再構築）**
* **今日判定**：ローカル日付（`DateTime.Now.Date`、0:00切替）
* **ミノ**：棒型のみ

  * 長さ `N=1..maxN`（maxNは連続日数により決定）
  * 方向：横（1×N）/縦（N×1）を **回転で切替**

---

## 1. MainWindow：イベント一覧（確定）

### 1.1 LoadDoneAndMino（起動時・再読み込み）

**目的**：DBから状態復元→統計・次ミノ・盤面を描画

* 入力：なし
* DB（読み）

  * Done：全件（Id/CreatedAt昇順で利用）
  * Move：全件（PlacedAt/Id昇順で適用）
  * Meta：CurrentStreak、LastActiveDate
* 処理（ロジック）

  1. Metaから連続日数などを復元
  2. 盤面を空で初期化
  3. Moveを順に適用して盤面再構築（ライン消去も反映）
  4. スコア = `sum(ClearedLines)`
  5. 今日Done数/総Done数/継続日数を算出
  6. **未配置Done（Moveが無いDone）を抽出**
  7. **次ミノ = 未配置Doneの先頭1件** の `GrantedLengthN` を表示

     * 未配置が無ければ「次ミノなし（手数0）」表示
* 出力（UI更新）

  * 盤面描画
  * 総Done数、今日Done数、スコア、継続日数
  * 次ミノ表示（長さN・向き）

> 運用ルール：画面更新は迷ったら **LoadDoneAndMino() で統一**（最初はこれが最強）

---

### 1.2 AddButton_Click（Done記録ボタン）

* 入力：なし
* 処理

  * AddDoneWindow を `ShowDialog()`
  * 戻り値 `true` のとき `LoadDoneAndMino()` を実行
* DB：なし（AddDoneWindow側で実施）
* UI：再読み込みで全更新

---

### 1.3 TodayDoneCount_Click（今日Done数エリアクリック）

* 入力：今日の日付
* DB（読み）

  * 今日Done一覧（DoneDate==today）を取得
* 出力（UI）

  * TodayDoneWindow（ListViewに表示）

---

### 1.4 Undo_Done_Addition_Click（直近の追加取り消し）

**目的**：直近の追加操作（複数Done）をまとめて取り消し、盤面も巻き戻す

* 入力：なし
* DB（読み）

  * 最新BatchId = `MAX(Done.BatchId)`
* DB（書き）

  * `Done`：そのBatchIdの行を削除
  * `Move`：それらDoneIdに紐づくMoveを削除（FK CASCADE推奨）
* 処理

  * 削除後 `LoadDoneAndMino()` を実行
* 出力（UI）

  * 盤面、統計、次ミノ、今日Done表示を再構築

> 注意：Idを「追加回数でインクリメント」ではなく、**BatchId列でグループ化**する。

---

### 1.5 TetrisArea_Click（盤面クリックで配置）

**目的**：次ミノ（未配置Done先頭）を、クリック列を中心に落下配置する

* 入力

  * クリックされた列 `col`
  * 次ミノ（未配置Done先頭の GrantedLengthN）と現在向き（IsVertical）
* DB（読み）

  * 次ミノ対象の Done（未配置先頭1件）
* 処理（配置ロジック）

  1. 未配置Doneが無ければ何もしない
  2. 置くミノの `N` と向き（横/縦）を確定
  3. 配置対象セル範囲を算出（**中心基準**）
  4. 下から上へ探索し、衝突しない最下段に配置
  5. 配置後、ライン消去→ `ClearedLines` を算出
* DB（書き）

  * MoveをINSERT（DoneIdと1:1で紐づけ）
* 出力（UI）

  * 盤面と統計と次ミノを更新
  * 最初は `LoadDoneAndMino()` 呼びでもOK

#### 中心基準：範囲計算（仕様として確定）

* **横向き（1×N）**

  * `left = col - (N-1)/2`（整数除算）
  * `right = left + N - 1`
  * 盤面外なら配置失敗（置けない）
* **縦向き（N×1）**

  * 列は `col` 固定、縦にNマス

> “中心”は見た目が自然。Nが偶数のときは左寄りになる（上式）で確定。

---

### 1.6 rotate_mino_right（右クリックで回転）

* 入力：なし
* 処理：次ミノの向きをトグル（横⇄縦）
* DB：MVPでは保存しない（再起動で戻るのは許容）
* 出力（UI）：次ミノ表示だけ更新（または再読み込み）

---

## 2. 追加画面（AddDoneWindow）：イベント一覧（確定）

### 2.1 SaveButton_Click（保存）

* 入力：テキスト（「。」区切りのDone）
* DB（書き）

  1. 新規BatchIdを採番（`MAX(BatchId)+1`）
  2. 入力を `。` で分割 → trim → 空要素除去
  3. 1要素ごとにDone INSERT

     * DoneDate = today
     * GrantedLengthN = ランダム決定（連続日数に応じてmaxN）
  4. Meta更新（継続日数・最終達成日）
* 出力（UI）

  * `DialogResult=true` で閉じる（Mainが再読み込み）

#### GrantedLengthN（確定ロジック例）

* `streak` に応じて `maxN` を決める（例）

  * 1〜2→2、3〜4→3、5〜6→4、7+→5
* `GrantedLengthN = Random(1..maxN)`

---

### 2.2 Cancel（キャンセル）

* 出力：`DialogResult=false` で閉じる

---

## 3. 今日Done表示画面（TodayDoneWindow）：イベント一覧（確定）

* 表示：今日のDone一覧（Text、日付、時刻など）
* MVP

  * 戻るボタン（Click）で閉じる
* Nice（余裕があれば）

  * ダブルクリックで編集画面（今回は後回し推奨）

---

## 4. DB最小設計
MVP完成後にSQLクエリを定数クラスでまとめることで処理が読みやすくなるかもしれない。現時点ではDbInitializeに書いておく。
### 4.1 Done

* `Id` INTEGER PRIMARY KEY AUTOINCREMENT 
* `BatchId` INTEGER NOT NULL 削除用：追加Doneのまとまり
* `DoneDate` TEXT NOT NULL（YYYY-MM-DD）やった日
* `Text` TEXT NOT NULL
* `CreatedAt` TEXT NOT NULL（ISO文字列）データを作った日時
* `GrantedLengthN` INTEGER NOT NULL（1〜5）ミノのサイズ

### 4.2 Move（1Done=1配置を保証）

* `Id` INTEGER PRIMARY KEY AUTOINCREMENT
* `DoneId` INTEGER NOT NULL UNIQUE（FK Done.Id）
* `PlacedAt` TEXT NOT NULL
* `Column` INTEGER NOT NULL（中心列）
* `StartRow` INTEGER NOT NULL（復元用に保存）
* `LengthN` INTEGER NOT NULL
* `IsVertical` INTEGER NOT NULL（0/1）
* `ClearedLines` INTEGER NOT NULL

> UNIQUE制約で「同じDoneを2回置く」バグをDB側で防ぐ。

### 4.3 Meta（Key-Value推奨）

* `Key` TEXT PRIMARY KEY
* `Value` TEXT NOT NULL

格納例：

* Key=`CurrentStreak` Value=`7`
* Key=`LastActiveDate` Value=`2026-01-14`

---

## 5. DBアクセス最小設計（Repository：確定）

イベントから逆算した、最低限のメソッド群。

### 5.1 DoneRepository

* `int GetNextBatchId()`
* `void AddDones(int batchId, DateOnly date, List<string> texts, Func<int> grantNFactory)`
* `List<Done> GetDonesByDate(DateOnly date)`
* `List<Done> GetAllDonesOrdered()`（Id or CreatedAt昇順）
* `int? GetLatestBatchId()`
* `void DeleteDoneBatch(int batchId)`（MoveがCASCADEならこれで足りる）
* `Done? GetOldestUnplacedDone()`（次ミノ用：Moveが無いDone先頭）

### 5.2 MoveRepository

* `List<Move> GetAllMovesOrdered()`
* `void AddMove(Move move)`
* `void DeleteMovesByDoneIds(IEnumerable<long> doneIds)`（CASCADEなし運用の場合）

### 5.3 MetaRepository

* `int GetCurrentStreak() / void SetCurrentStreak(int value)`
* `DateOnly? GetLastActiveDate() / void SetLastActiveDate(DateOnly date)`

---

## 6. イベント→Repository対応（確定）

* Main.LoadDoneAndMino
  → DoneRepo.GetAllDonesOrdered
  → MoveRepo.GetAllMovesOrdered
  → MetaRepo.Get…
  → DoneRepo.GetOldestUnplacedDone（次ミノ）
* AddDone.Save
  → DoneRepo.GetNextBatchId
  → DoneRepo.AddDones
  → MetaRepo.Set…
* Undo
  → DoneRepo.GetLatestBatchId
  → DoneRepo.DeleteDoneBatch（+ Move削除が必要ならMoveRepo）
* TetrisArea_Click
  → DoneRepo.GetOldestUnplacedDone
  → MoveRepo.AddMove
  → 再描画（LoadDoneAndMinoでも可）
* TodayDoneCount_Click
  → DoneRepo.GetDonesByDate

---

### 変更点や今後のためのメモ
ボタンを2つ作成する予定だったが無し。
右下に配置する予定であった追加ボタンはウィンドウの移動なく、そのまま入力して追加できるテキストボックスを配置。

本来左下に配置する予定であった削除ボタンは右下のテキストボックスに付随させる形で統合。本家テトリスらしく画面左端にはロゴ配置を予定。

DBアクセスのためのラッパーを作ってもいいかもしれない。