@echo off

.paket\paket.exe update

rem documentation
packages\docfx.console\tools\docfx.exe docfx.json

rem TODO use dotnet build instead when this is fixed:
rem https://github.com/dotnet/cli/issues/6032
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe" src\InterrogatorLib.NETStandard.sln /p:Configuration=Release

echo build complete.
echo.
EXIT /B %ERRORLEVEL%
