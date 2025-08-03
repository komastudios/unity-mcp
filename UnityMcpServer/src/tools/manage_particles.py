"""
Particle System Management Tool for Unity MCP Server
Provides comprehensive particle system creation, modification, and control capabilities.
"""

import json
from typing import Any, Dict, List, Optional, Union
from mcp.server import Server
from mcp.types import Tool, TextContent

try:
    from ..unity_client import UnityClient
except ImportError:
    # Fallback for testing
    class UnityClient:
        def __init__(self):
            pass
        def send_command(self, command_data):
            return {"success": False, "message": "Unity client not available"}

def register_manage_particles_tools(mcp: Server):
    """Register all particle system management tools with the MCP server."""
    
    @mcp.call_tool()
    async def manage_particles(
        action: str,
        particle_system_name: Optional[str] = None,
        position: Optional[List[float]] = None,
        rotation: Optional[List[float]] = None,
        duration: Optional[float] = None,
        looping: Optional[bool] = None,
        start_lifetime: Optional[float] = None,
        start_speed: Optional[float] = None,
        start_size: Optional[float] = None,
        start_color: Optional[List[float]] = None,
        max_particles: Optional[int] = None,
        simulation_space: Optional[str] = None,
        enabled: Optional[bool] = None,
        rate_over_time: Optional[float] = None,
        rate_over_distance: Optional[float] = None,
        bursts: Optional[List[Dict[str, Any]]] = None,
        shape_type: Optional[str] = None,
        angle: Optional[float] = None,
        radius: Optional[float] = None,
        arc: Optional[float] = None,
        box_thickness: Optional[List[float]] = None,
        linear_velocity: Optional[List[float]] = None,
        space: Optional[str] = None,
        gradient: Optional[Dict[str, Any]] = None,
        size_curve: Optional[List[Dict[str, Any]]] = None,
        angular_velocity: Optional[float] = None,
        strength: Optional[float] = None,
        frequency: Optional[float] = None,
        octaves: Optional[int] = None,
        octave_multiplier: Optional[float] = None,
        octave_scale: Optional[float] = None,
        collision_type: Optional[str] = None,
        dampen: Optional[float] = None,
        bounce: Optional[float] = None,
        lifetime_loss: Optional[float] = None,
        sub_emitters: Optional[List[Dict[str, Any]]] = None,
        tiles_x: Optional[int] = None,
        tiles_y: Optional[int] = None,
        animation_type: Optional[str] = None,
        frame_over_time: Optional[float] = None,
        start_frame: Optional[float] = None,
        cycles: Optional[int] = None,
        ratio: Optional[float] = None,
        lifetime: Optional[float] = None,
        minimum_vertex_distance: Optional[float] = None,
        width_over_trail: Optional[float] = None,
        color_over_lifetime: Optional[List[float]] = None,
        material: Optional[str] = None,
        render_mode: Optional[str] = None,
        sorting_layer: Optional[str] = None,
        sorting_order: Optional[int] = None,
        with_children: Optional[bool] = None,
        stop_and_clear: Optional[bool] = None,
        time: Optional[float] = None,
        restart: Optional[bool] = None,
        fixed_time_step: Optional[bool] = None,
        material_name: Optional[str] = None,
        shader_name: Optional[str] = None,
        properties: Optional[Dict[str, Any]] = None
    ) -> List[TextContent]:
        """
        Manage particle systems in Unity.
        
        Actions:
        - create_particle_system: Create a new particle system
        - modify_particle_system: Modify existing particle system properties
        - delete_particle_system: Delete a particle system
        - get_particle_info: Get detailed information about a particle system
        - list_particle_systems: List all particle systems in the scene
        - configure_emission: Configure emission module settings
        - configure_shape: Configure shape module settings
        - configure_velocity: Configure velocity over lifetime module
        - configure_color_over_lifetime: Configure color over lifetime module
        - configure_size_over_lifetime: Configure size over lifetime module
        - configure_rotation_over_lifetime: Configure rotation over lifetime module
        - configure_noise: Configure noise module settings
        - configure_collision: Configure collision module settings
        - configure_sub_emitters: Configure sub emitters module
        - configure_texture_sheet_animation: Configure texture sheet animation
        - configure_trails: Configure trails module settings
        - configure_renderer: Configure particle system renderer
        - play_particle_system: Start playing a particle system
        - pause_particle_system: Pause a particle system
        - stop_particle_system: Stop a particle system
        - clear_particle_system: Clear all particles from a system
        - simulate_particle_system: Simulate particle system for a specific time
        - create_particle_material: Create a new material for particles
        - optimize_particle_system: Optimize particle system for better performance
        """
        
        # Build parameters dictionary, excluding None values
        params = {"action": action}
        
        # Add all non-None parameters
        if particle_system_name is not None:
            params["particle_system_name"] = particle_system_name
        if position is not None:
            params["position"] = position
        if rotation is not None:
            params["rotation"] = rotation
        if duration is not None:
            params["duration"] = duration
        if looping is not None:
            params["looping"] = looping
        if start_lifetime is not None:
            params["start_lifetime"] = start_lifetime
        if start_speed is not None:
            params["start_speed"] = start_speed
        if start_size is not None:
            params["start_size"] = start_size
        if start_color is not None:
            params["start_color"] = start_color
        if max_particles is not None:
            params["max_particles"] = max_particles
        if simulation_space is not None:
            params["simulation_space"] = simulation_space
        if enabled is not None:
            params["enabled"] = enabled
        if rate_over_time is not None:
            params["rate_over_time"] = rate_over_time
        if rate_over_distance is not None:
            params["rate_over_distance"] = rate_over_distance
        if bursts is not None:
            params["bursts"] = bursts
        if shape_type is not None:
            params["shape_type"] = shape_type
        if angle is not None:
            params["angle"] = angle
        if radius is not None:
            params["radius"] = radius
        if arc is not None:
            params["arc"] = arc
        if box_thickness is not None:
            params["box_thickness"] = box_thickness
        if linear_velocity is not None:
            params["linear_velocity"] = linear_velocity
        if space is not None:
            params["space"] = space
        if gradient is not None:
            params["gradient"] = gradient
        if size_curve is not None:
            params["size_curve"] = size_curve
        if angular_velocity is not None:
            params["angular_velocity"] = angular_velocity
        if strength is not None:
            params["strength"] = strength
        if frequency is not None:
            params["frequency"] = frequency
        if octaves is not None:
            params["octaves"] = octaves
        if octave_multiplier is not None:
            params["octave_multiplier"] = octave_multiplier
        if octave_scale is not None:
            params["octave_scale"] = octave_scale
        if collision_type is not None:
            params["type"] = collision_type
        if dampen is not None:
            params["dampen"] = dampen
        if bounce is not None:
            params["bounce"] = bounce
        if lifetime_loss is not None:
            params["lifetime_loss"] = lifetime_loss
        if sub_emitters is not None:
            params["sub_emitters"] = sub_emitters
        if tiles_x is not None:
            params["tiles_x"] = tiles_x
        if tiles_y is not None:
            params["tiles_y"] = tiles_y
        if animation_type is not None:
            params["animation_type"] = animation_type
        if frame_over_time is not None:
            params["frame_over_time"] = frame_over_time
        if start_frame is not None:
            params["start_frame"] = start_frame
        if cycles is not None:
            params["cycles"] = cycles
        if ratio is not None:
            params["ratio"] = ratio
        if lifetime is not None:
            params["lifetime"] = lifetime
        if minimum_vertex_distance is not None:
            params["minimum_vertex_distance"] = minimum_vertex_distance
        if width_over_trail is not None:
            params["width_over_trail"] = width_over_trail
        if color_over_lifetime is not None:
            params["color_over_lifetime"] = color_over_lifetime
        if material is not None:
            params["material"] = material
        if render_mode is not None:
            params["render_mode"] = render_mode
        if sorting_layer is not None:
            params["sorting_layer"] = sorting_layer
        if sorting_order is not None:
            params["sorting_order"] = sorting_order
        if with_children is not None:
            params["with_children"] = with_children
        if stop_and_clear is not None:
            params["stop_and_clear"] = stop_and_clear
        if time is not None:
            params["time"] = time
        if restart is not None:
            params["restart"] = restart
        if fixed_time_step is not None:
            params["fixed_time_step"] = fixed_time_step
        if material_name is not None:
            params["material_name"] = material_name
        if shader_name is not None:
            params["shader_name"] = shader_name
        if properties is not None:
            params["properties"] = properties
        
        # Send command to Unity
        client = UnityClient()
        result = await client.send_command("manage_particles", params)
        
        # Cache results for read operations
        if action in ["get_particle_info", "list_particle_systems"]:
            cache_key = f"particles_{action}_{particle_system_name or 'all'}"
            client.cache_result(cache_key, result)
        
        return [TextContent(type="text", text=json.dumps(result, indent=2))]

# Tool definitions for the MCP server
PARTICLE_TOOLS = [
    Tool(
        name="manage_particles",
        description="""
        Comprehensive particle system management tool for Unity.
        
        Key Features:
        - Create and configure particle systems with full control over all modules
        - Modify existing particle systems (main, emission, shape, velocity, color, size, rotation, noise, collision, sub-emitters, texture sheet animation, trails, renderer)
        - Control particle system playback (play, pause, stop, clear, simulate)
        - Create optimized particle materials
        - Performance optimization suggestions and automatic fixes
        - Comprehensive information retrieval and listing
        
        Common Actions:
        - create_particle_system: Create new particle system with basic settings
        - configure_emission: Set emission rate, bursts, and timing
        - configure_shape: Set emission shape (sphere, cone, box, etc.)
        - configure_color_over_lifetime: Set color gradients and alpha fading
        - play_particle_system: Start particle system playback
        - optimize_particle_system: Apply performance optimizations
        
        Supports all Unity particle system modules and advanced features like sub-emitters, trails, and texture sheet animation.
        """,
        inputSchema={
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": [
                        "create_particle_system", "modify_particle_system", "delete_particle_system",
                        "get_particle_info", "list_particle_systems", "configure_emission",
                        "configure_shape", "configure_velocity", "configure_color_over_lifetime",
                        "configure_size_over_lifetime", "configure_rotation_over_lifetime",
                        "configure_noise", "configure_collision", "configure_sub_emitters",
                        "configure_texture_sheet_animation", "configure_trails", "configure_renderer",
                        "play_particle_system", "pause_particle_system", "stop_particle_system",
                        "clear_particle_system", "simulate_particle_system", "create_particle_material",
                        "optimize_particle_system"
                    ],
                    "description": "The particle system action to perform"
                },
                "particle_system_name": {
                    "type": "string",
                    "description": "Name of the particle system to work with"
                },
                "position": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 3,
                    "maxItems": 3,
                    "description": "Position as [x, y, z] coordinates"
                },
                "rotation": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 3,
                    "maxItems": 3,
                    "description": "Rotation as [x, y, z] Euler angles"
                },
                "duration": {
                    "type": "number",
                    "description": "Duration of the particle system in seconds"
                },
                "looping": {
                    "type": "boolean",
                    "description": "Whether the particle system should loop"
                },
                "start_lifetime": {
                    "type": "number",
                    "description": "Lifetime of individual particles in seconds"
                },
                "start_speed": {
                    "type": "number",
                    "description": "Initial speed of particles"
                },
                "start_size": {
                    "type": "number",
                    "description": "Initial size of particles"
                },
                "start_color": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 3,
                    "maxItems": 4,
                    "description": "Initial color as [r, g, b] or [r, g, b, a] (0-1 range)"
                },
                "max_particles": {
                    "type": "integer",
                    "description": "Maximum number of particles"
                },
                "simulation_space": {
                    "type": "string",
                    "enum": ["local", "world", "custom"],
                    "description": "Simulation space for the particle system"
                },
                "enabled": {
                    "type": "boolean",
                    "description": "Whether the module/feature should be enabled"
                },
                "rate_over_time": {
                    "type": "number",
                    "description": "Emission rate over time (particles per second)"
                },
                "rate_over_distance": {
                    "type": "number",
                    "description": "Emission rate over distance"
                },
                "bursts": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "time": {"type": "number"},
                            "count": {"type": "integer"},
                            "cycles": {"type": "integer"},
                            "interval": {"type": "number"}
                        }
                    },
                    "description": "Burst emission settings"
                },
                "shape_type": {
                    "type": "string",
                    "enum": ["sphere", "hemisphere", "cone", "box", "mesh", "circle", "edge"],
                    "description": "Shape type for particle emission"
                },
                "angle": {
                    "type": "number",
                    "description": "Emission angle for cone shapes"
                },
                "radius": {
                    "type": "number",
                    "description": "Radius for spherical/circular shapes"
                },
                "arc": {
                    "type": "number",
                    "description": "Arc angle for circular shapes"
                },
                "box_thickness": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 3,
                    "maxItems": 3,
                    "description": "Box thickness as [x, y, z] for box shapes"
                },
                "linear_velocity": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 3,
                    "maxItems": 3,
                    "description": "Linear velocity as [x, y, z]"
                },
                "space": {
                    "type": "string",
                    "enum": ["local", "world"],
                    "description": "Space for velocity calculations"
                },
                "gradient": {
                    "type": "object",
                    "properties": {
                        "color_keys": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "color": {"type": "array", "items": {"type": "number"}},
                                    "time": {"type": "number"}
                                }
                            }
                        },
                        "alpha_keys": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "alpha": {"type": "number"},
                                    "time": {"type": "number"}
                                }
                            }
                        }
                    },
                    "description": "Color gradient definition"
                },
                "size_curve": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "time": {"type": "number"},
                            "value": {"type": "number"},
                            "in_tangent": {"type": "number"},
                            "out_tangent": {"type": "number"}
                        }
                    },
                    "description": "Animation curve for size over lifetime"
                },
                "angular_velocity": {
                    "type": "number",
                    "description": "Angular velocity for rotation over lifetime"
                },
                "strength": {
                    "type": "number",
                    "description": "Noise strength"
                },
                "frequency": {
                    "type": "number",
                    "description": "Noise frequency"
                },
                "octaves": {
                    "type": "integer",
                    "description": "Number of noise octaves"
                },
                "octave_multiplier": {
                    "type": "number",
                    "description": "Noise octave multiplier"
                },
                "octave_scale": {
                    "type": "number",
                    "description": "Noise octave scale"
                },
                "collision_type": {
                    "type": "string",
                    "enum": ["planes", "world"],
                    "description": "Type of collision detection"
                },
                "dampen": {
                    "type": "number",
                    "description": "Collision damping factor"
                },
                "bounce": {
                    "type": "number",
                    "description": "Collision bounce factor"
                },
                "lifetime_loss": {
                    "type": "number",
                    "description": "Lifetime loss on collision"
                },
                "sub_emitters": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string", "enum": ["birth", "death", "collision"]}
                        }
                    },
                    "description": "Sub-emitter configurations"
                },
                "tiles_x": {
                    "type": "integer",
                    "description": "Number of tiles in X direction for texture sheet animation"
                },
                "tiles_y": {
                    "type": "integer",
                    "description": "Number of tiles in Y direction for texture sheet animation"
                },
                "animation_type": {
                    "type": "string",
                    "enum": ["wholesheetonce", "singlerow"],
                    "description": "Type of texture sheet animation"
                },
                "frame_over_time": {
                    "type": "number",
                    "description": "Frame rate for texture sheet animation"
                },
                "start_frame": {
                    "type": "number",
                    "description": "Starting frame for texture sheet animation"
                },
                "cycles": {
                    "type": "integer",
                    "description": "Number of animation cycles"
                },
                "ratio": {
                    "type": "number",
                    "description": "Trail ratio (0-1)"
                },
                "lifetime": {
                    "type": "number",
                    "description": "Trail lifetime"
                },
                "minimum_vertex_distance": {
                    "type": "number",
                    "description": "Minimum distance between trail vertices"
                },
                "width_over_trail": {
                    "type": "number",
                    "description": "Trail width multiplier"
                },
                "color_over_lifetime": {
                    "type": "array",
                    "items": {"type": "number"},
                    "minItems": 3,
                    "maxItems": 4,
                    "description": "Trail color over lifetime as [r, g, b, a]"
                },
                "material": {
                    "type": "string",
                    "description": "Path to material asset"
                },
                "render_mode": {
                    "type": "string",
                    "enum": ["billboard", "stretch", "horizontalbillboard", "verticalbillboard", "mesh"],
                    "description": "Particle rendering mode"
                },
                "sorting_layer": {
                    "type": "string",
                    "description": "Sorting layer name"
                },
                "sorting_order": {
                    "type": "integer",
                    "description": "Sorting order within layer"
                },
                "with_children": {
                    "type": "boolean",
                    "description": "Whether to include child particle systems"
                },
                "stop_and_clear": {
                    "type": "boolean",
                    "description": "Whether to clear particles when stopping"
                },
                "time": {
                    "type": "number",
                    "description": "Time to simulate in seconds"
                },
                "restart": {
                    "type": "boolean",
                    "description": "Whether to restart simulation from beginning"
                },
                "fixed_time_step": {
                    "type": "boolean",
                    "description": "Whether to use fixed time step for simulation"
                },
                "material_name": {
                    "type": "string",
                    "description": "Name for new material"
                },
                "shader_name": {
                    "type": "string",
                    "description": "Shader name for new material"
                },
                "properties": {
                    "type": "object",
                    "description": "Material properties as key-value pairs"
                }
            },
            "required": ["action"]
        }
    )
]