@echo off
setlocal

if [%1]==[] goto error
set monogame_ver=%1

:check
@echo Check dotnet-mgfxc tool...
dotnet tool list --global | findstr "dotnet-mgfxc" || goto install

:exists
dotnet tool list --global | findstr "dotnet-mgfxc.*%monogame_ver%" >nul 2>&1 || goto uninstall
goto build

:uninstall
dotnet tool uninstall --global dotnet-mgfxc || goto failed

:install
dotnet tool install --global dotnet-mgfxc --version %monogame_ver% || goto failed

:build
mgfxc "PngEffect.fx" "PngEffect.%monogame_ver%.mgfxo" /Profile:DirectX_11 || goto failed
goto :eof

:error
@echo %0 [monogame_ver]
@echo    monogame_ver: 3.8.0.1641/3.8.1.303
goto :failed

:failed
exit /B 1

endlocal