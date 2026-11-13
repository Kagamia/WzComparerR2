param(
    [string]$Wcr2InstallDir = $env:WCR2_INSTALL_DIR
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$McpSrcDir = Join-Path $RepoRoot "WzComparerR2.Mcp"
$UserConfigPath = Join-Path $HOME ".wcr2-mcp.json"

function Resolve-Wcr2InstallDir {
    param([string]$ExplicitDir)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitDir) -and (Test-Path (Join-Path $ExplicitDir "WzComparerR2.exe"))) {
        return (Resolve-Path $ExplicitDir).Path
    }

    if (Test-Path $UserConfigPath) {
        try {
            $cfg = Get-Content $UserConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($cfg.installDir -and (Test-Path (Join-Path $cfg.installDir "WzComparerR2.exe"))) {
                return (Resolve-Path $cfg.installDir).Path
            }
        } catch {}
    }

    $proc = Get-Process -Name "WzComparerR2" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc -and $proc.Path) {
        $procDir = Split-Path -Parent $proc.Path
        if (Test-Path (Join-Path $procDir "WzComparerR2.exe")) {
            return $procDir
        }
    }

    $localCandidates = @(
        (Join-Path $RepoRoot "Build\bin\Release"),
        (Join-Path $RepoRoot "Build\bin\Debug"),
        (Join-Path $RepoRoot "WzComparerR2\bin\Release"),
        (Join-Path $RepoRoot "WzComparerR2\bin\Debug")
    )
    foreach ($cand in $localCandidates) {
        if (Test-Path (Join-Path $cand "WzComparerR2.exe")) {
            return (Resolve-Path $cand).Path
        }
    }

    $searchRoots = @($HOME, (Join-Path $HOME "Desktop"), (Join-Path $HOME "Downloads"), "C:\Nexon", "D:\Nexon", "C:\MapleStory", "D:\MapleStory")
    foreach ($root in $searchRoots) {
        if ($root -and (Test-Path $root)) {
            $found = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                $exe = Join-Path $_.FullName "WzComparerR2.exe"
                if (Test-Path $exe) { $_.FullName }
            } | Select-Object -First 1
            if ($found) {
                return $found
            }
        }
    }

    throw "Could not auto-detect WzComparerR2 installation directory. Pass -Wcr2InstallDir <path> or set WCR2_INSTALL_DIR."
}

function Resolve-CscPath {
    $candidates = @(
        (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
        (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }
    throw "csc.exe not found under $env:WINDIR\Microsoft.NET"
}

$Wcr2InstallDir = Resolve-Wcr2InstallDir -ExplicitDir $Wcr2InstallDir
$CscPath = Resolve-CscPath

$LibDir = if (Test-Path (Join-Path $Wcr2InstallDir "Lib\WzComparerR2.Common.dll")) {
    Join-Path $Wcr2InstallDir "Lib"
} else {
    $Wcr2InstallDir
}

$PluginOutDir = Join-Path $Wcr2InstallDir "Plugin\WzComparerR2.Mcp"
$LocalBinDir = Join-Path $McpSrcDir "bin\Release\net462"

New-Item -ItemType Directory -Force -Path $PluginOutDir | Out-Null
New-Item -ItemType Directory -Force -Path $LocalBinDir | Out-Null

$Refs = @(
    "/r:$Wcr2InstallDir\WzComparerR2.exe",
    "/r:$LibDir\WzComparerR2.Common.dll",
    "/r:$LibDir\WzComparerR2.PluginBase.dll",
    "/r:$LibDir\WzComparerR2.WzLib.dll",
    "/r:$LibDir\CharaSimResource.dll",
    "/r:$LibDir\DevComponents.DotNetBar2.dll",
    "/r:$LibDir\Newtonsoft.Json.dll",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Data.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Xml.dll",
    "/r:System.Configuration.dll"
)

$OptionalPluginDlls = @(
    "Plugin\WzComparerR2.Avatar\WzComparerR2.Avatar.dll",
    "Plugin\WzComparerR2.LuaConsole\WzComparerR2.LuaConsole.dll",
    "Plugin\WzComparerR2.LuaConsole\NLua.dll",
    "Plugin\WzComparerR2.LuaConsole\KeraLua.dll",
    "WzComparerR2.Avatar.dll",
    "WzComparerR2.LuaConsole.dll",
    "NLua.dll",
    "KeraLua.dll"
)
$AddedDllNames = @{}
foreach ($rel in $OptionalPluginDlls) {
    $full = Join-Path $Wcr2InstallDir $rel
    $name = Split-Path -Leaf $full
    if ((Test-Path $full) -and (-not $AddedDllNames.ContainsKey($name))) {
        $Refs += "/r:$full"
        $AddedDllNames[$name] = $true
    }
}

$PluginSources = @(
    "$McpSrcDir\Properties\AssemblyInfo.cs",
    "$McpSrcDir\Entry.cs",
    "$McpSrcDir\Wcr2BridgeServer.cs",
    "$McpSrcDir\Wcr2Engine.cs"
)

$HostSources = $PluginSources + @("$McpSrcDir\HostProgram.cs")

$DllOut = Join-Path $PluginOutDir "WzComparerR2.Mcp.dll"
$ExeOut = Join-Path $Wcr2InstallDir "WzComparerR2.McpHost.exe"

Stop-Process -Name "WzComparerR2.McpHost" -Force -ErrorAction SilentlyContinue

Write-Host "Target WzComparerR2 directory: $Wcr2InstallDir"
Write-Host "Building WzComparerR2.Mcp.dll -> $DllOut"
& $CscPath /nologo /target:library /optimize+ /out:$DllOut $Refs $PluginSources
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile WzComparerR2.Mcp.dll (exit code $LASTEXITCODE)"
}

Write-Host "Building WzComparerR2.McpHost.exe -> $ExeOut"
& $CscPath /nologo /target:exe /optimize+ /out:$ExeOut $Refs $HostSources
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile WzComparerR2.McpHost.exe (exit code $LASTEXITCODE)"
}

Copy-Item -Force $DllOut (Join-Path $LocalBinDir "WzComparerR2.Mcp.dll")
Copy-Item -Force $ExeOut (Join-Path $LocalBinDir "WzComparerR2.McpHost.exe")

$HostConfigPath = "$ExeOut.config"
$HostConfigXml = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.6.2" />
  </startup>
  <runtime>
    <loadFromRemoteSources enabled="true"/>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <probing privatePath="Lib;Plugin\WzComparerR2.Avatar;Plugin\WzComparerR2.LuaConsole;Plugin\WzComparerR2.MapRender;Plugin\WzComparerR2.Network;Plugin\WzComparerR2.Mcp"/>
      <dependentAssembly>
        <assemblyIdentity name="System.Resources.Extensions" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-8.0.0.0" newVersion="8.0.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
'@
Set-Content -Path $HostConfigPath -Value $HostConfigXml -Encoding UTF8
Set-Content -Path (Join-Path $LocalBinDir "WzComparerR2.McpHost.exe.config") -Value $HostConfigXml -Encoding UTF8

$McpServerPath = Join-Path $PSScriptRoot "wcr2-mcp-server.mjs"
$ConfigObj = @{
    installDir = $Wcr2InstallDir
    mcpServerPath = $McpServerPath
    updatedAt = (Get-Date).ToString("o")
}
$ConfigObj | ConvertTo-Json | Set-Content -Path $UserConfigPath -Encoding UTF8
Write-Host "Saved user MCP config -> $UserConfigPath"
Write-Host "Build succeeded!"
