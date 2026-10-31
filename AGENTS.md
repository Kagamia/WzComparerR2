# WzComparerR2 — Agent Instructions & Architecture Guide

This workspace contains **WzComparerR2**, a MapleStory WZ/MsFile data analysis, comparison, simulation (`CharaSim`), and asset extraction tool, along with the **WzComparerR2 MCP & Agent Bridge (`WzComparerR2.Mcp`)**.

## 1. Portable Environment & Dynamic Path Discovery
The MCP server, CLI companion, and build scripts dynamically auto-detect paths across environments:
- **WzComparerR2 Installation**:
  - Automatically discovered via `WCR2_INSTALL_DIR` environment variable, `~/.wcr2-mcp.json`, active `WzComparerR2` processes, repository build output (`Build/bin/Release`), or standard directories (`%USERPROFILE%`, `C:\Nexon`, `D:\Nexon`, `C:\MapleStory`).
  - Contains `WzComparerR2.exe`, `WzComparerR2.McpHost.exe`, and `Plugin/WzComparerR2.Mcp/WzComparerR2.Mcp.dll`.
- **MapleStory WZ Data**:
  - Automatically resolved via `WCR2_BASE_WZ` or `WCR2_DEFAULT_WZ_PATH` environment variables, `~/.wcr2-mcp.json` (`baseWzPath`), or standard MapleStory paths (e.g., `C:\Nexon\Maple\Data\Base\Base.wz`).
- **Scripts & Tools** (workspace relative):
  - MCP Server: `mcp/wcr2-mcp-server.mjs`
  - CLI Companion: `mcp/wcr2-cli.mjs`
  - Plugin Build Script: `mcp/build-plugin.ps1` (supports optional `-Wcr2InstallDir <path>`)

## 2. Prefer MCP Tools (`wcr2_*`) First
Whenever the user asks to search MapleStory items/skills/monsters/maps/quests/NPCs, inspect WZ nodes, render tooltips, extract sprites/animations/audio/XML, render character avatars, compare WZ files, or control the live `WzComparerR2.exe` GUI:
1. **Use the `wcr2_*` MCP tools directly** (or run `node mcp/wcr2-cli.mjs <method> ...` if MCP tools are unavailable in a subshell):
   - `wcr2_status`: Check live GUI (`127.0.0.1:21480`) and headless daemon (`127.0.0.1:21481`) status.
   - `wcr2_open_wz` / `wcr2_close_wz` / `wcr2_list_wz_types`: Manage open WZ files and inspect loaded categories.
   - `wcr2_search_strings`: Search Korean/localized names, descriptions, or IDs across `eqp`, `item`, `skill`, `mob`, `map`, `npc`, `quest`.
   - `wcr2_get_node` / `wcr2_search_nodes`: Traverse WZ trees and search node names/values/regex.
   - `wcr2_get_object_info`: Parse domain objects (`Gear`, `Item`, `Skill`, `Mob`, `Npc`, `Map`) with full stats and strings.
   - `wcr2_render_tooltip`: Render authentic MapleStory tooltip PNGs using `TooltipRender22`, `MobTooltipRenderer`, `MapTooltipRenderer`, etc.
   - `wcr2_extract_png` / `wcr2_extract_animation` / `wcr2_extract_sound` / `wcr2_export_xml`: Extract game assets with automatic `_inlink` and `_outlink` resolution.
   - `wcr2_avatar_render`: Render paper-doll character sprites with `AvatarCanvasManager`.
   - `wcr2_compare_wz`: Run `EasyComparer` diffs between two WZ files or directories.
   - `wcr2_export_db_csv`: Export category string & metadata tables to CSV.
   - `wcr2_run_lua`: Execute custom NLua scripts inside WzComparerR2's `LuaSandbox`.
   - `wcr2_gui_control`: Launch `WzComparerR2.exe`, inspect/select tree nodes, switch tabs, or capture GUI screenshots.

## 3. Building & Modifying `WzComparerR2.Mcp`
- The C# bridge source lives in `WzComparerR2.Mcp/` (`Entry.cs`, `Wcr2BridgeServer.cs`, `Wcr2Engine.cs`, `HostProgram.cs`).
- Keep C# syntax compatible with `.NET Framework 4` `csc.exe` (C# 5 syntax) so `powershell -ExecutionPolicy Bypass -File mcp/build-plugin.ps1` compiles and deploys `WzComparerR2.Mcp.dll` and `WzComparerR2.McpHost.exe` without requiring external SDK installs.
