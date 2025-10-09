#!/bin/bash
set -e

echo "=== Cloning GGB_Buckstop (Testing branch) ==="
cd ~

if [ -d "GGB_Buckstop" ]; then
    echo "Repo already exists, pulling latest changes..."
    cd GGB_Buckstop
    git checkout Testing
    git pull origin Testing
else
    git clone -b Testing --single-branch https://github.com/ZealousKyng/GGB_Buckstop.git
    cd GGB_Buckstop
fi

echo "=== Current branch ==="
git branch --show-current

echo "✅ Repo is ready in ~/GGB_Buckstop on the Testing branch."
