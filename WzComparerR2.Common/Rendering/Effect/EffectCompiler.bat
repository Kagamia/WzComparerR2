@echo off
setlocal

dotnet tool install --global dotnet-mgfxc --version 3.8.0.1641
mgfxc "PngEffect.fx" "PngEffect.mgfxo" /Profile:DirectX_11

endlocal