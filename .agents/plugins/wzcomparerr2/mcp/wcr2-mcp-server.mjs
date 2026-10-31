#!/usr/bin/env node
/**
 * WzComparerR2 Model Context Protocol (MCP) Server
 *
 * Bridges AI agents (Antigravity / MCP clients) to WzComparerR2:
 *  - Live GUI Plugin (`WzComparerR2.exe` via `WzComparerR2.Mcp.dll` on 127.0.0.1:21480)
 *  - Persistent Headless Engine (`WzComparerR2.McpHost.exe` on 127.0.0.1:21481)
 *
 * Zero external npm dependencies required.
 */

import http from "node:http";
import { execSync, spawn } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const GUI_PORT = Number(process.env.WCR2_GUI_PORT || 21480);
const HOST_PORT = Number(process.env.WCR2_HOST_PORT || 21481);

function loadUserMcpConfig() {
  try {
    const cfgPath = path.join(os.homedir(), ".wcr2-mcp.json");
    if (fs.existsSync(cfgPath)) {
      return JSON.parse(fs.readFileSync(cfgPath, "utf8"));
    }
  } catch {
    // ignore invalid user config
  }
  return {};
}

function resolveDefaultBaseWz() {
  if (process.env.WCR2_BASE_WZ && fs.existsSync(process.env.WCR2_BASE_WZ)) {
    return process.env.WCR2_BASE_WZ;
  }
  const userCfg = loadUserMcpConfig();
  if (userCfg.baseWzPath && fs.existsSync(userCfg.baseWzPath)) {
    return userCfg.baseWzPath;
  }
  const candidates = [
    "C:\\Nexon\\Maple\\Data\\Base\\Base.wz",
    "D:\\Nexon\\Maple\\Data\\Base\\Base.wz",
    "C:\\Nexon\\Maple\\Base.wz",
    "C:\\MapleStory\\Data\\Base\\Base.wz",
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  return candidates[0];
}

const DEFAULT_BASE_WZ = resolveDefaultBaseWz();

function tryFindRunningWcr2Dir() {
  try {
    if (process.platform === "win32") {
      const out = execSync(
          'powershell -NoProfile -NonInteractive -Command "Get-Process -Name WzComparerR2,WzComparerR2.McpHost -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Path -First 1"',
          { encoding: "utf8", timeout: 3000, stdio: ["ignore", "pipe", "ignore"] }
        ).trim();
      if (out && fs.existsSync(out)) {
        return path.dirname(out);
      }
    }
  } catch {
    // ignore
  }
  return null;
}

function findRepoRootDir(startDir) {
  let curr = startDir;
  for (let i = 0; i < 7; i++) {
    if (fs.existsSync(path.join(curr, "WzComparerR2.sln"))) {
      return curr;
    }
    const parent = path.dirname(curr);
    if (parent === curr) break;
    curr = parent;
  }
  return null;
}

function resolveWcr2Paths() {
  const userCfg = loadUserMcpConfig();
  const runningDir = tryFindRunningWcr2Dir();
  const repoRoot = findRepoRootDir(__dirname);

  const candidates = [
    process.env.WCR2_INSTALL_DIR,
    userCfg.installDir,
    runningDir,
    repoRoot ? path.join(repoRoot, "Build", "bin", "Release") : null,
    repoRoot ? path.join(repoRoot, "Build", "bin", "Debug") : null,
    repoRoot ? path.join(repoRoot, "WzComparerR2", "bin", "Release") : null,
    repoRoot ? path.join(repoRoot, "WzComparerR2", "bin", "Debug") : null,
  ].filter(Boolean);

  for (const dir of candidates) {
    const hostExe = path.join(dir, "WzComparerR2.McpHost.exe");
    const guiExe = path.join(dir, "WzComparerR2.exe");
    if (fs.existsSync(hostExe) || fs.existsSync(guiExe)) {
      return { installDir: dir, hostExe, guiExe };
    }
  }

  const home = os.homedir();
  const searchRoots = [
    "C:\\Nexon",
    "D:\\Nexon",
    "C:\\MapleStory",
    "C:\\Program Files",
    "C:\\Program Files (x86)",
    path.join(home, "Desktop"),
    path.join(home, "Downloads"),
    home,
  ];
  for (const root of searchRoots) {
    try {
      if (!fs.existsSync(root)) continue;
      for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
        if (!entry.isDirectory()) continue;
        if (/wzcomparerr2|wcr2/i.test(entry.name)) {
          const dir = path.join(root, entry.name);
          const hostExe = path.join(dir, "WzComparerR2.McpHost.exe");
          const guiExe = path.join(dir, "WzComparerR2.exe");
          if (fs.existsSync(hostExe) || fs.existsSync(guiExe)) {
            return { installDir: dir, hostExe, guiExe };
          }
        }
      }
    } catch {
      // ignore permission errors
    }
  }

  const fallbackDir = repoRoot
    ? path.join(repoRoot, "Build", "bin", "Release")
    : path.resolve(__dirname, "..", "Build", "bin", "Release");

  return {
    installDir: fallbackDir,
    hostExe: path.join(fallbackDir, "WzComparerR2.McpHost.exe"),
    guiExe: path.join(fallbackDir, "WzComparerR2.exe"),
  };
}

function hasLoadedWz(status) {
  if (!status) return false;
  if (typeof status.openWzCount === "number" && status.openWzCount > 0) return true;
  if (Array.isArray(status.loadedWz) && status.loadedWz.length > 0) return true;
  return false;
}

function httpJson(port, urlPath, method = "GET", bodyObj = null, timeoutMs = 120000) {
  return new Promise((resolve, reject) => {
    const payload = bodyObj ? Buffer.from(JSON.stringify(bodyObj), "utf8") : null;
    const req = http.request(
      {
        hostname: "127.0.0.1",
        port,
        path: urlPath,
        method,
        headers: payload
          ? {
              "Content-Type": "application/json; charset=utf-8",
              "Content-Length": payload.length,
            }
          : {},
        timeout: timeoutMs,
      },
      (res) => {
        const chunks = [];
        res.on("data", (c) => chunks.push(c));
        res.on("end", () => {
          const text = Buffer.concat(chunks).toString("utf8");
          try {
            resolve(JSON.parse(text));
          } catch {
            resolve({ ok: res.statusCode === 200, raw: text, statusCode: res.statusCode });
          }
        });
      }
    );
    req.on("timeout", () => {
      req.destroy(new Error(`Request to 127.0.0.1:${port}${urlPath} timed out after ${timeoutMs}ms`));
    });
    req.on("error", (err) => reject(err));
    if (payload) req.write(payload);
    req.end();
  });
}

async function isPortAlive(port, timeoutMs = 800) {
  try {
    const res = await httpJson(port, "/health", "GET", null, timeoutMs);
    return Boolean(res && res.ok);
  } catch {
    return false;
  }
}

async function getPortStatus(port, timeoutMs = 1500) {
  try {
    const res = await httpJson(port, "/status", "GET", null, timeoutMs);
    if (res && res.ok && res.result) return res.result;
    return null;
  } catch {
    return null;
  }
}

let hostSpawnPromise = null;

async function ensureHeadlessHost() {
  if (await isPortAlive(HOST_PORT)) {
    return HOST_PORT;
  }
  if (hostSpawnPromise) {
    return hostSpawnPromise;
  }
  hostSpawnPromise = (async () => {
    const { installDir, hostExe } = resolveWcr2Paths();
    if (!fs.existsSync(hostExe)) {
      throw new Error(
        `WzComparerR2.McpHost.exe not found at '${hostExe}'. ` +
        `Please set the WCR2_INSTALL_DIR environment variable to your WzComparerR2 folder, ` +
        `or run 'powershell -File mcp/build-plugin.ps1' to build and configure ~/.wcr2-mcp.json.`
      );
    }
    const child = spawn(hostExe, ["--port", String(HOST_PORT)], {
      cwd: installDir,
      detached: true,
      stdio: "ignore",
      windowsHide: true,
    });
    child.unref();

    const deadline = Date.now() + 15000;
    while (Date.now() < deadline) {
      if (await isPortAlive(HOST_PORT, 600)) {
        return HOST_PORT;
      }
      await new Promise((r) => setTimeout(r, 200));
    }
    throw new Error(`Timed out waiting for WzComparerR2.McpHost.exe on port ${HOST_PORT}`);
  })();

  try {
    return await hostSpawnPromise;
  } finally {
    hostSpawnPromise = null;
  }
}

async function launchGuiInstance(args = {}) {
  const { installDir, guiExe } = resolveWcr2Paths();
  const wzPath = args.wzPath || args.path || null;

  if (await isPortAlive(GUI_PORT, 800)) {
    const status = await getPortStatus(GUI_PORT);
    if (wzPath && !hasLoadedWz(status)) {
      await httpJson(GUI_PORT, "/gui_control", "POST", { action: "open_wz", path: wzPath });
    }
    return {
      launched: false,
      alreadyRunning: true,
      guiPort: GUI_PORT,
      status: await getPortStatus(GUI_PORT),
    };
  }

  if (!fs.existsSync(guiExe)) {
    throw new Error(
      `WzComparerR2.exe not found at '${guiExe}'. ` +
      `Please set the WCR2_INSTALL_DIR environment variable to your WzComparerR2 folder, ` +
      `or run 'powershell -File mcp/build-plugin.ps1' to build and configure ~/.wcr2-mcp.json.`
    );
  }

  const child = spawn(guiExe, [], {
    cwd: installDir,
    detached: true,
    stdio: "ignore",
    windowsHide: false,
  });
  child.unref();

  const waitMs = Number(args.waitMs || 20000);
  const deadline = Date.now() + waitMs;
  while (Date.now() < deadline) {
    if (await isPortAlive(GUI_PORT, 800)) {
      if (wzPath && fs.existsSync(wzPath)) {
        const st = await getPortStatus(GUI_PORT);
        if (!hasLoadedWz(st)) {
          await httpJson(GUI_PORT, "/gui_control", "POST", { action: "open_wz", path: wzPath });
        }
      }
      return {
        launched: true,
        pid: child.pid,
        guiPort: GUI_PORT,
        status: await getPortStatus(GUI_PORT),
      };
    }
    await new Promise((r) => setTimeout(r, 300));
  }

  return {
    launched: true,
    pid: child.pid,
    guiPort: GUI_PORT,
    warning: `Spawned WzComparerR2.exe (PID ${child.pid}), but port ${GUI_PORT} did not respond within ${waitMs}ms.`,
  };
}

/**
 * Selects target port (`GUI_PORT` vs `HOST_PORT`) and dispatches RPC method.
 */
export async function callWcr2(method, args = {}) {
  const rawTarget = (args && args.target) || "auto";
  const routingTarget =
    rawTarget === "gui" || rawTarget === "headless" || rawTarget === "auto" ? rawTarget : "auto";

  if (method === "gui_control" && args && args.action === "launch") {
    return await launchGuiInstance(args);
  }

  if (method === "status" && routingTarget === "auto") {
    const guiAlive = await isPortAlive(GUI_PORT, 600);
    const hostPort = await ensureHeadlessHost();
    const hostStatus = await getPortStatus(hostPort);
    const guiStatus = guiAlive ? await getPortStatus(GUI_PORT) : null;
    return {
      activeTarget: hasLoadedWz(guiStatus) ? "gui" : "headless",
      gui: guiStatus || { running: false, port: GUI_PORT },
      headless: hostStatus || { running: false, port: HOST_PORT },
      defaultBaseWz: DEFAULT_BASE_WZ,
    };
  }

  let chosenPort = null;
  if (routingTarget === "gui" || method === "gui_control") {
    if (!(await isPortAlive(GUI_PORT, 800))) {
      if (args && args.autoLaunch !== false && method === "gui_control" && args.action !== "close") {
        await launchGuiInstance({ wzPath: args.wzPath || DEFAULT_BASE_WZ });
      } else {
        throw new Error(
          `WzComparerR2 GUI is not running on port ${GUI_PORT}. Call wcr2_gui_control with action="launch" or use target="headless".`
        );
      }
    }
    chosenPort = GUI_PORT;
  } else if (routingTarget === "headless") {
    chosenPort = await ensureHeadlessHost();
  } else {
    // auto mode: prefer GUI if it is running AND already has WZ loaded; otherwise use headless host
    const guiStatus = await getPortStatus(GUI_PORT, 600);
    if (hasLoadedWz(guiStatus)) {
      chosenPort = GUI_PORT;
    } else {
      chosenPort = await ensureHeadlessHost();
    }
  }

  const response = await httpJson(chosenPort, `/${method}`, "POST", args || {}, 180000);
  if (!response) {
    throw new Error(`Empty response from WzComparerR2 bridge on port ${chosenPort}`);
  }
  if (response.ok === false) {
    throw new Error(response.error || `WzComparerR2 error calling ${method}`);
  }
  return response.result !== undefined ? response.result : response;
}

export const TOOLS = [
  {
    name: "wcr2_status",
    description:
      "Get WzComparerR2 status for both the live GUI plugin (port 21480) and the headless engine daemon (port 21481), including open WZ archives, loaded StringLinker counts, and GUI state.",
    inputSchema: {
      type: "object",
      properties: {
        target: {
          type: "string",
          enum: ["auto", "gui", "headless"],
          description: "Target runtime ('auto' returns both GUI and headless status).",
        },
      },
    },
  },
  {
    name: "wcr2_open_wz",
    description:
      "Open a MapleStory WZ file (e.g. 'C:\\Nexon\\Maple\\Data\\Base\\Base.wz') and optionally load StringLinker for Korean/localized names and descriptions.",
    inputSchema: {
      type: "object",
      properties: {
        path: {
          type: "string",
          description:
            "Full path to Base.wz or any specific .wz/.ms/.img file. Defaults to C:\\Nexon\\Maple\\Data\\Base\\Base.wz.",
        },
        loadStrings: {
          type: "boolean",
          description: "Whether to initialize StringLinker right away (default: false; auto-loaded on string/tooltip queries).",
        },
        target: {
          type: "string",
          enum: ["auto", "gui", "headless"],
        },
      },
    },
  },
  {
    name: "wcr2_close_wz",
    description: "Close a specific open WZ file or all open WZ files in the engine.",
    inputSchema: {
      type: "object",
      properties: {
        path: {
          type: "string",
          description: "Path or name of WZ to close, or omit / pass 'all' to close all open WZ archives.",
        },
        target: {
          type: "string",
          enum: ["auto", "gui", "headless"],
        },
      },
    },
  },
  {
    name: "wcr2_list_wz_types",
    description:
      "List all loaded Wz_Type categories (Base, Character, Skill, Mob, Map, Item, String, Etc, Quest, Sound, UI, etc.) and their .img counts.",
    inputSchema: {
      type: "object",
      properties: {
        target: {
          type: "string",
          enum: ["auto", "gui", "headless"],
        },
      },
    },
  },
  {
    name: "wcr2_get_node",
    description:
      "Inspect a WZ node by path (e.g. 'Character/Weapon/01212129.img/info', 'Skill/400011000.img/skill/400011001', 'Mob/8880140.img'). Returns node value, type, canvas dimensions/inlink/outlink metadata, and children tree.",
    inputSchema: {
      type: "object",
      required: ["path"],
      properties: {
        path: {
          type: "string",
          description: "WZ node path using forward slashes (e.g. 'Mob/8880140.img/info').",
        },
        depth: {
          type: "integer",
          description: "Child recursion depth (0 to 6, default: 1).",
        },
        maxChildren: {
          type: "integer",
          description: "Maximum children returned per node (default: 200).",
        },
        resolveUol: {
          type: "boolean",
          description: "Automatically follow Wz_Uol references (default: false).",
        },
        target: {
          type: "string",
          enum: ["auto", "gui", "headless"],
        },
      },
    },
  },
  {
    name: "wcr2_search_nodes",
    description:
      "Search WZ nodes and .img structures under a root path by node name, text/number value, value type, or regex.",
    inputSchema: {
      type: "object",
      properties: {
        rootPath: {
          type: "string",
          description: "Root WZ path to search within (e.g. 'Mob/8880140.img', 'Etc', 'Character/Weapon').",
        },
        query: {
          type: "string",
          description: "Substring or regex pattern to match against node text or value.",
        },
        matchScope: {
          type: "string",
          enum: ["both", "name", "value"],
          description: "Whether to match node name, node value, or both (default: 'both').",
        },
        matchValue: {
          type: "boolean",
          description: "Also match node values in addition to node names (default: true).",
        },
        valueType: {
          type: "string",
          description: "Optional filter by WZ value type (e.g. 'Wz_Png', 'Wz_Sound', 'Wz_Vector', 'Wz_Uol', 'String', 'Int32').",
        },
        exact: {
          type: "boolean",
          description: "Require exact string match (default: false).",
        },
        regex: {
          type: "boolean",
          description: "Treat query as regular expression (default: false).",
        },
        maxDepth: {
          type: "integer",
          description: "Maximum traversal depth (default: 6).",
        },
        enterImg: {
          type: "boolean",
          description: "Whether to unpack and traverse inside .img nodes (default: true if rootPath is inside an .img).",
        },
        limit: {
          type: "integer",
          description: "Maximum matches to return (default: 100, max: 1000).",
        },
      },
    },
  },
  {
    name: "wcr2_search_strings",
    description:
      "Search MapleStory String.wz (via StringLinker) by Korean/English name, description, or numeric ID across eqp (gear), item, skill, mob, map, npc, quest, set_item, achievement, reactor, or familiar_skill.",
    inputSchema: {
      type: "object",
      required: ["query"],
      properties: {
        query: {
          type: "string",
          description: "Search keyword, space-separated tokens, regex, or numeric ID (e.g. 'Genesis Shining Rod' / '제네시스 샤이닝로드', 'Lucid' / '루시드', '8880140').",
        },
        category: {
          type: "string",
          enum: [
            "all",
            "eqp",
            "item",
            "skill",
            "mob",
            "map",
            "npc",
            "quest",
            "set_item",
            "achievement",
            "reactor",
            "familiar_skill",
          ],
          description: "String category to search (default: 'all').",
        },
        exact: {
          type: "boolean",
          description: "Require exact name/ID match (default: false).",
        },
        regex: {
          type: "boolean",
          description: "Treat query as regular expression (default: false).",
        },
        searchDesc: {
          type: "boolean",
          description: "Search inside description text as well as name (default: false).",
        },
        limit: {
          type: "integer",
          description: "Max results (default: 50, max: 500).",
        },
      },
    },
  },
  {
    name: "wcr2_get_object_info",
    description:
      "Parse a MapleStory game object (Gear, Item, Skill, Mob, Npc, Map, Quest, SetItem, Recipe, Familiar, Achievement, DamageSkin) using WzComparerR2's CharaSim domain models and return structured stats, properties, and localized strings.",
    inputSchema: {
      type: "object",
      properties: {
        type: {
          type: "string",
          enum: [
            "auto",
            "gear",
            "item",
            "skill",
            "mob",
            "npc",
            "map",
            "quest",
            "set_item",
            "recipe",
            "familiar",
            "achievement",
            "damage_skin",
          ],
          description: "Game object type (default: 'auto').",
        },
        id: {
          type: "integer",
          description: "Numeric object ID (e.g. 1212129 for Genesis Shining Rod, 8880140 for Lucid, 100000000 for Henesys).",
        },
        path: {
          type: "string",
          description: "Optional explicit WZ node path instead of numeric ID.",
        },
        level: {
          type: "integer",
          description: "Skill level to evaluate (for type='skill', default: maxLevel).",
        },
      },
    },
  },
  {
    name: "wcr2_render_tooltip",
    description:
      "Render an authentic WzComparerR2 in-game tooltip PNG image for any Gear, Item, Skill, Mob, Npc, Map, Quest, SetItem, Recipe, Familiar, Achievement, or DamageSkin.",
    inputSchema: {
      type: "object",
      properties: {
        type: {
          type: "string",
          enum: [
            "auto",
            "gear",
            "item",
            "skill",
            "mob",
            "npc",
            "map",
            "quest",
            "set_item",
            "recipe",
            "familiar",
            "achievement",
            "damage_skin",
          ],
          description: "Tooltip type to render (default: 'auto').",
        },
        id: {
          type: "integer",
          description: "Numeric ID of the gear, item, skill, mob, npc, map, quest, set_item, etc.",
        },
        path: {
          type: "string",
          description: "Optional explicit WZ path if not using numeric ID.",
        },
        level: {
          type: "integer",
          description: "Skill level to render (for type='skill', default: maxLevel).",
        },
        showId: {
          type: "boolean",
          description: "Show numeric ID on the tooltip header (default: true).",
        },
        enable22AniStyle: {
          type: "boolean",
          description: "Use modern KMS 2022+ tooltip style (default: true).",
        },
        showInGui: {
          type: "boolean",
          description: "Also display the floating tooltip window in the live GUI if running (default: false).",
        },
        outputPath: {
          type: "string",
          description: "Output PNG file path (optional; auto-generated in temp dir if omitted).",
        },
        returnBase64: {
          type: "boolean",
          description: "Whether to return an inline image/png block in the MCP response (default: false).",
        },
      },
    },
  },
  {
    name: "wcr2_extract_png",
    description:
      "Extract a single PNG bitmap or a recursive directory of PNG bitmaps from WZ canvas nodes, automatically resolving _inlink and _outlink references to _Canvas packs.",
    inputSchema: {
      type: "object",
      required: ["path"],
      properties: {
        path: {
          type: "string",
          description: "WZ path to the canvas node or parent container (e.g. 'Character/Weapon/01212129.img/info/icon').",
        },
        outputPath: {
          type: "string",
          description: "Destination .png file path (for single canvas) or folder path (when recursive=true).",
        },
        recursive: {
          type: "boolean",
          description: "Recursively extract all Wz_Png nodes under path (default: false).",
        },
        resolveLink: {
          type: "boolean",
          description: "Resolve _inlink / _outlink automatically (default: true).",
        },
        limit: {
          type: "integer",
          description: "Maximum PNG files to extract when recursive=true (default: 500).",
        },
        returnBase64: {
          type: "boolean",
          description: "Return inline image/png base64 content for single PNG extraction (default: false).",
        },
      },
    },
  },
  {
    name: "wcr2_extract_animation",
    description:
      "Extract an animated sprite node (numbered frames 0, 1, 2... with delay/origin) into an animated GIF, APNG, or individual frame PNGs using WzComparerR2's Gif.CreateFromNode and built-in encoders.",
    inputSchema: {
      type: "object",
      required: ["path"],
      properties: {
        path: {
          type: "string",
          description: "WZ path to the animation parent node (e.g. 'Mob/8880140.img/stand', 'Skill/400011000.img/skill/400011001/effect').",
        },
        format: {
          type: "string",
          enum: ["gif", "apng", "frames"],
          description: "Output animation format (default: 'apng').",
        },
        outputPath: {
          type: "string",
          description: "Output file path (.gif or .apng/.png) or directory path (when format='frames').",
        },
      },
    },
  },
  {
    name: "wcr2_extract_sound",
    description:
      "Extract audio (MP3 / WAV / PCM) from a Wz_Sound node (e.g. 'Sound/Bgm00.img/FloralLife').",
    inputSchema: {
      type: "object",
      required: ["path"],
      properties: {
        path: {
          type: "string",
          description: "WZ path to the sound node.",
        },
        outputPath: {
          type: "string",
          description: "Output audio file path (.mp3 or .wav).",
        },
      },
    },
  },
  {
    name: "wcr2_export_xml",
    description:
      "Export a WZ .img or node subtree into WzComparerR2 XML format and return both the file path and an inline XML preview.",
    inputSchema: {
      type: "object",
      required: ["path"],
      properties: {
        path: {
          type: "string",
          description: "WZ path to export (e.g. 'Character/Weapon/01212129.img/info').",
        },
        outputPath: {
          type: "string",
          description: "Optional file path to save the XML.",
        },
        maxPreviewLength: {
          type: "integer",
          description: "Maximum characters of inline XML preview to return (default: 16384).",
        },
      },
    },
  },
  {
    name: "wcr2_compare_wz",
    description:
      "Compare two WZ files or directories using WzComparerR2's WzFileComparer and EasyComparer engine to detect added, removed, or modified .img files and nodes.",
    inputSchema: {
      type: "object",
      required: ["newWzPath", "oldWzPath"],
      properties: {
        newWzPath: {
          type: "string",
          description: "Path to the newer .wz file or directory.",
        },
        oldWzPath: {
          type: "string",
          description: "Path to the older .wz file or directory.",
        },
        outputDir: {
          type: "string",
          description: "Optional directory to write EasyComparer diff reports.",
        },
        outputPng: {
          type: "boolean",
          description: "Export changed PNGs and tooltips during EasyComparer diff (default: false).",
        },
        resolvePngLink: {
          type: "boolean",
          description: "Resolve _inlink/_outlink when comparing PNGs (default: true).",
        },
      },
    },
  },
  {
    name: "wcr2_run_lua",
    description:
      "Execute a Lua script inside WzComparerR2's NLua / LuaConsole sandbox (`env:WriteLine(...)`, `PluginManager.FindWz(...)`, `import 'WzComparerR2.CharaSimControl'`). Enables custom batch queries and automation.",
    inputSchema: {
      type: "object",
      properties: {
        code: {
          type: "string",
          description: "Lua source code to execute.",
        },
        scriptPath: {
          type: "string",
          description: "Optional path to a .lua script file to execute.",
        },
      },
    },
  },
  {
    name: "wcr2_avatar_render",
    description:
      "Render a MapleStory character paper-doll avatar PNG given skin/body, hair, face, and equipment IDs using AvatarCanvasManager.",
    inputSchema: {
      type: "object",
      properties: {
        parts: {
          type: "array",
          items: { type: "integer" },
          description:
            "Array of item/part IDs, e.g. [2000, 20000, 30000, 1040036, 1060026, 1212129] (skin/body, face, hair, top, bottom/overall, weapon, etc.).",
        },
        action: {
          type: "string",
          description: "Character action name (default: 'stand1', e.g. 'walk1', 'alert', 'fly').",
        },
        emotion: {
          type: "string",
          description: "Facial expression (default: 'default', e.g. 'smile', 'bewildered', 'wink').",
        },
        frame: {
          type: "integer",
          description: "Action animation frame index (default: 0).",
        },
        earType: {
          type: "integer",
          description: "Ear type (0=Human, 1=Elf/Mercedes, 2=Flora/Illium, 3=HighFlora/Adele).",
        },
        outputPath: {
          type: "string",
          description: "Output PNG path.",
        },
        returnBase64: {
          type: "boolean",
          description: "Whether to return an inline image/png block in the MCP response (default: false).",
        },
      },
    },
  },
  {
    name: "wcr2_export_db_csv",
    description:
      "Export a complete database table of strings and metadata for a category (eqp, item, skill, mob, map, npc, quest, set_item, achievement, reactor, all, skill_db, skill_option) to UTF-8 CSV.",
    inputSchema: {
      type: "object",
      required: ["category"],
      properties: {
        category: {
          type: "string",
          enum: [
            "eqp",
            "item",
            "skill",
            "mob",
            "map",
            "npc",
            "quest",
            "set_item",
            "achievement",
            "reactor",
            "all",
            "skill_db",
            "skill_option",
          ],
          description: "Category to export.",
        },
        outputPath: {
          type: "string",
          description: "Output CSV file path (for single category).",
        },
        outputDir: {
          type: "string",
          description: "Output directory path (for 'all', 'skill_db', or 'skill_option').",
        },
      },
    },
  },
  {
    name: "wcr2_gui_control",
    description:
      "Launch or control the live WzComparerR2.exe GUI application: launch the GUI, inspect the currently selected tree node, select/navigate to a specific WZ path in the GUI tree, trigger quickView, run GUI string search, open a WZ file, switch Ribbon tabs, capture a screenshot, or close the GUI.",
    inputSchema: {
      type: "object",
      required: ["action"],
      properties: {
        action: {
          type: "string",
          enum: [
            "launch",
            "status",
            "get_selected",
            "select_node",
            "quick_view",
            "search_string",
            "open_wz",
            "switch_tab",
            "screenshot",
            "close",
          ],
          description: "GUI control action to perform.",
        },
        path: {
          type: "string",
          description: "WZ node path (for select_node / quick_view) or .wz file path (for open_wz).",
        },
        wzPath: {
          type: "string",
          description: "Optional Base.wz path to open automatically when action='launch'.",
        },
        query: {
          type: "string",
          description: "Search keyword when action='search_string'.",
        },
        categoryIndex: {
          type: "integer",
          description: "Category dropdown index when action='search_string' (default: 0).",
        },
        tabIndex: {
          type: "integer",
          description: "Tab index (0, 1, ...) when action='switch_tab'.",
        },
        tabName: {
          type: "string",
          description: "Tab title substring when action='switch_tab'.",
        },
        outputPath: {
          type: "string",
          description: "Output PNG file path when action='screenshot' or 'quick_view'.",
        },
      },
    },
  },
];

const METHOD_MAP = {
  wcr2_status: "status",
  wcr2_open_wz: "open_wz",
  wcr2_close_wz: "close_wz",
  wcr2_list_wz_types: "list_wz_types",
  wcr2_get_node: "get_node",
  wcr2_search_nodes: "search_nodes",
  wcr2_search_strings: "search_strings",
  wcr2_get_object_info: "get_object_info",
  wcr2_render_tooltip: "render_tooltip",
  wcr2_extract_png: "extract_png",
  wcr2_extract_animation: "extract_animation",
  wcr2_extract_sound: "extract_sound",
  wcr2_export_xml: "export_xml",
  wcr2_compare_wz: "compare_wz",
  wcr2_run_lua: "run_lua",
  wcr2_avatar_render: "avatar_render",
  wcr2_export_db_csv: "export_db_csv",
  wcr2_gui_control: "gui_control",
};

const PROMPTS = [
  {
    name: "wcr2-inspect-object",
    description:
      "Search for a MapleStory item, skill, monster, or map by name/ID, inspect its stats, and render its tooltip.",
    arguments: [
      {
        name: "query",
        description: "Name or numeric ID of the MapleStory object (e.g. 'Genesis Shining Rod' / '제네시스 샤이닝로드', 'Lucid' / '루시드', '8880140').",
        required: true,
      },
    ],
  },
  {
    name: "wcr2-extract-assets",
    description:
      "Extract sprites, animations, sounds, and tooltips for a MapleStory monster, skill, or equipment item.",
    arguments: [
      {
        name: "target",
        description: "Target monster, skill, or gear name/ID.",
        required: true,
      },
    ],
  },
];

async function handleJsonRpcRequest(msg) {
  if (!msg || typeof msg !== "object") {
    return {
      jsonrpc: "2.0",
      id: null,
      error: { code: -32600, message: "Invalid Request" },
    };
  }

  const { id, method, params } = msg;

  if (method === "initialize") {
    const clientVersion = (params && params.protocolVersion) || "2024-11-05";
    return {
      jsonrpc: "2.0",
      id,
      result: {
        protocolVersion: clientVersion,
        capabilities: {
          tools: { listChanged: false },
          resources: { subscribe: false, listChanged: false },
          prompts: { listChanged: false },
        },
        serverInfo: {
          name: "wzcomparerr2-mcp",
          version: "1.1.0",
        },
      },
    };
  }

  if (method === "notifications/initialized" || method === "initialized" || method === "notifications/cancelled") {
    return null;
  }

  if (method === "ping") {
    return { jsonrpc: "2.0", id, result: {} };
  }

  if (method === "tools/list") {
    return {
      jsonrpc: "2.0",
      id,
      result: {
        tools: TOOLS,
      },
    };
  }

  if (method === "tools/call") {
    const toolName = params && params.name;
    const toolArgs = (params && params.arguments) || {};
    const engineMethod = METHOD_MAP[toolName];
    if (!engineMethod) {
      return {
        jsonrpc: "2.0",
        id,
        error: {
          code: -32601,
          message: `Unknown tool: ${toolName}`,
        },
      };
    }

    try {
      const result = await callWcr2(engineMethod, toolArgs);
      const content = [];
      if (result && typeof result === "object" && result.base64 && result.mimeType) {
        const { base64, mimeType, ...rest } = result;
        content.push({
          type: "text",
          text: JSON.stringify(rest, null, 2),
        });
        content.push({
          type: "image",
          data: base64,
          mimeType,
        });
      } else {
        content.push({
          type: "text",
          text: JSON.stringify(result, null, 2),
        });
      }
      return {
        jsonrpc: "2.0",
        id,
        result: {
          content,
          isError: false,
        },
      };
    } catch (err) {
      return {
        jsonrpc: "2.0",
        id,
        result: {
          content: [
            {
              type: "text",
              text: JSON.stringify(
                {
                  error: err && err.message ? err.message : String(err),
                  tool: toolName,
                },
                null,
                2
              ),
            },
          ],
          isError: true,
        },
      };
    }
  }

  if (method === "resources/list") {
    return {
      jsonrpc: "2.0",
      id,
      result: {
        resources: [
          {
            uri: "wcr2://status",
            name: "WzComparerR2 Runtime Status",
            description: "Live status of WzComparerR2 GUI and headless engine, open WZ files, and StringLinker counts.",
            mimeType: "application/json",
          },
          {
            uri: "wcr2://wz-types",
            name: "Loaded WZ Types Summary",
            description: "List of all Wz_Type categories and .img counts in the loaded Base.wz.",
            mimeType: "application/json",
          },
        ],
      },
    };
  }

  if (method === "resources/read") {
    const uri = params && params.uri;
    if (uri === "wcr2://status") {
      const data = await callWcr2("status", {});
      return {
        jsonrpc: "2.0",
        id,
        result: {
          contents: [{ uri, mimeType: "application/json", text: JSON.stringify(data, null, 2) }],
        },
      };
    }
    if (uri === "wcr2://wz-types") {
      const data = await callWcr2("list_wz_types", {});
      return {
        jsonrpc: "2.0",
        id,
        result: {
          contents: [{ uri, mimeType: "application/json", text: JSON.stringify(data, null, 2) }],
        },
      };
    }
    return {
      jsonrpc: "2.0",
      id,
      error: { code: -32602, message: `Unknown resource URI: ${uri}` },
    };
  }

  if (method === "prompts/list") {
    return {
      jsonrpc: "2.0",
      id,
      result: { prompts: PROMPTS },
    };
  }

  if (method === "prompts/get") {
    const name = params && params.name;
    const args = (params && params.arguments) || {};
    if (name === "wcr2-inspect-object") {
      return {
        jsonrpc: "2.0",
        id,
        result: {
          description: "Inspect a MapleStory object and render its tooltip",
          messages: [
            {
              role: "user",
              content: {
                type: "text",
                text: `Find '${args.query || ""}' in WzComparerR2 using wcr2_search_strings, inspect its full stats with wcr2_get_object_info, and render its tooltip PNG with wcr2_render_tooltip.`,
              },
            },
          ],
        },
      };
    }
    if (name === "wcr2-extract-assets") {
      return {
        jsonrpc: "2.0",
        id,
        result: {
          description: "Extract sprites, animations, and tooltips from WzComparerR2",
          messages: [
            {
              role: "user",
              content: {
                type: "text",
                text: `Search for '${args.target || ""}' using wcr2_search_strings, inspect its WZ node structure with wcr2_get_node, and extract its icon PNG, stand/attack animation, and tooltip image.`,
              },
            },
          ],
        },
      };
    }
    return {
      jsonrpc: "2.0",
      id,
      error: { code: -32602, message: `Unknown prompt: ${name}` },
    };
  }

  if (id !== undefined && id !== null) {
    return {
      jsonrpc: "2.0",
      id,
      error: {
        code: -32601,
        message: `Method not found: ${method}`,
      },
    };
  }
  return null;
}

/**
 * Stdio transport supporting both newline-delimited JSON-RPC and Content-Length framed JSON-RPC,
 * with sequential dispatch ordering and JSON-RPC batch support.
 */
function startStdioServer() {
  let useContentLength = false;
  let buffer = Buffer.alloc(0);
  let dispatchChain = Promise.resolve();

  function sendResponse(obj) {
    if (!obj) return;
    const json = JSON.stringify(obj);
    if (useContentLength) {
      const byteLen = Buffer.byteLength(json, "utf8");
      process.stdout.write(`Content-Length: ${byteLen}\r\n\r\n${json}`);
    } else {
      process.stdout.write(`${json}\n`);
    }
  }

  async function processSingleOrBatch(rawJson) {
    let parsed;
    try {
      parsed = JSON.parse(rawJson);
    } catch {
      sendResponse({
        jsonrpc: "2.0",
        id: null,
        error: { code: -32700, message: "Parse error" },
      });
      return;
    }

    if (Array.isArray(parsed)) {
      const batchResults = [];
      for (const item of parsed) {
        try {
          const r = await handleJsonRpcRequest(item);
          if (r) batchResults.push(r);
        } catch (err) {
          if (item && item.id !== undefined && item.id !== null) {
            batchResults.push({
              jsonrpc: "2.0",
              id: item.id,
              error: { code: -32603, message: err && err.message ? err.message : String(err) },
            });
          }
        }
      }
      if (batchResults.length > 0) {
        sendResponse(batchResults);
      }
    } else {
      try {
        const resp = await handleJsonRpcRequest(parsed);
        if (resp) sendResponse(resp);
      } catch (err) {
        if (parsed && parsed.id !== undefined && parsed.id !== null) {
          sendResponse({
            jsonrpc: "2.0",
            id: parsed.id,
            error: {
              code: -32603,
              message: err && err.message ? err.message : String(err),
            },
          });
        }
      }
    }
  }

  function enqueueMessage(rawJson) {
    dispatchChain = dispatchChain.then(() => processSingleOrBatch(rawJson)).catch(() => {});
  }

  function processBuffer() {
    while (buffer.length > 0) {
      const headerProbe = buffer.subarray(0, Math.min(buffer.length, 64)).toString("ascii");
      if (headerProbe.startsWith("Content-Length:")) {
        useContentLength = true;
        const sepIdx = buffer.indexOf("\r\n\r\n");
        if (sepIdx === -1) return;
        const headerStr = buffer.subarray(0, sepIdx).toString("ascii");
        const match = /Content-Length:\s*(\d+)/i.exec(headerStr);
        if (!match) {
          buffer = buffer.subarray(sepIdx + 4);
          continue;
        }
        const contentLen = parseInt(match[1], 10);
        const totalLen = sepIdx + 4 + contentLen;
        if (buffer.length < totalLen) return;
        const body = buffer.subarray(sepIdx + 4, totalLen).toString("utf8");
        buffer = buffer.subarray(totalLen);
        enqueueMessage(body);
      } else {
        const nlIdx = buffer.indexOf(0x0a); // '\n'
        if (nlIdx === -1) return;
        const line = buffer.subarray(0, nlIdx).toString("utf8").trim();
        buffer = buffer.subarray(nlIdx + 1);
        if (line.length > 0) {
          enqueueMessage(line);
        }
      }
    }
  }

  process.stdin.on("data", (chunk) => {
    buffer = Buffer.concat([buffer, chunk]);
    processBuffer();
  });

  process.stdin.on("end", () => {
    dispatchChain.finally(() => process.exit(0));
  });
}

// Run stdio server if invoked directly
if (process.argv[1] && path.resolve(process.argv[1]) === path.resolve(__filename)) {
  startStdioServer();
}
