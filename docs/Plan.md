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
