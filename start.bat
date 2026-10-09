@echo off
title Antigravity Token Monitor
cd /d "%~dp0AntigravityTokens\bin\Debug\net8.0-windows10.0.19041.0\win-x64"
if exist "AntigravityTokens.exe" (
    start "" "AntigravityTokens.exe"
) else (
    cd /d "%~dp0AntigravityTokens\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64"
    start "" "AntigravityTokens.exe"
)
