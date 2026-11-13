#!/usr/bin/env node
/**
 * WzComparerR2 CLI Client (`wcr2-cli.mjs`)
 *
 * Usage:
 *   node mcp/wcr2-cli.mjs status
 *   node mcp/wcr2-cli.mjs search_strings --query=Genesis --category=eqp
 *   node mcp/wcr2-cli.mjs search_strings --query=제네시스 --category=eqp
 *   node mcp/wcr2-cli.mjs get_object_info --type=gear --id=1212129
 *   node mcp/wcr2-cli.mjs render_tooltip --type=mob --id=8880140 --outputPath=C:\temp\lucid.png
 *   node mcp/wcr2-cli.mjs get_node '{"path":"Mob/8880140.img/info","depth":2}'
 */

import { callWcr2, TOOLS } from "./wcr2-mcp-server.mjs";

function parseCliArgs(argv) {
  const args = {};
  const positionals = [];
  for (let i = 0; i < argv.length; i++) {
    const token = argv[i];
    if (token.startsWith("--")) {
      const eqIdx = token.indexOf("=");
      if (eqIdx !== -1) {
        const key = token.slice(2, eqIdx);
        const val = token.slice(eqIdx + 1);
        args[key] = coerceValue(val);
      } else {
        const key = token.slice(2);
        const next = argv[i + 1];
        if (next !== undefined && !next.startsWith("--")) {
          args[key] = coerceValue(next);
          i++;
        } else {
          args[key] = true;
        }
      }
    } else {
      positionals.push(token);
    }
  }
  if (positionals.length > 0) {
    const first = positionals[0].trim();
    if (first.startsWith("{") && first.endsWith("}")) {
      try {
        Object.assign(args, JSON.parse(first));
      } catch {
        args.raw = first;
      }
    } else if (!args.path && !args.query) {
      args.query = first;
      args.path = first;
    }
  }
  return args;
}

function coerceValue(v) {
  if (v === "true") return true;
  if (v === "false") return false;
  if (/^-?\d+$/.test(v)) return parseInt(v, 10);
  if (v.startsWith("[") && v.endsWith("]")) {
    try {
      return JSON.parse(v);
    } catch {
      return v;
    }
  }
  return v;
}

async function main() {
  const [, , rawMethod, ...rest] = process.argv;
  if (!rawMethod || rawMethod === "--help" || rawMethod === "-h" || rawMethod === "help") {
    console.log("WzComparerR2 CLI — Available commands:");
    for (const t of TOOLS) {
      console.log(`  ${t.name.replace(/^wcr2_/, "").padEnd(20)} ${t.description}`);
    }
    process.exit(0);
  }

  const method = rawMethod.replace(/^wcr2_/, "");
  const args = parseCliArgs(rest);

  try {
    const result = await callWcr2(method, args);
    console.log(JSON.stringify(result, null, 2));
  } catch (err) {
    console.error(JSON.stringify({ ok: false, error: err.message || String(err) }, null, 2));
    process.exit(1);
  }
}

main();
