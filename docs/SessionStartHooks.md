# SessionStart Hooks - Claude Code環境セットアップガイド

このガイドでは、Claude CodeのSessionStart Hooksを使用して、.NET開発環境を自動的にセットアップする方法を説明します。

## 目次
- [SessionStart Hooksとは](#sessionstart-hooksとは)
- [仕組みと特徴](#仕組みと特徴)
- [dotnetのインストール設定](#dotnetのインストール設定)
- [設定ファイルの詳細](#設定ファイルの詳細)
- [トラブルシューティング](#トラブルシューティング)
- [その他のユースケース](#その他のユースケース)

---

## SessionStart Hooksとは

**SessionStart Hooks**は、Claude Codeのセッションライフサイクルに組み込まれたフック機能です。セッションが開始または再開されるタイミングで、指定したスクリプトやコマンドを自動実行できます。

### 主な用途
- 開発ツールのインストール（dotnet, node, python など）
- 依存関係の復元（`dotnet restore`, `npm install` など）
- 環境変数の設定
- データベース接続の初期化
- 開発サーバーの起動

---

## 仕組みと特徴

### 実行タイミング

SessionStart Hooksは以下のタイミングで実行されます：

| タイミング | 説明 |
|-----------|------|
| セッション開始 | 新しいClaude Codeセッションが開始されたとき |
| セッション再開 | `--resume`オプションで既存セッションを再開したとき |
| `/clear`実行後 | 会話履歴をクリアした後 |
| コンテキスト圧縮後 | 長い会話でコンテキストが圧縮された後 |

### CLAUDE_ENV_FILE

SessionStart Hooksの重要な機能として、**CLAUDE_ENV_FILE**があります。この環境変数に指定されたファイルに書き込んだ内容は、以降のBashコマンドで利用可能になります。

```bash
# 環境変数をCLAUDE_ENV_FILEに永続化
if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "export PATH=\"\$HOME/.dotnet:\$PATH\"" >> "$CLAUDE_ENV_FILE"
fi
```

### フックの出力

SessionStart Hooksの標準出力は、Claude Codeのコンテキストに含まれます。これにより：
- Claudeがセットアップ状況を理解できる
- インストール済みのツールやバージョンを把握できる
- エラーが発生した場合、Claudeが問題を認識できる

---

## dotnetのインストール設定

### ディレクトリ構造

```
PowerTools/
├── .claude/
│   ├── settings.json      # フック設定ファイル
│   └── session-start.sh   # セットアップスクリプト
├── PowerTools.Server/
├── PowerTools.Tests/
└── ...
```

### settings.json

`.claude/settings.json`ファイルでフックを設定します：

```json
{
  "hooks": {
    "SessionStart": [
      {
        "hooks": [
          {
            "type": "command",
            "command": ".claude/session-start.sh"
          }
        ]
      }
    ]
  }
}
```

### session-start.sh

`.claude/session-start.sh`ファイルでdotnetをインストールします：

```bash
#!/bin/bash
# SessionStart hook for .NET development environment

set -e

echo "=== PowerTools SessionStart Hook ==="
echo "Setting up .NET development environment..."

# Install .NET SDK if not available
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET SDK 8.0..."

    # Download and run the official .NET install script
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
    rm /tmp/dotnet-install.sh

    echo ".NET SDK installed successfully"
else
    echo "dotnet is already available: $(dotnet --version)"
fi

# Add dotnet to PATH and persist in CLAUDE_ENV_FILE
DOTNET_PATH="$HOME/.dotnet"
if [[ ":$PATH:" != *":$DOTNET_PATH:"* ]]; then
    export PATH="$DOTNET_PATH:$PATH"
    export DOTNET_ROOT="$DOTNET_PATH"

    # Persist environment variables for subsequent bash commands
    if [ -n "$CLAUDE_ENV_FILE" ]; then
        echo "export PATH=\"$DOTNET_PATH:\$PATH\"" >> "$CLAUDE_ENV_FILE"
        echo "export DOTNET_ROOT=\"$DOTNET_PATH\"" >> "$CLAUDE_ENV_FILE"
        echo "Environment variables persisted to CLAUDE_ENV_FILE"
    fi
fi

# Restore NuGet packages if solution file exists
if [ -f "/home/user/PowerTools/PowerTools.sln" ]; then
    echo "Restoring NuGet packages..."
    cd /home/user/PowerTools
    dotnet restore --verbosity minimal
    echo "NuGet packages restored successfully"
fi

echo "=== SessionStart Hook Complete ==="
echo "dotnet version: $(dotnet --version 2>/dev/null || echo 'not available yet')"
```

### スクリプトを実行可能にする

```bash
chmod +x .claude/session-start.sh
```

---

## 設定ファイルの詳細

### settings.jsonの構造

```json
{
  "hooks": {
    "SessionStart": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "実行するコマンドまたはスクリプト"
          }
        ]
      }
    ]
  }
}
```

### フックの種類

| 種類 | 説明 |
|------|------|
| `SessionStart` | セッション開始時に実行 |
| `Stop` | ツール実行停止時に実行 |
| `PreToolUse` | ツール実行前に実行 |
| `PostToolUse` | ツール実行後に実行 |

### 複数のフックを設定

```json
{
  "hooks": {
    "SessionStart": [
      {
        "hooks": [
          {
            "type": "command",
            "command": ".claude/install-dotnet.sh"
          },
          {
            "type": "command",
            "command": ".claude/install-tools.sh"
          }
        ]
      }
    ]
  }
}
```

---

## トラブルシューティング

### dotnetコマンドが見つからない

**症状**: SessionStart後も`dotnet: command not found`が表示される

**原因と解決策**:

1. **CLAUDE_ENV_FILEへの書き込み漏れ**
   ```bash
   # CLAUDE_ENV_FILEが設定されているか確認
   if [ -n "$CLAUDE_ENV_FILE" ]; then
       echo "export PATH=\"\$HOME/.dotnet:\$PATH\"" >> "$CLAUDE_ENV_FILE"
   fi
   ```

2. **PATHの順序問題**
   ```bash
   # dotnetを先頭に追加
   export PATH="$HOME/.dotnet:$PATH"
   ```

3. **インストール先の確認**
   ```bash
   ls -la $HOME/.dotnet/dotnet
   ```

### NuGetパッケージの復元失敗

**症状**: `dotnet restore`がエラーで失敗する

**解決策**:

1. **ネットワーク接続を確認**
   ```bash
   curl -I https://api.nuget.org/v3/index.json
   ```

2. **キャッシュをクリア**
   ```bash
   dotnet nuget locals all --clear
   ```

3. **詳細ログを有効化**
   ```bash
   dotnet restore --verbosity detailed
   ```

### フックが実行されない

**症状**: セッション開始時にフックが実行されない

**確認事項**:

1. **settings.jsonの場所**
   - `.claude/settings.json`がリポジトリルートにあるか確認

2. **JSONの構文**
   ```bash
   cat .claude/settings.json | python3 -m json.tool
   ```

3. **スクリプトの実行権限**
   ```bash
   chmod +x .claude/session-start.sh
   ```

---

## その他のユースケース

### Node.js環境のセットアップ

```bash
#!/bin/bash
# Node.js環境セットアップ

if ! command -v node &> /dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_20.x -o /tmp/nodesource_setup.sh
    # Optionally verify the script's integrity here (e.g., checksum)
    sudo bash /tmp/nodesource_setup.sh
    rm /tmp/nodesource_setup.sh
    sudo apt-get install -y nodejs
fi

# npm依存関係のインストール
if [ -f "package.json" ]; then
    npm install
fi

if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "export NODE_ENV=development" >> "$CLAUDE_ENV_FILE"
fi
```

### Python環境のセットアップ

```bash
#!/bin/bash
# Python仮想環境セットアップ

if [ ! -d ".venv" ]; then
    python3 -m venv .venv
fi

source .venv/bin/activate

if [ -f "requirements.txt" ]; then
    pip install -r requirements.txt
fi

if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "source $(pwd)/.venv/bin/activate" >> "$CLAUDE_ENV_FILE"
fi
```

### Docker環境の確認

```bash
#!/bin/bash
# Docker環境確認

if command -v docker &> /dev/null; then
    echo "Docker version: $(docker --version)"

    # Docker Composeで依存サービスを起動
    if [ -f "docker-compose.yml" ]; then
        docker compose up -d 2>/dev/null || docker-compose up -d
    fi
else
    echo "Warning: Docker is not available"
fi
```

### 複合環境のセットアップ

```bash
#!/bin/bash
# 複合開発環境セットアップ

set -e

echo "=== Setting up development environment ==="

# .NET SDK
if ! command -v dotnet &> /dev/null; then
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
fi

# Node.js (フロントエンド用)
if ! command -v node &> /dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
    sudo apt-get install -y nodejs
fi

# 依存関係の復元
dotnet restore
npm install --prefix ./frontend

# 環境変数の永続化
if [ -n "$CLAUDE_ENV_FILE" ]; then
    cat >> "$CLAUDE_ENV_FILE" << 'ENVEOF'
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export NODE_ENV=development
ENVEOF
fi

echo "=== Development environment ready ==="
```

---

## ベストプラクティス

### 1. 冪等性を保つ

スクリプトは何度実行しても同じ結果になるように設計します：

```bash
# 良い例: 既にインストール済みかチェック
if ! command -v dotnet &> /dev/null; then
    # インストール処理
fi

# 悪い例: インストーラをダウンロードするだけで実行しない（実際には何もインストールされない）
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
```

### 2. エラーハンドリング

```bash
set -e  # エラー時に停止

# または個別にハンドリング
if ! dotnet restore; then
    echo "Warning: dotnet restore failed, continuing..."
fi
```

### 3. 進捗の可視化

```bash
echo "=== Step 1/3: Installing .NET SDK ==="
# ...
echo "=== Step 2/3: Restoring packages ==="
# ...
echo "=== Step 3/3: Setup complete ==="
```

### 4. タイムアウトの考慮

長時間かかる処理は避けるか、バックグラウンドで実行：

```bash
# 重い処理はバックグラウンドで
dotnet build &
BUILD_PID=$!

# 他の処理を続行
echo "Build started in background (PID: $BUILD_PID)"

# 注意: ビルド結果に依存する処理が後続にある場合は、必ず完了を待ってください
# 例:
# wait $BUILD_PID
# echo "Build completed with exit code: $?"
```

---

## 参考リンク

- [Claude Code Hooks ドキュメント](https://docs.anthropic.com/en/docs/claude-code/hooks)
- [.NET インストールスクリプト](https://dot.net/v1/dotnet-install.sh)
- [Microsoft .NET ドキュメント](https://docs.microsoft.com/dotnet/)
