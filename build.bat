@echo off

set /A RETURN_CODE=0

if not exist "%~dp0\Thirdparty\ACT\Advanced Combat Tracker.exe" (
	echo ERROR: %~dp0\Thirdparty\Advanced Combat Tracker.exe is not found.
	set /A RETURN_CODE=1
	goto END
)


set DOTNET_PATH=%windir%\Microsoft.NET\Framework\v4.0.30319
if not exist %DOTNET_PATH% (
	echo ERROR: Required build tool is not found. Please install build tools for .NET Framework 4.5.1.
	set /A RETURN_CODE=1
	goto END
)

%DOTNET_PATH%\msbuild /t:Rebuild /p:Configuration=Release /p:VisualStudioVersion=14.0 "%~dp0\Source\ActServer.sln"

if %ERRORLEVEL% neq 0 (
	echo ERROR: Build failed with code %ERRORLEVEL%.
	set /A RETURN_CODE=1
	goto END
)

:END

exit /B %RETURN_CODE%
