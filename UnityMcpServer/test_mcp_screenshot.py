#!/usr/bin/env python3
"""
Test script to verify the MCP server screenshot functionality works correctly
"""

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), 'src'))

from tools.take_screenshot import register_take_screenshot_tools
from mcp.server.fastmcp import FastMCP
import asyncio
import base64
from io import BytesIO
from PIL import Image

def test_mcp_screenshot():
    """Test the MCP server screenshot functionality"""
    try:
        # Create MCP server instance
        mcp = FastMCP("test-unity-mcp")
        
        # Register screenshot tools
        register_take_screenshot_tools(mcp)
        
        # Create a mock context
        class MockContext:
            pass
        
        ctx = MockContext()
        
        # Get the registered take_screenshot function
        # The function is registered as a tool, so we need to call it directly
        # Let's access it through the mcp instance
        take_screenshot_tool = None
        for tool_name, tool_info in mcp._tools.items():
            if tool_name == "take_screenshot":
                take_screenshot_tool = tool_info.func
                break
        
        if not take_screenshot_tool:
            print("❌ take_screenshot tool not found in registered tools")
            return False
        
        # Call the screenshot function
        print("Calling take_screenshot function...")
        result = take_screenshot_tool(ctx, view="scene")
        
        print("MCP Screenshot Result:")
        print(f"Success: {result.get('success')}")
        
        if result.get('success'):
            metadata = result.get('metadata', {})
            print(f"View: {metadata.get('view')}")
            print(f"Width: {metadata.get('width')}")
            print(f"Height: {metadata.get('height')}")
            print(f"Is Play Mode: {metadata.get('isPlayMode')}")
            print(f"Format: {metadata.get('format')}")
            print(f"Final Size: {metadata.get('finalSizeBytes')} bytes")
            
            # Check if we have image data
            image_data = result.get('image_data')
            if image_data:
                print(f"Image data length: {len(image_data)} characters")
                
                # Try to decode and verify the image
                try:
                    image_bytes = base64.b64decode(image_data)
                    print(f"Decoded image size: {len(image_bytes)} bytes")
                    
                    # Try to open with PIL to verify it's a valid image
                    image = Image.open(BytesIO(image_bytes))
                    print(f"PIL Image format: {image.format}")
                    print(f"PIL Image size: {image.size}")
                    print(f"PIL Image mode: {image.mode}")
                    
                    print("✅ MCP Screenshot test PASSED - Image data is valid!")
                    return True
                    
                except Exception as e:
                    print(f"❌ Failed to decode/verify image: {e}")
                    return False
            else:
                print("❌ No image data in response")
                return False
        else:
            print(f"❌ MCP screenshot failed: {result}")
            return False
            
    except Exception as e:
        print(f"❌ Test failed with error: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    print("Testing MCP server screenshot functionality...")
    success = test_mcp_screenshot()
    if success:
        print("\n🎉 All MCP tests passed! Screenshot functionality is working correctly through MCP server.")
    else:
        print("\n💥 MCP tests failed. Screenshot functionality needs more work.")