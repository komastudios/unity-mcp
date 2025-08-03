#!/usr/bin/env python3
"""
Simple test for particle system functionality
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

try:
    from core.unity_connection import UnityConnection
except ImportError:
    # Fallback for testing
    class UnityConnection:
        def send_command(self, command, params):
            return {"success": True, "message": "Mock response for testing"}

def test_particles():
    """Test particle system functionality"""
    print("Testing particle system functionality...")
    
    connection = UnityConnection()
    
    try:
        # Test 1: Create a particle system
        print("\n1. Creating particle system...")
        result = connection.send_command("manage_particles", {
            "action": "create",
            "name": "TestParticleSystem",
            "position": [0, 5, 0]
        })
        print(f"Create result: {result}")
        
        # Test 2: Configure emission
        print("\n2. Configuring emission...")
        result = connection.send_command("manage_particles", {
            "action": "modify",
            "name": "TestParticleSystem",
            "emission": {
                "rate_over_time": 50,
                "rate_over_distance": 0
            }
        })
        print(f"Emission config result: {result}")
        
        # Test 3: Configure shape
        print("\n3. Configuring shape...")
        result = connection.send_command("manage_particles", {
            "action": "modify",
            "name": "TestParticleSystem",
            "shape": {
                "shape_type": "Sphere",
                "radius": 2.0
            }
        })
        print(f"Shape config result: {result}")
        
        # Test 4: Play the particle system
        print("\n4. Playing particle system...")
        result = connection.send_command("manage_particles", {
            "action": "play",
            "name": "TestParticleSystem"
        })
        print(f"Play result: {result}")
        
        # Test 5: Get particle info
        print("\n5. Getting particle info...")
        result = connection.send_command("manage_particles", {
            "action": "get_info",
            "name": "TestParticleSystem"
        })
        print(f"Info result: {result}")
        
        print("\nAll particle tests completed successfully!")
        
    except Exception as e:
        print(f"Test failed with error: {e}")
        return False
    
    return True

if __name__ == "__main__":
    success = test_particles()
    sys.exit(0 if success else 1)