#!/usr/bin/env python3
"""
Test script to verify Unity MCP Server functionality.
Tests connection handling, diagnostics, and graceful degradation.
"""

import asyncio
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from unity_connection import UnityConnection
from unity_log_analyzer import UnityLogAnalyzer
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("unity-mcp-test")

def test_connection():
    """Test Unity connection."""
    logger.info("Testing Unity connection...")
    conn = UnityConnection()
    
    try:
        if conn.connect():
            logger.info("✓ Successfully connected to Unity")
            
            # Try ping
            try:
                result = conn.send_command("ping")
                logger.info(f"✓ Ping successful: {result}")
                return True
            except Exception as e:
                logger.error(f"✗ Ping failed: {e}")
                return False
        else:
            logger.warning("✗ Could not connect to Unity")
            return False
    finally:
        conn.disconnect()

def test_log_analyzer():
    """Test Unity log analyzer."""
    logger.info("\nTesting Unity log analyzer...")
    analyzer = UnityLogAnalyzer()
    
    # Get diagnostic info
    diagnostics = analyzer.get_diagnostic_info()
    
    logger.info(f"Unity running: {diagnostics['unity_running']}")
    logger.info(f"Safe mode: {diagnostics['safe_mode']}")
    logger.info(f"Log files found: {len(diagnostics['log_files_found'])}")
    
    for log_file in diagnostics['log_files_found']:
        logger.info(f"  - {log_file}")
    
    if diagnostics['safe_mode']:
        logger.warning(f"Unity is in Safe Mode! Log: {diagnostics['safe_mode_log']}")
        
        if diagnostics['compiler_errors']:
            logger.error(f"Found {len(diagnostics['compiler_errors'])} compiler errors:")
            for i, error in enumerate(diagnostics['compiler_errors'][:3]):
                logger.error(f"  {i+1}. {error.get('line', 'Unknown error')}")
    
    return diagnostics

async def test_server_startup():
    """Test server startup behavior."""
    logger.info("\nTesting server startup behavior...")
    
    # Import server components
    from server import server_lifespan
    from mcp.server.fastmcp import FastMCP
    
    # Create a minimal MCP instance for testing
    test_mcp = FastMCP("test-server", description="Test server")
    
    # Test the lifespan
    async with server_lifespan(test_mcp) as context:
        bridge = context.get('bridge')
        log_analyzer = context.get('log_analyzer')
        
        if bridge:
            logger.info("✓ Bridge connection established")
        else:
            logger.warning("✗ No bridge connection (expected if Unity not running)")
        
        if log_analyzer:
            logger.info("✓ Log analyzer initialized")
        else:
            logger.error("✗ Log analyzer not initialized")
        
        # Test diagnostics tool
        from tools.unity_diagnostics import register_unity_diagnostics_tools
        register_unity_diagnostics_tools(test_mcp)
        
        # Create a mock context
        class MockContext:
            def __init__(self, bridge, log_analyzer):
                self.bridge = bridge
                self.log_analyzer = log_analyzer
        
        mock_ctx = MockContext(bridge, log_analyzer)
        
        # Test the diagnostics tool
        logger.info("\nTesting diagnostics tool...")
        
        # Get the actual tool function
        for tool in test_mcp._tools.values():
            if tool.name == 'get_unity_diagnostics':
                result = await tool.handler(mock_ctx)
                if result['success']:
                    logger.info("✓ Diagnostics tool working")
                    data = result['data']
                    logger.info(f"  Connection status: {data['connection_status']}")
                    if 'advice' in data:
                        logger.info(f"  Advice: {data['advice']}")
                else:
                    logger.error(f"✗ Diagnostics tool failed: {result.get('error')}")
                break

def main():
    """Run all tests."""
    logger.info("Unity MCP Server Test Suite")
    logger.info("=" * 50)
    
    # Test 1: Connection
    connection_ok = test_connection()
    
    # Test 2: Log analyzer
    diagnostics = test_log_analyzer()
    
    # Test 3: Server startup
    asyncio.run(test_server_startup())
    
    # Summary
    logger.info("\n" + "=" * 50)
    logger.info("Test Summary:")
    
    if connection_ok:
        logger.info("✓ Unity connection: OK")
    else:
        logger.warning("✗ Unity connection: FAILED")
        
        if diagnostics['safe_mode']:
            logger.error("  → Unity is in Safe Mode due to compiler errors")
        elif not diagnostics['unity_running']:
            logger.warning("  → Unity does not appear to be running")
        else:
            logger.warning("  → Unity is running but MCP Bridge not responding")
    
    logger.info("\nThe server will still start even without Unity connection.")
    logger.info("Use the 'get_unity_diagnostics' tool for troubleshooting.")

if __name__ == "__main__":
    main()