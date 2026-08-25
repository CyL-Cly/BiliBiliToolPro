@echo off
setlocal EnableExtensions
cd /d "%~dp0"

REM Usage: git_push.bat
REM Usage: git_push.bat your commit message
REM No args: push current branch to origin
REM With args: git add -A, commit, then push

if not "%~1"=="" (
    git add -A
    git commit -m "%*"
    if errorlevel 1 (
        echo git_push: commit failed or nothing to commit, still trying push
    )
)

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo git_push: not a git repository
    exit /b 1
)

git push -u origin HEAD
if errorlevel 1 (
    echo git_push: push failed
    exit /b 1
)

echo git_push: done
endlocal
