"""
Test script for performance monitoring and profiling functionality.
"""

import asyncio
import json
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


async def test_performance_management():
    """Test performance monitoring and profiling operations."""
    
    server_params = StdioServerParameters(
        command="uv",
        args=["run", "server.py"],
        env=None
    )
    
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            
            print("Testing Performance Monitoring and Profiling...")
            
            # Test 1: Start profiling
            print("\n1. Starting profiling...")
            result = await session.call_tool(
                "manage_performance",
                {
                    "operation": "start_profiling",
                    "profiler_type": "cpu",
                    "duration": 10.0
                }
            )
            print(f"Start profiling result: {result.content}")
            
            # Test 2: Get performance data
            print("\n2. Getting performance data...")
            result = await session.call_tool(
                "manage_performance",
                {
                    "operation": "get_performance_data",
                    "data_type": "current"
                }
            )
            print(f"Performance data result: {result.content}")
            
            # Test 3: Analyze performance
            print("\n3. Analyzing performance...")
            result = await session.call_tool(
                "manage_performance",
                {
                    "operation": "analyze_performance",
                    "analysis_type": "bottlenecks",
                    "time_range": "last_minute"
                }
            )
            print(f"Performance analysis result: {result.content}")
            
            # Test 4: List profilers
            print("\n4. Listing profilers...")
            result = await session.call_tool(
                "manage_performance",
                {
                    "operation": "list_profilers"
                }
            )
            print(f"List profilers result: {result.content}")
            
            # Test 5: Configure profiler
            print("\n5. Configuring profiler...")
            result = await session.call_tool(
                "manage_performance",
                {
                    "operation": "configure_profiler",
                    "profiler_type": "memory",
                    "settings": {
                        "sample_rate": 100,
                        "track_allocations": True,
                        "deep_profiling": False
                    }
                }
            )
            print(f"Configure profiler result: {result.content}")
            
            # Test 6: Memory profiling - take snapshot
            print("\n6. Taking memory snapshot...")
            result = await session.call_tool(
                "memory_profiling_operations",
                {
                    "operation": "take_snapshot",
                    "snapshot_name": "TestSnapshot"
                }
            )
            print(f"Memory snapshot result: {result.content}")
            
            # Test 7: Get memory usage
            print("\n7. Getting memory usage...")
            result = await session.call_tool(
                "memory_profiling_operations",
                {
                    "operation": "get_memory_usage",
                    "detailed": True
                }
            )
            print(f"Memory usage result: {result.content}")
            
            # Test 8: CPU profiling - start
            print("\n8. Starting CPU profiling...")
            result = await session.call_tool(
                "cpu_profiling_operations",
                {
                    "operation": "start_cpu_profiling",
                    "deep_profiling": True
                }
            )
            print(f"Start CPU profiling result: {result.content}")
            
            # Test 9: Get CPU usage
            print("\n9. Getting CPU usage...")
            result = await session.call_tool(
                "cpu_profiling_operations",
                {
                    "operation": "get_cpu_usage",
                    "include_threads": True
                }
            )
            print(f"CPU usage result: {result.content}")
            
            # Test 10: Rendering profiling
            print("\n10. Starting rendering profiling...")
            result = await session.call_tool(
                "rendering_profiling_operations",
                {
                    "operation": "start_rendering_profiling",
                    "track_draw_calls": True,
                    "track_gpu_time": True
                }
            )
            print(f"Start rendering profiling result: {result.content}")
            
            # Test 11: Get rendering stats
            print("\n11. Getting rendering stats...")
            result = await session.call_tool(
                "rendering_profiling_operations",
                {
                    "operation": "get_rendering_stats"
                }
            )
            print(f"Rendering stats result: {result.content}")
            
            # Test 12: Performance benchmarking
            print("\n12. Running performance benchmark...")
            result = await session.call_tool(
                "performance_benchmarking",
                {
                    "operation": "run_benchmark",
                    "benchmark_type": "frame_rate",
                    "duration": 5.0,
                    "settings": {
                        "target_fps": 60,
                        "measure_frame_time": True
                    }
                }
            )
            print(f"Benchmark result: {result.content}")
            
            # Test 13: Export performance data
            print("\n13. Exporting performance data...")
            result = await session.call_tool(
                "manage_performance",
                {
                    "operation": "export_data",
                    "export_format": "json",
                    "file_path": "performance_data.json",
                    "data_types": ["cpu", "memory", "rendering"]
                }
            )
            print(f"Export data result: {result.content}")
            
            # Test 14: Clear performance data
            print("\n14. Clearing performance data...")
            result = await session.call_tool(
                "manage_performance",
                {
                    "operation": "clear_data",
                    "data_types": ["all"]
                }
            )
            print(f"Clear data result: {result.content}")


if __name__ == "__main__":
    asyncio.run(test_performance_management())