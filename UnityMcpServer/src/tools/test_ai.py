#!/usr/bin/env python3
"""
Test script for AI and navigation management functionality.
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from unity_connection import get_unity_connection
from tools.manage_ai import register_manage_ai_tools
from mcp.server.fastmcp import FastMCP
import asyncio

async def test_ai_operations():
    """Test basic AI and navigation operations."""
    print("Testing AI and navigation management operations...")
    
    try:
        # Get Unity connection
        unity_conn = get_unity_connection()
        if not unity_conn:
            print("❌ Failed to connect to Unity")
            return False
        
        # Create MCP instance and register tools
        mcp = FastMCP("test-ai")
        register_manage_ai_tools(mcp)
        
        # Test NavMesh agent creation
        print("\n1. Testing NavMesh agent creation...")
        result = await mcp.call_tool("manage_ai", {
            "action": "create_navmesh_agent",
            "agent_name": "TestAgent",
            "agent_settings": {
                "speed": 5.0,
                "angular_speed": 180.0,
                "acceleration": 10.0,
                "stopping_distance": 1.0,
                "radius": 0.5,
                "height": 2.0
            }
        })
        
        if result.get("success"):
            print(f"✅ NavMesh agent creation: {result.get('message')}")
        else:
            print(f"❌ NavMesh agent creation failed: {result.get('message')}")
        
        # Test agent listing
        print("\n2. Testing agent listing...")
        result = await mcp.call_tool("manage_ai", {
            "action": "list_agents"
        })
        
        if result.get("success"):
            agents = result.get("agents", [])
            print(f"✅ Found {len(agents)} NavMesh agents")
            for agent in agents:
                print(f"   - {agent.get('name')} (Speed: {agent.get('speed')})")
        else:
            print(f"❌ Agent listing failed: {result.get('message')}")
        
        # Test setting destination
        print("\n3. Testing destination setting...")
        result = await mcp.call_tool("manage_ai", {
            "action": "set_destination",
            "agent_name": "TestAgent",
            "destination": [10.0, 0.0, 10.0]
        })
        
        if result.get("success"):
            print(f"✅ Destination setting: {result.get('message')}")
        else:
            print(f"❌ Destination setting failed: {result.get('message')}")
        
        # Test NavMesh baking
        print("\n4. Testing NavMesh baking...")
        result = await mcp.call_tool("navmesh_operations", {
            "action": "bake_navmesh",
            "bake_settings": {
                "agent_radius": 0.5,
                "agent_height": 2.0,
                "agent_slope": 45.0,
                "agent_climb": 0.4
            }
        })
        
        if result.get("success"):
            print(f"✅ NavMesh baking: {result.get('message')}")
        else:
            print(f"❌ NavMesh baking failed: {result.get('message')}")
        
        # Test pathfinding
        print("\n5. Testing pathfinding...")
        result = await mcp.call_tool("pathfinding_operations", {
            "action": "calculate_path",
            "start_position": [0.0, 0.0, 0.0],
            "end_position": [10.0, 0.0, 10.0],
            "agent_settings": {
                "radius": 0.5,
                "height": 2.0
            }
        })
        
        if result.get("success"):
            print(f"✅ Pathfinding: {result.get('message')}")
        else:
            print(f"❌ Pathfinding failed: {result.get('message')}")
        
        print("\n✅ All AI tests completed!")
        return True
        
    except Exception as e:
        print(f"❌ Error during AI testing: {str(e)}")
        return False

if __name__ == "__main__":
    asyncio.run(test_ai_operations())