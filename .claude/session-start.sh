#!/bin/bash
# SessionStart hook for PowerTools .NET development environment
# This script runs automatically when a Claude Code session starts

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

# 環境変数を設定してローカルパッケージを使用
export NUGET_PACKAGES=./packages
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# Persist NuGet environment variables for subsequent bash commands
if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "export NUGET_PACKAGES=./packages" >> "$CLAUDE_ENV_FILE"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1" >> "$CLAUDE_ENV_FILE"
    echo "NuGet environment variables persisted to CLAUDE_ENV_FILE"
fi

# Restore NuGet packages if solution file exists (using local cache)
if [ -f "/home/user/PowerTools/PowerTools.sln" ]; then
    echo "Restoring NuGet packages from local cache..."
    cd /home/user/PowerTools
    # オフラインモードでリストア（ネットワークアクセスなし）
    dotnet restore --source ./packages --no-http-cache
    echo "NuGet packages restored successfully from local cache"
fi

echo "=== SessionStart Hook Complete ==="
echo "dotnet version: $(dotnet --version 2>/dev/null || echo 'not available yet')"
