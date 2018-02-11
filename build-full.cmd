@echo off

set config=Release

call:exec "%~dp0tools\nuget.exe" restore || exit /b 1
call:exec msbuild NUnitToMSTest.sln /p:Configuration=Release || exit /b 1
call:exec vstest.console.exe .\NUnitToMSTest.Tests\bin\Release\NUnitToMSTest.Tests.dll || exit /b 1
exit /b 0

:exec
    echo.======================================================================
    echo.    %*
    echo.======================================================================
    %* || exit /b 1
    goto:EOF