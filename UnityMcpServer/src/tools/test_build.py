"""
Test script for build and deployment management functionality.
"""

import asyncio
import json
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


async def test_build_management():
    """Test build and deployment management operations."""
    
    server_params = StdioServerParameters(
        command="uv",
        args=["run", "server.py"],
        env=None
    )
    
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            
            print("Testing Build and Deployment Management...")
            
            # Test 1: Create build configuration
            print("\n1. Creating build configuration...")
            result = await session.call_tool(
                "manage_build",
                {
                    "operation": "create_build_config",
                    "config_name": "TestBuild",
                    "target_platform": "StandaloneWindows64",
                    "build_path": "Builds/TestBuild",
                    "development_build": True,
                    "script_debugging": True
                }
            )
            print(f"Create build config result: {result.content}")
            
            # Test 2: List build configurations
            print("\n2. Listing build configurations...")
            result = await session.call_tool(
                "manage_build",
                {
                    "operation": "list_build_configs"
                }
            )
            print(f"List build configs result: {result.content}")
            
            # Test 3: Build project
            print("\n3. Building project...")
            result = await session.call_tool(
                "manage_build",
                {
                    "operation": "build_project",
                    "config_name": "TestBuild"
                }
            )
            print(f"Build project result: {result.content}")
            
            # Test 4: Get build log
            print("\n4. Getting build log...")
            result = await session.call_tool(
                "manage_build",
                {
                    "operation": "get_build_log"
                }
            )
            print(f"Build log result: {result.content}")
            
            # Test 5: Create asset bundle
            print("\n5. Creating asset bundle...")
            result = await session.call_tool(
                "asset_bundle_operations",
                {
                    "operation": "create_bundle",
                    "bundle_name": "TestBundle",
                    "variant": "default"
                }
            )
            print(f"Create asset bundle result: {result.content}")
            
            # Test 6: Add assets to bundle
            print("\n6. Adding assets to bundle...")
            result = await session.call_tool(
                "asset_bundle_operations",
                {
                    "operation": "add_assets",
                    "bundle_name": "TestBundle",
                    "asset_paths": ["Assets/TestFolder/TestAsset.prefab"]
                }
            )
            print(f"Add assets to bundle result: {result.content}")
            
            # Test 7: Build asset bundles
            print("\n7. Building asset bundles...")
            result = await session.call_tool(
                "asset_bundle_operations",
                {
                    "operation": "build_bundles",
                    "output_path": "AssetBundles",
                    "target_platform": "StandaloneWindows64"
                }
            )
            print(f"Build asset bundles result: {result.content}")
            
            # Test 8: List asset bundles
            print("\n8. Listing asset bundles...")
            result = await session.call_tool(
                "asset_bundle_operations",
                {
                    "operation": "list_bundles"
                }
            )
            print(f"List asset bundles result: {result.content}")
            
            # Test 9: Build pipeline operations
            print("\n9. Testing build pipeline operations...")
            result = await session.call_tool(
                "build_pipeline_operations",
                {
                    "operation": "get_pipeline_info"
                }
            )
            print(f"Build pipeline info result: {result.content}")
            
            # Test 10: Deployment operations
            print("\n10. Testing deployment operations...")
            result = await session.call_tool(
                "deployment_operations",
                {
                    "operation": "deploy_build",
                    "build_path": "Builds/TestBuild",
                    "deployment_target": "local",
                    "deployment_config": {
                        "target_directory": "Deployed/TestBuild"
                    }
                }
            )
            print(f"Deploy build result: {result.content}")


if __name__ == "__main__":
    asyncio.run(test_build_management())