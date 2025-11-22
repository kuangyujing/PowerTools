# 画像リサイズAPI - Power Platform統合ガイド

このガイドでは、Power AppsとPower Automateからカスタムコネクタ経由で画像リサイズAPIを使用する方法を説明します。

## 目次
- [APIの概要](#apiの概要)
- [カスタムコネクタのセットアップ](#カスタムコネクタのセットアップ)
- [Power Appsでの実装](#power-appsでの実装)
- [Power Automateでの実装](#power-automateでの実装)
- [APIリファレンス](#apiリファレンス)

---

## APIの概要

画像リサイズAPIは、JPEGおよびPNG画像のリサイズを行います。以下の機能をサポートしています：
- **アスペクト比維持** - 幅または高さのみ指定時に自動計算
- **Base64ベースのファイル転送** - Power Platformカスタムコネクタと互換性あり
- **フォーマット変換** - JPEGとPNG間の相互変換
- **品質調整** - JPEG出力時の品質を1-100で指定可能

### 主な機能
- JPEG、PNG画像のリサイズ
- アスペクト比を維持した縮小・拡大
- 幅のみ、高さのみ、または両方を指定可能
- 出力フォーマットの変換（JPEG⇔PNG）
- JPEG品質の調整
- 画像メタデータ（サイズ、フォーマット）の取得

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
5. コネクタを設定：
   - **全般タブ**:
     - 名前: `PowerTools Image Resizer`
     - ホスト: `your-api-domain.com`
     - ベースURL: `/`
   - **セキュリティタブ**:
     - 認証の種類: `認証なし`（必要に応じて設定）
   - **定義タブ**:
     - アクションはOpenAPIから自動インポートされます
   - **テストタブ**:
     - 接続を作成してAPIをテスト

6. **コネクタの作成** をクリック

### ステップ3: 接続の作成

1. コネクタ作成後、**+ 新しい接続** をクリック
2. 認証を完了（設定されている場合）
3. Power AppsとPower Automateで使用可能になります

---

## Power Appsでの実装

### シナリオ1: 画像アップロードとリサイズ

この例では以下を実行します：
1. カメラまたはファイル選択で画像を取得
2. 指定サイズにリサイズ
3. リサイズ結果を表示

#### アプリ構造

**コントロール：**
```
- AddMediaButton1 (メディアの追加ボタン)
- Image1 (画像コントロール - 元画像表示用)
- Image2 (画像コントロール - リサイズ後表示用)
- txtWidth (テキスト入力 - 幅指定)
- txtHeight (テキスト入力 - 高さ指定)
- btnResize (ボタン)
- lblStatus (ラベル)
```

#### 変数の初期化

**App.OnStart:**
```javascript
Set(varResizedImage, Blank());
Set(varOriginalImage, Blank());
Set(varStatus, "");
```

#### 画像選択時の処理

**AddMediaButton1.OnChange:**
```javascript
Set(varOriginalImage, AddMediaButton1.Media);
Set(varStatus, "画像が選択されました")
```

#### リサイズ処理

**btnResize.OnSelect:**
```javascript
// 入力検証
If(
    IsBlank(varOriginalImage),
    Set(varStatus, "画像を選択してください");
    Return()
);

If(
    IsBlank(txtWidth.Text) && IsBlank(txtHeight.Text),
    Set(varStatus, "幅または高さを指定してください");
    Return()
);

// リサイズAPIを呼び出し
Set(varStatus, "リサイズ中...");

Set(
    varResizeResult,
    PowerToolsImageResizer.Resize({
        FileContentBase64: JSON(varOriginalImage, JSONFormat.IncludeBinaryData),
        Width: If(!IsBlank(txtWidth.Text), Value(txtWidth.Text), Blank()),
        Height: If(!IsBlank(txtHeight.Text), Value(txtHeight.Text), Blank()),
        MaintainAspectRatio: true,
        Quality: 85
    })
);

// 結果を設定
Set(
    varResizedImage,
    "data:image/" & varResizeResult.Format & ";base64," & varResizeResult.FileContentBase64
);

Set(
    varStatus,
    "リサイズ完了: " & varResizeResult.Width & "x" & varResizeResult.Height &
    " (" & Round(varResizeResult.FileSizeBytes / 1024, 1) & " KB)"
)
```

#### 画像表示設定

**Image1.Image:**
```javascript
varOriginalImage
```

**Image2.Image:**
```javascript
varResizedImage
```

### シナリオ2: サムネイル生成とDataverse保存

#### サムネイル生成処理

**btnCreateThumbnail.OnSelect:**
```javascript
// サムネイル生成（幅200pxに縮小）
Set(
    varThumbnailResult,
    PowerToolsImageResizer.Resize({
        FileContentBase64: JSON(varOriginalImage, JSONFormat.IncludeBinaryData),
        Width: 200,
        MaintainAspectRatio: true,
        OutputFormat: "jpeg",
        Quality: 75
    })
);

// Dataverseに保存
Patch(
    Images,
    Defaults(Images),
    {
        Name: "thumbnail_" & Text(Now(), "yyyymmddhhmmss"),
        ImageData: varThumbnailResult.FileContentBase64
    }
);

Set(varStatus, "サムネイルを保存しました")
```

---

## Power Automateでの実装

### シナリオ1: SharePointの画像を自動リサイズ

このフローは、SharePointフォルダに新しい画像がアップロードされたとき、自動的にリサイズしてサムネイルフォルダに保存します。

#### フロー構成

```
トリガー: SharePoint - ファイルが作成されたとき
↓
アクション: SharePoint - ファイルコンテンツの取得
↓
アクション: 作成 (Base64変換)
↓
アクション: PowerTools - Resize
↓
アクション: SharePoint - ファイルの作成
```

#### 詳細設定

**1. トリガー設定**
- サイト: 対象のSharePointサイト
- フォルダー: `/Shared Documents/Images`

**2. ファイルコンテンツの取得**
- サイト: トリガーと同じ
- ファイル識別子: `@{triggerOutputs()?['body/{Identifier}']}`

**3. Base64変換（作成アクション）**
```javascript
base64(body('ファイルコンテンツの取得'))
```

**4. PowerTools Resize呼び出し**
```json
{
  "FileContentBase64": "@{outputs('作成')}",
  "Width": 800,
  "MaintainAspectRatio": true,
  "OutputFormat": "jpeg",
  "Quality": 80
}
```

**5. サムネイルの保存**
- サイト: 対象のSharePointサイト
- フォルダー: `/Shared Documents/Thumbnails`
- ファイル名: `thumb_@{triggerOutputs()?['body/{Name}']}`
- ファイルコンテンツ: `@{base64ToBinary(body('Resize')?['FileContentBase64'])}`

### シナリオ2: メール添付画像の一括リサイズ

#### フロー構成

```
トリガー: Outlook - 新しいメールが届いたとき
↓
条件: 添付ファイルあり
↓
Apply to each: 添付ファイルごとに処理
  ↓
  条件: 画像ファイルか確認
  ↓
  アクション: PowerTools - Resize
  ↓
  アクション: OneDrive - ファイルの作成
```

#### 画像ファイル判定条件

```javascript
@or(
    endsWith(items('Apply_to_each')?['Name'], '.jpg'),
    endsWith(items('Apply_to_each')?['Name'], '.jpeg'),
    endsWith(items('Apply_to_each')?['Name'], '.png')
)
```

### シナリオ3: Forms回答の画像処理

Microsoft Formsで受け取った画像を処理するフロー例：

```
トリガー: Microsoft Forms - 新しい応答が送信されるとき
↓
アクション: Microsoft Forms - 応答の詳細を取得する
↓
アクション: PowerTools - Info (画像情報取得)
↓
条件: 画像サイズが1920px以上か
↓
はいの場合: PowerTools - Resize (1920pxに縮小)
↓
アクション: SharePoint - ファイルの作成
```

---

## APIリファレンス

### POST /api/Image/resize

画像をリサイズします。

#### リクエスト

```json
{
  "FileContentBase64": "string (必須)",
  "Width": "integer (任意)",
  "Height": "integer (任意)",
  "MaintainAspectRatio": "boolean (既定: true)",
  "OutputFormat": "string (任意: 'jpeg' または 'png')",
  "Quality": "integer (既定: 85, 範囲: 1-100)"
}
```

#### パラメータ説明

| パラメータ | 型 | 必須 | 説明 |
|-----------|------|------|------|
| FileContentBase64 | string | ○ | Base64エンコードされた画像データ（JPEGまたはPNG） |
| Width | integer | △ | 目標幅（ピクセル）。WidthまたはHeightの少なくとも一方は必須 |
| Height | integer | △ | 目標高さ（ピクセル）。WidthまたはHeightの少なくとも一方は必須 |
| MaintainAspectRatio | boolean | - | アスペクト比を維持するか。両方の寸法指定時に有効。既定: true |
| OutputFormat | string | - | 出力フォーマット（'jpeg' または 'png'）。未指定時は入力と同じ |
| Quality | integer | - | JPEG品質（1-100）。PNG出力時は無視。既定: 85 |

#### レスポンス

```json
{
  "FileContentBase64": "string",
  "Format": "string",
  "Width": "integer",
  "Height": "integer",
  "FileSizeBytes": "integer"
}
```

#### レスポンス例

```json
{
  "FileContentBase64": "/9j/4AAQSkZJRgABAQAAAQABAAD...",
  "Format": "jpeg",
  "Width": 800,
  "Height": 600,
  "FileSizeBytes": 45230
}
```

#### サイズ計算の動作

| Width | Height | MaintainAspectRatio | 動作 |
|-------|--------|---------------------|------|
| 指定 | 未指定 | - | 高さは自動計算（アスペクト比維持） |
| 未指定 | 指定 | - | 幅は自動計算（アスペクト比維持） |
| 指定 | 指定 | true | 指定サイズ内に収まるよう縮小（アスペクト比維持） |
| 指定 | 指定 | false | 指定サイズに強制リサイズ（歪む可能性あり） |

---

### POST /api/Image/info

画像のメタデータを取得します。

#### リクエスト

```json
{
  "FileContentBase64": "string (必須)"
}
```

#### レスポンス

```json
{
  "Format": "string",
  "Width": "integer",
  "Height": "integer",
  "FileSizeBytes": "integer"
}
```

#### レスポンス例

```json
{
  "Format": "png",
  "Width": 1920,
  "Height": 1080,
  "FileSizeBytes": 2458624
}
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
| 400 | File content cannot be empty | FileContentBase64に値を設定 |
| 400 | Invalid Base64 file content | 正しいBase64エンコーディングか確認 |
| 400 | At least one of Width or Height must be specified | WidthまたはHeightを指定 |
| 400 | Width must be a positive integer | 正の整数を指定 |
| 400 | Height must be a positive integer | 正の整数を指定 |
| 400 | Unsupported image format | JPEG/PNG形式の画像を使用 |
| 400 | Output format must be 'jpeg' or 'png' | OutputFormatを修正 |
| 500 | An error occurred while resizing the image | サーバーログを確認 |

### Power Automateでのエラーハンドリング

```
スコープ: Try
  ↓
  アクション: PowerTools - Resize
↓
スコープ: Catch (Configure run after: has failed)
  ↓
  アクション: 作成 - エラー情報
  値: @{outputs('Resize')?['body']?['error']}
  ↓
  アクション: 通知の送信
```

---

## 制限事項

- **対応フォーマット**: JPEG、PNGのみ
- **最大ファイルサイズ**: サーバー設定に依存（既定: 約28MB - ASP.NET Core既定値）
- **出力サイズ**: メモリ制限に依存

## パフォーマンス考慮事項

- 大きな画像のリサイズには時間がかかる場合があります
- Power Automateのタイムアウト（既定120秒）に注意
- 大量の画像処理には非同期処理パターンを検討

---

## サンプルコード

### C# (HttpClient)

```csharp
using System.Net.Http.Json;

var client = new HttpClient();
var request = new
{
    FileContentBase64 = Convert.ToBase64String(imageBytes),
    Width = 800,
    MaintainAspectRatio = true,
    Quality = 85
};

var response = await client.PostAsJsonAsync(
    "https://your-api-domain.com/api/Image/resize",
    request
);

var result = await response.Content.ReadFromJsonAsync<ImageResizeResponse>();
```

### JavaScript (Fetch)

```javascript
const response = await fetch('https://your-api-domain.com/api/Image/resize', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        FileContentBase64: base64ImageData,
        Width: 800,
        MaintainAspectRatio: true,
        Quality: 85
    })
});

const result = await response.json();
console.log(`Resized: ${result.Width}x${result.Height}`);
```

### PowerShell

```powershell
$imageBytes = [System.IO.File]::ReadAllBytes("C:\path\to\image.jpg")
$base64 = [Convert]::ToBase64String($imageBytes)

$body = @{
    FileContentBase64 = $base64
    Width = 800
    MaintainAspectRatio = $true
    Quality = 85
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://your-api-domain.com/api/Image/resize" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

# 保存
$resizedBytes = [Convert]::FromBase64String($response.FileContentBase64)
[System.IO.File]::WriteAllBytes("C:\path\to\resized.jpg", $resizedBytes)
```
