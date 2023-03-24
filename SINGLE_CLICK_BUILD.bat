@echo off

rem * TODO: there should be a better way to find the path to MSVC
PATH=%PATH%;"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE"

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
echo *** version info
echo ***************************************************************************

rem example: git tag = 0.13
rem          git describe = 0.13-14-g87a9f97
rem          TT_GIT = 0.13-14-g87a9f97
rem          TT_VER = 0.13.14-g87a9f97
for /f "tokens=*" %%g in ('git describe') do (set TT_GIT=%%g)

set TT_VER=%TT_GIT:-g=#g%
set TT_VER=%TT_VER:-=.%
set TT_VER=%TT_VER:#g=-g%

set _TT_VER=%TT_VER%-gdummy
set _SUFFIX=%_TT_VER:*-=%
call set TT_V0=%%_TT_VER:%_SUFFIX%=%%
set TT_V0=%TT_V0:-=%

echo *** GIT INFO:  %TT_GIT%
echo *** VERSION:   %TT_VER%
echo *** VERSION-0: %TT_V0%

pushd TuringTrader
echo internal static class GitInfo { public static string Version = "%TT_GIT%"; } > GitVersion.cs
popd

echo *
echo *
echo ***************************************************************************
echo *** build project
echo ***************************************************************************

dotnet build TuringTrader/TuringTrader.csproj -c "Release" /p:Platform=x64 /p:Version=%TT_VER%
dotnet build BooksAndPubs/BooksAndPubs.csproj -c "Release" /p:Platform=x64 /p:Version=%TT_VER%
dotnet build BooksAndPubsV2/BooksAndPubsV2.csproj -c "Release" /p:Platform=x64 /p:Version=%TT_VER%

echo *
echo *
echo ***************************************************************************
echo *** publish project
echo ***************************************************************************

dotnet publish TuringTrader\TuringTrader.csproj -c "Release" /p:Platform=x64 /p:PublishProfile=FolderProfile /p:Version=%TT_VER%

echo *
echo *
echo ***************************************************************************
echo *** build setup file
echo ***************************************************************************

dotnet build TuringTrader.Setup\TuringTrader.Setup.wixproj -c "Release" /p:Platform=x64 /p:Version=%TT_V0%

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
echo *** cleanup
echo ***************************************************************************

git restore TuringTrader/GitVersion.cs

echo *
echo *
echo ***************************************************************************
echo *** end of build
echo ***************************************************************************

start TuringTrader.Setup\bin\x64\Release
pause
