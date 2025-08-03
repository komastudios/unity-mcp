"""
Unity UI System Management Tool

This tool provides comprehensive UI system management capabilities for Unity projects,
including Canvas creation, UI element management, layout systems, and event handling.
"""

import json
from typing import Dict, Any, Optional, List
from ..unity_client import UnityClient

class ManageUI:
    def __init__(self, unity_client: UnityClient):
        self.unity_client = unity_client

    async def create_canvas(
        self,
        canvas_name: str = "Canvas",
        render_mode: str = "screen_space_overlay",
        camera_name: Optional[str] = None,
        ui_scale_mode: Optional[str] = None,
        reference_resolution: Optional[Dict[str, float]] = None,
        match_width_or_height: Optional[float] = None,
        sort_order: Optional[int] = None,
        plane_distance: Optional[float] = None
    ) -> Dict[str, Any]:
        """
        Create a new Canvas for UI elements.
        
        Args:
            canvas_name: Name of the canvas
            render_mode: Render mode (screen_space_overlay, screen_space_camera, world_space)
            camera_name: Camera for screen space camera mode
            ui_scale_mode: UI scale mode for Canvas Scaler
            reference_resolution: Reference resolution for scaling
            match_width_or_height: Match width or height value (0-1)
            sort_order: Sorting order for the canvas
            plane_distance: Plane distance for screen space camera mode
            
        Returns:
            Dictionary containing operation result and canvas information
        """
        params = {
            "canvas_name": canvas_name,
            "render_mode": render_mode
        }
        
        if camera_name:
            params["camera_name"] = camera_name
        if ui_scale_mode:
            params["ui_scale_mode"] = ui_scale_mode
        if reference_resolution:
            params["reference_resolution"] = reference_resolution
        if match_width_or_height is not None:
            params["match_width_or_height"] = match_width_or_height
        if sort_order is not None:
            params["sort_order"] = sort_order
        if plane_distance is not None:
            params["plane_distance"] = plane_distance
            
        return await self.unity_client.send_command("manage_ui", "create_canvas", params)

    async def add_ui_element(
        self,
        element_type: str,
        element_name: str,
        parent_name: Optional[str] = None,
        position: Optional[Dict[str, float]] = None,
        size: Optional[Dict[str, float]] = None,
        anchors: Optional[Dict[str, Dict[str, float]]] = None,
        rotation: Optional[Dict[str, float]] = None,
        scale: Optional[Dict[str, float]] = None
    ) -> Dict[str, Any]:
        """
        Add a UI element to the scene.
        
        Args:
            element_type: Type of UI element (button, text, image, etc.)
            element_name: Name of the UI element
            parent_name: Name of parent object
            position: Position of the element
            size: Size of the element
            anchors: Anchor settings (min/max)
            rotation: Rotation of the element
            scale: Scale of the element
            
        Returns:
            Dictionary containing operation result and element information
        """
        params = {
            "element_type": element_type,
            "element_name": element_name
        }
        
        if parent_name:
            params["parent_name"] = parent_name
        if position:
            params["position"] = position
        if size:
            params["size"] = size
        if anchors:
            params["anchors"] = anchors
        if rotation:
            params["rotation"] = rotation
        if scale:
            params["scale"] = scale
            
        return await self.unity_client.send_command("manage_ui", "add_ui_element", params)

    async def modify_ui_element(
        self,
        element_name: str,
        position: Optional[Dict[str, float]] = None,
        size: Optional[Dict[str, float]] = None,
        anchors: Optional[Dict[str, Dict[str, float]]] = None,
        rotation: Optional[Dict[str, float]] = None,
        scale: Optional[Dict[str, float]] = None,
        text: Optional[str] = None,
        color: Optional[Dict[str, float]] = None,
        sprite_path: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Modify properties of an existing UI element.
        
        Args:
            element_name: Name of the UI element to modify
            position: New position
            size: New size
            anchors: New anchor settings
            rotation: New rotation
            scale: New scale
            text: New text content
            color: New color (r, g, b, a)
            sprite_path: Path to new sprite
            
        Returns:
            Dictionary containing operation result
        """
        params = {"element_name": element_name}
        
        if position:
            params["position"] = position
        if size:
            params["size"] = size
        if anchors:
            params["anchors"] = anchors
        if rotation:
            params["rotation"] = rotation
        if scale:
            params["scale"] = scale
        if text:
            params["text"] = text
        if color:
            params["color"] = color
        if sprite_path:
            params["sprite_path"] = sprite_path
            
        return await self.unity_client.send_command("manage_ui", "modify_ui_element", params)

    async def set_ui_layout(
        self,
        element_name: str,
        layout_type: str,
        padding: Optional[Dict[str, int]] = None,
        child_alignment: Optional[str] = None,
        child_control_size: Optional[Dict[str, bool]] = None,
        child_force_expand: Optional[Dict[str, bool]] = None,
        cell_size: Optional[Dict[str, float]] = None,
        spacing: Optional[Dict[str, float]] = None
    ) -> Dict[str, Any]:
        """
        Set layout group for a UI element.
        
        Args:
            element_name: Name of the UI element
            layout_type: Type of layout (horizontal, vertical, grid)
            padding: Padding settings (left, right, top, bottom)
            child_alignment: Child alignment
            child_control_size: Child control size settings
            child_force_expand: Child force expand settings
            cell_size: Cell size for grid layout
            spacing: Spacing for grid layout
            
        Returns:
            Dictionary containing operation result
        """
        params = {
            "element_name": element_name,
            "layout_type": layout_type
        }
        
        if padding:
            params["padding"] = padding
        if child_alignment:
            params["child_alignment"] = child_alignment
        if child_control_size:
            params["child_control_size"] = child_control_size
        if child_force_expand:
            params["child_force_expand"] = child_force_expand
        if cell_size:
            params["cell_size"] = cell_size
        if spacing:
            params["spacing"] = spacing
            
        return await self.unity_client.send_command("manage_ui", "set_ui_layout", params)

    async def create_ui_event(
        self,
        element_name: str,
        event_type: str,
        script_name: Optional[str] = None,
        method_name: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Create UI event for an element.
        
        Args:
            element_name: Name of the UI element
            event_type: Type of event (onclick, onvaluechanged)
            script_name: Name of the script to call
            method_name: Name of the method to call
            
        Returns:
            Dictionary containing operation result
        """
        params = {
            "element_name": element_name,
            "event_type": event_type
        }
        
        if script_name:
            params["script_name"] = script_name
        if method_name:
            params["method_name"] = method_name
            
        return await self.unity_client.send_command("manage_ui", "create_ui_event", params)

    async def set_ui_animation(
        self,
        element_name: str,
        animation_type: str
    ) -> Dict[str, Any]:
        """
        Set animation for a UI element.
        
        Args:
            element_name: Name of the UI element
            animation_type: Type of animation (fade, scale, slide, rotate)
            
        Returns:
            Dictionary containing operation result
        """
        params = {
            "element_name": element_name,
            "animation_type": animation_type
        }
        
        return await self.unity_client.send_command("manage_ui", "set_ui_animation", params)

    async def get_ui_info(
        self,
        element_name: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Get information about UI elements or the UI system.
        
        Args:
            element_name: Name of specific UI element (optional)
            
        Returns:
            Dictionary containing UI information
        """
        params = {}
        if element_name:
            params["element_name"] = element_name
            
        return await self.unity_client.send_command("manage_ui", "get_ui_info", params)

    async def create_ui_prefab(
        self,
        element_name: str,
        prefab_path: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Create a prefab from a UI element.
        
        Args:
            element_name: Name of the UI element
            prefab_path: Path where to save the prefab
            
        Returns:
            Dictionary containing operation result
        """
        params = {"element_name": element_name}
        
        if prefab_path:
            params["prefab_path"] = prefab_path
            
        return await self.unity_client.send_command("manage_ui", "create_ui_prefab", params)

    async def setup_event_system(
        self,
        replace_existing: bool = False,
        horizontal_axis: Optional[str] = None,
        vertical_axis: Optional[str] = None,
        submit_button: Optional[str] = None,
        cancel_button: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Setup EventSystem for UI interaction.
        
        Args:
            replace_existing: Whether to replace existing EventSystem
            horizontal_axis: Horizontal axis name
            vertical_axis: Vertical axis name
            submit_button: Submit button name
            cancel_button: Cancel button name
            
        Returns:
            Dictionary containing operation result
        """
        params = {"replace_existing": replace_existing}
        
        if horizontal_axis:
            params["horizontal_axis"] = horizontal_axis
        if vertical_axis:
            params["vertical_axis"] = vertical_axis
        if submit_button:
            params["submit_button"] = submit_button
        if cancel_button:
            params["cancel_button"] = cancel_button
            
        return await self.unity_client.send_command("manage_ui", "setup_event_system", params)

    # Convenience methods for common UI elements
    async def create_button(
        self,
        name: str,
        parent: Optional[str] = None,
        text: str = "Button",
        position: Optional[Dict[str, float]] = None,
        size: Optional[Dict[str, float]] = None
    ) -> Dict[str, Any]:
        """Create a button with text."""
        result = await self.add_ui_element("button", name, parent, position, size)
        if result.get("success") and text != name:
            await self.modify_ui_element(name, text=text)
        return result

    async def create_text_label(
        self,
        name: str,
        text: str,
        parent: Optional[str] = None,
        position: Optional[Dict[str, float]] = None,
        size: Optional[Dict[str, float]] = None
    ) -> Dict[str, Any]:
        """Create a text label."""
        result = await self.add_ui_element("text", name, parent, position, size)
        if result.get("success"):
            await self.modify_ui_element(name, text=text)
        return result

    async def create_input_field(
        self,
        name: str,
        placeholder: str = "Enter text...",
        parent: Optional[str] = None,
        position: Optional[Dict[str, float]] = None,
        size: Optional[Dict[str, float]] = None
    ) -> Dict[str, Any]:
        """Create an input field."""
        return await self.add_ui_element("inputfield", name, parent, position, size)

    async def create_panel(
        self,
        name: str,
        parent: Optional[str] = None,
        position: Optional[Dict[str, float]] = None,
        size: Optional[Dict[str, float]] = None,
        color: Optional[Dict[str, float]] = None
    ) -> Dict[str, Any]:
        """Create a panel."""
        result = await self.add_ui_element("panel", name, parent, position, size)
        if result.get("success") and color:
            await self.modify_ui_element(name, color=color)
        return result