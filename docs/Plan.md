# PowerTools 機能拡張計画

このドキュメントでは、Power Platformの制限事項を調査し、PowerToolsで補完可能な機能をまとめています。

## 目次
- [背景](#背景)
- [Power Platformの主な制限事項](#power-platformの主な制限事項)
- [機能一覧](#機能一覧)
- [機能詳細](#機能詳細)
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

| カテゴリ | 機能名 | Power Platformの制限 | PowerTools実装案 | ステータス |
|---------|--------|---------------------|------------------|-----------|
| ファイル処理 | 文字コード変換 | ネイティブサポートなし | `EncodingConverterController` | **実装済み** |
| ファイル処理 | Excelデータ抽出 | Excel Online必須、2,000行制限 | `ExcelController` | **実装済み** |
| ファイル処理 | 画像リサイズ/情報取得 | リサイズ/圧縮/変換がネイティブ不可 | `ImageController` | **実装済み** |
| ファイル処理 | ZIP圧縮/解凍 | 100ファイル制限、100MB制限、圧縮機能なし | `ZipController` | 未実装 |
| ファイル処理 | PDF処理 | AI Builder必要（有料）、50ページ以上で遅延 | `PdfController` | 未実装 |
| データ変換 | CSV解析 | ループ処理で非効率、大量データで制限超過 | `CsvController` | 未実装 |
| データ変換 | JSON/XML変換 | 配列や属性のサポートが限定的 | `DataConvertController` | 未実装 |
| データ変換 | HTMLテーブル生成 | スタイリングが困難、フォント問題 | `HtmlController` | 未実装 |
| テキスト処理 | 正規表現 | クラウドフローでネイティブサポートなし | `RegexController` | **実装済み** |
| テキスト処理 | 文字種カウント | 漢字・ひらがな・カタカナ等の分類カウント不可 | `CharacterCounterController` | **実装済み** |
| テキスト処理 | 文字列操作 | 基本的な操作のみ | `StringController` | 未実装 |
| 暗号化・セキュリティ | ハッシュ生成 | Desktop版のみ、クラウドフローでは限定的 | `CryptoController` | 未実装 |
| 暗号化・セキュリティ | 暗号化/復号化 | 限定的なサポート | `CryptoController` | 未実装 |
| 暗号化・セキュリティ | Base64エンコード | 一部サポートあるが使いにくい | `EncodingController` | 未実装 |
| バーコード・QRコード | QRコード生成 | ネイティブ生成不可 | `BarcodeController` | **実装済み** |
| バーコード・QRコード | バーコード生成 | ネイティブ生成不可 | `BarcodeController` | **実装済み** |
| バーコード・QRコード | バーコード読み取り | モバイルのみ、Web非対応 | `BarcodeController` | 未実装 |
| ドキュメント生成 | Wordテンプレート | 10MB制限、画像リサイズ不可、リッチテキスト制限 | `DocumentController` | 未実装 |
| ドキュメント生成 | PDF生成 | サードパーティ必要 | `PdfController` | 未実装 |
| ドキュメント生成 | メールテンプレート | HTMLレンダリング問題、フォント挿入問題 | `HtmlController` | 未実装 |
| 日時処理 | タイムゾーン変換 | 過去のDST変更に非対応 | `DateTimeController` | 未実装 |
| 日時処理 | 日付フォーマット | ISO 8601形式必須 | `DateTimeController` | 未実装 |
| 日時処理 | 和暦変換 | サポートなし | `DateTimeController` | 未実装 |
| 一時ストレージ | ファイルアップロード | Custom Connectorで大容量ファイル転送が困難 | `StorageController` | **実装済み** |
| 一時ストレージ | ファイル取得 | フロー間でのファイル共有が困難 | `StorageController` | **実装済み** |
| 一時ストレージ | ファイル削除 | 一時ファイルの自動クリーンアップなし | `StorageController` | **実装済み** |
| その他 | 非同期ジョブ実行 | タイムアウト制限（5秒〜数分） | `JobsController` | 未実装 |
| その他 | バッチデータ処理 | ループ処理で制限超過しやすい | `BatchController` | 未実装 |
| その他 | 配列操作 | Canvas Appでのソート負荷が高い、集計が複雑 | `ArrayController` | 未実装 |
| その他 | JSON操作 | Pretty Print/Validate/JSONPath抽出が困難 | `JsonController` | 未実装 |
| その他 | 画像EXIF抽出 | 撮影日時・GPS等の取得不可 | `ImageController` | 未実装 |
| その他 | GUID/トークン生成 | UUID, NanoID等の生成が限定的 | `UtilController` | 未実装 |
| その他 | 外部APIプロキシ | APIキーの安全な管理が困難 | `ProxyController` | 未実装 |
| その他 | ログ・監査 | 操作ログ・例外ログの集約が困難 | `LogController` | 未実装 |

---

## 機能詳細

### ファイル処理

**文字コード変換** ✅
- ファイルの文字コードを検出・変換（UTF-8, Shift_JIS, EUC-JP等）

**Excelデータ抽出** ✅
- シート/範囲/テーブルからJSON形式でデータ抽出
- Excel Online不要、2,000行制限なし

**画像リサイズ/情報取得** ✅
- 画像のリサイズ、フォーマット変換（JPEG/PNG）
- 画像メタデータ（サイズ、形式）の取得

**ZIP圧縮/解凍**
- 複数ファイルのZIP圧縮（圧縮レベル指定可）
- ZIPファイルの解凍
- パスワード付きZIP対応

**PDF処理**
- PDFからテキスト抽出
- PDF結合/分割
- HTML→PDF変換

### データ変換

**CSV解析**
- CSV⇔JSON高速変換
- カスタム区切り文字対応
- ヘッダー行の有無指定

**JSON/XML変換**
- JSON⇔XML⇔CSV相互変換
- XMLスキーマ検証

**HTMLテーブル生成**
- データからHTMLテーブル生成
- カスタムスタイリング対応

### テキスト処理

**正規表現** ✅
- パターンマッチ、置換、分割
- ReDoS攻撃対策済み

**文字種カウント** ✅
- 漢字・ひらがな・カタカナ・英字・数字等の分類カウント

**文字列操作**
- 高度な文字列操作（トリム、パディング、ケース変換等）

### 暗号化・セキュリティ

**ハッシュ・暗号化**
- ハッシュ生成（MD5, SHA1, SHA256, SHA512）
- HMAC生成
- AES暗号化/復号化
- Base64エンコード/デコード

### バーコード・QRコード

**QRコード生成** ✅
- PNG/SVG形式で出力
- エラー訂正レベル指定可

**バーコード生成** ✅
- Code128, Code39, EAN13, EAN8, UPC_A, ITF, Codabar対応
- PNG/SVG形式で出力

**バーコード読み取り**
- 画像からバーコード/QRコードを読み取り

### ドキュメント生成

**Wordテンプレート**
- テンプレートへのデータ差し込み
- 画像挿入対応

**PDF生成**
- HTML→PDF変換
- テンプレートからPDF生成

**メールテンプレート**
- HTMLメールテンプレート生成
- インラインスタイル変換

### 日時処理

**日時処理**
- 柔軟な日付フォーマット変換
- タイムゾーン変換（DST対応）
- 和暦⇔西暦変換
- 営業日計算

### 一時ストレージ

**一時ストレージ**
- ファイルアップロード（Base64またはマルチパート）
- アクセスキー発行
- 有効期限付きストレージ（自動削除）
- ユースケース: 複数ステップの処理で中間ファイルを一時保存

### その他

**非同期ジョブ実行**
- 長時間処理のバックグラウンド実行
- ジョブステータス確認

**バッチデータ処理**
- 大量データの一括処理
- API呼び出し回数の削減

**配列操作**
- ソート、フィルタ、集計
- 配列の結合・分割

**JSON操作**
- Pretty Print、Validate
- JSONPath抽出

**画像EXIF抽出**
- 撮影日時、GPS情報等の取得

**GUID/トークン生成**
- UUID v4生成
- NanoID生成

**外部APIプロキシ**
- APIキーの安全な管理
- リクエスト/レスポンス変換

**ログ・監査**
- 操作ログ収集
- 例外ログ集約

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
| 2025-11-24 | 機能一覧と機能詳細に構造を再編成 |
| 2025-11-25 | StorageController 実装完了 |
