@echo off

rem * TODO: there should be a better way to find the path to MSVC
PATH=%PATH%;"C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\Common7\IDE"

echo *
echo *
echo ***************************************************************************
echo *** clean project
echo ***************************************************************************

rem * we'd like to clean a bit more aggressively than 'dotnet clean'
rd /s /q TuringTrader\bin
rd /s /q TuringTrader\obj
rd /s /q TuringTrader.Simulator\bin
rd /s /q TuringTrader.Simulator\obj
rd /s /q BooksAndPubs\bin
rd /s /q BooksAndPubs\obj
rd /s /q TuringTrader.Setup\bin
rd /s /q TuringTrader.Setup\obj

echo *
echo *
echo ***************************************************************************
echo *** build project
echo ***************************************************************************

dotnet build TuringTrader/TuringTrader.csproj -c "Release" /p:Platform=x64
dotnet build BooksAndPubs/BooksAndPubs.csproj -c "Release" /p:Platform=x64

echo *
echo *
echo ***************************************************************************
echo *** publish project
echo ***************************************************************************

dotnet publish TuringTrader\TuringTrader.csproj -c "Release" /p:Platform=x64 /p:PublishProfile=FolderProfile

echo *
echo *
echo ***************************************************************************
echo *** build setup file
echo ***************************************************************************

rem * 'dotnet build' cannot build WiX project
devenv TuringTrader.sln /Build "Release|x64" /Project TuringTrader.Setup

echo *
echo *
echo ***************************************************************************
echo *** build documentation
echo ***************************************************************************

pushd DocFX
docfx
popd

echo *
echo *
echo ***************************************************************************
echo *** end of build
echo ***************************************************************************

start TuringTrader.Setup\bin\Release
pause
