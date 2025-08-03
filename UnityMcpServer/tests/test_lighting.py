"""
Test script for the Lighting Management Tool

This script tests all lighting and rendering management capabilities including:
- Light creation and modification
- Material management
- Lightmap baking
- Post-processing setup
- Render pipeline configuration
- Reflection probes
- Light probe groups
"""

import asyncio
import json
from typing import Dict, Any

async def test_lighting_management():
    """Test all lighting management functionality"""
    
    print("=== Testing Lighting Management Tool ===\n")
    
    # Test light creation
    print("1. Testing Light Creation...")
    
    # Create directional light
    directional_light = await test_create_light(
        "MainDirectionalLight",
        "Directional",
        [0, 10, 0],
        [50, -30, 0],
        [1.0, 0.95, 0.8],
        1.0,
        shadows="Soft"
    )
    print(f"Directional Light: {directional_light}")
    
    # Create point light
    point_light = await test_create_light(
        "PointLight1",
        "Point",
        [5, 2, 0],
        [0, 0, 0],
        [1.0, 0.8, 0.6],
        2.0,
        range_value=10.0,
        shadows="Hard"
    )
    print(f"Point Light: {point_light}")
    
    # Create spot light
    spot_light = await test_create_light(
        "SpotLight1",
        "Spot",
        [0, 5, 5],
        [45, 0, 0],
        [0.8, 0.9, 1.0],
        3.0,
        range_value=15.0,
        spot_angle=30.0,
        shadows="Soft"
    )
    print(f"Spot Light: {spot_light}")
    
    print("\n2. Testing Light Modification...")
    
    # Modify light properties
    modified_light = await test_modify_light(
        "PointLight1",
        intensity=1.5,
        color=[1.0, 0.9, 0.7],
        range_value=8.0
    )
    print(f"Modified Light: {modified_light}")
    
    print("\n3. Testing Material Management...")
    
    # Create standard material
    standard_material = await test_create_material(
        "TestMaterial",
        "Standard",
        {
            "_Color": [0.8, 0.2, 0.2, 1.0],
            "_Metallic": 0.5,
            "_Glossiness": 0.8
        }
    )
    print(f"Standard Material: {standard_material}")
    
    # Create PBR material
    pbr_material = await test_create_material(
        "PBRMaterial",
        "Universal Render Pipeline/Lit",
        {
            "_BaseColor": [0.2, 0.8, 0.2, 1.0],
            "_Metallic": 0.0,
            "_Smoothness": 0.6,
            "_BumpScale": 1.0
        }
    )
    print(f"PBR Material: {pbr_material}")
    
    print("\n4. Testing Lighting Setup...")
    
    # Configure global lighting
    lighting_setup = await test_setup_lighting({
        "ambient_mode": "Skybox",
        "ambient_intensity": 1.0,
        "ambient_color": [0.5, 0.5, 0.5, 1.0],
        "fog_enabled": True,
        "fog_color": [0.8, 0.8, 0.9, 1.0],
        "fog_mode": "Linear",
        "fog_start": 10.0,
        "fog_end": 100.0
    })
    print(f"Lighting Setup: {lighting_setup}")
    
    print("\n5. Testing Lightmap Baking...")
    
    # Configure lightmap settings
    lightmap_bake = await test_bake_lightmaps({
        "lightmap_resolution": 1024,
        "lightmap_padding": 2,
        "compress_lightmaps": True,
        "ambient_occlusion": True,
        "ao_max_distance": 1.0,
        "directional_mode": "Directional",
        "indirect_resolution": 2.0,
        "lightmap_size": "1024"
    })
    print(f"Lightmap Baking: {lightmap_bake}")
    
    print("\n6. Testing Post-Processing...")
    
    # Setup post-processing effects
    post_processing = await test_setup_post_processing(
        "MainPostProcessProfile",
        {
            "bloom": {
                "enabled": True,
                "intensity": 0.5,
                "threshold": 1.0,
                "soft_knee": 0.5
            },
            "color_grading": {
                "enabled": True,
                "temperature": 0.0,
                "tint": 0.0,
                "saturation": 0.0,
                "contrast": 0.0
            },
            "vignette": {
                "enabled": True,
                "intensity": 0.3,
                "smoothness": 0.4
            }
        }
    )
    print(f"Post-Processing: {post_processing}")
    
    print("\n7. Testing Render Pipeline Configuration...")
    
    # Configure URP settings
    render_pipeline = await test_configure_render_pipeline(
        "URP",
        {
            "render_scale": 1.0,
            "upscaling_filter": "Auto",
            "fsrOverrideSharpness": False,
            "fsrSharpness": 0.92,
            "hdr": True,
            "msaa": "4x",
            "render_pipeline_asset": "UniversalRenderPipelineAsset"
        }
    )
    print(f"Render Pipeline: {render_pipeline}")
    
    print("\n8. Testing Reflection Probes...")
    
    # Create reflection probe
    reflection_probe = await test_create_reflection_probe(
        "MainReflectionProbe",
        "Realtime",
        {
            "resolution": 256,
            "hdr": True,
            "shadow_distance": 100.0,
            "clear_flags": "Skybox",
            "background": "Skybox",
            "culling_mask": -1,
            "intensity": 1.0,
            "box_projection": False,
            "blend_distance": 1.0,
            "box_size": [10.0, 10.0, 10.0],
            "box_offset": [0.0, 0.0, 0.0]
        }
    )
    print(f"Reflection Probe: {reflection_probe}")
    
    print("\n9. Testing Light Probe Groups...")
    
    # Create light probe group
    light_probe_group = await test_create_light_probe_group(
        "MainLightProbeGroup",
        [
            [0.0, 1.0, 0.0],
            [5.0, 1.0, 0.0],
            [-5.0, 1.0, 0.0],
            [0.0, 1.0, 5.0],
            [0.0, 1.0, -5.0],
            [2.5, 1.0, 2.5],
            [-2.5, 1.0, 2.5],
            [2.5, 1.0, -2.5],
            [-2.5, 1.0, -2.5]
        ]
    )
    print(f"Light Probe Group: {light_probe_group}")
    
    print("\n10. Testing Environment Settings...")
    
    # Configure environment lighting
    environment = await test_set_environment({
        "skybox_material": "Default-Skybox",
        "sun_source": "MainDirectionalLight",
        "ambient_mode": "Trilight",
        "ambient_sky_color": [0.5, 0.7, 1.0, 1.0],
        "ambient_equator_color": [0.4, 0.4, 0.4, 1.0],
        "ambient_ground_color": [0.2, 0.2, 0.2, 1.0],
        "default_reflection_mode": "Skybox",
        "default_reflection_resolution": 128,
        "reflection_bounces": 1,
        "reflection_intensity": 1.0
    })
    print(f"Environment: {environment}")
    
    print("\n11. Testing Information Retrieval...")
    
    # Get lighting information
    lighting_info = await test_get_lighting_info()
    print(f"Lighting Info: {json.dumps(lighting_info, indent=2)}")
    
    # List all lights
    lights_list = await test_list_lights()
    print(f"All Lights: {json.dumps(lights_list, indent=2)}")
    
    # List all materials
    materials_list = await test_list_materials()
    print(f"All Materials: {json.dumps(materials_list, indent=2)}")
    
    print("\n=== Lighting Management Tests Completed ===")

async def test_create_light(name: str, light_type: str, position: list, rotation: list, 
                          color: list, intensity: float, range_value: float = None, 
                          spot_angle: float = None, shadows: str = None) -> Dict[str, Any]:
    """Test light creation"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="create_light",
        light_name=name,
        light_type=light_type,
        position=position,
        rotation=rotation,
        color=color,
        intensity=intensity,
        range_value=range_value,
        spot_angle=spot_angle,
        shadows=shadows
    )

async def test_modify_light(name: str, **properties) -> Dict[str, Any]:
    """Test light modification"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="modify_light",
        light_name=name,
        **properties
    )

async def test_create_material(name: str, shader: str, properties: dict) -> Dict[str, Any]:
    """Test material creation"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="create_material",
        material_name=name,
        shader_name=shader,
        material_properties=properties
    )

async def test_setup_lighting(settings: dict) -> Dict[str, Any]:
    """Test lighting setup"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="setup_lighting",
        **settings
    )

async def test_bake_lightmaps(settings: dict) -> Dict[str, Any]:
    """Test lightmap baking"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="bake_lightmaps",
        lightmap_settings=settings
    )

async def test_setup_post_processing(profile: str, settings: dict) -> Dict[str, Any]:
    """Test post-processing setup"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="setup_post_processing",
        post_processing_profile=profile,
        post_processing_settings=settings
    )

async def test_configure_render_pipeline(pipeline: str, settings: dict) -> Dict[str, Any]:
    """Test render pipeline configuration"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="configure_render_pipeline",
        render_pipeline=pipeline,
        render_settings=settings
    )

async def test_create_reflection_probe(name: str, probe_type: str, settings: dict) -> Dict[str, Any]:
    """Test reflection probe creation"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="create_reflection_probe",
        probe_name=name,
        probe_type=probe_type,
        probe_settings=settings
    )

async def test_create_light_probe_group(name: str, positions: list) -> Dict[str, Any]:
    """Test light probe group creation"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="create_light_probe_group",
        light_probe_group_name=name,
        probe_positions=positions
    )

async def test_set_environment(settings: dict) -> Dict[str, Any]:
    """Test environment settings"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(
        action="set_environment",
        environment_settings=settings
    )

async def test_get_lighting_info() -> Dict[str, Any]:
    """Test getting lighting information"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(action="get_lighting_info")

async def test_list_lights() -> Dict[str, Any]:
    """Test listing all lights"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(action="list_lights")

async def test_list_materials() -> Dict[str, Any]:
    """Test listing all materials"""
    from src.tools.manage_lighting import manage_lighting
    
    return await manage_lighting(action="list_materials")

if __name__ == "__main__":
    asyncio.run(test_lighting_management())