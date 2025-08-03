#!/usr/bin/env python3
"""
Test script for networking and multiplayer management functionality.
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from unity_connection import get_unity_connection
from tools.manage_networking import register_manage_networking_tools
from mcp.server.fastmcp import FastMCP
import asyncio

async def test_networking_operations():
    """Test basic networking and multiplayer operations."""
    print("Testing networking and multiplayer management operations...")
    
    try:
        # Get Unity connection
        unity_conn = get_unity_connection()
        if not unity_conn:
            print("❌ Failed to connect to Unity")
            return False
        
        # Create MCP instance and register tools
        mcp = FastMCP("test-networking")
        register_manage_networking_tools(mcp)
        
        # Test network object creation
        print("\n1. Testing network object creation...")
        result = await mcp.call_tool("manage_networking", {
            "action": "create_network_object",
            "network_object_name": "TestNetworkObject"
        })
        
        if result.get("success"):
            print(f"✅ Network object creation: {result.get('message')}")
        else:
            print(f"❌ Network object creation failed: {result.get('message')}")
        
        # Test network identity addition
        print("\n2. Testing network identity addition...")
        result = await mcp.call_tool("manage_networking", {
            "action": "add_network_identity",
            "network_object_name": "TestNetworkObject"
        })
        
        if result.get("success"):
            print(f"✅ Network identity addition: {result.get('message')}")
        else:
            print(f"❌ Network identity addition failed: {result.get('message')}")
        
        # Test connection listing
        print("\n3. Testing connection listing...")
        result = await mcp.call_tool("manage_networking", {
            "action": "list_connections"
        })
        
        if result.get("success"):
            connections = result.get("connections", [])
            print(f"✅ Found {len(connections)} network connections")
            for conn in connections:
                print(f"   - Client {conn.get('client_id')} ({conn.get('ip_address')}:{conn.get('port')})")
        else:
            print(f"❌ Connection listing failed: {result.get('message')}")
        
        # Test network statistics
        print("\n4. Testing network statistics...")
        result = await mcp.call_tool("manage_networking", {
            "action": "get_network_stats"
        })
        
        if result.get("success"):
            stats = result.get("network_stats", {})
            print(f"✅ Network statistics retrieved:")
            print(f"   - Total connections: {stats.get('total_connections')}")
            print(f"   - Bytes sent: {stats.get('bytes_sent')}")
            print(f"   - Average ping: {stats.get('average_ping')}ms")
        else:
            print(f"❌ Network statistics failed: {result.get('message')}")
        
        # Test transport operations
        print("\n5. Testing transport operations...")
        result = await mcp.call_tool("network_transport_operations", {
            "action": "setup_transport",
            "transport_type": "Unity Netcode",
            "transport_settings": {
                "connection_data": {
                    "address": "127.0.0.1",
                    "port": 7777
                }
            }
        })
        
        if result.get("success"):
            print(f"✅ Transport setup: {result.get('message')}")
        else:
            print(f"❌ Transport setup failed: {result.get('message')}")
        
        # Test RPC operations
        print("\n6. Testing RPC operations...")
        result = await mcp.call_tool("rpc_operations", {
            "action": "create_rpc",
            "rpc_name": "TestRPC",
            "target_settings": {
                "target_type": "All"
            },
            "delivery_settings": {
                "delivery_method": "Reliable"
            }
        })
        
        if result.get("success"):
            print(f"✅ RPC creation: {result.get('message')}")
        else:
            print(f"❌ RPC creation failed: {result.get('message')}")
        
        # Test lobby operations
        print("\n7. Testing lobby operations...")
        result = await mcp.call_tool("lobby_operations", {
            "action": "create_lobby",
            "lobby_settings": {
                "lobby_name": "TestLobby",
                "max_players": 4,
                "is_private": False
            }
        })
        
        if result.get("success"):
            print(f"✅ Lobby creation: {result.get('message')}")
        else:
            print(f"❌ Lobby creation failed: {result.get('message')}")
        
        # Test network diagnostics
        print("\n8. Testing network diagnostics...")
        result = await mcp.call_tool("network_diagnostics", {
            "action": "get_network_stats",
            "diagnostic_settings": {
                "include_detailed_stats": True
            }
        })
        
        if result.get("success"):
            print(f"✅ Network diagnostics: {result.get('message')}")
        else:
            print(f"❌ Network diagnostics failed: {result.get('message')}")
        
        print("\n✅ All networking tests completed!")
        return True
        
    except Exception as e:
        print(f"❌ Error during networking testing: {str(e)}")
        return False

if __name__ == "__main__":
    asyncio.run(test_networking_operations())