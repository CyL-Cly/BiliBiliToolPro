@echo off
setlocal EnableExtensions
cd /d "%~dp0"

REM git_push.bat [commit message]
REM   无参数：把当前分支推到 origin
REM   有参数：git add -A → commit → push

if not "%~1"=="" (
    git add -A
    git commit -m "%~1"
    if errorlevel 1 (
        echo [git_push] commit 失败或没有可提交的改动，继续尝试 push
    )
)

git rev-parse --abbrev-ref HEAD >nul 2>&1
if errorlevel 1 (
    echo [git_push] 当前目录不是 git 仓库
    exit /b 1
)

git push -u origin HEAD
if errorlevel 1 (
    echo [git_push] push 失败
    exit /b 1
)

echo [git_push] 完成
endlocal
