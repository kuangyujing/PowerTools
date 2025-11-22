# ASP.NET Core ミドルウェア 完全ガイド

このドキュメントでは、ASP.NET Core 8の全組み込みミドルウェアと、PowerToolsでの使用方法について包括的に解説します。

## 目次
- [ミドルウェアとは](#ミドルウェアとは)
- [ミドルウェアパイプラインの仕組み](#ミドルウェアパイプラインの仕組み)
- [全組み込みミドルウェア一覧](#全組み込みミドルウェア一覧)
- [ミドルウェア詳細解説](#ミドルウェア詳細解説)
- [推奨される実行順序](#推奨される実行順序)
- [PowerToolsでの使用例](#powertoolsでの使用例)
- [カスタムミドルウェアの作成](#カスタムミドルウェアの作成)
- [トラブルシューティング](#トラブルシューティング)

---

## ミドルウェアとは

ミドルウェアは、**HTTPリクエスト処理パイプラインを構成するソフトウェアコンポーネント**です。各ミドルウェアは以下の役割を持ちます：

- 受信したHTTPリクエストを処理
- 次のミドルウェアにリクエストを渡すか、処理を終了（短絡）
- レスポンスを生成・変更

### ミドルウェアの特徴

```
リクエスト → [MW1] → [MW2] → [MW3] → [エンドポイント]
                ↓        ↓        ↓            ↓
               次へ     次へ     次へ       レスポンス生成
                ↓        ↓        ↓            ↓
レスポンス ← [MW1] ← [MW2] ← [MW3] ← [エンドポイント]
```

- **双方向処理**: リクエストとレスポンスの両方を処理可能
- **順序重要**: ミドルウェアの追加順序が動作に影響
- **短絡（Short-circuiting）**: ミドルウェアが処理を終了し、次のミドルウェアをスキップ可能

---

## ミドルウェアパイプラインの仕組み

### 基本的なパイプライン構造

ASP.NET Coreアプリケーションは、`Program.cs`でミドルウェアパイプラインを構成します：

```csharp
var builder = WebApplication.CreateBuilder(args);

// サービス登録
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ミドルウェアパイプラインの構成
app.UseHttpsRedirection();      // ①
app.UseStaticFiles();            // ②
app.UseRouting();                // ③
app.UseAuthentication();         // ④
app.UseAuthorization();          // ⑤
app.MapHealthChecks("/health");  // ⑥
app.MapControllers();            // ⑦

app.Run();
```

### ミドルウェアの実行フロー

**リクエスト処理:**
```
HTTP GET / → ① → ② → ③ → ④ → ⑤ → ⑥ → ⑦ (エンドポイント実行)
```

**レスポンス処理:**
```
⑦ (レスポンス生成) → ⑥ → ⑤ → ④ → ③ → ② → ① → HTTPレスポンス
```

### 短絡（Short-circuiting）の例

```csharp
app.UseStaticFiles();  // 静的ファイルが見つかれば、ここで処理終了

// ↓ 静的ファイルリクエストの場合、以下は実行されない
app.UseRouting();
app.MapControllers();
```

---

## 全組み込みミドルウェア一覧

ASP.NET Core 8には**25以上**の組み込みミドルウェアがあります。

| # | ミドルウェア | カテゴリ | 主な用途 |
|---|------------|---------|---------|
| 1 | **Authentication** | セキュリティ | ユーザー認証 |
| 2 | **Authorization** | セキュリティ | アクセス制御 |
| 3 | **CORS** | セキュリティ | クロスオリジンリクエスト制御 |
| 4 | **HTTPS Redirection** | セキュリティ | HTTP→HTTPS自動リダイレクト |
| 5 | **HSTS** | セキュリティ | HTTP Strict Transport Security |
| 6 | **Cookie Policy** | セキュリティ | Cookie同意管理 |
| 7 | **Routing** | ルーティング | エンドポイント選択 |
| 8 | **Endpoints** | ルーティング | エンドポイント実行 |
| 9 | **MVC** | ルーティング | MVC/Razor Pages処理 |
| 10 | **URL Rewrite** | ルーティング | URL書き換え |
| 11 | **Static Files** | 静的コンテンツ | 静的ファイル提供 |
| 12 | **SPA** | 静的コンテンツ | Single Page Application対応 |
| 13 | **Developer Exception Page** | エラー処理 | 開発時の詳細エラー表示 |
| 14 | **Exception Handler** | エラー処理 | 本番環境のエラーハンドリング |
| 15 | **Response Caching** | パフォーマンス | レスポンスキャッシング |
| 16 | **Response Compression** | パフォーマンス | レスポンス圧縮 |
| 17 | **Request Decompression** | パフォーマンス | リクエスト解凍 |
| 18 | **HTTP Logging** | 診断・監視 | HTTPリクエスト/レスポンスログ |
| 19 | **Health Check** | 診断・監視 | ヘルスチェックエンドポイント |
| 20 | **Diagnostics** | 診断・監視 | 診断情報とステータスページ |
| 21 | **Forwarded Headers** | リクエスト処理 | プロキシヘッダー処理 |
| 22 | **HTTP Method Override** | リクエスト処理 | HTTPメソッド上書き |
| 23 | **Request Localization** | リクエスト処理 | 多言語対応 |
| 24 | **Header Propagation** | リクエスト処理 | HTTPヘッダー伝播 |
| 25 | **Session** | リクエスト処理 | セッション管理 |
| 26 | **OWIN** | その他 | OWINベースアプリ連携 |

---

## ミドルウェア詳細解説

### 1. セキュリティ系ミドルウェア

#### 1.1 Authentication（認証）

**目的:** ユーザーの身元を確認

**使用方法:**
```csharp
// サービス登録
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT設定 */ });

// ミドルウェア追加（UseRoutingの後、UseAuthorizationの前）
app.UseAuthentication();
```

**実行順序:** `UseRouting()`の後、`UseAuthorization()`の前

**重要なポイント:**
- `HttpContext.User`プロパティを設定
- 認証スキーム（JWT、Cookie、OAuth等）を選択可能
- 認証が必要なエンドポイントの前に配置

**実例:**
```csharp
// JWT Bearer認証の設定
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = "https://api.example.com",
            ValidAudience = "https://api.example.com"
        };
    });
```

---

#### 1.2 Authorization（認可）

**目的:** ユーザーがリソースにアクセスする権限があるか確認

**使用方法:**
```csharp
// サービス登録
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ミドルウェア追加（UseAuthenticationの直後）
app.UseAuthorization();
```

**実行順序:** `UseAuthentication()`の直後

**実例:**
```csharp
// コントローラーでの使用
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAdminData() => Ok(new { data = "admin" });
}
```

---

#### 1.3 CORS（Cross-Origin Resource Sharing）

**目的:** 異なるオリジンからのリクエストを制御

**使用方法:**
```csharp
// サービス登録
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins("https://example.com")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// ミドルウェア追加（UseRoutingの後、UseAuthorizationの前）
app.UseCors("AllowSpecificOrigin");
```

**実行順序:** CORSを使用するコンポーネントの前（通常`UseRouting()`の後）

**重要なポイント:**
- SPAやモバイルアプリからのAPIアクセスに必須
- セキュリティリスクを避けるため、適切なオリジン制限が重要

**実例（複数オリジン許可）:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(
                "https://app.example.com",
                "https://admin.example.com")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();  // Cookieを許可
    });
});
```

---

#### 1.4 HTTPS Redirection（HTTPS リダイレクト）

**目的:** すべてのHTTPリクエストをHTTPSにリダイレクト

**使用方法:**
```csharp
app.UseHttpsRedirection();
```

**実行順序:** パイプラインの早い段階（URLを消費するコンポーネントの前）

**実例:**
```csharp
// カスタム設定
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});
```

**動作:**
```
HTTP GET http://api.example.com/users
↓
301/307 リダイレクト
↓
HTTPS GET https://api.example.com/users
```

---

#### 1.5 HSTS（HTTP Strict Transport Security）

**目的:** ブラウザに常にHTTPS接続を強制

**使用方法:**
```csharp
app.UseHsts();
```

**実行順序:** レスポンスが送信される前

**重要なポイント:**
- 本番環境のみで使用（開発環境では無効化）
- `Strict-Transport-Security`ヘッダーを追加

**実例:**
```csharp
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
```

**生成されるヘッダー:**
```
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

---

#### 1.6 Cookie Policy（Cookie ポリシー）

**目的:** Cookie使用に関するユーザー同意を管理（GDPR対応）

**使用方法:**
```csharp
builder.Services.AddCookiePolicy(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

app.UseCookiePolicy();
```

**実行順序:** Cookieを発行するミドルウェアの前

---

### 2. ルーティング系ミドルウェア

#### 2.1 Routing（ルーティング）

**目的:** リクエストに最適なエンドポイントを選択

**使用方法:**
```csharp
app.UseRouting();
```

**実行順序:** `UseAuthentication()`の前、`UseEndpoints()`の前

**重要なポイント:**
- エンドポイントを**選択**するが、**実行しない**
- `UseEndpoints()`とペアで使用

**動作の仕組み:**
```csharp
app.UseRouting();  // ← エンドポイント選択
// ↓ この間で認証・認可などを実行
app.UseAuthentication();
app.UseAuthorization();
// ↓
app.UseEndpoints(endpoints =>  // ← エンドポイント実行
{
    endpoints.MapControllers();
});
```

---

#### 2.2 Endpoints（エンドポイント）

**目的:** 選択されたエンドポイントを実行

**使用方法:**
```csharp
app.MapControllers();              // MVC Controller
app.MapRazorPages();               // Razor Pages
app.MapGet("/hello", () => "Hi");  // Minimal API
app.MapHealthChecks("/health");    // Health Check
```

**実行順序:** パイプラインの最後

**重要なポイント:**
- ASP.NET Core 6以降、`WebApplication`が自動的に`UseRouting()`と`UseEndpoints()`を追加
- 明示的に`UseRouting()`を呼ぶと、その位置でルーティングが実行される

---

#### 2.3 MVC

**目的:** MVC/Razor Pagesリクエストを処理

**使用方法:**
```csharp
// サービス登録
builder.Services.AddControllers();       // API用
builder.Services.AddControllersWithViews(); // MVC用
builder.Services.AddRazorPages();        // Razor Pages用

// エンドポイントマッピング
app.MapControllers();
app.MapRazorPages();
```

**実行順序:** ルートがマッチした場合に実行（ターミナルミドルウェア）

---

#### 2.4 URL Rewrite（URL書き換え）

**目的:** URLを動的に書き換え

**使用方法:**
```csharp
using Microsoft.AspNetCore.Rewrite;

var rewriteOptions = new RewriteOptions()
    .AddRedirect("old-path/(.*)", "new-path/$1")
    .AddRewrite(@"^product/(\d+)", "products?id=$1", skipRemainingRules: true);

app.UseRewriter(rewriteOptions);
```

**実行順序:** ルーティングの前

**実例:**
```csharp
// www付きドメインへのリダイレクト
var rewriteOptions = new RewriteOptions()
    .AddRedirectToWwwPermanent();

app.UseRewriter(rewriteOptions);
```

---

### 3. 静的コンテンツ系ミドルウェア

#### 3.1 Static Files（静的ファイル）

**目的:** CSS、JavaScript、画像などの静的ファイルを提供

**使用方法:**
```csharp
app.UseStaticFiles();
```

**実行順序:** パイプラインの早い段階（ルーティング前が推奨）

**重要なポイント:**
- デフォルトで`wwwroot`ディレクトリから提供
- ファイルが見つかればパイプラインを短絡

**実例（カスタムディレクトリ）:**
```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "MyStaticFiles")),
    RequestPath = "/static"
});
```

**動作:**
```
GET /css/style.css → wwwroot/css/style.css を返す（短絡）
GET /api/users     → 次のミドルウェアへ
```

---

#### 3.2 SPA（Single Page Application）

**目的:** React、Angular、Vue.jsなどのSPAをホスト

**使用方法:**
```csharp
app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";

    if (app.Environment.IsDevelopment())
    {
        spa.UseReactDevelopmentServer(npmScript: "start");
    }
});
```

**実行順序:** ミドルウェアチェーンの後半

**重要なポイント:**
- すべてのルートで`index.html`を返すフォールバック機能
- 開発時は開発サーバーをプロキシ

---

### 4. エラー処理系ミドルウェア

#### 4.1 Developer Exception Page（開発者例外ページ）

**目的:** 開発時に詳細なエラー情報を表示

**使用方法:**
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

**実行順序:** パイプラインの**最初**

**重要なポイント:**
- ASP.NET Core 6以降、開発環境では自動的に追加される
- 本番環境では**絶対に使用しない**（セキュリティリスク）

**表示される情報:**
- スタックトレース
- リクエストヘッダー
- Cookie
- クエリパラメータ

---

#### 4.2 Exception Handler（例外ハンドラー）

**目的:** 本番環境でのエラーハンドリング

**使用方法:**
```csharp
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/error");
}
```

**実行順序:** パイプラインの早い段階

**実例:**
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
{
            await context.Response.WriteAsJsonAsync(new
            {
                error = "An internal server error occurred",
                details = app.Environment.IsDevelopment()
                    ? error.Error.Message
                    : null
            });
        }
    });
});
```

---

### 5. パフォーマンス系ミドルウェア

#### 5.1 Response Caching（レスポンスキャッシング）

**目的:** レスポンスをキャッシュしてパフォーマンス向上

**使用方法:**
```csharp
builder.Services.AddResponseCaching();

app.UseResponseCaching();
```

**実行順序:** キャッシュを必要とするコンポーネントの前

**実例:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60)]  // 60秒キャッシュ
    public IActionResult GetProducts()
    {
        return Ok(new[] { "Product1", "Product2" });
    }
}
```

**生成されるヘッダー:**
```
Cache-Control: public, max-age=60
```

---

#### 5.2 Response Compression（レスポンス圧縮）

**目的:** レスポンスを圧縮して転送量を削減

**使用方法:**
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

app.UseResponseCompression();
```

**実行順序:** 圧縮を必要とするコンポーネントの前

**圧縮アルゴリズム:**
- **Brotli**: 最高の圧縮率（推奨）
- **Gzip**: 広い互換性

---

#### 5.3 Request Decompression（リクエスト解凍）

**目的:** 圧縮されたリクエストボディを解凍

**使用方法:**
```csharp
builder.Services.AddRequestDecompression();

app.UseRequestDecompression();
```

**実行順序:** リクエストボディを読み取るコンポーネントの前

**対応形式:**
- Gzip
- Deflate
- Brotli

---

### 6. 診断・監視系ミドルウェア

#### 6.1 HTTP Logging（HTTPロギング）

**目的:** HTTPリクエスト/レスポンスをログに記録

**使用方法:**
```csharp
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.All;
});

app.UseHttpLogging();
```

**実行順序:** パイプラインの最初

**ログ出力例:**
```
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[1]
      Request:
      Protocol: HTTP/1.1
      Method: GET
      Path: /api/users
      Headers:
      Accept: application/json
```

**重要なポイント:**
- パフォーマンスへの影響に注意
- 本番環境では必要な情報のみログ

---

#### 6.2 Health Check（ヘルスチェック）

**目的:** アプリケーションの健全性を監視

**使用方法:**
```csharp
builder.Services.AddHealthChecks();

app.MapHealthChecks("/health");
```

**詳細:** [HealthCheck.md](./HealthCheck.md)を参照

---

#### 6.3 Diagnostics（診断）

**目的:** 診断情報とステータスページを提供

**使用方法:**
```csharp
app.UseStatusCodePages();  // 404, 500などのステータスページ
```

**実例:**
```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

---

### 7. リクエスト処理系ミドルウェア

#### 7.1 Forwarded Headers（転送ヘッダー）

**目的:** プロキシ/ロードバランサー経由のリクエストで元の情報を取得

**使用方法:**
```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

**実行順序:** パイプラインの**最初**

**重要なポイント:**
- リバースプロキシ（Nginx、HAProxy等）の背後で動作する場合に必須
- クライアントの実際のIPアドレスを取得

**処理されるヘッダー:**
- `X-Forwarded-For`: 元のクライアントIP
- `X-Forwarded-Proto`: 元のプロトコル（http/https）
- `X-Forwarded-Host`: 元のホスト名

---

#### 7.2 HTTP Method Override（HTTPメソッド上書き）

**目的:** POSTリクエストを他のHTTPメソッド（PUT、DELETE等）として扱う

**使用方法:**
```csharp
app.UseHttpMethodOverride();
```

**実行順序:** メソッドを消費するコンポーネントの前

**使用例:**
```html
<!-- HTMLフォームはPUTをサポートしていないため -->
<form method="POST" action="/api/users/123">
    <input type="hidden" name="_method" value="PUT">
    <button type="submit">Update</button>
</form>
```

---

#### 7.3 Request Localization（リクエストローカライゼーション）

**目的:** 多言語対応（ユーザーの言語に応じた内容を提供）

**使用方法:**
```csharp
builder.Services.AddLocalization();

var supportedCultures = new[] { "en-US", "ja-JP", "zh-CN" };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList()
});
```

**実行順序:** ローカライゼーションが必要なコンポーネントの前

**言語の決定方法:**
1. クエリパラメータ: `?culture=ja-JP`
2. Cookie: `Culture=ja-JP`
3. `Accept-Language`ヘッダー

---

#### 7.4 Header Propagation（ヘッダー伝播）

**目的:** HTTPヘッダーを下流のHTTPリクエストに伝播

**使用方法:**
```csharp
builder.Services.AddHeaderPropagation(options =>
{
    options.Headers.Add("X-Correlation-Id");
    options.Headers.Add("Authorization");
});

builder.Services.AddHttpClient("MyClient")
    .AddHeaderPropagation();

app.UseHeaderPropagation();
```

**実行順序:** 特定の順序要件なし

**使用シナリオ:**
- マイクロサービス間での相関ID伝播
- 認証トークンの伝播

---

#### 7.5 Session（セッション）

**目的:** ユーザーセッション状態を管理

**使用方法:**
```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

app.UseSession();
```

**実行順序:** Cookie Policyの後、セッションを使用するコンポーネントの前

**実例:**
```csharp
// セッションへの書き込み
HttpContext.Session.SetString("UserName", "Alice");

// セッションからの読み取り
var userName = HttpContext.Session.GetString("UserName");
```

---

### 8. その他のミドルウェア

#### 8.1 OWIN

**目的:** OWIN（Open Web Interface for .NET）ベースのアプリケーションと連携

**使用方法:**
```csharp
app.UseOwin(pipeline =>
{
    pipeline(next => OwinHandler);
});
```

**実行順序:** OWIN処理が必要な位置

**重要なポイント:**
- レガシーコードとの互換性のため
- 新規プロジェクトではASP.NET Coreネイティブの機能を推奨

---

## 推奨される実行順序

### 標準的なパイプライン構成

```csharp
var builder = WebApplication.CreateBuilder(args);

// サービス登録
builder.Services.AddControllers();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddResponseCompression();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ═══════════════════════════════════════════
// ミドルウェアパイプライン（推奨順序）
// ═══════════════════════════════════════════

// 1. エラー処理（最優先）
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 2. プロキシ対応（リバースプロキシ使用時）
app.UseForwardedHeaders();

// 3. HTTPS関連
app.UseHttpsRedirection();

// 4. パフォーマンス
app.UseResponseCompression();
app.UseResponseCaching();

// 5. 静的ファイル（早い段階で短絡）
app.UseStaticFiles();

// 6. ルーティング（エンドポイント選択）
app.UseRouting();

// 7. CORS（ルーティング後、認証前）
app.UseCors();

// 8. 認証（ルーティング後）
app.UseAuthentication();

// 9. 認可（認証の直後）
app.UseAuthorization();

// 10. セッション（必要に応じて）
app.UseSession();

// 11. ヘルスチェック
app.MapHealthChecks("/api/health");

// 12. エンドポイント（最後）
app.MapControllers();

app.Run();
```

### 順序が重要な理由

#### ❌ 間違った例: 認証がルーティングより前

```csharp
app.UseAuthentication();  // ← ルート情報がないため正しく動作しない
app.UseRouting();
app.UseAuthorization();
```

**問題:** 認証ミドルウェアがエンドポイント情報にアクセスできない

#### ✅ 正しい例

```csharp
app.UseRouting();         // ← エンドポイント選択
app.UseAuthentication();  // ← ルート情報を使って認証
app.UseAuthorization();   // ← 認証結果を使って認可
```

---

### WebApplicationの自動追加

ASP.NET Core 6以降、`WebApplication`は以下のミドルウェアを**自動的に追加**します：

| 条件 | 自動追加されるミドルウェア |
|------|--------------------------|
| 環境が`Development` | `UseDeveloperExceptionPage()` |
| エンドポイントが設定されている | `UseRouting()` + `UseEndpoints()` |
| `IAuthenticationSchemeProvider`検出 | `UseAuthentication()` |
| `IAuthorizationHandlerProvider`検出 | `UseAuthorization()` |

**つまり、最小限のコードでも動作:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

// UseRouting()とUseEndpoints()は自動追加される
app.MapControllers();

app.Run();
```

**明示的な制御が必要な場合:**
```csharp
var app = builder.Build();

app.UseStaticFiles();    // 静的ファイルを先に処理
app.UseRouting();        // ← 明示的に位置を指定
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

---

## PowerToolsでの使用例

### 現在のProgram.cs

PowerToolsでは、以下のミドルウェアを使用しています：

```csharp
using System.Text;
using PowerTools.Server.Services;

// Register code pages encoding provider for Shift_JIS, EUC-JP, etc.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<EncodingDetectionService>();

// Add health checks
builder.Services.AddHealthChecks();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/api/health");

app.MapControllers();

app.Run();
```

### 使用中のミドルウェア解説

| ミドルウェア | 目的 | PowerToolsでの役割 |
|------------|------|-------------------|
| `UseDeveloperExceptionPage()` | 開発時エラー表示 | 自動追加（Development環境） |
| `UseSwagger()` | Swagger仕様生成 | API仕様の自動生成 |
| `UseSwaggerUI()` | Swagger UI表示 | インタラクティブなAPI ドキュメント |
| `UseHttpsRedirection()` | HTTPS強制 | セキュアな通信の強制 |
| `UseRouting()` | ルーティング | 自動追加（MapControllers使用時） |
| `UseAuthorization()` | 認可 | 将来の認可機能のための準備 |
| `MapHealthChecks()` | ヘルスチェック | コンテナ監視用 |
| `MapControllers()` | MVC Controller | API エンドポイント実行 |

### 将来的な拡張例

PowerToolsに機能を追加する場合の例：

```csharp
var app = builder.Build();

// CORS追加（フロントエンドアプリからの呼び出しを許可）
app.UseCors(policy => policy
    .WithOrigins("https://app.example.com")
    .AllowAnyMethod()
    .AllowAnyHeader());

// レスポンス圧縮追加（パフォーマンス向上）
app.UseResponseCompression();

// 認証追加（API キー認証など）
app.UseAuthentication();

// ロギング追加（本番環境の監視）
if (app.Environment.IsProduction())
{
    app.UseHttpLogging();
}

// 既存のミドルウェア
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapHealthChecks("/api/health");
app.MapControllers();

app.Run();
```

---

## カスタムミドルウェアの作成

### 方法1: インラインミドルウェア（Use拡張メソッド）

**簡単な処理に適している:**

```csharp
app.Use(async (context, next) =>
{
    // リクエスト処理（前処理）
    Console.WriteLine($"Request: {context.Request.Path}");

    // 次のミドルウェアを呼び出し
    await next(context);

    // レスポンス処理（後処理）
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});
```

### 方法2: Run拡張メソッド（ターミナルミドルウェア）

**パイプラインを終了する場合:**

```csharp
app.Run(async context =>
{
    await context.Response.WriteAsync("Hello World!");
    // ← ここで終了、次のミドルウェアは実行されない
});
```

### 方法3: Map拡張メソッド（条件分岐）

**特定のパスでのみ実行:**

```csharp
app.Map("/api/v1", apiApp =>
{
    apiApp.Use(async (context, next) =>
    {
        context.Response.Headers.Add("API-Version", "1.0");
        await next(context);
    });

    apiApp.MapControllers();
});
```

### 方法4: ミドルウェアクラス（複雑な処理に適している）

**RequestTimingMiddleware.cs:**
```csharp
using System.Diagnostics;

namespace PowerTools.Server.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(
        RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 次のミドルウェアを呼び出し
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Request {Method} {Path} completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
    }
}

// 拡張メソッド
public static class RequestTimingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestTimingMiddleware>();
    }
}
```

**Program.csで使用:**
```csharp
app.UseRequestTiming();
```

### 方法5: IMiddlewareインターフェース（DI対応）

**ApiKeyAuthenticationMiddleware.cs:**
```csharp
public class ApiKeyAuthenticationMiddleware : IMiddleware
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationMiddleware(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key is missing");
            return;
        }

        var validApiKey = _configuration["ApiKey"];
        if (apiKey != validApiKey)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        await next(context);
    }
}
```

**Program.cs:**
```csharp
// サービス登録（IMiddlewareはScopedまたはTransient）
builder.Services.AddScoped<ApiKeyAuthenticationMiddleware>();

// 使用
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
```

---

## トラブルシューティング

### 問題1: CORSが機能しない

**症状:**
```
Access to fetch at 'https://api.example.com' from origin 'https://app.example.com'
has been blocked by CORS policy
```

**原因:**
- `UseCors()`の位置が間違っている
- ポリシーが正しく設定されていない

**解決策:**
```csharp
// ✅ 正しい順序
app.UseRouting();
app.UseCors("MyPolicy");  // UseRoutingの後、UseEndpointsの前
app.UseAuthentication();
app.MapControllers();
```

---

### 問題2: 認証が機能しない

**症状:**
```
HttpContext.User is always null
```

**原因:**
- `UseAuthentication()`が呼ばれていない
- 順序が間違っている

**解決策:**
```csharp
// ✅ 正しい順序
app.UseRouting();
app.UseAuthentication();  // UseRoutingの後
app.UseAuthorization();   // UseAuthenticationの直後
app.MapControllers();
```

---

### 問題3: 静的ファイルが404を返す

**症状:**
```
GET /css/style.css → 404 Not Found
```

**原因:**
- `UseStaticFiles()`が呼ばれていない
- `wwwroot`ディレクトリにファイルが存在しない

**解決策:**
```csharp
app.UseStaticFiles();  // 追加

// wwwroot/css/style.css が存在することを確認
```

---

### 問題4: レスポンス圧縮が動作しない

**症状:**
レスポンスが圧縮されない（`Content-Encoding`ヘッダーがない）

**原因:**
- `UseResponseCompression()`の位置が間違っている
- クライアントが`Accept-Encoding`ヘッダーを送信していない

**解決策:**
```csharp
// ✅ 圧縮を使用するコンポーネントの前に配置
app.UseResponseCompression();
app.UseStaticFiles();
app.MapControllers();
```

---

### 問題5: ミドルウェアが実行されない

**症状:**
カスタムミドルウェアのログが出力されない

**原因:**
- ミドルウェアの後に短絡するミドルウェアがある
- `next()`を呼び忘れている

**解決策:**
```csharp
// ❌ 間違い: next()を呼び忘れ
app.Use(async (context, next) =>
{
    Console.WriteLine("This runs");
    // next()が呼ばれないため、次のミドルウェアが実行されない
});

// ✅ 正しい
app.Use(async (context, next) =>
{
    Console.WriteLine("Before");
    await next(context);  // 次のミドルウェアを呼び出す
    Console.WriteLine("After");
});
```

---

## ベストプラクティス

### 1. ミドルウェアは最小限に

```csharp
// ❌ 悪い例: 不要なミドルウェアを追加
app.UseSession();        // セッション使わないのに追加
app.UseResponseCaching(); // キャッシュ使わないのに追加

// ✅ 良い例: 必要なもののみ
app.UseHttpsRedirection();
app.MapControllers();
```

### 2. 順序を理解する

```csharp
// ✅ 推奨順序テンプレート
app.UseExceptionHandler();    // 1. エラー処理
app.UseHttpsRedirection();    // 2. HTTPS
app.UseStaticFiles();         // 3. 静的ファイル
app.UseRouting();             // 4. ルーティング
app.UseAuthentication();      // 5. 認証
app.UseAuthorization();       // 6. 認可
app.MapControllers();         // 7. エンドポイント
```

### 3. カスタムミドルウェアは拡張メソッドで提供

```csharp
// ✅ 良い例
public static class CustomMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CustomMiddleware>();
    }
}

// 使用時
app.UseCustomMiddleware();
```

### 4. 環境に応じた設定

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
```

### 5. パフォーマンスを考慮

```csharp
// ✅ 静的ファイルを早い段階で処理（短絡）
app.UseStaticFiles();  // ← ファイルが見つかればここで終了
app.UseRouting();      // ← 静的ファイルリクエストは到達しない
```

---

## 参考リンク

- [ASP.NET Core Middleware - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Write custom ASP.NET Core middleware - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write)
- [ASP.NET Core Middleware Ordering](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0#middleware-order)

---

## まとめ

ASP.NET Coreのミドルウェアシステムにより、以下が実現できます：

- ✅ **モジュール性**: 機能を独立したコンポーネントとして追加
- ✅ **柔軟性**: 必要な機能のみを選択して組み込み
- ✅ **拡張性**: カスタムミドルウェアで独自機能を追加
- ✅ **パフォーマンス**: 短絡により不要な処理をスキップ
- ✅ **標準化**: Microsoftが提供する豊富な組み込みミドルウェア

ミドルウェアの順序を理解し、適切に構成することで、高性能で保守性の高いASP.NET Coreアプリケーションを構築できます。PowerToolsでは、今後の機能追加に応じて、適切なミドルウェアを追加していくことができます。
