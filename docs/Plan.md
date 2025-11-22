# PowerTools 機能拡張計画

このドキュメントでは、Power Platformの制限事項を調査し、PowerToolsで補完可能な機能をまとめています。

## 目次
- [背景](#背景)
- [Power Platformの主な制限事項](#power-platformの主な制限事項)
- [機能一覧](#機能一覧)
- [実装優先度](#実装優先度)
- [実装ステータス](#実装ステータス)

---

## 背景

Power Platformは非エンジニアでも比較的簡単にローコードで業務アプリを作成できるソリューションです。しかし、その「簡単さ」は「小回りが効かない」「痒いところに手が届かない」という状況を生み出すこともあります。

PowerToolsは、Custom Connector経由で利用できるSwiss Army Knife（万能ツール）としてのWeb APIコレクションを提供し、Power Platformの制限を補完することを目的としています。

---

## Power Platformの主な制限事項

### API要求数の制限
- 1ユーザーあたり1日6,000回のAPI実行制限
- 1フローあたり5,000 APIリクエスト制限
- コネクタごとの制限（例：Forms は60秒あたり300要求）
- 制限超過時は処理が遅延、14日間超過でフロー無効化

### ファイルサイズの制限
- 標準コネクタ：100MB制限
- Custom Connector：5秒の実行時間制限
- インライン画像：100KB制限

### データ処理の制限
- データソースからの取得：最大2,000行
- 委任（Delegation）非対応の関数が多い
- ループ処理は非効率（大量データで制限超過）

### ライセンスの制限
- Custom Connectorの利用にはPremiumライセンスが必要
- AI Builderは追加ライセンスが必要

---

## 機能一覧

### 1. ファイル処理

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| 文字コード変換 | ネイティブサポートなし | `EncodingConverterController` | **実装済み** |
| Excelデータ抽出 | Excel Online必須、2,000行制限 | `ExcelController` | **実装済み** |
| ZIP圧縮/解凍 | 100ファイル制限、100MB制限、圧縮機能なし | `ZipController` | 高 |
| PDF処理 | AI Builder必要（有料）、50ページ以上で遅延 | `PdfController` | 中 |
| 画像処理 | リサイズ/圧縮/変換がネイティブ不可 | `ImageController` | 中 |

#### ZIP圧縮/解凍の詳細
- **現状の制限**
  - Extract archive to folder: 100ファイル制限
  - ファイルサイズ: 100MB制限
  - SharePoint方式: 圧縮なし（格納のみ）
- **実装機能案**
  - 複数ファイルのZIP圧縮（圧縮レベル指定可）
  - ZIPファイルの解凍
  - パスワード付きZIP対応

#### PDF処理の詳細
- **現状の制限**
  - テキスト抽出にAI Builderが必要
  - 50ページ以上で処理が遅延
  - 手書きテキストのOCR精度が低い
- **実装機能案**
  - PDFからテキスト抽出
  - PDF結合/分割
  - HTML→PDF変換

#### 画像処理の詳細
- **現状の制限**
  - リサイズ/圧縮のネイティブアクションなし
  - サードパーティコネクタ（Encodian、Cloudmersive等）が必要
- **実装機能案**
  - 画像リサイズ（幅/高さ指定）
  - 画像圧縮（品質指定）
  - フォーマット変換（PNG/JPEG/WebP等）

---

### 2. データ変換

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| CSV解析 | ループ処理で非効率、大量データで制限超過 | `CsvController` | 高 |
| JSON/XML変換 | 配列や属性のサポートが限定的 | `DataConvertController` | 中 |
| HTMLテーブル生成 | スタイリングが困難、フォント問題 | `HtmlController` | 低 |

#### CSV解析の詳細
- **現状の制限**
  - ループ処理が必要で非効率
  - 100,000行以上で日次制限に到達
  - 固定フォーマットのみ対応
- **実装機能案**
  - CSV→JSON高速変換
  - JSON→CSV変換
  - カスタム区切り文字対応
  - ヘッダー行の有無指定

#### JSON/XML変換の詳細
- **現状の制限**
  - JSON→XML変換で配列サポートが限定的
  - 要素属性の扱いが困難
- **実装機能案**
  - JSON⇔XML相互変換
  - JSON⇔CSV相互変換
  - XMLスキーマ検証

---

### 3. テキスト処理

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| 正規表現 | クラウドフローでネイティブサポートなし | `RegexController` | 高 |
| 文字列操作 | 基本的な操作のみ | `StringController` | 低 |

#### 正規表現の詳細
- **現状の制限**
  - Cloud Flowsで正規表現サポートなし
  - Power Automate Desktopのみ一部対応
  - Office Scriptsは1日1,600回制限
- **実装機能案**
  - パターンマッチング（IsMatch）
  - 文字列抽出（Match/Matches）
  - 文字列置換（Replace）
  - 文字列分割（Split）

---

### 4. 暗号化・セキュリティ

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| ハッシュ生成 | Desktop版のみ、クラウドフローでは限定的 | `CryptoController` | 中 |
| 暗号化/復号化 | 限定的なサポート | `CryptoController` | 中 |
| Base64エンコード | 一部サポートあるが使いにくい | `EncodingController` | 低 |

#### ハッシュ・暗号化の詳細
- **現状の制限**
  - クラウドフローでのハッシュ生成が困難
  - AES暗号化のネイティブサポートなし
- **実装機能案**
  - ハッシュ生成（MD5, SHA1, SHA256, SHA512）
  - HMAC生成
  - AES暗号化/復号化
  - Base64エンコード/デコード

---

### 5. バーコード・QRコード

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| QRコード生成 | ネイティブ生成不可 | `BarcodeController` | 高 |
| バーコード生成 | ネイティブ生成不可 | `BarcodeController` | 高 |
| バーコード読み取り | モバイルのみ、Web非対応 | `BarcodeController` | 中 |

#### バーコード・QRコードの詳細
- **現状の制限**
  - Power Appsで画像生成不可
  - QRコード生成にはサードパーティサービスが必要
  - バーコードスキャンはモバイルアプリのみ
- **実装機能案**
  - QRコード生成（PNG/SVG）
  - バーコード生成（Code128, Code39, EAN, UPC等）
  - 画像からQRコード/バーコード読み取り

---

### 6. ドキュメント生成

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| Wordテンプレート | 10MB制限、画像リサイズ不可、リッチテキスト制限 | `DocumentController` | 低 |
| PDF生成 | サードパーティ必要 | `PdfController` | 中 |
| メールテンプレート | HTMLレンダリング問題、フォント挿入問題 | `HtmlController` | 低 |

---

### 7. 日時処理

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| タイムゾーン変換 | 過去のDST変更に非対応 | `DateTimeController` | 低 |
| 日付フォーマット | ISO 8601形式必須 | `DateTimeController` | 低 |
| 和暦変換 | サポートなし | `DateTimeController` | 低 |

#### 日時処理の詳細
- **現状の制限**
  - ISO 8601形式以外でエラー
  - 過去の夏時間変更に非対応
  - カスタムタイムゾーン作成不可
- **実装機能案**
  - 柔軟な日付フォーマット変換
  - タイムゾーン変換
  - 和暦⇔西暦変換
  - 営業日計算

---

### 8. その他

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| 非同期ジョブ実行 | タイムアウト制限（5秒〜数分） | `JobsController` | 中 |
| ジョブ進捗取得 | 長時間処理の状態確認が困難 | `JobsController` | 中 |
| バッチデータ処理 | ループ処理で制限超過しやすい | `BatchController` | 中 |
| 大量データページング | デリゲーション制限（2,000行） | `DataController` | 中 |
| 配列ソート | Canvas Appでのソート負荷が高い | `ArrayController` | 低 |
| 配列グループ化 | 集計・クロス集計の前処理が困難 | `ArrayController` | 低 |
| 配列集計 | Sum/Avg/Max/Min/Countの一括処理不可 | `ArrayController` | 低 |
| JSON整形 | Pretty Print/Validateが困難 | `JsonController` | 低 |
| JSONパス抽出 | JSONPathでの抽出が式で複雑化 | `JsonController` | 低 |
| テキスト抽出(grep) | 正規表現検索が困難 | `TextController` | 低 |
| 画像EXIF抽出 | 撮影日時・GPS等の取得不可 | `ImageController` | 低 |
| GUID/トークン生成 | UUID, NanoID等の生成が限定的 | `UtilController` | 低 |
| Cron/スケジュール実行 | Flowのトリガー制限 | `ScheduleController` | 低 |
| 外部APIプロキシ | APIキーの安全な管理が困難 | `ProxyController` | 低 |
| IP制限付きリレー | Power Platformの固定IP制限突破 | `ProxyController` | 低 |
| ログ送信 | 操作ログ・例外ログの集約が困難 | `LogController` | 低 |
| 利用統計取得 | API利用や操作回数の集計が困難 | `MetricsController` | 低 |
| 監査トレイル | 操作内容＋ユーザIDの記録が困難 | `AuditController` | 低 |
| 料金計算 | 複雑な計算ロジックで式が複雑化 | `LogicController` | 低 |
| スコアリング | AI/統計モデルの推論呼び出し不可 | `LogicController` | 低 |
| ルーティング | 経路・スケジューリングの組合せ最適化が困難 | `LogicController` | 低 |
| ビジネス規則評価 | 条件セットによる結果判定が複雑化 | `LogicController` | 低 |

#### 非同期処理・バッチ処理の詳細
- **現状の制限**
  - Custom Connectorは5秒でタイムアウト
  - Power Automateのフロー実行も数分でタイムアウト
  - 大量データのループ処理でAPI制限に到達
  - デリゲーション非対応で2,000行制限
- **実装機能案**
  - 非同期ジョブ開始（即座にジョブIDを返却）
  - ジョブ状態取得（Queued/Running/Done/Error）
  - バッチ処理（リストを渡してまとめて処理）
  - ページング付きデータ取得

#### 配列操作の詳細
- **現状の制限**
  - Canvas Appでの大量データソートはパフォーマンス低下
  - グループ化や集計は複雑な式が必要
  - Power Automateでの配列操作は限定的
- **実装機能案**
  - 配列ソート（数値・文字列・オブジェクトキー対応）
  - 配列グループ化（任意キーでグループ化）
  - 配列集計（Sum/Avg/Max/Min/Count）

#### JSON操作の詳細
- **現状の制限**
  - JSONの整形・検証機能がない
  - 複雑なJSONからの値抽出は式が複雑化
  - JSONPathのようなクエリ言語が使えない
- **実装機能案**
  - JSON整形（Pretty Print）
  - JSON検証（Validate）
  - JSONPath抽出（$.store.book[0].title形式）

#### テキスト抽出の詳細
- **現状の制限**
  - ログファイルからの特定行抽出が困難
  - 正規表現による複数行マッチが不可
- **実装機能案**
  - 文字列・正規表現による行抽出
  - 複数パターンによるフィルタリング
  - 前後の行も含めた抽出（コンテキスト取得）

#### 画像EXIF抽出の詳細
- **現状の制限**
  - 画像メタデータの取得がネイティブ不可
  - 撮影日時やGPS情報の抽出にサードパーティが必要
- **実装機能案**
  - EXIF情報取得（撮影日時、カメラ機種、設定等）
  - GPS情報取得（緯度・経度）
  - 画像サイズ・フォーマット情報取得

#### ユーティリティの詳細
- **現状の制限**
  - GUID生成は可能だがNanoID等は不可
  - 定期実行はRecurrenceトリガーのみで柔軟性が低い
- **実装機能案**
  - UUID v4生成
  - NanoID生成（カスタム長・文字セット）
  - Cron式による定期実行（サーバーサイド）

#### プロキシの詳細
- **現状の制限**
  - 外部APIのキーをPower Platform側で保持する必要がある
  - IP制限のある外部APIへのアクセスが困難
- **実装機能案**
  - 外部APIプロキシ（PowerToolsがAPIキーを保持）
  - IP制限付きリレー（固定IPからのアクセスを中継）
  - リクエスト/レスポンスの変換

#### ログ・監査の詳細
- **現状の制限**
  - フロー/アプリの操作ログを集約する仕組みがない
  - API利用統計の取得が困難
  - 監査証跡の記録に外部システムが必要
- **実装機能案**
  - ログ送信（レベル、メッセージ、コンテキスト）
  - 利用統計取得（API呼び出し回数、エラー率等）
  - 監査トレイル記録（操作内容、ユーザID、タイムスタンプ）

#### ビジネスロジックの詳細
- **現状の制限**
  - 複雑な計算ロジックはPower Appsの式で表現困難
  - AI/MLモデルの呼び出しにAI Builderが必要
  - 組合せ最適化やルールエンジンのネイティブサポートなし
- **実装機能案**
  - 料金計算（複雑な料金体系の計算）
  - スコアリング（外部モデルの推論呼び出し）
  - ルーティング（経路最適化、スケジューリング）
  - ビジネス規則評価（条件セットによる判定）

---

### 9. 一時ストレージ

| 機能 | Power Platformの制限 | PowerTools実装案 | 優先度 |
|------|---------------------|------------------|--------|
| ファイルアップロード | Custom Connectorで大容量ファイル転送が困難 | `StorageController` | 中 |
| ファイル取得 | フロー間でのファイル共有が困難 | `StorageController` | 中 |
| ファイル削除 | 一時ファイルの自動クリーンアップなし | `StorageController` | 中 |

#### 一時ストレージの詳細
- **現状の制限**
  - Custom Connectorの5秒タイムアウトで大容量ファイル転送が困難
  - フロー間やアプリ間でのファイル共有に外部ストレージが必要
  - SharePointやOneDriveを一時領域として使うと管理が煩雑
- **実装機能案**
  - ファイルアップロード（Base64またはマルチパート）
  - アクセスキー発行（一意のキーでファイルを識別）
  - アクセスキーによるファイル取得
  - 有効期限付きストレージ（自動削除）
  - ファイル削除（明示的な削除）
- **ユースケース**
  - 複数ステップの処理で中間ファイルを一時保存
  - 大容量ファイルを分割アップロード後に結合
  - Power AppsとPower Automate間でのファイル受け渡し

---

## 実装優先度

### 高優先度
1. **ZIP圧縮/解凍** - 100ファイル/100MB制限の回避、圧縮機能の提供
2. **正規表現処理** - クラウドフローで最も要望が多い機能
3. **CSV高速パース** - ループ処理回避によるパフォーマンス改善
4. **QRコード/バーコード生成** - ネイティブ生成不可の解消

### 中優先度
5. **画像リサイズ/圧縮** - サードパーティコネクタ依存の解消
6. **PDF処理** - AI Builder不要の軽量代替
7. **ハッシュ/暗号化** - セキュリティ要件対応
8. **JSON/XML/CSV相互変換** - データ統合の簡素化

### 低優先度
9. **HTMLテンプレート生成** - メール本文生成用
10. **日時処理** - 和暦変換、柔軟なフォーマット
11. **Wordテンプレート処理** - 制限の回避

---

## 実装ステータス

| コントローラー | 機能 | ステータス |
|---------------|------|-----------|
| `EncodingConverterController` | 文字コード変換 | 実装済み |
| `ExcelController` | Excelデータ抽出 | 実装済み |
| `HealthController` | ヘルスチェック | 実装済み |
| `ZipController` | ZIP圧縮/解凍 | 未実装 |
| `RegexController` | 正規表現処理 | 未実装 |
| `CsvController` | CSV解析 | 未実装 |
| `BarcodeController` | QRコード/バーコード | 未実装 |
| `ImageController` | 画像処理 | 未実装 |
| `PdfController` | PDF処理 | 未実装 |
| `CryptoController` | 暗号化/ハッシュ | 未実装 |
| `DataConvertController` | データ変換 | 未実装 |
| `DateTimeController` | 日時処理 | 未実装 |
| `StorageController` | 一時ストレージ | 未実装 |

---

## 参考情報

### Power Platformの公式制限
- [自動化フローの制限事項 - Microsoft Learn](https://learn.microsoft.com/ja-jp/power-automate/limits-and-config)
- [要求の制限と割り当て - Microsoft Learn](https://learn.microsoft.com/ja-jp/power-platform/admin/api-request-limits-allocations)

### Custom Connectorの制限
- 実行時間: 5秒
- OpenAPI: 2.0形式のみ（3.0は変換が必要）
- ライセンス: Premium以上が必要

---

## 更新履歴

| 日付 | 内容 |
|------|------|
| 2024-11-23 | 初版作成 |
