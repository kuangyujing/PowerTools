# PowerTools ユニットテスト ガイド

このドキュメントでは、PowerToolsプロジェクトでユニットテストを包括的に実行する方法を説明します。

## 目次
- [前提条件](#前提条件)
- [テストの実行方法](#テストの実行方法)
- [コードカバレッジ](#コードカバレッジ)
- [テストのフィルタリング](#テストのフィルタリング)
- [CI/CD環境での実行](#cicd環境での実行)
- [テストの構造](#テストの構造)
- [テストの作成](#テストの作成)
- [トラブルシューティング](#トラブルシューティング)

---

## 前提条件

以下がインストールされていることを確認してください：
- **.NET 8 SDK**: バージョン 8.0 以降

インストール確認：
```bash
dotnet --version
```

---

## テストの実行方法

### 基本的なテスト実行

```bash
# すべてのテストを実行
dotnet test

# 詳細な出力でテストを実行
dotnet test --verbosity normal

# さらに詳細な出力
dotnet test --verbosity detailed
```

### プロジェクト指定でのテスト実行

```bash
# ソリューション全体のテスト
dotnet test PowerTools.sln

# 特定のテストプロジェクトのみ
dotnet test PowerTools.Tests/
```

### ビルドをスキップしてテスト実行

```bash
# 既にビルド済みの場合（高速）
dotnet test --no-build
```

---

## コードカバレッジ

### カバレッジの収集

PowerToolsプロジェクトでは、`coverlet.collector` を使用してコードカバレッジを収集します。

```bash
# カバレッジ付きでテストを実行
dotnet test --collect:"XPlat Code Coverage"

# 出力先を指定
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### カバレッジレポートの確認

テスト実行後、`TestResults/` ディレクトリに `coverage.cobertura.xml` が生成されます。

```bash
# 生成されたカバレッジファイルを確認
ls TestResults/*/coverage.cobertura.xml
```

### HTMLレポートの生成（オプション）

より見やすいHTMLレポートを生成するには、`ReportGenerator` ツールを使用します：

```bash
# ReportGeneratorをインストール
dotnet tool install -g dotnet-reportgenerator-globaltool

# HTMLレポートを生成
reportgenerator \
  -reports:"TestResults/*/coverage.cobertura.xml" \
  -targetdir:"CoverageReport" \
  -reporttypes:Html

# レポートを開く（macOS）
open CoverageReport/index.html
```

### カバレッジの閾値設定

CI/CDでカバレッジの閾値を設定する場合：

```bash
# カバレッジが80%未満なら失敗
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

---

## テストのフィルタリング

### 特定のテストを実行

```bash
# テスト名でフィルタ
dotnet test --filter "FullyQualifiedName~ConvertEncoding"

# 特定のテストメソッドを実行
dotnet test --filter "ConvertEncoding_UTF8ToShiftJIS_Success"

# クラス名でフィルタ
dotnet test --filter "ClassName=PowerTools.Tests.EncodingConverterControllerTests"
```

### トレイト（カテゴリ）でフィルタ

テストにカテゴリを付けている場合：

```bash
# カテゴリでフィルタ
dotnet test --filter "Category=Unit"
dotnet test --filter "Category!=Integration"
```

### 複合フィルタ

```bash
# AND条件
dotnet test --filter "ClassName=EncodingConverterControllerTests&Method~Success"

# OR条件
dotnet test --filter "Method~Success|Method~Error"
```

---

## CI/CD環境での実行

### GitHub Actions用の設定例

```yaml
# .github/workflows/test.yml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test with coverage
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./TestResults

      - name: Upload coverage reports
        uses: codecov/codecov-action@v4
        with:
          files: ./TestResults/*/coverage.cobertura.xml
```

### Azure DevOps用の設定例

```yaml
# azure-pipelines.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'

  - script: dotnet test --collect:"XPlat Code Coverage"
    displayName: 'Run tests with coverage'

  - task: PublishCodeCoverageResults@2
    inputs:
      codeCoverageTool: 'Cobertura'
      summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
```

### JUnitフォーマットでの出力

CI/CDでテスト結果を解析する場合：

```bash
dotnet test --logger "trx;LogFileName=test-results.trx"
dotnet test --logger "junit;LogFileName=test-results.xml"
```

---

## テストの構造

### プロジェクト構成

```
PowerTools/
├── PowerTools.Server/     # メインプロジェクト
│   ├── Controllers/
│   ├── Models/
│   └── Services/
└── PowerTools.Tests/      # テストプロジェクト
    ├── PowerTools.Tests.csproj
    ├── EncodingConverterControllerTests.cs
    └── UnitTest1.cs
```

### 使用フレームワーク

| パッケージ | 用途 |
|-----------|------|
| xUnit | テストフレームワーク |
| coverlet.collector | コードカバレッジ収集 |
| Microsoft.NET.Test.Sdk | テストSDK |

---

## テストの作成

### 基本的なテストの書き方

```csharp
using Xunit;

namespace PowerTools.Tests;

public class MyControllerTests
{
    [Fact]
    public void MyMethod_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange - テストの準備
        var controller = new MyController();
        var input = "test";

        // Act - テスト対象の実行
        var result = controller.MyMethod(input);

        // Assert - 結果の検証
        Assert.NotNull(result);
        Assert.Equal("expected", result);
    }
}
```

### パラメータ化テスト

```csharp
[Theory]
[InlineData("UTF-8", "Shift_JIS")]
[InlineData("UTF-8", "UTF-16")]
[InlineData("Shift_JIS", "UTF-8")]
public void ConvertEncoding_WithVariousEncodings_Success(
    string inputEncoding,
    string outputEncoding)
{
    // テストコード
}
```

### 例外テスト

```csharp
[Fact]
public void MyMethod_WithInvalidInput_ThrowsException()
{
    var controller = new MyController();

    Assert.Throws<ArgumentException>(() =>
        controller.MyMethod(null));
}
```

### 非同期テスト

```csharp
[Fact]
public async Task MyAsyncMethod_ReturnsData()
{
    var service = new MyService();

    var result = await service.GetDataAsync();

    Assert.NotEmpty(result);
}
```

---

## トラブルシューティング

### 問題: テストが検出されない

**確認事項:**
```bash
# プロジェクトのビルドを確認
dotnet build PowerTools.Tests/

# テスト一覧を表示
dotnet test --list-tests
```

**よくある原因:**
- `[Fact]` または `[Theory]` 属性が付いていない
- テストメソッドが `public` でない
- テストクラスが `public` でない

### 問題: テストが失敗する

**詳細なログを確認:**
```bash
dotnet test --verbosity detailed --logger "console;verbosity=detailed"
```

### 問題: カバレッジが収集されない

**確認事項:**
- `coverlet.collector` パッケージがインストールされているか確認
- .csproj ファイルに以下が含まれているか確認:
  ```xml
  <PackageReference Include="coverlet.collector" Version="6.0.0" />
  ```

### 問題: テストが遅い

**解決策:**
```bash
# 並列実行を有効化（デフォルトで有効）
dotnet test

# 並列実行を無効化（デバッグ時）
dotnet test -- xUnit.parallelizeTestCollections=false
```

---

## コマンドリファレンス

| コマンド | 説明 |
|---------|------|
| `dotnet test` | すべてのテストを実行 |
| `dotnet test --verbosity normal` | 詳細出力でテスト実行 |
| `dotnet test --no-build` | ビルドスキップでテスト実行 |
| `dotnet test --collect:"XPlat Code Coverage"` | カバレッジ収集付きでテスト実行 |
| `dotnet test --filter "Name~Test"` | フィルタ付きでテスト実行 |
| `dotnet test --list-tests` | テスト一覧を表示 |
| `dotnet test --logger "trx"` | TRX形式で結果を出力 |

---

## まとめ

PowerToolsのユニットテストを効果的に活用するためのポイント：

1. **定期的なテスト実行**: 変更後は必ず `dotnet test` を実行
2. **カバレッジの監視**: 新機能追加時はカバレッジを確認
3. **CI/CDへの統合**: 自動テストでコード品質を維持
4. **テストの保守**: テストコードも本番コードと同様に保守

詳細な情報は [xUnit公式ドキュメント](https://xunit.net/) を参照してください。
