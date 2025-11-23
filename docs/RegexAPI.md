# Regex API ドキュメント

正規表現処理を提供するAPIです。Power Platform Cloud Flowsではネイティブサポートされていない正規表現機能を、Custom Connector経由で利用できます。

## 目次
- [概要](#概要)
- [エンドポイント一覧](#エンドポイント一覧)
- [共通オプション](#共通オプション)
- [API詳細](#api詳細)
  - [IsMatch - パターンマッチング判定](#ismatch---パターンマッチング判定)
  - [Match - 最初のマッチを取得](#match---最初のマッチを取得)
  - [Matches - すべてのマッチを取得](#matches---すべてのマッチを取得)
  - [Replace - 文字列置換](#replace---文字列置換)
  - [Split - 文字列分割](#split---文字列分割)
- [使用例](#使用例)
- [エラーハンドリング](#エラーハンドリング)
- [制限事項](#制限事項)

---

## 概要

### 背景
Power Platform Cloud Flowsでは正規表現がサポートされておらず、文字列からのパターン抽出や複雑な置換処理が困難です。このAPIはその制限を補完します。

### 機能
- **IsMatch**: 文字列がパターンにマッチするか判定
- **Match**: 最初にマッチした部分を取得（グループ含む）
- **Matches**: すべてのマッチを取得
- **Replace**: パターンにマッチした部分を置換
- **Split**: パターンで文字列を分割

---

## エンドポイント一覧

| メソッド | パス | 説明 |
|---------|------|------|
| POST | `/api/regex/ismatch` | パターンにマッチするか判定 |
| POST | `/api/regex/match` | 最初のマッチを取得 |
| POST | `/api/regex/matches` | すべてのマッチを取得 |
| POST | `/api/regex/replace` | パターンに一致する部分を置換 |
| POST | `/api/regex/split` | パターンで文字列を分割 |

---

## 共通オプション

すべてのエンドポイントで以下のオプションを指定できます。

```json
{
  "options": {
    "ignoreCase": true,
    "multiline": false,
    "singleline": false
  }
}
```

| オプション | 説明 | デフォルト |
|-----------|------|-----------|
| `ignoreCase` | 大文字小文字を区別しない | `false` |
| `multiline` | `^`と`$`が各行の先頭・末尾にマッチ | `false` |
| `singleline` | `.`が改行文字にもマッチ | `false` |

### オプションの詳細

#### ignoreCase
```
パターン: "hello"
入力: "Hello World"
ignoreCase=false → マッチしない
ignoreCase=true  → マッチする
```

#### multiline
```
パターン: "^Line"
入力: "Line 1\nLine 2\nLine 3"
multiline=false → 1件マッチ（文字列先頭のみ）
multiline=true  → 3件マッチ（各行の先頭）
```

#### singleline
```
パターン: "Start.+End"
入力: "Start\nMiddle\nEnd"
singleline=false → マッチしない（.は改行にマッチしない）
singleline=true  → マッチする（.が改行にもマッチ）
```

---

## API詳細

### IsMatch - パターンマッチング判定

文字列がパターンにマッチするかを判定します。

#### リクエスト
```http
POST /api/regex/ismatch
Content-Type: application/json
```

```json
{
  "input": "Contact: support@example.com",
  "pattern": "[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}",
  "options": {
    "ignoreCase": true
  }
}
```

| フィールド | 型 | 必須 | 説明 |
|-----------|-----|------|------|
| `input` | string | Yes | 検索対象の文字列 |
| `pattern` | string | Yes | 正規表現パターン |
| `options` | object | No | 正規表現オプション |

#### レスポンス
```json
{
  "isMatch": true
}
```

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `isMatch` | boolean | パターンにマッチした場合 `true` |

---

### Match - 最初のマッチを取得

最初にマッチした部分とキャプチャグループを取得します。

#### リクエスト
```http
POST /api/regex/match
Content-Type: application/json
```

```json
{
  "input": "2024-01-15",
  "pattern": "(?<year>\\d{4})-(?<month>\\d{2})-(?<day>\\d{2})"
}
```

#### レスポンス
```json
{
  "success": true,
  "value": "2024-01-15",
  "index": 0,
  "length": 10,
  "groups": [
    {
      "name": "0",
      "value": "2024-01-15",
      "success": true,
      "index": 0,
      "length": 10
    },
    {
      "name": "year",
      "value": "2024",
      "success": true,
      "index": 0,
      "length": 4
    },
    {
      "name": "month",
      "value": "01",
      "success": true,
      "index": 5,
      "length": 2
    },
    {
      "name": "day",
      "value": "15",
      "success": true,
      "index": 8,
      "length": 2
    }
  ]
}
```

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `success` | boolean | マッチした場合 `true` |
| `value` | string | マッチした文字列（マッチなしの場合 `null`） |
| `index` | number | マッチ開始位置 |
| `length` | number | マッチした文字列の長さ |
| `groups` | array | キャプチャグループの配列 |

#### グループオブジェクト

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `name` | string | グループ名（番号グループは "0", "1", ...） |
| `value` | string | キャプチャされた値 |
| `success` | boolean | グループがマッチした場合 `true` |
| `index` | number | キャプチャ開始位置 |
| `length` | number | キャプチャした文字列の長さ |

---

### Matches - すべてのマッチを取得

すべてのマッチを配列で取得します。

#### リクエスト
```http
POST /api/regex/matches
Content-Type: application/json
```

```json
{
  "input": "電話番号: 03-1234-5678, 090-9876-5432",
  "pattern": "\\d{2,3}-\\d{4}-\\d{4}"
}
```

#### レスポンス
```json
{
  "count": 2,
  "matches": [
    {
      "value": "03-1234-5678",
      "index": 6,
      "length": 12,
      "groups": [...]
    },
    {
      "value": "090-9876-5432",
      "index": 20,
      "length": 13,
      "groups": [...]
    }
  ]
}
```

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `count` | number | マッチ数 |
| `matches` | array | マッチオブジェクトの配列 |

---

### Replace - 文字列置換

パターンにマッチした部分を置換します。後方参照（`$1`, `$2`など）が使用可能です。

#### リクエスト
```http
POST /api/regex/replace
Content-Type: application/json
```

```json
{
  "input": "John Smith",
  "pattern": "(\\w+)\\s+(\\w+)",
  "replacement": "$2, $1"
}
```

| フィールド | 型 | 必須 | 説明 |
|-----------|-----|------|------|
| `input` | string | Yes | 置換対象の文字列 |
| `pattern` | string | Yes | 正規表現パターン |
| `replacement` | string | Yes | 置換文字列（後方参照可） |
| `options` | object | No | 正規表現オプション |

#### 置換文字列での後方参照

| 記法 | 説明 |
|------|------|
| `$0` | マッチ全体 |
| `$1`, `$2`, ... | 番号付きグループ |
| `${name}` | 名前付きグループ |

#### レスポンス
```json
{
  "result": "Smith, John",
  "replacementCount": 1
}
```

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `result` | string | 置換後の文字列 |
| `replacementCount` | number | 置換回数 |

---

### Split - 文字列分割

パターンで文字列を分割します。

#### リクエスト
```http
POST /api/regex/split
Content-Type: application/json
```

```json
{
  "input": "apple,banana;cherry:date",
  "pattern": "[,;:]"
}
```

#### レスポンス
```json
{
  "count": 4,
  "parts": ["apple", "banana", "cherry", "date"]
}
```

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `count` | number | 分割後の要素数 |
| `parts` | array | 分割された文字列の配列 |

---

## 使用例

### 例1: メールアドレスの検証

```json
// リクエスト
POST /api/regex/ismatch
{
  "input": "user@example.com",
  "pattern": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
}

// レスポンス
{
  "isMatch": true
}
```

### 例2: 日付フォーマットの変換（YYYY-MM-DD → DD/MM/YYYY）

```json
// リクエスト
POST /api/regex/replace
{
  "input": "Date: 2024-01-15",
  "pattern": "(\\d{4})-(\\d{2})-(\\d{2})",
  "replacement": "$3/$2/$1"
}

// レスポンス
{
  "result": "Date: 15/01/2024",
  "replacementCount": 1
}
```

### 例3: 電話番号の抽出

```json
// リクエスト
POST /api/regex/matches
{
  "input": "連絡先: 03-1234-5678 または 090-9876-5432",
  "pattern": "\\d{2,3}-\\d{4}-\\d{4}"
}

// レスポンス
{
  "count": 2,
  "matches": [
    { "value": "03-1234-5678", "index": 5, "length": 12, "groups": [...] },
    { "value": "090-9876-5432", "index": 22, "length": 13, "groups": [...] }
  ]
}
```

### 例4: HTMLタグの除去

```json
// リクエスト
POST /api/regex/replace
{
  "input": "<p>Hello <b>World</b></p>",
  "pattern": "<[^>]+>",
  "replacement": ""
}

// レスポンス
{
  "result": "Hello World",
  "replacementCount": 4
}
```

### 例5: CSVの分割（カンマ区切り）

```json
// リクエスト
POST /api/regex/split
{
  "input": "Name,Age,City,Country",
  "pattern": ","
}

// レスポンス
{
  "count": 4,
  "parts": ["Name", "Age", "City", "Country"]
}
```

### 例6: 名前付きグループでの日付解析

```json
// リクエスト
POST /api/regex/match
{
  "input": "Created: 2024-01-15",
  "pattern": "(?<year>\\d{4})-(?<month>\\d{2})-(?<day>\\d{2})"
}

// レスポンス
{
  "success": true,
  "value": "2024-01-15",
  "index": 9,
  "length": 10,
  "groups": [
    { "name": "0", "value": "2024-01-15", "success": true, "index": 9, "length": 10 },
    { "name": "year", "value": "2024", "success": true, "index": 9, "length": 4 },
    { "name": "month", "value": "01", "success": true, "index": 14, "length": 2 },
    { "name": "day", "value": "15", "success": true, "index": 17, "length": 2 }
  ]
}
```

### 例7: 複数行テキストでの行頭マッチ

```json
// リクエスト
POST /api/regex/matches
{
  "input": "Error: Something failed\nWarning: Check this\nError: Another issue",
  "pattern": "^Error:.+",
  "options": {
    "multiline": true
  }
}

// レスポンス
{
  "count": 2,
  "matches": [
    { "value": "Error: Something failed", ... },
    { "value": "Error: Another issue", ... }
  ]
}
```

---

## エラーハンドリング

### 400 Bad Request

#### 無効な正規表現パターン
```json
{
  "error": "Invalid regex pattern: parsing \"[invalid\" - Unterminated [] set."
}
```

#### 空のパターン
```json
{
  "error": "Pattern cannot be empty"
}
```

#### 入力サイズ超過
```json
{
  "error": "Input string exceeds maximum length of 1048576 characters"
}
```

#### タイムアウト
```json
{
  "error": "Regex operation timed out. The pattern may be too complex."
}
```

---

## 制限事項

### 入力サイズ
- 最大入力文字列長: **1MB（1,048,576文字）**

### タイムアウト
- 処理タイムアウト: **3秒**
- Custom Connectorの5秒制限を考慮した設定

### セキュリティ
- ReDoS（Regular Expression Denial of Service）対策として、タイムアウトと入力サイズ制限を設けています
- 複雑すぎるパターンや長すぎる入力は処理が中断されます

### 正規表現の構文
- .NET の `System.Text.RegularExpressions.Regex` クラスの構文に準拠
- ECMAScript互換モードではありません

---

## Power Automateでの使用例

### HTTP アクションでの呼び出し

```
メソッド: POST
URI: https://your-powertools-instance/api/regex/match
ヘッダー:
  Content-Type: application/json
本文:
{
  "input": "@{triggerBody()?['email']}",
  "pattern": "([^@]+)@(.+)"
}
```

### レスポンスの解析

```
// 式でグループの値を取得
body('HTTP')?['groups'][1]?['value']  // ローカル部分
body('HTTP')?['groups'][2]?['value']  // ドメイン部分
```

---

## 更新履歴

| 日付 | 内容 |
|------|------|
| 2025-11-23 | 初版作成 |
