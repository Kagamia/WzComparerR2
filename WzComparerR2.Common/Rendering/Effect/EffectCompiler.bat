@echo off
setlocal

SET MGFX="%ProgramFiles(x86)%\MSBuild\MonoGame\v3.0\Tools\2MGFX.exe"
CALL %MGFX% "PngEffect.fx" "PngEffect.mgfxo" /Profile:DirectX_11

endlocal