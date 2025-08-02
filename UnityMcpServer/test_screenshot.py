#!/usr/bin/env python3
"""
Test script to verify the screenshot functionality works correctly
"""

import json
import socket
import base64
from io import BytesIO
from PIL import Image

def test_unity_screenshot():
    """Test the Unity screenshot functionality directly"""
    try:
        # Connect to Unity
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.connect(('localhost', 6400))
        
        # Send screenshot command with correct format (type and params, not command and params)
        command = {
            "type": "take_screenshot",
            "params": {
                "view": "scene"
            }
        }
        
        message = json.dumps(command) + '\n'
        sock.send(message.encode('utf-8'))
        
        # Receive response
        response_data = sock.recv(1024 * 1024 * 10)  # 10MB buffer
        sock.close()
        
        print(f"Raw response data: {response_data[:500]}...")  # Show first 500 chars
        
        response = json.loads(response_data.decode('utf-8'))
        print("Unity Response:")
        print(f"Full response: {json.dumps(response, indent=2)}")
        
        # Check if the response has the expected structure
        if response.get('status') == 'success':
            result = response.get('result', {})
            print(f"Success: {result.get('success')}")
            
            if result.get('success'):
                data = result.get('data', {})
                print(f"View: {data.get('view')}")
                print(f"Width: {data.get('width')}")
                print(f"Height: {data.get('height')}")
                print(f"Is Play Mode: {data.get('isPlayMode')}")
                
                # Check if we have image data
                image_data = data.get('imageData')
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
                        
                        print("✅ Screenshot test PASSED - Image data is valid!")
                        return True
                        
                    except Exception as e:
                        print(f"❌ Failed to decode/verify image: {e}")
                        return False
                else:
                    print("❌ No image data in response")
                    return False
            else:
                print(f"❌ Unity command failed: {result.get('error')}")
                return False
        else:
            print(f"❌ Unity response failed: {response.get('error')}")
            return False
            
    except Exception as e:
        print(f"❌ Test failed with error: {e}")
        return False

if __name__ == "__main__":
    print("Testing Unity screenshot functionality...")
    success = test_unity_screenshot()
    if success:
        print("\n🎉 All tests passed! Screenshot functionality is working correctly.")
    else:
        print("\n💥 Tests failed. Screenshot functionality needs more work.")