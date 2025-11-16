#!/bin/bash

echo "========================================"
echo "   清理并编译 OpenUO"
echo "========================================"
echo ""

echo "[0/3] 检查并关闭运行中的游戏..."
if pgrep -x "OpenUO" > /dev/null; then
    echo "发现运行中的 OpenUO，正在关闭..."
    pkill -9 OpenUO
    sleep 2
    echo "已关闭游戏进程"
else
    echo "没有运行中的游戏进程"
fi

echo ""
echo "[1/2] 编译项目..."
dotnet build --configuration Debug --no-restore --verbosity minimal

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================"
    echo "   ✓ 编译成功！"
    echo "========================================"
    echo ""
    echo "[2/2] 启动项目..."
    echo ""
    cd bin/Debug/net9.0/osx-arm64/
    rm -rf Data/Profiles/
    ./OpenUO
    # mv bin/Debug/net9.0/osx-arm64/
    echo "游戏已启动！"
    echo ""
else
    echo ""
    echo "[错误] 编译失败！"
    echo ""
    exit 1
fi
