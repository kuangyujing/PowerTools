# PowerTools ヘルスチェック ガイド

このドキュメントでは、PowerTools Web APIのヘルスチェック機能について説明します。

## 目次
- [ヘルスチェックとは](#ヘルスチェックとは)
- [PowerToolsでの実装](#powertoolsでの実装)
- [使用方法](#使用方法)
- [カスタムヘルスチェックの追加](#カスタムヘルスチェックの追加)
- [高度な設定](#高度な設定)
- [Kubernetesとの統合](#kubernetesとの統合)
- [トラブルシューティング](#トラブルシューティング)

---

## ヘルスチェックとは

ヘルスチェックは、アプリケーションの健全性を監視するためのエンドポイントです。主に以下の目的で使用されます：

- **コンテナオーケストレーション**: Kubernetes、Docker Swarm等が自動的にコンテナの健全性を確認
- **ロードバランサー**: 正常なインスタンスにのみトラフィックを振り分け
- **監視システム**: Prometheus、Datadog、New Relic等の監視ツールでアプリケーションの状態を追跡
- **自動復旧**: 異常を検出した際に自動的にコンテナを再起動

### ヘルスチェックの種類

| 種類 | 説明 | 用途 |
|------|------|------|
| **Liveness Probe** | アプリケーションが稼働中か | コンテナの再起動判断 |
| **Readiness Probe** | リクエストを受け付けられる状態か | トラフィックのルーティング |
| **Startup Probe** | 初期化が完了したか | 起動時の猶予期間設定 |

---

## PowerToolsでの実装

PowerToolsは、ASP.NET Coreの**組み込みヘルスチェックミドルウェア**を使用しています。

### なぜControllerを使わないのか？

ASP.NET Coreには公式のヘルスチェックミドルウェアが用意されており、Controllerを作成する必要がありません。この方式には以下のメリットがあります：

- ✅ **パフォーマンス**: MVC処理を経由せず、ミドルウェアが直接処理
- ✅ **標準化**: Microsoftの推奨する公式の方法
- ✅ **拡張性**: カスタムヘルスチェックを簡単に追加可能
- ✅ **依存関係チェック**: データベース、外部API等の健全性も確認可能

### 実装コード (Program.cs)

```csharp
// ヘルスチェックサービスを登録
builder.Services.AddHealthChecks();

// エンドポイントをマッピング
app.MapHealthChecks("/api/health");
```

### 動作の仕組み

```
HTTPリクエスト
    ↓
Kestrelサーバー
    ↓
ミドルウェアパイプライン
    ↓
MapHealthChecks("/api/health")
    ↓
登録されたIHealthCheck実装を実行
    ↓
レスポンス生成
    ↓
HTTP 200 OK (Healthy) または HTTP 503 Service Unavailable (Unhealthy)
```

---

## 使用方法

### 基本的な使用

**エンドポイント:**
```
GET /api/health
```

**curlでの確認:**
```bash
curl http://localhost:8080/api/health
```

**正常時のレスポンス:**
```
HTTP/1.1 200 OK
Content-Type: text/plain
Content-Length: 7

Healthy
```

**異常時のレスポンス:**
```
HTTP/1.1 503 Service Unavailable
Content-Type: text/plain
Content-Length: 9

Unhealthy
```

### ステータスコード

| HTTPステータス | 状態 | 説明 |
|---------------|------|------|
| `200 OK` | Healthy | すべてのチェックが正常 |
| `503 Service Unavailable` | Unhealthy | 1つ以上のチェックが失敗 |
| `200 OK` | Degraded | 一部の機能が低下（カスタム設定時） |

### Dockerでの使用

PowerToolsのDockerfileとdocker-compose.ymlには、ヘルスチェックがあらかじめ設定されています。

**Dockerfile:**
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --spider --tries=1 --no-verbose http://localhost:8080/api/health || exit 1
```

**docker-compose.yml:**
```yaml
healthcheck:
  test: ["CMD", "wget", "--spider", "--tries=1", "--no-verbose", "http://localhost:8080/api/health"]
  interval: 30s
  timeout: 3s
  retries: 3
  start_period: 10s
```

**ヘルスチェックステータスの確認:**
```bash
# コンテナの状態を確認
docker ps

# ヘルスチェックの詳細を確認
docker inspect powertools-server | jq '.[0].State.Health'
```

---

## カスタムヘルスチェックの追加

外部依存関係（データベース、API等）の健全性を確認するカスタムヘルスチェックを追加できます。

### 例1: カスタムヘルスチェッククラスの作成

**PowerTools.Server/HealthChecks/DatabaseHealthCheck.cs:**
```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PowerTools.Server.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // データベース接続チェックのロジック
            // 例: await _dbContext.Database.CanConnectAsync(cancellationToken);

            var isHealthy = true; // 実際の接続チェック結果

            if (isHealthy)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("Database connection is healthy."));
            }

            return Task.FromResult(
                HealthCheckResult.Unhealthy("Database connection failed."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Database check exception.", ex));
        }
    }
}
```

### 例2: ヘルスチェックの登録 (Program.cs)

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck("example_check", () =>
    {
        // インラインで簡単なチェックを定義
        var isHealthy = true; // 実際のチェックロジック
        return isHealthy
            ? HealthCheckResult.Healthy("Example check passed")
            : HealthCheckResult.Unhealthy("Example check failed");
    });
```

### 例3: 既存ライブラリの使用

NuGetパッケージ `AspNetCore.Diagnostics.HealthChecks` を使用すると、様々な依存関係のヘルスチェックを簡単に追加できます。

**インストール:**
```bash
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package AspNetCore.HealthChecks.Npgsql
```

**使用例 (Program.cs):**
```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "sql-server")
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("Redis"),
        name: "redis")
    .AddUrlGroup(
        new Uri("https://api.example.com/health"),
        name: "external-api");
```

---

## 高度な設定

### 1. JSON形式のレスポンス

デフォルトでは "Healthy" というテキストが返されますが、詳細なJSON形式のレスポンスに変更できます。

**NuGetパッケージのインストール:**
```bash
dotnet add package AspNetCore.HealthChecks.UI.Client
```

**Program.cs:**
```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**JSONレスポンス例:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0098765",
      "description": "Database connection is healthy."
    },
    "external-api": {
      "status": "Healthy",
      "duration": "00:00:00.0024691"
    }
  }
}
```

### 2. 複数のヘルスチェックエンドポイント

Liveness ProbeとReadiness Probeを分離できます。

**Program.cs:**
```csharp
// Liveness: アプリケーションが稼働中か（基本的なチェックのみ）
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "liveness" });

// Readiness: 依存関係を含めた準備状態（すべてのチェック）
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "readiness" })
    .AddCheck("external-api", () => /* APIチェック */, tags: new[] { "readiness" });

// エンドポイントを分離
app.MapHealthChecks("/api/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("liveness")
});

app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("readiness")
});
```

### 3. タイムアウトの設定

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" },
        timeout: TimeSpan.FromSeconds(3));
```

### 4. ヘルスチェックの結果に基づくフィルタリング

```csharp
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    // Degraded状態を200 OKとして返す
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
```

---

## Kubernetesとの統合

Kubernetesでヘルスチェックを使用する場合の設定例：

**deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: powertools
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: powertools
        image: powertools:latest
        ports:
        - containerPort: 8080

        # Liveness Probe: コンテナが稼働中か
        livenessProbe:
          httpGet:
            path: /api/health/live
            port: 8080
            scheme: HTTP
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 3
          successThreshold: 1
          failureThreshold: 3

        # Readiness Probe: トラフィックを受け付けられるか
        readinessProbe:
          httpGet:
            path: /api/health/ready
            port: 8080
            scheme: HTTP
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          successThreshold: 1
          failureThreshold: 3

        # Startup Probe: 初期化が完了したか
        startupProbe:
          httpGet:
            path: /api/health/live
            port: 8080
            scheme: HTTP
          initialDelaySeconds: 0
          periodSeconds: 2
          timeoutSeconds: 3
          successThreshold: 1
          failureThreshold: 30  # 最大60秒待機
```

### プローブパラメータの説明

| パラメータ | 説明 | 推奨値 |
|-----------|------|--------|
| `initialDelaySeconds` | 最初のプローブまでの待機時間 | 5-10秒 |
| `periodSeconds` | プローブの実行間隔 | 5-10秒 |
| `timeoutSeconds` | プローブのタイムアウト | 3秒 |
| `successThreshold` | 成功と判定するまでの連続成功回数 | 1 |
| `failureThreshold` | 失敗と判定するまでの連続失敗回数 | 3 |

---

## トラブルシューティング

### 問題: ヘルスチェックが常に503を返す

**原因:**
- カスタムヘルスチェックが失敗している
- 依存関係（DB、外部API等）に接続できない

**解決策:**
```bash
# ログを確認
docker logs powertools-server

# 詳細なJSONレスポンスを取得
curl http://localhost:8080/api/health -i

# 個別のヘルスチェックをテスト（タグで分離している場合）
curl http://localhost:8080/api/health/live
curl http://localhost:8080/api/health/ready
```

### 問題: ヘルスチェックエンドポイントが404を返す

**原因:**
- `app.MapHealthChecks()` が呼び出されていない
- エンドポイントのパスが間違っている

**解決策:**
```csharp
// Program.cs で以下が呼び出されているか確認
app.MapHealthChecks("/api/health");
```

### 問題: Kubernetesでコンテナが再起動を繰り返す

**原因:**
- Liveness Probeの `initialDelaySeconds` が短すぎる
- アプリケーションの起動に時間がかかっている

**解決策:**
```yaml
livenessProbe:
  initialDelaySeconds: 30  # 起動時間を増やす
  periodSeconds: 15        # チェック間隔を延ばす

# または Startup Probe を使用
startupProbe:
  httpGet:
    path: /api/health/live
    port: 8080
  failureThreshold: 30  # 最大60秒待機
  periodSeconds: 2
```

### 問題: ヘルスチェックが遅い

**原因:**
- カスタムヘルスチェックが重い処理を実行している
- タイムアウトが設定されていない

**解決策:**
```csharp
// タイムアウトを設定
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        "database",
        timeout: TimeSpan.FromSeconds(3));  // 3秒でタイムアウト

// または非同期処理を最適化
public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
{
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken, timeoutCts.Token);

    try
    {
        await _dbContext.Database.CanConnectAsync(linkedCts.Token);
        return HealthCheckResult.Healthy();
    }
    catch (OperationCanceledException)
    {
        return HealthCheckResult.Degraded("Health check timed out");
    }
}
```

---

## ベストプラクティス

### 1. Liveness と Readiness を分離

```csharp
// Liveness: 軽量なチェック（プロセスが生きているか）
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "liveness" });

// Readiness: 重い処理を含むチェック（依存関係を含む）
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "readiness" })
    .AddCheck<ExternalApiHealthCheck>("api", tags: new[] { "readiness" });
```

### 2. タイムアウトは必ず設定

```csharp
.AddCheck<DatabaseHealthCheck>("database", timeout: TimeSpan.FromSeconds(3))
```

### 3. ヘルスチェックは軽量に保つ

```csharp
// ❌ 悪い例: 重い処理
public async Task<HealthCheckResult> CheckHealthAsync(...)
{
    var users = await _dbContext.Users.ToListAsync(); // 全件取得は重い
    return HealthCheckResult.Healthy();
}

// ✅ 良い例: 軽量なチェック
public async Task<HealthCheckResult> CheckHealthAsync(...)
{
    var canConnect = await _dbContext.Database.CanConnectAsync(); // 接続確認のみ
    return canConnect
        ? HealthCheckResult.Healthy()
        : HealthCheckResult.Unhealthy();
}
```

### 4. 環境ごとに設定を変更

```csharp
if (app.Environment.IsDevelopment())
{
    // 開発環境: 詳細なJSONレスポンス
    app.MapHealthChecks("/api/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
}
else
{
    // 本番環境: シンプルなテキストレスポンス
    app.MapHealthChecks("/api/health");
}
```

---

## 参考リンク

- [Microsoft Docs: Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [AspNetCore.Diagnostics.HealthChecks (GitHub)](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)
- [Kubernetes: Configure Liveness, Readiness and Startup Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)

---

## まとめ

PowerToolsのヘルスチェック機能により、以下が実現できます：

- ✅ **自動監視**: Dockerとk8sによる自動健全性チェック
- ✅ **標準実装**: ASP.NET Core公式のミドルウェアを使用
- ✅ **拡張性**: カスタムヘルスチェックを簡単に追加可能
- ✅ **本番対応**: Liveness/Readinessプローブの分離に対応

ヘルスチェックは、本番環境でのアプリケーション運用において不可欠な機能です。適切に設定することで、システムの信頼性と可用性を大幅に向上させることができます。
