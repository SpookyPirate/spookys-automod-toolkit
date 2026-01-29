@echo off
REM ========================================
REM {{PROJECT_NAME}} Build Script
REM ========================================
REM This script simplifies building on Windows CMD

setlocal enabledelayedexpansion

set "BUILD_DIR=build"
set "CONFIG=Release"
set "USE_VCPKG=1"

REM Parse arguments
:parse_args
if "%~1"=="" goto :end_parse_args
if /i "%~1"=="--no-vcpkg" (
    set "USE_VCPKG=0"
)
if /i "%~1"=="--debug" (
    set "CONFIG=Debug"
)
if /i "%~1"=="--clean" (
    if exist "%BUILD_DIR%" (
        echo Cleaning build directory...
        rmdir /s /q "%BUILD_DIR%"
    )
)
shift
goto :parse_args
:end_parse_args

echo ========================================
echo Building {{PROJECT_NAME}}
echo Configuration: %CONFIG%
echo VCPKG: %USE_VCPKG%
echo ========================================

REM Create build directory
if not exist "%BUILD_DIR%" (
    mkdir "%BUILD_DIR%"
)

cd "%BUILD_DIR%"

REM Configure CMake
if "%USE_VCPKG%"=="1" (
    echo Configuring with VCPKG...
    if not defined VCPKG_ROOT (
        set "VCPKG_ROOT=C:\vcpkg"
    )

    if not exist "!VCPKG_ROOT!\scripts\buildsystems\vcpkg.cmake" (
        echo ERROR: VCPKG not found at !VCPKG_ROOT!
        echo.
        echo Please install VCPKG or use --no-vcpkg flag
        echo.
        echo To install VCPKG:
        echo   git clone https://github.com/Microsoft/vcpkg.git C:\vcpkg
        echo   cd C:\vcpkg
        echo   .\bootstrap-vcpkg.bat
        echo   vcpkg integrate install
        echo   vcpkg install commonlibsse-ng:x64-windows-static
        exit /b 1
    )

    cmake .. -DCMAKE_TOOLCHAIN_FILE="!VCPKG_ROOT!\scripts\buildsystems\vcpkg.cmake"
) else (
    echo Configuring without VCPKG (using vendor/)...
    cmake ..
)

if errorlevel 1 (
    echo.
    echo ERROR: CMake configuration failed
    echo.
    echo Troubleshooting:
    echo   1. Make sure MSVC Build Tools are installed
    echo   2. Run this from "x64 Native Tools Command Prompt for VS 2022"
    echo   3. If using VCPKG, ensure dependencies are installed
    echo   4. If not using VCPKG, ensure vendor/ folder has all dependencies
    exit /b 1
)

REM Build
echo.
echo Building...
cmake --build . --config %CONFIG%

if errorlevel 1 (
    echo.
    echo ERROR: Build failed
    echo.
    echo Check the error messages above for details
    exit /b 1
)

echo.
echo ========================================
echo Build successful!
echo.
echo Output: %BUILD_DIR%\%CONFIG%\{{PROJECT_NAME}}.dll
echo.
echo To install:
echo   1. Copy the DLL to: ^<Skyrim^>\Data\SKSE\Plugins\
echo   2. Launch Skyrim with SKSE
echo ========================================

cd ..
