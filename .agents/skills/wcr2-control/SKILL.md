---
name: wcr2-control
description: Control and query WzComparerR2 (both live GUI and headless engine) to open MapleStory WZ files, search Korean/localized strings, inspect WZ node trees, parse Gear/Item/Skill/Mob/Npc/Map objects, and manipulate the WzComparerR2 GUI.
---

# WzComparerR2 Control & Data Inspection Skill (`wcr2-control`)

Use this skill whenever the user asks to:
- Open or inspect MapleStory `.wz` / `.ms` data files (`Base.wz`, `Character.wz`, `Skill.wz`, `Mob.wz`, `Map.wz`, `Item.wz`, `String.wz`, `Etc.wz`, `Quest.wz`).
- Search for MapleStory equipment, items, skills, monsters, maps, NPCs, or quests by name, description, or numeric ID.
- Inspect raw WZ node trees, properties, vectors, and canvas link metadata (`_inlink`, `_outlink`).
- Launch or manipulate the live `WzComparerR2.exe` GUI (selecting nodes in the tree, switching tabs, opening WZ archives, or capturing screenshots).

## 1. Architecture Overview

WzComparerR2 is exposed to agents via two synchronized runtimes managed by `mcp/wcr2-mcp-server.mjs`:
1. **Live GUI Bridge (`127.0.0.1:21480`)**: Hosted inside `WzComparerR2.exe` by `Plugin\WzComparerR2.Mcp\WzComparerR2.Mcp.dll`. Allows inspecting and controlling the user's open GUI session on the UI thread.
2. **Persistent Headless Daemon (`127.0.0.1:21481`)**: Hosted by `WzComparerR2.McpHost.exe --port 21481`. Automatically spawned on demand and keeps `Base.wz` and `StringLinker` warm in memory so queries complete in milliseconds.

## 2. Primary MCP Tools & CLI Equivalents

Prefer calling the `wcr2_*` MCP tools directly. If running from a terminal or script, use `node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" <command> [flags]`:

### Check Runtime Status
- **MCP**: `wcr2_status`
- **CLI**: `node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" status`

### Search Localized Names & IDs (`StringLinker`)
- **MCP**: `wcr2_search_strings` (`query`, `category`: `"all" | "eqp" | "item" | "skill" | "mob" | "map" | "npc" | "quest"`, `exact`, `searchDesc`, `limit`)
- **CLI**:
  ```powershell
  # Search by English or Korean keywords (e.g., Genesis / 제네시스, Lucid / 루시드)
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" search_strings --query=Genesis --category=eqp --limit=20
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" search_strings --query=제네시스 --category=eqp --limit=20
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" search_strings --query=Lucid --category=mob
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" search_strings --query=루시드 --category=mob
  ```

### Parse Structured Game Objects (`CharaSim`)
- **MCP**: `wcr2_get_object_info` (`type`: `"gear" | "item" | "skill" | "mob" | "npc" | "map"`, `id` or `path`, `level`)
- **CLI**:
  ```powershell
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" get_object_info --type=gear --id=1212129
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" get_object_info --type=mob --id=8880140
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" get_object_info --type=skill --id=400011001 --level=30
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" get_object_info --type=map --id=100000000
  ```

### Traverse & Search WZ Node Trees
- **MCP**: `wcr2_get_node` (`path`, `depth`, `maxChildren`) and `wcr2_search_nodes` (`rootPath`, `query`, `regex`, `matchValue`, `maxDepth`, `enterImg`, `limit`)
- **CLI**:
  ```powershell
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" get_node --path=Character/Weapon/01212129.img/info --depth=2
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" search_nodes --rootPath=Mob/8880140.img --query=attack --maxDepth=3
  ```

### Control the Live `WzComparerR2.exe` GUI
- **MCP**: `wcr2_gui_control`
  - `action: "launch"` — Spawns `WzComparerR2.exe` (optionally passing `wzPath: "C:\\Nexon\\Maple\\Data\\Base\\Base.wz"`) and waits for port `21480` to come online.
  - `action: "get_selected"` — Returns the currently selected `Wz_Node` in `advTree1` and `advTree3`.
  - `action: "select_node"`, `path: "Character/Weapon/01212129.img/info"` — Expands and selects the target node directly in the GUI tree!
  - `action: "switch_tab"`, `tabIndex: 0` — Switches the main ribbon tab.
  - `action: "screenshot"`, `outputPath: "C:\\temp\\wcr2_gui.png"` — Captures the live GUI window to a PNG file.
- **CLI**:
  ```powershell
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" gui_control --action=launch --wzPath="C:\Nexon\Maple\Data\Base\Base.wz"
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" gui_control --action=select_node --path="Mob/8880140.img/info"
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" gui_control --action=get_selected
  ```

## 3. Common WZ Path Conventions (KMST1125+ / Modern KMS)
- **Equipment (`Gear`)**: `Character/<Category>/<8-digit-id>.img` (e.g., `Character/Weapon/01212129.img`, `Character/Coat/01042392.img`).
- **Items (`Item`)**:
  - Consume (`2xxxxxx`): `Item/Consume/<4-digit-prefix>.img/<8-digit-id>`
  - Install (`3xxxxxx`): `Item/Install/<4-digit-prefix>.img/<8-digit-id>`
  - Etc (`4xxxxxx`): `Item/Etc/<4-digit-prefix>.img/<8-digit-id>`
  - Cash (`5xxxxxx`): `Item/Cash/<4-digit-prefix>.img/<8-digit-id>`
- **Skills (`Skill`)**: `Skill/<JobId>.img/skill/<SkillId>`
- **Monsters (`Mob`)**: `Mob/<7-digit-id>.img`
- **NPCs (`Npc`)**: `Npc/<7-digit-id>.img`
- **Maps (`Map`)**: `Map/Map/Map<first-digit>/<9-digit-id>.img`
