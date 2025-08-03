"""
Lighting and Rendering Management Tool for Unity MCP Server

This tool provides comprehensive lighting and rendering management capabilities for Unity,
including light creation/modification, material management, lightmap baking, post-processing,
render pipeline configuration, and more.
"""

from typing import Dict, Any, Optional, List
try:
    from ..core.tool_registry import register_tool
    from ..core.cache_manager import CacheManager
except ImportError:
    # Fallback for when running tests directly
    def register_tool(func):
        return func
    
    class CacheManager:
        def __init__(self, default_ttl=30):
            self.cache = {}
        def get(self, key):
            return None
        def set(self, key, value):
            pass
        def clear_pattern(self, pattern):
            pass

# Cache for read operations
lighting_cache = CacheManager(default_ttl=30)

@register_tool
def manage_lighting(
    action: str,
    light_name: Optional[str] = None,
    light_type: Optional[str] = None,
    position: Optional[List[float]] = None,
    rotation: Optional[List[float]] = None,
    color: Optional[List[float]] = None,
    intensity: Optional[float] = None,
    range_value: Optional[float] = None,
    spot_angle: Optional[float] = None,
    shadows: Optional[str] = None,
    cookie: Optional[str] = None,
    material_name: Optional[str] = None,
    shader_name: Optional[str] = None,
    material_properties: Optional[Dict[str, Any]] = None,
    texture_path: Optional[str] = None,
    texture_property: Optional[str] = None,
    lightmap_settings: Optional[Dict[str, Any]] = None,
    post_processing_profile: Optional[str] = None,
    post_processing_settings: Optional[Dict[str, Any]] = None,
    render_pipeline: Optional[str] = None,
    render_settings: Optional[Dict[str, Any]] = None,
    probe_name: Optional[str] = None,
    probe_type: Optional[str] = None,
    probe_settings: Optional[Dict[str, Any]] = None,
    light_probe_group_name: Optional[str] = None,
    probe_positions: Optional[List[List[float]]] = None,
    environment_settings: Optional[Dict[str, Any]] = None,
    **kwargs
) -> Dict[str, Any]:
    """
    Manage lighting and rendering in Unity.
    
    Actions:
    - create_light: Create a new light
    - modify_light: Modify an existing light
    - delete_light: Delete a light
    - get_light_info: Get information about a light
    - list_lights: List all lights in the scene
    - setup_lighting: Configure global lighting settings
    - bake_lightmaps: Bake lightmaps for the scene
    - create_material: Create a new material
    - modify_material: Modify an existing material
    - delete_material: Delete a material
    - get_material_info: Get information about a material
    - list_materials: List all materials in the project
    - assign_material: Assign a material to a renderer
    - setup_post_processing: Configure post-processing effects
    - configure_render_pipeline: Configure render pipeline settings
    - create_reflection_probe: Create a reflection probe
    - modify_reflection_probe: Modify a reflection probe
    - delete_reflection_probe: Delete a reflection probe
    - create_light_probe_group: Create a light probe group
    - modify_light_probe_group: Modify a light probe group
    - delete_light_probe_group: Delete a light probe group
    - get_lighting_info: Get comprehensive lighting information
    - set_environment: Configure environment lighting
    """
    
    # Use cache for read operations
    cache_key = f"lighting_{action}_{light_name or material_name or probe_name or ''}"
    if action in ['get_light_info', 'list_lights', 'get_material_info', 'list_materials', 'get_lighting_info']:
        cached_result = lighting_cache.get(cache_key)
        if cached_result is not None:
            return cached_result
    
    command_data = {
        "type": "manage_lighting",
        "params": {
            "action": action,
            "light_name": light_name,
            "light_type": light_type,
            "position": position,
            "rotation": rotation,
            "color": color,
            "intensity": intensity,
            "range": range_value,
            "spot_angle": spot_angle,
            "shadows": shadows,
            "cookie": cookie,
            "material_name": material_name,
            "shader_name": shader_name,
            "material_properties": material_properties,
            "texture_path": texture_path,
            "texture_property": texture_property,
            "lightmap_settings": lightmap_settings,
            "post_processing_profile": post_processing_profile,
            "post_processing_settings": post_processing_settings,
            "render_pipeline": render_pipeline,
            "render_settings": render_settings,
            "probe_name": probe_name,
            "probe_type": probe_type,
            "probe_settings": probe_settings,
            "light_probe_group_name": light_probe_group_name,
            "probe_positions": probe_positions,
            "environment_settings": environment_settings,
            **kwargs
        }
    }
    
    try:
        from ..core.unity_bridge import send_command_to_unity
    except ImportError:
        # Fallback for testing
        def send_command_to_unity(command_data):
            return {"success": False, "message": "Unity bridge not available"}
    
    result = send_command_to_unity(command_data)
    
    # Cache read operation results
    if action in ['get_light_info', 'list_lights', 'get_material_info', 'list_materials', 'get_lighting_info']:
        lighting_cache.set(cache_key, result)
    else:
        # Clear cache for write operations
        lighting_cache.clear_pattern("lighting_*")
    
    return result

def register_manage_lighting_tools():
    """Register all lighting management tools"""
    # The @register_tool decorator already handles registration
    pass