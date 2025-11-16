# ファイルエンコーディング変換API - Power Platform統合ガイド

このガイドでは、Power AppsとPower Automateからカスタムコネクタ経由でファイルエンコーディング変換APIを使用する方法を説明します。

## 目次
- [APIの概要](#apiの概要)
- [カスタムコネクタのセットアップ](#カスタムコネクタのセットアップ)
- [Power Appsでの実装](#power-appsでの実装)
- [Power Automateでの実装](#power-automateでの実装)
- [APIリファレンス](#apiリファレンス)

---

## APIの概要

エンコーディング変換APIは、テキストファイルの文字エンコーディングを別のエンコーディングに変換します。以下の機能をサポートしています：
- **自動エンコーディング検出** - 入力エンコーディングが指定されていない場合
- **Base64ベースのファイル転送** - Power Platformカスタムコネクタと互換性あり
- **複数のエンコーディング対応** - UTF-8、Shift_JIS、EUC-JP、ISO-2022-JPなど

### 主な機能
- 任意のプレーンテキストファイル（CSV、TXT、JSON、XML、HTMLなど）に対応
- 統計分析とBOM検出による自動エンコーディング検出
- 指定されたエンコーディングで同じフォーマットの変換済みファイルを返却
- 自動検出結果の信頼度スコアを提供

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
     - 名前: `PowerTools Encoding Converter`
     - ホスト: `your-api-domain.com`（テストの場合は `localhost:7XXX`）
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

### シナリオ1: ファイル変換とDataverseへの保存

この例では以下を実行します：
1. 添付ファイルコントロールを使用したファイルアップロード
2. ファイルエンコーディングの変換
3. 変換済みファイルをDataverseテーブルのファイル列に保存

#### アプリ構造

**コントロール：**
```
- FileInput1 (添付ファイルコントロール)
- ddOutputEncoding (ドロップダウン)
- btnConvert (ボタン)
- btnSave (ボタン)
- lblStatus (ラベル)
```

#### エンコーディング選択用ドロップダウンの設定

**ddOutputEncoding.Items:**
```javascript
Table(
    {Value: "UTF-8", DisplayName: "UTF-8"},
    {Value: "Shift_JIS", DisplayName: "Shift_JIS (日本語)"},
    {Value: "EUC-JP", DisplayName: "EUC-JP (日本語)"},
    {Value: "ISO-2022-JP", DisplayName: "ISO-2022-JP (日本語)"},
    {Value: "GB2312", DisplayName: "GB2312 (簡体字中国語)"},
    {Value: "Big5", DisplayName: "Big5 (繁体字中国語)"}
)
```

#### ファイル変換ボタン

**btnConvert.OnSelect:**
```javascript
// アップロードされたファイルを選択したエンコーディングに変換
Set(
    varConvertedFile,
    'PowerTools Encoding Converter'.ConvertFileEncoding({
        fileContentBase64: Base64(FileInput1.SelectedFile),
        fileName: FileInput1.SelectedFile.Name,
        outputEncoding: ddOutputEncoding.Selected.Value
        // inputEncodingはオプション - 省略すると自動検出
    })
);

// 変換結果を表示
If(
    !IsBlank(varConvertedFile),
    Set(
        lblStatus.Text,
        "変換成功！" & Char(10) &
        "検出: " & varConvertedFile.detectedEncoding & Char(10) &
        "出力: " & varConvertedFile.outputEncoding & Char(10) &
        "サイズ: " & Round(varConvertedFile.fileSizeBytes / 1024, 2) & " KB" &
        If(
            !IsBlank(varConvertedFile.detectionConfidence),
            Char(10) & "信頼度: " & Text(varConvertedFile.detectionConfidence * 100, "0.0") & "%",
            ""
        )
    );
    Notify("変換が正常に完了しました！", NotificationType.Success),
    Notify("変換に失敗しました！", NotificationType.Error)
);
```

#### Dataverseに保存するボタン

**btnSave.OnSelect:**
```javascript
// 変換済みファイルをDataverseレコードに保存
Patch(
    YourDataverseTable,
    LookUp(YourDataverseTable, ID = varRecordId),
    {
        FileColumn: {
            FileName: varConvertedFile.fileName,
            Value: varConvertedFile.fileContentBase64
        },
        Description: "変換: " & varConvertedFile.detectedEncoding &
                     " → " & varConvertedFile.outputEncoding
    }
);

Notify("ファイルをDataverseに保存しました！", NotificationType.Success);

// フォームをクリア
Reset(FileInput1);
Set(varConvertedFile, Blank());
```

### シナリオ2: 変換済みファイルのダウンロード

Dataverseに保存する代わりに変換済みファイルをダウンロードする場合：

**btnDownload.OnSelect:**
```javascript
// Download関数を使用（Power Appsの試験的機能が必要）
Download(
    varConvertedFile.fileContentBase64,
    varConvertedFile.fileName
);
```

### シナリオ3: 入力エンコーディングの指定

入力エンコーディングが既知で自動検出が不要な場合：

**btnConvert.OnSelect:**
```javascript
Set(
    varConvertedFile,
    'PowerTools Encoding Converter'.ConvertFileEncoding({
        fileContentBase64: Base64(FileInput1.SelectedFile),
        fileName: FileInput1.SelectedFile.Name,
        outputEncoding: ddOutputEncoding.Selected.Value,
        inputEncoding: "Shift_JIS"  // 入力エンコーディングを指定
    })
);
```

### Power Apps数式リファレンス

**サポートされているエンコーディング一覧を取得：**
```javascript
Set(
    varEncodings,
    'PowerTools Encoding Converter'.GetSupportedEncodings()
);

// ドロップダウンで使用
ddOutputEncoding.Items = varEncodings.encodings
```

**エラーハンドリング：**
```javascript
If(
    IsError('PowerTools Encoding Converter'.ConvertFileEncoding({...})),
    Notify(
        "エラー: " & FirstError.Message,
        NotificationType.Error
    ),
    // 成功時の処理
    Notify("成功しました！", NotificationType.Success)
);
```

---

## Power Automateでの実装

### シナリオ1: SharePointファイルの自動変換

このフローは、SharePointにアップロードされたファイルを自動的にUTF-8エンコーディングに変換します。

**トリガー:**
- **ファイルが作成されたとき (SharePoint)**
  - サイトのアドレス: `https://yoursite.sharepoint.com/sites/yoursite`
  - ライブラリ名: `Documents`

**アクション1: ファイル コンテンツの取得**
- **ファイル コンテンツの取得 (SharePoint)**
  - ファイル識別子: `@{triggerOutputs()?['body/{Identifier}']}`

**アクション2: エンコーディング変換**
- **PowerTools Encoding Converter - ConvertFileEncoding**
  - fileContentBase64: `@{base64(body('Get_file_content'))}`
  - fileName: `@{triggerOutputs()?['body/{FilenameWithExtension}']}`
  - outputEncoding: `UTF-8`
  - inputEncoding: _（自動検出の場合は空白のまま）_

**アクション3: 変換済みファイルの保存**
- **ファイルの作成 (SharePoint)**
  - サイトのアドレス: `https://yoursite.sharepoint.com/sites/yoursite`
  - フォルダー パス: `/Converted`
  - ファイル名: `@{body('PowerTools_Encoding_Converter_-_ConvertFileEncoding')?['fileName']}`
  - ファイル コンテンツ: `@{base64ToBinary(body('PowerTools_Encoding_Converter_-_ConvertFileEncoding')?['fileContentBase64'])}`

### シナリオ2: 変換してメール送信

**アクション: メールを送信（添付ファイル付き）**
```
宛先: user@example.com
件名: 変換済みファイル - @{body('ConvertFileEncoding')?['fileName']}
本文:
  ファイルが正常に変換されました！
  元のエンコーディング: @{body('ConvertFileEncoding')?['detectedEncoding']}
  新しいエンコーディング: @{body('ConvertFileEncoding')?['outputEncoding']}

添付ファイル:
  - 名前: @{body('ConvertFileEncoding')?['fileName']}
  - コンテンツ バイト: @{base64ToBinary(body('ConvertFileEncoding')?['fileContentBase64'])}
```

### シナリオ3: 複数ファイルの一括変換

**Apply to each（SharePointフォルダーのファイル）**
```
For each: @{body('Get_files_(properties_only)')?['value']}

  アクション: ファイル コンテンツの取得
    ファイル識別子: @{items('Apply_to_each')?['{Identifier}']}

  アクション: エンコーディング変換
    fileContentBase64: @{base64(body('Get_file_content'))}
    fileName: @{items('Apply_to_each')?['{FilenameWithExtension}']}
    outputEncoding: Shift_JIS

  アクション: ファイルの作成
    ファイル名: @{body('Convert_encoding')?['fileName']}
    ファイル コンテンツ: @{base64ToBinary(body('Convert_encoding')?['fileContentBase64'])}
```

### フローJSON例

```json
{
  "type": "OpenApiConnection",
  "inputs": {
    "host": {
      "connectionName": "shared_powertoolsencodingco_xxxxx",
      "operationId": "ConvertFileEncoding",
      "apiId": "/providers/Microsoft.PowerApps/apis/shared_powertoolsencodingco"
    },
    "parameters": {
      "body": {
        "fileContentBase64": "@{base64(body('Get_file_content'))}",
        "fileName": "@{triggerOutputs()?['body/{FilenameWithExtension}']}",
        "outputEncoding": "UTF-8"
      }
    }
  }
}
```

---

## APIリファレンス

### エンドポイント: ファイルエンコーディング変換

**リクエスト:**
```http
POST /api/encodingconverter/convert
Content-Type: application/json

{
  "fileContentBase64": "base64_encoded_file_content",
  "fileName": "example.csv",
  "outputEncoding": "UTF-8",
  "inputEncoding": "Shift_JIS"  // オプション
}
```

**レスポンス:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "fileContentBase64": "converted_base64_content",
  "fileName": "example.csv",
  "detectedEncoding": "shift_jis",
  "outputEncoding": "utf-8",
  "fileSizeBytes": 12345,
  "detectionConfidence": 0.95  // 自動検出時のみ
}
```

### エンドポイント: サポートされているエンコーディング一覧取得

**リクエスト:**
```http
GET /api/encodingconverter/encodings
```

**レスポンス:**
```json
{
  "encodings": [
    { "name": "UTF-8", "displayName": "UTF-8" },
    { "name": "Shift_JIS", "displayName": "Shift_JIS (Japanese)" },
    { "name": "EUC-JP", "displayName": "EUC-JP (Japanese)" },
    ...
  ]
}
```

### サポートされているエンコーディング

| エンコーディング | 説明 | 一般的な用途 |
|----------|-------------|------------|
| UTF-8 | Unicode (8ビット) | 現代の標準、Web |
| UTF-16 | Unicode (16ビット LE) | Windows、.NET |
| UTF-16BE | Unicode (16ビット BE) | Unix、Mac |
| Shift_JIS | 日本語 | レガシー日本語ファイル |
| EUC-JP | 日本語 | Unix日本語ファイル |
| ISO-2022-JP | 日本語 | メール（JIS） |
| GB2312 | 簡体字中国語 | 中国語ファイル |
| Big5 | 繁体字中国語 | 台湾、香港 |
| EUC-KR | 韓国語 | 韓国語ファイル |
| ISO-8859-1 | Latin-1 | 西ヨーロッパ |
| Windows-1252 | Windows Latin | Windows西ヨーロッパ |

### エラーレスポンス

**400 Bad Request:**
```json
{
  "error": "Invalid Base64 file content"
}
```

**400 Bad Request:**
```json
{
  "error": "Binary files are not supported. Only text files can be converted."
}
```

**400 Bad Request:**
```json
{
  "error": "Unsupported output encoding: XYZ"
}
```

**500 Internal Server Error:**
```json
{
  "error": "An error occurred during file conversion",
  "details": "Error message details"
}
```

---

## ベストプラクティス

### 1. ファイルサイズの考慮事項
- 10MBを超えるファイルの場合は、チャンク処理の使用を検討
- Base64エンコーディングによりペイロードサイズが約33%増加
- Power Automateはアクション入出力に100MBの制限あり

### 2. エンコーディング検出
- 自動検出は1KB以上のファイルで最良の結果を提供
- 重要なアプリケーションでは、既知の場合は入力エンコーディングを指定
- `detectionConfidence`を確認 - 0.8未満の値は信頼性が低い可能性

### 3. エラーハンドリング
- フロー/アプリには必ずエラーハンドリングを実装
- Power Appsでは`IsError()`を確認
- Power Automateでは「実行後の構成」を使用して失敗を処理

### 4. パフォーマンス
- サポートされているエンコーディングのリストをコレクションにキャッシュ
- Power Automateでのバッチ変換には並列処理を使用
- より良いパフォーマンスのために「Apply to each」の同時実行設定を検討

### 5. セキュリティ
- 変換前にファイルタイプを検証
- 機密データを扱う場合はAPIに認証を実装
- APIエンドポイントには環境変数を使用

---

## トラブルシューティング

### 問題: "Invalid Base64 file content"
**解決策:** Power Appsでは`Base64()`関数、Power Automateでは`base64()`式を使用していることを確認してください。

### 問題: "Binary files are not supported"
**解決策:** このAPIはテキストファイルのみをサポートしています。変換前にファイルタイプを確認してください。

### 問題: 検出信頼度が低い
**解決策:** 入力エンコーディングを明示的に指定するか、ファイルに正確な検出のための十分なコンテンツがあることを確認してください。

### 問題: カスタムコネクタがPower Appsに表示されない
**解決策:** カスタムコネクタ作成後に接続を作成したことを確認してください。

### 問題: "Could not execute request"
**解決策:** APIエンドポイントがアクセス可能で、認証が正しく設定されていることを確認してください。

---

## 追加リソース

- [Power Platformカスタムコネクタドキュメント](https://learn.microsoft.com/ja-jp/connectors/custom-connectors/)
- [Power Apps数式リファレンス](https://learn.microsoft.com/ja-jp/powerapps/maker/canvas-apps/formula-reference)
- [Power Automate式リファレンス](https://learn.microsoft.com/ja-jp/power-automate/use-expressions-in-conditions)

---

## サポート

問題や質問がある場合：
1. [PowerTools GitHubリポジトリ](https://github.com/yourorg/powertools)を確認
2. エラー詳細についてはAPIログを確認
3. コネクタの問題をトラブルシューティングする前に、Swagger UIを使用してAPIを直接テスト
