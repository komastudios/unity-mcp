from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_scene_tools(mcp: FastMCP):
    """Register all scene management tools with the MCP server."""

    @mcp.tool()
    def manage_scene(
        ctx: Context,
        action: str,
        name: str = None,
        path: str = None,
        build_index: int = None,
        max_depth: int = None,  # Maximum hierarchy depth for get_hierarchy
        max_objects: int = None,  # Maximum number of objects to return
        include_components: bool = True,  # Whether to include component details
        include_inactive: bool = True,  # Whether to include inactive objects
        name_filter: str = None,  # Filter objects by name (contains)
        tag_filter: str = None,  # Filter objects by tag
        component_filters: list = None,  # Filter objects that have all specified components
        auto_adjust_depth: bool = True,  # Automatically reduce depth if response too large
        traversal_order: str = "breadth_first",  # "breadth_first" or "depth_first"
    ) -> Dict[str, Any]:
        """Manages Unity scenes (load, save, create, get hierarchy, etc.).

        Args:
            action: Operation (e.g., 'load', 'save', 'create', 'get_hierarchy').
            name: Scene name (no extension) for create/load/save.
            path: Asset path for scene operations (default: "Assets/").
            build_index: Build index for load/build settings actions.
            max_depth: Maximum hierarchy depth for get_hierarchy (default: unlimited).
            max_objects: Maximum number of objects to return (default: unlimited).
            include_components: Whether to include component details (default: True).
            include_inactive: Whether to include inactive objects (default: True).
            name_filter: Filter objects by name (contains).
            tag_filter: Filter objects by tag.
            component_filters: Filter objects that have all specified components.
            auto_adjust_depth: Automatically reduce depth if response too large (default: True).
            traversal_order: "breadth_first" or "depth_first" (default: "breadth_first").

        Returns:
            Dictionary with results ('success', 'message', 'data').
        """
        try:
            params = {
                "action": action,
                "name": name,
                "path": path,
                "buildIndex": build_index,
                "maxDepth": max_depth,
                "maxObjects": max_objects,
                "includeComponents": include_components,
                "includeInactive": include_inactive,
                "nameFilter": name_filter,
                "tagFilter": tag_filter,
                "componentFilters": component_filters,
                "autoAdjustDepth": auto_adjust_depth,
                "traversalOrder": traversal_order
            }
            params = {k: v for k, v in params.items() if v is not None}
            
            # Send command to Unity
            response = get_unity_connection().send_command("manage_scene", params)

            # Process response
            if response.get("success"):
                # Check if response was cached by unity_connection
                if response.get("cached"):
                    # Response was already cached, add action-specific examples
                    cache_id = response.get("cache_id")
                    response["data"]["example_filters"] = [
                        f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".hierarchy | length")',
                        f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".hierarchy[] | select(.name | contains(\"Player\"))")',
                        f'fetch_cached_response(cache_id="{cache_id}", action="get_page", page=1, page_size_kb=100)'
                    ]
                    return response
                
                data = response.get("data")
                
                # For get_hierarchy action, check if response is too large
                if action == "get_hierarchy" and data:
                    import json
                    data_str = json.dumps(data)
                    
                    # Estimate tokens (roughly 4 chars per token)
                    estimated_tokens = len(data_str) // 4
                    
                    # If auto_adjust_depth is enabled and response is too large, retry with reduced depth
                    if auto_adjust_depth and estimated_tokens > 20000 and (max_depth is None or max_depth > 1):
                        current_depth = data.get("maxDepth", max_depth or 10)
                        if current_depth > 1:
                            # Try with reduced depth
                            new_depth = max(1, current_depth - 1)
                            params["maxDepth"] = new_depth
                            
                            # If components were included, try without them first
                            if include_components:
                                params["includeComponents"] = False
                                retry_response = get_unity_connection().send_command("manage_scene", params)
                                if retry_response.get("success"):
                                    retry_data = retry_response.get("data")
                                    retry_str = json.dumps(retry_data)
                                    retry_tokens = len(retry_str) // 4
                                    
                                    if retry_tokens <= 20000:
                                        return {
                                            "success": True, 
                                            "message": f"Auto-adjusted response (depth: {new_depth}, components: false) to fit within token limit.",
                                            "data": retry_data,
                                            "auto_adjusted": True,
                                            "original_tokens": estimated_tokens,
                                            "adjusted_tokens": retry_tokens
                                        }
                            
                            # If still too large or components weren't the issue, reduce depth
                            params["maxDepth"] = new_depth
                            retry_response = get_unity_connection().send_command("manage_scene", params)
                            if retry_response.get("success"):
                                retry_data = retry_response.get("data")
                                retry_str = json.dumps(retry_data)
                                retry_tokens = len(retry_str) // 4
                                
                                if retry_tokens <= 20000:
                                    return {
                                        "success": True,
                                        "message": f"Auto-adjusted depth from {current_depth} to {new_depth} to fit within token limit.",
                                        "data": retry_data,
                                        "auto_adjusted": True,
                                        "original_tokens": estimated_tokens,
                                        "adjusted_tokens": retry_tokens
                                    }
                    
                    # If still too large, cache the response and return cache ID
                    if estimated_tokens > 20000:
                        # Cache the large response
                        cache = get_cache()
                        metadata = {
                            "tool": "manage_scene",
                            "action": action,
                            "scene_name": data.get("sceneName", "unknown"),
                            "total_objects": data.get("totalObjectsInScene", "unknown"),
                            "estimated_tokens": estimated_tokens,
                            "parameters": {
                                "max_depth": max_depth,
                                "max_objects": max_objects,
                                "include_components": include_components,
                                "filters": {
                                    "name_filter": name_filter,
                                    "tag_filter": tag_filter,
                                    "component_filters": component_filters
                                }
                            }
                        }
                        cache_id = cache.add(data, metadata)
                        
                        return {
                            "success": True,
                            "message": f"Scene hierarchy is large ({estimated_tokens} estimated tokens) and has been cached.",
                            "cached": True,
                            "cache_id": cache_id,
                            "data": {
                                "scene_name": data.get("sceneName", "unknown"),
                                "total_objects": data.get("totalObjectsInScene", "unknown"),
                                "estimated_tokens": estimated_tokens,
                                "size_kb": len(data_str) // 1024,
                                "usage_hint": "Use fetch_cached_response tool to retrieve data using the cache_id",
                                "example_filters": [
                                    f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".hierarchy | length")',
                                    f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".hierarchy[] | select(.name | contains(\"Player\"))")',
                                    f'fetch_cached_response(cache_id="{cache_id}", action="get_page", page=1, page_size_kb=100)'
                                ]
                            }
                        }
                
                # Check if any other large responses should be cached
                if data and action in ["get_build_settings", "get_active"]:
                    import json
                    data_str = json.dumps(data)
                    size_kb = len(data_str) // 1024
                    
                    if size_kb > 50:  # Cache responses larger than 50KB
                        cache = get_cache()
                        metadata = {
                            "tool": "manage_scene",
                            "action": action,
                            "size_kb": size_kb
                        }
                        cache_id = cache.add(data, metadata)
                        
                        return {
                            "success": True,
                            "message": response.get("message", "Scene operation successful.") + f" (Large response cached: {cache_id})",
                            "cached": True,
                            "cache_id": cache_id,
                            "data": {
                                "summary": data.get("summary", "Data cached due to size"),
                                "size_kb": size_kb,
                                "cache_id": cache_id
                            }
                        }
                
                return {"success": True, "message": response.get("message", "Scene operation successful."), "data": data}
            else:
                return {"success": False, "message": response.get("error", "An unknown error occurred during scene management.")}

        except Exception as e:
            return {"success": False, "message": f"Python error managing scene: {str(e)}"}