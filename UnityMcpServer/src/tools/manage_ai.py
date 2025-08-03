from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, List, Optional
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_ai_tools(mcp: FastMCP):
    """Register all AI and navigation management tools with the MCP server."""

    @mcp.tool()
    def manage_ai(
        ctx: Context,
        action: str,
        agent_name: str = None,
        navmesh_settings: Dict[str, Any] = None,
        agent_settings: Dict[str, Any] = None,
        destination: List[float] = None,
        behavior_tree_path: str = None,
        state_machine_settings: Dict[str, Any] = None,
        obstacle_settings: Dict[str, Any] = None,
        area_settings: Dict[str, Any] = None,
        crowd_settings: Dict[str, Any] = None,
        pathfinding_settings: Dict[str, Any] = None,
        page_size: int = 50,
        page_number: int = 1
    ) -> Dict[str, Any]:
        """
        Comprehensive AI and navigation management tool for Unity.
        
        Actions:
        - create_navmesh_agent: Create NavMesh agent with AI capabilities
        - modify_agent: Modify existing NavMesh agent properties
        - set_destination: Set agent destination for pathfinding
        - create_behavior_tree: Create AI behavior tree
        - create_state_machine: Create AI state machine
        - add_obstacle: Add NavMesh obstacle
        - create_navmesh_area: Create custom NavMesh area
        - bake_navmesh: Bake NavMesh for scene
        - setup_crowd_simulation: Configure crowd simulation
        - list_agents: Get all NavMesh agents in scene
        - get_agent_info: Get detailed agent information
        - delete_agent: Remove NavMesh agent
        """
        
        try:
            # Get Unity connection
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            # Prepare command parameters
            params = {
                "action": action,
                "agent_name": agent_name,
                "navmesh_settings": navmesh_settings or {},
                "agent_settings": agent_settings or {
                    "speed": 3.5,
                    "angular_speed": 120.0,
                    "acceleration": 8.0,
                    "stopping_distance": 0.0,
                    "auto_traverse_off_mesh_link": True,
                    "auto_repath": True,
                    "radius": 0.5,
                    "height": 2.0,
                    "quality": "High",
                    "priority": 50
                },
                "destination": destination or [0, 0, 0],
                "behavior_tree_path": behavior_tree_path,
                "state_machine_settings": state_machine_settings or {},
                "obstacle_settings": obstacle_settings or {
                    "shape": "Capsule",
                    "center": [0, 1, 0],
                    "size": [1, 2, 1],
                    "carve": True,
                    "move_threshold": 0.1,
                    "time_to_stationary": 0.5
                },
                "area_settings": area_settings or {
                    "area_type": "Walkable",
                    "cost": 1.0
                },
                "crowd_settings": crowd_settings or {
                    "max_agents": 100,
                    "max_agent_radius": 2.0,
                    "max_obstacles": 50
                },
                "pathfinding_settings": pathfinding_settings or {
                    "area_mask": -1,
                    "max_path_length": 1000
                },
                "page_size": page_size,
                "page_number": page_number
            }
            
            # Send command to Unity
            result = unity_conn.send_command("manage_ai", params)
            
            # Cache the result if it's a list operation
            if action in ["list_agents", "get_agent_info"] and result.get("success"):
                cache = get_cache()
                cache_id = f"ai_{action}_{hash(str(params))}"
                cache.set(cache_id, result, expire_time=300)  # 5 minutes
                result["cache_id"] = cache_id
            
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in AI management: {str(e)}"
            }

    @mcp.tool()
    def navmesh_operations(
        ctx: Context,
        action: str,
        navmesh_data_path: str = None,
        bake_settings: Dict[str, Any] = None,
        surface_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Advanced NavMesh operations.
        
        Actions:
        - bake_navmesh: Bake NavMesh with custom settings
        - clear_navmesh: Clear existing NavMesh data
        - save_navmesh: Save NavMesh data to file
        - load_navmesh: Load NavMesh data from file
        - get_navmesh_info: Get NavMesh statistics and info
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"navmesh_{action}",
                "navmesh_data_path": navmesh_data_path,
                "bake_settings": bake_settings or {
                    "agent_radius": 0.5,
                    "agent_height": 2.0,
                    "agent_slope": 45.0,
                    "agent_climb": 0.4,
                    "ledge_drop_height": 0.0,
                    "jump_distance": 0.0,
                    "min_region_area": 2.0,
                    "manual_cell_size": False,
                    "cell_size": 0.16,
                    "accurate_placement": False
                },
                "surface_settings": surface_settings or {
                    "override_voxel_size": False,
                    "voxel_size": 0.16,
                    "override_tile_size": False,
                    "tile_size": 256,
                    "build_height_mesh": False
                }
            }
            
            result = unity_conn.send_command("manage_ai", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in NavMesh operations: {str(e)}"
            }

    @mcp.tool()
    def behavior_tree_operations(
        ctx: Context,
        action: str,
        tree_name: str = None,
        tree_data: Dict[str, Any] = None,
        node_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Behavior tree operations for AI.
        
        Actions:
        - create_behavior_tree: Create new behavior tree
        - modify_behavior_tree: Modify existing behavior tree
        - add_node: Add node to behavior tree
        - remove_node: Remove node from behavior tree
        - connect_nodes: Connect behavior tree nodes
        - save_tree: Save behavior tree to file
        - load_tree: Load behavior tree from file
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"behavior_tree_{action}",
                "tree_name": tree_name,
                "tree_data": tree_data or {},
                "node_settings": node_settings or {
                    "node_type": "Sequence",
                    "position": [0, 0],
                    "parameters": {}
                }
            }
            
            result = unity_conn.send_command("manage_ai", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in behavior tree operations: {str(e)}"
            }

    @mcp.tool()
    def pathfinding_operations(
        ctx: Context,
        action: str,
        start_position: List[float] = None,
        end_position: List[float] = None,
        agent_settings: Dict[str, Any] = None,
        path_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Advanced pathfinding operations.
        
        Actions:
        - calculate_path: Calculate path between two points
        - sample_position: Sample valid position on NavMesh
        - raycast: Perform NavMesh raycast
        - find_closest_edge: Find closest NavMesh edge
        - get_area_cost: Get area traversal cost
        - set_area_cost: Set area traversal cost
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"pathfinding_{action}",
                "start_position": start_position or [0, 0, 0],
                "end_position": end_position or [0, 0, 0],
                "agent_settings": agent_settings or {
                    "radius": 0.5,
                    "height": 2.0,
                    "area_mask": -1
                },
                "path_settings": path_settings or {
                    "max_distance": 100.0,
                    "sample_distance": 1.0
                }
            }
            
            result = unity_conn.send_command("manage_ai", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in pathfinding operations: {str(e)}"
            }