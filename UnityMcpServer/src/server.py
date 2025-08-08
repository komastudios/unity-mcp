from mcp.server.fastmcp import FastMCP, Context, Image
import logging
import argparse
from dataclasses import dataclass
from contextlib import asynccontextmanager
from typing import AsyncIterator, Dict, Any, List
from config import config
from tools import register_all_tools
from unity_connection import get_unity_connection, UnityConnection
from unity_log_analyzer import UnityLogAnalyzer

# Configure logging using settings from config
logging.basicConfig(
    level=getattr(logging, config.log_level),
    format=config.log_format
)
logger = logging.getLogger("unity-mcp-server")

# Global connection state
_unity_connection: UnityConnection = None
_log_analyzer: UnityLogAnalyzer = None

@asynccontextmanager
async def server_lifespan(server: FastMCP) -> AsyncIterator[Dict[str, Any]]:
    """Handle server startup and shutdown."""
    global _unity_connection, _log_analyzer
    logger.info("Unity MCP Server starting up")

    # Initialize log analyzer
    _log_analyzer = UnityLogAnalyzer()

    try:
        _unity_connection = get_unity_connection()
        logger.info("Connected to Unity on startup")
    except Exception as e:
        logger.warning(f"Could not connect to Unity on startup: {str(e)}")
        _unity_connection = None

        # Analyze Unity logs to provide more context
        logger.info("Analyzing Unity logs for diagnostic information...")
        diagnostics = _log_analyzer.get_diagnostic_info()

        if diagnostics['safe_mode']:
            logger.error("Unity is running in Safe Mode! This prevents MCP Bridge from loading.")
            logger.error(f"Safe Mode detected in: {diagnostics['safe_mode_log']}")

            if diagnostics['compiler_errors']:
                logger.error(f"Found {len(diagnostics['compiler_errors'])} compiler errors:")
                for i, error in enumerate(diagnostics['compiler_errors'][:5]):  # Show first 5 errors
                    logger.error(f"  {i+1}. {error.get('line', 'Unknown error')}")
                    if 'file' in error:
                        logger.error(f"     File: {error['file']}:{error.get('line_number', '?')}")

        elif not diagnostics['unity_running']:
            logger.warning("Unity Editor does not appear to be running.")
            logger.warning("Please start Unity Editor and ensure the MCP Bridge is loaded.")
        else:
            logger.warning("Unity appears to be running but MCP Bridge is not responding.")
            logger.warning("Please check that the MCP Bridge package is properly installed.")

    try:
        # Yield the connection object and log analyzer so they can be attached to the context
        # The key 'bridge' matches how tools like read_console expect to access it (ctx.bridge)
        yield {"bridge": _unity_connection, "log_analyzer": _log_analyzer}
    finally:
        if _unity_connection:
            _unity_connection.disconnect()
            _unity_connection = None
        logger.info("Unity MCP Server shut down")

# Initialize MCP server
mcp = FastMCP(
    "unity-mcp-server",
    description="Unity Editor integration via Model Context Protocol",
    lifespan=server_lifespan,
    middleware=[],
    enable_cache=False
)

# Optional ASCII banner on startup for visibility
try:
    banner = [
        "===========================================",
        "   Unity MCP Server – Connected to Bridge  ",
        "==========================================="
    ]
    for line in banner:
        print(line)
except Exception:
    pass


# Register all tools
register_all_tools(mcp)

# Asset Creation Strategy

@mcp.prompt()
def asset_creation_strategy() -> str:
    """Guide for discovering and using Unity MCP tools effectively."""
    return (
        "Available Unity MCP Server Tools:\\n\\n"
        "- `manage_editor`: Controls editor state and queries info.\\n"
        "- `execute_menu_item`: Executes Unity Editor menu items by path.\\n"
        "- `read_console`: Reads or clears Unity console messages, with filtering options.\\n"
        "- `manage_scene`: Manages scenes.\\n"
        "- `manage_gameobject`: Manages GameObjects in the scene.\\n"
        "- `manage_script`: Manages C# script files.\\n"
        "- `manage_asset`: Manages prefabs and assets.\\n\\n"
        "Tips:\\n"
        "- Create prefabs for reusable GameObjects.\\n"
        "- Always include a camera and main light in your scenes.\\n"
    )

# Run the server
if __name__ == "__main__":
    # Parse command line arguments
    parser = argparse.ArgumentParser(description='Unity MCP Server')
    parser.add_argument('--unity-port', type=int, help='Unity Bridge port')
    parser.add_argument('--mcp-port', type=int, help='MCP Server port')
    parser.add_argument('--debug', action='store_true', help='Enable debug logging')
    args = parser.parse_args()

    # Update config with command line arguments if provided
    if args.unity_port:
        config.unity_port = args.unity_port
        logger.info(f"Using Unity port: {config.unity_port}")

    if args.mcp_port:
        config.mcp_port = args.mcp_port
        logger.info(f"Using MCP port: {config.mcp_port}")

    # Set logging level based on debug flag
    if args.debug:
        logging.getLogger().setLevel(logging.DEBUG)
        logger.setLevel(logging.DEBUG)
        logger.debug("Debug logging enabled")

    mcp.run(transport='stdio')
