---
name: wcr2-asset-extractor
description: Render MapleStory tooltips, extract PNG sprites (with automatic _inlink/_outlink resolution), export GIF/APNG animations, extract BGM/SFX audio, render character avatars, compare WZ patches, and export database CSVs using WzComparerR2.
---

# WzComparerR2 Asset Extraction & Rendering Skill (`wcr2-asset-extractor`)

Use this skill whenever the user wants to:
- Render authentic MapleStory tooltips (`Gear`, `Item`, `Skill`, `Mob`, `Npc`, `Map`, `Quest`) to `.png` images.
- Extract static sprites/icons (`Wz_Png`) with automatic KMST1125+ `_inlink` and `_outlink` (`_Canvas`) resolution.
- Export animations (monster stand/attack, skill hit/effect, NPC actions) as animated `.apng`, `.gif`, or frame sequences.
- Extract BGM or SFX audio (`Wz_Sound`) as `.mp3` or `.wav`.
- Render character paper-doll avatars (`AvatarCanvasManager`) with custom skin, hair, face, and equipment IDs.
- Export `.img` or WZ node subtrees to XML, export category databases to `.csv`, or run `EasyComparer` patch diffs between two `.wz` archives.

## 1. Tooltip Rendering (`wcr2_render_tooltip`)

Uses WzComparerR2's built-in `GearTooltipRender22`, `ItemTooltipRender22`, `SkillTooltipRender2`, `MobTooltipRenderer`, `NpcTooltipRenderer`, `MapTooltipRenderer`, and `QuestTooltipRenderer`.

- **MCP Tool**: `wcr2_render_tooltip`
  - `type`: `"gear" | "item" | "skill" | "mob" | "npc" | "map" | "quest"`
  - `id`: Numeric ID (e.g., `1212129`, `8880140`, `100000000`) or `path`
  - `outputPath`: Target `.png` path (optional; defaults to `%TEMP%\wcr2_mcp\`)
- **CLI Example**:
  ```powershell
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" render_tooltip --type=gear --id=1212129 --outputPath="C:\temp\genesis_rod.png"
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" render_tooltip --type=mob --id=8880140 --outputPath="C:\temp\lucid_tooltip.png"
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" render_tooltip --type=map --id=100000000 --outputPath="C:\temp\henesys_tooltip.png"
  ```

## 2. Static Sprite Extraction (`wcr2_extract_png`)

Modern MapleStory (KMST1125+) stores 1x1 stub canvases in primary `.img` files with `_outlink` pointing to `_Canvas/xxxx.img/...` or `_inlink` pointing within the same `.img`. `wcr2_extract_png` automatically resolves `_inlink` and `_outlink` via `Wz_NodeExtension2.GetLinkedSourceNode(PluginManager.FindWz)`.

- **MCP Tool**: `wcr2_extract_png`
  - `path`: e.g. `"Character/Weapon/01212129.img/info/icon"`
  - `outputPath`: Destination `.png` file path
  - `resolveLink`: `true` (default)
- **CLI Example**:
  ```powershell
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" extract_png --path="Character/Weapon/01212129.img/info/icon" --outputPath="C:\temp\icon.png"
  ```

## 3. Animation Extraction (`wcr2_extract_animation`)

Extracts multi-frame animation nodes (`0`, `1`, `2`, ... with `delay` and `origin`) using `FrameNodeData`, `FrameAnimator`, `BuildInApngEncoder`, and `BuildInGifEncoder`.

- **MCP Tool**: `wcr2_extract_animation`
  - `path`: Parent animation node path (e.g. `"Mob/8880140.img/stand"`, `"Skill/400011000.img/skill/400011001/effect"`)
  - `format`: `"apng" | "gif" | "frames"`
  - `outputPath`: Destination `.png` (for APNG), `.gif`, or folder (for `frames`)
- **CLI Example**:
  ```powershell
  node "${CLAUDE_PLUGIN_ROOT}/mcp/wcr2-cli.mjs" extract_animation --path="Mob/8880140.img/stand" --format=gif --outputPath="C:\temp\lucid_stand.gif"
  ```

## 4. Character Paper-Doll Avatar Rendering (`wcr2_avatar_render`)

Uses `WzComparerR2.AvatarCommon.AvatarCanvasManager` to composite body, head, hair, face, and equipment layers for any action, emotion, frame, and ear type.

- **MCP Tool**: `wcr2_avatar_render`
  - `parts`: Array of part IDs (e.g. `[2000, 20000, 30000, 1040036, 1060026, 1212129]`)
    - `2000`–`2099` / `12000`+: Skin/Body ID (automatically adds matching head `id + 10000`)
    - `20000`–`29999` / `50000`+: Face ID
    - `30000`–`49999` / `60000`+: Hair ID
    - `1000000`+: Equipment (Hat, Coat, Longcoat, Pants, Shoes, Cape, Weapon, Shield, etc.)
  - `action`: `"stand1"` (default), `"stand2"`, `"walk1"`, `"alert"`, `"fly"`, etc.
  - `emotion`: `"default"`, `"smile"`, `"wink"`, `"bewildered"`, etc.
  - `frame`: `0` (default)
  - `earType`: `0` (Human), `1` (Elf), `2` (Flora), `3` (HighFlora)
  - `outputPath`: Destination `.png` file path

## 5. Audio, XML, CSV & Patch Comparison

- **Sound (`wcr2_extract_sound`)**: Extracts `Wz_Sound` nodes (e.g., `"Sound/Bgm00.img/FloralLife"`) to `.mp3` / `.wav`.
- **XML (`wcr2_export_xml`)**: Exports any `.img` or node subtree to XML using `Wz_Image.ExportXml` / `Wz_Node.ExportXml`.
- **Database CSV (`wcr2_export_db_csv`)**: Exports `StringLinker` tables (`eqp`, `item`, `skill`, `mob`, `map`, `npc`, `quest`) to UTF-8 CSV.
- **WZ Patch Diff (`wcr2_compare_wz`)**: Runs `EasyComparer` between `newWzPath` and `oldWzPath`.
