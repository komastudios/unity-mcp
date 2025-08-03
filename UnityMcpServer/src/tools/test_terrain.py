#!/usr/bin/env python3
"""
Test script for terrain management functionality.
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from unity_connection import get_unity_connection
from tools.manage_terrain import register_manage_terrain_tools
from mcp.server.fastmcp import FastMCP
import asyncio

async def test_terrain_operations():
    """Test basic terrain operations."""
    print("Testing terrain management operations...")
    
    try:
        # Get Unity connection
        unity_conn = get_unity_connection()
        if not unity_conn:
            print("❌ Failed to connect to Unity")
            return False
        
        # Create MCP instance and register tools
        mcp = FastMCP("test-terrain")
        register_manage_terrain_tools(mcp)
        
        # Test terrain creation
        print("\n1. Testing terrain creation...")
        result = await mcp.call_tool("manage_terrain", {
            "action": "create_terrain",
            "terrain_name": "TestTerrain",
            "terrain_settings": {
                "width": 100,
                "height": 100,
                "length": 100,
                "height_resolution": 513,
                "detail_resolution": 1024
            }
        })
        
        if result.get("success"):
            print(f"✅ Terrain creation: {result.get('message')}")
        else:
            print(f"❌ Terrain creation failed: {result.get('message')}")
        
        # Test terrain listing
        print("\n2. Testing terrain listing...")
        result = await mcp.call_tool("manage_terrain", {
            "action": "list_terrains"
        })
        
        if result.get("success"):
            terrains = result.get("terrains", [])
            print(f"✅ Found {len(terrains)} terrains")
            for terrain in terrains:
                print(f"   - {terrain.get('name')} (ID: {terrain.get('id')})")
        else:
            print(f"❌ Terrain listing failed: {result.get('message')}")
        
        # Test terrain modification
        print("\n3. Testing terrain modification...")
        result = await mcp.call_tool("manage_terrain", {
            "action": "modify_terrain",
            "terrain_name": "TestTerrain",
            "terrain_settings": {
                "pixel_error": 5,
                "base_map_distance": 1000,
                "shadow_casting_mode": "On"
            }
        })
        
        if result.get("success"):
            print(f"✅ Terrain modification: {result.get('message')}")
        else:
            print(f"❌ Terrain modification failed: {result.get('message')}")
        
        # Test heightmap operations
        print("\n4. Testing heightmap operations...")
        result = await mcp.call_tool("heightmap_operations", {
            "action": "generate_noise",
            "terrain_name": "TestTerrain",
            "noise_settings": {
                "scale": 0.01,
                "octaves": 4,
                "persistence": 0.5,
                "lacunarity": 2.0
            }
        })
        
        if result.get("success"):
            print(f"✅ Heightmap generation: {result.get('message')}")
        else:
            print(f"❌ Heightmap generation failed: {result.get('message')}")
        
        print("\n✅ All terrain tests completed!")
        return True
        
    except Exception as e:
        print(f"❌ Error during terrain testing: {str(e)}")
        return False

if __name__ == "__main__":
    asyncio.run(test_terrain_operations())