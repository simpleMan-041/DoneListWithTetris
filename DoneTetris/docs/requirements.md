# DoneTetris 要件定義（実装準拠・確定版）

## 0. 前提

* **アプリ概要**
  * 1日のDone（やったこと）を記録すると、テトリス盤面にミノを1つ配置できる
  * Done管理とゲーム進行は常にDBの履歴から再構築される

* **次ミノの選び方**
  * 未配置Done（Moveが存在しないDone）の **先頭（最も古い）1件**

* **ミノ配置基準**
  * クリックした列を **中心** として配置（中心揃え）

* **Done削除と盤面**
  * Doneを削除すると、それに対応するMoveも削除される
  * 盤面は **Move履歴から再構築** されるため、常に整合が保たれる

* **今日判定**
  * ローカル日付（`DateTime.Now.Date`）
  * 0:00 で日付切替

* **ミノ仕様**
  * 棒型のみ
  * 長さ `N = 1..maxN`
  * `maxN` は継続日数（CurrentStreak）により決定
  * 方向：横（1×N）/縦（N×1）
  * 右クリックで回転（縦横切替）

---

## 1. MainWindow：イベント一覧

### 1.1 LoadDoneAndMino（起動時・再描画）

**目的**  
DBから状態を復元し、統計・次ミノ・盤面を描画する  
※ 画面更新はすべてこのメソッドに集約する

**DB（読み）**

* Done：全件（Id昇順）
* Move：全件（Id昇順）
* Meta：CurrentStreak / LastActiveDate

**処理**

1. 継続日数（Meta）を読み込み
2. 盤面を空で初期化
3. Moveを順に適用して盤面を再構築（行消去含む）
4. スコア = `sum(Move.ClearedLines)`
5. 今日Done数 / 総Done数 / 継続日数を算出
6. 未配置Doneを抽出
7. 次ミノ = 未配置Doneの先頭1件の `GrantedLengthN`

**UI更新**

* 盤面描画
* スコア / 今日Done / 総Done / 継続日数
* 次ミノ表示（長さ・向き）

---

### 1.2 Done入力（右下テキストボックス）

**入力方式**

* 右下に **常設の入力欄**
* 「。」区切りで複数Done入力可能
* **Enterキーで保存**
* 改行は不可（1行入力）
* 表示は折り返し

**処理**

1. 空入力チェック
   * 空の場合：警告表示＋入力欄を赤点滅
2. `。` で分割 → trim → 空要素除去
3. 新規BatchIdを採番
4. 継続日数（Meta）を更新
5. 更新後の継続日数を元に `maxN` を決定
6. DoneをINSERT（各Doneに GrantedLengthN 付与）
7. 入力欄クリア
8. `LoadDoneAndMino()` 実行

**UI演出**

* 成功：入力欄の枠が一瞬緑
* 失敗：入力欄の枠が赤

---

### 1.3 今日Done一覧（右下 ListView）

* 今日のDoneを常時表示
* 各行にチェックボックスを持つ
* Textは折り返し表示

---

### 1.4 選択削除（右下）

**目的**  
チェックされたDoneを任意に削除する

**処理**

1. チェックされたDoneIdを取得
2. 確認ダイアログ表示
3. Doneを削除（FK CASCADE により Move も削除）
4. `LoadDoneAndMino()` 実行

**仕様**

* 未配置 / 配置済み Done のどちらも削除可能
* 削除後、盤面は自動で巻き戻る

---

### 1.5 TetrisArea_Click（盤面クリック）

**目的**  
次ミノをクリック列中心で落下配置する

**入力**

* クリック列
* 次ミノ（未配置Done先頭）
* 向き（横/縦）

**処理**

1. 未配置Doneが無ければ何もしない
2. ミノの長さと向きを確定
3. 中心基準で配置範囲を計算
4. 下から上へ探索し、配置可能な最下段を探す
5. 仮配置 → 行消去数算出
6. MoveをINSERT（DoneIdと1:1）
7. `LoadDoneAndMino()` 実行

#### 中心基準計算

* 横（1×N）
  * `left = col - (N-1)/2`
  * `right = left + N - 1`
* 縦（N×1）
  * 列固定、縦にNマス

---

### 1.6 右クリック回転

* 右クリックで次ミノの向きを切替
* 状態はメモリ保持（DBには保存しない）
* 表示のみ更新

---

## 2. 継続日数（Meta）仕様（本実装）

* 達成日 = その日にDoneが1件以上追加された日
* 同日に何回追加しても +1 されない

**更新ルール**

* LastActiveDate == 今日  
  → 変化なし
* LastActiveDate == 昨日  
  → CurrentStreak +1
* それ以外  
  → CurrentStreak = 1

**起動時補正**

* LastActiveDate が 今日/昨日 以外なら
  * CurrentStreak = 0

---

## 3. DBスキーマ（実装準拠）

### 3.1 Done

| 列名 | 型 | 説明 |
|----|----|----|
| Id | INTEGER PK | |
| BatchId | INTEGER | 追加操作の単位 |
| DoneDate | TEXT | YYYY-MM-DD |
| Text | TEXT | Done内容 |
| CreatedAt | TEXT | 作成日時 |
| GrantedLengthN | INTEGER | ミノ長さ（1〜5） |

---

### 3.2 Move

| 列名 | 型 | 説明 |
|----|----|----|
| Id | INTEGER PK | |
| DoneId | INTEGER UNIQUE | Doneと1:1 |
| PlacedAt | TEXT | 配置日時 |
| Column | INTEGER | 中心列 |
| StartRow | INTEGER | 上端行 |
| LengthN | INTEGER | ミノ長さ |
| IsVertical | INTEGER | 0/1 |
| ClearedLines | INTEGER | 消去行数 |

* FK: Done(Id) ON DELETE CASCADE

---

### 3.3 Meta

| Key | Value |
|----|----|
| CurrentStreak | int |
| LastActiveDate | yyyy-MM-dd |

---

## 4. Repository（最小構成）

### DoneRepository
* GetNextBatchId
* AddDones
* GetAllDonesOrdered
* GetDonesByDate
* GetOldestUnplacedDone
* DeleteDoneByIds

### MoveRepository
* GetAllMovesOrdered
* AddMove

### MetaRepository
* Get / Set
* GetInt / SetInt

---

## 5. 設計メモ（将来向け）

* UIイベントは MainWindow に集約
* 状態は保持せず、必ず DB → 再構築
* Board / Streak を Service として分離可能
* 演出系（枠色点滅・行消去エフェクト）は後付け前提

## 6. 将来的にアップデートを行うとしたら
* 行がそろったときに音声・エフェクトを入れる。
* listview内で日付検索が出来るようにする。
* ミノの形状を本家にそろえる
* スコアが一定量に到達するごとに演出が入る。

