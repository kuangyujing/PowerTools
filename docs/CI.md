# PowerTools CI/CD ガイド

このドキュメントでは、PowerToolsプロジェクトのCI（継続的インテグレーション）について説明します。

## 目次
- [概要](#概要)
- [ワークフロー構成](#ワークフロー構成)
- [トリガー](#トリガー)
- [実行ステップ](#実行ステップ)
- [手動実行](#手動実行)
- [テスト結果の確認](#テスト結果の確認)
- [トラブルシューティング](#トラブルシューティング)

---

## 概要

PowerToolsでは、GitHub Actionsを使用してCIを実装しています。主な目的は以下の通りです：

- コードの品質を維持する
- プルリクエスト時に自動テストを実行する
- テストカバレッジを収集・可視化する

---

## ワークフロー構成

### ファイル

```
.github/
└── workflows/
    └── test.yml    # テスト実行ワークフロー
```

### 使用環境

| 項目 | 値 |
|------|-----|
| ランナー | ubuntu-latest |
| .NET SDK | 8.0.x |
| ビルド構成 | Release |

---

## トリガー

ワークフローは以下のタイミングで実行されます：

### 1. プルリクエスト

`main` ブランチへのプルリクエストが作成・更新されたとき自動実行されます。

```yaml
on:
  pull_request:
    branches: [main]
```

### 2. 手動実行（workflow_dispatch）

GitHub UIまたはGitHub CLIから手動で実行できます。

```yaml
on:
  workflow_dispatch:
```

---

## 実行ステップ

ワークフローは以下のステップで構成されています：

### 1. Checkout repository
リポジトリのコードをチェックアウトします。

### 2. Setup .NET
.NET 8 SDKをインストールします。

### 3. Restore dependencies
`dotnet restore` でNuGetパッケージを復元します。

### 4. Build
`dotnet build` でReleaseビルドを実行します。

### 5. Run tests
`dotnet test` でユニットテストを実行し、コードカバレッジを収集します。

### 6. Upload test results
テスト結果をアーティファクトとしてアップロードします（30日間保持）。

### 7. Upload coverage to Codecov
カバレッジレポートをCodecovにアップロードします（オプション）。

---

## 手動実行

### GitHub UIから実行

1. GitHubリポジトリの **Actions** タブを開く
2. 左側のワークフロー一覧から **Test** を選択
3. **Run workflow** ボタンをクリック
4. ブランチを選択して **Run workflow** を実行

### GitHub CLIから実行

```bash
# ワークフローを手動実行
gh workflow run test.yml

# 特定のブランチで実行
gh workflow run test.yml --ref feature/my-branch

# 実行状況を確認
gh run list --workflow=test.yml
```

---

## テスト結果の確認

### PRでの確認

プルリクエストを作成すると、自動的にテストが実行されます。結果は以下で確認できます：

1. PRページの **Checks** タブ
2. コミットステータス（緑のチェックマークまたは赤のX）

### アーティファクトのダウンロード

テスト結果とカバレッジレポートはアーティファクトとして保存されます：

1. **Actions** タブでワークフロー実行を選択
2. **Artifacts** セクションから `test-results` をダウンロード
3. `coverage.cobertura.xml` でカバレッジを確認

### Codecovでの確認（オプション）

Codecovを設定している場合、PRにカバレッジレポートがコメントされます。

---

## トラブルシューティング

### 問題: テストが失敗する

**確認事項:**
1. ローカルで `dotnet test` が成功するか確認
2. Actions タブで詳細なログを確認
3. 環境依存の問題（パス、OS）がないか確認

**ローカルでの再現:**
```bash
# CIと同じ条件でテスト
dotnet build --configuration Release
dotnet test --no-build --configuration Release --verbosity normal
```

### 問題: ワークフローが実行されない

**確認事項:**
1. PRのターゲットブランチが `main` か確認
2. ワークフローファイルの構文エラーがないか確認
3. リポジトリのActionsが有効か確認（Settings > Actions）

### 問題: カバレッジがアップロードされない

**確認事項:**
1. Codecovトークンが設定されているか確認（必要な場合）
2. `coverage.cobertura.xml` が生成されているか確認

---

## カスタマイズ

### カバレッジ閾値の追加

テストカバレッジが一定以下の場合にCIを失敗させるには：

```yaml
- name: Run tests with threshold
  run: dotnet test --no-build --configuration Release /p:CollectCoverage=true /p:Threshold=80
```

### ブランチ保護ルールの設定

`main` ブランチへのマージ前にCIを必須にするには：

1. リポジトリの **Settings** > **Branches** を開く
2. **Add branch protection rule** をクリック
3. Branch name pattern に `main` を入力
4. **Require status checks to pass before merging** を有効化
5. **Run Tests** を必須チェックとして追加

---

## 関連ドキュメント

- [Testing.md](./Testing.md) - ユニットテストの詳細
- [Docker.md](./Docker.md) - Docker環境でのテスト実行
