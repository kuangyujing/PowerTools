# ◇ PowerTools で提供可能な API 一覧（候補表）

## ■ ファイル操作系

| 機能カテゴリ         | API名（案）                 | 説明                            | 主な用途                           |
| -------------- | ----------------------- | ----------------------------- | ------------------------------ |
| Excel 読取       | `excel/read-range`      | シート名＋レンジを指定してデータ抽出            | Excel Online 非依存のデータ取得         |
| Excel 読取（テーブル） | `excel/read-table`      | テーブル名＋範囲を指定して抽出               | 標準コネクタの弱点を補う                   |
| Excel 書き込み     | `excel/write-range`     | 範囲を指定して書き込み（Rows/Cells）       | Power Automate の Excel 書込み地獄回避 |
| テキスト文字コード変換    | `text/convert-encoding` | UTF-8, Shift_JIS, EUC-JP 等へ変換 | Power Platform では困難な処理         |
| テキスト抽出         | `text/grep`             | 指定文字列・正規表現で抽出                 | ログ・定型文検索                       |
| ZIP 解凍         | `zip/extract`           | ZIP → ディレクトリ構造(JSON)＋ファイル一覧   | バッチ処理前の解凍                      |
| ZIP 圧縮         | `zip/create`            | 複数ファイル → ZIP バイナリ             | 添付ファイルの一括生成                    |
| PDF 結合         | `pdf/merge`             | PDF 複数ファイルを結合                 | 帳票をまとめて出力                      |
| PDF ページ抽出      | `pdf/extract-pages`     | ページ範囲を指定して抽出                  | 長大 PDF の分割                     |
| PDF テキスト抽出     | `pdf/extract-text`      | PDF → テキスト                    | 解析・要約前処理                       |
| 画像リサイズ         | `image/resize`          | PNG/JPEG の縮小・切り抜き             | サムネイル生成                        |
| 画像EXIF抽出       | `image/exif`            | EXIF（撮影日時・GPS 等）取得            | 写真管理アプリ                        |

---

## ■ データ変換・テキスト処理

| カテゴリ       | API名（案）           | 説明                           | 主な用途                 |
| ---------- | ----------------- | ---------------------------- | -------------------- |
| 正規表現検索     | `regex/match`     | Pattern → Matches(JSON)      | 構造化されていないデータの抽出      |
| 正規表現置換     | `regex/replace`   | Pattern＋置換文字列                | Power Automate の式削減  |
| 配列ソート      | `array/sort`      | 数値・文字列・オブジェクトキー対応            | Canvas App のソート負荷削減  |
| 配列グループ化    | `array/group-by`  | 任意キーでグループ化                   | 集計・クロス集計の前処理         |
| 配列集計       | `array/summarize` | Sum/Avg/Max/Min/Count        | 数値集計を1 APIにオフロード     |
| JSON 整形    | `json/format`     | JSON Pretty Print / Validate | デバッグ用途               |
| JSON パス抽出  | `json/query`      | JSONPath で抽出                 | Power Automate の式簡略化 |
| XML → JSON | `xml/to-json`     | XML を JSON に変換               | 外部APIの XML を扱う       |
| JSON → XML | `json/to-xml`     | JSON を XML に変換               | SOAP 系連携の補助          |

---

## ■ 長時間処理／非同期処理

| カテゴリ      | API名（案）            | 説明                                | 主な用途                     |
| --------- | ------------------ | --------------------------------- | ------------------------ |
| 非同期ジョブ実行  | `jobs/start`       | 長時間処理を非同期で実行                      | Power Automate のタイムアウト回避 |
| ジョブ進捗取得   | `jobs/status/{id}` | 実行状況（Queued/Running/Done/Error）取得 | ポーリングで進捗判定               |
| バッチデータ処理  | `batch/process`    | リストを渡すとまとめて処理                     | 1件ずつのループ地獄回避             |
| 大量データ読み込み | `data/paginate`    | ページング済みデータ返却                      | デリゲーション制限回避              |

---

## ■ ビジネスロジック系（サーバ実装向き）

| カテゴリ     | API名（案）             | 説明              | 主な用途                   |
| -------- | ------------------- | --------------- | ---------------------- |
| 料金計算     | `logic/calc-price`  | 複雑な計算ロジックを委譲    | Power Apps では式が複雑化しやすい |
| スコアリング   | `logic/score`       | AI/統計モデルの推論呼び出し | モデルを外部呼び出し             |
| ルーティング   | `logic/route`       | 経路・スケジューリング     | 組合せ最適化系                |
| ビジネス規則評価 | `logic/rule-engine` | 条件セットを渡して結果判定   | ルール変更多い業務向け            |

---

## ■ ログ・監査・監視

| カテゴリ    | API名（案）         | 説明                 | 主な用途                   |
| ------- | --------------- | ------------------ | ---------------------- |
| ログ送信    | `log/write`     | Flow/App からログ送信    | 操作ログ・例外ログの集約           |
| 利用統計取得  | `metrics/usage` | API利用や操作回数を集計      | 運用可視化                  |
| ヘルスチェック | `system/health` | PowerTools 自体の死活監視 | Custom Connector の生存確認 |
| 監査トレイル  | `audit/record`  | 操作内容＋ユーザIDを記録      | 内部統制・ガバナンス強化           |

---

## ■ セキュリティ／ガバナンス補助

| カテゴリ        | API名（案）          | 説明                      | 主な用途            |
| ----------- | ---------------- | ----------------------- | --------------- |
| 外部 API プロキシ | `proxy/http`     | PowerTools だけが外部鍵を保持    | APIキー隠蔽         |
| IP 制限付きリレー  | `proxy/restrict` | Power Platform の固定IPで中継 | IP制限を突破する安全な構成  |
| 暗号化         | `crypto/encrypt` | テキスト/バイナリのAES暗号化        | 安全な保管           |
| 復号化         | `crypto/decrypt` | AES復号                   | PowerApps 側に鍵不要 |

---

## ■ その他ユーティリティ

| カテゴリ        | API名（案）               | 説明           | 主な用途               |
| ----------- | --------------------- | ------------ | ------------------ |
| HTML → PDF  | `convert/html-to-pdf` | HTMLの帳票をPDF化 | 見積書・注文書            |
| QRコード生成     | `barcode/qrcode`      | テキスト→QR画像    | 在庫・受付システム          |
| バーコード生成     | `barcode/code128`     | 各種バーコード生成    | ラベル発行              |
| GUID/トークン生成 | `util/generate-id`    | UUID, NanoID | 主キー生成              |
| 日付操作        | `date/calc`           | 月末計算・期間差分    | Power Automateの式削減 |
| Cron/スケジュール | `schedule/run`        | Server側で定期処理 | Flow のトリガー制限回避     |
