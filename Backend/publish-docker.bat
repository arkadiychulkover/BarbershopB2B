@echo off
chcp 65001 > nul
echo ========================================================
echo   Building and Pushing Backend Docker Image to Docker Hub
echo   Target Image: arkadii228555/barbershop-backend:latest
echo ========================================================
echo.

cd /d "%~dp0"

echo [1/2] Building Docker image...
docker build -t arkadii228555/barbershop-backend:latest -f Dockerfile .

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Docker build failed!
    echo Please make sure Docker Desktop is launched and running.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Pushing to Docker Hub...
docker push arkadii228555/barbershop-backend:latest

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Docker push failed!
    echo Please make sure you are logged in (docker login).
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo   Successfully built and pushed to Docker Hub!
echo   Image: arkadii228555/barbershop-backend:latest
echo ========================================================
pause
