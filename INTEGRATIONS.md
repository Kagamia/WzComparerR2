# WzComparerR2 Multi-Platform AI Integration Guide

Connect **WzComparerR2** to **OpenAI ChatGPT and Codex plugins**, **Claude (Desktop & Code)**, **Cursor**, **Windsurf**, **Antigravity**, **Smithery**, and legacy **GPT Actions**.

## OpenAI ChatGPT Plugin Directory / GitHub Marketplace

This repository includes an Agent Plugins package at `.agents/plugins/wzcomparerr2/` and a GitHub marketplace catalog at `.agents/plugins/marketplace.json`. The package bundles the WzComparerR2 skills and a local stdio MCP server. It is marked **Desktop only** because it launches WzComparerR2 on the user's Windows computer.

### Install from a local repository (ChatGPT desktop)

1. Install Node.js 18 or later and build/deploy the WzComparerR2 bridge using `mcp/build-plugin.ps1` as described below.
2. Register the repository marketplace in Codex CLI:
   ```powershell
   codex plugin marketplace add https://github.com/Kagamia/WzComparerR2.git --sparse .agents/plugins
   ```
3. Restart the ChatGPT desktop app, open **Plugins**, select **WzComparerR2 Tools**, then install **WzComparerR2**. The local marketplace is separate from the public plugin directory and workspace GitHub imports.

The local MCP server can find the installation from `WCR2_INSTALL_DIR`, `~/.wcr2-mcp.json`, an active WzComparerR2 process, or the documented standard folders. Set `WCR2_INSTALL_DIR` (and optionally `WCR2_BASE_WZ`) in the user's environment when auto-detection does not find the installation.

### Import into a ChatGPT workspace

A ChatGPT workspace admin can import the GitHub marketplace from **Admin Console → Plugins → Add → Import marketplace**:

- **Source**: `https://github.com/Kagamia/WzComparerR2`
- **Path**: `.agents/plugins`
- **Branch**: `master`

The workspace catalog imports package updates from GitHub. The plugin still runs locally in the ChatGPT desktop app; installing it does not install WzComparerR2, Node.js, MapleStory data, or the MCP bridge binaries. An administrator controls workspace availability and installation policy.

### Public directory submission

The package format is prepared for OpenAI's plugin directory, but local MCP submissions are restricted to approved partners and require OpenAI allowlisting. This repository does not provide a hosted public MCP endpoint. Public listing therefore requires OpenAI submission access; it cannot be achieved by publishing this GitHub repository alone. See OpenAI's [desktop-only plugin submission guide](https://developers.openai.com/plugins/deploy/desktop-only).

---

## 1. Claude Desktop Integration

Claude Desktop natively supports the Model Context Protocol (MCP).

### Configuration
Open your Claude Desktop configuration file:
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

Add the `wzcomparerr2` server:
```json
{
  "mcpServers": {
    "wzcomparerr2": {
      "command": "node",
      "args": [
        "C:\\Path\\To\\WzComparerR2\\mcp\\wcr2-mcp-server.mjs"
      ]
    }
  }
}
```

Restart Claude Desktop. You will see a hammer icon 🔨 indicating that the 18 MapleStory tools are active!

---

## 2. Claude Code Plugin & Marketplace

The repository includes a Claude Code plugin at `.agents/plugins/wzcomparerr2/` and a marketplace catalog at `.claude-plugin/marketplace.json`. The package reuses the WzComparerR2 skills and starts the local MCP server through `.mcp.json`.

In a Claude Code session, add the GitHub marketplace and install the plugin:

```text
/plugin marketplace add Kagamia/WzComparerR2
/plugin install wzcomparerr2@wzcomparerr2
```

Or run the commands in a terminal:

```bash
claude plugin marketplace add Kagamia/WzComparerR2
claude plugin install wzcomparerr2@wzcomparerr2
```

Install Node.js 18+ and build/deploy the WzComparerR2 bridge using `mcp/build-plugin.ps1` before using the tools. The plugin runs locally on a machine that can run WzComparerR2 (Windows). Start a new Claude Code session, then use `/mcp` to check the server connection.

> [!TIP]
> **Plugin Cache & `WCR2_INSTALL_DIR`**: When installed via Claude Code's `/plugin install`, the plugin runs from the isolated Claude cache folder (`~/.claude/plugins/cache/...`). In this directory, repository-relative paths (`Build/bin/Release`) are not present.
> To ensure the MCP server always locates your installation, set the `WCR2_INSTALL_DIR` environment variable:
> ```powershell
> [Environment]::SetEnvironmentVariable("WCR2_INSTALL_DIR", "C:\Path\To\WzComparerR2", "User")
> ```
> Or run `powershell -File mcp/build-plugin.ps1` once on your machine to save it into `~/.wcr2-mcp.json`.

To validate the plugin and marketplace with Claude Code installed:

```bash
claude plugin validate .agents/plugins/wzcomparerr2
claude plugin validate .
```

### Direct MCP setup (without the plugin)

Add WzComparerR2 directly to Claude Code via CLI:

```bash
claude mcp add wzcomparerr2 -- node mcp/wcr2-mcp-server.mjs
```

Verify with:
```bash
claude mcp list
```

---

## Legacy: ChatGPT GPT Actions

The OpenAPI documents remain for existing GPT Actions workflows. OpenAI's current plugin directory uses Agent Plugins packages and MCP connections; new GPT creation and public publishing availability varies by account and workspace, and OpenAI has announced a transition from custom GPTs to plugins. Check the current [GPT sharing and publishing guidance](https://help.openai.com/en/articles/8798878-sharing-and-publishing-gpts) before starting a new Actions integration.

### Step 1: Start the Local Daemon or Bridge
Run `build-plugin.ps1` to ensure `WzComparerR2.McpHost.exe` is built, then start it (or launch `WzComparerR2.exe`):
```powershell
powershell -ExecutionPolicy Bypass -File mcp/build-plugin.ps1
```
The daemon runs locally on `http://127.0.0.1:21481`.

### Step 2: Expose via Tunnel (for ChatGPT to access)
Since ChatGPT cloud servers cannot reach `127.0.0.1` directly, expose the port using **Cloudflare Tunnel** or **ngrok**:
```bash
# Using Cloudflare Tunnel (free, no sign-in required):
cloudflared tunnel --url http://127.0.0.1:21481

# Or using ngrok:
ngrok http 21481
```
Copy your public HTTPS URL (e.g. `https://your-tunnel.trycloudflare.com`).

### Step 3: Configure an existing eligible GPT
1. In ChatGPT, open an existing GPT you can edit and add a new Action.
2. Go to the **Configure** tab:
   - **Name**: MapleStory WzComparerR2 Assistant
   - **Description**: Inspects MapleStory gear, skills, mobs, maps, and authentic tooltips using WzComparerR2.
3. Click **Create new action**:
   - In the **Schema** box, paste the contents of [`mcp/openapi.json`](mcp/openapi.json) or [`mcp/openapi.yaml`](mcp/openapi.yaml).
   - Change the `servers.url` at the top of the schema to your tunnel URL:
     ```yaml
     servers:
       - url: https://your-tunnel.trycloudflare.com
     ```
   - **Authentication**: None (or custom API Key header if you configure one).
4. Save the Action. Public GPT sharing may require a valid Privacy Policy URL for each Action. Personal ChatGPT accounts may not be able to create or publish GPTs; check the current OpenAI availability guidance above.

---

## 4. Smithery / MCP Marketplace Registry

[Smithery.ai](https://smithery.ai) is the open registry and marketplace for Model Context Protocol servers.

This repository includes [`smithery.yaml`](smithery.yaml) and [`mcp/package.json`](mcp/package.json).

### How to Publish to Smithery
1. Push your repository to GitHub (`https://github.com/Kagamia/WzComparerR2`).
2. Visit [smithery.ai](https://smithery.ai) and sign in with GitHub.
3. Click **Submit a Server** and paste your GitHub repository URL.
4. Smithery reads `smithery.yaml` and lists your MCP server on the marketplace.
5. Users worldwide can install it via one command:
   ```bash
   npx -y @smithery/cli install wzcomparerr2 --client claude
   ```

---

## 5. Cursor & Windsurf Integration

### Cursor
Add to your project's `.cursor/mcp.json` (already pre-configured in this repository):
```json
{
  "mcpServers": {
    "wzcomparerr2": {
      "command": "node",
      "args": ["mcp/wcr2-mcp-server.mjs"]
    }
  }
}
```

### Windsurf
Add to `~/.codeium/windsurf/mcp_config.json`:
```json
{
  "mcpServers": {
    "wzcomparerr2": {
      "command": "node",
      "args": ["C:\\Path\\To\\WzComparerR2\\mcp\\wcr2-mcp-server.mjs"]
    }
  }
}
```

---

## 6. Antigravity Integration

Antigravity auto-discovers this plugin via `.agents/plugins/wzcomparerr2/` and `.agents/skills/`:
- **Rules**: `.agents/plugins/wzcomparerr2/rules/AGENTS.md`
- **Skills**:
  - `wcr2-control`: Traverse WZ nodes, search localized strings, parse CharaSim domain objects.
  - `wcr2-asset-extractor`: Render tooltips, extract PNG/APNG/GIF/audio, render avatars.
  - `wcr2-plugin-dev`: Lua scripting & C# plugin development.
