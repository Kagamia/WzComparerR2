---
name: wcr2-plugin-dev
description: Develop, compile, and run Lua automation scripts (LuaSandbox) and C# plugins (PluginEntry) for WzComparerR2.
---

# WzComparerR2 Lua Scripting & C# Plugin Development Skill (`wcr2-plugin-dev`)

Use this skill when the user wants to:
- Write or execute custom **Lua scripts** inside WzComparerR2's `LuaConsole` (`LuaSandbox`) via `wcr2_run_lua`.
- Extend the **`WzComparerR2.Mcp`** bridge or develop new **C# plugins** (`WzComparerR2.PluginBase.PluginEntry`).

## 1. Running Lua Scripts via `wcr2_run_lua`

`wcr2_run_lua` executes Lua code inside `WzComparerR2.LuaConsole.LuaSandbox` with CLR interop (`NLua`) pre-initialized:
- `env:WriteLine(value)` — Appends a line to the returned `output` string.
- `PluginManager.FindWz(path)` — Resolves any WZ node across open WZ files (e.g. `PluginManager.FindWz("Mob/8880140.img/info/level")`).
- `import('WzComparerR2.WzLib')`, `import('WzComparerR2.Common')`, `import('WzComparerR2.CharaSimControl')` — Grants direct access to all WzComparerR2 .NET types.

### Example: Batch Query via `wcr2_run_lua`
```lua
import 'WzComparerR2.WzLib'
import 'WzComparerR2.Common'

local mobNode = PluginManager.FindWz("Mob/8880140.img")
if mobNode then
    local img = mobNode:GetNodeWzImage()
    if img and img:TryExtract() then
        local info = img.Node:FindNodeByPath("info")
        for _, child in each(info.Nodes) do
            env:WriteLine(child.Text .. " = " .. tostring(child.Value))
        end
    end
end
```

## 2. Building & Extending `WzComparerR2.Mcp` or Custom C# Plugins

### Plugin Discovery Rules in `WzComparerR2.exe`
`WzComparerR2` discovers plugins matching:
`Plugin/<PluginFolder>/<PluginFolder>.dll` where the folder name and `.dll` base name match (e.g., `<WCR2_INSTALL_DIR>\Plugin\WzComparerR2.Mcp\WzComparerR2.Mcp.dll`).

Each plugin defines a subclass of `WzComparerR2.PluginBase.PluginEntry`:
```csharp
using WzComparerR2.PluginBase;

namespace WzComparerR2.Mcp
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context) : base(context) { }

        protected override void OnLoad()
        {
            // Access this.Context.MainForm, this.Context.DefaultStringLinker,
            // this.Context.DefaultTooltipWindow, and register RibbonBars or event handlers.
        }
    }
}
```

### Compiling and Deploying the Plugin

You can compile and deploy `WzComparerR2.Mcp.dll` and `WzComparerR2.McpHost.exe` using either:

1. **Standalone PowerShell Build Script** (Auto-detects Windows `.NET Framework` `csc.exe` and installation folder without requiring external SDK installs):
   ```powershell
   powershell -ExecutionPolicy Bypass -File mcp/build-plugin.ps1
   # Or specify an explicit target installation directory:
   powershell -ExecutionPolicy Bypass -File mcp/build-plugin.ps1 -Wcr2InstallDir "C:\Path\To\WzComparerR2"
   ```
2. **MSBuild / Visual Studio**:
   Build `WzComparerR2.Mcp/WzComparerR2.Mcp.csproj` in Release configuration.

> [!NOTE]
> If `WzComparerR2.McpHost.exe` is currently running on port `21481`, stop it before rebuilding (`Stop-Process -Name "WzComparerR2.McpHost" -Force -ErrorAction SilentlyContinue`) so the executable is not locked during file copy. Keep C# source files in `WzComparerR2.Mcp/` compatible with C# 5 syntax so standalone `csc.exe` builds work seamlessly on all Windows machines.
