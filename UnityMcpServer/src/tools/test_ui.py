#!/usr/bin/env python3
"""
Test script for Unity UI Management Tool
Tests all UI management functionality including Canvas, UI elements, layouts, and events.
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from tools.manage_ui import register_manage_ui_tools
from mcp.server.fastmcp import FastMCP
from mcp.server.context import Context
import json

def test_ui_management():
    """Test all UI management functionality."""
    
    # Create MCP instance and register tools
    mcp = FastMCP("test-ui")
    register_manage_ui_tools(mcp)
    
    # Get the manage_ui tool
    manage_ui = None
    for tool in mcp.tools:
        if tool.name == "manage_ui":
            manage_ui = tool.handler
            break
    
    if not manage_ui:
        print("❌ manage_ui tool not found!")
        return False
    
    ctx = Context()
    
    print("🧪 Testing Unity UI Management Tool")
    print("=" * 50)
    
    # Test 1: Setup EventSystem
    print("\n1. Testing EventSystem Setup...")
    try:
        result = manage_ui(
            ctx,
            action="setup_event_system",
            replace_existing=True,
            horizontal_axis="Horizontal",
            vertical_axis="Vertical",
            submit_button="Submit",
            cancel_button="Cancel"
        )
        print(f"✅ EventSystem setup: {result.get('success', False)}")
        if result.get('message'):
            print(f"   Message: {result['message']}")
    except Exception as e:
        print(f"❌ EventSystem setup failed: {e}")
    
    # Test 2: Create Canvas (Overlay)
    print("\n2. Testing Canvas Creation (Overlay)...")
    try:
        result = manage_ui(
            ctx,
            action="create_canvas",
            canvas_name="TestCanvas",
            render_mode="screen_space_overlay",
            ui_scale_mode="scale_with_screen_size",
            reference_resolution={"x": 1920, "y": 1080},
            match_width_or_height=0.5,
            sort_order=0
        )
        print(f"✅ Canvas creation: {result.get('success', False)}")
        if result.get('canvas_info'):
            print(f"   Canvas: {result['canvas_info']['name']}")
            print(f"   Render Mode: {result['canvas_info']['render_mode']}")
    except Exception as e:
        print(f"❌ Canvas creation failed: {e}")
    
    # Test 3: Create Canvas (Camera)
    print("\n3. Testing Canvas Creation (Camera)...")
    try:
        result = manage_ui(
            ctx,
            action="create_canvas",
            canvas_name="CameraCanvas",
            render_mode="screen_space_camera",
            camera_name="Main Camera",
            plane_distance=10.0,
            sort_order=1
        )
        print(f"✅ Camera Canvas creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Camera Canvas creation failed: {e}")
    
    # Test 4: Add UI Panel
    print("\n4. Testing UI Panel Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="panel",
            element_name="MainPanel",
            parent_name="TestCanvas",
            position={"x": 0, "y": 0, "z": 0},
            size={"x": 400, "y": 300},
            color={"r": 0.2, "g": 0.2, "b": 0.2, "a": 0.8}
        )
        print(f"✅ Panel creation: {result.get('success', False)}")
        if result.get('element_info'):
            print(f"   Panel: {result['element_info']['name']}")
    except Exception as e:
        print(f"❌ Panel creation failed: {e}")
    
    # Test 5: Add UI Button
    print("\n5. Testing UI Button Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="button",
            element_name="TestButton",
            parent_name="MainPanel",
            position={"x": 0, "y": 50, "z": 0},
            size={"x": 160, "y": 30},
            text="Click Me!",
            color={"r": 0.2, "g": 0.6, "b": 1.0, "a": 1.0}
        )
        print(f"✅ Button creation: {result.get('success', False)}")
        if result.get('element_info'):
            print(f"   Button: {result['element_info']['name']}")
    except Exception as e:
        print(f"❌ Button creation failed: {e}")
    
    # Test 6: Add UI Text
    print("\n6. Testing UI Text Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="text",
            element_name="InfoText",
            parent_name="MainPanel",
            position={"x": 0, "y": -50, "z": 0},
            size={"x": 200, "y": 50},
            text="Hello Unity UI!",
            color={"r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0}
        )
        print(f"✅ Text creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Text creation failed: {e}")
    
    # Test 7: Add Input Field
    print("\n7. Testing Input Field Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="input_field",
            element_name="NameInput",
            parent_name="MainPanel",
            position={"x": 0, "y": -100, "z": 0},
            size={"x": 200, "y": 30},
            text="Enter name..."
        )
        print(f"✅ Input Field creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Input Field creation failed: {e}")
    
    # Test 8: Add Slider
    print("\n8. Testing Slider Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="slider",
            element_name="VolumeSlider",
            parent_name="MainPanel",
            position={"x": 0, "y": -150, "z": 0},
            size={"x": 200, "y": 20}
        )
        print(f"✅ Slider creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Slider creation failed: {e}")
    
    # Test 9: Add Dropdown
    print("\n9. Testing Dropdown Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="dropdown",
            element_name="OptionsDropdown",
            parent_name="MainPanel",
            position={"x": 100, "y": 50, "z": 0},
            size={"x": 120, "y": 30}
        )
        print(f"✅ Dropdown creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Dropdown creation failed: {e}")
    
    # Test 10: Add Toggle
    print("\n10. Testing Toggle Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="toggle",
            element_name="EnableToggle",
            parent_name="MainPanel",
            position={"x": -100, "y": 50, "z": 0},
            size={"x": 20, "y": 20}
        )
        print(f"✅ Toggle creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Toggle creation failed: {e}")
    
    # Test 11: Add Scroll View
    print("\n11. Testing Scroll View Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="scroll_view",
            element_name="ContentScroll",
            parent_name="TestCanvas",
            position={"x": 300, "y": 0, "z": 0},
            size={"x": 200, "y": 300}
        )
        print(f"✅ Scroll View creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Scroll View creation failed: {e}")
    
    # Test 12: Add TextMeshPro
    print("\n12. Testing TextMeshPro Creation...")
    try:
        result = manage_ui(
            ctx,
            action="add_ui_element",
            element_type="textmeshpro",
            element_name="TMPText",
            parent_name="MainPanel",
            position={"x": 0, "y": 100, "z": 0},
            size={"x": 200, "y": 50},
            text="TextMeshPro Text",
            color={"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0}
        )
        print(f"✅ TextMeshPro creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ TextMeshPro creation failed: {e}")
    
    # Test 13: Modify UI Element
    print("\n13. Testing UI Element Modification...")
    try:
        result = manage_ui(
            ctx,
            action="modify_ui_element",
            element_name="TestButton",
            text="Modified Button",
            color={"r": 1.0, "g": 0.2, "b": 0.2, "a": 1.0},
            size={"x": 180, "y": 35}
        )
        print(f"✅ Element modification: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Element modification failed: {e}")
    
    # Test 14: Set Vertical Layout
    print("\n14. Testing Vertical Layout...")
    try:
        result = manage_ui(
            ctx,
            action="set_ui_layout",
            element_name="MainPanel",
            layout_type="vertical",
            padding={"left": 10, "right": 10, "top": 10, "bottom": 10},
            child_alignment="middle_center",
            child_control_size={"width": True, "height": False},
            child_force_expand={"width": False, "height": False}
        )
        print(f"✅ Vertical layout: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Vertical layout failed: {e}")
    
    # Test 15: Set Grid Layout
    print("\n15. Testing Grid Layout...")
    try:
        result = manage_ui(
            ctx,
            action="set_ui_layout",
            element_name="ContentScroll/Viewport/Content",
            layout_type="grid",
            cell_size={"x": 100, "y": 100},
            spacing={"x": 10, "y": 10},
            child_alignment="upper_left"
        )
        print(f"✅ Grid layout: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ Grid layout failed: {e}")
    
    # Test 16: Create UI Event
    print("\n16. Testing UI Event Creation...")
    try:
        result = manage_ui(
            ctx,
            action="create_ui_event",
            element_name="TestButton",
            event_type="onclick",
            script_name="UIController",
            method_name="OnButtonClick"
        )
        print(f"✅ UI event creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ UI event creation failed: {e}")
    
    # Test 17: Set UI Animation
    print("\n17. Testing UI Animation...")
    try:
        result = manage_ui(
            ctx,
            action="set_ui_animation",
            element_name="TestButton",
            animation_type="scale"
        )
        print(f"✅ UI animation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ UI animation failed: {e}")
    
    # Test 18: Get UI Information
    print("\n18. Testing UI Information Retrieval...")
    try:
        result = manage_ui(
            ctx,
            action="get_ui_info",
            element_name="TestButton"
        )
        print(f"✅ UI info retrieval: {result.get('success', False)}")
        if result.get('ui_info'):
            print(f"   Element: {result['ui_info']['name']}")
            print(f"   Type: {result['ui_info']['type']}")
    except Exception as e:
        print(f"❌ UI info retrieval failed: {e}")
    
    # Test 19: Create UI Prefab
    print("\n19. Testing UI Prefab Creation...")
    try:
        result = manage_ui(
            ctx,
            action="create_ui_prefab",
            element_name="MainPanel",
            prefab_path="Assets/UI/Prefabs/MainPanel.prefab"
        )
        print(f"✅ UI prefab creation: {result.get('success', False)}")
    except Exception as e:
        print(f"❌ UI prefab creation failed: {e}")
    
    # Test 20: Get Canvas Information
    print("\n20. Testing Canvas Information...")
    try:
        result = manage_ui(
            ctx,
            action="get_ui_info",
            canvas_name="TestCanvas"
        )
        print(f"✅ Canvas info retrieval: {result.get('success', False)}")
        if result.get('canvas_info'):
            print(f"   Canvas: {result['canvas_info']['name']}")
            print(f"   Elements: {len(result['canvas_info'].get('elements', []))}")
    except Exception as e:
        print(f"❌ Canvas info retrieval failed: {e}")
    
    print("\n" + "=" * 50)
    print("🎉 UI Management Tool testing completed!")
    print("Check Unity Editor to see the created UI elements.")
    
    return True

if __name__ == "__main__":
    test_ui_management()