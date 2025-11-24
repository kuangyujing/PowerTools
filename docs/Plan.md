# PowerTools 機能拡張計画

このドキュメントでは、Power Platformの制限事項を調査し、PowerToolsで補完可能な機能をまとめています。

## 目次
- [背景](#背景)
- [Power Platformの主な制限事項](#power-platformの主な制限事項)
- [機能一覧](#機能一覧)
- [実装ステータス](#実装ステータス)
- [参考情報](#参考情報)

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

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| 文字コード変換 | ネイティブサポートなし | `EncodingConverterController` | **実装済み** |
| Excelデータ抽出 | Excel Online必須、2,000行制限 | `ExcelController` | **実装済み** |
| 画像リサイズ/情報取得 | リサイズ/圧縮/変換がネイティブ不可 | `ImageController` | **実装済み** |
| ZIP圧縮/解凍 | 100ファイル制限、100MB制限、圧縮機能なし | `ZipController` | 未実装 |
| PDF処理 | AI Builder必要（有料）、50ページ以上で遅延 | `PdfController` | 未実装 |

#### 未実装機能の詳細

**ZIP圧縮/解凍**
- 現状: Extract archive to folder は100ファイル/100MB制限、圧縮機能なし
- 実装案: 複数ファイルのZIP圧縮（圧縮レベル指定可）、ZIPファイルの解凍、パスワード付きZIP対応

**PDF処理**
- 現状: テキスト抽出にAI Builderが必要、50ページ以上で処理が遅延
- 実装案: PDFからテキスト抽出、PDF結合/分割、HTML→PDF変換

---

### 2. データ変換

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| CSV解析 | ループ処理で非効率、大量データで制限超過 | `CsvController` | 未実装 |
| JSON/XML変換 | 配列や属性のサポートが限定的 | `DataConvertController` | 未実装 |
| HTMLテーブル生成 | スタイリングが困難、フォント問題 | `HtmlController` | 未実装 |

#### 未実装機能の詳細

**CSV解析**
- 現状: ループ処理が必要で非効率、100,000行以上で日次制限に到達
- 実装案: CSV⇔JSON高速変換、カスタム区切り文字対応、ヘッダー行の有無指定

**JSON/XML変換**
- 現状: JSON→XML変換で配列サポートが限定的、要素属性の扱いが困難
- 実装案: JSON⇔XML⇔CSV相互変換、XMLスキーマ検証

---

### 3. テキスト処理

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| 正規表現 | クラウドフローでネイティブサポートなし | `RegexController` | **実装済み** |
| 文字種カウント | 漢字・ひらがな・カタカナ等の分類カウント不可 | `CharacterCounterController` | **実装済み** |
| 文字列操作 | 基本的な操作のみ | `StringController` | 未実装 |

---

### 4. 暗号化・セキュリティ

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| ハッシュ生成 | Desktop版のみ、クラウドフローでは限定的 | `CryptoController` | 未実装 |
| 暗号化/復号化 | 限定的なサポート | `CryptoController` | 未実装 |
| Base64エンコード | 一部サポートあるが使いにくい | `EncodingController` | 未実装 |

#### 未実装機能の詳細

**ハッシュ・暗号化**
- 現状: クラウドフローでのハッシュ生成が困難、AES暗号化のネイティブサポートなし
- 実装案: ハッシュ生成（MD5, SHA1, SHA256, SHA512）、HMAC生成、AES暗号化/復号化、Base64エンコード/デコード

---

### 5. バーコード・QRコード

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| QRコード生成 | ネイティブ生成不可 | `BarcodeController` | **実装済み** |
| バーコード生成 | ネイティブ生成不可 | `BarcodeController` | **実装済み** |
| バーコード読み取り | モバイルのみ、Web非対応 | `BarcodeController` | 未実装 |

---

### 6. ドキュメント生成

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| Wordテンプレート | 10MB制限、画像リサイズ不可、リッチテキスト制限 | `DocumentController` | 未実装 |
| PDF生成 | サードパーティ必要 | `PdfController` | 未実装 |
| メールテンプレート | HTMLレンダリング問題、フォント挿入問題 | `HtmlController` | 未実装 |

---

### 7. 日時処理

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| タイムゾーン変換 | 過去のDST変更に非対応 | `DateTimeController` | 未実装 |
| 日付フォーマット | ISO 8601形式必須 | `DateTimeController` | 未実装 |
| 和暦変換 | サポートなし | `DateTimeController` | 未実装 |

#### 未実装機能の詳細

**日時処理**
- 現状: ISO 8601形式以外でエラー、過去の夏時間変更に非対応
- 実装案: 柔軟な日付フォーマット変換、タイムゾーン変換、和暦⇔西暦変換、営業日計算

---

### 8. 一時ストレージ

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| ファイルアップロード | Custom Connectorで大容量ファイル転送が困難 | `StorageController` | 未実装 |
| ファイル取得 | フロー間でのファイル共有が困難 | `StorageController` | 未実装 |
| ファイル削除 | 一時ファイルの自動クリーンアップなし | `StorageController` | 未実装 |

#### 未実装機能の詳細

**一時ストレージ**
- 現状: Custom Connectorの5秒タイムアウトで大容量ファイル転送が困難、フロー間でのファイル共有に外部ストレージが必要
- 実装案: ファイルアップロード（Base64またはマルチパート）、アクセスキー発行、有効期限付きストレージ（自動削除）
- ユースケース: 複数ステップの処理で中間ファイルを一時保存、Power AppsとPower Automate間でのファイル受け渡し

---

### 9. その他

| 機能 | Power Platformの制限 | PowerTools実装案 | ステータス |
|------|---------------------|------------------|-----------|
| 非同期ジョブ実行 | タイムアウト制限（5秒〜数分） | `JobsController` | 未実装 |
| バッチデータ処理 | ループ処理で制限超過しやすい | `BatchController` | 未実装 |
| 配列操作 | Canvas Appでのソート負荷が高い、集計が複雑 | `ArrayController` | 未実装 |
| JSON操作 | Pretty Print/Validate/JSONPath抽出が困難 | `JsonController` | 未実装 |
| 画像EXIF抽出 | 撮影日時・GPS等の取得不可 | `ImageController` | 未実装 |
| GUID/トークン生成 | UUID, NanoID等の生成が限定的 | `UtilController` | 未実装 |
| 外部APIプロキシ | APIキーの安全な管理が困難 | `ProxyController` | 未実装 |
| ログ・監査 | 操作ログ・例外ログの集約が困難 | `LogController` | 未実装 |

---

## 実装ステータス

| コントローラー | 機能 | ステータス |
|---------------|------|-----------|
| `ImageController` | 画像リサイズ/情報取得 | **実装済み** |
| `BarcodeController` | QRコード/バーコード生成 | **実装済み** |
| `ExcelController` | Excelデータ抽出 | **実装済み** |
| `RegexController` | 正規表現処理 | **実装済み** |
| `CharacterCounterController` | 文字種カウント | **実装済み** |
| `EncodingConverterController` | 文字コード変換 | **実装済み** |
| `HealthController` | ヘルスチェック | **実装済み** |
| `ZipController` | ZIP圧縮/解凍 | 未実装 |
| `CsvController` | CSV解析 | 未実装 |
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
| 2025-11-23 | RegexController 実装完了 |
| 2025-11-23 | BarcodeController 実装完了 |
| 2025-11-24 | ImageController, CharacterCounterController 実装完了 |
| 2025-11-24 | 優先度セクション削除、構造整理 |
