# WzComparerR2 Agent Plugin

Integrates **WzComparerR2** (MapleStory WZ data analysis, comparison, simulation, and asset extraction tool) with supported agent hosts through Model Context Protocol (MCP) and specialized skills. This package includes the Agent Plugins `plugin.json` and local MCP `mcp.json` formats for OpenAI ChatGPT/Codex.

## Features

- **18 MCP Tools (`wcr2_*`)**:
  - `wcr2_status`, `wcr2_open_wz`, `wcr2_close_wz`, `wcr2_list_wz_types`
  - `wcr2_get_node`, `wcr2_search_nodes`, `wcr2_search_strings`
  - `wcr2_get_object_info`, `wcr2_render_tooltip`, `wcr2_avatar_render`
  - `wcr2_extract_png`, `wcr2_extract_animation`, `wcr2_extract_sound`, `wcr2_export_xml`
  - `wcr2_compare_wz`, `wcr2_export_db_csv`, `wcr2_run_lua`, `wcr2_gui_control`
- **3 Specialized Skills**:
  - `wcr2-control`: WZ tree traversal, localized string search, structured CharaSim object parsing (`Gear`, `Item`, `Skill`, `Mob`, `Map`), and live GUI control.
  - `wcr2-asset-extractor`: Authentic tooltip rendering, PNG/APNG/GIF extraction (with automatic `_inlink`/`_outlink` resolution), audio/XML/CSV export, avatar rendering, and WZ diffs.
  - `wcr2-plugin-dev`: Lua automation scripting (`LuaSandbox`) and C# plugin (`PluginEntry`) development.

## OpenAI ChatGPT / Codex

The package is listed in the repository marketplace at `.agents/plugins/marketplace.json`. Its MCP server uses stdio and runs on the user's computer, so OpenAI labels this package Desktop only. Install Node.js 18+, build/deploy the WzComparerR2 bridge from the repository root with `mcp/build-plugin.ps1`, then install the plugin through the local repository marketplace or a workspace marketplace imported by an admin.

For public OpenAI directory submission, local MCP plugins are limited to approved partners. See the [submission guide](https://developers.openai.com/plugins/deploy/desktop-only) and [integration guide](../../../INTEGRATIONS.md).

## Claude Code

The repository marketplace is at `.claude-plugin/marketplace.json`. In Claude Code, install it with:

```text
/plugin marketplace add Kagamia/WzComparerR2
/plugin install wzcomparerr2@wzcomparerr2
```

The plugin starts the bundled MCP server locally through `.mcp.json`. Build and deploy the WzComparerR2 bridge first, then open a new Claude Code session and check `/mcp` for the connection.

## Quick Start (Antigravity)

1. **Build and Deploy the Plugin**:
   Run the standalone PowerShell build script from the repository root:
   ```powershell
   powershell -ExecutionPolicy Bypass -File mcp/build-plugin.ps1
   ```
   - Automatically detects your Windows `.NET Framework` compiler (`csc.exe`) without requiring external SDKs.
   - Automatically discovers your WzComparerR2 directory or accepts an explicit `-Wcr2InstallDir <path>`.
   - Records the installation path to `~/.wcr2-mcp.json` so MCP connects with zero configuration.

2. **Load in Antigravity**:
   - The plugin configuration is defined in `mcp_config.json`.
   - Start querying MapleStory data, rendering tooltips, or controlling WzComparerR2 directly through agent prompts!

## WzComparerR2 Installation Path (`WCR2_INSTALL_DIR`)

When this plugin is installed into an agent's plugin cache (such as `~/.claude/plugins/cache/...`), repository-relative build paths (`Build/bin/Release`) are not present in the cache folder.

To ensure the MCP server and headless daemon always locate your WzComparerR2 binaries regardless of where the plugin runs, **set the `WCR2_INSTALL_DIR` environment variable**:

```powershell
# Set permanently for your user account (recommended):
[Environment]::SetEnvironmentVariable("WCR2_INSTALL_DIR", "C:\Path\To\WzComparerR2", "User")
```

Restart the agent host after changing the user environment so its MCP server process inherits the new value.

Alternatively, running `powershell -File mcp/build-plugin.ps1` automatically writes your installation directory to `~/.wcr2-mcp.json`, which the MCP server also reads.

### Path Resolution Priority:
1. `WCR2_INSTALL_DIR` environment variable
2. `~/.wcr2-mcp.json` (`installDir` field)
3. Active running `WzComparerR2.exe` / `WzComparerR2.McpHost.exe` process path
4. Walk-up search for `WzComparerR2.sln` repository root (`Build/bin/Release`)
5. Standard candidate paths (`C:\Nexon`, `D:\Nexon`, `C:\MapleStory`, `%USERPROFILE%`)

## Platform Configuration Files Reference

This package includes multi-platform manifests so it can be installed natively across various agent platforms:

| Platform | Manifest | MCP Config | Notes |
| :--- | :--- | :--- | :--- |
| **Claude Code** | `.claude-plugin/plugin.json` | `.mcp.json` | Uses `${CLAUDE_PLUGIN_ROOT}` for cwd-independent MCP execution |
| **OpenAI ChatGPT / Codex** | `plugin.json` | `mcp.json` | Conforms to `agent-plugins.org/schemas/1.0.0/` schema |
| **Antigravity** | `plugin.json` | `mcp_config.json` | Uses `${PLUGIN_ROOT}` so MCP startup does not depend on the current working directory |
