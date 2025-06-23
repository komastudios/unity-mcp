# Unity MCP Server - Claude Code Integration Guide

## Overview

Unity MCP (Model Context Protocol) Server provides a bridge between Claude Code and the Unity Editor, enabling AI-assisted Unity development through direct editor control and inspection capabilities.

This repository contains:
- **UnityMcpServer/**: Python-based MCP server that communicates with Unity
- **UnityMcpBridge/**: Unity package that runs inside the Unity Editor

## Architecture

```
Claude Code <--(MCP Protocol)--> Unity MCP Server <--(TCP)--> Unity Editor (with UnityMcpBridge)
```

## Setup Instructions

### Prerequisites

- Unity 2021.3 or later
- Python 3.12+
- `uv` package manager (for Python dependencies)
- Claude Code CLI

### Step 1: Install Unity Package

1. In Unity, open Package Manager (Window → Package Manager)
2. Click the "+" button → "Add package from disk..."
3. Navigate to `UnityMcpBridge/package.json` and select it
4. The package will install and add a "Unity MCP" menu to your Unity Editor

### Step 2: Configure Unity Editor

1. In Unity, go to Unity MCP → Open MCP Window
2. Start the MCP Bridge server (it will listen on the configured Unity port, default: 6400)
3. Keep Unity Editor open during your Claude Code session

### Step 3: Set Up MCP Server

The setup depends on your environment configuration:

#### Scenario A: Claude Code and Unity on Same Machine

If both Claude Code and Unity are running on the same Windows machine:

```bash
# Example: If this repo is cloned to C:\Projects\unity-mcp
# Note: Add port arguments if using non-default ports
claude mcp add unityMCP uv.exe -- --directory "C:\\Projects\\unity-mcp\\UnityMcpServer\\src" run server.py

# Or with custom ports (e.g., Unity: 6405, MCP: 6505)
claude mcp add unityMCP uv.exe -- --directory "C:\\Projects\\unity-mcp\\UnityMcpServer\\src" run server.py --unity-port 6405 --mcp-port 6505
```

#### Scenario B: Unity on Windows Host, Claude Code in WSL

When Unity runs on Windows but Claude Code runs in WSL:

1. First, ensure the Windows path is accessible from WSL:
   ```bash
   # Windows path C:\Projects\unity-mcp becomes /mnt/c/Projects/unity-mcp in WSL
   # Windows path D:\Development\unity-mcp becomes /mnt/d/Development/unity-mcp in WSL
   ```

2. Configure the MCP server with WSL paths:
   ```bash
   # Example: If repo is at C:\Projects\unity-mcp on Windows
   claude mcp add unityMCP uv -- --directory "/mnt/c/Projects/unity-mcp/UnityMcpServer/src" run server.py
   
   # Or with custom ports (e.g., Unity: 6405, MCP: 6505)
   claude mcp add unityMCP uv -- --directory "/mnt/c/Projects/unity-mcp/UnityMcpServer/src" run server.py --unity-port 6405 --mcp-port 6505
   ```

3. Configure network access (if needed):
   - The MCP server needs to reach Unity Editor on the Windows host
   - By default, it connects to `localhost:<unity-port>` (default: 6400)
   - If connection fails, you may need to use the Windows host IP:
     ```bash
     # Find Windows host IP from WSL
     cat /etc/resolv.conf | grep nameserver
     # This shows your Windows host IP (e.g., 172.26.144.1)
     ```
   - Update `UnityMcpServer/src/config.py` if needed to use the host IP

### Step 4: Verify Connection

Once setup is complete, verify the connection:

```bash
# In Claude Code, you should be able to use Unity tools
# For example:
"Use the Unity MCP tools to get the current scene hierarchy"
```

## Available Tools

The Unity MCP Server provides these tools to Claude Code:

- **manage_scene**: Load, save, create scenes, get hierarchy
- **manage_gameobject**: Create, modify, find GameObjects
- **manage_script**: Create and edit C# scripts
- **manage_asset**: Import, create, modify assets
- **manage_editor**: Control play mode, pause, stop
- **read_console**: Read Unity console logs
- **execute_menu_item**: Execute any Unity menu command
- **take_screenshot**: Capture Game or Scene view

## Troubleshooting

### Connection Issues

1. **Check Unity MCP Window**: Ensure the server is running (green status)
2. **Check Firewall**: The configured Unity port must be accessible (default: 6400)
3. **WSL Networking**: Use `ping` from WSL to test connectivity to Windows host

### Path Issues

- **Windows to WSL**: Remember to convert paths (C:\ → /mnt/c/)
- **Spaces in Paths**: Always quote paths containing spaces
- **Forward vs Backslashes**: Use appropriate slashes for your OS

### Common Errors

- **"Unity connection failed"**: Make sure Unity Editor is open with MCP Bridge running
- **"Module not found"**: Run `uv sync` in the UnityMcpServer/src directory
- **"Permission denied"**: Check file permissions, especially in WSL scenarios

## Development Workflow

### Making Changes to MCP Server

1. Edit files in `UnityMcpServer/src/`
2. No restart needed - changes take effect on next tool invocation
3. For major changes, restart Claude Code session

### Making Changes to Unity Package

1. Edit files in `UnityMcpBridge/`
2. Unity will automatically recompile
3. You may need to restart the MCP Bridge server in Unity

### Contributing

1. Fork this repository
2. Create a feature branch
3. Make your changes
4. Test with both local and WSL setups if possible
5. Submit a pull request

## Security Notes

- The MCP server only accepts connections from localhost by default
- No authentication is implemented - use only in development
- Be cautious when exposing ports in WSL/Windows scenarios