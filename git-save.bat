@echo off
REM Быстрое сохранение в Git
REM Использование: git-save.bat "Коммит"

setlocal

if "%1"=="" (
    set "MSG=Auto-save %date% %time%"
) else (
    set "MSG=%*"
)

echo [Git Save] Committing changes...
git add -A
git status
git commit -m "%MSG%"
git log --oneline -3

echo.
echo [Git Save] Done.
endlocal