PATH=%PATH%;"C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\Common7\IDE"

devenv TuringTrader.sln /Clean "Release|x64"
devenv TuringTrader.sln /Build "Release|x64" /Project TuringTrader
devenv TuringTrader.sln /Build "Release|x64" /Project BooksAndPubs

dotnet publish TuringTrader\TuringTrader.csproj /p:PublishProfile=FolderProfile
devenv TuringTrader.sln /Build "Release|x64" /Project TuringTrader.Setup

pushd DocFX
docfx
popd

pause
