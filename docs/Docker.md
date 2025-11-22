# PowerTools Docker ガイド

このドキュメントでは、PowerTools Web APIをDockerコンテナとして実行する方法を説明します。

## 目次
- [前提条件](#前提条件)
- [クイックスタート](#クイックスタート)
- [Dockerfileの詳細](#dockerfileの詳細)
- [docker-composeの使用](#docker-composeの使用)
- [環境変数](#環境変数)
- [本番環境での推奨設定](#本番環境での推奨設定)
- [トラブルシューティング](#トラブルシューティング)

---

## 前提条件

以下がインストールされていることを確認してください：
- **Docker**: バージョン 20.10 以降
- **Docker Compose**: バージョン 2.0 以降（オプション）

インストール確認：
```bash
docker --version
docker-compose --version
```

---

## クイックスタート

### 方法1: Dockerコマンドを使用

**1. イメージのビルド**
```bash
docker build -t powertools:latest .
```

**2. コンテナの実行**
```bash
docker run -d \
  --name powertools-server \
  -p 8080:8080 \
  powertools:latest
```

**3. 動作確認**
```bash
# ヘルスチェック
curl http://localhost:8080/api/health

# Swagger UI（ブラウザで開く）
open http://localhost:8080/swagger
```

**4. ログの確認**
```bash
docker logs powertools-server
```

**5. コンテナの停止**
```bash
docker stop powertools-server
docker rm powertools-server
```

### 方法2: Docker Composeを使用（推奨）

**1. 起動**
```bash
docker-compose up -d
```

**2. 動作確認**
```bash
curl http://localhost:8080/api/health
```

**3. ログの確認**
```bash
docker-compose logs -f powertools
```

**4. 停止**
```bash
docker-compose down
```

**5. 再ビルド**
```bash
docker-compose up -d --build
```

---

## Dockerfileの詳細

### マルチステージビルド

PowerToolsのDockerfileは3ステージで構成されています：

#### ステージ1: ビルド
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
```
- .NET 8 SDKを使用してアプリケーションをビルド
- 依存関係の復元とコンパイルを実行

#### ステージ2: パブリッシュ
```dockerfile
FROM build AS publish
```
- リリース用の最適化されたビルドを作成
- 実行に必要なファイルのみを出力

#### ステージ3: ランタイム
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
```
- 軽量なASP.NET Core Runtimeのみを含む
- 非rootユーザー（appuser）で実行
- ポート8080を公開

### イメージサイズの最適化

- **マルチステージビルド**: SDKイメージ（約1.8GB）からランタイムイメージ（約200MB）へ
- **.dockerignore**: 不要なファイルを除外してビルドコンテキストを削減
- **レイヤーキャッシュ**: プロジェクトファイルを先にコピーして依存関係を効率的にキャッシュ

### セキュリティ対策

- **非rootユーザー実行**: `appuser`（UID 1000）で実行
- **最小権限の原則**: 必要最小限のファイルのみをコピー
- **ベースイメージ**: Microsoft公式のイメージを使用

### ヘルスチェック

PowerToolsのDockerfileには、コンテナの稼働状態を監視するヘルスチェックが含まれています。

**wgetを使用する理由:**
ASP.NET Coreの公式イメージ（`mcr.microsoft.com/dotnet/aspnet:8.0`）には、デフォルトでHTTPクライアントツールが含まれていません。PowerToolsでは、イメージサイズと軽量性を考慮してwgetを採用しています：

```dockerfile
# Install wget for healthcheck (more lightweight than curl: ~2MB vs ~15MB)
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*
```

**パッケージサイズの比較:**
- **wget**: 約2MB（依存関係込み）
- **curl**: 約15MB（依存関係込み）

wgetを使用することで、約13MBのイメージサイズ削減を実現しています。

**wgetのヘルスチェックコマンド:**
```dockerfile
HEALTHCHECK CMD wget --spider --tries=1 --no-verbose http://localhost:8080/api/health || exit 1
```

**オプションの説明:**
- `--spider`: ページをダウンロードせず、存在確認のみ実行
- `--tries=1`: リトライを1回のみに制限（無限ループを防止）
- `--no-verbose`: エラー時のみログ出力

**代替案:**

1. **curlを使用**: より多機能だがイメージサイズが大きい
   ```dockerfile
   RUN apt-get update && apt-get install -y --no-install-recommends curl
   ```
   ```dockerfile
   HEALTHCHECK CMD curl --fail http://localhost:8080/api/health || exit 1
   ```

2. **.NETベースのヘルスチェックツール**: 追加パッケージ不要（例: MrRabbit.HealthChecks.Container.Client）
   ```dockerfile
   HEALTHCHECK CMD ["dotnet", "/app/healthcheck.dll", "http://localhost:8080/api/health"]
   ```

3. **ヘルスチェックを削除**: 外部の監視ツール（Kubernetes Liveness/Readiness Probes等）を使用する場合

---

## docker-composeの使用

### 基本的な設定

`docker-compose.yml` の主要な設定項目：

```yaml
services:
  powertools:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./logs:/app/logs
    restart: unless-stopped
```

### カスタマイズ例

#### ポート番号の変更
```yaml
ports:
  - "5000:8080"  # ホストの5000番ポートにマッピング
```

#### 環境変数の追加
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - Logging__LogLevel__Default=Warning
  - CustomSetting__Value=MyValue
```

#### 複数のサービスとの連携
```yaml
services:
  powertools:
    # ... PowerTools設定 ...
    depends_on:
      - database
    networks:
      - app-network

  database:
    image: postgres:15
    environment:
      - POSTGRES_PASSWORD=secretpassword
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

---

## 環境変数

### 必須の環境変数

| 変数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `ASPNETCORE_URLS` | `http://+:8080` | リスニングURL |
| `ASPNETCORE_ENVIRONMENT` | `Production` | 実行環境 |

### オプションの環境変数

| 変数名 | 例 | 説明 |
|--------|-----|------|
| `Logging__LogLevel__Default` | `Information` | デフォルトログレベル |
| `Logging__LogLevel__Microsoft.AspNetCore` | `Warning` | ASP.Coreログレベル |
| `AllowedHosts` | `*` | 許可するホスト |

### 環境変数の設定方法

**Docker run:**
```bash
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Logging__LogLevel__Default=Warning \
  -p 8080:8080 \
  powertools:latest
```

**Docker Compose:**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - Logging__LogLevel__Default=Warning
```

または `.env` ファイルを使用：
```bash
# .env
ASPNETCORE_ENVIRONMENT=Production
LOGGING_LEVEL=Warning
```

```yaml
env_file:
  - .env
```

---

## 本番環境での推奨設定

### 1. HTTPSの有効化

本番環境ではHTTPSを使用することを強く推奨します。

**証明書のマウント:**
```yaml
volumes:
  - ./certs:/https:ro
environment:
  - ASPNETCORE_URLS=https://+:8443;http://+:8080
  - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/certificate.pfx
  - ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword
ports:
  - "8443:8443"
  - "8080:8080"
```

**リバースプロキシの使用（推奨）:**
NginxやTraefikなどのリバースプロキシでSSL/TLSを終端させる方が管理が容易です。

### 2. リソース制限

```yaml
services:
  powertools:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
```

### 3. ヘルスチェック

Dockerfileにはデフォルトでヘルスチェックが含まれていますが、カスタマイズも可能です：

```yaml
healthcheck:
  test: ["CMD", "curl", "--fail", "http://localhost:8080/api/health"]
  interval: 30s
  timeout: 3s
  retries: 3
  start_period: 10s
```

### 4. ログ管理

**ログローテーション:**
```yaml
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"
```

**外部ログシステムへの転送:**
```yaml
logging:
  driver: "syslog"
  options:
    syslog-address: "tcp://logserver:514"
```

### 5. 環境設定

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - Logging__LogLevel__Default=Warning
  - Logging__LogLevel__Microsoft.AspNetCore=Error
restart: always
```

---

## トラブルシューティング

### 問題: コンテナが起動しない

**確認事項:**
```bash
# コンテナのログを確認
docker logs powertools-server

# コンテナの状態を確認
docker ps -a
```

**よくある原因:**
- ポートが既に使用されている → 別のポート番号を使用
- ビルドエラー → `docker build` の出力を確認
- 環境変数の設定ミス → 環境変数を確認

### 問題: ポート8080に接続できない

**解決策:**
```bash
# Dockerコンテナのポートマッピングを確認
docker port powertools-server

# ファイアウォール設定を確認
# macOS
sudo pfctl -s rules

# Linux
sudo iptables -L
```

### 問題: ヘルスチェックが失敗する

**原因:**
- アプリケーションの起動に時間がかかっている
- ヘルスチェックエンドポイントが利用できない

**解決策:**
```yaml
healthcheck:
  start_period: 30s  # 起動時間を増やす
  interval: 60s      # チェック間隔を延ばす
```

### 問題: イメージサイズが大きすぎる

**確認:**
```bash
docker images powertools
```

**解決策:**
- `.dockerignore` が正しく設定されているか確認
- マルチステージビルドが使用されているか確認
- 不要なファイルがコピーされていないか確認

### 問題: コンテナ内でファイル書き込みができない

**原因:**
- 非rootユーザーで実行されているため、権限が不足

**解決策:**
```yaml
volumes:
  - ./data:/app/data
# ホスト側でディレクトリの権限を変更
# chown -R 1000:1000 ./data
```

---

## コマンドリファレンス

### イメージ操作

```bash
# イメージのビルド
docker build -t powertools:latest .

# イメージのタグ付け
docker tag powertools:latest powertools:v1.0.0

# イメージの削除
docker rmi powertools:latest

# イメージの一覧
docker images powertools
```

### コンテナ操作

```bash
# コンテナの起動
docker run -d --name powertools-server -p 8080:8080 powertools:latest

# コンテナの停止
docker stop powertools-server

# コンテナの再起動
docker restart powertools-server

# コンテナの削除
docker rm powertools-server

# 実行中のコンテナ一覧
docker ps

# 全てのコンテナ一覧
docker ps -a
```

### ログとデバッグ

```bash
# ログの表示
docker logs powertools-server

# ログのリアルタイム表示
docker logs -f powertools-server

# コンテナ内でコマンド実行
docker exec -it powertools-server /bin/bash

# コンテナの統計情報
docker stats powertools-server
```

### Docker Compose

```bash
# 起動
docker-compose up -d

# 停止
docker-compose down

# 再ビルドして起動
docker-compose up -d --build

# ログ表示
docker-compose logs -f

# サービスの一覧
docker-compose ps

# サービスの再起動
docker-compose restart powertools
```

---

## 推奨: リバースプロキシの使用

本番環境では、Nginx、Traefik、またはCaddyなどのリバースプロキシの使用を推奨します。

### Nginx + PowerTools の例

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  powertools:
    build: .
    expose:
      - 8080
    networks:
      - app-network

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/nginx/certs:ro
    depends_on:
      - powertools
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

**nginx.conf:**
```nginx
server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass http://powertools:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## まとめ

PowerToolsのDocker化により以下のメリットがあります：

- **環境の統一**: 開発、テスト、本番環境で同一のコンテナを使用
- **簡単なデプロイ**: `docker-compose up` だけで起動
- **スケーラビリティ**: 複数のコンテナを簡単に起動可能
- **分離**: ホストシステムへの影響を最小限に

詳細な情報やサポートが必要な場合は、[Docker公式ドキュメント](https://docs.docker.com/)を参照してください。
