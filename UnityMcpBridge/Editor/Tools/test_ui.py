"""
Test script for Unity UI System Management Tool

This script tests all UI management functionality including Canvas creation,
UI element management, layout systems, and event handling.
"""

import asyncio
import sys
import os

# Add the parent directory to the path to import the unity_client
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from unity_client import UnityClient
from manage_ui import ManageUI

async def test_ui_management():
    """Test all UI management functionality."""
    
    print("🎨 Testing Unity UI System Management...")
    
    # Initialize Unity client
    unity_client = UnityClient()
    ui_manager = ManageUI(unity_client)
    
    try:
        # Connect to Unity
        await unity_client.connect()
        print("✅ Connected to Unity")
        
        # Test 1: Create Canvas
        print("\n📋 Test 1: Creating Canvas...")
        canvas_result = await ui_manager.create_canvas(
            canvas_name="TestCanvas",
            render_mode="screen_space_overlay",
            ui_scale_mode="ScaleWithScreenSize",
            reference_resolution={"x": 1920, "y": 1080},
            match_width_or_height=0.5,
            sort_order=0
        )
        print(f"Canvas creation result: {canvas_result}")
        
        # Test 2: Setup Event System
        print("\n🎮 Test 2: Setting up Event System...")
        event_system_result = await ui_manager.setup_event_system(
            replace_existing=True,
            horizontal_axis="Horizontal",
            vertical_axis="Vertical",
            submit_button="Submit",
            cancel_button="Cancel"
        )
        print(f"Event system setup result: {event_system_result}")
        
        # Test 3: Create UI Elements
        print("\n🔘 Test 3: Creating UI Elements...")
        
        # Create a panel
        panel_result = await ui_manager.create_panel(
            name="MainPanel",
            parent="TestCanvas",
            position={"x": 0, "y": 0},
            size={"width": 400, "height": 300},
            color={"r": 0.8, "g": 0.8, "b": 0.8, "a": 1.0}
        )
        print(f"Panel creation result: {panel_result}")
        
        # Create a button
        button_result = await ui_manager.create_button(
            name="TestButton",
            parent="MainPanel",
            text="Click Me!",
            position={"x": 0, "y": 50},
            size={"width": 160, "height": 30}
        )
        print(f"Button creation result: {button_result}")
        
        # Create a text label
        text_result = await ui_manager.create_text_label(
            name="TitleText",
            text="UI Test Panel",
            parent="MainPanel",
            position={"x": 0, "y": 100},
            size={"width": 200, "height": 30}
        )
        print(f"Text creation result: {text_result}")
        
        # Create an input field
        input_result = await ui_manager.create_input_field(
            name="UserInput",
            placeholder="Enter your name...",
            parent="MainPanel",
            position={"x": 0, "y": 0},
            size={"width": 200, "height": 30}
        )
        print(f"Input field creation result: {input_result}")
        
        # Create a slider
        slider_result = await ui_manager.add_ui_element(
            element_type="slider",
            element_name="VolumeSlider",
            parent_name="MainPanel",
            position={"x": 0, "y": -50},
            size={"width": 200, "height": 20}
        )
        print(f"Slider creation result: {slider_result}")
        
        # Test 4: Modify UI Elements
        print("\n✏️ Test 4: Modifying UI Elements...")
        modify_result = await ui_manager.modify_ui_element(
            element_name="TestButton",
            color={"r": 0.2, "g": 0.8, "b": 0.2, "a": 1.0},
            text="Modified Button"
        )
        print(f"Button modification result: {modify_result}")
        
        # Test 5: Set UI Layout
        print("\n📐 Test 5: Setting UI Layout...")
        layout_result = await ui_manager.set_ui_layout(
            element_name="MainPanel",
            layout_type="vertical",
            padding={"left": 10, "right": 10, "top": 10, "bottom": 10},
            child_alignment="MiddleCenter",
            child_control_size={"width": False, "height": False},
            child_force_expand={"width": False, "height": False}
        )
        print(f"Layout setup result: {layout_result}")
        
        # Test 6: Create UI Events
        print("\n⚡ Test 6: Creating UI Events...")
        event_result = await ui_manager.create_ui_event(
            element_name="TestButton",
            event_type="onclick",
            script_name="UIController",
            method_name="OnButtonClick"
        )
        print(f"UI event creation result: {event_result}")
        
        # Test 7: Set UI Animation
        print("\n🎬 Test 7: Setting UI Animation...")
        animation_result = await ui_manager.set_ui_animation(
            element_name="TestButton",
            animation_type="scale"
        )
        print(f"UI animation setup result: {animation_result}")
        
        # Test 8: Create more complex UI elements
        print("\n🔧 Test 8: Creating Complex UI Elements...")
        
        # Create a dropdown
        dropdown_result = await ui_manager.add_ui_element(
            element_type="dropdown",
            element_name="OptionsDropdown",
            parent_name="MainPanel",
            position={"x": 0, "y": -100},
            size={"width": 160, "height": 30}
        )
        print(f"Dropdown creation result: {dropdown_result}")
        
        # Create a toggle
        toggle_result = await ui_manager.add_ui_element(
            element_type="toggle",
            element_name="EnableToggle",
            parent_name="MainPanel",
            position={"x": -80, "y": -130},
            size={"width": 160, "height": 20}
        )
        print(f"Toggle creation result: {toggle_result}")
        
        # Create a scroll view
        scrollview_result = await ui_manager.add_ui_element(
            element_type="scrollview",
            element_name="ContentScrollView",
            parent_name="TestCanvas",
            position={"x": 300, "y": 0},
            size={"width": 200, "height": 200}
        )
        print(f"Scroll view creation result: {scrollview_result}")
        
        # Test 9: Create Grid Layout
        print("\n🔲 Test 9: Creating Grid Layout...")
        
        # Create a grid panel
        grid_panel_result = await ui_manager.add_ui_element(
            element_type="panel",
            element_name="GridPanel",
            parent_name="TestCanvas",
            position={"x": -300, "y": 0},
            size={"width": 240, "height": 240}
        )
        print(f"Grid panel creation result: {grid_panel_result}")
        
        # Set grid layout
        grid_layout_result = await ui_manager.set_ui_layout(
            element_name="GridPanel",
            layout_type="grid",
            cell_size={"width": 50, "height": 50},
            spacing={"x": 5, "y": 5},
            padding={"left": 10, "right": 10, "top": 10, "bottom": 10}
        )
        print(f"Grid layout setup result: {grid_layout_result}")
        
        # Add grid items
        for i in range(9):
            grid_item_result = await ui_manager.add_ui_element(
                element_type="button",
                element_name=f"GridButton{i}",
                parent_name="GridPanel",
                size={"width": 50, "height": 50}
            )
            await ui_manager.modify_ui_element(
                element_name=f"GridButton{i}",
                text=str(i + 1)
            )
        
        print("Grid items created successfully")
        
        # Test 10: Get UI Information
        print("\n📊 Test 10: Getting UI Information...")
        
        # Get general UI info
        general_info = await ui_manager.get_ui_info()
        print(f"General UI info: {general_info}")
        
        # Get specific element info
        button_info = await ui_manager.get_ui_info("TestButton")
        print(f"Button info: {button_info}")
        
        # Test 11: Create UI Prefab
        print("\n💾 Test 11: Creating UI Prefab...")
        prefab_result = await ui_manager.create_ui_prefab(
            element_name="MainPanel",
            prefab_path="Assets/UI/Prefabs/MainPanel.prefab"
        )
        print(f"Prefab creation result: {prefab_result}")
        
        # Test 12: Create TextMeshPro Element
        print("\n📝 Test 12: Creating TextMeshPro Element...")
        tmp_result = await ui_manager.add_ui_element(
            element_type="textmeshpro",
            element_name="TMPText",
            parent_name="TestCanvas",
            position={"x": 0, "y": 200},
            size={"width": 300, "height": 50}
        )
        print(f"TextMeshPro creation result: {tmp_result}")
        
        await ui_manager.modify_ui_element(
            element_name="TMPText",
            text="TextMeshPro Sample Text",
            color={"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0}
        )
        
        # Test 13: Create Multiple Canvas Types
        print("\n🖼️ Test 13: Creating Different Canvas Types...")
        
        # Screen Space Camera Canvas
        camera_canvas_result = await ui_manager.create_canvas(
            canvas_name="CameraCanvas",
            render_mode="screen_space_camera",
            camera_name="Main Camera",
            plane_distance=10.0,
            sort_order=1
        )
        print(f"Camera canvas creation result: {camera_canvas_result}")
        
        # World Space Canvas
        world_canvas_result = await ui_manager.create_canvas(
            canvas_name="WorldCanvas",
            render_mode="world_space",
            sort_order=2
        )
        print(f"World canvas creation result: {world_canvas_result}")
        
        print("\n🎉 All UI management tests completed successfully!")
        
    except Exception as e:
        print(f"❌ Test failed with error: {e}")
        import traceback
        traceback.print_exc()
    
    finally:
        # Clean up
        await unity_client.disconnect()
        print("🔌 Disconnected from Unity")

if __name__ == "__main__":
    asyncio.run(test_ui_management())