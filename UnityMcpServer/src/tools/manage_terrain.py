from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, List, Optional
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_terrain_tools(mcp: FastMCP):
    """Register all terrain management tools with the MCP server."""

    @mcp.tool()
    def manage_terrain(
        ctx: Context,
        action: str,
        terrain_name: str = None,
        position: List[float] = None,
        size: List[float] = None,
        heightmap_resolution: int = None,
        detail_resolution: int = None,
        texture_settings: Dict[str, Any] = None,
        tree_settings: Dict[str, Any] = None,
        grass_settings: Dict[str, Any] = None,
        wind_settings: Dict[str, Any] = None,
        terrain_data_path: str = None,
        layer_index: int = None,
        brush_size: float = None,
        brush_strength: float = None,
        height_value: float = None,
        smooth_iterations: int = None,
        texture_path: str = None,
        texture_tiling: List[float] = None,
        tree_prefab_path: str = None,
        tree_density: float = None,
        grass_texture_path: str = None,
        grass_density: float = None,
        page_size: int = 50,
        page_number: int = 1
    ) -> Dict[str, Any]:
        """
        Comprehensive terrain management tool for Unity.
        
        Actions:
        - create_terrain: Create new terrain with specified settings
        - modify_terrain: Modify existing terrain properties
        - sculpt_terrain: Sculpt terrain height using brush tools
        - paint_texture: Paint textures on terrain surfaces
        - place_trees: Place trees and vegetation on terrain
        - place_grass: Place grass and detail meshes
        - configure_wind: Set up wind zones for terrain effects
        - bake_terrain: Bake terrain data for optimization
        - list_terrains: Get all terrain objects in scene
        - get_terrain_info: Get detailed terrain information
        - delete_terrain: Remove terrain from scene
        """
        
        try:
            # Get Unity connection
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            # Prepare command parameters
            params = {
                "action": action,
                "terrain_name": terrain_name,
                "position": position or [0, 0, 0],
                "size": size or [100, 30, 100],
                "heightmap_resolution": heightmap_resolution or 513,
                "detail_resolution": detail_resolution or 1024,
                "texture_settings": texture_settings or {},
                "tree_settings": tree_settings or {},
                "grass_settings": grass_settings or {},
                "wind_settings": wind_settings or {},
                "terrain_data_path": terrain_data_path,
                "layer_index": layer_index or 0,
                "brush_size": brush_size or 10.0,
                "brush_strength": brush_strength or 0.5,
                "height_value": height_value or 0.0,
                "smooth_iterations": smooth_iterations or 1,
                "texture_path": texture_path,
                "texture_tiling": texture_tiling or [1.0, 1.0],
                "tree_prefab_path": tree_prefab_path,
                "tree_density": tree_density or 0.5,
                "grass_texture_path": grass_texture_path,
                "grass_density": grass_density or 0.5,
                "page_size": page_size,
                "page_number": page_number
            }
            
            # Send command to Unity
            result = unity_conn.send_command("manage_terrain", params)
            
            # Cache the result if it's a list operation
            if action in ["list_terrains", "get_terrain_info"] and result.get("success"):
                cache = get_cache()
                cache_id = f"terrain_{action}_{hash(str(params))}"
                cache.set(cache_id, result, expire_time=300)  # 5 minutes
                result["cache_id"] = cache_id
            
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in terrain management: {str(e)}"
            }

    @mcp.tool()
    def terrain_heightmap_operations(
        ctx: Context,
        action: str,
        terrain_name: str,
        heightmap_data: List[List[float]] = None,
        import_path: str = None,
        export_path: str = None,
        format: str = "RAW",
        bit_depth: int = 16
    ) -> Dict[str, Any]:
        """
        Advanced heightmap operations for terrain.
        
        Actions:
        - import_heightmap: Import heightmap from file
        - export_heightmap: Export current heightmap to file
        - set_heightmap: Set heightmap data directly
        - get_heightmap: Get current heightmap data
        - generate_noise: Generate procedural noise heightmap
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"heightmap_{action}",
                "terrain_name": terrain_name,
                "heightmap_data": heightmap_data,
                "import_path": import_path,
                "export_path": export_path,
                "format": format,
                "bit_depth": bit_depth
            }
            
            result = unity_conn.send_command("manage_terrain", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in heightmap operations: {str(e)}"
            }

    @mcp.tool()
    def terrain_streaming_operations(
        ctx: Context,
        action: str,
        terrain_group_name: str = None,
        chunk_size: int = 256,
        load_distance: float = 500.0,
        unload_distance: float = 1000.0,
        terrain_chunks: List[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        """
        Terrain streaming and LOD operations for large worlds.
        
        Actions:
        - setup_streaming: Configure terrain streaming system
        - create_terrain_group: Create group of terrain chunks
        - load_terrain_chunk: Load specific terrain chunk
        - unload_terrain_chunk: Unload terrain chunk
        - get_streaming_status: Get current streaming status
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"streaming_{action}",
                "terrain_group_name": terrain_group_name,
                "chunk_size": chunk_size,
                "load_distance": load_distance,
                "unload_distance": unload_distance,
                "terrain_chunks": terrain_chunks or []
            }
            
            result = unity_conn.send_command("manage_terrain", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in terrain streaming: {str(e)}"
            }