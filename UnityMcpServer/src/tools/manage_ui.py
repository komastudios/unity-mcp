from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_ui_tools(mcp: FastMCP):
    """Register all UI management tools with the MCP server."""

    @mcp.tool()
    def manage_ui(
        ctx: Context,
        action: str,
        canvas_name: str = None,
        render_mode: str = None,
        camera_name: str = None,
        ui_scale_mode: str = None,
        reference_resolution: Dict[str, float] = None,
        match_width_or_height: float = None,
        sort_order: int = None,
        plane_distance: float = None,
        element_type: str = None,
        element_name: str = None,
        parent_name: str = None,
        position: Dict[str, float] = None,
        size: Dict[str, float] = None,
        anchors: Dict[str, Dict[str, float]] = None,
        rotation: Dict[str, float] = None,
        scale: Dict[str, float] = None,
        text: str = None,
        color: Dict[str, float] = None,
        sprite_path: str = None,
        layout_type: str = None,
        padding: Dict[str, int] = None,
        child_alignment: str = None,
        child_control_size: Dict[str, bool] = None,
        child_force_expand: Dict[str, bool] = None,
        cell_size: Dict[str, float] = None,
        spacing: Dict[str, float] = None,
        event_type: str = None,
        script_name: str = None,
        method_name: str = None,
        animation_type: str = None,
        prefab_path: str = None,
        replace_existing: bool = False,
        horizontal_axis: str = None,
        vertical_axis: str = None,
        submit_button: str = None,
        cancel_button: str = None
    ) -> Dict[str, Any]:
        """
        Manage Unity UI system including Canvas, UI elements, layouts, and events.
        
        Actions:
        - create_canvas: Create a new Canvas for UI elements
        - add_ui_element: Add UI elements (button, text, image, etc.)
        - modify_ui_element: Modify existing UI element properties
        - set_ui_layout: Set layout group for UI elements
        - create_ui_event: Create UI events for elements
        - set_ui_animation: Set animations for UI elements
        - get_ui_info: Get information about UI elements or system
        - create_ui_prefab: Create prefab from UI element
        - setup_event_system: Setup EventSystem for UI interaction
        
        Args:
            action: The UI action to perform
            canvas_name: Name of the canvas
            render_mode: Canvas render mode (screen_space_overlay, screen_space_camera, world_space)
            camera_name: Camera for screen space camera mode
            ui_scale_mode: UI scale mode for Canvas Scaler
            reference_resolution: Reference resolution for scaling
            match_width_or_height: Match width or height value (0-1)
            sort_order: Sorting order for the canvas
            plane_distance: Plane distance for screen space camera mode
            element_type: Type of UI element (button, text, image, etc.)
            element_name: Name of the UI element
            parent_name: Name of parent object
            position: Position of the element
            size: Size of the element
            anchors: Anchor settings (min/max)
            rotation: Rotation of the element
            scale: Scale of the element
            text: Text content for text elements
            color: Color (r, g, b, a)
            sprite_path: Path to sprite for image elements
            layout_type: Type of layout (horizontal, vertical, grid)
            padding: Padding settings (left, right, top, bottom)
            child_alignment: Child alignment
            child_control_size: Child control size settings
            child_force_expand: Child force expand settings
            cell_size: Cell size for grid layout
            spacing: Spacing for grid layout
            event_type: Type of event (onclick, onvaluechanged)
            script_name: Name of the script to call
            method_name: Name of the method to call
            animation_type: Type of animation (fade, scale, slide, rotate)
            prefab_path: Path where to save the prefab
            replace_existing: Whether to replace existing EventSystem
            horizontal_axis: Horizontal axis name
            vertical_axis: Vertical axis name
            submit_button: Submit button name
            cancel_button: Cancel button name
            
        Returns:
            Dictionary containing the operation result and UI information
        """
        cache_key = f"manage_ui_{action}_{element_name or canvas_name or 'system'}"
        
        # Check cache first for read operations
        if action in ["get_ui_info"]:
            cached_result = get_cache().get(cache_key)
            if cached_result:
                return cached_result

        connection = get_unity_connection()
        
        # Build parameters dictionary
        params = {"action": action}
        
        # Add all non-None parameters
        if canvas_name is not None:
            params["canvas_name"] = canvas_name
        if render_mode is not None:
            params["render_mode"] = render_mode
        if camera_name is not None:
            params["camera_name"] = camera_name
        if ui_scale_mode is not None:
            params["ui_scale_mode"] = ui_scale_mode
        if reference_resolution is not None:
            params["reference_resolution"] = reference_resolution
        if match_width_or_height is not None:
            params["match_width_or_height"] = match_width_or_height
        if sort_order is not None:
            params["sort_order"] = sort_order
        if plane_distance is not None:
            params["plane_distance"] = plane_distance
        if element_type is not None:
            params["element_type"] = element_type
        if element_name is not None:
            params["element_name"] = element_name
        if parent_name is not None:
            params["parent_name"] = parent_name
        if position is not None:
            params["position"] = position
        if size is not None:
            params["size"] = size
        if anchors is not None:
            params["anchors"] = anchors
        if rotation is not None:
            params["rotation"] = rotation
        if scale is not None:
            params["scale"] = scale
        if text is not None:
            params["text"] = text
        if color is not None:
            params["color"] = color
        if sprite_path is not None:
            params["sprite_path"] = sprite_path
        if layout_type is not None:
            params["layout_type"] = layout_type
        if padding is not None:
            params["padding"] = padding
        if child_alignment is not None:
            params["child_alignment"] = child_alignment
        if child_control_size is not None:
            params["child_control_size"] = child_control_size
        if child_force_expand is not None:
            params["child_force_expand"] = child_force_expand
        if cell_size is not None:
            params["cell_size"] = cell_size
        if spacing is not None:
            params["spacing"] = spacing
        if event_type is not None:
            params["event_type"] = event_type
        if script_name is not None:
            params["script_name"] = script_name
        if method_name is not None:
            params["method_name"] = method_name
        if animation_type is not None:
            params["animation_type"] = animation_type
        if prefab_path is not None:
            params["prefab_path"] = prefab_path
        if replace_existing is not None:
            params["replace_existing"] = replace_existing
        if horizontal_axis is not None:
            params["horizontal_axis"] = horizontal_axis
        if vertical_axis is not None:
            params["vertical_axis"] = vertical_axis
        if submit_button is not None:
            params["submit_button"] = submit_button
        if cancel_button is not None:
            params["cancel_button"] = cancel_button
        
        result = connection.send_command("manage_ui", params)
        
        # Cache read operations
        if action in ["get_ui_info"] and result.get("success"):
            get_cache().set(cache_key, result, ttl=30)  # Cache for 30 seconds
        
        return result