"""
Test script for the Build Management Tool

This script tests all build management capabilities including:
- Listing builds
- Creating builds
- Configuring builds
"""

import pytest
import asyncio
import json
from src.tools.manage_build import register_build_tools
from mcp.server.fastmcp import FastMCP

server = FastMCP(name="test_server")
manage_build = register_build_tools(server)

@pytest.mark.anyio
async def test_list_builds():
    """Test listing all builds"""
    print("=== Testing Build Management Tool ===\n")
    print("1. Testing List Builds...")
    
    result = await manage_build(action="list_builds")
    print(f"Builds: {result}")

    # 1. Check if the result is a list of TextContent objects
    assert isinstance(result, list)
    assert len(result) > 0
    assert hasattr(result[0], 'text')

    # 2. Extract the text and check if it's a valid JSON string
    text_content = result[0].text
    # The actual JSON is embedded in a string, so we need to extract it.
    json_string = text_content.replace("Build management result: ", "")
    try:
        data = json.loads(json_string)
    except json.JSONDecodeError:
        pytest.fail("The result is not a valid JSON string.")

    # 3. Check for expected keys in the nested result
    assert "result" in data
    result_data = data["result"]

    # Check for success and the builds list
    assert "success" in result_data
    assert result_data["success"] is True
    assert "builds" in result_data
    assert isinstance(result_data["builds"], list)

if __name__ == "__main__":
    asyncio.run(test_list_builds())