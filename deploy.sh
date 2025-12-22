#!/usr/bin/env bash
set -e

APP_NAME="protabula"
APP_DIR="/var/www/protabula_com"
PUBLISH_DIR="$APP_DIR/app"
BUILD_DIR="$APP_DIR/bin/Release/net10.0/publish"

echo "🚀 Deploying $APP_NAME..."

echo "📥 Pulling latest code"
git pull

echo "🛠️ Publishing .NET app"
dotnet publish -c Release

echo "📦 Syncing publish output"
sudo rsync -a --delete "$BUILD_DIR/" "$PUBLISH_DIR/"

echo "🔁 Restarting service"
sudo systemctl restart $APP_NAME

echo "✅ Deploy completed successfully"

