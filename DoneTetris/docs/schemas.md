## 1週間開発向け：実装順チェックリスト（MVP）

この順でやると、戻り作業が最小になる（依存が少ない順）。

### 0. 土台

* [ ] WPFプロジェクト作成、`docs/SPEC.txt` を配置
* [ ] 盤面サイズ、セルサイズ、定数クラス（`AppConstants`）作成

### 1. DB（最優先）

* [ ] SQLiteファイルの置き場所決定（例：`AppData` or 実行フォルダ）
* [ ] `CREATE TABLE` を実装（Done / Move / Meta）
* [ ] DB初期化（起動時にテーブルが無ければ作る）

### 2. Model（データ構造）

* [ ] `Done` クラス（Id, BatchId, DoneDate, Text, CreatedAt, GrantedLengthN）
* [ ] `Move` クラス（Id, DoneId, PlacedAt, Column, StartRow, LengthN, IsVertical, ClearedLines）

### 3. Repository（DBアクセス最小）

* [ ] `DoneRepository`（GetNextBatchId, AddDones, GetDonesByDate, GetAllDonesOrdered, GetLatestBatchId, DeleteDoneBatch, GetOldestUnplacedDone）
* [ ] `MoveRepository`（GetAllMovesOrdered, AddMove, DeleteMovesByDoneIds ※CASCADE次第）
* [ ] `MetaRepository`（Get/Set CurrentStreak, LastActiveDate）

### 4. “盤面の純ロジック”（UI抜きで完成させる）

* [ ] `Board`（2次元bool配列など）を作る
* [ ] 配置判定

  * [ ] 中心基準の横ミノ範囲計算（left/right）
  * [ ] 縦ミノ配置判定
* [ ] 落下探索（最下段を探す）
* [ ] ライン消去（消えた行数返す）
* [ ] Move適用で盤面再構築（Move一覧→盤面）

> ここまでできると、Undoや再起動復元がほぼ確定で動く。

### 5. MainWindow（まずは表示だけ）

* [ ] `LoadDoneAndMino()` 実装（DB→再構築→表示用データ作成）
* [ ] 統計表示（総Done/今日Done/スコア/継続日数）
* [ ] 次ミノ表示（未配置Done先頭の GrantedLengthN）

### 6. AddDoneWindow（Done追加）

* [ ] 画面作成（入力欄＋保存/キャンセル）
* [ ] `SaveButton_Click`

  * [ ] 「。」分割→Done INSERT（BatchId付与、GrantedLengthN付与）
  * [ ] Meta更新（継続日数/最終達成日）
  * [ ] `DialogResult=true`
* [ ] MainWindow側 `AddButton_Click` で呼び出し→再読み込み

### 7. 配置操作（テトリスの核）

* [ ] 盤面クリックで Column取得
* [ ] 次ミノ（未配置Done先頭）取得
* [ ] 配置→Move INSERT→再読み込み

### 8. Undo（直近追加取り消し）

* [ ] ConfirmDeleteWindow 作成（はい/いいえ）
* [ ] 最新BatchId取得→Done削除（＋Move削除）→再読み込み

### 9. TodayDoneWindow（今日一覧）

* [ ] 今日Done取得→ListView表示
* [ ] 戻る

### 10. 仕上げ（最低限）

* [ ] 例外処理（DBが壊れてても落ちにくく）
* [ ] UI微調整（ボタン無効化：未配置ミノが無い時など）
* [ ] 動作確認シナリオ（起動→追加→配置→Undo→再起動復元）