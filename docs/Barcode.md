# バーコード・QRコードAPI - Power Platform統合ガイド

このガイドでは、Power AppsとPower Automateからカスタムコネクタ経由でバーコード・QRコード生成APIを使用する方法を説明します。

## 目次
- [APIの概要](#apiの概要)
- [カスタムコネクタのセットアップ](#カスタムコネクタのセットアップ)
- [Power Appsでの実装](#power-appsでの実装)
- [Power Automateでの実装](#power-automateでの実装)
- [APIリファレンス](#apiリファレンス)

---

## APIの概要

バーコード・QRコードAPIは、各種バーコードとQRコードの画像を生成します。以下の機能をサポートしています：

### QRコード生成
- **サイズ指定** - 50〜2000ピクセルの正方形画像
- **誤り訂正レベル** - L/M/Q/Hの4段階
- **出力フォーマット** - PNG/SVG対応
- **Data URI対応** - 直接HTMLに埋め込み可能

### バーコード生成
- **複数フォーマット対応** - Code128, Code39, EAN-13, EAN-8, UPC-A, ITF, Codabar
- **サイズ指定** - 幅・高さを個別指定可能
- **テキスト表示** - バーコード下のテキスト表示ON/OFF
- **出力フォーマット** - PNG/SVG対応
- **Data URI対応** - 直接HTMLに埋め込み可能

---

## カスタムコネクタのセットアップ

### ステップ1: OpenAPI定義のエクスポート

1. PowerTools APIをローカルで実行します：
   ```bash
   dotnet run --project PowerTools.Server
   ```

2. Swagger UIにアクセスします：
   ```
   https://localhost:7XXX/swagger
   ```

3. OpenAPI JSON定義をダウンロードします：
   ```
   https://localhost:7XXX/swagger/v1/swagger.json
   ```

### ステップ2: Power Platformでカスタムコネクタを作成

1. [Power Apps](https://make.powerapps.com) または [Power Automate](https://make.powerautomate.com) にアクセス
2. **データ** → **カスタムコネクタ** に移動
3. **+ 新しいカスタムコネクタ** → **OpenAPIファイルをインポート** をクリック
4. ダウンロードした `swagger.json` ファイルをアップロード
5. コネクタを設定し、**コネクタの作成** をクリック

---

## Power Appsでの実装

### シナリオ1: QRコード生成と表示

この例では、テキスト入力からQRコードを生成して表示します。

#### アプリ構造

**コントロール：**
```
- txtQrContent (テキスト入力 - QRコードの内容)
- ddQrSize (ドロップダウン - サイズ選択)
- btnGenerateQr (ボタン)
- imgQrCode (画像コントロール - QRコード表示用)
- lblStatus (ラベル)
```

#### QRコード生成処理

**btnGenerateQr.OnSelect:**
```javascript
// 入力検証
If(
    IsBlank(txtQrContent.Text),
    Set(varStatus, "内容を入力してください");
    Return()
);

// QRコード生成APIを呼び出し
Set(varStatus, "生成中...");

Set(
    varQrResult,
    PowerToolsBarcode.GenerateQrCode({
        Content: txtQrContent.Text,
        Size: Value(ddQrSize.Selected.Value),
        Format: "png",
        ErrorCorrectionLevel: "M"
    })
);

// Data URIを使って直接画像を表示
Set(varQrImage, varQrResult.DataUri);
Set(varStatus, "QRコード生成完了: " & varQrResult.Width & "x" & varQrResult.Height)
```

#### 画像表示設定

**imgQrCode.Image:**
```javascript
varQrImage
```

### シナリオ2: バーコード生成（商品コード）

#### バーコード生成処理

**btnGenerateBarcode.OnSelect:**
```javascript
// EAN-13バーコード生成
Set(
    varBarcodeResult,
    PowerToolsBarcode.GenerateBarcode({
        Content: txtProductCode.Text,
        BarcodeType: "EAN13",
        Width: 300,
        Height: 100,
        Format: "png",
        ShowText: true
    })
);

// Data URIで直接表示
Set(varBarcodeImage, varBarcodeResult.DataUri)
```

### シナリオ3: QRコードをSharePointに保存

```javascript
// QRコード生成
Set(
    varQrResult,
    PowerToolsBarcode.GenerateQrCode({
        Content: "https://example.com/product/" & txtProductId.Text,
        Size: 400,
        Format: "png"
    })
);

// SharePointに保存（Power Automateフロー経由）
PowerAutomateFlow.Run({
    FileName: "qr_" & txtProductId.Text & ".png",
    FileContent: varQrResult.ImageBase64
})
```

---

## Power Automateでの実装

### シナリオ1: 商品登録時にQRコードを自動生成

#### フロー構成

```
トリガー: Dataverse - 行が追加されたとき（Products テーブル）
↓
アクション: PowerTools - GenerateQrCode
↓
アクション: SharePoint - ファイルの作成
↓
アクション: Dataverse - 行を更新（QRコードURL設定）
```

#### 詳細設定

**1. トリガー設定**
- テーブル名: Products
- スコープ: Organization

**2. QRコード生成**
```json
{
  "Content": "https://yoursite.com/products/@{triggerOutputs()?['body/productid']}",
  "Size": 300,
  "Format": "png",
  "ErrorCorrectionLevel": "M"
}
```

**3. SharePointにファイル作成**
- サイト: 対象のSharePointサイト
- フォルダー: `/Shared Documents/QRCodes`
- ファイル名: `qr_@{triggerOutputs()?['body/productid']}.png`
- ファイルコンテンツ: `@{base64ToBinary(body('GenerateQrCode')?['ImageBase64'])}`

### シナリオ2: 在庫ラベル用バーコード一括生成

#### フロー構成

```
トリガー: 手動トリガー（ボタンフロー）
↓
アクション: Excel Online - テーブル内に存在する行を一覧表示
↓
Apply to each: 各商品に対して
  ↓
  アクション: PowerTools - GenerateBarcode
  ↓
  アクション: OneDrive - ファイルの作成
```

#### バーコード生成設定

```json
{
  "Content": "@{items('Apply_to_each')?['ProductCode']}",
  "BarcodeType": "Code128",
  "Width": 300,
  "Height": 100,
  "Format": "png",
  "ShowText": true
}
```

### シナリオ3: イベント参加チケットQRコード

```
トリガー: Microsoft Forms - 新しい応答が送信されるとき
↓
アクション: Microsoft Forms - 応答の詳細を取得する
↓
アクション: 作成 - チケットID生成
値: @{guid()}
↓
アクション: PowerTools - GenerateQrCode
↓
アクション: Outlook - メールの送信
  - 本文にData URIを直接埋め込み
```

**メール本文でのQRコード表示:**
```html
<p>参加チケットのQRコードです：</p>
<img src="@{body('GenerateQrCode')?['DataUri']}" alt="チケットQRコード" />
```

---

## APIリファレンス

### POST /api/Barcode/qrcode

QRコードを生成します。

#### リクエスト

```json
{
  "Content": "string (必須)",
  "Size": "integer (既定: 200)",
  "Format": "string (既定: 'png')",
  "ErrorCorrectionLevel": "string (既定: 'M')"
}
```

#### パラメータ説明

| パラメータ | 型 | 必須 | 説明 |
|-----------|------|------|------|
| Content | string | ○ | QRコードにエンコードする内容（URL、テキスト等） |
| Size | integer | - | 画像サイズ（ピクセル、50〜2000）。既定: 200 |
| Format | string | - | 出力フォーマット（'png' または 'svg'）。既定: 'png' |
| ErrorCorrectionLevel | string | - | 誤り訂正レベル（'L', 'M', 'Q', 'H'）。既定: 'M' |

**誤り訂正レベルの説明：**

| レベル | 訂正能力 | 用途 |
|--------|---------|------|
| L | 約7% | データ量を最大化したい場合 |
| M | 約15% | 標準的な用途（既定値） |
| Q | 約25% | やや汚れる可能性がある場合 |
| H | 約30% | ロゴ埋め込みや汚れやすい環境 |

#### レスポンス

```json
{
  "ImageBase64": "string",
  "DataUri": "string",
  "Format": "string",
  "Width": "integer",
  "Height": "integer",
  "FileSizeBytes": "integer"
}
```

#### レスポンスフィールド説明

| フィールド | 型 | 説明 |
|-----------|------|------|
| ImageBase64 | string | Base64エンコードされた画像データ |
| DataUri | string | Data URI形式（例: `data:image/png;base64,...`）HTMLのimg srcに直接使用可能 |
| Format | string | 出力フォーマット（'png' または 'svg'） |
| Width | integer | 画像幅（ピクセル） |
| Height | integer | 画像高さ（ピクセル） |
| FileSizeBytes | integer | ファイルサイズ（バイト） |

#### レスポンス例

```json
{
  "ImageBase64": "iVBORw0KGgoAAAANSUhEUgAAAMgAAADI...",
  "DataUri": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAADI...",
  "Format": "png",
  "Width": 200,
  "Height": 200,
  "FileSizeBytes": 1245
}
```

---

### POST /api/Barcode/barcode

バーコードを生成します。

#### リクエスト

```json
{
  "Content": "string (必須)",
  "BarcodeType": "string (必須)",
  "Width": "integer (既定: 300)",
  "Height": "integer (既定: 100)",
  "Format": "string (既定: 'png')",
  "ShowText": "boolean (既定: true)"
}
```

#### パラメータ説明

| パラメータ | 型 | 必須 | 説明 |
|-----------|------|------|------|
| Content | string | ○ | バーコードにエンコードする内容 |
| BarcodeType | string | ○ | バーコード形式（下表参照） |
| Width | integer | - | 画像幅（ピクセル、50〜2000）。既定: 300 |
| Height | integer | - | 画像高さ（ピクセル、30〜500）。既定: 100 |
| Format | string | - | 出力フォーマット（'png' または 'svg'）。既定: 'png' |
| ShowText | boolean | - | バーコード下にテキストを表示するか。既定: true |

**対応バーコード形式：**

| BarcodeType | 説明 | 内容の制約 |
|-------------|------|-----------|
| Code128 | 汎用バーコード | ASCII文字（英数字、記号） |
| Code39 | 工業用バーコード | 大文字英字、数字、一部記号 |
| EAN13 | JANコード（13桁） | 12〜13桁の数字 |
| EAN8 | 短縮JANコード（8桁） | 7〜8桁の数字 |
| UPC_A | 米国商品コード | 11〜12桁の数字 |
| ITF | 物流用バーコード | 偶数桁の数字 |
| Codabar | 医療・図書館用 | 数字、一部記号、A-D |

#### レスポンス

```json
{
  "ImageBase64": "string",
  "DataUri": "string",
  "Format": "string",
  "Width": "integer",
  "Height": "integer",
  "BarcodeType": "string",
  "FileSizeBytes": "integer"
}
```

#### レスポンス例

```json
{
  "ImageBase64": "iVBORw0KGgoAAAANSUhEUgAAASwAAABk...",
  "DataUri": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAASwAAABk...",
  "Format": "png",
  "Width": 300,
  "Height": 100,
  "BarcodeType": "Code128",
  "FileSizeBytes": 2156
}
```

---

### GET /api/Barcode/types

対応しているバーコード形式の一覧を取得します。

#### レスポンス

```json
["Code128", "Code39", "EAN13", "EAN8", "UPC_A", "ITF", "Codabar"]
```

---

## Data URIの活用

### HTMLでの直接埋め込み

レスポンスの `DataUri` フィールドを使用すると、画像を直接HTMLに埋め込むことができます：

```html
<img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAADI..." alt="QRコード" />
```

### Power Appsでの利用

```javascript
// Image コントロールの Image プロパティに直接設定
varQrResult.DataUri
```

### Power Automateでのメール埋め込み

```html
<img src="@{body('GenerateQrCode')?['DataUri']}" alt="QRコード" />
```

### SVG形式のData URI

SVGフォーマットを指定した場合のData URI：
```
data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIC...
```

---

## エラーハンドリング

### エラーレスポンス形式

```json
{
  "error": "エラーメッセージ"
}
```

### 一般的なエラー

| HTTPステータス | エラー内容 | 対処方法 |
|---------------|-----------|----------|
| 400 | Content cannot be empty | Contentに値を設定 |
| 400 | BarcodeType is required | BarcodeTypeを指定 |
| 400 | Unsupported barcode type | 対応形式を使用（/api/Barcode/typesで確認） |
| 400 | Format must be 'png' or 'svg' | Formatを修正 |
| 400 | ErrorCorrectionLevel must be 'L', 'M', 'Q', or 'H' | 有効な値を指定 |
| 400 | EAN-13 requires exactly 12 or 13 digits | 正しい桁数の数字を指定 |
| 400 | ITF requires an even number of digits | 偶数桁の数字を指定 |
| 500 | An error occurred while generating | サーバーログを確認 |

---

## サンプルコード

### C# (HttpClient)

```csharp
using System.Net.Http.Json;

var client = new HttpClient();

// QRコード生成
var qrRequest = new
{
    Content = "https://example.com",
    Size = 300,
    Format = "png",
    ErrorCorrectionLevel = "M"
};

var qrResponse = await client.PostAsJsonAsync(
    "https://your-api-domain.com/api/Barcode/qrcode",
    qrRequest
);

var qrResult = await qrResponse.Content.ReadFromJsonAsync<QrCodeResponse>();

// Data URIを直接HTMLに使用
Console.WriteLine($"<img src=\"{qrResult.DataUri}\" />");

// バーコード生成
var barcodeRequest = new
{
    Content = "4901234567890",
    BarcodeType = "EAN13",
    Width = 300,
    Height = 100
};

var barcodeResponse = await client.PostAsJsonAsync(
    "https://your-api-domain.com/api/Barcode/barcode",
    barcodeRequest
);

var barcodeResult = await barcodeResponse.Content.ReadFromJsonAsync<BarcodeResponse>();
```

### JavaScript (Fetch)

```javascript
// QRコード生成
const qrResponse = await fetch('https://your-api-domain.com/api/Barcode/qrcode', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        Content: 'https://example.com',
        Size: 300,
        Format: 'png'
    })
});

const qrResult = await qrResponse.json();

// Data URIで直接画像を表示
document.getElementById('qrImage').src = qrResult.DataUri;

// バーコード生成
const barcodeResponse = await fetch('https://your-api-domain.com/api/Barcode/barcode', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        Content: 'ABC-12345',
        BarcodeType: 'Code128',
        Width: 300,
        Height: 100
    })
});

const barcodeResult = await barcodeResponse.json();
document.getElementById('barcodeImage').src = barcodeResult.DataUri;
```

### PowerShell

```powershell
# QRコード生成
$qrBody = @{
    Content = "https://example.com"
    Size = 300
    Format = "png"
} | ConvertTo-Json

$qrResponse = Invoke-RestMethod -Uri "https://your-api-domain.com/api/Barcode/qrcode" `
    -Method Post `
    -ContentType "application/json" `
    -Body $qrBody

# ファイルに保存
$qrBytes = [Convert]::FromBase64String($qrResponse.ImageBase64)
[System.IO.File]::WriteAllBytes("C:\path\to\qrcode.png", $qrBytes)

# バーコード生成
$barcodeBody = @{
    Content = "4901234567890"
    BarcodeType = "EAN13"
    Width = 300
    Height = 100
} | ConvertTo-Json

$barcodeResponse = Invoke-RestMethod -Uri "https://your-api-domain.com/api/Barcode/barcode" `
    -Method Post `
    -ContentType "application/json" `
    -Body $barcodeBody

# ファイルに保存
$barcodeBytes = [Convert]::FromBase64String($barcodeResponse.ImageBase64)
[System.IO.File]::WriteAllBytes("C:\path\to\barcode.png", $barcodeBytes)
```

---

## 制限事項

- **QRコードサイズ**: 50〜2000ピクセル
- **バーコード幅**: 50〜2000ピクセル
- **バーコード高さ**: 30〜500ピクセル
- **出力フォーマット**: PNG、SVGのみ
- **Content最大長**: バーコード形式により異なる

## パフォーマンス考慮事項

- 大きなサイズ（1000px以上）の生成には時間がかかる場合があります
- SVG形式は解像度に依存しないため、印刷用途に適しています
- PNG形式は固定解像度のため、表示用途に適しています
