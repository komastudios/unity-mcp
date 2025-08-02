from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, List, Union, Optional
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_animation_tools(mcp: FastMCP):
    """Register all Animation System management tools with the MCP server."""

    @mcp.tool()
    def manage_animation(
        ctx: Context,
        action: str,
        # --- Common Parameters ---
        name: str = None,
        path: str = None,
        target: str = None,
        # --- Animation Clip Parameters ---
        length: float = None,
        looping: bool = None,
        curves: List[Dict[str, Any]] = None,
        # --- Animator Controller Parameters ---
        controller_path: str = None,
        parameters: List[Dict[str, Any]] = None,
        layers: List[Dict[str, Any]] = None,
        add_parameters: List[Dict[str, Any]] = None,
        remove_parameters: List[str] = None,
        add_layers: List[Dict[str, Any]] = None,
        # --- Timeline Parameters ---
        timeline_path: str = None,
        duration: float = None,
        tracks: List[Dict[str, Any]] = None,
        add_tracks: List[Dict[str, Any]] = None,
        # --- Animation Event Parameters ---
        clip_path: str = None,
        time: float = None,
        function_name: str = None,
        string_parameter: str = None,
        float_parameter: float = None,
        int_parameter: int = None,
        # --- Animator Parameter Control ---
        parameter_name: str = None,
        value: Union[bool, float, int, str] = None,
        # --- Animation Playback ---
        state_name: str = None,
        layer: int = None,
        normalized_time: float = None,
        # --- Animation Recording ---
        clip_name: str = None,
        save_path: str = None,
        # --- State Management ---
        layer_index: int = None,
        # --- Transition Parameters ---
        from_state: str = None,
        to_state: str = None,
        transition_duration: float = None,
        has_exit_time: bool = None,
        exit_time: float = None,
        conditions: List[Dict[str, Any]] = None,
        # --- Curve Modification ---
        property_name: str = None,
        target_path: str = None,
        component_type: str = None,
        keyframes: List[Dict[str, Any]] = None,
        # Meta arguments for standard reply contract
        summary: bool = None,
        page: int = None,
        pageSize: int = None,
        select: List[str] = None
    ) -> Dict[str, Any]:
        """Manages Unity Animation System: clips, controllers, timeline, events, and playback.

        This tool provides comprehensive animation management capabilities including:
        - Animation Clip creation and modification
        - Animator Controller setup and parameter control
        - Timeline asset creation and track management
        - Animation events and curve editing
        - Runtime animation playback and recording

        Args:
            action: Animation operation to perform:
                - 'create_clip': Create new Animation Clip
                - 'create_controller': Create new Animator Controller
                - 'modify_controller': Modify existing Animator Controller
                - 'create_timeline': Create new Timeline asset
                - 'modify_timeline': Modify existing Timeline asset
                - 'add_animation_event': Add event to Animation Clip
                - 'set_animator_parameter': Set Animator parameter value
                - 'play_animation': Play animation state
                - 'record_animation': Start animation recording
                - 'get_animation_info': Get animation information
                - 'create_state': Create Animator state
                - 'create_transition': Create state transition
                - 'modify_curve': Modify animation curve

            name: Asset name for creation operations
            path: Asset path for saving (defaults: Animations/, Animators/, Timeline/)
            target: Target GameObject name for runtime operations
            
            # Animation Clip Parameters
            length: Clip duration in seconds (default: 1.0)
            looping: Whether clip should loop (default: false)
            curves: List of animation curves with keyframes:
                [{"property": "localPosition.x", "target_path": "", "component_type": "Transform",
                  "keyframes": [{"time": 0, "value": 0}, {"time": 1, "value": 5}]}]
            
            # Animator Controller Parameters
            controller_path: Path to existing Animator Controller
            parameters: List of parameters to add:
                [{"name": "Speed", "type": "Float", "default_value": 1.0}]
            layers: List of layers to add:
                [{"name": "Base Layer", "weight": 1.0}]
            
            # Timeline Parameters
            timeline_path: Path to existing Timeline asset
            duration: Timeline duration in seconds (default: 10.0)
            tracks: List of tracks to add:
                [{"type": "animation", "name": "Character Animation"}]
            
            # Animation Events
            clip_path: Path to Animation Clip for event addition
            time: Event time in seconds
            function_name: Method name to call
            string_parameter: String parameter for event
            float_parameter: Float parameter for event
            int_parameter: Integer parameter for event
            
            # Runtime Control
            parameter_name: Animator parameter name
            value: Parameter value (bool/float/int based on parameter type)
            state_name: Animation state name to play
            layer: Animator layer index (default: 0)
            normalized_time: Playback start time (0-1, default: 0)
            
            # State & Transition Management
            layer_index: Animator layer index (default: 0)
            from_state: Source state name for transition
            to_state: Target state name for transition
            transition_duration: Transition duration
            has_exit_time: Whether transition has exit time
            exit_time: Exit time for transition
            conditions: Transition conditions:
                [{"parameter": "Speed", "mode": "Greater", "threshold": 0.1}]
            
            # Curve Editing
            property_name: Property name for curve (e.g., "localPosition.x")
            target_path: Target object path in hierarchy
            component_type: Component type name (default: "Transform")
            keyframes: List of keyframes:
                [{"time": 0, "value": 0}, {"time": 1, "value": 10}]

        Returns:
            Dictionary with operation results including success status, message, and relevant data.

        Examples:
            # Create Animation Clip
            manage_animation(action="create_clip", name="WalkCycle", length=2.0, looping=True)
            
            # Create Animator Controller
            manage_animation(action="create_controller", name="PlayerController",
                           parameters=[{"name": "Speed", "type": "Float", "default_value": 0}])
            
            # Play Animation
            manage_animation(action="play_animation", target="Player", state_name="Walk")
            
            # Set Animator Parameter
            manage_animation(action="set_animator_parameter", target="Player", 
                           parameter_name="Speed", value=5.0)
            
            # Add Animation Event
            manage_animation(action="add_animation_event", clip_path="Assets/Animations/Jump.anim",
                           time=0.5, function_name="OnJumpPeak")
        """
        try:
            # Prepare parameters, removing None values
            params = {
                "action": action,
                "name": name,
                "path": path,
                "target": target,
                "length": length,
                "looping": looping,
                "curves": curves,
                "controller_path": controller_path,
                "parameters": parameters,
                "layers": layers,
                "add_parameters": add_parameters,
                "remove_parameters": remove_parameters,
                "add_layers": add_layers,
                "timeline_path": timeline_path,
                "duration": duration,
                "tracks": tracks,
                "add_tracks": add_tracks,
                "clip_path": clip_path,
                "time": time,
                "function_name": function_name,
                "string_parameter": string_parameter,
                "float_parameter": float_parameter,
                "int_parameter": int_parameter,
                "parameter_name": parameter_name,
                "value": value,
                "state_name": state_name,
                "layer": layer,
                "normalized_time": normalized_time,
                "clip_name": clip_name,
                "save_path": save_path,
                "layer_index": layer_index,
                "from_state": from_state,
                "to_state": to_state,
                "duration": transition_duration,  # Rename to avoid conflict
                "has_exit_time": has_exit_time,
                "exit_time": exit_time,
                "conditions": conditions,
                "property_name": property_name,
                "target_path": target_path,
                "component_type": component_type,
                "keyframes": keyframes,
                # Meta arguments
                "summary": summary,
                "page": page,
                "pageSize": pageSize,
                "select": select
            }
            params = {k: v for k, v in params.items() if v is not None}
            
            # Send the command to Unity via the established connection
            response = get_unity_connection().send_command("manage_animation", params)

            # Check if the response indicates success
            if response.get("success"):
                data = response.get("data")
                
                # For get_animation_info action with large results, cache the response
                if action == "get_animation_info" and data:
                    import json
                    data_str = json.dumps(data)
                    size_kb = len(data_str) // 1024
                    
                    if size_kb > 50:  # Cache responses larger than 50KB
                        cache = get_cache()
                        metadata = {
                            "tool": "manage_animation",
                            "action": action,
                            "target": target or clip_path,
                            "size_kb": size_kb
                        }
                        cache_id = cache.add(data, metadata)
                        
                        return {
                            "success": True,
                            "message": f"Animation info retrieved (Large response cached)",
                            "cached": True,
                            "cache_id": cache_id,
                            "data": {
                                "size_kb": size_kb,
                                "cache_id": cache_id,
                                "usage_hint": "Use fetch_cached_response tool to retrieve data",
                                "example_filters": [
                                    f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".parameters | length")',
                                    f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".currentState")',
                                    f'fetch_cached_response(cache_id="{cache_id}", action="filter", jq_filter=".events")'
                                ]
                            }
                        }
                
                return {
                    "success": True, 
                    "message": response.get("message", "Animation operation successful."), 
                    "data": data
                }
            else:
                return {
                    "success": False, 
                    "message": response.get("error", "An unknown error occurred during animation management.")
                }

        except Exception as e:
            return {
                "success": False, 
                "message": f"Python error managing animation: {str(e)}"
            }