from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_physics_tools(mcp: FastMCP):
    """Register all physics management tools with the MCP server."""

    @mcp.tool()
    def manage_physics(
        ctx: Context,
        action: str,
        gameobject_name: str = None,
        rigidbody_settings: Dict[str, Any] = None,
        collider_settings: Dict[str, Any] = None,
        joint_settings: Dict[str, Any] = None,
        physics_material_settings: Dict[str, Any] = None,
        force_settings: Dict[str, Any] = None,
        raycast_settings: Dict[str, Any] = None,
        physics_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Manage physics components and operations in Unity.
        
        Actions:
        - add_rigidbody: Add Rigidbody component to GameObject
        - modify_rigidbody: Modify existing Rigidbody properties
        - add_collider: Add Collider component (Box, Sphere, Capsule, Mesh)
        - modify_collider: Modify existing Collider properties
        - add_joint: Add Joint component (Fixed, Hinge, Spring, etc.)
        - modify_joint: Modify existing Joint properties
        - simulate_physics: Control physics simulation
        - set_physics_settings: Configure global physics settings
        - get_physics_info: Get physics information from GameObject
        - create_physics_material: Create PhysicMaterial asset
        - apply_force: Apply force to Rigidbody
        - perform_raycast: Perform raycast operation
        """
        try:
            connection = get_unity_connection()
            
            command_data = {
                "type": "manage_physics",
                "params": {
                    "action": action,
                    "gameobject_name": gameobject_name,
                    "rigidbody_settings": rigidbody_settings,
                    "collider_settings": collider_settings,
                    "joint_settings": joint_settings,
                    "physics_material_settings": physics_material_settings,
                    "force_settings": force_settings,
                    "raycast_settings": raycast_settings,
                    "physics_settings": physics_settings
                }
            }
            
            response = connection.send_command(command_data)
            
            # Cache the response for potential future use
            cache = get_cache()
            cache_key = f"physics_{action}_{gameobject_name or 'global'}"
            cache.set(cache_key, response, ttl=300)  # 5 minutes TTL
            
            return response
            
        except Exception as e:
            return {
                "success": False,
                "error": f"Failed to manage physics: {str(e)}"
            }