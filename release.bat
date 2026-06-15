@echo off
setlocal

if "%~1"=="" (
    echo Usage: release.bat ^<version^>
    echo Example: release.bat v1.0.0
    exit /b 1
)

set VERSION=%~1

git tag %VERSION%
if errorlevel 1 (
    echo Failed to create tag. It may already exist.
    exit /b 1
)

git push origin %VERSION%
if errorlevel 1 (
    echo Failed to push tag.
    exit /b 1
)

echo.
echo Release %VERSION% triggered.
echo https://github.com/kowgli/MsGraphEmailClient/actions
