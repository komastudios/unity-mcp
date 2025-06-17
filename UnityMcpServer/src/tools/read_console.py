"""
Defines the read_console tool for accessing Unity Editor console messages.
"""
from typing import List, Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_read_console_tools(mcp: FastMCP):
    """Registers the read_console tool with the MCP server."""

    @mcp.tool()
    def read_console(
        ctx: Context,
        action: str = None,
        types: List[str] = None,
        count: int = None,
        filter_text: str = None,
        since_timestamp: str = None,
        format: str = None,
        include_stacktrace: bool = None,
        # Meta arguments for standard reply contract
        summary: bool = None,
        page: int = None,
        pageSize: int = None,
        select: List[str] = None
    ) -> Dict[str, Any]:
        """Gets messages from or clears the Unity Editor console.

        Args:
            ctx: The MCP context.
            action: Operation ('get' or 'clear').
            types: Message types to get ('error', 'warning', 'log', 'all').
            count: Max messages to return.
            filter_text: Text filter for messages.
            since_timestamp: Get messages after this timestamp (ISO 8601).
            format: Output format ('plain', 'detailed', 'json').
            include_stacktrace: Include stack traces in output.

        Returns:
            Dictionary with results. For 'get', includes 'data' (messages).
        """
        
        # Get the connection instance
        bridge = get_unity_connection()

        # Set defaults if values are None
        action = action if action is not None else 'get'
        types = types if types is not None else ['error', 'warning', 'log']
        format = format if format is not None else 'detailed'
        include_stacktrace = include_stacktrace if include_stacktrace is not None else True

        # Normalize action if it's a string
        if isinstance(action, str):
            action = action.lower()
        
        # Prepare parameters for the C# handler
        params_dict = {
            "action": action,
            "types": types,
            "count": count,
            "filterText": filter_text,
            "sinceTimestamp": since_timestamp,
            "format": format.lower() if isinstance(format, str) else format,
            "includeStacktrace": include_stacktrace,
            # Meta arguments
            "summary": summary,
            "page": page,
            "pageSize": pageSize,
            "select": select
        }

        # Remove None values unless it's 'count' (as None might mean 'all')
        params_dict = {k: v for k, v in params_dict.items() if v is not None or k == 'count'} 
        
        # Add count back if it was None, explicitly sending null might be important for C# logic
        if 'count' not in params_dict:
             params_dict['count'] = None 

        # Forward the command using the bridge's send_command method
        response = bridge.send_command("read_console", params_dict)
        
        # Check if response needs caching
        if response.get("success") and action == "get":
            data = response.get("data")
            if data:
                import json
                data_str = json.dumps(data)
                size_kb = len(data_str) // 1024
                
                # Cache large console outputs
                if size_kb > 50:  # Cache responses larger than 50KB
                    cache = get_cache()
                    message_count = len(data.get("messages", [])) if isinstance(data, dict) else len(data)
                    metadata = {
                        "tool": "read_console",
                        "action": action,
                        "types": types,
                        "filter_text": filter_text,
                        "message_count": message_count,
                        "size_kb": size_kb
                    }
                    cache_id = cache.add(data, metadata)
                    
                    return {
                        "success": True,
                        "message": f"Retrieved {message_count} console messages (Large response cached)",
                        "cached": True,
                        "cache_id": cache_id,
                        "data": {
                            "message_count": message_count,
                            "size_kb": size_kb,
                            "cache_id": cache_id,
                            "types": types,
                            "usage_hint": "Use fetch_cached_response tool to retrieve messages",
                            "example_filters": [
                                f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".messages | length")',
                                f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".messages[] | select(.type == \"error\")")',
                                f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".messages[0:20]")'  # First 20 messages
                            ]
                        }
                    }
        
        return response 