from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_input_tools(mcp: FastMCP):
    """Register all input management tools with the MCP server."""

    @mcp.tool()
    def manage_input(
        ctx: Context,
        action: str,
        input_action_settings: Dict[str, Any] = None,
        control_scheme_settings: Dict[str, Any] = None,
        device_settings: Dict[str, Any] = None,
        legacy_input_settings: Dict[str, Any] = None,
        binding_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Manage input system components and operations in Unity.
        
        Actions:
        - create_input_actions: Create Input Action Asset
        - modify_input_actions: Modify existing Input Actions
        - create_control_scheme: Create Control Scheme
        - modify_control_scheme: Modify existing Control Scheme
        - add_input_binding: Add Input Binding to Action
        - modify_input_binding: Modify existing Input Binding
        - get_input_info: Get input system information
        - handle_device: Configure input device handling
        - set_legacy_input: Configure legacy input settings
        - enable_input_system: Enable/disable Input System
        """
        try:
            connection = get_unity_connection()
            
            command_data = {
                "type": "manage_input",
                "params": {
                    "action": action,
                    "input_action_settings": input_action_settings,
                    "control_scheme_settings": control_scheme_settings,
                    "device_settings": device_settings,
                    "legacy_input_settings": legacy_input_settings,
                    "binding_settings": binding_settings
                }
            }
            
            response = connection.send_command(command_data)
            
            # Cache the response for potential future use
            cache = get_cache()
            cache_key = f"input_{action}"
            cache.set(cache_key, response, ttl=300)  # 5 minutes TTL
            
            return response
            
        except Exception as e:
            return {
                "success": False,
                "error": f"Failed to manage input: {str(e)}"
            }